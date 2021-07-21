#! /bin/bash
lldbMIDownloadLink="https://download.visualstudio.microsoft.com/download/pr/173e6ced-0717-401d-87fc-169ca3424c72/f1228fd847c140b7f9839612f497bb7a/lldb-mi-10.0.0.zip"
toolsDirectory=$(dirname "$0")

# Go to root project folder
pushd "$toolsDirectory/.." > /dev/null 2>&1 || exit 1

if [ -z "$1" ] || [ "$1" == "-h" ]; then
    echo "Please pass in output folder of publish"
    echo "Example: $0 $(pwd)/bin/DebugAdapterProtocolTests/Debug/extension/debugAdapters"
    exit 1
fi

if [ ! -f "$1/OpenDebugAD7" ]; then
    echo "Please build MIDebugEngine-Unix.sln and run PublishOpenDebugAD7.sh before running DownloadLldbMI.sh"
    popd || exit
    exit 1
fi

# Download the latest version of lldb-ui that vscode-cpptools uses.
if ! `curl $lldbMIDownloadLink --output ./lldb-mi-10.0.0.zip > /dev/null 2>&1` ; then
  echo "Failed to download lldb-mi."
  exit 1
fi

unzip -o ./lldb-mi-10.0.0.zip > /dev/null 2>&1

if [ ! -f ./debugAdapters/lldb-mi/bin/lldb-mi ]; then
  echo "Failed to unzip."
fi

# Ensure we can run it or we will get permission denied.
if ! `sudo chmod 755 ./debugAdapters/lldb-mi/bin/lldb-mi` ; then
  echo "Failed to change permissions for lldb-mi."
  exit 1
fi

# place lldb-mi folder in output's debugAdapters folder
mv ./debugAdapters/lldb-mi $1/../.

# Clean up unused zip
rm ./lldb-mi-10.0.0.zip

popd > /dev/null 2>&1 || exit