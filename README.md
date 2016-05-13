## Welcome to the Visual Studio MI Debug Engine ("MIEngine")

||Debug|Release|
|:--:|:--:|:--:|
|**master**|[![Build Status](http://dotnet-ci.cloudapp.net/job/Microsoft_MIEngine/job/master/job/debug/badge/icon)](http://dotnet-ci.cloudapp.net/job/Microsoft_MIEngine/job/master/job/debug/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/Microsoft_MIEngine/job/master/job/release/badge/icon)](http://dotnet-ci.cloudapp.net/job/Microsoft_MIEngine/job/master/job/release/)|

The Visual Studio MI Debug Engine ("MIEngine") provides an open-source Visual Studio extension that enables debugging with debuggers that support the gdb Machine Interface ("MI")
specification such as [GDB](http://www.gnu.org/software/gdb/), [LLDB](http://lldb.llvm.org/), and [CLRDBG](https://github.com/Microsoft/MIEngine/wiki/What-is-CLRDBG).

### What is MIEngine?

MIEngine is a Visual Studio **Debug Engine** that understands **Machine Interface** ("MI"). A Debug Engine is an implementation of the [Core Debug Interfaces](https://msdn.microsoft.com/en-us/library/bb146305.aspx), 
enabling the VS UI to drive debugging. Machine Interface is a text-based protocol developed by GDB that allows a debugger to be used as a separate component of a larger system. 
Additional information:
 - [Visual Studio Debugger Extensibility](https://msdn.microsoft.com/en-us/library/bb161718.aspx)
 - [GDB's Machine Interface](https://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI.html)

### Debug Multiple Platforms

* Support for debugging C/C++ on [Android](http://blogs.msdn.com/b/vcblog/archive/2014/12/12/debug-jni-android-applications-using-visual-c-cross-platform-mobile.aspx) and [iOS](http://blogs.msdn.com/b/vcblog/archive/2015/04/29/debugging-c-code-on-ios-with-visual-studio-2015.aspx).
* Debug on any platform that supports GDB, such as Linux and even [Raspberry Pi](http://blogs.msdn.com/b/vcblog/archive/2015/04/29/debug-c-code-on-linux-from-visual-studio.aspx).

### Prerequisites
MIEngine requires [Visual Studio 2015](https://www.visualstudio.com/downloads/download-visual-studio-vs) with the following features installed:
* Programming Languages -> Visual C++ -> Common Tools for Visual C++
* Cross Platform Mobile Development -> Visual C++ Mobile Development
* Cross Platform Mobile Development -> Microsoft Visual Studio Emulator for Android
* Common Tools -> Visual Studio Extensibility Tools

### Get Started
* Clone the sources: `git clone https://github.com/Microsoft/MIEngine.git`
* Open [src/MIDebugEngine.sln](https://github.com/Microsoft/MIEngine/blob/master/src/MIDebugEngine.sln) in Visual Studio.
* Debug -> Start Debugging (or F5) to to build, deploy, and start debugging the [Experimental Instance of Visual Studio](https://msdn.microsoft.com/en-us/library/bb166560.aspx).
* See the [wiki](https://github.com/Microsoft/MIEngine/wiki) for more info.


### Contribute!
Before you contribute, please read through the contributing and developer guides to get an idea of requirements for pull requests. 

* [Contributing Guide](https://github.com/Microsoft/MIEngine/wiki/Contributing-Code)
* [Developer Guide](https://github.com/Microsoft/MIEngine/wiki/Building-Testing-and-Debugging)

Want to get more familiar with what's going on in the code?
* [Pull requests](https://github.com/Microsoft/MIEngine/pulls): [Open](https://github.com/Microsoft/MIEngine/pulls?q=is%3Aopen+is%3Apr)/[Closed](https://github.com/Microsoft/MIEngine/pulls?q=is%3Apr+is%3Aclosed)
* [Issues](https://github.com/Microsoft/MIEngine/issues)

You are also encouraged to start a discussion by filing an issue or creating a gist. 

### License
MIEngine is licensed under the [MIT License](https://github.com/Microsoft/MIEngine/blob/master/License.txt).
