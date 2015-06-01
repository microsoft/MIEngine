// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AndroidDebugLauncher;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MICoreUnitTests
{
    [TestClass]
    public class AndroidLauncherTests
    {
        [TestMethod]
        public void TestAndroidLaunchOptions1()
        {
            string temp = Environment.GetEnvironmentVariable("TMP");

            string content = string.Concat("<AndroidLaunchOptions xmlns=\"http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014\"\n",
                "Package=\"com.example.hellojni\"\n",
                "LaunchActivity=\".HelloJni\"\n",
                "TargetArchitecture=\"arm\"\n",
                "IntermediateDirectory=\"", temp, "\"\n",
                "AdditionalSOLibSearchPath=\"c:\\example\\bin\\debug;c:\\someotherdir\\bin\\debug\"\n",
                "DeviceId=\"default\">");

            var options = AndroidDebugLauncher.AndroidLaunchOptions.CreateFromXml(content);
            Assert.AreEqual(options.Package, "com.example.hellojni");
            Assert.AreEqual(options.LaunchActivity, ".HelloJni");
            Assert.AreEqual(options.TargetArchitecture, MICore.TargetArchitecture.ARM);
            Assert.AreEqual(options.IntermediateDirectory, temp);
            Assert.AreEqual(options.AdditionalSOLibSearchPath, "c:\\example\\bin\\debug;c:\\someotherdir\\bin\\debug");
            Assert.AreEqual(options.DeviceId, "default");
            Assert.AreEqual(options.IsAttach, false);
        }

        [TestMethod]
        public void TestAndroidLaunchOptions2()
        {
            // NOTE: 'LaunchActivity' is missing
            string temp = Environment.GetEnvironmentVariable("TMP");

            string content = string.Concat("<AndroidLaunchOptions xmlns=\"http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014\"\n",
                "Package=\"com.example.hellojni\"\n",
                "TargetArchitecture=\"arm\"\n",
                "IntermediateDirectory=\"", temp, "\"\n",
                "AdditionalSOLibSearchPath=\"c:\\example\\bin\\debug;c:\\someotherdir\\bin\\debug\"\n",
                "DeviceId=\"default\">");

            try
            {
                var options = AndroidDebugLauncher.AndroidLaunchOptions.CreateFromXml(content);

                Assert.Fail("Exception was not thrown");
            }
            catch (ArgumentException e)
            {
                Assert.IsTrue(e.Message.Contains("LaunchActivity"));
            }
        }

        [TestMethod]
        public void TestAndroidLaunchOptionsAttach()
        {
            string temp = Environment.GetEnvironmentVariable("TMP");

            string content = string.Concat("<AndroidLaunchOptions xmlns=\"http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014\"\n",
                "Package=\"com.example.hellojni\"\n",
                "TargetArchitecture=\"arm\"\n",
                "IntermediateDirectory=\"", temp, "\"\n",
                "AdditionalSOLibSearchPath=\"c:\\example\\bin\\debug;c:\\someotherdir\\bin\\debug\"\n",
                "DeviceId=\"default\"\n",
                "Attach=\"true\">");

            var options = AndroidDebugLauncher.AndroidLaunchOptions.CreateFromXml(content);
            Assert.AreEqual(options.Package, "com.example.hellojni");
            Assert.AreEqual(null, options.LaunchActivity);
            Assert.AreEqual(options.TargetArchitecture, MICore.TargetArchitecture.ARM);
            Assert.AreEqual(options.IntermediateDirectory, temp);
            Assert.AreEqual(options.AdditionalSOLibSearchPath, "c:\\example\\bin\\debug;c:\\someotherdir\\bin\\debug");
            Assert.AreEqual(options.DeviceId, "default");
            Assert.AreEqual(options.IsAttach, true);
        }

        [TestMethod]
        public void TestAndroidLaunchOptionsAttachBadAttribute()
        {
            // NOTE: Attach is not 'true' or 'false'
            string temp = Environment.GetEnvironmentVariable("TMP");

            string content = string.Concat("<AndroidLaunchOptions xmlns=\"http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014\"\n",
                "Package=\"com.example.hellojni\"\n",
                "TargetArchitecture=\"arm\"\n",
                "IntermediateDirectory=\"", temp, "\"\n",
                "AdditionalSOLibSearchPath=\"c:\\example\\bin\\debug;c:\\someotherdir\\bin\\debug\"\n",
                "DeviceId=\"default\"\n",
                "Attach=\"BadValue\">");

            try
            {
                var options = AndroidDebugLauncher.AndroidLaunchOptions.CreateFromXml(content);

                Assert.Fail("Exception was not thrown");
            }
            catch (LauncherException e)
            {
                Assert.IsTrue(e.Message.Contains("Attach"));
            }
        }

        [TestMethod]
        public void TestTextColumn()
        {
            string[] lines = GetFakeProcessListText1();
            TextColumn[] columns = TextColumn.TryParseHeader(lines[0]);
            Assert.IsTrue(columns != null && columns.Length == 8);
            Assert.AreEqual(columns[0].Name, "USER");
            Assert.AreEqual(columns[1].Name, "PID");
            Assert.AreEqual(columns[2].Name, "PPID");
            Assert.AreEqual(columns[3].Name, "VSIZE");
            Assert.AreEqual(columns[4].Name, "RSS");
            Assert.AreEqual(columns[5].Name, "WCHAN");
            Assert.AreEqual(columns[6].Name, "PC");
            Assert.AreEqual(columns[7].Name, "NAME");

            Assert.AreEqual(columns[0].ExtractCell(lines[1]), "root");
            Assert.AreEqual(columns[1].ExtractCell(lines[1]), "1");
            Assert.AreEqual(columns[2].ExtractCell(lines[1]), "0");
            Assert.AreEqual(columns[3].ExtractCell(lines[1]), "640");
            Assert.AreEqual(columns[4].ExtractCell(lines[1]), "496");
            Assert.AreEqual(columns[5].ExtractCell(lines[1]), "c00bd520");
            Assert.AreEqual(columns[6].ExtractCell(lines[1]), "00019fb8 S");
            Assert.AreEqual(columns[7].ExtractCell(lines[1]), "/init");

            Assert.AreEqual(columns[0].ExtractCell(lines[2]), "root");
            Assert.AreEqual(columns[1].ExtractCell(lines[2]), "2");
            Assert.AreEqual(columns[2].ExtractCell(lines[2]), "0");
            Assert.AreEqual(columns[3].ExtractCell(lines[2]), "0");
            Assert.AreEqual(columns[4].ExtractCell(lines[2]), "0");
            Assert.AreEqual(columns[5].ExtractCell(lines[2]), "c00335a0");
            Assert.AreEqual(columns[6].ExtractCell(lines[2]), "00000000 S");
            Assert.AreEqual(columns[7].ExtractCell(lines[2]), "fake_name");

            Assert.AreEqual(columns[0].ExtractCell(lines[3]), "root");
            Assert.AreEqual(columns[1].ExtractCell(lines[3]), "3");
            Assert.AreEqual(columns[2].ExtractCell(lines[3]), "2");
            Assert.AreEqual(columns[3].ExtractCell(lines[3]), "0");
            Assert.AreEqual(columns[4].ExtractCell(lines[3]), "0");
            Assert.AreEqual(columns[5].ExtractCell(lines[3]), "c001e39c");
            Assert.AreEqual(columns[6].ExtractCell(lines[3]), "00000000 S");
            Assert.AreEqual(columns[7].ExtractCell(lines[3]), "fake_name");

            Assert.AreEqual(columns[0].ExtractCell(lines[4]), "u0_a56");
            Assert.AreEqual(columns[1].ExtractCell(lines[4]), "1165");
            Assert.AreEqual(columns[2].ExtractCell(lines[4]), "50");
            Assert.AreEqual(columns[3].ExtractCell(lines[4]), "211416");
            Assert.AreEqual(columns[4].ExtractCell(lines[4]), "16040");
            Assert.AreEqual(columns[5].ExtractCell(lines[4]), "ffffffff");
            Assert.AreEqual(columns[6].ExtractCell(lines[4]), "b6f46798 S");
            Assert.AreEqual(columns[7].ExtractCell(lines[4]), "com.example.hellojni");

            Assert.AreEqual(columns[0].ExtractCell(lines[5]), "root");
            Assert.AreEqual(columns[1].ExtractCell(lines[5]), "1181");
            Assert.AreEqual(columns[2].ExtractCell(lines[5]), "59");
            Assert.AreEqual(columns[3].ExtractCell(lines[5]), "1236");
            Assert.AreEqual(columns[4].ExtractCell(lines[5]), "460");
            Assert.AreEqual(columns[5].ExtractCell(lines[5]), "00000000");
            Assert.AreEqual(columns[6].ExtractCell(lines[5]), "b6f11158 R");
            Assert.AreEqual(columns[7].ExtractCell(lines[5]), "ps");
        }

        [TestMethod]
        public void TestProcessListParser1()
        {
            List<int> result;

            var processListParer = new ProcessListParser(GetFakeProcessListText1());

            result = processListParer.FindProcesses("hellojni");
            Assert.AreEqual(result.Count, 0);

            result = processListParer.FindProcesses("com.example.hellojni");
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(result[0], 1165);

            result = processListParer.FindProcesses("fake_name");
            Assert.AreEqual(result.Count, 2);
            Assert.AreEqual(result[0], 2);
            Assert.AreEqual(result[1], 3);
        }

        [TestMethod]
        public void TestProcessListParser2()
        {
            List<int> result;

            var processListParer = new ProcessListParser(GetFakeProcessListText2());

            result = processListParer.FindProcesses("hellojni");
            Assert.AreEqual(result.Count, 0);

            result = processListParer.FindProcesses("com.example.hellojni");
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(result[0], 7848);

            result = processListParer.FindProcesses("com.example.memory_hog");
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(result[0], 7915);
        }

        private string[] GetFakeProcessListText1()
        {
            // Output from the 'ps' command
            string[] lines = {
                "USER     PID   PPID  VSIZE  RSS     WCHAN    PC         NAME",
                "root      1     0     640    496   c00bd520 00019fb8 S /init",
                "root      2     0     0      0     c00335a0 00000000 S fake_name",
                "root      3     2     0      0     c001e39c 00000000 S fake_name",
                "u0_a56    1165  50    211416 16040 ffffffff b6f46798 S com.example.hellojni",
                "root      1181  59    1236   460   00000000 b6f11158 R ps"
            };

            return lines;
        }

        private string[] GetFakeProcessListText2()
        {
            // Output from the 'ps' command on a Nexus 5 running Lollipop, the
            // memory_hog process is fake, but I am assuming that is what we would get
            // if a process was consuming that much memory
            string[] lines = {
                "USER     PID   PPID  VSIZE  RSS     WCHAN    PC        NAME",
                "root      1     0     2616   780   ffffffff 00000000 S /init",
                "root      2     0     0      0     ffffffff 00000000 S kthreadd",
                "root      3     2     0      0     ffffffff 00000000 S ksoftirqd/0",
                "root      7711  2     0      0     ffffffff 00000000 S kworker/0:1",
                "root      7818  2     0      0     ffffffff 00000000 S kworker/0:0H",
                "u0_a82    7848  195   1488916 42940 ffffffff 00000000 t com.example.hellojni",
                "u0_a82    7874  5448  656    372   ffffffff 00000000 S /data/data/com.example.hellojni/lib/gdbserver",
                "u0_a83    7915  195   111488916 42940 ffffffff 00000000 t com.example.memory_hog",
                "shell     7916  5448  4460   756   00000000 b6eafb18 R ps"
            };

            return lines;
        }
    }
}
