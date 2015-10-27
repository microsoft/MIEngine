﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Ten kod został wygenerowany przez narzędzie.
//     Wersja wykonawcza:4.0.30319.42000
//
//     Zmiany w tym pliku mogą spowodować nieprawidłowe zachowanie i zostaną utracone, jeśli
//     kod zostanie ponownie wygenerowany.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by xsd, Version=4.6.81.0.
// 
namespace MICore.Xml.LaunchOptions {
    using System.Xml.Serialization;
    
    
    /// <uwagi/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.81.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014", IsNullable=false)]
    public partial class AndroidLaunchOptions {
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Package;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string LaunchActivity;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string SDKRoot;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string NDKRoot;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string IntermediateDirectory;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string DeviceId;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string LogcatServiceId;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool Attach;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute("")]
        public string SourceRoots;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(65534)]
        public int JVMPort;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute("localhost")]
        public string JVMHost;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public TargetArchitecture TargetArchitecture;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string AdditionalSOLibSearchPath;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public MIMode MIMode;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool MIModeSpecified;
        
        public AndroidLaunchOptions() {
            this.Attach = false;
            this.SourceRoots = "";
            this.JVMPort = 65534;
            this.JVMHost = "localhost";
        }
    }
    
    /// <uwagi/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.81.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014")]
    public enum TargetArchitecture {
        
        /// <uwagi/>
        x86,
        
        /// <uwagi/>
        arm,
        
        /// <uwagi/>
        arm64,
        
        /// <uwagi/>
        mips,
        
        /// <uwagi/>
        x64,
        
        /// <uwagi/>
        amd64,
        
        /// <uwagi/>
        x86_64,
        
        /// <uwagi/>
        X86,
        
        /// <uwagi/>
        ARM,
        
        /// <uwagi/>
        ARM64,
        
        /// <uwagi/>
        MIPS,
        
        /// <uwagi/>
        X64,
        
        /// <uwagi/>
        AMD64,
        
        /// <uwagi/>
        X86_64,
    }
    
    /// <uwagi/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.81.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014")]
    public enum MIMode {
        
        /// <uwagi/>
        gdb,
        
        /// <uwagi/>
        lldb,
        
        /// <uwagi/>
        clrdbg,
    }
    
    /// <uwagi/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.81.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014")]
    public partial class Command {
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool IgnoreFailures;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool IgnoreFailuresSpecified;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Description;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value;
    }
    
    /// <uwagi/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.81.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014")]
    public partial class BaseLaunchOptions {
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlArrayItemAttribute(IsNullable=false)]
        public Command[] SetupCommands;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlArrayItemAttribute(IsNullable=false)]
        public Command[] CustomLaunchSetupCommands;
        
        /// <uwagi/>
        public BaseLaunchOptionsLaunchCompleteCommand LaunchCompleteCommand;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool LaunchCompleteCommandSpecified;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ExePath;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ExeArguments;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string WorkingDirectory;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string VisualizerFile;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public TargetArchitecture TargetArchitecture;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string AdditionalSOLibSearchPath;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public MIMode MIMode;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool MIModeSpecified;
    }
    
    /// <uwagi/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.81.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014")]
    public enum BaseLaunchOptionsLaunchCompleteCommand {
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlEnumAttribute("exec-run")]
        execrun,
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlEnumAttribute("exec-continue")]
        execcontinue,
        
        /// <uwagi/>
        None,
    }
    
    /// <uwagi/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.81.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014", IsNullable=false)]
    public partial class IOSLaunchOptions {
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string RemoteMachineName;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string PackageId;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int vcremotePort;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public IOSLaunchOptionsIOSDebugTarget IOSDebugTarget;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string DeviceUdid;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public IOSLaunchOptionsSecure Secure;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public TargetArchitecture TargetArchitecture;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string AdditionalSOLibSearchPath;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public MIMode MIMode;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool MIModeSpecified;
    }
    
    /// <uwagi/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.81.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014")]
    public enum IOSLaunchOptionsIOSDebugTarget {
        
        /// <uwagi/>
        Device,
        
        /// <uwagi/>
        Simulator,
    }
    
    /// <uwagi/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.81.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014")]
    public enum IOSLaunchOptionsSecure {
        
        /// <uwagi/>
        True,
        
        /// <uwagi/>
        False,
    }
    
    /// <uwagi/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.81.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014", IsNullable=false)]
    public partial class BlackBerryLaunchOptions {
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string GdbPath;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string GdbHostPath;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string TargetAddress;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(typeof(uint), "8000")]
        public uint TargetPort;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public BlackBerryLaunchOptionsTargetType TargetType;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool Attach;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint PID;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string NdkHostPath;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string NdkTargetPath;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public TargetArchitecture TargetArchitecture;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string AdditionalSOLibSearchPath;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public MIMode MIMode;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool MIModeSpecified;
        
        public BlackBerryLaunchOptions() {
            this.TargetPort = ((uint)(8000));
            this.Attach = false;
        }
    }
    
    /// <uwagi/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.81.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014")]
    public enum BlackBerryLaunchOptionsTargetType {
        
        /// <uwagi/>
        Phone,
        
        /// <uwagi/>
        Tablet,
        
        /// <uwagi/>
        Simulator,
    }
    
    /// <uwagi/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.81.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014", IsNullable=false)]
    public partial class LocalLaunchOptions : BaseLaunchOptions {
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string MIDebuggerPath;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string MIDebuggerServerAddress;
    }
    
    /// <uwagi/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.81.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014", IsNullable=false)]
    public partial class SerialPortLaunchOptions : BaseLaunchOptions {
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Port;
    }
    
    /// <uwagi/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.81.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014", IsNullable=false)]
    public partial class PipeLaunchOptions : BaseLaunchOptions {
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string PipePath;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string PipeArguments;
    }
    
    /// <uwagi/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.81.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014", IsNullable=false)]
    public partial class TcpLaunchOptions : BaseLaunchOptions {
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Hostname;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int Port;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool Secure;
        
        /// <uwagi/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool SecureSpecified;
    }
}
