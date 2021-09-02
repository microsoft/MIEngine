#!/bin/bash

ScriptDir=$(dirname "$0")
RootDir=$ScriptDir/../..

if [ "$(which dotnet)" == "" ]; then
    echo "ERROR: Missing .NET SDK. Please install the SDK at https://dotnet.microsoft.com/download"
fi

if [ ! -f "$RootDir/bin/DebugAdapterProtocolTests/Debug/CppTests/config.xml" ]; then
    if [ "$(uname)" = "Darwin" ]; then
        if ! "$RootDir"/tools/DownloadLldbMI.sh "$RootDir"/bin/DebugAdapterProtocolTests/Debug/extension/debugAdapters; then
            echo "ERROR: Failed to download lldb-mi"
            exit 1
        fi 
        cp "$RootDir"/bin/DebugAdapterProtocolTests/Debug/CppTests/TestConfigurations/config_lldb.xml "$RootDir"/bin/DebugAdapterProtocolTests/Debug/CppTests/config.xml
    else
        cp "$RootDir"/bin/DebugAdapterProtocolTests/Debug/CppTests/TestConfigurations/config_gdb.xml "$RootDir"/bin/DebugAdapterProtocolTests/Debug/CppTests/config.xml
    fi
fi

dotnet test "$RootDir"/bin/DebugAdapterProtocolTests/Debug/CppTests/CppTests.dll --logger "trx;LogFileName=$RootDir/bin/DebugAdapterProtocolTests/Debug/CppTests/results.trx"