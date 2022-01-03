﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security;
using Microsoft.SSHDebugPS;
using Microsoft.SSHDebugPS.Utilities;
using Xunit;

namespace SSHDebugTests
{
    public class SSHConnectionStringTests
    {
        internal struct ConnectionStringTestItem
        {
            internal string rawConnectionString;
            internal string expectedUsername;
            internal string expectedPassword;
            internal string expectedHostname;
            internal int expectedPort;
        }

        [Fact]
        public void IPv6ConnectionStrings()
        {
            List<ConnectionStringTestItem> ipv6TestStrings = new List<ConnectionStringTestItem>();
            ipv6TestStrings.Add(
                new ConnectionStringTestItem()
                {
                    // valid
                    rawConnectionString = "testuser@[1:2:3:4:5:6:7:8]:24",
                    expectedUsername = "testuser",
                    expectedPassword = null,
                    expectedHostname = "[1:2:3:4:5:6:7:8]",
                    expectedPort = 24
                });
            ipv6TestStrings.Add(
                new ConnectionStringTestItem()
                {
                    // valid with no port
                    rawConnectionString = "testuser@[1:2:3:4:5:6:7:8]",
                    expectedUsername = "testuser",
                    expectedPassword = null,
                    expectedHostname = "[1:2:3:4:5:6:7:8]",
                    expectedPort = ConnectionManager.DefaultSSHPort
                });
            ipv6TestStrings.Add(
                new ConnectionStringTestItem()
                {
                    // valid with username:password
                    rawConnectionString = "test:user@[1234::6:7:8]",
                    expectedUsername = "test",
                    expectedPassword = "user",
                    expectedHostname = "[1234::6:7:8]",
                    expectedPort = ConnectionManager.DefaultSSHPort
                });
            ipv6TestStrings.Add(
                new ConnectionStringTestItem()
                {
                    // Valid with large port
                    rawConnectionString = "[1:2:3:4:5:6:7:8]:12345",
                    expectedUsername = StringResources.UserName_PlaceHolder,
                    expectedPassword = null,
                    expectedHostname = "[1:2:3:4:5:6:7:8]",
                    expectedPort = 12345
                });
            ipv6TestStrings.Add(
                new ConnectionStringTestItem()
                {
                    // Invalid format
                    rawConnectionString = "testuser@:8",
                    expectedUsername = StringResources.UserName_PlaceHolder,
                    expectedPassword = null,
                    expectedHostname = StringResources.HostName_PlaceHolder,
                    expectedPort = ConnectionManager.DefaultSSHPort
                });
            ipv6TestStrings.Add(
                new ConnectionStringTestItem()
                {
                    // Invalid string (just port)
                    rawConnectionString = ":8",
                    expectedUsername = StringResources.UserName_PlaceHolder,
                    expectedPassword = null,
                    expectedHostname = StringResources.HostName_PlaceHolder,
                    expectedPort = ConnectionManager.DefaultSSHPort
                });
            ipv6TestStrings.Add(
                new ConnectionStringTestItem()
                {
                    // Empty String
                    rawConnectionString = string.Empty,
                    expectedUsername = StringResources.UserName_PlaceHolder,
                    expectedPassword = null,
                    expectedHostname = StringResources.HostName_PlaceHolder,
                    expectedPort = ConnectionManager.DefaultSSHPort
                });
            ipv6TestStrings.Add(
                new ConnectionStringTestItem()
                {
                    // Invalid port
                    rawConnectionString = "[1:2:3:4:5:6:7:8]:123456",
                    expectedUsername = StringResources.UserName_PlaceHolder,
                    expectedPassword = null,
                    expectedHostname = StringResources.HostName_PlaceHolder,
                    expectedPort = ConnectionManager.DefaultSSHPort
                });

            foreach (var connection in ipv6TestStrings)
            {
                ParseConnectionAndValidate(connection);
            }
        }

        [Fact]
        public void Ipv4ConnectionStrings()
        {
            List<ConnectionStringTestItem> ipv4TestStrings = new List<ConnectionStringTestItem>();
            ipv4TestStrings.Add(
                new ConnectionStringTestItem()
                {
                    // valid no username
                    rawConnectionString = "192.168.1.1:156",
                    expectedUsername = StringResources.UserName_PlaceHolder,
                    expectedPassword = null,
                    expectedHostname = "192.168.1.1",
                    expectedPort = 156
                });
            ipv4TestStrings.Add(
                new ConnectionStringTestItem()
                {
                    // valid username with port
                    rawConnectionString = "customUser@192.168.1.1:65354",
                    expectedUsername = "customUser",
                    expectedPassword = null,
                    expectedHostname = "192.168.1.1",
                    expectedPort = 65354
                });
            ipv4TestStrings.Add(
                new ConnectionStringTestItem()
                {
                    // valid no username, Large port
                    rawConnectionString = "192.168.1.1:" + (ushort.MaxValue).ToString("d", CultureInfo.InvariantCulture),
                    expectedUsername = StringResources.UserName_PlaceHolder,
                    expectedPassword = null,
                    expectedHostname = "192.168.1.1",
                    expectedPort = ushort.MaxValue
                });
            ipv4TestStrings.Add(
                new ConnectionStringTestItem()
                {
                    // valid username no port
                    rawConnectionString = "user@10.10.10.10",
                    expectedUsername = "user",
                    expectedPassword = null,
                    expectedHostname = "10.10.10.10",
                    expectedPort = ConnectionManager.DefaultSSHPort
                });
            ipv4TestStrings.Add(
                 new ConnectionStringTestItem()
                 {
                     // valid username no port with password
                     rawConnectionString = "user:pass@10.10.10.10",
                     expectedUsername = "user",
                     expectedPassword = "pass",
                     expectedHostname = "10.10.10.10",
                     expectedPort = ConnectionManager.DefaultSSHPort
                 });
            ipv4TestStrings.Add(
                new ConnectionStringTestItem()
                {
                    // Invalid port
                    rawConnectionString = "192.168.1.1:123456",
                    expectedUsername = StringResources.UserName_PlaceHolder,
                    expectedPassword = null,
                    expectedHostname = StringResources.HostName_PlaceHolder,
                    expectedPort = ConnectionManager.DefaultSSHPort
                });
            ipv4TestStrings.Add(
                new ConnectionStringTestItem()
                {
                    // Invalid address
                    rawConnectionString = "1%92.168.1.1:23",
                    expectedUsername = StringResources.UserName_PlaceHolder,
                    expectedPassword = null,
                    expectedHostname = StringResources.HostName_PlaceHolder,
                    expectedPort = ConnectionManager.DefaultSSHPort
                });

            foreach (var connection in ipv4TestStrings)
            {
                ParseConnectionAndValidate(connection);
            }
        }

        private const string _comparisonErrorStringFormat = "{0} - Expected:'{1}' Actual:'{2}'";
        private void ParseConnectionAndValidate(ConnectionStringTestItem item)
        {
            string username;
            string hostname;
            int port;
            ConnectionManager.ParseSSHConnectionString(item.rawConnectionString, out username, out SecureString password, out hostname, out port);

            Assert.True(item.expectedUsername.Equals(username, StringComparison.Ordinal), _comparisonErrorStringFormat.FormatInvariantWithArgs("UserName", item.expectedUsername, username));
            Assert.True(item.expectedHostname.Equals(hostname, StringComparison.Ordinal), _comparisonErrorStringFormat.FormatInvariantWithArgs("Hostname", item.expectedHostname, hostname));
            Assert.True(item.expectedPort == port, _comparisonErrorStringFormat.FormatInvariantWithArgs("Port", item.expectedPort, port));
            if (item.expectedPassword == null)
            {
                Assert.True(password == null);
            }
            else
            {
                string passwordString = StringFromSecureString(password);
                Assert.True(item.expectedPassword.Equals(passwordString, StringComparison.Ordinal), _comparisonErrorStringFormat.FormatInvariantWithArgs("Password", item.expectedPassword, passwordString)); 
            }
        }

        private static string StringFromSecureString(SecureString secString)
        {
            if (secString == null)
            {
                return null;
            }

            IntPtr bstr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(secString);
            string value = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(bstr);
            System.Runtime.InteropServices.Marshal.FreeBSTR(bstr);
            return value;
        }
    }
}
