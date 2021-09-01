#!/bin/bash

ScriptDir=$(dirname "$0")
RootDir=$ScriptDir/../..

if [ "$(which dotnet)" == "" ]; then
    echo "ERROR: Missing .NET SDK. Please install the SDK at https://dotnet.microsoft.com/download"
    exit 1
fi

dotnet build "$RootDir"/src/MIDebugEngine-Unix.sln
if [ $? -ne 0 ]; then
    echo "ERROR: Failed to build MIDebugEngine-Unix.sln"
    exit 1
fi 

"$RootDir"/PublishOpenDebugAD7.sh -c Debug -o "$RootDir"/bin/DebugAdapterProtocolTests/Debug/extension/debugAdapters
if [ $? -ne 0 ]; then
    echo "ERROR: Failed to build MIDebugEngine-Unix.sln"
    exit 1
fi 

exit 0