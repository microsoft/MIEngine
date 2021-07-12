#!/bin/sh

# Copyright (c) Microsoft. All rights reserved.

# Working dirctory to return to
__InitialCWD=$(pwd)

# Location of the script
__ScriptDirectory=

# RuntimeID to publish
__RuntimeID=

# Configuration of build
__Configuration=Debug

# OutputFolder
__OutputFolder=

log_and_exec_cmd() {
    echo "##[command] $1"
    $1
}

print_help()
{
    echo 'PublishOpenDebugAD7.sh [-h] [-c C] [-r R]'
    echo ''
    echo 'This script publishes OpenDebugAD7'
    echo '-h    Prints usage information.'
    echo '-c C  The configuration to publish OpenDebugAD7. Defaults to Debug.'
    echo '-r R  The RuntimeID to publish OpenDebugAD7. Defaults to machine arch.'
    echo '-o O  Folder to output the publish. Defaults to the `dotnet publish` folder.'
}

# Gets the script directory
get_script_directory()
{
    scriptDirectory=$(dirname "$0")
    cd "$scriptDirectory" || fail "Command failed: 'cd \"$scriptDirectory\"'"
    __ScriptDirectory=$(pwd)
    cd "$__InitialCWD" || fail "Command failed: 'cd \"$__InitialCWD\"'"
}

# Parses and populates the arguments
parse_and_get_arguments()
{
    while getopts "hc:r:o:" opt; do
        case $opt in
            c)
                __Configuration=$OPTARG
                ;;
            r)
                __RuntimeID=$OPTARG
                ;;
            o)
                __OutputFolder=$OPTARG
                ;;
            h)
                print_help
                exit 1
                ;;
            :)
                echo "ERROR: Option expected for -$OPTARG"
                print_help
                exit 1
                ;;
        esac
    done
}

get_dotnet_runtime_id()
{
    if [ "$(uname)" = "Darwin" ]; then
        if [ "$(uname -m)" = "arm64" ]; then
            __RuntimeID=osx-arm64
        else
            __RuntimeID=osx-x64
        fi
    elif [ "$(uname -m)" = "x86_64" ]; then
        __RuntimeID=linux-x64
        if [ -e /etc/os-release ]; then
            # '.' is the same as 'source' but is POSIX compliant
            . /etc/os-release
            if [ "$ID" = "alpine" ]; then
                __RuntimeID=linux-musl-x64
            fi
        fi
    elif [ "$(uname -m)" = "armv7l" ]; then
        __RuntimeID=linux-arm
    elif [ "$(uname -m)" = "aarch64" ]; then
         __RuntimeID=linux-arm64
         if [ -e /etc/os-release ]; then
            # '.' is the same as 'source' but is POSIX compliant
            . /etc/os-release
            if [ "$ID" = "alpine" ]; then
                __RuntimeID=linux-musl-arm64
            fi
        fi
    fi
}

get_script_directory

if [ -z "$1" ]; then
    echo "ERROR: Missing arguments for PublishOpenDebugAD7.sh"
    print_help
    exit 1
else
    parse_and_get_arguments "$@"
fi

if [ -z "$__RuntimeID" ]; then
    get_dotnet_runtime_id
fi

echo "Info: Using Configuration '$__Configuration'"
echo "Info: Using Runtime ID '$__RuntimeID'"

__DotnetPublishArgs="-c ${__Configuration} -r ${__RuntimeID} --self-contained"

if [ -n "${__OutputFolder}" ]; then
    __DotnetPublishArgs="${__DotnetPublishArgs} -o ${__OutputFolder}"
else
    __OutputFolder=${__ScriptDirectory}/bin/${__Configuration}/vscode/${__RuntimeID}/publish
fi

log_and_exec_cmd "dotnet build ${__ScriptDirectory}/src/MIDebugEngine-Unix.sln -c ${__Configuration}"
log_and_exec_cmd "dotnet publish ${__ScriptDirectory}/src/OpenDebugAD7/OpenDebugAD7.csproj ${__DotnetPublishArgs}"
log_and_exec_cmd "cp ${__ScriptDirectory}/bin/${__Configuration}/Microsoft.MIDebugEngine.dll ${__OutputFolder}/."
log_and_exec_cmd "cp ${__ScriptDirectory}/bin/${__Configuration}/Microsoft.MICore.dll ${__OutputFolder}/."