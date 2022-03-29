#! /bin/bash
lldbMIx8664DownloadLink="https://download.visualstudio.microsoft.com/download/pr/6d406764-8d41-49f3-a0e7-81ff0612be30/445b0b4489b5fd0eefae30f644dbc68a/lldb-mi_13.x_x86_64.zip"
lldbMIARM64DownloadLink="https://download.visualstudio.microsoft.com/download/pr/6d406764-8d41-49f3-a0e7-81ff0612be30/123a33b78e99601a187c44fe57db03c0/lldb-mi_13.x_arm64.zip"
toolsDirectory=$(dirname "$0")

arch=$(uname -m)
echo $arch

if [ $arch = "arm64" ]; then
    lldbMIDownloadLink=$lldbMIARM64DownloadLink
else
    lldbMIDownloadLink=$lldbMIx8664DownloadLink
fi

# Go to root project folder
pushd "$toolsDirectory/.." > /dev/null 2>&1 || exit 1

if [ -z "$1" ] || [ "$1" == "-h" ]; then
    echo "Please pass in output folder of publish"
    echo "Example: $0 $(pwd)/bin/DebugAdapterProtocolTests/Debug/extension/debugAdapters"
    exit 1
fi

echo $lldbMIDownloadLink

# Download the latest version of lldb-ui that vscode-cpptools uses.
if ! `curl $lldbMIDownloadLink --output ./lldb-mi_13.x_${arch}.zip > /dev/null 2>&1` ; then
  echo "Failed to download lldb-mi."
  exit 1
fi

unzip -o ./lldb-mi_13.x_${arch}.zip > /dev/null 2>&1

if [ ! -f ./debugAdapters/lldb-mi_$arch/bin/lldb-mi ]; then
  echo "Failed to unzip."
  exit 1
fi

# Ensure we can run it or we will get permission denied.
if ! `sudo chmod 755 ./debugAdapters/lldb-mi_$arch/bin/lldb-mi` ; then
  echo "Failed to change permissions for lldb-mi."
  exit 1
fi

# place lldb-mi folder in output's debugAdapters folder
mv ./debugAdapters/lldb-mi_$arch $1/../lldb-mi

# Clean up unused zip
rm ./lldb-mi_13.x_$arch.zip

popd > /dev/null 2>&1 || exit

exit 0