#!/bin/bash

DefaultDestDir=$HOME/.vscode/extensions/coreclr-debug
script_dir=`dirname $0`

print_help()
{
    echo 'InstallToVSCode.sh <-c|-l> [--pdbs] <OpenDebugAD7-dir> -d <clrdbg-binaries-dir> [destination-dir]'
    echo ''
    echo 'This script is used to copy files needed to enable MIEngine based debugging'
    echo 'into VS Code.'
    echo ''
    echo ' -l : Create links to files instead of copying them. With this mode, it'
    echo '   is possible to rebuild MIEngine or OpenDebugAD7 without re-running this'
    echo '   script.'
    echo ' -c : Copy files to the output directory'
    echo ' --pdbs : Copy PDB files in addition to the dlls'
    echo ' open-debug-ad7-dir : Directory which contains OpenDebugAD7.exe'
    echo ' clrdbg-binaries-dir : Directory containing clrdbg binaries'
    echo ' destination-dir: Directory to install to. By default this is:'
    echo "    $DefaultDestDir"
    echo ''
    echo 'Example:'
    echo "$script_dir/InstallToVSCode.sh -l --pdbs /Volumes/dd/OpenDebugAD7/bin/Debug-PortablePDB -d ~/clrdbg/out/Linux/bin/x64.Debug/clrdbg $HOME/.vscode-alpha/extensions/coreclr-debug"
}

# Copies a file to another file or directory
# arg1: source file
# arg2: destination file or directory
copy_file()
{
    echo "cp $1 $2"
    cp $1 $2
    if [ $? -ne 0 ]
    then
        echo "ERROR: Failed to copy '$1'"
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

    echo "ln -fs $1 $2"
    ln -fs $1 $2
    if [ $? -ne 0 ]
    then
        echo "ERROR: Failed to link file '$1'"
        InstallError=1
        return 1
    fi

    return 0
}

install_file()
{
    $InstallAction $1 $DESTDIR/$2/$(basename $1)
}

install_module()
{
    modulPath=$1
    moduleName=$(basename $modulPath)
    $InstallAction $modulPath $DESTDIR/$2/$moduleName
    if [ $? -ne 0 ]; then
        return $?
    fi

    if [ "$install_pdbs" == "1" ]; then
        sourcePdb=${modulPath%\.[^.]*}.pdb
        if [ ! -f "$sourcePdb" ]; then
            if [ "$3" == "ignoreMissingPdbs" ]; then
                return 0
            fi
        fi

        $InstallAction $sourcePdb $DESTDIR/$2/${moduleName%\.[^.]*}.pdb
    fi
}

InstallAction=
if [ -z "$1" ]; then
    print_help
    exit 1
elif [ "$1" == "-h" ]; then
    print_help
    exit 1
elif [ "$1" == "-l" ]; then
    InstallAction=link_file
elif [ "$1" == "-c" ]; then
    InstallAction=copy_file
else
    echo "ERROR: Unexpected first argument '$1'. Expected '-l' or '-c'."
    exit 1
fi

install_pdbs=0
if [ "$2" == "--pdbs" ]; then
    install_pdbs=1
    shift
fi

OpenDebugAD7Dir=${2:?"ERROR: OpenDebugAD7 binaries directory must be specified. See -h for usage."}
[ ! -f "$OpenDebugAD7Dir/OpenDebugAD7.dll" ] && echo "ERROR: $OpenDebugAD7Dir/OpenDebugAD7.dll does not exist." && exit 1

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

[ ! "$3" == "-d" ] && echo "ERROR: Bad command line argument. Expected '-d <clrdbg-dir>'." && exit 1
CLRDBGBITSDIR=${4:?"ERROR: Clrdbg binaries directory must be specified with -d option. See -h for usage."}
[ ! -f "$CLRDBGBITSDIR/clrdbg" ] && echo "ERROR: $CLRDBGBITSDIR/clrdbg does not exist." && exit 1
DESTDIR=$5

if [ -z "$DESTDIR" ]
then
    DESTDIR=$DefaultDestDir
fi

hash dotnet 2>/dev/null 
[ $? -ne 0 ] && echo "ERROR: The .NET CLI is not installed. see: http://dotnet.github.io/getting-started/" && exit 1

if [ -d "$DESTDIR" ]
then
    rm -r "$DESTDIR"
    [ $? -ne 0 ] && echo "ERROR: Unable to clean destination directory '$DESTDIR'." && exit 1
fi

mkdir -p "$DESTDIR/debugAdapters"
[ $? -ne 0 ] && echo "ERROR: unable to create destination directory '$DESTDIR/debugAdapters'." && exit 1

pushd $script_dir/CLRDependencies 1>/dev/null 2>/dev/null
[ $? -ne 0 ] && echo "ERROR: Unable to find CLRDependencies directory???" && exit 1

dotnet restore
[ $? -ne 0 ] && echo "ERROR: dotnet restore failed." && exit 1

dotnet publish -o "$DESTDIR/debugAdapters"
[ $? -ne 0 ] && echo "ERROR: dotnet publish failed." && exit 1

popd >/dev/null

# Work arround https://github.com/dotnet/coreclr/issues/3164
[ -f $DESTDIR/debugAdapters/libcoreclrtraceptprovider.so ] && rm $DESTDIR/debugAdapters/libcoreclrtraceptprovider.so
[ -f $DESTDIR/debugAdapters/libcoreclrtraceptprovider.dylib ] && rm $DESTDIR/debugAdapters/libcoreclrtraceptprovider.dylib

mv "$DESTDIR/debugAdapters/dummy" "$DESTDIR/debugAdapters/OpenDebugAD7"
[ $? -ne 0 ] && echo "ERROR: Unable to move OpenDebugAD7 executable." && exit 1

set InstallError=
install_module "$OpenDebugAD7Dir/dar.exe" debugAdapters
install_module "$OpenDebugAD7Dir/xunit.console.netcore.exe" debugAdapters
for dll in $(ls $OpenDebugAD7Dir/*.dll); do
    install_module "$dll" debugAdapters ignoreMissingPdbs
done

echo ''
echo "Installing clrdbg bits from $CLRDBGBITSDIR"

for clrdbgFile in $(ls $CLRDBGBITSDIR/*); do
    if [ -f "$clrdbgFile" ]; then
        install_file "$clrdbgFile" debugAdapters
    fi
done
    
for directory in $(ls -d $CLRDBGBITSDIR/*/); do
    echo "Installing clrdbg bits from $directory..."
    directory_name=$(basename $directory)
        
    if [ ! -d "$DESTDIR/debugAdapters/$directory_name" ]; then
        mkdir "$DESTDIR/debugAdapters/$directory_name"
    fi
        
    for dll in $(ls $directory/*.dll); do
        install_file "$dll" debugAdapters/$directory_name
    done
done

copy_file "$script_dir/coreclr/package.json" $DESTDIR/package.json

install_file "$script_dir/coreclr/coreclr.ad7Engine.json" debugAdapters

for dll in Microsoft.MICore.dll Microsoft.MIDebugEngine.dll
do 
    install_module "$DropDir/$dll" debugAdapters
done

echo ''
if [ ! -z "$InstallError" ]; then
    echo "ERROR: Failed to copy one or more files."
    echo ''
    exit 1
fi

echo "InstallToVSCode.sh succeeded."
echo ""
exit 0
