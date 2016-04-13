// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AndroidDebugLauncher;
using MICore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Xunit;

namespace MICoreUnitTests
{
    public class AndroidLauncherTests
    {
        [Fact]
        public void TestAndroidLaunchOptions1()
        {
            string temp = Environment.GetEnvironmentVariable("TMP");

            string content = string.Concat("<AndroidLaunchOptions xmlns=\"http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014\"\n",
                "Package=\"com.example.hellojni\"\n",
                "LaunchActivity=\".HelloJni\"\n",
                "TargetArchitecture=\"arm\"\n",
                "IntermediateDirectory=\"", temp, "\"\n",
                "AdditionalSOLibSearchPath=\"c:\\example\\bin\\debug;c:\\someotherdir\\bin\\debug\"\n",
                "DeviceId=\"default\"/>");

            var options = CreateFromXml(content);
            Assert.Equal(options.Package, "com.example.hellojni");
            Assert.Equal(options.LaunchActivity, ".HelloJni");
            Assert.Equal(options.TargetArchitecture, MICore.TargetArchitecture.ARM);
            Assert.Equal(options.IntermediateDirectory, temp);
            Assert.Equal(options.AdditionalSOLibSearchPath, "c:\\example\\bin\\debug;c:\\someotherdir\\bin\\debug");
            Assert.Equal(options.AbsolutePrefixSOLibSearchPath, "\"\"");
            Assert.Equal(options.DeviceId, "default");
            Assert.False(options.IsAttach);
        }

        [Fact]
        public void TestAndroidLaunchOptions2()
        {
            // NOTE: 'LaunchActivity' is missing
            string temp = Environment.GetEnvironmentVariable("TMP");

            string content = string.Concat("<AndroidLaunchOptions xmlns=\"http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014\"\n",
                "Package=\"com.example.hellojni\"\n",
                "TargetArchitecture=\"arm\"\n",
                "IntermediateDirectory=\"", temp, "\"\n",
                "AdditionalSOLibSearchPath=\"c:\\example\\bin\\debug;c:\\someotherdir\\bin\\debug\"\n",
                "DeviceId=\"default\"/>");

            try
            {
                var options = CreateFromXml(content);

                Assert.True(false, "Exception was not thrown");
            }
            catch (MICore.InvalidLaunchOptionsException e)
            {
                Assert.True(e.Message.Contains("LaunchActivity"));
            }
        }

        [Fact]
        public void TestGetSourceRoots()
        {
            var actualRoots = AndroidLaunchOptions.GetSourceRoots("c:\\example\\src; d:\\someotherdir\\src\\**;d://otherdir//src//**");

            List<SourceRoot> expectedRoots = new List<SourceRoot>();
            expectedRoots.Add(new SourceRoot("c:\\example\\src", false));
            expectedRoots.Add(new SourceRoot("d:\\someotherdir\\src\\", true));
            expectedRoots.Add(new SourceRoot("d://otherdir//src//", true));

            Assert.Equal(expectedRoots.Count(), actualRoots.Count());
            var comparer = new SourceRootComparer();
            expectedRoots.All(x => actualRoots.Contains(x, comparer));
        }

        private class SourceRootComparer : IEqualityComparer<SourceRoot>
        {
            public bool Equals(SourceRoot x, SourceRoot y)
            {
                return x.Path.Equals(y.Path) && x.RecursiveSearchEnabled == y.RecursiveSearchEnabled;
            }

            public int GetHashCode(SourceRoot obj)
            {
                return obj.GetHashCode();
            }
        }

        [Fact]
        public void TestAndroidLaunchOptionsAttach()
        {
            string temp = Environment.GetEnvironmentVariable("TMP");

            string content = string.Concat("<AndroidLaunchOptions xmlns=\"http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014\"\n",
                "Package=\"com.example.hellojni\"\n",
                "TargetArchitecture=\"arm\"\n",
                "IntermediateDirectory=\"", temp, "\"\n",
                "AdditionalSOLibSearchPath=\"c:\\example\\bin\\debug;c:\\someotherdir\\bin\\debug\"\n",
                "DeviceId=\"default\"\n",
                "Attach=\"true\"/>");

            var options = CreateFromXml(content);
            Assert.Equal(options.Package, "com.example.hellojni");
            Assert.Equal(null, options.LaunchActivity);
            Assert.Equal(options.TargetArchitecture, MICore.TargetArchitecture.ARM);
            Assert.Equal(options.IntermediateDirectory, temp);
            Assert.Equal(options.AdditionalSOLibSearchPath, "c:\\example\\bin\\debug;c:\\someotherdir\\bin\\debug");
            Assert.Equal(options.AbsolutePrefixSOLibSearchPath, "\"\"");
            Assert.Equal(options.DeviceId, "default");
            Assert.Equal(options.IsAttach, true);
        }

        [Fact]
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
                var options = CreateFromXml(content);

                Assert.True(false, "Exception was not thrown");
            }
            catch (MICore.InvalidLaunchOptionsException e)
            {
                Assert.True(e.Message.Contains("BadValue"));
            }
        }

        private AndroidDebugLauncher.AndroidLaunchOptions CreateFromXml(string content)
        {
            using (XmlReader reader = MICore.LaunchOptions.OpenXml(content))
            {
                var serializer = new XmlSerializer(typeof(MICore.Xml.LaunchOptions.AndroidLaunchOptions));
                var xmlOptions = (MICore.Xml.LaunchOptions.AndroidLaunchOptions)MICore.LaunchOptions.Deserialize(serializer, reader);
                return new AndroidLaunchOptions(xmlOptions, TargetEngine.Native);
            }
        }

        [Fact]
        public void TestTextColumn()
        {
            string[] lines = GetFakeProcessListText1();
            TextColumn[] columns = TextColumn.TryParseHeader(lines[0]);
            Assert.True(columns != null && columns.Length == 8);
            Assert.Equal(columns[0].Name, "USER");
            Assert.Equal(columns[1].Name, "PID");
            Assert.Equal(columns[2].Name, "PPID");
            Assert.Equal(columns[3].Name, "VSIZE");
            Assert.Equal(columns[4].Name, "RSS");
            Assert.Equal(columns[5].Name, "WCHAN");
            Assert.Equal(columns[6].Name, "PC");
            Assert.Equal(columns[7].Name, "NAME");

            Assert.Equal(columns[0].ExtractCell(lines[1]), "root");
            Assert.Equal(columns[1].ExtractCell(lines[1]), "1");
            Assert.Equal(columns[2].ExtractCell(lines[1]), "0");
            Assert.Equal(columns[3].ExtractCell(lines[1]), "640");
            Assert.Equal(columns[4].ExtractCell(lines[1]), "496");
            Assert.Equal(columns[5].ExtractCell(lines[1]), "c00bd520");
            Assert.Equal(columns[6].ExtractCell(lines[1]), "00019fb8 S");
            Assert.Equal(columns[7].ExtractCell(lines[1]), "/init");

            Assert.Equal(columns[0].ExtractCell(lines[2]), "root");
            Assert.Equal(columns[1].ExtractCell(lines[2]), "2");
            Assert.Equal(columns[2].ExtractCell(lines[2]), "0");
            Assert.Equal(columns[3].ExtractCell(lines[2]), "0");
            Assert.Equal(columns[4].ExtractCell(lines[2]), "0");
            Assert.Equal(columns[5].ExtractCell(lines[2]), "c00335a0");
            Assert.Equal(columns[6].ExtractCell(lines[2]), "00000000 S");
            Assert.Equal(columns[7].ExtractCell(lines[2]), "fake_name");

            Assert.Equal(columns[0].ExtractCell(lines[3]), "root");
            Assert.Equal(columns[1].ExtractCell(lines[3]), "3");
            Assert.Equal(columns[2].ExtractCell(lines[3]), "2");
            Assert.Equal(columns[3].ExtractCell(lines[3]), "0");
            Assert.Equal(columns[4].ExtractCell(lines[3]), "0");
            Assert.Equal(columns[5].ExtractCell(lines[3]), "c001e39c");
            Assert.Equal(columns[6].ExtractCell(lines[3]), "00000000 S");
            Assert.Equal(columns[7].ExtractCell(lines[3]), "fake_name");

            Assert.Equal(columns[0].ExtractCell(lines[4]), "u0_a56");
            Assert.Equal(columns[1].ExtractCell(lines[4]), "1165");
            Assert.Equal(columns[2].ExtractCell(lines[4]), "50");
            Assert.Equal(columns[3].ExtractCell(lines[4]), "211416");
            Assert.Equal(columns[4].ExtractCell(lines[4]), "16040");
            Assert.Equal(columns[5].ExtractCell(lines[4]), "ffffffff");
            Assert.Equal(columns[6].ExtractCell(lines[4]), "b6f46798 S");
            Assert.Equal(columns[7].ExtractCell(lines[4]), "com.example.hellojni");

            Assert.Equal(columns[0].ExtractCell(lines[5]), "root");
            Assert.Equal(columns[1].ExtractCell(lines[5]), "1181");
            Assert.Equal(columns[2].ExtractCell(lines[5]), "59");
            Assert.Equal(columns[3].ExtractCell(lines[5]), "1236");
            Assert.Equal(columns[4].ExtractCell(lines[5]), "460");
            Assert.Equal(columns[5].ExtractCell(lines[5]), "00000000");
            Assert.Equal(columns[6].ExtractCell(lines[5]), "b6f11158 R");
            Assert.Equal(columns[7].ExtractCell(lines[5]), "ps");
        }

        [Fact]
        public void TestProcessListParser1()
        {
            List<int> result;

            var processListParer = new ProcessListParser(GetFakeProcessListText1());

            result = processListParer.FindProcesses("hellojni");
            Assert.Equal(result.Count, 0);

            result = processListParer.FindProcesses("com.example.hellojni");
            Assert.Equal(result.Count, 1);
            Assert.Equal(result[0], 1165);

            result = processListParer.FindProcesses("fake_name");
            Assert.Equal(result.Count, 2);
            Assert.Equal(result[0], 2);
            Assert.Equal(result[1], 3);
        }

        [Fact]
        public void TestProcessListParser2()
        {
            List<int> result;

            var processListParer = new ProcessListParser(GetFakeProcessListText2());

            result = processListParer.FindProcesses("hellojni");
            Assert.Equal(result.Count, 0);

            result = processListParer.FindProcesses("com.example.hellojni");
            Assert.Equal(result.Count, 1);
            Assert.Equal(result[0], 7848);

            result = processListParer.FindProcesses("com.example.memory_hog");
            Assert.Equal(result.Count, 1);
            Assert.Equal(result[0], 7915);
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

        [Fact]
        public void TestPwdOutputParser()
        {
            string result = PwdOutputParser.ExtractWorkingDirectory("/example/directory", "does-not-matter");
            Assert.Equal(result, "/example/directory");

            string outputWithDebugSpew = string.Concat(
                "Function: selinux_compare_spd_ram , priority [2] , priority version is VE=SEPF_SM-G920I_5.0.2_0011\n",
                "[DEBUG] get_category: variable seinfo: default sensitivity: NULL, cateogry: NULL\n",
                "/data/data/com.Android53");
            result = PwdOutputParser.ExtractWorkingDirectory(outputWithDebugSpew, "com.Android53");
            Assert.Equal(result, "/data/data/com.Android53");

            try
            {
                PwdOutputParser.ExtractWorkingDirectory("/bogus-directory-with-a-*", "com.Android53");
                Assert.True(false, "Code should not be reached");
            }
            catch (LauncherException e)
            {
                Assert.True(e.TelemetryCode == Telemetry.LaunchFailureCode.BadPwdOutput);
            }

            try
            {
                PwdOutputParser.ExtractWorkingDirectory("/directory1\n/directory2", "com.Android53");
                Assert.True(false, "Code should not be reached");
            }
            catch (LauncherException e)
            {
                Assert.True(e.TelemetryCode == Telemetry.LaunchFailureCode.BadPwdOutput);
            }

            try
            {
                PwdOutputParser.ExtractWorkingDirectory("run-as: Package 'com.bogus.hellojni' is unknown", "com.bogus.hellojni");
                Assert.True(false, "Code should not be reached");
            }
            catch (LauncherException e)
            {
                Assert.True(e.TelemetryCode == Telemetry.LaunchFailureCode.RunAsPackageUnknown);
            }
        }

        [Fact]
        public void TestRunAsOutputParser1()
        {
            try
            {
                RunAsOutputParser.ThrowIfRunAsErrors("run-as: Package 'com.bogus.hellojni' is unknown", "com.bogus.hellojni");
                Assert.True(false, "Code should not be reached");
            }
            catch (LauncherException e)
            {
                Assert.True(e.TelemetryCode == Telemetry.LaunchFailureCode.RunAsPackageUnknown);
            }
        }

        [Fact]
        public void TestRunAsOutputParser2()
        {
            try
            {
                RunAsOutputParser.ThrowIfRunAsErrors("run-as: Package 'com.android.phone' is not an application", "com.android.phone");
                Assert.True(false, "Code should not be reached");
            }
            catch (LauncherException e)
            {
                Assert.True(e.TelemetryCode == Telemetry.LaunchFailureCode.RunAsFailure);
            }
        }

        [Fact]
        public void TestRunAsOutputParser3()
        {
            try
            {
                RunAsOutputParser.ThrowIfRunAsErrors("run-as: Package 'com.android.email' is not debuggable", "com.android.email");
                Assert.True(false, "Code should not be reached");
            }
            catch (LauncherException e)
            {
                Assert.True(e.TelemetryCode == Telemetry.LaunchFailureCode.RunAsPackageNotDebuggable);
            }
        }
    }
}
