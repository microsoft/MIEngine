#!/bin/bash

DefaultDestDir=$HOME/.vscode/extensions/coreclr-debug
script_dir=`dirname $0`

print_help()
{
    echo 'InstallToVSCode.sh <-c|-l> [--pdbs] <OpenDebugAD7-dir> [-d <clrdbg-binaries-dir>] [destination-dir]'
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
    echo "$script_dir/InstallToVSCode.sh -l --pdbs /Volumes/OpenDebugAD7/bin/Debug-PortablePDB $HOME/OpenDebugAD7-debug"
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
[ ! -f "$OpenDebugAD7Dir/OpenDebugAD7.exe" ] && echo "ERROR: $OpenDebugAD7Dir/OpenDebugAD7.exe does not exist." && exit 1

DropDir=$script_dir/../../bin/Debug-PortablePDB
if [ ! -f "$DropDir/Microsoft.MIDebugEngine.dll" ]
then
    echo "ERROR: '$DropDir/Microsoft.MIDebugEngine.dll' has not been built."
    exit 1
fi

# Remove the relative path from DropDir
pushd $DropDir
DropDir=$(pwd)
popd

if [ "$3" == "-d" ]; then
    CLRDBGBITSDIR=${4:?"ERROR: Clrdbg binaries directory must be specified with -d option. See -h for usage."}
    [ ! -f "$CLRDBGBITSDIR/clrdbg" ] && echo "ERROR: $CLRDBGBITSDIR/clrdbg does not exist." && exit 1
    DESTDIR=$5
else
    DESTDIR=$3
fi

if [ -z "$DESTDIR" ]
then
    DESTDIR=$DefaultDestDir
fi

if [ ! -d "$DESTDIR" ]
then
    mkdir "$DESTDIR"
    if [ $? -ne 0 ]
    then
        echo "ERROR: unable to create destination directory '$DESTDIR'."
        exit 1
    fi
fi

if [ ! -d "$DESTDIR/debugAdapters" ]
then
    mkdir "$DESTDIR/debugAdapters"
    if [ $? -ne 0 ]
    then
        echo "ERROR: unable to create destination directory '$DESTDIR/debugAdapters'."
        exit 1
    fi
fi

set InstallError=
install_module "$OpenDebugAD7Dir/OpenDebugAD7.exe" debugAdapters
install_module "$OpenDebugAD7Dir/dar.exe" debugAdapters
install_module "$OpenDebugAD7Dir/xunit.console.netcore.exe" debugAdapters
for dll in $(ls $OpenDebugAD7Dir/*.dll); do
    install_module "$dll" debugAdapters ignoreMissingPdbs
done

if [ ! -z "$CLRDBGBITSDIR" ]; then
    echo ''
    echo "Installing clrdbg bits from $CLRDBGBITSDIR"

    for dll in $(ls $CLRDBGBITSDIR/*.dll); do
        install_module "$dll" debugAdapters ignoreMissingPdbs
    done

    OSName=$(uname -s)
    if [ $OSName == "Linux" ]; then
        library_ext=so
    else
        library_ext=dylib
    fi

    for shared_library in $(ls $CLRDBGBITSDIR/*.$library_ext); do
        install_file "$shared_library" debugAdapters
    done

    for vsdconfig in $(ls $CLRDBGBITSDIR/*.vsdconfig); do
        install_file "$vsdconfig" debugAdapters
    done

    install_file "$CLRDBGBITSDIR/clrdbg" debugAdapters
    install_file "$CLRDBGBITSDIR/version.txt" debugAdapters
    install_file "$CLRDBGBITSDIR/mscorlib.resources" debugAdapters
    
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

fi

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

echo "InstallToVSCode.sh succeeded. To complete setup:"
echo "Create a link or copy the corerun runtime next to the debug adapter. Ex:"
echo ""
echo "   cp -r /Volumes/runtime-osx/x64 $DESTDIR/runtime"
echo ""
exit 0
