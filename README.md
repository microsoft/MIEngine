## Welcome to the Visual Studio MI Debug Engine ("MIEngine")

The Visual Studio MI Debug Engine ("MIEngine") provides an open-source Visual Studio extension that enables debugging with MI enabled debuggers such as gdb, lldb, and clrdbg.

### Debug Multiple Platforms

* First class support for debugging C/C++ on [Android](http://blogs.msdn.com/b/vcblog/archive/2014/12/12/debug-jni-android-applications-using-visual-c-cross-platform-mobile.aspx) and [iOS](http://blogs.msdn.com/b/vcblog/archive/2015/04/29/debugging-c-code-on-ios-with-visual-studio-2015.aspx).
* Debug on any platform that supports gdb, such as linux and even [Raspberry Pi](http://blogs.msdn.com/b/vcblog/archive/2015/04/29/debug-c-code-on-linux-from-visual-studio.aspx).

### Prerequisites
* [Visual Studio 2015 RC](https://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs.aspx)
* [Visual Studio 2015 RC SDK](http://go.microsoft.com/?linkid=9877247)

### Get Started
* Clone the sources: `git clone https://github.com/Microsoft/MIEngine.git`
* Open [MIDebugEngine.sln](https://github.com/Microsoft/MIEngine/blob/master/MIDebugEngine.sln) in Visual Studio.
* Debug -> Start Debugging (or F5) to to build, deploy, and start debugging the Experimental Instance of Visaul Studio.
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