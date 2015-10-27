using System.Xml;
using BlackBerryDebugLauncher;
using Xunit;

namespace MICoreUnitTests
{
    public sealed class BlackBerryLauncherTests
    {
        private BlackBerryDebugLauncher.BlackBerryLaunchOptions CreateFromXml(string exePath, string content)
        {
            using (XmlReader reader = MICore.LaunchOptions.OpenXml(content))
            {
                var serializer = new Microsoft.Xml.Serialization.GeneratedAssembly.BlackBerryLaunchOptionsSerializer();
                var xmlOptions = (MICore.Xml.LaunchOptions.BlackBerryLaunchOptions)MICore.LaunchOptions.Deserialize(serializer, reader);
                return new BlackBerryDebugLauncher.BlackBerryLaunchOptions(exePath, xmlOptions, MICore.TargetEngine.Native);
            }
        }

        [Fact]
        public void TestBlackBerryLaunchOptionsAttach()
        {
            var exePath = "C:\\project\\Debug\\test_app";
            var gdbPath = "C:\\bbndk\\gold_2_0\\host_10_2_0_15\\win32\\x86\\usr\\bin\\ntoarm-gdb.exe";
            var gdbHostPath = "C:\\package\\BlackBerry.GdbHost.exe";
            var ndkHostPath = "C:\\bbndk\\gold_2_0\\host_10_2_0_15\\win32\\x86";
            var ndkTargetPath = "C:\\bbndk\\gold_2_0\\target_10_2_0_1155\\qnx6";

            string content = string.Concat("<BlackBerryLaunchOptions xmlns=\"http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014\"\n",
                    "TargetAddress=\"192.168.2.148\"\n",
                    "GdbPath=\"", gdbPath, "\"\n",
                    "GdbHostPath=\"", gdbHostPath, "\"\n",
                    "NdkHostPath=\"", ndkHostPath, "\"\n",
                    "NdkTargetPath=\"", ndkTargetPath, "\"\n",
                    "PID=\"123456\"\n",
                    "TargetType=\"Phone\"\n",
                    "TargetArchitecture=\"arm\"\n",
                    "Attach=\"false\"\n",
                    "/>\n");

            var options = CreateFromXml(exePath, content);
            Assert.Equal("192.168.2.148", options.TargetAddress);
            Assert.Equal(8000u, options.TargetPort);
            Assert.Equal(MICore.TargetArchitecture.ARM, options.TargetArchitecture);
            Assert.Equal(exePath, options.ExePath);
            Assert.Equal(gdbPath, options.GdbPath);
            Assert.Equal(gdbHostPath, options.GdbHostPath);
            Assert.Equal(TargetType.Phone, options.TargetType);
            Assert.Equal(false, string.IsNullOrEmpty(options.AdditionalSOLibSearchPath));
            Assert.Equal(123456u, options.PID);
            Assert.Equal(false, options.IsAttach);
        }

        [Fact]
        public void TestInvalidBlackBerryLaunchOptions()
        {
            var exePath = "C:\\project\\Debug\\test_app";
            var gdbPath = "C:\\bbndk\\gold_2_0\\host_10_2_0_15\\win32\\x86\\usr\\bin\\ntoarm-gdb.exe";
            var gdbHostPath = "C:\\package\\BlackBerry.GdbHost.exe";
            var ndkHostPath = "C:\\bbndk\\gold_2_0\\host_10_2_0_15\\win32\\x86";
            var ndkTargetPath = "C:\\bbndk\\gold_2_0\\target_10_2_0_1155\\qnx6";

            // missing PID
            string content = string.Concat("<BlackBerryLaunchOptions xmlns=\"http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014\"\n",
                    "TargetAddress=\"192.168.2.148\"\n",
                    "GdbPath=\"", gdbPath, "\"\n",
                    "GdbHostPath=\"", gdbHostPath, "\"\n",
                    "NdkHostPath=\"", ndkHostPath, "\"\n",
                    "NdkTargetPath=\"", ndkTargetPath, "\"\n",
                    "TargetType=\"Phone\"\n",
                    "TargetArchitecture=\"arm\"\n",
                    "Attach=\"false\"\n",
                    "/>\n");
            try
            {
                var options = CreateFromXml(exePath, content);

                Assert.True(false, "Exception was not thrown");
            }
            catch (MICore.InvalidLaunchOptionsException e)
            {
                Assert.True(e.Message.Contains("PID"));
            }
        }
    }
}
