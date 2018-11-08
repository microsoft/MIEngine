// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.SSHDebugPS;
using System.Collections.Generic;

namespace SSHDebugTests
{
    [TestClass]
    public class PSOutputParserTests
    {
        [TestMethod]
        public void PSOutputParser_Ubuntu14()
        {
            const string username = "greggm";
            // example output from ps on a real Ubuntu 14 machine (with many processes removed):
            const string input =
                "    A B        C\n" +
                "    1 root     /sbin/init\n" +
                "    2 root     [kthreadd]\n" +
                "  720 message+ dbus-daemon --system --fork\n" +
                " 2389 greggm   -bash\n" +
                " 2580 root     /sbin/dhclient -d -sf /usr/lib/NetworkManager/nm-dhcp-client.action -pf /run/sendsigs.omit.d/network-manager.dhclient-eth0.pid -lf /var/lib/NetworkManager/dhclient-d08a482b-ff90-4007-9b13-6500eb94b673-eth0.lease -cf /var/lib/NetworkManager/dhclient-eth0.conf eth0\n" +
                " 2913 greggm   ps -axww -o pid=A,ruser=B,args=C\n";

            List<Process> r = PSOutputParser.Parse(input, username);
            Assert.AreEqual(5, r.Count);

            uint[] pids = { 1, 2, 720, 2389, 2580 };
            string[] userNames = { "root", "root", "message+", "greggm", "root" };
            string[] commandLine = { "/sbin/init", "[kthreadd]", "dbus-daemon --system --fork", "-bash", "/sbin/dhclient -d -sf /usr/lib/NetworkManager/nm-dhcp-client.action -pf /run/sendsigs.omit.d/network-manager.dhclient-eth0.pid -lf /var/lib/NetworkManager/dhclient-d08a482b-ff90-4007-9b13-6500eb94b673-eth0.lease -cf /var/lib/NetworkManager/dhclient-eth0.conf eth0" };
            for (int c = 0; c < r.Count; c++)
            {
                Assert.AreEqual(pids[c], r[c].Id);
                Assert.AreEqual(userNames[c], r[c].UserName);
                Assert.AreEqual(commandLine[c], r[c].CommandLine);
                bool isSameUser = pids[c] == 2389;
                Assert.AreEqual(isSameUser, r[c].IsSameUser);
            }
        }

        [TestMethod]
        public void PSOutputParser_SmallCol()
        {
            const string username = "greggm";
            // made up output for what could happen if the fields were all just 1 character in size
            const string input =
                "A B C\n" +
                "9 r /sbin/init";

            List<Process> r = PSOutputParser.Parse(input, username);
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual<uint>(9, r[0].Id);
            Assert.AreEqual("r", r[0].UserName);
            Assert.AreEqual("/sbin/init", r[0].CommandLine);
        }

        [TestMethod]
        public void PSOutputParser_NoUserName()
        {
            // Made up ps output from a system where $USER wasn't a thing
            const string username = "";
            const string input =
                "    A B        C\n" +
                "    1 root     /sbin/init\n" +
                "  720          dbus-daemon --system --fork\n" +
                " 2389 greggm   -bash\n";

            List<Process> r = PSOutputParser.Parse(input, username);
            Assert.AreEqual(3, r.Count);

            uint[] pids = { 1, 720, 2389 };
            string[] userNames = { "root", "", "greggm" };
            string[] commandLine = { "/sbin/init", "dbus-daemon --system --fork", "-bash" };
            for (int c = 0; c < r.Count; c++)
            {
                Assert.AreEqual(pids[c], r[c].Id);
                Assert.AreEqual(userNames[c], r[c].UserName);
                Assert.AreEqual(commandLine[c], r[c].CommandLine);
                bool isSameUser = pids[c] != 1;
                Assert.AreEqual(isSameUser, r[c].IsSameUser);
            }
        }
    }
}
