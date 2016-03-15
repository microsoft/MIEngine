#! /bin/bash

script_dir=`dirname $0`

print_help()
{
    echo 'GetClrDbg.sh <version> [<install path>]'
    echo ''
    echo 'This script downloads and configures clrdbg, the Cross Platform .NET Debugger'
    echo 'If <install path> is not specified, clrdbg will be installed to the directory'
    echo 'from which this script was executed.' 
    echo '<version> can be "latest" or a version number such as 14.0.25109-preview-2865786'
}

# Produces project.json in the current directory
generate_project_json()
{
    echo "{"                                                                >  project.json
    echo "   \"dependencies\": {"                                           >> project.json
    echo "       \"Microsoft.VisualStudio.clrdbg\": \"$__ClrDbgVersion\""   >> project.json
    echo "   },"                                                            >> project.json
    echo "   \"frameworks\": {"                                             >> project.json
    echo "       \"dnxcore50\": { }"                                        >> project.json
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
    echo "      <add key=\"dotnet-core\" value=\"https://www.myget.org/F/dotnet-core/api/v3/index.json\" />"    >> NuGet.config
    echo "      <add key=\"api.nuget.org\" value=\"https://api.nuget.org/v3/index.json\" />"                    >> NuGet.config
    echo "      <add key=\"coreclr-debug\" value=\"https://www.myget.org/F/coreclr-debug/api/v3/index.json\" />" >> NuGet.config
    echo "  </packageSources>"                                                                                  >> NuGet.config
    echo "</configuration>"                                                                                     >> NuGet.config
}

if [ -z "$1" ]; then
    print_help
    exit 1
elif [ "$1" == "-h" ]; then
    print_help
    exit 1
fi

if [ "$1" == "latest" ]; then
    __ClrDbgVersion=14.0.25109-preview-2865786
else
    simpleVersionRegex="^[0-9].*"
    if ! [[ "$1" =~ $simpleVersionRegex ]]; then
        echo "Error: '$1' does not look like a valid version number."
        exit 1
    fi
    __ClrDbgVersion=$1
fi

__InstallPath=$PWD
if [ ! -z "$2" ]; then
    if [ -f "$2" ]; then
        echo "Error: Path '$2' points to a regular file and not a directory"
        exit 1
    elif [ ! -d "$2" ]; then
        echo 'Info: Creating install directory'
        mkdir -p $2
        if [ "$?" -ne 0 ]; then
            echo "Error: Unable to create install directory: '$2'"
            exit 1
        fi
    fi
    __InstallPath=$2
fi

pushd $__InstallPath > /dev/null 2>&1
if [ "$?" -ne 0 ]; then
    echo "Error: Unable to cd to install directory '$__InstallPath'"
    exit 1
fi

echo 'Info: Generating project.json'
generate_project_json

echo 'Info: Generating NuGet.config'
generate_nuget_config

# dotnet restore and publish add color to their output
# I have found that the extra characters to provide color can corrupt
# the shell output when running this as part of docker build
# Therefore, I redirect the output of these commands to a log
echo 'Info: Executing dotnet restore'
dotnet restore > $__InstallPath/dotnet_restore.log 2>&1
if [ $? -ne 0 ]; then
    echo "dotnet restore failed"
    exit 1
fi

echo 'Info: Executing dotnet publish'
dotnet publish -o $__InstallPath > $__InstallPath/dotnet_publish.log 2>&1
if [ $? -ne 0 ]; then
    echo "dotnet publish failed"
    exit 1
fi

popd > /dev/null 2>&1

echo "Successfully installed clrdbg at '$__InstallPath'"
exit 0
