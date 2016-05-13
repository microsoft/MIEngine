// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace AndroidDebugLauncher
{
    public class NDKPrebuiltFilePath: INDKFilePath
    {
        public string PartialFilePath { get; }

        private NDKPrebuiltFilePath(string partialFilePath)
        {
            this.PartialFilePath = partialFilePath;
        }

        public static NDKPrebuiltFilePath[] GDBPaths()
        {
            return new NDKPrebuiltFilePath[] {
                new NDKPrebuiltFilePath(@"windows\bin\gdb.exe"), // windows-x86 NDK path
                new NDKPrebuiltFilePath(@"windows-x86_64\bin\gdb.exe"), // windows-x86 NDK path
            };
        }

        public string TryResolve(string ndkRoot)
        {
            string prebuiltDirectory = GetPrebuiltDirectory(ndkRoot);
            string path = Path.Combine(prebuiltDirectory, PartialFilePath);
            if (File.Exists(path))
            {
                return path;
            }

            return null;
        }

        public string GetSearchPathDescription(string ndkRoot)
        {
            return GetPrebuiltDirectory(ndkRoot);
        }

        private static string GetPrebuiltDirectory(string ndkRoot)
        {
            return Path.Combine(ndkRoot, "prebuilt");
        }

        public static NDKPrebuiltFilePath[] GDBServerPaths(string targetArchitecture)
        {
            string gdbServerPartialPath = Path.Combine(
                String.Concat("android-", targetArchitecture),
                "gdbserver", "gdbserver");

            return new NDKPrebuiltFilePath[] {
                new NDKPrebuiltFilePath(gdbServerPartialPath)
            };
        }
    }
}
