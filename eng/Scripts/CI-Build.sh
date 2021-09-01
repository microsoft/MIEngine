#!/bin/bash

ScriptDir=`dirname $0`
RootDir=$ScriptDir/../..

if [ "$(which dotnet)" == "" ]; then
    echo "ERROR: Missing .NET SDK. Please install the SDK at https://dotnet.microsoft.com/download"
fi

dotnet build $RootDir/src/MIDebugEngine-Unix.sln
$RootDir/PublishOpenDebugAD7.sh -c Debug -o $RootDir/bin/DebugAdapterProtocolTests/Debug/extension/debugAdapters