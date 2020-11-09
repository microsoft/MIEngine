// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MICore;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace MICoreUnitTests
{
    public class BasicLaunchOptionsTests
    {
        [Fact]
        public void TestLaunchOptions_Local1()
        {
            string fakeFilePath = typeof(BasicLaunchOptionsTests).Assembly.Location;
            string content = string.Concat("<LocalLaunchOptions xmlns=\"http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014\"\n",
                "MIDebuggerPath=\"", fakeFilePath, "\"\n",
                "MIDebuggerServerAddress=\"myserverbox:345\"\n",
                "ExePath=\"", fakeFilePath, "\"\n",
                "TargetArchitecture=\"arm\"\n",
                "/>");

            var baseOptions = GetLaunchOptions(content);
            Assert.IsAssignableFrom<LocalLaunchOptions>(baseOptions);
            var options = (LocalLaunchOptions)baseOptions;

            Assert.Equal(options.MIDebuggerPath, fakeFilePath);
            Assert.Equal("myserverbox:345", options.MIDebuggerServerAddress);
            Assert.Equal(options.ExePath, fakeFilePath);
            Assert.Equal(TargetArchitecture.ARM, options.TargetArchitecture);
            Assert.True(string.IsNullOrEmpty(options.AdditionalSOLibSearchPath));
            Assert.True(string.IsNullOrEmpty(options.AbsolutePrefixSOLibSearchPath));
            Assert.Equal(MIMode.Gdb, options.DebuggerMIMode);
            Assert.Equal(LaunchCompleteCommand.ExecRun, options.LaunchCompleteCommand);
            Assert.Null(options.CustomLaunchSetupCommands);
            Assert.True(options.SetupCommands != null && options.SetupCommands.Count == 0);
            Assert.True(String.IsNullOrEmpty(options.CoreDumpPath));
            Assert.False(options.UseExternalConsole);
            Assert.False(options.IsCoreDump);
        }

        [Fact]
        public void TestLaunchOptions_Pipe1()
        {
            string fakeFilePath = typeof(BasicLaunchOptionsTests).Assembly.Location;
            string content = string.Concat("<PipeLaunchOptions xmlns=\"http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014\"\n",
                "PipePath=\"", fakeFilePath, "\"\n",
                "ExePath=\"/home/user/myname/foo\" ExeArguments=\"arg1 arg2\"\n",
                "TargetArchitecture=\"AMD64\"\n",
                "WorkingDirectory=\"/home/user/myname\"\n",
                ">\n",
                "  <SetupCommands>\n",
                "    <Command>-gdb-set my-example-setting on</Command>\n",
                "  </SetupCommands>\n",
                "</PipeLaunchOptions>");

            var baseOptions = GetLaunchOptions(content);
            Assert.IsAssignableFrom<PipeLaunchOptions>(baseOptions);
            var options = (PipeLaunchOptions)baseOptions;

            Assert.Equal(options.PipePath, fakeFilePath);
            Assert.Null(options.PipeCwd);
            Assert.Equal("/home/user/myname/foo", options.ExePath);
            Assert.Equal("arg1 arg2", options.ExeArguments);
            Assert.Equal(TargetArchitecture.X64, options.TargetArchitecture);
            Assert.True(string.IsNullOrEmpty(options.AdditionalSOLibSearchPath));
            Assert.True(string.IsNullOrEmpty(options.AbsolutePrefixSOLibSearchPath));
            Assert.Equal(MIMode.Gdb, options.DebuggerMIMode);
            Assert.Equal(LaunchCompleteCommand.ExecRun, options.LaunchCompleteCommand);
            Assert.True(options.CustomLaunchSetupCommands == null);
            Assert.True(options.SetupCommands != null && options.SetupCommands.Count == 1);
            Assert.True(options.SetupCommands[0].IsMICommand);
            Assert.Equal("-gdb-set my-example-setting on", options.SetupCommands[0].CommandText);
            Assert.Contains("gdb-set", options.SetupCommands[0].Description, StringComparison.Ordinal);
            Assert.False(options.SetupCommands[0].IgnoreFailures);
            Assert.True(options.PipeEnvironment.Count == 0);
        }

        [Fact]
        public void TestLaunchOptions_Pipe2()
        {
            // Test for:
            // MIMode=lldb
            // Having commands in CustomLaunchSetupCommands
            // Specifying the LaunchCompleteCommand
            string fakeFilePath = typeof(BasicLaunchOptionsTests).Assembly.Location;
            string content = string.Concat("<PipeLaunchOptions xmlns=\"http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014\"\n",
                "PipePath=\"", fakeFilePath, "\"\n",
                "PipeCwd=\"/home/user/my program/src\"\n",
                "ExePath=\"/home/user/myname/foo\"\n",
                "TargetArchitecture=\"x86_64\"\n",
                "AbsolutePrefixSOLibSearchPath='/system/bin'\n",
                "AdditionalSOLibSearchPath='/a/b/c;/a/b/c'\n",
                "MIMode='lldb'\n",
                ">\n",
                "  <CustomLaunchSetupCommands>\n",
                "    <Command Description='Example description'>Example command</Command>\n",
                "  </CustomLaunchSetupCommands>\n",
                "  <LaunchCompleteCommand>None</LaunchCompleteCommand>\n",
                "  <PipeEnvironment>\n",
                "    <EnvironmentEntry Name='PipeVar1' Value='PipeValue1' />\n",
                "  </PipeEnvironment>\n",
                "</PipeLaunchOptions>");

            var baseOptions = GetLaunchOptions(content);
            Assert.IsAssignableFrom<PipeLaunchOptions>(baseOptions);
            var options = (PipeLaunchOptions)baseOptions;

            Assert.Equal(options.PipePath, fakeFilePath);
            Assert.Equal("/home/user/my program/src", options.PipeCwd);
            Assert.Equal("/home/user/myname/foo", options.ExePath);
            Assert.Equal(TargetArchitecture.X64, options.TargetArchitecture);
            Assert.Equal("/system/bin", options.AbsolutePrefixSOLibSearchPath);
            string[] searchPaths = options.GetSOLibSearchPath().ToArray();
            Assert.Equal(2, searchPaths.Length);
            Assert.Equal("/home/user/myname", searchPaths[0]);
            Assert.Equal("/a/b/c", searchPaths[1]);
            Assert.Equal(MIMode.Lldb, options.DebuggerMIMode);
            Assert.True(options.SetupCommands != null && options.SetupCommands.Count == 0);
            Assert.True(options.CustomLaunchSetupCommands != null && options.CustomLaunchSetupCommands.Count == 1);
            var command = options.CustomLaunchSetupCommands[0];
            Assert.False(command.IsMICommand);
            Assert.Equal("Example command", command.CommandText);
            Assert.Equal("Example description", command.Description);
            Assert.Equal(LaunchCompleteCommand.None, options.LaunchCompleteCommand);
            Assert.Equal("PipeVar1", options.PipeEnvironment.First().Name);
            Assert.Equal("PipeValue1", options.PipeEnvironment.First().Value);
        }

        // TODO this test is broken by a bug: the assembly binder only searches the unit test
        // project's output directory for dependencies and thus won't find assemblies in the
        // package cache. This can be worked around by moving the missing assembly
        // (System.Net.Security) to that directory.
        [Fact]
        public void TestLaunchOptions_Tcp1()
        {
            // Tests for:
            // TcpLaunchOptions
            // Using CustomLaunchSetupCommands without specifying a namespace
            // Using CustomLaunchSetupCommands/LaunchCompleteCommand like an attach
            string content = @"<TcpLaunchOptions
              Hostname=""destinationComputer""
              Port=""1234""
              ExePath=""/a/b/c""
              TargetArchitecture=""ARM"">
              <CustomLaunchSetupCommands>
                <Command IgnoreFailures=""false"" Description=""Attaching to the 'foo' process"">-target-attach 1234</Command>
              </CustomLaunchSetupCommands>
              <LaunchCompleteCommand>exec-continue</LaunchCompleteCommand>
            </TcpLaunchOptions>";

            var baseOptions = GetLaunchOptions(content);
            Assert.IsAssignableFrom<TcpLaunchOptions>(baseOptions);
            var options = (TcpLaunchOptions)baseOptions;

            Assert.Equal("/a/b/c", options.ExePath);
            Assert.Equal(TargetArchitecture.ARM, options.TargetArchitecture);
            Assert.Equal(MIMode.Gdb, options.DebuggerMIMode);
            Assert.True(options.SetupCommands != null && options.SetupCommands.Count == 0);
            Assert.True(options.CustomLaunchSetupCommands != null && options.CustomLaunchSetupCommands.Count == 1);
            var command = options.CustomLaunchSetupCommands[0];
            Assert.True(command.IsMICommand);
            Assert.Equal("-target-attach 1234", command.CommandText);
            Assert.Equal("Attaching to the 'foo' process", command.Description);
            Assert.Equal(LaunchCompleteCommand.ExecContinue, options.LaunchCompleteCommand);
        }

        [Fact]
        public void TestLaunchOptions_Tcp2()
        {
            // Test for missing port attribute
            string content = @"<TcpLaunchOptions
              Hostname=""destinationComputer""
              ExePath=""/a/b/c""
              TargetArchitecture=""ARM""/>";

            try
            {
                GetLaunchOptions(content);
                Assert.True(false, "Should be unreachable");
            }
            catch (InvalidLaunchOptionsException e)
            {
                Assert.Contains("Port", e.Message, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void TestLaunchOptions_BadXml1()
        {
            // Test bad XML (extra close element)
            string content = @"<TcpLaunchOptions/></TcpLaunchOptions>";

            try
            {
                GetLaunchOptions(content);
                Assert.True(false, "Should be unreachable");
            }
            catch (InvalidLaunchOptionsException e)
            {
                Assert.StartsWith("Launch options", e.Message, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void TestLaunchOptions_BadXml2()
        {
            // Test for missing port attribute
            string content = @"<ThisIsNotAKnownType/>";

            try
            {
                GetLaunchOptions(content);
                Assert.True(false, "Should be unreachable");
            }
            catch (InvalidLaunchOptionsException e)
            {
                Assert.StartsWith("Launch options", e.Message, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void TestLaunchOptions_BadXml3()
        {
            // Tests for:
            // TcpLaunchOptions
            // Using CustomLaunchSetupCommands without specifying a namespace
            // Using CustomLaunchSetupCommands/LaunchCompleteCommand like an attach
            string content = @"<TcpLaunchOptions xmlns=""http://schemas.microsoft.com/ThisIsABogusNamespace""
              Hostname =""destinationComputer""
              Port=""1234""
              ExePath=""/a/b/c""
              TargetArchitecture=""ARM""/>";

            try
            {
                GetLaunchOptions(content);
                Assert.True(false, "Should be unreachable");
            }
            catch (InvalidLaunchOptionsException e)
            {
                Assert.StartsWith("Launch options", e.Message, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Compilation test, do not execute.
        /// Verify that types relied upon by launcher extensions are exported by MIEngine in current build. 
        /// Don't change this test without checking with C++ team.
        /// </summary>
        internal void VerifyCoreApisPresent()
        {
            LaunchOptions launchOptions = new LocalLaunchOptions("/usr/bin/gdb", "10.10.10.10:2345");
            launchOptions.ExePath = @"c:\users\me\myapp.out";
            launchOptions.AdditionalSOLibSearchPath = @"c:\temp;e:\foo\bar";
            launchOptions.TargetArchitecture = TargetArchitecture.ARM;
            launchOptions.WorkingDirectory = "/home/user";
            launchOptions.DebuggerMIMode = MIMode.Gdb;
            launchOptions.WaitDynamicLibLoad = false;
            launchOptions.VisualizerFile = @"c:\myproject\file.natvis";
            launchOptions.SourceMap = new ReadOnlyCollection<SourceMapEntry>(new List<SourceMapEntry>());
            launchOptions.Environment = new ReadOnlyCollection<EnvironmentEntry>(new List<EnvironmentEntry>());
            Microsoft.DebugEngineHost.HostConfigurationStore configStore = null;
            IDeviceAppLauncherEventCallback eventCallback = null;
            IPlatformAppLauncher iLauncher = null;
            IPlatformAppLauncherSerializer iSerializer = null;
            iLauncher.Initialize(configStore, eventCallback);
            iLauncher.OnResume();
            iLauncher.SetLaunchOptions(string.Empty, string.Empty, string.Empty, (object)null, TargetEngine.Native);
            iLauncher.SetupForDebugging(out launchOptions);
            iLauncher.Dispose();
            XmlSerializer serializer = iSerializer.GetXmlSerializer("foobar");
        }

        private LaunchOptions GetLaunchOptions(string content)
        {
            return LaunchOptions.GetInstance(null, "bogus-exe-path", null, null, content, false, null, TargetEngine.Native, null);
        }
    }
}
