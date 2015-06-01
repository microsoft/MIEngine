// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MICore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MICoreUnitTests
{
    [TestClass]
    public class BasicLaunchOptionsTests
    {
        [TestMethod]
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
            Assert.IsInstanceOfType(baseOptions, typeof(LocalLaunchOptions));
            var options = (LocalLaunchOptions)baseOptions;

            Assert.AreEqual(options.MIDebuggerPath, fakeFilePath);
            Assert.AreEqual(options.MIDebuggerServerAddress, "myserverbox:345");
            Assert.AreEqual(options.ExePath, fakeFilePath);
            Assert.AreEqual(options.TargetArchitecture, TargetArchitecture.ARM);
            Assert.IsTrue(string.IsNullOrEmpty(options.AdditionalSOLibSearchPath));
            Assert.AreEqual(options.DebuggerMIMode, MIMode.Gdb);
            Assert.AreEqual(options.LaunchCompleteCommand, LaunchCompleteCommand.ExecRun);
            Assert.IsNull(options.CustomLaunchSetupCommands);
            Assert.IsTrue(options.SetupCommands != null && options.SetupCommands.Count == 0);
        }

        [TestMethod]
        public void TestLaunchOptions_Local2()
        {
            // Differences from #1: There is an empty CustomLaunchSetupCommands, and MIMode is set
            string fakeFilePath = typeof(BasicLaunchOptionsTests).Assembly.Location;
            string content = string.Concat("<LocalLaunchOptions\n",
                "MIDebuggerPath=\"", fakeFilePath, "\"\n",
                "MIDebuggerServerAddress=\"myserverbox:345\"\n",
                "ExePath=\"", fakeFilePath, "\"\n",
                "TargetArchitecture=\"arm\"\n",
                "MIMode=\"clrdbg\"",
                ">\n",
                "  <CustomLaunchSetupCommands>\n",
                "  </CustomLaunchSetupCommands>\n",
                "</LocalLaunchOptions>");

            var baseOptions = GetLaunchOptions(content);
            Assert.IsInstanceOfType(baseOptions, typeof(LocalLaunchOptions));
            var options = (LocalLaunchOptions)baseOptions;

            Assert.AreEqual(options.MIDebuggerPath, fakeFilePath);
            Assert.AreEqual(options.MIDebuggerServerAddress, "myserverbox:345");
            Assert.AreEqual(options.ExePath, fakeFilePath);
            Assert.AreEqual(options.TargetArchitecture, TargetArchitecture.ARM);
            Assert.IsTrue(string.IsNullOrEmpty(options.AdditionalSOLibSearchPath));
            Assert.AreEqual(options.DebuggerMIMode, MIMode.Clrdbg);
            Assert.AreEqual(options.LaunchCompleteCommand, LaunchCompleteCommand.ExecRun);
            Assert.IsTrue(options.CustomLaunchSetupCommands != null && options.CustomLaunchSetupCommands.Count == 0);
            Assert.IsTrue(options.SetupCommands != null && options.SetupCommands.Count == 0);
        }

        [TestMethod]
        public void TestLaunchOptions_Local3()
        {
            // Differences from #2: required argument 'MIDebuggerPath' is missing
            string fakeFilePath = typeof(BasicLaunchOptionsTests).Assembly.Location;
            string content = string.Concat("<LocalLaunchOptions\n",
                "MIDebuggerServerAddress=\"myserverbox:345\"\n",
                "ExePath=\"", fakeFilePath, "\"\n",
                "TargetArchitecture=\"arm\"\n",
                "MIMode=\"clrdbg\"",
                ">\n",
                "  <CustomLaunchSetupCommands>\n",
                "  </CustomLaunchSetupCommands>\n",
                "</LocalLaunchOptions>");

            try
            {
                var baseOptions = GetLaunchOptions(content);
                Assert.Fail("Code path should be unreachable");
            }
            catch (ArgumentException e)
            {
                Assert.IsTrue(e.Message.Contains("MIDebuggerPath"));
            }
        }


        [TestMethod]
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
            Assert.IsInstanceOfType(baseOptions, typeof(PipeLaunchOptions));
            var options = (PipeLaunchOptions)baseOptions;

            Assert.AreEqual(options.PipePath, fakeFilePath);
            Assert.AreEqual(options.ExePath, "/home/user/myname/foo");
            Assert.AreEqual(options.ExeArguments, "arg1 arg2");
            Assert.AreEqual(options.TargetArchitecture, TargetArchitecture.X64);
            Assert.IsTrue(string.IsNullOrEmpty(options.AdditionalSOLibSearchPath));
            Assert.AreEqual(options.DebuggerMIMode, MIMode.Gdb);
            Assert.AreEqual(options.LaunchCompleteCommand, LaunchCompleteCommand.ExecRun);
            Assert.IsTrue(options.CustomLaunchSetupCommands == null);
            Assert.IsTrue(options.SetupCommands != null && options.SetupCommands.Count == 1);
            Assert.IsTrue(options.SetupCommands[0].IsMICommand);
            Assert.AreEqual(options.SetupCommands[0].CommandText, "gdb-set my-example-setting on");
            Assert.IsTrue(options.SetupCommands[0].Description.Contains("gdb-set"));
            Assert.IsFalse(options.SetupCommands[0].IgnoreFailures);
        }

        [TestMethod]
        public void TestLaunchOptions_Pipe2()
        {
            // Test for:
            // MIMode=lldb
            // Having commands in CustomLaunchSetupCommands
            // Specifying the LaunchCompleteCommand
            string fakeFilePath = typeof(BasicLaunchOptionsTests).Assembly.Location;
            string content = string.Concat("<PipeLaunchOptions xmlns=\"http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014\"\n",
                "PipePath=\"", fakeFilePath, "\"\n",
                "ExePath=\"/home/user/myname/foo\"\n",
                "TargetArchitecture=\"x86_64\"\n",
                "AdditionalSOLibSearchPath='/a/b/c;/a/b/c'\n",
                "MIMode='lldb'\n",
                ">\n",
                "  <CustomLaunchSetupCommands>\n",
                "    <Command Description='Example description'>Example command</Command>\n",
                "  </CustomLaunchSetupCommands>\n",
                "  <LaunchCompleteCommand>None</LaunchCompleteCommand>\n",
                "</PipeLaunchOptions>");

            var baseOptions = GetLaunchOptions(content);
            Assert.IsInstanceOfType(baseOptions, typeof(PipeLaunchOptions));
            var options = (PipeLaunchOptions)baseOptions;

            Assert.AreEqual(options.PipePath, fakeFilePath);
            Assert.AreEqual(options.ExePath, "/home/user/myname/foo");
            Assert.AreEqual(options.TargetArchitecture, TargetArchitecture.X64);
            string[] searchPaths = options.GetSOLibSearchPath().ToArray();
            Assert.AreEqual(searchPaths.Length, 2);
            Assert.AreEqual(searchPaths[0], "/home/user/myname");
            Assert.AreEqual(searchPaths[1], "/a/b/c");
            Assert.AreEqual(options.DebuggerMIMode, MIMode.Lldb);
            Assert.IsTrue(options.SetupCommands != null && options.SetupCommands.Count == 0);
            Assert.IsTrue(options.CustomLaunchSetupCommands != null && options.CustomLaunchSetupCommands.Count == 1);
            var command = options.CustomLaunchSetupCommands[0];
            Assert.IsFalse(command.IsMICommand);
            Assert.AreEqual(command.CommandText, "Example command");
            Assert.AreEqual(command.Description, "Example description");
            Assert.AreEqual(options.LaunchCompleteCommand, LaunchCompleteCommand.None);
        }

        [TestMethod]
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
            Assert.IsInstanceOfType(baseOptions, typeof(TcpLaunchOptions));
            var options = (TcpLaunchOptions)baseOptions;

            Assert.AreEqual(options.ExePath, "/a/b/c");
            Assert.AreEqual(options.TargetArchitecture, TargetArchitecture.ARM);
            Assert.AreEqual(options.DebuggerMIMode, MIMode.Gdb);
            Assert.IsTrue(options.SetupCommands != null && options.SetupCommands.Count == 0);
            Assert.IsTrue(options.CustomLaunchSetupCommands != null && options.CustomLaunchSetupCommands.Count == 1);
            var command = options.CustomLaunchSetupCommands[0];
            Assert.IsTrue(command.IsMICommand);
            Assert.AreEqual(command.CommandText, "target-attach 1234");
            Assert.AreEqual(command.Description, "Attaching to the 'foo' process");
            Assert.AreEqual(options.LaunchCompleteCommand, LaunchCompleteCommand.ExecContinue);
        }

        [TestMethod]
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
                Assert.Fail("Should be unreachable");
            }
            catch (ArgumentException e)
            {
                Assert.IsTrue(e.Message.Contains("Port"));
            }
        }

        [TestMethod]
        public void TestLaunchOptions_BadXml1()
        {
            // Test bad XML (extra close element)
            string content = @"<TcpLaunchOptions/></TcpLaunchOptions>";

            try
            {
                GetLaunchOptions(content);
                Assert.Fail("Should be unreachable");
            }
            catch (ArgumentException e)
            {
                Assert.IsTrue(e.Message.StartsWith("Launch options", StringComparison.Ordinal));
            }
        }

        [TestMethod]
        public void TestLaunchOptions_BadXml2()
        {
            // Test for missing port attribute
            string content = @"<ThisIsNotAKnownType/>";

            try
            {
                GetLaunchOptions(content);
                Assert.Fail("Should be unreachable");
            }
            catch (ArgumentException e)
            {
                Assert.IsTrue(e.Message.StartsWith("Launch options", StringComparison.Ordinal));
            }
        }

        [TestMethod]
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
                Assert.Fail("Should be unreachable");
            }
            catch (ArgumentException e)
            {
                Assert.IsTrue(e.Message.StartsWith("Launch options", StringComparison.Ordinal));
            }
        }

        private LaunchOptions GetLaunchOptions(string content)
        {
            return LaunchOptions.GetInstance("bogus-registry-root", "bogus-exe-path", null, null, content, null);
        }
    }
}
