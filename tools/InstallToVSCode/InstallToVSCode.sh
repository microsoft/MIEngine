#!/bin/bash

script_dir=`dirname $0`

print_help()
{
    echo 'InstallToVSCode.sh <link|copy> <alpha|insider|stable> <open-debug-ad7-dir> -d <clrdbg-binaries>'
    echo ''
    echo 'This script is used to copy files needed to enable MIEngine based debugging'
    echo 'into VS Code.'
    echo ''
    echo ' link : Create links to files instead of copying them. With this mode, it'
    echo '   is possible to rebuild MIEngine or OpenDebugAD7 without re-running this'
    echo '   script. **NOTE**: Using this option requires starting OpenDebugAD7 with'
    echo "   '--adapterDirectory=\${env.HOME}/.MIEngine-VSCode-Debug'"
    echo ' copy : Copy files to the output directory'
    echo ''
    echo ' alpha: Install to VSCode alpha'
    echo ' insider: Install to VSCode insider'
    echo ' stable: Install to VSCode stable'
    echo ''
    echo ' open-debug-ad7-dir : Root of the OpenDebugAD7 repo'
    echo ' clrdbg-binaries-dir : Directory containing clrdbg binaries'
    echo ''
    echo 'Example:'
    echo "$script_dir/InstallToVSCode.sh link alpha /Volumes/dd/OpenDebugAD7 -d ~/clrdbg/out/Linux/bin/x64.Debug/clrdbg"
}

# Copies a file to another file or directory
# arg1: source file
# arg2: destination file or directory
copy_file()
{
    cp $1 $2
    if [ $? -ne 0 ]
    then
        echo "ERROR: Failed to copy '$1' to '$2'"
        InstallError=1
    fi
}

# Links one file to the other
# arg1: Source file
# arg2: Destination file
link_file()
{
    if [ ! -f "$1" ]
    then
        echo "ERROR: '$1' does not exist"
        InstallError=1
        return 1
    fi

    if [ -f "$2" ]
    then
        rm $2
        if [ $? -ne 0 ]
        then
            echo "ERROR: Unable to remove link target file '$2'"
            InstallError=1
            return 1
        fi
    fi

    ln -fs $1 $2
    if [ $? -ne 0 ]
    then
        echo "ERROR: Failed to link file '$1' to '$2'"
        InstallError=1
        return 1
    fi

    return 0
}

install_file()
{
    $InstallAction $1 $DESTDIR/$2$(basename $1)
}

install_module()
{
    modulPath=$1
    moduleName=$(basename $modulPath)
    $InstallAction $modulPath $DESTDIR/$2$moduleName
    if [ $? -ne 0 ]; then
        return $?
    fi

    sourcePdb=${modulPath%\.[^.]*}.pdb
    if [ ! -f "$sourcePdb" ]; then
        if [ "$3" == "ignoreMissingPdbs" ]; then
            return 0
        fi
    fi

    $InstallAction $sourcePdb $DESTDIR/$2${moduleName%\.[^.]*}.pdb
}

# Setup the symbolic link from under the extension directory to DESTDIR
# arg 1 : symbolic link
# arg 2 : DESTDIR
SetupSymLink()
{
    # If arg1 already exists, remove it first
    if [ -d $1 ]; then
        # If it is already a link, remove it
        if [ -L $1 ]; then
            rm $1
        else
            rm -r $1
        fi
    fi
    
    ln -s $2 $1
    return $?
}

InstallAction=
if [ -z "$1" ]; then
    print_help
    exit 1
elif [ "$1" == "-h" ]; then
    print_help
    exit 1
elif [ "$1" == "link" ]; then
    InstallAction=link_file
elif [ "$1" == "copy" ]; then
    InstallAction=copy_file
else
    echo "ERROR: Unexpected first argument '$1'. Expected 'link' or 'copy'."
    exit 1
fi

VSCodeDirName=
if [ "$2" == "alpha" ]; then
    VSCodeDirName=".vscode-alpha"
elif [ "$2" == "insider" ]; then
    VSCodeDirName=".vscode-insider"
elif [ "$2" == "stable" ]; then
    VSCodeDirName=".vscode"
else
    echo "ERROR: Unexpected second argument '$2'. Expected 'alpha', 'insider' or 'stable'."
    exit 1
fi

OpenDebugAD7Dir=${3:?"ERROR: OpenDebugAD7 directory must be specified. See -h for usage."}
OpenDebugAD7BinDir=$OpenDebugAD7Dir/bin/Debug-PortablePDB
[ ! -d "$OpenDebugAD7Dir/src/OpenDebugAD7" ] && echo "ERROR: argmument 3 is invalid. '$OpenDebugAD7Dir/src/OpenDebugAD7' does not exist." && exit 1
[ ! -f "$OpenDebugAD7BinDir/OpenDebugAD7.dll" ] && echo "ERROR: $OpenDebugAD7BinDir/OpenDebugAD7.dll does not exist." && exit 1

DropDir=$script_dir/../../bin/Debug-PortablePDB
if [ ! -f "$DropDir/Microsoft.MIDebugEngine.dll" ]
then
    echo "ERROR: '$DropDir/Microsoft.MIDebugEngine.dll' has not been built."
    exit 1
fi

# Remove the relative path from DropDir
pushd $DropDir >/dev/null
DropDir=$(pwd)
popd >/dev/null

[ ! "$4" == "-d" ] && echo "ERROR: Bad command line argument. Expected '-d <clrdbg-dir>'." && exit 1
CLRDBGBITSDIR=${5:?"ERROR: Clrdbg binaries directory must be specified with -d option. See -h for usage."}
[ ! -f "$CLRDBGBITSDIR/clrdbg" ] && echo "ERROR: $CLRDBGBITSDIR/clrdbg does not exist." && exit 1
DESTDIR=$HOME/.MIEngine-VSCode-Debug

VSCodeExtensionsRoot=$HOME/$VSCodeDirName/extensions
[ ! -d "$VSCodeExtensionsRoot" ] && echo "ERROR: $VSCodeExtensionsRoot does not exist." && exit 1

CSharpExtensionRoot="$(ls -d $VSCodeExtensionsRoot/ms-vscode.csharp-* 2>/dev/null)" 
[ "$CSharpExtensionRoot" == "" ] && echo "ERROR: C# extension is not installed in VS Code. No directory matching '$VSCodeExtensionsRoot/ms-vscode.csharp-*' found." && exit 1

num_results=$(echo "$CSharpExtensionRoot" | wc -l)
[ "$num_results" != "1" ] && echo "ERROR: more than one instance of the C# extension is found under '$VSCodeExtensionsRoot/ms-vscode.csharp-*'." && exit 1

if [ -d "$DESTDIR" ]
then
    rm -r "$DESTDIR"
    [ $? -ne 0 ] && echo "ERROR: Unable to clean destination directory '$DESTDIR'." && exit 1
fi

mkdir -p "$DESTDIR"
[ $? -ne 0 ] && echo "ERROR: unable to create destination directory '$DESTDIR'." && exit 1

hash dotnet 2>/dev/null 
[ $? -ne 0 ] && echo "ERROR: The .NET CLI is not installed. see: http://dotnet.github.io/getting-started/" && exit 1

SetupSymLink "$CSharpExtensionRoot/coreclr-debug/debugAdapters" "$DESTDIR"
[ $? -ne 0 ] && echo "ERROR: Unable to link $CSharpExtensionRoot/coreclr-debug/debugAdapters to $DESTDIR" && exit 1

pushd $script_dir/CLRDependencies 1>/dev/null 2>/dev/null
[ $? -ne 0 ] && echo "ERROR: Unable to find CLRDependencies directory???" && exit 1

dotnet restore
[ $? -ne 0 ] && echo "ERROR: dotnet restore failed." && exit 1

dotnet publish -o "$DESTDIR"
[ $? -ne 0 ] && echo "ERROR: dotnet publish failed." && exit 1

popd >/dev/null

# Work arround https://github.com/dotnet/coreclr/issues/3164
[ -f $DESTDIR/libcoreclrtraceptprovider.so ] && rm $DESTDIR/libcoreclrtraceptprovider.so
[ -f $DESTDIR/libcoreclrtraceptprovider.dylib ] && rm $DESTDIR/libcoreclrtraceptprovider.dylib

mv "$DESTDIR/dummy" "$DESTDIR/OpenDebugAD7"
[ $? -ne 0 ] && echo "ERROR: Unable to move OpenDebugAD7 executable." && exit 1

InstallError=
install_module "$OpenDebugAD7BinDir/dar.exe"
install_module "$OpenDebugAD7BinDir/xunit.console.netcore.exe" "" ignoreMissingPdbs 
for dll in $(ls $OpenDebugAD7BinDir/*.dll); do
    install_module "$dll" "" ignoreMissingPdbs
done

echo ''
echo "Installing clrdbg bits from $CLRDBGBITSDIR"

for clrdbgFile in $(ls $CLRDBGBITSDIR/*); do
    if [ -f "$clrdbgFile" ]; then
        install_file "$clrdbgFile"
    fi
done
    
for directory in $(ls -d $CLRDBGBITSDIR/*/); do
    directory_name=$(basename $directory)
        
    if [ ! -d "$DESTDIR/$directory_name" ]; then
        mkdir "$DESTDIR/$directory_name"
    fi
        
    for dll in $(ls $directory/*.dll); do
        install_file "$dll" "$directory_name/"
    done
done

install_file "$script_dir/coreclr/coreclr.ad7Engine.json"

for dll in Microsoft.MICore.dll Microsoft.MIDebugEngine.dll
do 
    install_module "$DropDir/$dll"
done

if [ ! -f "$OpenDebugAD7Dir/.vscode/launch.json" ]; then
    cp "$OpenDebugAD7Dir/.vscode/example-launch.json" "$OpenDebugAD7Dir/.vscode/launch.json"
    [ $? -ne 0 ] && echo "ERROR: failed to setup launch.json file. Copy to $OpenDebugAD7Dir/.vscode/launch.json failed." && exit 1
fi


echo ''
if [ ! -z "$InstallError" ]; then
    echo "ERROR: Failed to copy one or more files."
    echo ''
    exit 1
fi

# Write out an install.complete file so that the C# extension doesn't try to restore.
echo "InstallToVSCode.sh done">$DESTDIR/install.complete

echo "InstallToVSCode.sh succeeded. Open directory '$OpenDebugAD7Dir' in VS Code" 
echo "to debug. Edit .vscode/launch.json before launching."
echo ""
exit 0
