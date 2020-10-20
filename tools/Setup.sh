#!/bin/bash

if ! hash dotnet 2>/dev/null; then
    echo "ERROR: The dotnet is not installed. see: https://dotnet.microsoft.com/download/dotnet-core"
    exit 1
fi

if ! dotnet script -v &> /dev/null; then
    echo "dotnet script needs to be installed. Run 'dotnet tool install -g dotnet-script'".
    echo "More Information: https://github.com/filipw/dotnet-script#net-core-global-tool"
    exit 1
fi

ScriptDir=$(dirname "$0")

"$ScriptDir/Setup.csx" "${@:1}"

exit 0