#!/bin/bash

ScriptDir=$(dirname "$0")
RootDir=$ScriptDir/../..

if [ "$(which dotnet)" == "" ]; then
    echo "ERROR: Missing .NET SDK. Please install the SDK at https://dotnet.microsoft.com/download"
    exit 1
fi

if ! dotnet build "$RootDir"/src/MIDebugEngine-Unix.sln; then
    echo "ERROR: Failed to build MIDebugEngine-Unix.sln"
    exit 1
fi 

if ! "$RootDir"/PublishOpenDebugAD7.sh -c Debug -o "$RootDir"/bin/DebugAdapterProtocolTests/Debug/extension/debugAdapters; then
    echo "ERROR: Failed to build MIDebugEngine-Unix.sln"
    exit 1
fi 

exit 0