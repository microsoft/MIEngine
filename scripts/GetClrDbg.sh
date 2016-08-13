#! /bin/bash

script_dir="$( cd $( dirname "$0" ); pwd )"

# ClrDbg Meta Version. It could be something like 'latest', 'vs2015', or a fully specified version. 
__ClrDbgMetaVersion=

# Install directory of the clrdbg relative to the script. 
__InstallLocation=

# When SkipDownloads is set to true, no access to internet is made.
__SkipDownloads=false

# Launches ClrDbg after downloading/upgrading.
__LaunchClrDbg=false

# Removes existing installation of ClrDbg in the Install Location.
__RemoveExisting=false

# Internal, fully specified version of the ClrDbg. Computed when the meta version is used.
__ClrDbgVersion=

print_help()
{
    echo 'GetClrDbg.sh [-rsdh] -v V [-l L]'
    echo ''
    echo 'This script downloads and configures clrdbg, the Cross Platform .NET Debugger'
    echo '-r    Removes the existing installation before installing the specified version. Can be used for a clean install of the debugger.'
    echo '-s    Skips any steps which requires downloading from the internet.'
    echo '-d    Launches debugger after the script completion.'
    echo '-h    Prints usage information.'
    echo '-v V  Version V can be "latest" or a version number such as 14.0.25109-preview-2865786'
    echo '-l L  Location L is relative location of where the debugger should be installed.'
    echo ''
    echo 'Legacy commandline'
    echo '  GetClrDbg.sh <version> [<install path>]'   
    echo '  If <install path> is not specified, clrdbg will be installed to the directory'
    echo '  from which this script was executed.' 
    echo '  <version> can be "latest" or a version number such as 14.0.25109-preview-2865786'
}

# Sets __RuntimeID as given by 'dotnet --info'
get_dotnet_runtime_id()
{
    # example output of dotnet --info
    # .NET Command Line Tools (1.0.0-beta-002194)
    #
    # Product Information:
    #  Version:     1.0.0-beta-002194
    #  Commit Sha:  a28369bfe0
    #
    # Runtime Environment:
    #  OS Name:     ubuntu
    #  OS Version:  14.04
    #  OS Platform: Linux
    #  RID:         ubuntu.14.04-x64

    # Get the output of dotnet --info
    info="$(dotnet --info)"

    # filter to the line that contains the RID. Output still contains spaces. Example: '         ubuntu.14.04-x64'
    rid_line="$(echo "${info}" | awk 'BEGIN { FS=":" }{ if ( $1 ~ "RID" ) { print $2 } }')"

    # trim whitespace from awk return
    rid="$(echo -e "${rid_line}" | tr -d '[[:space:]]')"

    __RuntimeID=$rid
}

# Produces project.json in the current directory
# $1 is Runtime ID
generate_project_json()
{
    if [ -z $1 ]; then
        echo "Error: project.json cannot be produced without a Runtime ID being provided."
        exit 1
    fi

    echo "{"                                                                >  project.json
    echo "   \"dependencies\": {"                                           >> project.json
    echo "       \"Microsoft.VisualStudio.clrdbg\": \"$__ClrDbgVersion\""   >> project.json
    echo "   },"                                                            >> project.json
    echo "   \"frameworks\": {"                                             >> project.json
    echo "       \"netcoreapp1.0\": {"                                      >> project.json
    echo "          \"imports\": [ \"dnxcore50\", \"portable-net45+win8\" ]" >> project.json
    echo "       }"                                                         >> project.json
    echo "   },"                                                            >> project.json
    echo "   \"runtimes\": {"                                               >> project.json
    echo "      \"$1\": {}"                                                 >> project.json
    echo "   }"                                                             >> project.json
    echo "}"                                                                >> project.json
}

# Produces NuGet.config in the current directory
generate_nuget_config()
{
    echo "<?xml version=\"1.0\" encoding=\"utf-8\"?>"                                                           >  NuGet.config
    echo "<configuration>"                                                                                      >> NuGet.config
    echo "  <packageSources>"                                                                                   >> NuGet.config
    echo "      <clear />"                                                                                      >> NuGet.config
    echo "      <add key=\"api.nuget.org\" value=\"https://api.nuget.org/v3/index.json\" />"                    >> NuGet.config
    echo "  </packageSources>"                                                                                  >> NuGet.config
    echo "</configuration>"                                                                                     >> NuGet.config
}

# Parses and populates the arguments
parse_and_get_arguments()
{
    while getopts "v:l:srhd" opt; do
        case $opt in
            v)
                __ClrDbgMetaVersion=$OPTARG;
                ;;
            l)
                __InstallLocation=$OPTARG
                ;;
            r)
                __RemoveExisting=true
                ;;
            s)
                __SkipDownloads=true
                ;;
            d)
                __LaunchClrDbg=true
                ;;
            h)            
                print_help
                exit 1
                ;;    
            \?)
                echo "Error: Invalid Option: -$OPTARG"
                print_help
                exit 1
                ;;
            :)
                echo "Error: Option expected for -$OPTARG"
                print_help
                exit 1
                ;;
        esac
    done
}

# Parses and populates the arguments for the legacy commandline.
get_legacy_arguments()
{
    if [ ! -z "$1" ]; then
        __ClrDbgMetaVersion=$1
    fi
    if [ ! -z "$2" ]; then
        __InstallLocation=$2
    fi
}

# Prints the arguments to stdout for the benefit of the user and does a quick sanity check.
print_and_verify_arguments()
{
    echo "Using arguments"
    echo "    Version                    : '$__ClrDbgMetaVersion'"
    echo "    Location                   : '$__InstallLocation'"
    echo "    SkipDownloads              : '$__SkipDownloads'"
    echo "    LaunchClrDbgAfter          : '$__LaunchClrDbg'"
    echo "    RemoveExistingInstallation : '$__RemoveExisting'"

    if [ -z $__ClrDbgMetaVersion ]; then
        echo "Error: Version is not an optional parameter"
        exit 1
    fi
}

# Converts relative location of the installation directory to absolute location.
convert_install_path_to_absolute()
{
    if [ -z $__InstallLocation ]; then
        __InstallLocation=$pwd
    else
        __InstallLocation=$script_dir/$__InstallLocation
    fi    
}

# Computes the CLRDBG version
set_clrdbg_version()
{    
    # This case statement is done on the lower case version of version_string
    # Add new version constants here
    # 'latest' version may be updated
    # all other version contstants i.e. 'vs2015u2' may not be updated after they are finalized
    version_string="$(echo $1 | awk '{print tolower($0)}')"
    case $version_string in
        latest)
            __ClrDbgVersion=14.0.25520-preview-3139256
            ;;
        vs2015u2)
            __ClrDbgVersion=14.0.25520-preview-3139256 #This version is now locked and should not be updated.
            ;;
        *)
            simpleVersionRegex="^[0-9].*"
            if ! [[ "$1" =~ $simpleVersionRegex ]]; then
                echo "Error: '$1' does not look like a valid version number."
                exit 1
            fi
            __ClrDbgVersion=$1
            ;;
    esac
}

# Prepares installation directory.
prepare_install_location()
{
    if [ -z $__InstallLocation ]; then
        echo "Error: Install location is not set"
        exit 1
    fi

    if [ -f "$__InstallLocation" ]; then
        echo "Error: Path '$2' points to a regular file and not a directory"
        exit 1
    elif [ ! -d "$__InstallLocation" ]; then
        echo 'Info: Creating install directory'
        mkdir -p $__InstallLocation
        if [ "$?" -ne 0 ]; then
            echo "Error: Unable to create install directory: '$2'"
            exit 1
        fi
    fi   
}

# Removes installation directory if remove option is specified.
process_removal()
{
    if [ "$__RemoveExisting" = true ]; then
        echo "Info: Attempting to remove '$__InstallLocation'"
        if [ -d $__InstallLocation ]; then
            lsof $__InstallLocation > /dev/null 2>&1
            if [ "$?" -eq 0 ]; then
                echo "Error: files are being used in location '$__InstallLocation'"
                exit 1
            fi

            rm -rf $__InstallLocation
            if [ "$?" -ne 0 ]; then
                echo "Error: files could not be removed from '$__InstallLocation'"
                exit 1
            fi
        fi
    fi
}

# Checks if the existing copy is the latest version.
check_latest()
{
    __SuccessFile="$__InstallLocation/success.txt" 
    if [ -f "$__SuccessFile" ]; then
        __LastInstalled=$(cat "$__SuccessFile")
        echo "Info: LastIntalled version of clrdbg is '$__LastInstalled'"
        if [ "$__ClrDbgVersion"="__LastInstalled" ]; then
            __SkipDownloads=true
            echo "Info: ClrDbg is upto date"
        fi
    else
        echo "Info: Previous installation at "$__InstallLocation" not found"
    fi
}

if [ -z "$1" ]; then
    print_help
    exit 1
else
    parse_and_get_arguments $@

    if [ -z $__ClrDbgMetaVersion ]; then
        get_legacy_arguments $@
    fi    
fi

convert_install_path_to_absolute
print_and_verify_arguments
set_clrdbg_version "$__ClrDbgMetaVersion"
echo "Info: Using clrdbg version '$__ClrDbgVersion'"

process_removal
check_latest


if [ "$__SkipDownloads" = true ]; then
    echo "Info: Skipping downloads"
else
    prepare_install_location

    pushd $__InstallLocation > /dev/null 2>&1
    if [ "$?" -ne 0 ]; then
        echo "Error: Unable to cd to install directory '$__InstallLocation'"
        exit 1
    fi

    # For the rest of this script we can assume the working directory is the install path

    echo 'Info: Determinig Runtime ID'
    __RuntimeID=
    get_dotnet_runtime_id
    if [ -z $__RuntimeID ]; then
        echo "Error: Unable to determine dotnet Runtime ID"
        echo "GetClrDbg.sh requires .NET CLI Tools version >= 1.0.0-beta-002173. Please make sure your install of .NET CLI is up to date"
        exit 1
    fi
    echo "Info: Using Runtime ID '$__RuntimeID'"

    echo 'Info: Generating project.json'
    generate_project_json $rid

    echo 'Info: Generating NuGet.config'
    generate_nuget_config

    # dotnet restore and publish add color to their output
    # I have found that the extra characters to provide color can corrupt
    # the shell output when running this as part of docker build
    # Therefore, I redirect the output of these commands to a log
    echo 'Info: Executing dotnet restore'
    dotnet restore > dotnet_restore.log 2>&1
    if [ $? -ne 0 ]; then
        echo "dotnet restore failed"
        exit 1
    fi

    echo 'Info: Executing dotnet publish'
    dotnet publish -o . > dotnet_publish.log 2>&1
    if [ $? -ne 0 ]; then
        echo "dotnet publish failed"
        exit 1
    fi

    echo "$__ClrDbgVersion" > success.txt
    popd > /dev/null 2>&1

    echo "Info: Successfully installed clrdbg at '$__InstallLocation'"    
fi


if [ "$__LaunchClrDbg" = true ]; then
    "$__InstallLocation/clrdbg" "--interpreter=mi"
    exit $?
fi

exit 0
