using MICore;
using System;

namespace BlackBerryDebugLauncher
{
    internal sealed class BlackBerryLaunchOptions
    {
        public BlackBerryLaunchOptions(string exePath, MICore.Xml.LaunchOptions.BlackBerryLaunchOptions xmlOptions)
        {
            if (string.IsNullOrEmpty(exePath))
                throw new ArgumentNullException("exePath");
            if (xmlOptions == null)
                throw new ArgumentNullException("xmlOptions");

            GdbPath = LaunchOptions.RequireAttribute(xmlOptions.GdbPath, "GdbPath");
            GdbHostPath = LaunchOptions.RequireAttribute(xmlOptions.GdbHostPath, "GdbHostPath");
            PID = xmlOptions.PID;
            ExePath = exePath;
            TargetAddress = LaunchOptions.RequireAttribute(xmlOptions.TargetAddress, "TargetAddress");
            TargetPort = xmlOptions.TargetPort;
            IsAttach = xmlOptions.Attach;
            AdditionalSOLibSearchPath = xmlOptions.AdditionalSOLibSearchPath;
            TargetArchitecture = LaunchOptions.ConvertTargetArchitectureAttribute(xmlOptions.TargetArchitecture);
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

        public TargetArchitecture TargetArchitecture
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
