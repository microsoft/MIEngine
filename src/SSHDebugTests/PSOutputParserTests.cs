// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.SSHDebugPS;
using System.Collections.Generic;
using Xunit;

namespace SSHDebugTests
{
    public class PSOutputParserTests
    {
        [Fact]
        public void PSOutputParser_macOS()
        {
            const string username = "username";
            const string architecture = "x86_64";
            const string input =
               "pppppppppp ffffffff rrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr ARGS\n" +
               "1          4004     root                             /sbin/launchd\n" +
               "50         1004004  root                             /System/Library/Frameworks/CoreServices.framework/Versions/A/Frameworks/FSEvents.framework/Versions/A/Support/fseventsd\n" +
               "70         1004004  root                             /System/Library/Frameworks/CoreServices.framework/Frameworks/Metadata.framework/Support/mds\n" +
               "83         4004     _timed                           /usr/libexec/timed\n" +
               "96         80004104 root                             /System/Library/CoreServices/loginwindow.app/Contents/MacOS/loginwindow console\n" +
               "7835       4104     username                         ps axww -o pid=pppppppppp -o flags=ffffffff -o ruser=rrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr -o args\n";

            PSOutputParser psOutputParser = new PSOutputParser(new SystemInformation(username, architecture, PlatformID.MacOSX));
            List<Process> r = psOutputParser.Parse(input);
            Assert.Equal(5, r.Count);
            // Testing flags here as PID USER ARGS are tested in the other tests.
            Assert.Equal((uint)0x4004, r[0].Flags.Value);
            Assert.Equal((uint)0x1004004, r[1].Flags.Value);
            Assert.Equal((uint)0x80004104, r[4].Flags.Value);
        }

        [Fact]
        public void PSOutputParser_Ubuntu14()
        {
            const string username = "greggm";
            const string architecture = "x86_64";
            // example output from ps on a real Ubuntu 14 machine (with many processes removed):
            const string input =
                "pppppppppp rrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr COMMAND\n" +
                "         1 root                             /sbin/init\n" +
                "         2 root                             [kthreadd]\n" +
                "       720 message+                         dbus-daemon --system --fork\n" +
                "      2389 greggm                           -bash\n" +
                "      2580 root                             /sbin/dhclient -d -sf /usr/lib/NetworkManager/nm-dhcp-client.action -pf /run/sendsigs.omit.d/network-manager.dhclient-eth0.pid -lf /var/lib/NetworkManager/dhclient-d08a482b-ff90-4007-9b13-6500eb94b673-eth0.lease -cf /var/lib/NetworkManager/dhclient-eth0.conf eth0\n" +
                "      2913 greggm                           ps axww -o pid=pppppppppp -o ruser=rrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr -o args\n";

            PSOutputParser psOutputParser = new PSOutputParser(new SystemInformation(username, architecture, PlatformID.Unix));
            List<Process> r = psOutputParser.Parse(input);
            Assert.Equal(5, r.Count);

            uint[] pids = { 1, 2, 720, 2389, 2580 };
            string[] userNames = { "root", "root", "message+", "greggm", "root" };
            string[] commandLine = { "/sbin/init", "[kthreadd]", "dbus-daemon --system --fork", "-bash", "/sbin/dhclient -d -sf /usr/lib/NetworkManager/nm-dhcp-client.action -pf /run/sendsigs.omit.d/network-manager.dhclient-eth0.pid -lf /var/lib/NetworkManager/dhclient-d08a482b-ff90-4007-9b13-6500eb94b673-eth0.lease -cf /var/lib/NetworkManager/dhclient-eth0.conf eth0" };
            for (int c = 0; c < r.Count; c++)
            {
                Assert.Equal(pids[c], r[c].Id);
                Assert.Equal(userNames[c], r[c].UserName);
                Assert.Equal(commandLine[c], r[c].CommandLine);
                bool isSameUser = pids[c] == 2389;
                Assert.Equal(isSameUser, r[c].IsSameUser);
            }
        }

        [Fact]
        public void PSOutputParser_SmallCol()
        {
            const string username = "greggm";
            const string architecture = "x86_64";
            // made up output for what could happen if the fields were all just 1 character in size
            const string input =
                "A B C\n" +
                "9 r /sbin/init";

            PSOutputParser psOutputParser = new PSOutputParser(new SystemInformation(username, architecture, PlatformID.Unix));
            List<Process> r = psOutputParser.Parse(input);
            Assert.Single(r);
            Assert.Equal<uint>(9, r[0].Id);
            Assert.Equal("r", r[0].UserName);
            Assert.Equal("/sbin/init", r[0].CommandLine);
        }

        [Fact]
        public void PSOutputParser_NoUserName()
        {
            // Made up ps output from a system where $USER wasn't a thing
            const string username = "";
            const string architecture = "";
            const string input =
                "pppppppppp rrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr COMMAND\n" +
                "         1 root                             /sbin/init\n" +
                "       720                                  dbus-daemon --system --fork\n" +
                "      2389 greggm                           -bash\n";

            PSOutputParser psOutputParser = new PSOutputParser(new SystemInformation(username, architecture, PlatformID.Unix));
            List<Process> r = psOutputParser.Parse(input);
            Assert.Equal(3, r.Count);

            uint[] pids = { 1, 720, 2389 };
            string[] userNames = { "root", "", "greggm" };
            string[] commandLine = { "/sbin/init", "dbus-daemon --system --fork", "-bash" };
            for (int c = 0; c < r.Count; c++)
            {
                Assert.Equal(pids[c], r[c].Id);
                Assert.Equal(userNames[c], r[c].UserName);
                Assert.Equal(commandLine[c], r[c].CommandLine);
                bool isSameUser = pids[c] != 1;
                Assert.Equal(isSameUser, r[c].IsSameUser);
            }
        }
    }
}
