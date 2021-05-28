// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace DebuggerTesting.Tests
{
    public sealed class AttributionTests :
        ILoggingComponent
    {
        #region Constructor

        public AttributionTests(ITestOutputHelper outputHelper)
        {
            this.OutputHelper = outputHelper;
        }

        #endregion

        #region ILoggingComponent Members

        public ITestOutputHelper OutputHelper { get; private set; }

        #endregion

        #region Methods

        #region Test Methods

        [Fact]
        public void TestPlatformWithoutAttribute()
        {
            this.TestPurpose("Check that tests without SupportedPlatformAttribute will run with all test settings.");
            IEnumerable<ITestSettings> actual = AttributionTests.FilterSettings(
                nameof(AttributionTests.MockTest_PlatformNeutral),
                SupportedPlatform.Windows,
                SupportedArchitecture.x64);
            IEnumerable<ITestSettings> expected = AttributionTests.ChangeName(
                AttributionTests.AllSettings,
                nameof(AttributionTests.MockTest_PlatformNeutral));
            Assert.Equal(expected, actual);

            actual = AttributionTests.FilterSettings(
                nameof(AttributionTests.MockTest_PlatformNeutral),
                SupportedPlatform.Windows,
                SupportedArchitecture.x86);
            Assert.Equal(expected, actual);

            actual = AttributionTests.FilterSettings(
                nameof(AttributionTests.MockTest_PlatformNeutral),
                SupportedPlatform.Linux,
                SupportedArchitecture.x64);
            Assert.Equal(expected, actual);

            actual = AttributionTests.FilterSettings(
                nameof(AttributionTests.MockTest_PlatformNeutral),
                SupportedPlatform.Linux,
                SupportedArchitecture.x86);
            Assert.Equal(expected, actual);

            actual = AttributionTests.FilterSettings(
                nameof(AttributionTests.MockTest_PlatformNeutral),
                SupportedPlatform.MacOS,
                SupportedArchitecture.x64);
            Assert.Equal(expected, actual);

            actual = AttributionTests.FilterSettings(
                nameof(AttributionTests.MockTest_PlatformNeutral),
                SupportedPlatform.MacOS,
                SupportedArchitecture.x86);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestPlatformIsSame()
        {
            this.TestPurpose("Check that tests with SupportedPlatformAttribute will run on the specified platform.");
            IEnumerable<ITestSettings> actual = AttributionTests.FilterSettings(
                nameof(AttributionTests.MockTest_PlatformWindows),
                SupportedPlatform.Windows,
                SupportedArchitecture.x64);
            IEnumerable<ITestSettings> expected = AttributionTests.ChangeName(
                AttributionTests.AllSettings,
                nameof(AttributionTests.MockTest_PlatformWindows));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestPlatformIsSameArchitectureIsSame()
        {
            this.TestPurpose("Check that tests with SupportedPlatformAttribute will run on the specified platform and architecture.");
            IEnumerable<ITestSettings> actual = AttributionTests.FilterSettings(
                nameof(AttributionTests.MockTest_PlatformMac64),
                SupportedPlatform.MacOS,
                SupportedArchitecture.x64);
            IEnumerable<ITestSettings> expected = AttributionTests.ChangeName(
                AttributionTests.AllSettings,
                nameof(AttributionTests.MockTest_PlatformMac64));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestPlatformIsSameArchitectureIsDifferent()
        {
            this.TestPurpose("Check that tests with SupportedPlatformAttribute will not run on the specified platform and different architecture.");
            IEnumerable<ITestSettings> actual = AttributionTests.FilterSettings(
                nameof(AttributionTests.MockTest_PlatformLinux86),
                SupportedPlatform.Linux,
                SupportedArchitecture.x64);
            IEnumerable<ITestSettings> expected = Enumerable.Empty<ITestSettings>();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestPlatformIsDifferent()
        {
            this.TestPurpose("Check that tests with SupportedPlatformAttribute will not run on a different platform.");
            IEnumerable<ITestSettings> actual = AttributionTests.FilterSettings(
                nameof(AttributionTests.MockTest_PlatformWindows),
                SupportedPlatform.Linux,
                SupportedArchitecture.x86);
            IEnumerable<ITestSettings> expected = Enumerable.Empty<ITestSettings>();
            Assert.Equal(expected, actual);

            actual = AttributionTests.FilterSettings(
                nameof(AttributionTests.MockTest_PlatformWindows),
                SupportedPlatform.MacOS,
                SupportedArchitecture.x86);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestDebuggerSupported()
        {
            this.TestPurpose("Check that tests with a single SupportedDebuggerAttribute will only run with the specified debugger.");

            IEnumerable<ITestSettings> actual = AttributionTests.FilterSettings(
                nameof(AttributionTests.MockTest_DebuggerGdbSupported),
                SupportedPlatform.Windows,
                SupportedArchitecture.x64);
            IEnumerable<ITestSettings> expected = AttributionTests.ChangeName(
                new ITestSettings[]
                {
                    AttributionTests.Settings_GppGdbX64,
                    AttributionTests.Settings_GppGdbX86
                },
                nameof(AttributionTests.MockTest_DebuggerGdbSupported));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestDebuggerMultipleSupported()
        {
            this.TestPurpose("Check that tests with several SupportedDebuggerAttribute will only run with the specified debuggers.");

            IEnumerable<ITestSettings> actual = AttributionTests.FilterSettings(
                nameof(AttributionTests.MockTest_DebuggerGdbLldbSupported),
                SupportedPlatform.Windows,
                SupportedArchitecture.x64);
            IEnumerable<ITestSettings> expected = AttributionTests.ChangeName(
                new ITestSettings[]
                {
                    AttributionTests.Settings_GppGdbX64,
                    AttributionTests.Settings_GppGdbX86,
                    AttributionTests.Settings_ClangLldbX64
                },
                nameof(AttributionTests.MockTest_DebuggerGdbLldbSupported));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestDebuggerUnmatched()
        {
            this.TestPurpose("Check that tests that do not match the settings will not run.");

            IEnumerable<ITestSettings> settings = new ITestSettings[]
            {
                AttributionTests.Settings_ClangLldbX64,
                AttributionTests.Settings_ClangLldbX86
            };
            IEnumerable<ITestSettings> actual = settings.FilterSettings(
                AttributionTests.GetTestMethodInfo(nameof(AttributionTests.MockTest_DebuggerGdbSupported)),
                SupportedPlatform.Windows,
                SupportedArchitecture.x64);
            IEnumerable<ITestSettings> expected = Enumerable.Empty<ITestSettings>();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestDebuggerUnsupported()
        {
            this.TestPurpose("Check that tests with a single UnsupportedDebuggerAttribute will not run for the specified debugger.");

            IEnumerable<ITestSettings> actual = AttributionTests.FilterSettings(
                nameof(AttributionTests.MockTest_DebuggerLldbUnsupported),
                SupportedPlatform.Windows,
                SupportedArchitecture.x64);
            IEnumerable<ITestSettings> expected = AttributionTests.ChangeName(
                new ITestSettings[]
                {
                    AttributionTests.Settings_GppGdbX64,
                    AttributionTests.Settings_GppGdbX86,
                    AttributionTests.Settings_CygwinGppGdbX64,
                    AttributionTests.Settings_CygwinGppGdbX86,
                    AttributionTests.Settings_MingwGppGdbX64,
                    AttributionTests.Settings_MingwGppGdbX86
                },
                nameof(AttributionTests.MockTest_DebuggerLldbUnsupported));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestDebuggerMultipleUnsupported()
        {
            this.TestPurpose("Check that tests with multiple UnsupportedDebuggerAttribute will not run for the specified debuggers.");

            IEnumerable<ITestSettings> actual = AttributionTests.FilterSettings(
                nameof(AttributionTests.MockTest_DebuggerGdbLldbUnsupported),
                SupportedPlatform.Windows,
                SupportedArchitecture.x64);
            IEnumerable<ITestSettings> expected = AttributionTests.ChangeName(
                new ITestSettings[]
                {
                    AttributionTests.Settings_GppGdbX64,
                    AttributionTests.Settings_CygwinGppGdbX64,
                    AttributionTests.Settings_CygwinGppGdbX86,
                    AttributionTests.Settings_MingwGppGdbX86,
                },
                nameof(AttributionTests.MockTest_DebuggerGdbLldbUnsupported));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestDebuggerSupportedUnsupportedIsSame()
        {
            this.TestPurpose("Check that tests that specify the same debugger as supported and unsupported will not run with that debugger.");

            IEnumerable<ITestSettings> actual = AttributionTests.FilterSettings(
                nameof(AttributionTests.MockTest_DebuggerGdbSupportedUnsupported),
                SupportedPlatform.Windows,
                SupportedArchitecture.x64);
            IEnumerable<ITestSettings> expected = Enumerable.Empty<ITestSettings>();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestCompilerSupported()
        {
            this.TestPurpose("Check that tests with a single SupportedCompilerAttribute will only run with the specified compiler.");

            IEnumerable<ITestSettings> actual = AttributionTests.FilterSettings(
                nameof(AttributionTests.MockTest_CompilerGPlusPlus86Supported),
                SupportedPlatform.Windows,
                SupportedArchitecture.x64);
            IEnumerable<ITestSettings> expected = AttributionTests.ChangeName(
                new ITestSettings[]
                {
                    AttributionTests.Settings_GppGdbX86,
                    AttributionTests.Settings_CygwinGppGdbX86,
                    AttributionTests.Settings_MingwGppGdbX86
                },
                nameof(AttributionTests.MockTest_CompilerGPlusPlus86Supported));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestCompilerMultipleSupported()
        {
            this.TestPurpose("Check that tests with several SupportedCompilerAttribute will only run with the specified compilers.");

            IEnumerable<ITestSettings> actual = AttributionTests.FilterSettings(
                nameof(AttributionTests.MockTest_CompilerGPlusPlus86ClangPlusPlus64Supported),
                SupportedPlatform.Windows,
                SupportedArchitecture.x64);
            IEnumerable<ITestSettings> expected = AttributionTests.ChangeName(
                new ITestSettings[]
                {
                    AttributionTests.Settings_GppGdbX64,
                    AttributionTests.Settings_CygwinGppGdbX64,
                    AttributionTests.Settings_MingwGppGdbX64,
                    AttributionTests.Settings_ClangLldbX86
                },
                nameof(AttributionTests.MockTest_CompilerGPlusPlus86ClangPlusPlus64Supported));
            Assert.Equal(expected, actual);
        }

        #endregion

        #region Mock Tests

#pragma warning disable xUnit1013

        public void MockTest_PlatformNeutral(ITestSettings settings)
        {
        }

        [SupportedPlatform(SupportedPlatform.Windows, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void MockTest_PlatformWindows(ITestSettings settings)
        {
        }

        [SupportedPlatform(SupportedPlatform.MacOS, SupportedArchitecture.x64)]
        public void MockTest_PlatformMac64(ITestSettings settings)
        {
        }

        [SupportedPlatform(SupportedPlatform.Linux, SupportedArchitecture.x86)]
        public void MockTest_PlatformLinux86(ITestSettings settings)
        {
        }

        [SupportedDebugger(SupportedDebugger.Gdb_Gnu, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void MockTest_DebuggerGdbSupported(ITestSettings settings)
        {
        }

        [SupportedDebugger(SupportedDebugger.Gdb_Gnu, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        [SupportedDebugger(SupportedDebugger.Lldb, SupportedArchitecture.x64)]
        public void MockTest_DebuggerGdbLldbSupported(ITestSettings settings)
        {
        }

        [UnsupportedDebugger(SupportedDebugger.Lldb, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void MockTest_DebuggerLldbUnsupported(ITestSettings settings)
        {
        }

        [UnsupportedDebugger(SupportedDebugger.Gdb_Gnu, SupportedArchitecture.x86)]
        [UnsupportedDebugger(SupportedDebugger.Lldb, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        [UnsupportedDebugger(SupportedDebugger.Gdb_MinGW, SupportedArchitecture.x64)]
        public void MockTest_DebuggerGdbLldbUnsupported(ITestSettings settings)
        {
        }

        [SupportedDebugger(SupportedDebugger.Gdb_Gnu, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        [UnsupportedDebugger(SupportedDebugger.Gdb_Gnu, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void MockTest_DebuggerGdbSupportedUnsupported(ITestSettings settings)
        {
        }

        [SupportedCompiler(SupportedCompiler.GPlusPlus, SupportedArchitecture.x86)]
        public void MockTest_CompilerGPlusPlus86Supported(ITestSettings settings)
        {
        }

        [SupportedCompiler(SupportedCompiler.GPlusPlus, SupportedArchitecture.x64)]
        [SupportedCompiler(SupportedCompiler.ClangPlusPlus, SupportedArchitecture.x86)]
        public void MockTest_CompilerGPlusPlus86ClangPlusPlus64Supported(ITestSettings settings)
        {
        }

#pragma warning restore xUnit1013

        #endregion

        #region Helpers

        private static IEnumerable<ITestSettings> ChangeName(IEnumerable<ITestSettings> settings, string name)
        {
            return settings.Select(s => TestSettings.CloneWithName(s, name)).ToArray();
        }

        private static TestSettings CreateClangLldbSettings(SupportedArchitecture debuggeeArchitecture, string compilerName, string debuggerName)
        {
            return new TestSettings(debuggeeArchitecture, compilerName, SupportedCompiler.ClangPlusPlus, "clang++", null, debuggerName, SupportedDebugger.Lldb, "lldb-mi", null, "lldb", null);
        }

        private static TestSettings CreateGppGdbSettings(SupportedArchitecture debuggeeArchitecture, string compilerName, string debuggerName, SupportedDebugger debuggerType)
        {
            return new TestSettings(debuggeeArchitecture, compilerName, SupportedCompiler.GPlusPlus, "g++", null, debuggerName, debuggerType, "gdb", null, null, null);
        }

        private static IEnumerable<ITestSettings> FilterSettings(string methodName, SupportedPlatform platform, SupportedArchitecture platformArchitecture)
        {
            MethodInfo methodInfo = AttributionTests.GetTestMethodInfo(methodName);
            return AttributionTests.AllSettings.FilterSettings(methodInfo, platform, platformArchitecture);
        }

        private static MethodInfo GetTestMethodInfo(string methodName)
        {
            MethodInfo testMethod = typeof(AttributionTests).GetTypeInfo().GetDeclaredMethod(methodName);
            Assert.NotNull(testMethod);
            return testMethod;
        }

        #endregion

        #endregion

        #region Properties

        private static IEnumerable<ITestSettings> AllSettings
        {
            get
            {
                if (null == AttributionTests.s_settings)
                {
                    lock (AttributionTests.s_syncObject)
                    {
                        if (null == AttributionTests.s_settings)
                        {
                            IList<ITestSettings> settings = new List<ITestSettings>();

                            settings.Add(AttributionTests.Settings_GppGdbX64);
                            settings.Add(AttributionTests.Settings_GppGdbX86);
                            settings.Add(AttributionTests.Settings_CygwinGppGdbX64);
                            settings.Add(AttributionTests.Settings_CygwinGppGdbX86);
                            settings.Add(AttributionTests.Settings_MingwGppGdbX64);
                            settings.Add(AttributionTests.Settings_MingwGppGdbX86);
                            settings.Add(AttributionTests.Settings_ClangLldbX64);
                            settings.Add(AttributionTests.Settings_ClangLldbX86);

                            AttributionTests.s_settings = settings;
                        }
                    }
                }
                return AttributionTests.s_settings;
            }
        }

        #endregion

        #region Fields

        private static IEnumerable<ITestSettings> s_settings;
        private static object s_syncObject = new object();

        private static readonly ITestSettings Settings_GppGdbX64 =
            AttributionTests.CreateGppGdbSettings(SupportedArchitecture.x64, "Gpp", "Gdb", SupportedDebugger.Gdb_Gnu);
        private static readonly ITestSettings Settings_GppGdbX86 =
            AttributionTests.CreateGppGdbSettings(SupportedArchitecture.x86, "Gpp", "Gdb", SupportedDebugger.Gdb_Gnu);
        private static readonly ITestSettings Settings_CygwinGppGdbX64 =
            AttributionTests.CreateGppGdbSettings(SupportedArchitecture.x64, "GppCygwin64", "GdbCygwin64", SupportedDebugger.Gdb_Cygwin);
        private static readonly ITestSettings Settings_CygwinGppGdbX86 =
            AttributionTests.CreateGppGdbSettings(SupportedArchitecture.x86, "GppCygwin86", "GdbCygwin86", SupportedDebugger.Gdb_Cygwin);
        private static readonly ITestSettings Settings_MingwGppGdbX64 =
            AttributionTests.CreateGppGdbSettings(SupportedArchitecture.x64, "GppMingw64", "GdbMingw64", SupportedDebugger.Gdb_MinGW);
        private static readonly ITestSettings Settings_MingwGppGdbX86 =
            AttributionTests.CreateGppGdbSettings(SupportedArchitecture.x86, "GppMingw86", "GdbMingw86", SupportedDebugger.Gdb_MinGW);
        private static readonly ITestSettings Settings_ClangLldbX64 =
            AttributionTests.CreateClangLldbSettings(SupportedArchitecture.x64, "Clang", "Lldb");
        private static readonly ITestSettings Settings_ClangLldbX86 =
            AttributionTests.CreateClangLldbSettings(SupportedArchitecture.x86, "Clang", "Lldb");

        #endregion
    }
}
