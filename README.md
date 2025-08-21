## Welcome to the Visual Studio MI Debug Engine ("MIEngine")

[![Build Status](https://dev.azure.com/ms/MIEngine/_apis/build/status/Microsoft.MIEngine?branchName=main)](https://dev.azure.com/ms/MIEngine/_build/latest?definitionId=98&branchName=main)

The Visual Studio MI Debug Engine ("MIEngine") provides an open-source Visual Studio extension that enables debugging with debuggers that support the gdb Machine Interface ("MI")
specification such as [GDB](http://www.gnu.org/software/gdb/) and [LLDB](http://lldb.llvm.org/).

In Visual Studio Code, MIEngine also powers the 'cppdbg' debug adapter which is part of the [C/C++ Extension](https://github.com/microsoft/vscode-cpptools).

### What is MIEngine?

MIEngine is a Visual Studio **Debug Engine** that understands **Machine Interface** ("MI"). A Debug Engine is an implementation of the [Visual Studio Core Debug Interfaces (IDebug* interfaces)](https://msdn.microsoft.com/en-us/library/bb146305.aspx), 
enabling the VS UI to drive debugging. Machine Interface is a text-based protocol developed by GDB that allows a debugger to be used as a separate component of a larger system. 
Additional information:
 - [Visual Studio Debugger Extensibility](https://msdn.microsoft.com/en-us/library/bb161718.aspx)
 - [GDB's Machine Interface](https://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI.html)

This repo also includes:
* [OpenDebugAD7](https://github.com/microsoft/MIEngine/tree/main/src/OpenDebugAD7): An adaptation layer between the [Debug Adapter Protocol](https://microsoft.github.io/debug-adapter-protocol/) and debug engines. This is what allows MIEngine to be used with Visual Studio Code.
* [SSHDebugPS](https://github.com/microsoft/MIEngine/tree/main/src/SSHDebugPS): A Visual Studio 'Port Supplier' which enables Visual Studio to attach to processes over SSH or Linux Docker and could be easily extended to support any other exe that provides a shell into a Linux container/machine.

### Debug Multiple Platforms

* Support for debugging C/C++ on [Android](http://blogs.msdn.com/b/vcblog/archive/2014/12/12/debug-jni-android-applications-using-visual-c-cross-platform-mobile.aspx) ~~and [iOS](http://blogs.msdn.com/b/vcblog/archive/2015/04/29/debugging-c-code-on-ios-with-visual-studio-2015.aspx).~~
    * Note: iOS support is not available after Visual Studio 2022.
* * Debug on any platform that supports GDB, such as Linux and even [Raspberry Pi](http://blogs.msdn.com/b/vcblog/archive/2015/04/29/debug-c-code-on-linux-from-visual-studio.aspx).

### Prerequisites
MIEngine can be built with either [Visual Studio](https://visualstudio.microsoft.com/downloads/) or with the [.NET CLI](https://dotnet.microsoft.com/download/dotnet).

* See the [wiki](https://github.com/Microsoft/MIEngine/wiki) for more info.

### Contribute!
Before you contribute, please read through the [Contributing Guide](https://github.com/Microsoft/MIEngine/wiki/Contributing-Code) to get an idea of requirements for pull requests. 

Want to get more familiar with what's going on in the code?
* [Pull requests](https://github.com/Microsoft/MIEngine/pulls): [Open](https://github.com/Microsoft/MIEngine/pulls?q=is%3Aopen+is%3Apr)/[Closed](https://github.com/Microsoft/MIEngine/pulls?q=is%3Apr+is%3Aclosed)
* [Issues](https://github.com/Microsoft/MIEngine/issues)

You are also encouraged to start a discussion by filing an issue or creating a gist. 

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

### License
MIEngine is licensed under the [MIT License](https://github.com/Microsoft/MIEngine/blob/main/License.txt).
