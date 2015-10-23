using MICore;
using System;
using System.Collections.Generic;
using MICore.Xml.LaunchOptions;
using System.IO;

namespace BlackBerryDebugLauncher
{
    /// <summary>
    /// Launch options dedicated to BlackBerry devices.
    /// </summary>
    internal sealed class BlackBerryLaunchOptions
    {
        public BlackBerryLaunchOptions(string exePath, MICore.Xml.LaunchOptions.BlackBerryLaunchOptions xmlOptions, TargetEngine targetEngine)
        {
            if (string.IsNullOrEmpty(exePath))
                throw new ArgumentNullException("exePath");
            if (xmlOptions == null)
                throw new ArgumentNullException("xmlOptions");

            GdbPath = LaunchOptions.RequireAttribute(xmlOptions.GdbPath, "GdbPath");
            GdbHostPath = LaunchOptions.RequireAttribute(xmlOptions.GdbHostPath, "GdbHostPath");
            PID = LaunchOptions.RequirePositiveAttribute(xmlOptions.PID, "PID");
            ExePath = exePath;
            TargetAddress = LaunchOptions.RequireAttribute(xmlOptions.TargetAddress, "TargetAddress");
            TargetPort = xmlOptions.TargetPort;
            TargetType = GetTargetType(xmlOptions.TargetType);
            IsAttach = xmlOptions.Attach;
            AdditionalSOLibSearchPath = Combine(";", xmlOptions.AdditionalSOLibSearchPath, GetDefaultSearchPaths(xmlOptions.NdkHostPath, xmlOptions.NdkTargetPath, TargetType));
            TargetArchitecture = LaunchOptions.ConvertTargetArchitectureAttribute(xmlOptions.TargetArchitecture);
        }

        private static string Combine(string separator, string additionalSoLibSearchPath, IReadOnlyCollection<string> defaultSoLibSearchPath)
        {
            if (string.IsNullOrEmpty(separator))
                throw new ArgumentNullException("separator");

            // if there are no specified .so search paths:
            if (string.IsNullOrEmpty(additionalSoLibSearchPath))
            {
                if (defaultSoLibSearchPath == null || defaultSoLibSearchPath.Count == 0)
                    return null;

                // return only joined default paths based on NDK:
                return string.Join(separator, defaultSoLibSearchPath);
            }

            if (defaultSoLibSearchPath == null || defaultSoLibSearchPath.Count == 0)
                return additionalSoLibSearchPath;

            // or concat everything:
            return string.Concat(additionalSoLibSearchPath, separator, string.Join(separator, defaultSoLibSearchPath));
        }

        private static string GetArchitectureFolderName(TargetType type)
        {
            switch (type)
            {
                case TargetType.Phone: // pass though
                case TargetType.Tablet:
                    return "armle-v7";
                case TargetType.Simulator:
                    return "x86";
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        private IReadOnlyCollection<string> GetDefaultSearchPaths(string ndkHostPath, string ndkTargetPath, TargetType type)
        {
            var archFolder = GetArchitectureFolderName(type);

            if (string.IsNullOrEmpty(archFolder) || string.IsNullOrEmpty(ndkTargetPath))
                return null;

            // default .so folders, that belong to the NDK:
            return new [] { Path.Combine(ndkTargetPath, archFolder, "lib"), Path.Combine(ndkTargetPath, archFolder, "usr", "lib") };
        }

        private static TargetType GetTargetType(BlackBerryLaunchOptionsTargetType type)
        {
            switch (type)
            {
                case BlackBerryLaunchOptionsTargetType.Phone:
                    return TargetType.Phone;
                case BlackBerryLaunchOptionsTargetType.Tablet:
                    return TargetType.Tablet;
                case BlackBerryLaunchOptionsTargetType.Simulator:
                    return TargetType.Simulator;
                default:
                    throw new ArgumentOutOfRangeException("type", "Value \"" + type + "\" is out of scope");
            }
        }

        #region Properties

        public string GdbPath
        {
            get;
            private set;
        }

        public string GdbHostPath
        {
            get;
            private set;
        }

        public string TargetAddress
        {
            get;
            private set;
        }

        public uint TargetPort
        {
            get;
            private set;
        }

        public MICore.TargetArchitecture TargetArchitecture
        {
            get;
            private set;
        }

        public TargetType TargetType
        {
            get;
            private set;
        }

        public uint PID
        {
            get;
            private set;
        }

        public string ExePath
        {
            get;
            private set;
        }

        public bool IsAttach
        {
            get;
            private set;
        }

        public string AdditionalSOLibSearchPath
        {
            get;
            private set;
        }

        #endregion
    }
}
