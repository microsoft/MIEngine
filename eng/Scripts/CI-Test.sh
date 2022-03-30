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

# Adapted from https://www.cazzulino.com/dotnet-test-retry.html
exitcode=0
dotnet test "$RootDir"/bin/DebugAdapterProtocolTests/Debug/CppTests/CppTests.dll --logger "trx;LogFileName=$RootDir/bin/DebugAdapterProtocolTests/Debug/CppTests/results.trx" | tee ./output.log
# run test and forward output also to a file in addition to stdout (tee command)
# capture dotnet test exit status, different from tee
exitcode=${PIPESTATUS[0]}
if [ "$exitcode" == 0 ]
then
    exit 0
fi
# Get failed test names, join as DisplayName=TEST with |, remove trailing |.
filter=$(grep -o -P '(?<=\sFailed\s)\w*' < ./output.log | awk 'BEGIN { ORS="|" } { print("DisplayName=" $0) }' | grep -o -P '.*(?=\|$)')
if [ -z "$filter" ] && [ "$(uname)" = "Darwin" ]; then
    echo -e "Failed to set a filter. Make sure you are using GNU grep from homebrew."
    exit 1
fi

# Make sure the next 3 runs are clean for the failing tests.
counter=0
while [ $counter -lt 3 ]
do
    echo -e "Retry $counter for $filter"
    if [ ! "$(dotnet test "$RootDir"/bin/DebugAdapterProtocolTests/Debug/CppTests/CppTests.dll --logger "trx;LogFileName=$RootDir/bin/DebugAdapterProtocolTests/Debug/CppTests/results-$counter.trx" --filter "$filter" > /dev/null 2>&1)" ]
    then
        echo "Tests failed on rerun #$counter".
        exit $exitcode
    fi
    ((counter++))
done
exit 0
