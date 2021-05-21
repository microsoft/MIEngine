// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using DebuggerTesting.Utilities;

namespace DebuggerTesting.Settings
{
    public static class PathSettings
    {
        private static string tempPath;
        public static string TempPath
        {
            get
            {
                if (PathSettings.tempPath == null)
                {
                    PathSettings.tempPath = Path.GetTempPath();
                }
                return PathSettings.tempPath;
            }
        }

        private static string rootPath;
        private static string RootPath
        {
            get
            {
                if (PathSettings.rootPath == null)
                {
                    // Allow root path to be overridden for dev environments
                    PathSettings.rootPath = Environment.GetEnvironmentVariable("TEST_ROOT");
#if !CORECLR
                    if (String.IsNullOrEmpty(PathSettings.rootPath))
                    {
                        // If not overridden, the root path is one level up from the path containing the test binaries
                        string thisAssemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetPath());
                        PathSettings.rootPath = Path.GetDirectoryName(thisAssemblyFolder);
                    }
#endif
                }
                return PathSettings.rootPath;
            }
        }

        private static string testsPath;
        public static string TestsPath
        {
            get
            {
                if (PathSettings.testsPath == null)
                {
                    PathSettings.testsPath = Path.Combine(PathSettings.RootPath, "tests");

#if !CORECLR
                    // If the normal debuggees path doesn't exist, try an alternative one.
                    // This may occur if running the test from VS.
                    if (!Directory.Exists(PathSettings.testsPath))
                    {
                        string thisAssemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetPath());
                        if (Directory.Exists(thisAssemblyFolder))
                        {
                            PathSettings.testsPath = thisAssemblyFolder;
                        }
                    }
#endif
                }
                return PathSettings.testsPath;
            }
        }

        private static string debuggeesPath;
        public static string DebuggeesPath
        {
            get
            {
                if (PathSettings.debuggeesPath == null)
                {
                    PathSettings.debuggeesPath = Path.Combine(PathSettings.TestsPath, "debuggees");
                }
                return PathSettings.debuggeesPath;
            }
        }

        private static string debugAdaptersPath;
        public static string DebugAdaptersPath
        {
            get
            {
                if (PathSettings.debugAdaptersPath == null)
                {
                    PathSettings.debugAdaptersPath = Path.Combine(PathSettings.RootPath, "extension", "debugAdapters");
                }
                return PathSettings.debugAdaptersPath;
            }
        }

        private static string testConfigurationFilePath;
        public static string TestConfigurationFilePath
        {
            get
            {
                if (PathSettings.testConfigurationFilePath == null)
                {
                    PathSettings.testConfigurationFilePath = Path.Combine(PathSettings.TestsPath, "config.xml");

                    // If the normal config file path doesn't exist, try an alternative one.
                    // This may occur if running the test from VS.
                    if (!File.Exists(PathSettings.testConfigurationFilePath))
                    {
                        string alternatePath = Path.Combine(PathSettings.RootPath, "config.xml");
                        if (File.Exists(alternatePath))
                        {
                            PathSettings.testConfigurationFilePath = alternatePath;
                        }
                    }

                }
                return PathSettings.testConfigurationFilePath;
            }
        }

        public static string GetDebugPathString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Temp      =");
            sb.AppendLine(PathSettings.TempPath);
            sb.Append("Adapters  =");
            sb.AppendLine(PathSettings.DebugAdaptersPath);
            sb.Append("Tests     =");
            sb.AppendLine(PathSettings.TestsPath);
            sb.Append("Debuggees =");
            sb.AppendLine(PathSettings.DebuggeesPath);
            sb.Append("Config    =");
            sb.AppendLine(PathSettings.TestConfigurationFilePath);
            return sb.ToString();
        }
    }
}
