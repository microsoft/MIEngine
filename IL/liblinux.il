.assembly extern System
{
	.publickeytoken = (B7 7A 5C 56 19 34 E0 89)
	.ver 4:0:0:0
}
.assembly extern System.Core
{
	.publickeytoken = (B7 7A 5C 56 19 34 E0 89)
	.ver 4:0:0:0
}
.assembly extern System.Xml.Linq
{
	.publickeytoken = (B7 7A 5C 56 19 34 E0 89)
	.ver 4:0:0:0
}
.assembly extern mscorlib
{
	.publickeytoken = (B7 7A 5C 56 19 34 E0 89)
	.ver 4:0:0:0
}
.assembly liblinux
{
	.custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = { bool(true) }
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = { }
	.custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = { string('liblinux.IO, PublicKey=002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293') }
	.custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = { string('liblinux.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293') }
	.custom instance void [mscorlib]System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool) = { bool(false) }
	.publickey = (00 24 00 00 04 80 00 00 94 00 00 00 06 02 00 00 00 24 00 00 52 53 41 31 00 04 00 00 01 00 01 00 07 D1 FA 57 C4 AE D9 F0 A3 2E 84 AA 0F AE FD 0D E9 E8 FD 6A EC 8F 87 FB 03 76 6C 83 4C 99 92 1E B2 3B E7 9A D9 D5 DC C1 DD 9A D2 36 13 21 02 90 0B 72 3C F9 80 95 7F C4 E1 77 10 8F C6 07 77 4F 29 E8 32 0E 92 EA 05 EC E4 E8 21 C0 A5 EF E8 F1 64 5C 4C 0C 93 C1 AB 99 28 5D 62 2C AA 65 2C 1D FA D6 3D 74 5D 6F 2D E5 F1 7E 5E AF 0F C4 96 3D 26 1C 8A 12 43 65 18 20 6D C0 93 34 4D 5A D2 93)
	.hash algorithm 0x00008004
	.ver 16:0:0:0
}
.namespace liblinux
{
	.class public sealed AuthenticationMode
		extends [mscorlib]System.Enum
	{
		.field public rtspecialname specialname int32 value__
		.field static public literal valuetype liblinux.AuthenticationMode Password = int32(0x00000000)
		.field static public literal valuetype liblinux.AuthenticationMode PrivateKey = int32(0x00000001)
	}
	.class public abstract ConnectionInfo
		extends [mscorlib]System.Object
		implements liblinux.IConnectionInfo
	{
		.method public hidebysig specialname 
			instance string get_HostNameOrAddress()
		{
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_HostNameOrAddress(string 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance int32 get_Port()
		{
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_Port(int32 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance valuetype [mscorlib]System.TimeSpan get_Timeout()
		{
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_Timeout(valuetype [mscorlib]System.TimeSpan 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance string get_UserName()
		{
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_UserName(string 'value')
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance string get_Name()
		{
			ret
		}
		.method famorassem virtual hidebysig newslot abstract specialname 
			instance valuetype liblinux.AuthenticationMode get_AuthenticationMode()
		{
		}
		.method public virtual hidebysig 
			instance bool Equals(object obj)
		{
			ret
		}
		.method public virtual hidebysig 
			instance int32 GetHashCode()
		{
			ret
		}
		.method public virtual hidebysig 
			instance string ToString()
		{
			ret
		}
		.method public hidebysig 
			instance int32 ToInt()
		{
			ret
		}
		.method famorassem virtual hidebysig newslot 
			instance void OnAfterConnect(bool succeeded)
		{
			ret
		}
		.method family hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.property instance valuetype liblinux.AuthenticationMode AuthenticationMode()
		{
			.get instance valuetype liblinux.AuthenticationMode liblinux.ConnectionInfo::get_AuthenticationMode()
		}
		.property instance string HostNameOrAddress()
		{
			.get instance string liblinux.ConnectionInfo::get_HostNameOrAddress()
			.set instance void liblinux.ConnectionInfo::set_HostNameOrAddress(string)
		}
		.property instance string Name()
		{
			.get instance string liblinux.ConnectionInfo::get_Name()
		}
		.property instance int32 Port()
		{
			.get instance int32 liblinux.ConnectionInfo::get_Port()
			.set instance void liblinux.ConnectionInfo::set_Port(int32)
		}
		.property instance valuetype [mscorlib]System.TimeSpan Timeout()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.ConnectionInfo::get_Timeout()
			.set instance void liblinux.ConnectionInfo::set_Timeout(valuetype [mscorlib]System.TimeSpan)
		}
		.property instance string UserName()
		{
			.get instance string liblinux.ConnectionInfo::get_UserName()
			.set instance void liblinux.ConnectionInfo::set_UserName(string)
		}
	}
	.class public ErrorOccuredEventArgs
		extends [mscorlib]System.EventArgs
	{
		.method public hidebysig specialname 
			instance class [mscorlib]System.Exception get_Exception()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class [mscorlib]System.Exception exception)
		{
			ret
		}
		.property instance class [mscorlib]System.Exception Exception()
		{
			.get instance class [mscorlib]System.Exception liblinux.ErrorOccuredEventArgs::get_Exception()
		}
	}
	.class public ExceptionBase
		extends [mscorlib]System.Exception
	{
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class [mscorlib]System.Exception innerException)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message, class [mscorlib]System.Exception innerException)
		{
			ret
		}
	}
	.class public interface abstract IConnectionInfo
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance string get_Name()
		{
		}
		.property instance string Name()
		{
			.get instance string liblinux.IConnectionInfo::get_Name()
		}
	}
	.class public interface abstract IRemoteSystem
		implements liblinux.IRemoteSystemBare, [mscorlib]System.IDisposable
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance class 'liblinux.IO.IRemoteFileSystem' get_FileSystem()
		{
		}
		.property instance class 'liblinux.IO.IRemoteFileSystem' FileSystem()
		{
			.get instance class 'liblinux.IO.IRemoteFileSystem' liblinux.IRemoteSystem::get_FileSystem()
		}
	}
	.class public interface abstract IRemoteSystemBare
		implements [mscorlib]System.IDisposable
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance void add_Disconnected(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void remove_Disconnected(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void add_ErrorOccured(class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void remove_ErrorOccured(class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_IsConnected()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance class liblinux.ConnectionInfo get_ConnectionInfo()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance class liblinux.Shell.ICommandingShell get_Shell()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(class liblinux.ConnectionInfo connectionInfo)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, string userName, class [mscorlib]System.Security.SecureString password)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, int32 port, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, int32 port, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Disconnect()
		{
		}
		.event [mscorlib]System.EventHandler Disconnected
		{
			.addon instance void liblinux.IRemoteSystemBare::add_Disconnected(class [mscorlib]System.EventHandler)
			.removeon instance void liblinux.IRemoteSystemBare::remove_Disconnected(class [mscorlib]System.EventHandler)
		}
		.event class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs> ErrorOccured
		{
			.addon instance void liblinux.IRemoteSystemBare::add_ErrorOccured(class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs>)
			.removeon instance void liblinux.IRemoteSystemBare::remove_ErrorOccured(class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs>)
		}
		.property instance class liblinux.ConnectionInfo ConnectionInfo()
		{
			.get instance class liblinux.ConnectionInfo liblinux.IRemoteSystemBare::get_ConnectionInfo()
		}
		.property instance bool IsConnected()
		{
			.get instance bool liblinux.IRemoteSystemBare::get_IsConnected()
		}
		.property instance class liblinux.Shell.ICommandingShell Shell()
		{
			.get instance class liblinux.Shell.ICommandingShell liblinux.IRemoteSystemBare::get_Shell()
		}
	}
	.class public LinuxSystem
		extends liblinux.UnixSystem
	{
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.ConnectionInfo connectionInfo)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
	}
	.class public sealed ModelId
		extends [mscorlib]System.Enum
	{
		.field public rtspecialname specialname int32 value__
		.field static public literal valuetype liblinux.ModelId Pi1ModelA = int32(0x00000003)
			.custom instance void [System]System.ComponentModel.DescriptionAttribute::.ctor(string) = { string('Raspberry Pi 1 Model A') }
		.field static public literal valuetype liblinux.ModelId Pi1ModelAPlus = int32(0x00000005)
			.custom instance void [System]System.ComponentModel.DescriptionAttribute::.ctor(string) = { string('Raspberry Pi 1 Model A+') }
		.field static public literal valuetype liblinux.ModelId Pi1ModelB = int32(0x00000001)
			.custom instance void [System]System.ComponentModel.DescriptionAttribute::.ctor(string) = { string('Raspberry Pi 1 Model B') }
		.field static public literal valuetype liblinux.ModelId Pi1ModelBPlus = int32(0x00000004)
			.custom instance void [System]System.ComponentModel.DescriptionAttribute::.ctor(string) = { string('Raspberry Pi 1 Model B+') }
		.field static public literal valuetype liblinux.ModelId Pi1ModelBRev2 = int32(0x00000002)
			.custom instance void [System]System.ComponentModel.DescriptionAttribute::.ctor(string) = { string('Raspberry Pi 1 Model B Rev. 2.0') }
		.field static public literal valuetype liblinux.ModelId Pi2ModelB = int32(0x00000007)
			.custom instance void [System]System.ComponentModel.DescriptionAttribute::.ctor(string) = { string('Raspberry Pi 2 Model B') }
		.field static public literal valuetype liblinux.ModelId Pi3ModelB = int32(0x00000009)
			.custom instance void [System]System.ComponentModel.DescriptionAttribute::.ctor(string) = { string('Raspberry Pi 3 Model B') }
		.field static public literal valuetype liblinux.ModelId PiComputeModule = int32(0x00000006)
			.custom instance void [System]System.ComponentModel.DescriptionAttribute::.ctor(string) = { string('Raspberry Pi Compute Module') }
		.field static public literal valuetype liblinux.ModelId PiZero = int32(0x00000008)
			.custom instance void [System]System.ComponentModel.DescriptionAttribute::.ctor(string) = { string('Raspberry Pi Zero') }
		.field static public literal valuetype liblinux.ModelId Unknown = int32(0x00000000)
			.custom instance void [System]System.ComponentModel.DescriptionAttribute::.ctor(string) = { string('unknown') }
	}
	.class public PasswordConnectionInfo
		extends liblinux.ConnectionInfo
	{
		.method public hidebysig specialname 
			instance class [mscorlib]System.Security.SecureString get_Password()
		{
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_Password(class [mscorlib]System.Security.SecureString 'value')
		{
			ret
		}
		.method famorassem virtual hidebysig specialname 
			instance valuetype liblinux.AuthenticationMode get_AuthenticationMode()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.property instance valuetype liblinux.AuthenticationMode AuthenticationMode()
		{
			.get instance valuetype liblinux.AuthenticationMode liblinux.PasswordConnectionInfo::get_AuthenticationMode()
		}
		.property instance class [mscorlib]System.Security.SecureString Password()
		{
			.get instance class [mscorlib]System.Security.SecureString liblinux.PasswordConnectionInfo::get_Password()
			.set instance void liblinux.PasswordConnectionInfo::set_Password(class [mscorlib]System.Security.SecureString)
		}
	}
	.class public sealed Pi
		extends liblinux.LinuxSystem
	{
		.method public hidebysig specialname 
			instance valuetype liblinux.ModelId get_ModelId()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.ConnectionInfo connectionInfo)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnConnected()
		{
			ret
		}
		.property instance valuetype liblinux.ModelId ModelId()
		{
			.get instance valuetype liblinux.ModelId liblinux.Pi::get_ModelId()
		}
	}
	.class public PrivateKeyConnectionInfo
		extends liblinux.ConnectionInfo
	{
		.method public hidebysig specialname 
			instance string get_PrivateKeyFileName()
		{
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_PrivateKeyFileName(string 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance class [mscorlib]System.Security.SecureString get_Passphrase()
		{
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_Passphrase(class [mscorlib]System.Security.SecureString 'value')
		{
			ret
		}
		.method famorassem virtual hidebysig specialname 
			instance valuetype liblinux.AuthenticationMode get_AuthenticationMode()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.property instance valuetype liblinux.AuthenticationMode AuthenticationMode()
		{
			.get instance valuetype liblinux.AuthenticationMode liblinux.PrivateKeyConnectionInfo::get_AuthenticationMode()
		}
		.property instance class [mscorlib]System.Security.SecureString Passphrase()
		{
			.get instance class [mscorlib]System.Security.SecureString liblinux.PrivateKeyConnectionInfo::get_Passphrase()
			.set instance void liblinux.PrivateKeyConnectionInfo::set_Passphrase(class [mscorlib]System.Security.SecureString)
		}
		.property instance string PrivateKeyFileName()
		{
			.get instance string liblinux.PrivateKeyConnectionInfo::get_PrivateKeyFileName()
			.set instance void liblinux.PrivateKeyConnectionInfo::set_PrivateKeyFileName(string)
		}
	}
	.class public PrivateKeyException
		extends liblinux.ExceptionBase
	{
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message, class [mscorlib]System.Exception innerException)
		{
			ret
		}
	}
	.class public RemoteAuthenticationException
		extends liblinux.RemoteConnectivityException
	{
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message, class [mscorlib]System.Exception innerException)
		{
			ret
		}
	}
	.class public RemoteConnectionTimeoutException
		extends liblinux.RemoteConnectivityException
	{
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message, class [mscorlib]System.Exception innerException)
		{
			ret
		}
	}
	.class public RemoteConnectivityException
		extends liblinux.ExceptionBase
	{
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message, class [mscorlib]System.Exception innerException)
		{
			ret
		}
	}
	.class public RemoteException
		extends liblinux.ExceptionBase
	{
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message, class [mscorlib]System.Exception innerException)
		{
			ret
		}
	}
	.class public RemoteHostKeyAlgorithmException
		extends liblinux.ExceptionBase
	{
		.method public hidebysig specialname 
			instance void .ctor(class [mscorlib]System.Exception innerException)
		{
			ret
		}
	}
	.class public RemoteSystem
		extends liblinux.RemoteSystemBare
		implements liblinux.IRemoteSystem, liblinux.IRemoteSystemBare, [mscorlib]System.IDisposable
	{
		.method public virtual hidebysig newslot specialname 
			instance class 'liblinux.IO.IRemoteFileSystem' get_FileSystem()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.ConnectionInfo connectionInfo)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public virtual hidebysig 
			instance void Disconnect()
		{
			ret
		}
		.property instance class 'liblinux.IO.IRemoteFileSystem' FileSystem()
		{
			.get instance class 'liblinux.IO.IRemoteFileSystem' liblinux.RemoteSystem::get_FileSystem()
		}
	}
	.class public RemoteSystemBare
		extends liblinux.RemoteSystemBase
		implements liblinux.IRemoteSystemBare, [mscorlib]System.IDisposable
	{
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.ConnectionInfo connectionInfo)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
	}
	.class public abstract RemoteSystemBase
		extends [mscorlib]System.Object
		implements liblinux.IRemoteSystemBare, [mscorlib]System.IDisposable
	{
		.method public final virtual hidebysig newslot specialname 
			instance void add_Disconnected(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void remove_Disconnected(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void add_ErrorOccured(class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void remove_ErrorOccured(class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance bool get_IsConnected()
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance class liblinux.ConnectionInfo get_ConnectionInfo()
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance class liblinux.Shell.ICommandingShell get_Shell()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.ConnectionInfo connectionInfo)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(class liblinux.ConnectionInfo connectionInfo)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 port, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 port, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Disconnect()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Dispose()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void Connect()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void Connect(class liblinux.PasswordConnectionInfo passwordConnectionInfo)
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void Connect(class liblinux.PrivateKeyConnectionInfo privateKeyConnectionInfo)
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnBeforeConnect()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnConnected()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnAfterConnect(bool succeeded, class [mscorlib]System.Exception ex)
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnDisconnected()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnErrorOccured(class [mscorlib]System.Exception ex)
		{
			ret
		}
		.event [mscorlib]System.EventHandler Disconnected
		{
			.addon instance void liblinux.RemoteSystemBase::add_Disconnected(class [mscorlib]System.EventHandler)
			.removeon instance void liblinux.RemoteSystemBase::remove_Disconnected(class [mscorlib]System.EventHandler)
		}
		.event class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs> ErrorOccured
		{
			.addon instance void liblinux.RemoteSystemBase::add_ErrorOccured(class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs>)
			.removeon instance void liblinux.RemoteSystemBase::remove_ErrorOccured(class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs>)
		}
		.property instance class liblinux.ConnectionInfo ConnectionInfo()
		{
			.get instance class liblinux.ConnectionInfo liblinux.RemoteSystemBase::get_ConnectionInfo()
		}
		.property instance bool IsConnected()
		{
			.get instance bool liblinux.RemoteSystemBase::get_IsConnected()
		}
		.property instance class liblinux.Shell.ICommandingShell Shell()
		{
			.get instance class liblinux.Shell.ICommandingShell liblinux.RemoteSystemBase::get_Shell()
		}
	}
	.class public sealed SystemId
		extends [mscorlib]System.Enum
	{
		.field public rtspecialname specialname int32 value__
		.field static public literal valuetype liblinux.SystemId Android = int32(0x00000008)
		.field static public literal valuetype liblinux.SystemId Arch = int32(0x00000004)
		.field static public literal valuetype liblinux.SystemId BSD = int32(0x0000001B)
		.field static public literal valuetype liblinux.SystemId CentOS = int32(0x0000001A)
		.field static public literal valuetype liblinux.SystemId Debian = int32(0x00000003)
		.field static public literal valuetype liblinux.SystemId Fedora = int32(0x00000017)
		.field static public literal valuetype liblinux.SystemId FreeBSD = int32(0x0000001C)
		.field static public literal valuetype liblinux.SystemId Gentoo = int32(0x00000005)
		.field static public literal valuetype liblinux.SystemId Kali = int32(0x0000000F)
		.field static public literal valuetype liblinux.SystemId Knoppix = int32(0x0000000D)
		.field static public literal valuetype liblinux.SystemId Kubuntu = int32(0x00000011)
		.field static public literal valuetype liblinux.SystemId Mandrake = int32(0x00000018)
		.field static public literal valuetype liblinux.SystemId Minix = int32(0x00000020)
		.field static public literal valuetype liblinux.SystemId NetBSD = int32(0x0000001E)
		.field static public literal valuetype liblinux.SystemId OSX = int32(0x0000001D)
		.field static public literal valuetype liblinux.SystemId OpenBSD = int32(0x0000001F)
		.field static public literal valuetype liblinux.SystemId OpenSUSE = int32(0x00000014)
		.field static public literal valuetype liblinux.SystemId OracleLinux = int32(0x00000024)
		.field static public literal valuetype liblinux.SystemId PCLinuxOS = int32(0x00000019)
		.field static public literal valuetype liblinux.SystemId Poky = int32(0x00000022)
		.field static public literal valuetype liblinux.SystemId Puppy = int32(0x00000009)
		.field static public literal valuetype liblinux.SystemId Raspbian = int32(0x00000012)
		.field static public literal valuetype liblinux.SystemId RedHat = int32(0x00000007)
		.field static public literal valuetype liblinux.SystemId RedHatEnterprise = int32(0x00000016)
		.field static public literal valuetype liblinux.SystemId 'SUSE' = int32(0x00000013)
		.field static public literal valuetype liblinux.SystemId Slackware = int32(0x00000006)
		.field static public literal valuetype liblinux.SystemId Slax = int32(0x00000015)
		.field static public literal valuetype liblinux.SystemId Sorcerer = int32(0x0000000A)
		.field static public literal valuetype liblinux.SystemId Ubuntu = int32(0x0000000E)
		.field static public literal valuetype liblinux.SystemId UbuntuServer = int32(0x00000010)
		.field static public literal valuetype liblinux.SystemId Unix = int32(0x00000002)
		.field static public literal valuetype liblinux.SystemId Unknown = int32(0x00000001)
		.field static public literal valuetype liblinux.SystemId WindowsSubsystemForLinux = int32(0x00000023)
		.field static public literal valuetype liblinux.SystemId Yocto = int32(0x00000021)
		.field static public literal valuetype liblinux.SystemId ddWRT = int32(0x0000000C)
		.field static public literal valuetype liblinux.SystemId openWRT = int32(0x0000000B)
	}
	.class public sealed SystemPlatform
		extends [mscorlib]System.Enum
	{
		.field public rtspecialname specialname int32 value__
		.field static public literal valuetype liblinux.SystemPlatform ARM = int32(0x00000002)
		.field static public literal valuetype liblinux.SystemPlatform 'ARM64' = int32(0x00000003)
		.field static public literal valuetype liblinux.SystemPlatform 'MIPS' = int32(0x00000008)
		.field static public literal valuetype liblinux.SystemPlatform 'MIPS64' = int32(0x00000009)
		.field static public literal valuetype liblinux.SystemPlatform PPC = int32(0x00000006)
		.field static public literal valuetype liblinux.SystemPlatform 'PPC64' = int32(0x00000007)
		.field static public literal valuetype liblinux.SystemPlatform Unknown = int32(0x00000001)
		.field static public literal valuetype liblinux.SystemPlatform x64 = int32(0x00000005)
		.field static public literal valuetype liblinux.SystemPlatform 'x86' = int32(0x00000004)
	}
	.class public SystemProperties
		extends [mscorlib]System.Object
		implements class [mscorlib]'System.Collections.Generic.IEnumerable`1'<valuetype [mscorlib]'System.Collections.Generic.KeyValuePair`2'<string,string>>, [mscorlib]System.Collections.IEnumerable
	{
		.custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = { string('Item') }
		.method public virtual hidebysig newslot specialname 
			instance valuetype liblinux.SystemId get_Id()
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance valuetype liblinux.SystemId get_BaseId()
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance string get_Name()
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance class [mscorlib]System.Version get_Version()
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance valuetype liblinux.SystemPlatform get_Platform()
		{
			ret
		}
		.method public hidebysig specialname 
			instance string get_Item(valuetype liblinux.SystemPropertyId propertyId)
		{
			ret
		}
		.method public hidebysig specialname 
			instance string get_Item(string propertyName)
		{
			ret
		}
		.method public hidebysig 
			instance bool HasProperty(valuetype liblinux.SystemPropertyId propertyId)
		{
			ret
		}
		.method public hidebysig 
			instance bool HasProperty(string propertyName)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance string GetPropertyById(valuetype liblinux.SystemPropertyId propertyId)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance string GetPropertyByName(string propertyName)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance class [mscorlib]'System.Collections.Generic.IEnumerator`1'<valuetype [mscorlib]'System.Collections.Generic.KeyValuePair`2'<string,string>> GetEnumerator()
		{
			ret
		}
		.method private final virtual hidebysig newslot 
			instance class [mscorlib]System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			.override [mscorlib]System.Collections.IEnumerable::GetEnumerator
			ret
		}
		.property instance valuetype liblinux.SystemId BaseId()
		{
			.get instance valuetype liblinux.SystemId liblinux.SystemProperties::get_BaseId()
		}
		.property instance valuetype liblinux.SystemId Id()
		{
			.get instance valuetype liblinux.SystemId liblinux.SystemProperties::get_Id()
		}
		.property instance string Item(valuetype liblinux.SystemPropertyId)
		{
			.get instance string liblinux.SystemProperties::get_Item(valuetype liblinux.SystemPropertyId)
		}
		.property instance string Item(string)
		{
			.get instance string liblinux.SystemProperties::get_Item(string)
		}
		.property instance string Name()
		{
			.get instance string liblinux.SystemProperties::get_Name()
		}
		.property instance valuetype liblinux.SystemPlatform Platform()
		{
			.get instance valuetype liblinux.SystemPlatform liblinux.SystemProperties::get_Platform()
		}
		.property instance class [mscorlib]System.Version Version()
		{
			.get instance class [mscorlib]System.Version liblinux.SystemProperties::get_Version()
		}
	}
	.class public sealed SystemPropertyId
		extends [mscorlib]System.Enum
	{
		.field public rtspecialname specialname int32 value__
		.field static public literal valuetype liblinux.SystemPropertyId Id = int32(0x00000000)
		.field static public literal valuetype liblinux.SystemPropertyId Name = int32(0x00000001)
		.field static public literal valuetype liblinux.SystemPropertyId Platform = int32(0x00000002)
		.field static public literal valuetype liblinux.SystemPropertyId Version = int32(0x00000003)
	}
	.class public UnixSystem
		extends liblinux.RemoteSystem
	{
		.method public hidebysig specialname 
			instance class liblinux.Services.IServices get_Services()
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance class liblinux.SystemProperties get_Properties()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.ConnectionInfo connectionInfo)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.property instance class liblinux.SystemProperties Properties()
		{
			.get instance class liblinux.SystemProperties liblinux.UnixSystem::get_Properties()
		}
		.property instance class liblinux.Services.IServices Services()
		{
			.get instance class liblinux.Services.IServices liblinux.UnixSystem::get_Services()
		}
	}
}
.namespace 'liblinux.IO'
{
	.class public FileNotFoundException
		extends 'liblinux.IO.IOException'
	{
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class [mscorlib]System.Exception innerException)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message, class [mscorlib]System.Exception innerException)
		{
			ret
		}
	}
	.class public FilePermissions
		extends [mscorlib]System.Object
	{
		.method public hidebysig specialname 
			instance valuetype 'liblinux.IO.SecurityAttributes' get_Owner()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_Owner(valuetype 'liblinux.IO.SecurityAttributes' 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance valuetype 'liblinux.IO.SecurityAttributes' get_Group()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_Group(valuetype 'liblinux.IO.SecurityAttributes' 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance valuetype 'liblinux.IO.SecurityAttributes' get_Other()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_Other(valuetype 'liblinux.IO.SecurityAttributes' 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(int32 permissions)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(valuetype 'liblinux.IO.SecurityAttributes' ownerAttributes, valuetype 'liblinux.IO.SecurityAttributes' groupAttributes, valuetype 'liblinux.IO.SecurityAttributes' otherAttributes)
		{
			ret
		}
		.method public virtual hidebysig 
			instance string ToString()
		{
			ret
		}
		.method public hidebysig 
			instance int32 ToInt()
		{
			ret
		}
		.property instance valuetype 'liblinux.IO.SecurityAttributes' Group()
		{
			.get instance valuetype 'liblinux.IO.SecurityAttributes' 'liblinux.IO.FilePermissions'::get_Group()
			.set instance void 'liblinux.IO.FilePermissions'::set_Group(valuetype 'liblinux.IO.SecurityAttributes')
		}
		.property instance valuetype 'liblinux.IO.SecurityAttributes' Other()
		{
			.get instance valuetype 'liblinux.IO.SecurityAttributes' 'liblinux.IO.FilePermissions'::get_Other()
			.set instance void 'liblinux.IO.FilePermissions'::set_Other(valuetype 'liblinux.IO.SecurityAttributes')
		}
		.property instance valuetype 'liblinux.IO.SecurityAttributes' Owner()
		{
			.get instance valuetype 'liblinux.IO.SecurityAttributes' 'liblinux.IO.FilePermissions'::get_Owner()
			.set instance void 'liblinux.IO.FilePermissions'::set_Owner(valuetype 'liblinux.IO.SecurityAttributes')
		}
	}
	.class public IOException
		extends liblinux.ExceptionBase
	{
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class [mscorlib]System.Exception innerException)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message, class [mscorlib]System.Exception innerException)
		{
			ret
		}
	}
	.class public interface abstract IRemoteDirectory
		implements 'liblinux.IO.IRemoteFileSystemInfo', 'liblinux.IO.Unified.IDirectory'
	{
		.method public virtual hidebysig newslot abstract 
			instance void Delete()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.FileInfo' DownloadFile(string remotePath, string localFileName)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.FileInfo' DownloadFile(string remotePath, string localFileName, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteFile' UploadFile(string localFileName)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteFile' UploadFile(string localFileName, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.DirectoryInfo' Download(string localPath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.DirectoryInfo' Download(string localPath, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.DirectoryInfo' DownloadDirectory(string remotePath, string localPath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.DirectoryInfo' DownloadDirectory(string remotePath, string localPath, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory' UploadDirectory(string localPath, string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory' UploadDirectory(string localPath, string remotePath, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory' CreateDirectory(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory' CreateDirectories(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory'[] GetDirectories()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteFile'[] GetFiles()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool FileExists(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool DirectoryExists(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteFile' GetFile(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory' GetDirectory(string remotePath)
		{
		}
	}
	.class public interface abstract IRemoteFile
		implements 'liblinux.IO.IRemoteFileSystemInfo'
	{
		.method public virtual hidebysig newslot abstract 
			instance void Delete()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.FileInfo' DownloadTo(string localPath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.FileInfo' DownloadTo(string localPath, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.FileInfo' Download(string localFileName)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.FileInfo' Download(string localFileName, int32 bufferSize)
		{
		}
	}
	.class public interface abstract IRemoteFileSystem
		implements [mscorlib]System.IDisposable, class 'liblinux.IO.Unified.IFileSystem`1'<class 'liblinux.IO.IRemoteDirectory'>
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_IsConnected()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance class liblinux.ConnectionInfo get_ConnectionInfo()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteFile' UploadFile(string localFileName, string remoteFileName)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteFile' UploadFile(string localFileName, string remoteFileName, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class 'liblinux.IO.IRemoteFile'> UploadFileAsync(string localFileName, string remoteFileName, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class 'liblinux.IO.IRemoteFile'> UploadFileAsync(string localFileName, string remoteFileName, int32 bufferSize, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.FileInfo' DownloadFile(string remoteFileName, string localFileName)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.FileInfo' DownloadFile(string remoteFileName, string localFileName, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class [mscorlib]'System.IO.FileInfo'> DownloadFileAsync(string remoteFileName, string localFileName, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class [mscorlib]'System.IO.FileInfo'> DownloadFileAsync(string remoteFileName, string localFileName, int32 bufferSize, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory' CreateDirectory(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory' CreateDirectories(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory' UploadDirectory(string localPath, string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory' UploadDirectory(string localPath, string remotePath, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class 'liblinux.IO.IRemoteDirectory'> UploadDirectoryAsync(string localPath, string remotePath, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class 'liblinux.IO.IRemoteDirectory'> UploadDirectoryAsync(string localPath, string remotePath, string[] localExcludedPaths, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class 'liblinux.IO.IRemoteDirectory'> UploadDirectoryAsync(string localPath, string remotePath, string[] localExcludedPaths, int32 bufferSize, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.DirectoryInfo' DownloadDirectory(string remotePath, string localPath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.DirectoryInfo' DownloadDirectory(string remotePath, string localPath, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class [mscorlib]'System.IO.DirectoryInfo'> DownloadDirectoryAsync(string remotePath, string localPath, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class [mscorlib]'System.IO.DirectoryInfo'> DownloadDirectoryAsync(string remotePath, string localPath, int32 bufferSize, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool Exists(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool IsDirectory(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool IsFile(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool IsSymbolicLink(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteFileSystemInfo' ResolvePath(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance string GetDirectoryName(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.FilePermissions' GetPermissions(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void ChangePermissions(string remotePath, class 'liblinux.IO.FilePermissions' permissions)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Delete(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteFile'[] GetFiles(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory'[] GetDirectories()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteFile'[] GetFiles()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteFile' GetFile(string remoteFileName)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory' GetDirectory(valuetype 'liblinux.IO.SpecialDirectory' directory)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteFileSystemInfo' Stat(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance int64 GetFileSize(string remoteFileName)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(class liblinux.IRemoteSystemBare remoteSystem)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(class liblinux.ConnectionInfo connectionInfo)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, string userName, class [mscorlib]System.Security.SecureString password)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, int32 port, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, int32 port, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Disconnect()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Close()
		{
		}
		.property instance class liblinux.ConnectionInfo ConnectionInfo()
		{
			.get instance class liblinux.ConnectionInfo 'liblinux.IO.IRemoteFileSystem'::get_ConnectionInfo()
		}
		.property instance bool IsConnected()
		{
			.get instance bool 'liblinux.IO.IRemoteFileSystem'::get_IsConnected()
		}
	}
	.class public interface abstract IRemoteFileSystemInfo
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance string get_Name()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance string get_FullPath()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance class 'liblinux.IO.FilePermissions' get_Permissions()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance valuetype [mscorlib]System.DateTime get_DateAccessed()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance valuetype [mscorlib]System.DateTime get_DateModified()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool Exists()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool IsFile()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool IsDirectory()
		{
		}
		.property instance valuetype [mscorlib]System.DateTime DateAccessed()
		{
			.get instance valuetype [mscorlib]System.DateTime 'liblinux.IO.IRemoteFileSystemInfo'::get_DateAccessed()
		}
		.property instance valuetype [mscorlib]System.DateTime DateModified()
		{
			.get instance valuetype [mscorlib]System.DateTime 'liblinux.IO.IRemoteFileSystemInfo'::get_DateModified()
		}
		.property instance string FullPath()
		{
			.get instance string 'liblinux.IO.IRemoteFileSystemInfo'::get_FullPath()
		}
		.property instance string Name()
		{
			.get instance string 'liblinux.IO.IRemoteFileSystemInfo'::get_Name()
		}
		.property instance class 'liblinux.IO.FilePermissions' Permissions()
		{
			.get instance class 'liblinux.IO.FilePermissions' 'liblinux.IO.IRemoteFileSystemInfo'::get_Permissions()
		}
	}
	.class public abstract sealed PathUtils
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = { }
		.method public static hidebysig 
			string CombineUnixPaths(string path1, string path2)
		{
			ret
		}
		.method public static hidebysig 
			string ConvertUnixPathToWindowsPath(string remotePath)
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = { }
			ret
		}
		.method public static hidebysig 
			string ConvertWindowsPathToUnixPath(string remotePath)
		{
			ret
		}
		.method public static hidebysig 
			bool IsAbsolutePath(string localOrRemotePath)
		{
			ret
		}
		.method public static hidebysig 
			bool IsRemotePathRelative(string remotePath)
		{
			ret
		}
		.method public static hidebysig 
			bool IsSubdirectory(string path, string subPath)
		{
			ret
		}
		.method public static hidebysig 
			bool IsSubdirectory(class [mscorlib]'System.Collections.Generic.IEnumerable`1'<string> paths, string subPath)
		{
			ret
		}
		.method public static hidebysig 
			string TrimStartAnySlashes(string path)
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = { }
			ret
		}
		.method public static hidebysig 
			string AppendTrailingBackslash(string path)
		{
			ret
		}
		.method public static hidebysig 
			string AppendTrailingForwardSlash(string path)
		{
			ret
		}
		.method public static hidebysig 
			string RemoveTrailingSlash(string path)
		{
			ret
		}
		.method public static hidebysig 
			string RemoveLeadingSlash(string path)
		{
			ret
		}
		.method public static hidebysig 
			string MakeUnixPath(string pathUnixDir, string relativeWindowsPath)
		{
			ret
		}
		.method public static hidebysig 
			string RemoveDriveFromPath(string localOrRemotePath)
		{
			ret
		}
		.method public static hidebysig 
			string RemoveParentPath(string path, string parentPath)
		{
			ret
		}
		.method public static hidebysig 
			string EnsureLeadingForwardSlash(string path)
		{
			ret
		}
		.method public static hidebysig 
			bool EqualsToAnyElement(string path, string name)
		{
			ret
		}
		.method public static hidebysig 
			bool GlobMatchesAnyElement(string path, string globExpression)
		{
			ret
		}
		.method public static hidebysig 
			bool IsPathExcluded(string sourceBasePath, string itemPath, string[] exclusionList)
		{
			ret
		}
		.method public static hidebysig 
			string GetPathExact(string path)
		{
			ret
		}
		.method public static hidebysig 
			string EscapeStringForUnixShell(string input)
		{
			ret
		}
		.method public static hidebysig 
			string GetUnixDirectoryName(string path)
		{
			ret
		}
		.method public static hidebysig 
			string GetFullPath(class 'liblinux.Shell.Unified.ICommandHost`2'<class liblinux.Shell.ICommand,class liblinux.Shell.IAsyncCommand> shell, string remotePath)
		{
			ret
		}
		.method public static hidebysig 
			string GetFileName(class 'liblinux.Shell.Unified.ICommandHost`2'<class liblinux.Shell.ICommand,class liblinux.Shell.IAsyncCommand> shell, string remotePath)
		{
			ret
		}
		.method public static hidebysig 
			string[] TranslateWindowsPathToWSLPath(string[] localWindowsPaths)
		{
			ret
		}
		.method public static hidebysig 
			string TranslateWindowsPathToWSLPath(string localWindowsPath)
		{
			ret
		}
		.method public static hidebysig 
			string NormalizePathSlow(string path)
		{
			ret
		}
		.method public static hidebysig 
			string[] TranslateWSLToWindows(string[] wslPaths)
		{
			ret
		}
		.method public static hidebysig 
			string TranslateWSLPathToWindowsPath(string wslPath)
		{
			ret
		}
		.field static public initonly string UnixDirectorySeparator
		.field static public initonly char UnixDirectorySeparatorChar
		.field static public initonly string WindowsDirectorySeparator
		.field static public initonly char WindowsDirectorySeparatorChar
	}
	.class public RemoteFileSystem
		extends [mscorlib]System.Object
		implements 'liblinux.IO.IRemoteFileSystem', [mscorlib]System.IDisposable, class 'liblinux.IO.Unified.IFileSystem`1'<class 'liblinux.IO.IRemoteDirectory'>
	{
		.method public final virtual hidebysig newslot specialname 
			instance bool get_IsConnected()
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance class liblinux.ConnectionInfo get_ConnectionInfo()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.IRemoteSystemBare remoteSystem)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.ConnectionInfo connectionInfo)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(class liblinux.IRemoteSystemBare remoteSystem)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(class liblinux.ConnectionInfo connectionInfo)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 port, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 port, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class 'liblinux.IO.IRemoteFile' UploadFile(string localFileName, string remoteFileName)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class 'liblinux.IO.IRemoteFile' UploadFile(string localFileName, string remoteFileName, int32 bufferSize)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class 'liblinux.IO.IRemoteFile'> UploadFileAsync(string localFileName, string remoteFileName, int32 bufferSize)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class 'liblinux.IO.IRemoteFile'> UploadFileAsync(string localFileName, string remoteFileName, int32 bufferSize, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.IO.FileInfo' DownloadFile(string remoteFileName, string localFileName)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.IO.FileInfo' DownloadFile(string remoteFileName, string localFileName, int32 bufferSize)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class [mscorlib]'System.IO.FileInfo'> DownloadFileAsync(string remoteFileName, string localFileName, int32 bufferSize)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class [mscorlib]'System.IO.FileInfo'> DownloadFileAsync(string remoteFileName, string localFileName, int32 bufferSize, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class 'liblinux.IO.IRemoteDirectory' CreateDirectory(string remotePath)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class 'liblinux.IO.IRemoteDirectory' CreateDirectories(string remotePath)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class 'liblinux.IO.IRemoteDirectory' UploadDirectory(string localPath, string remotePath)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class 'liblinux.IO.IRemoteDirectory' UploadDirectory(string localPath, string remotePath, int32 bufferSize)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class 'liblinux.IO.IRemoteDirectory'> UploadDirectoryAsync(string localPath, string remotePath, int32 bufferSize)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class 'liblinux.IO.IRemoteDirectory'> UploadDirectoryAsync(string localPath, string remotePath, string[] localExcludedPaths, int32 bufferSize)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class 'liblinux.IO.IRemoteDirectory'> UploadDirectoryAsync(string localPath, string remotePath, string[] localExcludedPaths, int32 bufferSize, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.IO.DirectoryInfo' DownloadDirectory(string remotePath, string localPath)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.IO.DirectoryInfo' DownloadDirectory(string remotePath, string localPath, int32 bufferSize)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class [mscorlib]'System.IO.DirectoryInfo'> DownloadDirectoryAsync(string remotePath, string localPath, int32 bufferSize)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class [mscorlib]'System.IO.DirectoryInfo'> DownloadDirectoryAsync(string remotePath, string localPath, int32 bufferSize, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance bool Exists(string remotePath)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance bool FileExists(string remoteFileName)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance bool DirectoryExists(string remotePath)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance bool IsDirectory(string remotePath)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance bool IsFile(string remotePath)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance bool IsSymbolicLink(string remotePath)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance class 'liblinux.IO.IRemoteFileSystemInfo' ResolvePath(string remotePath)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance string GetFullPath(string remotePath)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance string GetFileName(string remotePath)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance string GetDirectoryName(string remotePath)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance class 'liblinux.IO.FilePermissions' GetPermissions(string remotePath)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void ChangePermissions(string remotePath, class 'liblinux.IO.FilePermissions' permissions)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Delete(string remotePath)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance class 'liblinux.IO.IRemoteDirectory'[] GetDirectories(string remotePath)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance class 'liblinux.IO.IRemoteFile'[] GetFiles(string remotePath)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance class 'liblinux.IO.IRemoteDirectory'[] GetDirectories()
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance class 'liblinux.IO.IRemoteFile'[] GetFiles()
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance class 'liblinux.IO.IRemoteDirectory' GetDirectory(string remotePath)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance class 'liblinux.IO.IRemoteFile' GetFile(string remoteFileName)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance class 'liblinux.IO.IRemoteDirectory' GetDirectory(valuetype 'liblinux.IO.SpecialDirectory' directory)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance class 'liblinux.IO.IRemoteFileSystemInfo' Stat(string remotePath)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance int64 GetFileSize(string remoteFileName)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Dispose()
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Disconnect()
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Close()
		{
			ret
		}
		.property instance class liblinux.ConnectionInfo ConnectionInfo()
		{
			.get instance class liblinux.ConnectionInfo 'liblinux.IO.RemoteFileSystem'::get_ConnectionInfo()
		}
		.property instance bool IsConnected()
		{
			.get instance bool 'liblinux.IO.RemoteFileSystem'::get_IsConnected()
		}
	}
	.class public abstract RemoteFileSystemBase
		extends [mscorlib]System.Object
		implements 'liblinux.IO.IRemoteFileSystem', [mscorlib]System.IDisposable, class 'liblinux.IO.Unified.IFileSystem`1'<class 'liblinux.IO.IRemoteDirectory'>
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_IsConnected()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance class liblinux.ConnectionInfo get_ConnectionInfo()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteFile' UploadFile(string localFileName, string remoteFileName)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteFile' UploadFile(string localFileName, string remoteFileName, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class 'liblinux.IO.IRemoteFile'> UploadFileAsync(string localFileName, string remoteFileName, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class 'liblinux.IO.IRemoteFile'> UploadFileAsync(string localFileName, string remoteFileName, int32 bufferSize, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.FileInfo' DownloadFile(string remoteFileName, string localFileName)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.FileInfo' DownloadFile(string remoteFileName, string localFileName, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class [mscorlib]'System.IO.FileInfo'> DownloadFileAsync(string remoteFileName, string localFileName, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class [mscorlib]'System.IO.FileInfo'> DownloadFileAsync(string remoteFileName, string localFileName, int32 bufferSize, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory' CreateDirectory(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory' CreateDirectories(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory' UploadDirectory(string localPath, string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory' UploadDirectory(string localPath, string remotePath, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class 'liblinux.IO.IRemoteDirectory'> UploadDirectoryAsync(string localPath, string remotePath, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class 'liblinux.IO.IRemoteDirectory'> UploadDirectoryAsync(string localPath, string remotePath, string[] localExcludedPaths, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class 'liblinux.IO.IRemoteDirectory'> UploadDirectoryAsync(string localPath, string remotePath, string[] localExcludedPaths, int32 bufferSize, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.DirectoryInfo' DownloadDirectory(string remotePath, string localPath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.DirectoryInfo' DownloadDirectory(string remotePath, string localPath, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class [mscorlib]'System.IO.DirectoryInfo'> DownloadDirectoryAsync(string remotePath, string localPath, int32 bufferSize)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<class [mscorlib]'System.IO.DirectoryInfo'> DownloadDirectoryAsync(string remotePath, string localPath, int32 bufferSize, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool Exists(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool FileExists(string remoteFileName)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool DirectoryExists(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool IsDirectory(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool IsFile(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool IsSymbolicLink(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteFileSystemInfo' ResolvePath(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance string GetFullPath(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance string GetFileName(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance string GetDirectoryName(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.FilePermissions' GetPermissions(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void ChangePermissions(string remotePath, class 'liblinux.IO.FilePermissions' permissions)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance valuetype [mscorlib]System.DateTime GetDateModified(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance valuetype [mscorlib]System.DateTime GetDateAccessed(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void DeleteDirectory(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void DeleteFile(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Delete(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory'[] GetDirectories(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteFile'[] GetFiles(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory'[] GetDirectories()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteFile'[] GetFiles()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory' GetDirectory(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteFile' GetFile(string remoteFileName)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteDirectory' GetDirectory(valuetype 'liblinux.IO.SpecialDirectory' directory)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.IRemoteFileSystemInfo' Stat(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance int64 GetFileSize(string remoteFileName)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(class liblinux.IRemoteSystemBare remoteSystem)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Connect(class liblinux.ConnectionInfo connectionInfo)
		{
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 port, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 port, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public virtual hidebysig newslot abstract 
			instance void Dispose()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Disconnect()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Close()
		{
		}
		.method family hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.property instance class liblinux.ConnectionInfo ConnectionInfo()
		{
			.get instance class liblinux.ConnectionInfo 'liblinux.IO.RemoteFileSystemBase'::get_ConnectionInfo()
		}
		.property instance bool IsConnected()
		{
			.get instance bool 'liblinux.IO.RemoteFileSystemBase'::get_IsConnected()
		}
	}
	.class public sealed SecurityAttributes
		extends [mscorlib]System.Enum
	{
		.custom instance void [mscorlib]System.FlagsAttribute::.ctor() = { }
		.field public rtspecialname specialname int32 value__
		.field static public literal valuetype 'liblinux.IO.SecurityAttributes' Execute = int32(0x00000001)
		.field static public literal valuetype 'liblinux.IO.SecurityAttributes' Read = int32(0x00000004)
		.field static public literal valuetype 'liblinux.IO.SecurityAttributes' Write = int32(0x00000002)
	}
	.class public sealed SecurityGroupType
		extends [mscorlib]System.Enum
	{
		.field public rtspecialname specialname int32 value__
		.field static public literal valuetype 'liblinux.IO.SecurityGroupType' Group = int32(0x00000002)
		.field static public literal valuetype 'liblinux.IO.SecurityGroupType' Other = int32(0x00000003)
		.field static public literal valuetype 'liblinux.IO.SecurityGroupType' Owner = int32(0x00000001)
	}
	.class public sealed SpecialDirectory
		extends [mscorlib]System.Enum
	{
		.field public rtspecialname specialname int32 value__
		.field static public literal valuetype 'liblinux.IO.SpecialDirectory' Home = int32(0x00000001)
		.field static public literal valuetype 'liblinux.IO.SpecialDirectory' Lib = int32(0x00000003)
		.field static public literal valuetype 'liblinux.IO.SpecialDirectory' Tmp = int32(0x00000004)
		.field static public literal valuetype 'liblinux.IO.SpecialDirectory' Usr = int32(0x00000002)
	}
}
.namespace 'liblinux.IO.Unified'
{
	.class public interface abstract IDirectory
		implements 'liblinux.IO.IRemoteFileSystemInfo'
	{
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.Unified.IDirectory'[] GetUnifiedDirectories()
		{
		}
	}
	.class public interface abstract 'IFileSystem`1'<+ ('liblinux.IO.Unified.IDirectory') 'TDirectory'>
	{
		.method public virtual hidebysig newslot abstract 
			instance bool FileExists(string remoteFileName)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool DirectoryExists(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance !0 GetDirectory(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance string GetFullPath(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance string GetFileName(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance !0[] GetDirectories(string remotePath)
		{
		}
	}
}
.namespace liblinux.Persistence
{
	.class public ConnectionInfoStore
		extends [mscorlib]System.Object
	{
		.method public hidebysig specialname 
			instance class liblinux.Persistence.StoredConnectionInfos get_Connections()
		{
			ret
		}
		.method public hidebysig specialname 
			instance class liblinux.Persistence.StoredConnectionInfo get_LastSuccessfulConnection()
		{
			ret
		}
		.method public hidebysig specialname 
			instance class liblinux.Persistence.StoredConnectionInfo get_FirstSuccessfulConnection()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string storeName)
		{
			ret
		}
		.method public hidebysig 
			instance class liblinux.Persistence.StoredConnectionInfo Add(class liblinux.ConnectionInfo connectionInfo)
		{
			ret
		}
		.method public hidebysig 
			instance class liblinux.Persistence.StoredConnectionInfo Add(class liblinux.ConnectionInfo connectionInfo, class [System]System.Collections.Specialized.NameValueCollection properties)
		{
			ret
		}
		.method public hidebysig 
			instance void Add(class liblinux.Persistence.StoredConnectionInfo storedConnectionInfo)
		{
			ret
		}
		.method public hidebysig 
			instance class liblinux.Persistence.StoredConnectionInfo Add(class liblinux.ConnectionInfo connectionInfo, valuetype liblinux.Persistence.PersistenceMode mode)
		{
			ret
		}
		.method public hidebysig 
			instance class liblinux.Persistence.StoredConnectionInfo Add(class liblinux.ConnectionInfo connectionInfo, class [System]System.Collections.Specialized.NameValueCollection properties, valuetype liblinux.Persistence.PersistenceMode mode)
		{
			ret
		}
		.method public hidebysig 
			instance void Add(class liblinux.Persistence.StoredConnectionInfo storedConnectionInfo, valuetype liblinux.Persistence.PersistenceMode mode)
		{
			ret
		}
		.method public hidebysig 
			instance void Update(class liblinux.Persistence.StoredConnectionInfo storedConnectionInfo)
		{
			ret
		}
		.method public hidebysig 
			instance void Update(class liblinux.ConnectionInfo connectionInfo, [opt] valuetype liblinux.Persistence.UpdateMode updateMode)
		{
			.param [2] = int32(0x00000000)
			ret
		}
		.method public hidebysig 
			instance void Update(class liblinux.Persistence.StoredConnectionInfo storedConnectionInfo, [opt] valuetype liblinux.Persistence.UpdateMode updateMode)
		{
			.param [2] = int32(0x00000000)
			ret
		}
		.method public hidebysig 
			instance bool TryGetById(int32 id, [out] class liblinux.ConnectionInfo& connectionInfo)
		{
			ret
		}
		.method public hidebysig 
			instance bool TryGetById(int32 id, valuetype liblinux.Persistence.RetrieveMode mode, [out] class liblinux.ConnectionInfo& connectionInfo)
		{
			ret
		}
		.method public hidebysig 
			instance bool TryGetById(int32 id, [out] class liblinux.Persistence.StoredConnectionInfo& storedConnectionInfo)
		{
			ret
		}
		.method public hidebysig 
			instance bool TryGetById(int32 id, valuetype liblinux.Persistence.RetrieveMode mode, [out] class liblinux.Persistence.StoredConnectionInfo& storedConnectionInfo)
		{
			ret
		}
		.method public hidebysig 
			instance bool TryGet(class liblinux.ConnectionInfo connectionInfo, [out] class liblinux.Persistence.StoredConnectionInfo& storedConnectionInfo)
		{
			ret
		}
		.method public hidebysig 
			instance bool TryGetByName(string name, [out] class liblinux.ConnectionInfo& connectionInfo)
		{
			ret
		}
		.method public hidebysig 
			instance bool TryGetByName(string name, valuetype liblinux.Persistence.RetrieveMode mode, [out] class liblinux.ConnectionInfo& connectionInfo)
		{
			ret
		}
		.method public hidebysig 
			instance bool TryGetByName(string name, valuetype [mscorlib]System.StringComparison comparisonType, [out] class liblinux.ConnectionInfo& connectionInfo)
		{
			ret
		}
		.method public hidebysig 
			instance bool TryGetByName(string name, valuetype [mscorlib]System.StringComparison comparisonType, valuetype liblinux.Persistence.RetrieveMode mode, [out] class liblinux.ConnectionInfo& connectionInfo)
		{
			ret
		}
		.method public hidebysig 
			instance bool TryGetByName(string name, [out] class liblinux.Persistence.StoredConnectionInfo& storedConnectionInfo)
		{
			ret
		}
		.method public hidebysig 
			instance bool TryGetByName(string name, valuetype liblinux.Persistence.RetrieveMode mode, [out] class liblinux.Persistence.StoredConnectionInfo& storedConnectionInfo)
		{
			ret
		}
		.method public hidebysig 
			instance bool TryGetByName(string name, valuetype [mscorlib]System.StringComparison comparisonType, [out] class liblinux.Persistence.StoredConnectionInfo& storedConnectionInfo)
		{
			ret
		}
		.method public hidebysig 
			instance bool TryGetByName(string name, valuetype [mscorlib]System.StringComparison comparisonType, valuetype liblinux.Persistence.RetrieveMode mode, [out] class liblinux.Persistence.StoredConnectionInfo& storedConnectionInfo)
		{
			ret
		}
		.method public hidebysig 
			instance bool Contains(class liblinux.ConnectionInfo connectionInfo)
		{
			ret
		}
		.method public hidebysig 
			instance bool Contains(int32 connectionId)
		{
			ret
		}
		.method public hidebysig 
			instance bool Contains(string name)
		{
			ret
		}
		.method public hidebysig 
			instance bool Contains(string name, valuetype [mscorlib]System.StringComparison comparisonType)
		{
			ret
		}
		.method public hidebysig 
			instance void RemoveById(int32 connectionId)
		{
			ret
		}
		.method public hidebysig 
			instance void Load()
		{
			ret
		}
		.method public hidebysig 
			instance void LoadIfNeeded()
		{
			ret
		}
		.method public hidebysig 
			instance void Save()
		{
			ret
		}
		.method public hidebysig 
			instance void Clear()
		{
			ret
		}
		.property instance class liblinux.Persistence.StoredConnectionInfos Connections()
		{
			.get instance class liblinux.Persistence.StoredConnectionInfos liblinux.Persistence.ConnectionInfoStore::get_Connections()
		}
		.property instance class liblinux.Persistence.StoredConnectionInfo FirstSuccessfulConnection()
		{
			.get instance class liblinux.Persistence.StoredConnectionInfo liblinux.Persistence.ConnectionInfoStore::get_FirstSuccessfulConnection()
		}
		.property instance class liblinux.Persistence.StoredConnectionInfo LastSuccessfulConnection()
		{
			.get instance class liblinux.Persistence.StoredConnectionInfo liblinux.Persistence.ConnectionInfoStore::get_LastSuccessfulConnection()
		}
	}
	.class public ConnectionInfoStoreException
		extends liblinux.ExceptionBase
	{
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message, class [mscorlib]System.Exception innerException)
		{
			ret
		}
	}
	.class public abstract sealed ConnectionInfoStoreExtensions
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = { }
		.method public static hidebysig 
			bool TryGetConnectionInfoOrDefault(class liblinux.Persistence.ConnectionInfoStore store, string remoteTargetStringOrId, [out] class liblinux.ConnectionInfo& connectionInfo)
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = { }
			ret
		}
		.method public static hidebysig 
			bool TryGetConnectionInfoOrDefault(class liblinux.Persistence.ConnectionInfoStore store, string remoteTargetStringOrId, valuetype liblinux.Persistence.RetrieveMode mode, [out] class liblinux.ConnectionInfo& connectionInfo)
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = { }
			ret
		}
		.method public static hidebysig 
			bool TryGetConnectionInfoOrDefault(class liblinux.Persistence.ConnectionInfoStore store, string remoteTargetStringOrId, [out] class liblinux.Persistence.StoredConnectionInfo& storedConnectionInfo)
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = { }
			ret
		}
		.method public static hidebysig 
			bool TryGetConnectionInfoOrDefault(class liblinux.Persistence.ConnectionInfoStore store, string remoteTargetStringOrId, valuetype liblinux.Persistence.RetrieveMode mode, [out] class liblinux.Persistence.StoredConnectionInfo& storedConnectionInfo)
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = { }
			ret
		}
		.method public static hidebysig 
			bool TryGetDefaultConnectionInfo(class liblinux.Persistence.ConnectionInfoStore store, valuetype liblinux.Persistence.RetrieveMode mode, class liblinux.Persistence.StoredConnectionInfo[] excludedConnections, [out] class liblinux.Persistence.StoredConnectionInfo& storedConnectionInfo)
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = { }
			ret
		}
		.method public static hidebysig 
			class liblinux.Persistence.StoredConnectionInfo[] GetAvailableConnectionsByAddedDate(class liblinux.Persistence.ConnectionInfoStore store)
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = { }
			ret
		}
	}
	.class public ConnectionInfoStoreWatcher
		extends [mscorlib]System.Object
		implements [mscorlib]System.IDisposable
	{
		.method public hidebysig specialname 
			instance void add_Changed(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void remove_Changed(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Dispose()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnChanged()
		{
			ret
		}
		.event [mscorlib]System.EventHandler Changed
		{
			.addon instance void liblinux.Persistence.ConnectionInfoStoreWatcher::add_Changed(class [mscorlib]System.EventHandler)
			.removeon instance void liblinux.Persistence.ConnectionInfoStoreWatcher::remove_Changed(class [mscorlib]System.EventHandler)
		}
	}
	.class public sealed PersistenceMode
		extends [mscorlib]System.Enum
	{
		.field public rtspecialname specialname int32 value__
		.field static public literal valuetype liblinux.Persistence.PersistenceMode Disk = int32(0x00000001)
		.field static public literal valuetype liblinux.Persistence.PersistenceMode Memory = int32(0x00000000)
	}
	.class public sealed RetrieveMode
		extends [mscorlib]System.Enum
	{
		.field public rtspecialname specialname int32 value__
		.field static public literal valuetype liblinux.Persistence.RetrieveMode All = int32(0x00000000)
		.field static public literal valuetype liblinux.Persistence.RetrieveMode PropertiesOnly = int32(0x00000001)
	}
	.class public StoredConnectionInfo
		extends [mscorlib]System.Object
	{
		.method public hidebysig specialname 
			instance int32 get_Id()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_Id(int32 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance valuetype [mscorlib]System.DateTime get_DateAdded()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_DateAdded(valuetype [mscorlib]System.DateTime 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance valuetype [mscorlib]System.DateTime get_LastSuccessful()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_LastSuccessful(valuetype [mscorlib]System.DateTime 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance valuetype [mscorlib]System.DateTime get_LastUpdated()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_LastUpdated(valuetype [mscorlib]System.DateTime 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance valuetype [mscorlib]System.DateTime get_LastFailed()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_LastFailed(valuetype [mscorlib]System.DateTime 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance valuetype liblinux.Persistence.PersistenceMode get_PersistenceMode()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_PersistenceMode(valuetype liblinux.Persistence.PersistenceMode 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance object get_Tag()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_Tag(object 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance class [System]System.Collections.Specialized.NameValueCollection get_Properties()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public static hidebysig specialname 
			class liblinux.ConnectionInfo op_Implicit(class liblinux.Persistence.StoredConnectionInfo storedInfo)
		{
			ret
		}
		.property instance valuetype [mscorlib]System.DateTime DateAdded()
		{
			.get instance valuetype [mscorlib]System.DateTime liblinux.Persistence.StoredConnectionInfo::get_DateAdded()
			.set instance void liblinux.Persistence.StoredConnectionInfo::set_DateAdded(valuetype [mscorlib]System.DateTime)
		}
		.property instance int32 Id()
		{
			.get instance int32 liblinux.Persistence.StoredConnectionInfo::get_Id()
			.set instance void liblinux.Persistence.StoredConnectionInfo::set_Id(int32)
		}
		.property instance valuetype [mscorlib]System.DateTime LastFailed()
		{
			.get instance valuetype [mscorlib]System.DateTime liblinux.Persistence.StoredConnectionInfo::get_LastFailed()
			.set instance void liblinux.Persistence.StoredConnectionInfo::set_LastFailed(valuetype [mscorlib]System.DateTime)
		}
		.property instance valuetype [mscorlib]System.DateTime LastSuccessful()
		{
			.get instance valuetype [mscorlib]System.DateTime liblinux.Persistence.StoredConnectionInfo::get_LastSuccessful()
			.set instance void liblinux.Persistence.StoredConnectionInfo::set_LastSuccessful(valuetype [mscorlib]System.DateTime)
		}
		.property instance valuetype [mscorlib]System.DateTime LastUpdated()
		{
			.get instance valuetype [mscorlib]System.DateTime liblinux.Persistence.StoredConnectionInfo::get_LastUpdated()
			.set instance void liblinux.Persistence.StoredConnectionInfo::set_LastUpdated(valuetype [mscorlib]System.DateTime)
		}
		.property instance valuetype liblinux.Persistence.PersistenceMode PersistenceMode()
		{
			.get instance valuetype liblinux.Persistence.PersistenceMode liblinux.Persistence.StoredConnectionInfo::get_PersistenceMode()
			.set instance void liblinux.Persistence.StoredConnectionInfo::set_PersistenceMode(valuetype liblinux.Persistence.PersistenceMode)
		}
		.property instance class [System]System.Collections.Specialized.NameValueCollection Properties()
		{
			.get instance class [System]System.Collections.Specialized.NameValueCollection liblinux.Persistence.StoredConnectionInfo::get_Properties()
		}
		.property instance object Tag()
		{
			.get instance object liblinux.Persistence.StoredConnectionInfo::get_Tag()
			.set instance void liblinux.Persistence.StoredConnectionInfo::set_Tag(object)
		}
	}
	.class public StoredConnectionInfos
		extends [mscorlib]System.Object
		implements class [mscorlib]'System.Collections.Generic.IReadOnlyList`1'<class liblinux.Persistence.StoredConnectionInfo>, class [mscorlib]'System.Collections.Generic.IReadOnlyCollection`1'<class liblinux.Persistence.StoredConnectionInfo>, class [mscorlib]'System.Collections.Generic.IEnumerable`1'<class liblinux.Persistence.StoredConnectionInfo>, [mscorlib]System.Collections.IEnumerable
	{
		.custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = { string('Item') }
		.method public final virtual hidebysig newslot specialname 
			instance int32 get_Count()
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance class liblinux.Persistence.StoredConnectionInfo get_Item(int32 index)
		{
			ret
		}
		.method public hidebysig specialname 
			instance class liblinux.Persistence.StoredConnectionInfo get_Item(string name)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance class [mscorlib]'System.Collections.Generic.IEnumerator`1'<class liblinux.Persistence.StoredConnectionInfo> GetEnumerator()
		{
			ret
		}
		.method private final virtual hidebysig newslot 
			instance class [mscorlib]System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			.override [mscorlib]System.Collections.IEnumerable::GetEnumerator
			ret
		}
		.property instance int32 Count()
		{
			.get instance int32 liblinux.Persistence.StoredConnectionInfos::get_Count()
		}
		.property instance class liblinux.Persistence.StoredConnectionInfo Item(int32)
		{
			.get instance class liblinux.Persistence.StoredConnectionInfo liblinux.Persistence.StoredConnectionInfos::get_Item(int32)
		}
		.property instance class liblinux.Persistence.StoredConnectionInfo Item(string)
		{
			.get instance class liblinux.Persistence.StoredConnectionInfo liblinux.Persistence.StoredConnectionInfos::get_Item(string)
		}
	}
	.class public sealed UpdateMode
		extends [mscorlib]System.Enum
	{
		.field public rtspecialname specialname int32 value__
		.field static public literal valuetype liblinux.Persistence.UpdateMode Credentials = int32(0x00000002)
		.field static public literal valuetype liblinux.Persistence.UpdateMode NoPassword = int32(0x00000001)
		.field static public literal valuetype liblinux.Persistence.UpdateMode None = int32(0x00000000)
		.field static public literal valuetype liblinux.Persistence.UpdateMode PropertiesOnly = int32(0x00000003)
	}
}
.namespace liblinux.Services
{
	.class public sealed CompilerId
		extends [mscorlib]System.Enum
	{
		.field public rtspecialname specialname int32 value__
		.field static public literal valuetype liblinux.Services.CompilerId Clang = int32(0x00000001)
		.field static public literal valuetype liblinux.Services.CompilerId Gcc = int32(0x00000000)
	}
	.class public GdbServer
		extends [mscorlib]System.Object
		implements [mscorlib]System.IDisposable
	{
		.method public hidebysig specialname 
			instance int32 get_DebugeePid()
		{
			ret
		}
		.method public hidebysig specialname 
			instance class liblinux.Services.GdbServerStartInfo get_StartInfo()
		{
			ret
		}
		.method public hidebysig specialname 
			instance class liblinux.Shell.IStreamingShell get_Shell()
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Dispose()
		{
			ret
		}
		.method public hidebysig 
			instance void Stop()
		{
			ret
		}
		.property instance int32 DebugeePid()
		{
			.get instance int32 liblinux.Services.GdbServer::get_DebugeePid()
		}
		.property instance class liblinux.Shell.IStreamingShell Shell()
		{
			.get instance class liblinux.Shell.IStreamingShell liblinux.Services.GdbServer::get_Shell()
		}
		.property instance class liblinux.Services.GdbServerStartInfo StartInfo()
		{
			.get instance class liblinux.Services.GdbServerStartInfo liblinux.Services.GdbServer::get_StartInfo()
		}
	}
	.class public GdbServerFactory
		extends [mscorlib]System.Object
	{
		.method public hidebysig 
			instance class liblinux.Services.GdbServer Start(string remotePath)
		{
			ret
		}
		.method public hidebysig 
			instance class liblinux.Services.GdbServer Start(string preLaunchCommand, string remotePath)
		{
			ret
		}
		.method public hidebysig 
			instance class liblinux.Services.GdbServer Start(class liblinux.Services.GdbServerStartInfo startInfo)
		{
			ret
		}
		.method public hidebysig 
			instance class liblinux.Services.GdbServer Start(string remotePath, int32 localPort, int32 remotePort)
		{
			ret
		}
		.method public hidebysig 
			instance class liblinux.Services.GdbServer Start(string preLaunchCommand, string remotePath, int32 localPort, int32 remotePort)
		{
			ret
		}
	}
	.class public GdbServerLocalPortInUseException
		extends liblinux.ExceptionBase
	{
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message, class [mscorlib]System.Exception innerException)
		{
			ret
		}
	}
	.class public GdbServerPortSelectionException
		extends liblinux.ExceptionBase
	{
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message, class [mscorlib]System.Exception innerException)
		{
			ret
		}
	}
	.class public GdbServerRemotePortInUseException
		extends liblinux.ExceptionBase
	{
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message, class [mscorlib]System.Exception innerException)
		{
			ret
		}
	}
	.class public GdbServerStartInfo
		extends [mscorlib]System.Object
	{
		.method public hidebysig specialname 
			instance string get_RemotePath()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_RemotePath(string 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance int32 get_LocalPort()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_LocalPort(int32 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance int32 get_RemotePort()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_RemotePort(int32 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance string get_PreLaunchCommand()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_PreLaunchCommand(string 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance string get_WorkingDirectory()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_WorkingDirectory(string 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance string[] get_Arguments()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_Arguments(string[] 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance valuetype [mscorlib]'System.Nullable`1'<int32> get_ProcessId()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_ProcessId(valuetype [mscorlib]'System.Nullable`1'<int32> 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.property instance string[] Arguments()
		{
			.get instance string[] liblinux.Services.GdbServerStartInfo::get_Arguments()
			.set instance void liblinux.Services.GdbServerStartInfo::set_Arguments(string[])
		}
		.property instance int32 LocalPort()
		{
			.get instance int32 liblinux.Services.GdbServerStartInfo::get_LocalPort()
			.set instance void liblinux.Services.GdbServerStartInfo::set_LocalPort(int32)
		}
		.property instance string PreLaunchCommand()
		{
			.get instance string liblinux.Services.GdbServerStartInfo::get_PreLaunchCommand()
			.set instance void liblinux.Services.GdbServerStartInfo::set_PreLaunchCommand(string)
		}
		.property instance valuetype [mscorlib]'System.Nullable`1'<int32> ProcessId()
		{
			.get instance valuetype [mscorlib]'System.Nullable`1'<int32> liblinux.Services.GdbServerStartInfo::get_ProcessId()
			.set instance void liblinux.Services.GdbServerStartInfo::set_ProcessId(valuetype [mscorlib]'System.Nullable`1'<int32>)
		}
		.property instance string RemotePath()
		{
			.get instance string liblinux.Services.GdbServerStartInfo::get_RemotePath()
			.set instance void liblinux.Services.GdbServerStartInfo::set_RemotePath(string)
		}
		.property instance int32 RemotePort()
		{
			.get instance int32 liblinux.Services.GdbServerStartInfo::get_RemotePort()
			.set instance void liblinux.Services.GdbServerStartInfo::set_RemotePort(int32)
		}
		.property instance string WorkingDirectory()
		{
			.get instance string liblinux.Services.GdbServerStartInfo::get_WorkingDirectory()
			.set instance void liblinux.Services.GdbServerStartInfo::set_WorkingDirectory(string)
		}
	}
	.class public interface abstract IServices
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance class liblinux.Services.GdbServerFactory get_GdbServer()
		{
		}
		.property instance class liblinux.Services.GdbServerFactory GdbServer()
		{
			.get instance class liblinux.Services.GdbServerFactory liblinux.Services.IServices::get_GdbServer()
		}
	}
	.class public abstract RemoteCompiler
		extends [mscorlib]System.Object
		implements [mscorlib]System.IDisposable
	{
		.method public hidebysig specialname 
			instance void add_DownloadProgress(class [mscorlib]'System.EventHandler`1'<class liblinux.Services.RemoteCompilerDownloadProgressEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void remove_DownloadProgress(class [mscorlib]'System.EventHandler`1'<class liblinux.Services.RemoteCompilerDownloadProgressEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void add_BeforeCreateLocalSysroot(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void remove_BeforeCreateLocalSysroot(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void add_RsyncCleanupNeeded(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void remove_RsyncCleanupNeeded(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance class [mscorlib]System.Version get_Version()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance valuetype liblinux.Services.CompilerId get_CompilerId()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance string get_Archiver()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_Archiver(string 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance class [System.Core]'System.Collections.Generic.HashSet`1'<string> get_AdditionalIncludes()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance class [System.Core]'System.Collections.Generic.HashSet`1'<string> get_ExcludedDirectories()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance bool get_ForceUpdate()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_ForceUpdate(bool 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance string get_ArchiverArchiveCommandFormat()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_ArchiverArchiveCommandFormat(string 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance class [mscorlib]'System.Collections.Generic.List`1'<string> get_IncludeDirectories()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance bool get_UseCompiler()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_UseCompiler(bool 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance string get_TemporaryArchiveFileName()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_TemporaryArchiveFileName(string 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance valuetype liblinux.SystemPlatform get_Platform()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method family hidebysig specialname 
			instance void set_Platform(valuetype liblinux.SystemPlatform 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance valuetype [mscorlib]System.TimeSpan get_DownloadDuration()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance string get_LocalDestinationDirectory()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_LocalDestinationDirectory(string 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance string get_CCompilerIncludeDirectoriesQueryCommandFormat()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_CCompilerIncludeDirectoriesQueryCommandFormat(string 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance string get_CppCompilerIncludeDirectoriesQueryCommandFormat()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_CppCompilerIncludeDirectoriesQueryCommandFormat(string 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance string get_CCompilerIncludePreprocessorDefinitionsQueryCommandFormat()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_CCompilerIncludePreprocessorDefinitionsQueryCommandFormat(string 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance string get_CppCompilerPreprocessorDefinitionsQueryCommandFormat()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_CppCompilerPreprocessorDefinitionsQueryCommandFormat(string 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance string get_VCPkgLocationQueryCommandFormat()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_VCPkgLocationQueryCommandFormat(string 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance valuetype [mscorlib]System.TimeSpan get_DownloadProgressLogInterval()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_DownloadProgressLogInterval(valuetype [mscorlib]System.TimeSpan 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance int32 get_LongestPathLength()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_LongestPathLength(int32 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_HasLocalSysroot()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_LocalSysrootEnabled()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance valuetype [mscorlib]System.DateTime get_LastSysrootUpdate()
		{
		}
		.method public hidebysig specialname 
			instance int32 get_UpdateIntervalMinutes()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_UpdateIntervalMinutes(int32 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance bool get_Cancelled()
		{
			ret
		}
		.method public hidebysig specialname 
			instance string get_SyncMethod()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance class liblinux.Services.RemoteCompilerIOImplBase get_RemoteCompilerIO()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_RemoteCompilerIO(class liblinux.Services.RemoteCompilerIOImplBase 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance class liblinux.Services.RemoteCompilerDetail get_DefaultCompilerInfo()
		{
			ret
		}
		.method public hidebysig specialname 
			instance class liblinux.Services.RemoteCompilerDetail get_DefaultClangCompilerInfo()
		{
			ret
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance class 'liblinux.IO.Unified.IFileSystem`1'<class 'liblinux.IO.Unified.IDirectory'> get_FileSystem()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance class 'liblinux.Shell.Unified.ICommandHost`2'<class liblinux.Shell.ICommand,class liblinux.Shell.IAsyncCommand> get_Shell()
		{
		}
		.method public final virtual hidebysig newslot 
			instance void Dispose()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void Cleanup()
		{
			ret
		}
		.method public static hidebysig 
			void CleanupLocalSysroots()
		{
			ret
		}
		.method public static hidebysig 
			bool CheckIfLocalSysrootsNeedsCleanup()
		{
			ret
		}
		.method public static hidebysig 
			bool CheckIfLocalSysrootsNeedsCleanup(class liblinux.Persistence.ConnectionInfoStore store)
		{
			ret
		}
		.method public static hidebysig 
			string GetDefaultLocalSysrootsDirectory()
		{
			ret
		}
		.method family virtual hidebysig newslot abstract 
			instance string GetDefaultLocalDestinationDirectory()
		{
		}
		.method public virtual hidebysig newslot 
			instance void Cancel()
		{
			ret
		}
		.method public hidebysig 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<bool> CreateLocalSysrootAsync([opt] bool throwIfNoCompilerFound)
		{
			.param [1] = bool(false)
			ret
		}
		.method public virtual hidebysig newslot 
			instance void PurgeLocalSysroot()
		{
			ret
		}
		.method public hidebysig 
			instance void RetrieveCompilerInformation([opt] bool throwIfNoCompilerFound)
		{
			.param [1] = bool(false)
			ret
		}
		.method public static hidebysig 
			bool IsSysrootLocked(class liblinux.IConnectionInfo identifier)
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance class [mscorlib]System.Threading.Tasks.Task DownloadSysrootAsync()
		{
			ret
		}
		.method family virtual hidebysig newslot abstract 
			instance void Initialize()
		{
		}
		.method famorassem hidebysig 
			instance void OnProgress(string message, string source)
		{
			ret
		}
		.method famorassem hidebysig 
			instance void OnProgress(string message, string source, object[] args)
		{
			.param [3]
			.custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = { }
			ret
		}
		.method famorassem hidebysig 
			instance string TranslatePath(string remotePath)
		{
			ret
		}
		.method famorassem hidebysig 
			instance void ExpandRemotePaths(class [System.Core]'System.Collections.Generic.HashSet`1'<string> paths)
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void AddSettingsToWrite(class [System.Xml.Linq]System.Xml.Linq.XElement element)
		{
			ret
		}
		.method public hidebysig 
			instance void LoadPreprocessorDefinitions()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance string GetErrorOutput(class liblinux.Shell.ICommand command)
		{
			ret
		}
		.method family virtual hidebysig newslot abstract 
			instance void PersistSysrootInfo()
		{
		}
		.method family hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.field static famorassem initonly string 'GENERAL'
		.field family class liblinux.Services.RemoteCompilerIOImplBase ioImpl
		.field family string uniqueLockId
		.event [mscorlib]System.EventHandler BeforeCreateLocalSysroot
		{
			.addon instance void liblinux.Services.RemoteCompiler::add_BeforeCreateLocalSysroot(class [mscorlib]System.EventHandler)
			.removeon instance void liblinux.Services.RemoteCompiler::remove_BeforeCreateLocalSysroot(class [mscorlib]System.EventHandler)
		}
		.event class [mscorlib]'System.EventHandler`1'<class liblinux.Services.RemoteCompilerDownloadProgressEventArgs> DownloadProgress
		{
			.addon instance void liblinux.Services.RemoteCompiler::add_DownloadProgress(class [mscorlib]'System.EventHandler`1'<class liblinux.Services.RemoteCompilerDownloadProgressEventArgs>)
			.removeon instance void liblinux.Services.RemoteCompiler::remove_DownloadProgress(class [mscorlib]'System.EventHandler`1'<class liblinux.Services.RemoteCompilerDownloadProgressEventArgs>)
		}
		.event [mscorlib]System.EventHandler RsyncCleanupNeeded
		{
			.addon instance void liblinux.Services.RemoteCompiler::add_RsyncCleanupNeeded(class [mscorlib]System.EventHandler)
			.removeon instance void liblinux.Services.RemoteCompiler::remove_RsyncCleanupNeeded(class [mscorlib]System.EventHandler)
		}
		.property instance class [System.Core]'System.Collections.Generic.HashSet`1'<string> AdditionalIncludes()
		{
			.get instance class [System.Core]'System.Collections.Generic.HashSet`1'<string> liblinux.Services.RemoteCompiler::get_AdditionalIncludes()
		}
		.property instance string Archiver()
		{
			.get instance string liblinux.Services.RemoteCompiler::get_Archiver()
			.set instance void liblinux.Services.RemoteCompiler::set_Archiver(string)
		}
		.property instance string ArchiverArchiveCommandFormat()
		{
			.get instance string liblinux.Services.RemoteCompiler::get_ArchiverArchiveCommandFormat()
			.set instance void liblinux.Services.RemoteCompiler::set_ArchiverArchiveCommandFormat(string)
		}
		.property instance string CCompilerIncludeDirectoriesQueryCommandFormat()
		{
			.get instance string liblinux.Services.RemoteCompiler::get_CCompilerIncludeDirectoriesQueryCommandFormat()
			.set instance void liblinux.Services.RemoteCompiler::set_CCompilerIncludeDirectoriesQueryCommandFormat(string)
		}
		.property instance string CCompilerIncludePreprocessorDefinitionsQueryCommandFormat()
		{
			.get instance string liblinux.Services.RemoteCompiler::get_CCompilerIncludePreprocessorDefinitionsQueryCommandFormat()
			.set instance void liblinux.Services.RemoteCompiler::set_CCompilerIncludePreprocessorDefinitionsQueryCommandFormat(string)
		}
		.property instance bool Cancelled()
		{
			.get instance bool liblinux.Services.RemoteCompiler::get_Cancelled()
		}
		.property instance valuetype liblinux.Services.CompilerId CompilerId()
		{
			.get instance valuetype liblinux.Services.CompilerId liblinux.Services.RemoteCompiler::get_CompilerId()
		}
		.property instance string CppCompilerIncludeDirectoriesQueryCommandFormat()
		{
			.get instance string liblinux.Services.RemoteCompiler::get_CppCompilerIncludeDirectoriesQueryCommandFormat()
			.set instance void liblinux.Services.RemoteCompiler::set_CppCompilerIncludeDirectoriesQueryCommandFormat(string)
		}
		.property instance string CppCompilerPreprocessorDefinitionsQueryCommandFormat()
		{
			.get instance string liblinux.Services.RemoteCompiler::get_CppCompilerPreprocessorDefinitionsQueryCommandFormat()
			.set instance void liblinux.Services.RemoteCompiler::set_CppCompilerPreprocessorDefinitionsQueryCommandFormat(string)
		}
		.property instance class liblinux.Services.RemoteCompilerDetail DefaultClangCompilerInfo()
		{
			.get instance class liblinux.Services.RemoteCompilerDetail liblinux.Services.RemoteCompiler::get_DefaultClangCompilerInfo()
		}
		.property instance class liblinux.Services.RemoteCompilerDetail DefaultCompilerInfo()
		{
			.get instance class liblinux.Services.RemoteCompilerDetail liblinux.Services.RemoteCompiler::get_DefaultCompilerInfo()
		}
		.property instance valuetype [mscorlib]System.TimeSpan DownloadDuration()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.Services.RemoteCompiler::get_DownloadDuration()
		}
		.property instance valuetype [mscorlib]System.TimeSpan DownloadProgressLogInterval()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.Services.RemoteCompiler::get_DownloadProgressLogInterval()
			.set instance void liblinux.Services.RemoteCompiler::set_DownloadProgressLogInterval(valuetype [mscorlib]System.TimeSpan)
		}
		.property instance class [System.Core]'System.Collections.Generic.HashSet`1'<string> ExcludedDirectories()
		{
			.get instance class [System.Core]'System.Collections.Generic.HashSet`1'<string> liblinux.Services.RemoteCompiler::get_ExcludedDirectories()
		}
		.property instance class 'liblinux.IO.Unified.IFileSystem`1'<class 'liblinux.IO.Unified.IDirectory'> FileSystem()
		{
			.get instance class 'liblinux.IO.Unified.IFileSystem`1'<class 'liblinux.IO.Unified.IDirectory'> liblinux.Services.RemoteCompiler::get_FileSystem()
		}
		.property instance bool ForceUpdate()
		{
			.get instance bool liblinux.Services.RemoteCompiler::get_ForceUpdate()
			.set instance void liblinux.Services.RemoteCompiler::set_ForceUpdate(bool)
		}
		.property instance bool HasLocalSysroot()
		{
			.get instance bool liblinux.Services.RemoteCompiler::get_HasLocalSysroot()
		}
		.property instance class [mscorlib]'System.Collections.Generic.List`1'<string> IncludeDirectories()
		{
			.get instance class [mscorlib]'System.Collections.Generic.List`1'<string> liblinux.Services.RemoteCompiler::get_IncludeDirectories()
		}
		.property instance valuetype [mscorlib]System.DateTime LastSysrootUpdate()
		{
			.get instance valuetype [mscorlib]System.DateTime liblinux.Services.RemoteCompiler::get_LastSysrootUpdate()
		}
		.property instance string LocalDestinationDirectory()
		{
			.get instance string liblinux.Services.RemoteCompiler::get_LocalDestinationDirectory()
			.set instance void liblinux.Services.RemoteCompiler::set_LocalDestinationDirectory(string)
		}
		.property instance bool LocalSysrootEnabled()
		{
			.get instance bool liblinux.Services.RemoteCompiler::get_LocalSysrootEnabled()
		}
		.property instance int32 LongestPathLength()
		{
			.get instance int32 liblinux.Services.RemoteCompiler::get_LongestPathLength()
			.set instance void liblinux.Services.RemoteCompiler::set_LongestPathLength(int32)
		}
		.property instance valuetype liblinux.SystemPlatform Platform()
		{
			.get instance valuetype liblinux.SystemPlatform liblinux.Services.RemoteCompiler::get_Platform()
			.set instance void liblinux.Services.RemoteCompiler::set_Platform(valuetype liblinux.SystemPlatform)
		}
		.property instance class liblinux.Services.RemoteCompilerIOImplBase RemoteCompilerIO()
		{
			.get instance class liblinux.Services.RemoteCompilerIOImplBase liblinux.Services.RemoteCompiler::get_RemoteCompilerIO()
			.set instance void liblinux.Services.RemoteCompiler::set_RemoteCompilerIO(class liblinux.Services.RemoteCompilerIOImplBase)
		}
		.property instance class 'liblinux.Shell.Unified.ICommandHost`2'<class liblinux.Shell.ICommand,class liblinux.Shell.IAsyncCommand> Shell()
		{
			.get instance class 'liblinux.Shell.Unified.ICommandHost`2'<class liblinux.Shell.ICommand,class liblinux.Shell.IAsyncCommand> liblinux.Services.RemoteCompiler::get_Shell()
		}
		.property instance string SyncMethod()
		{
			.get instance string liblinux.Services.RemoteCompiler::get_SyncMethod()
		}
		.property instance string TemporaryArchiveFileName()
		{
			.get instance string liblinux.Services.RemoteCompiler::get_TemporaryArchiveFileName()
			.set instance void liblinux.Services.RemoteCompiler::set_TemporaryArchiveFileName(string)
		}
		.property instance int32 UpdateIntervalMinutes()
		{
			.get instance int32 liblinux.Services.RemoteCompiler::get_UpdateIntervalMinutes()
			.set instance void liblinux.Services.RemoteCompiler::set_UpdateIntervalMinutes(int32)
		}
		.property instance bool UseCompiler()
		{
			.get instance bool liblinux.Services.RemoteCompiler::get_UseCompiler()
			.set instance void liblinux.Services.RemoteCompiler::set_UseCompiler(bool)
		}
		.property instance string VCPkgLocationQueryCommandFormat()
		{
			.get instance string liblinux.Services.RemoteCompiler::get_VCPkgLocationQueryCommandFormat()
			.set instance void liblinux.Services.RemoteCompiler::set_VCPkgLocationQueryCommandFormat(string)
		}
		.property instance class [mscorlib]System.Version Version()
		{
			.get instance class [mscorlib]System.Version liblinux.Services.RemoteCompiler::get_Version()
		}
	}
	.class public RemoteCompilerDetail
		extends [mscorlib]System.Object
	{
		.method public hidebysig specialname 
			instance string get_CCompilerPath()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance string get_CppCompilerPath()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance string get_ResolvedCCompilerPath()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance string get_ResolvedCppCompilerPath()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance bool get_Installed()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance class [mscorlib]'System.Collections.Generic.List`1'<string> get_CCompilerIncludeDirectories()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance class [mscorlib]'System.Collections.Generic.List`1'<string> get_CppCompilerIncludeDirectories()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance class [mscorlib]'System.Collections.Generic.Dictionary`2'<string,string> get_CCompilerPreprocessorDefinitions()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance class [mscorlib]'System.Collections.Generic.Dictionary`2'<string,string> get_CppCompilerPreprocessorDefinitions()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance class [mscorlib]'System.Collections.Generic.List`1'<string> get_LocalCCompilerIncludeDirectories()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance class [mscorlib]'System.Collections.Generic.List`1'<string> get_LocalCppCompilerIncludeDirectories()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance string get_CIncludesPath()
		{
			ret
		}
		.method public hidebysig specialname 
			instance string get_CppIncludesPath()
		{
			ret
		}
		.method public hidebysig specialname 
			instance string get_CDefinesPath()
		{
			ret
		}
		.method public hidebysig specialname 
			instance string get_CppDefinesPath()
		{
			ret
		}
		.method public hidebysig specialname 
			instance bool get_IsGcc()
		{
			ret
		}
		.method public hidebysig specialname 
			instance bool get_IsClang()
		{
			ret
		}
		.property instance class [mscorlib]'System.Collections.Generic.List`1'<string> CCompilerIncludeDirectories()
		{
			.get instance class [mscorlib]'System.Collections.Generic.List`1'<string> liblinux.Services.RemoteCompilerDetail::get_CCompilerIncludeDirectories()
		}
		.property instance string CCompilerPath()
		{
			.get instance string liblinux.Services.RemoteCompilerDetail::get_CCompilerPath()
		}
		.property instance class [mscorlib]'System.Collections.Generic.Dictionary`2'<string,string> CCompilerPreprocessorDefinitions()
		{
			.get instance class [mscorlib]'System.Collections.Generic.Dictionary`2'<string,string> liblinux.Services.RemoteCompilerDetail::get_CCompilerPreprocessorDefinitions()
		}
		.property instance string CDefinesPath()
		{
			.get instance string liblinux.Services.RemoteCompilerDetail::get_CDefinesPath()
		}
		.property instance string CIncludesPath()
		{
			.get instance string liblinux.Services.RemoteCompilerDetail::get_CIncludesPath()
		}
		.property instance class [mscorlib]'System.Collections.Generic.List`1'<string> CppCompilerIncludeDirectories()
		{
			.get instance class [mscorlib]'System.Collections.Generic.List`1'<string> liblinux.Services.RemoteCompilerDetail::get_CppCompilerIncludeDirectories()
		}
		.property instance string CppCompilerPath()
		{
			.get instance string liblinux.Services.RemoteCompilerDetail::get_CppCompilerPath()
		}
		.property instance class [mscorlib]'System.Collections.Generic.Dictionary`2'<string,string> CppCompilerPreprocessorDefinitions()
		{
			.get instance class [mscorlib]'System.Collections.Generic.Dictionary`2'<string,string> liblinux.Services.RemoteCompilerDetail::get_CppCompilerPreprocessorDefinitions()
		}
		.property instance string CppDefinesPath()
		{
			.get instance string liblinux.Services.RemoteCompilerDetail::get_CppDefinesPath()
		}
		.property instance string CppIncludesPath()
		{
			.get instance string liblinux.Services.RemoteCompilerDetail::get_CppIncludesPath()
		}
		.property instance bool Installed()
		{
			.get instance bool liblinux.Services.RemoteCompilerDetail::get_Installed()
		}
		.property instance bool IsClang()
		{
			.get instance bool liblinux.Services.RemoteCompilerDetail::get_IsClang()
		}
		.property instance bool IsGcc()
		{
			.get instance bool liblinux.Services.RemoteCompilerDetail::get_IsGcc()
		}
		.property instance class [mscorlib]'System.Collections.Generic.List`1'<string> LocalCCompilerIncludeDirectories()
		{
			.get instance class [mscorlib]'System.Collections.Generic.List`1'<string> liblinux.Services.RemoteCompilerDetail::get_LocalCCompilerIncludeDirectories()
		}
		.property instance class [mscorlib]'System.Collections.Generic.List`1'<string> LocalCppCompilerIncludeDirectories()
		{
			.get instance class [mscorlib]'System.Collections.Generic.List`1'<string> liblinux.Services.RemoteCompilerDetail::get_LocalCppCompilerIncludeDirectories()
		}
		.property instance string ResolvedCCompilerPath()
		{
			.get instance string liblinux.Services.RemoteCompilerDetail::get_ResolvedCCompilerPath()
		}
		.property instance string ResolvedCppCompilerPath()
		{
			.get instance string liblinux.Services.RemoteCompilerDetail::get_ResolvedCppCompilerPath()
		}
	}
	.class public RemoteCompilerDownloadProgressEventArgs
		extends [mscorlib]System.EventArgs
	{
		.method public hidebysig specialname 
			instance string get_Message()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance string get_Source()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method famorassem hidebysig specialname 
			instance void .ctor(string message)
		{
			ret
		}
		.method famorassem hidebysig specialname 
			instance void .ctor(string message, string source)
		{
			ret
		}
		.property instance string Message()
		{
			.get instance string liblinux.Services.RemoteCompilerDownloadProgressEventArgs::get_Message()
		}
		.property instance string Source()
		{
			.get instance string liblinux.Services.RemoteCompilerDownloadProgressEventArgs::get_Source()
		}
	}
	.class public abstract RemoteCompilerIOImplBase
		extends [mscorlib]System.Object
		implements [mscorlib]System.IDisposable
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance string get_Id()
		{
		}
		.method public hidebysig specialname 
			instance class liblinux.Services.RemoteCompiler get_RemoteCompiler()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_RemoteCompiler(class liblinux.Services.RemoteCompiler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance int32 get_LongestPathLength()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_LongestPathLength(int32 'value')
		{
			ret
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance string[] get_FailedDirectories()
		{
		}
		.method family virtual hidebysig newslot abstract specialname 
			instance void set_FailedDirectories(string[] 'value')
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]System.Threading.Tasks.Task DownloadDirectoriesAsync(string[] remotePaths, string[] localPaths)
		{
		}
		.method public virtual hidebysig newslot 
			instance void Dispose()
		{
			ret
		}
		.method family hidebysig 
			instance void OnProgress(string message, string source, object[] args)
		{
			.param [3]
			.custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = { }
			ret
		}
		.method family hidebysig 
			instance void OnRsyncCleanup()
		{
			ret
		}
		.method family hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.property instance string[] FailedDirectories()
		{
			.get instance string[] liblinux.Services.RemoteCompilerIOImplBase::get_FailedDirectories()
			.set instance void liblinux.Services.RemoteCompilerIOImplBase::set_FailedDirectories(string[])
		}
		.property instance string Id()
		{
			.get instance string liblinux.Services.RemoteCompilerIOImplBase::get_Id()
		}
		.property instance int32 LongestPathLength()
		{
			.get instance int32 liblinux.Services.RemoteCompilerIOImplBase::get_LongestPathLength()
			.set instance void liblinux.Services.RemoteCompilerIOImplBase::set_LongestPathLength(int32)
		}
		.property instance class liblinux.Services.RemoteCompiler RemoteCompiler()
		{
			.get instance class liblinux.Services.RemoteCompiler liblinux.Services.RemoteCompilerIOImplBase::get_RemoteCompiler()
			.set instance void liblinux.Services.RemoteCompilerIOImplBase::set_RemoteCompiler(class liblinux.Services.RemoteCompiler)
		}
	}
	.class public RemoteCompilerLoggingSource
		extends [mscorlib]System.Object
	{
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.field static public initonly string 'GENERAL'
		.field static public initonly string 'RSYNC'
	}
	.class public RemoteLinuxCompiler
		extends liblinux.Services.RemoteCompiler
	{
		.method public hidebysig specialname 
			instance class liblinux.ConnectionInfo get_ConnectionInfo()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void set_ConnectionInfo(class liblinux.ConnectionInfo 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance class liblinux.UnixSystem get_RemoteSystem()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public virtual hidebysig specialname 
			instance class 'liblinux.IO.Unified.IFileSystem`1'<class 'liblinux.IO.Unified.IDirectory'> get_FileSystem()
		{
			ret
		}
		.method public virtual hidebysig specialname 
			instance class 'liblinux.Shell.Unified.ICommandHost`2'<class liblinux.Shell.ICommand,class liblinux.Shell.IAsyncCommand> get_Shell()
		{
			ret
		}
		.method public virtual hidebysig specialname 
			instance bool get_HasLocalSysroot()
		{
			ret
		}
		.method public virtual hidebysig specialname 
			instance bool get_LocalSysrootEnabled()
		{
			ret
		}
		.method public virtual hidebysig specialname 
			instance valuetype [mscorlib]System.DateTime get_LastSysrootUpdate()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.ConnectionInfo connectionInfo)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.Persistence.ConnectionInfoStore store, class liblinux.ConnectionInfo connectionInfo, class liblinux.Services.RemoteCompilerIOImplBase ioImpl)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(int32 connectionId)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string connectionName)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string connectionName, class liblinux.Services.RemoteCompilerIOImplBase ioImpl)
		{
			ret
		}
		.method family virtual hidebysig 
			instance void Cleanup()
		{
			ret
		}
		.method family virtual hidebysig 
			instance string GetDefaultLocalDestinationDirectory()
		{
			ret
		}
		.method public virtual hidebysig 
			instance void Cancel()
		{
			ret
		}
		.method public virtual hidebysig 
			instance void PurgeLocalSysroot()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void Initialize()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void AddSettingsToWrite(class [System.Xml.Linq]System.Xml.Linq.XElement element)
		{
			ret
		}
		.method family virtual hidebysig 
			instance void PersistSysrootInfo()
		{
			ret
		}
		.property instance class liblinux.ConnectionInfo ConnectionInfo()
		{
			.get instance class liblinux.ConnectionInfo liblinux.Services.RemoteLinuxCompiler::get_ConnectionInfo()
			.set instance void liblinux.Services.RemoteLinuxCompiler::set_ConnectionInfo(class liblinux.ConnectionInfo)
		}
		.property instance class 'liblinux.IO.Unified.IFileSystem`1'<class 'liblinux.IO.Unified.IDirectory'> FileSystem()
		{
			.get instance class 'liblinux.IO.Unified.IFileSystem`1'<class 'liblinux.IO.Unified.IDirectory'> liblinux.Services.RemoteLinuxCompiler::get_FileSystem()
		}
		.property instance bool HasLocalSysroot()
		{
			.get instance bool liblinux.Services.RemoteLinuxCompiler::get_HasLocalSysroot()
		}
		.property instance valuetype [mscorlib]System.DateTime LastSysrootUpdate()
		{
			.get instance valuetype [mscorlib]System.DateTime liblinux.Services.RemoteLinuxCompiler::get_LastSysrootUpdate()
		}
		.property instance bool LocalSysrootEnabled()
		{
			.get instance bool liblinux.Services.RemoteLinuxCompiler::get_LocalSysrootEnabled()
		}
		.property instance class liblinux.UnixSystem RemoteSystem()
		{
			.get instance class liblinux.UnixSystem liblinux.Services.RemoteLinuxCompiler::get_RemoteSystem()
		}
		.property instance class 'liblinux.Shell.Unified.ICommandHost`2'<class liblinux.Shell.ICommand,class liblinux.Shell.IAsyncCommand> Shell()
		{
			.get instance class 'liblinux.Shell.Unified.ICommandHost`2'<class liblinux.Shell.ICommand,class liblinux.Shell.IAsyncCommand> liblinux.Services.RemoteLinuxCompiler::get_Shell()
		}
	}
	.class public SftpRemoteCompilerIOImpl
		extends liblinux.Services.RemoteCompilerIOImplBase
	{
		.method public virtual hidebysig specialname 
			instance string get_Id()
		{
			ret
		}
		.method public virtual hidebysig specialname 
			instance string[] get_FailedDirectories()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method family virtual hidebysig specialname 
			instance void set_FailedDirectories(string[] 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public virtual hidebysig 
			instance class [mscorlib]System.Threading.Tasks.Task DownloadDirectoriesAsync(string[] remotePaths, string[] localPaths)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.property instance string[] FailedDirectories()
		{
			.get instance string[] liblinux.Services.SftpRemoteCompilerIOImpl::get_FailedDirectories()
			.set instance void liblinux.Services.SftpRemoteCompilerIOImpl::set_FailedDirectories(string[])
		}
		.property instance string Id()
		{
			.get instance string liblinux.Services.SftpRemoteCompilerIOImpl::get_Id()
		}
	}
}
.namespace liblinux.Shell
{
	.class public abstract CommandBase
		extends [mscorlib]System.Object
		implements liblinux.Shell.ICommand, [mscorlib]System.IDisposable
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance string get_CommandText()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void set_CommandText(string 'value')
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance int32 get_ExitCode()
		{
		}
		.method famorassem virtual hidebysig newslot abstract specialname 
			instance void set_ExitCode(int32 'value')
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance string get_Output()
		{
		}
		.method famorassem virtual hidebysig newslot abstract specialname 
			instance void set_Output(string 'value')
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance valuetype [mscorlib]System.TimeSpan get_Timeout()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void set_Timeout(valuetype [mscorlib]System.TimeSpan 'value')
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance valuetype [mscorlib]System.TimeSpan get_Elapsed()
		{
		}
		.method family virtual hidebysig newslot abstract specialname 
			instance void set_Elapsed(valuetype [mscorlib]System.TimeSpan 'value')
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_IsFaulted()
		{
		}
		.method family virtual hidebysig newslot abstract specialname 
			instance void set_IsFaulted(bool 'value')
		{
		}
		.method famorassem virtual hidebysig newslot 
			instance void Initialize<(liblinux.RemoteSystemBase, liblinux.IRemoteSystemBare) 'T'>(!!0 remoteSystem, object state)
		{
			ret
		}
		.method famorassem virtual hidebysig newslot abstract 
			instance void Run()
		{
		}
		.method famorassem virtual hidebysig newslot 
			instance void RunAsynchronously()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Dispose()
		{
			ret
		}
		.method family hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.property instance string CommandText()
		{
			.get instance string liblinux.Shell.CommandBase::get_CommandText()
			.set instance void liblinux.Shell.CommandBase::set_CommandText(string)
		}
		.property instance valuetype [mscorlib]System.TimeSpan Elapsed()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.Shell.CommandBase::get_Elapsed()
			.set instance void liblinux.Shell.CommandBase::set_Elapsed(valuetype [mscorlib]System.TimeSpan)
		}
		.property instance int32 ExitCode()
		{
			.get instance int32 liblinux.Shell.CommandBase::get_ExitCode()
			.set instance void liblinux.Shell.CommandBase::set_ExitCode(int32)
		}
		.property instance bool IsFaulted()
		{
			.get instance bool liblinux.Shell.CommandBase::get_IsFaulted()
			.set instance void liblinux.Shell.CommandBase::set_IsFaulted(bool)
		}
		.property instance string Output()
		{
			.get instance string liblinux.Shell.CommandBase::get_Output()
			.set instance void liblinux.Shell.CommandBase::set_Output(string)
		}
		.property instance valuetype [mscorlib]System.TimeSpan Timeout()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.Shell.CommandBase::get_Timeout()
			.set instance void liblinux.Shell.CommandBase::set_Timeout(valuetype [mscorlib]System.TimeSpan)
		}
	}
	.class public CommandHost
		extends [mscorlib]System.Object
		implements liblinux.Shell.ICommandHost, class 'liblinux.Shell.Unified.ICommandHost`2'<class liblinux.Shell.INonHostedCommand,class liblinux.Shell.NonHostedCommand>
	{
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.RemoteSystemBase remoteSystem)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class liblinux.Shell.INonHostedCommand ExecuteCommand(string commandText)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class liblinux.Shell.INonHostedCommand ExecuteCommand(string commandText, int32 timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class liblinux.Shell.INonHostedCommand ExecuteCommand(string commandText, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class liblinux.Shell.NonHostedCommand ExecuteCommandAsynchronously(string commandText)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class liblinux.Shell.NonHostedCommand ExecuteCommandAsynchronously(string commandText, int32 timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class liblinux.Shell.NonHostedCommand ExecuteCommandAsynchronously(string commandText, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance !!0 ExecuteCommand<.ctor (liblinux.Shell.CommandBase) 'CommandType'>(string commandText)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance !!0 ExecuteCommand<.ctor (liblinux.Shell.CommandBase) 'CommandType'>(string commandText, int32 timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance !!0 ExecuteCommand<.ctor (liblinux.Shell.CommandBase) 'CommandType'>(string commandText, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance !!1 ExecuteCommand<.ctor (liblinux.Shell.CommandBase, !!1) 'CommandType', (liblinux.Shell.ICommand) 'CommandInterfaceType'>(string commandText, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance !!0 ExecuteCommandAsynchronously<.ctor (liblinux.Shell.CommandBase, liblinux.Shell.IAsyncronousCommand) 'CommandType'>(string commandText)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance !!0 ExecuteCommandAsynchronously<.ctor (liblinux.Shell.CommandBase, liblinux.Shell.IAsyncronousCommand) 'CommandType'>(string commandText, int32 timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance !!0 ExecuteCommandAsynchronously<.ctor (liblinux.Shell.CommandBase, liblinux.Shell.IAsyncronousCommand) 'CommandType'>(string commandText, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance !!1 ExecuteCommandAsynchronously<.ctor (liblinux.Shell.CommandBase, !!1) 'CommandType', (liblinux.Shell.ICommand, liblinux.Shell.IAsyncronousCommand) 'CommandInterfaceType'>(string commandText, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance !!0 ExecuteCommand<(liblinux.Shell.CommandBase) 'CommandType'>(!!0 command)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance !!1 ExecuteCommand<(liblinux.Shell.CommandBase, !!1) 'CommandType', (liblinux.Shell.ICommand) 'CommandInterfaceType'>(!!0 command)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance !!0 ExecuteCommandAsynchronously<(liblinux.Shell.CommandBase, liblinux.Shell.IAsyncronousCommand) 'CommandType'>(!!0 command)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance !!1 ExecuteCommandAsynchronously<(liblinux.Shell.CommandBase, !!1) 'CommandType', (liblinux.Shell.IAsyncronousCommand) 'CommandInterfaceType'>(!!0 command)
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance !!0 CreateCommandInstance<(liblinux.Shell.CommandBase) 'CommandType'>()
		{
			ret
		}
	}
	.class public CommandingException
		extends liblinux.ExceptionBase
	{
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message, class [mscorlib]System.Exception innerException)
		{
			ret
		}
	}
	.class public abstract CommonCommandBase
		extends liblinux.Shell.CommandBase
		implements liblinux.Shell.ICommand, [mscorlib]System.IDisposable, liblinux.Shell.IAsyncronousCommand, liblinux.Shell.IAsyncCommand, liblinux.Shell.IStreamableCommand
	{
		.method family virtual hidebysig newslot specialname 
			instance class liblinux.IRemoteSystemBare get_RemoteSystem()
		{
			ret
		}
		.method family hidebysig specialname 
			instance bool get_RunningSyncronously()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string commandText)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string commandText, int32 timeout)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string commandText, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public virtual hidebysig 
			instance void Dispose()
		{
			ret
		}
		.method public virtual hidebysig specialname 
			instance string get_CommandText()
		{
			ret
		}
		.method public virtual hidebysig specialname 
			instance void set_CommandText(string 'value')
		{
			ret
		}
		.method public virtual hidebysig specialname 
			instance int32 get_ExitCode()
		{
			ret
		}
		.method famorassem virtual hidebysig specialname 
			instance void set_ExitCode(int32 'value')
		{
			ret
		}
		.method public virtual hidebysig specialname 
			instance string get_Output()
		{
			ret
		}
		.method famorassem virtual hidebysig specialname 
			instance void set_Output(string 'value')
		{
			ret
		}
		.method public virtual hidebysig specialname 
			instance valuetype [mscorlib]System.TimeSpan get_Timeout()
		{
			ret
		}
		.method public virtual hidebysig specialname 
			instance void set_Timeout(valuetype [mscorlib]System.TimeSpan 'value')
		{
			ret
		}
		.method public virtual hidebysig specialname 
			instance valuetype [mscorlib]System.TimeSpan get_Elapsed()
		{
			ret
		}
		.method family virtual hidebysig specialname 
			instance void set_Elapsed(valuetype [mscorlib]System.TimeSpan 'value')
		{
			ret
		}
		.method public virtual hidebysig specialname 
			instance bool get_IsFaulted()
		{
			ret
		}
		.method family virtual hidebysig specialname 
			instance void set_IsFaulted(bool 'value')
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void add_Finished(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void remove_Finished(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance bool get_Running()
		{
			ret
		}
		.method famorassem virtual hidebysig newslot specialname 
			instance void set_Running(bool 'value')
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance bool get_Completed()
		{
			ret
		}
		.method famorassem virtual hidebysig newslot specialname 
			instance void set_Completed(bool 'value')
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance bool get_Cancelled()
		{
			ret
		}
		.method family virtual hidebysig newslot specialname 
			instance void set_Cancelled(bool 'value')
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance bool get_IsTimeout()
		{
			ret
		}
		.method famorassem virtual hidebysig newslot specialname 
			instance void set_IsTimeout(bool 'value')
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance bool get_ThrowOnTimeout()
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance void set_ThrowOnTimeout(bool 'value')
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Wait()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance bool Wait(int32 timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance bool Wait(valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance class [mscorlib]System.Threading.Tasks.Task WaitAsync()
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<bool> WaitAsync(valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<bool> WaitAsync(valuetype [mscorlib]System.TimeSpan timeout, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Cancel()
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void add_OutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void remove_OutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void add_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void remove_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void add_ErrorOutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void remove_ErrorOutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void add_ErrorOutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void remove_ErrorOutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public virtual hidebysig newslot abstract 
			instance void Write(string text)
		{
		}
		.method public virtual hidebysig newslot specialname 
			instance bool get_Queue()
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance void set_Queue(bool 'value')
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance valuetype [mscorlib]System.TimeSpan get_QueueTimeout()
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance void set_QueueTimeout(valuetype [mscorlib]System.TimeSpan 'value')
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance valuetype [mscorlib]System.TimeSpan get_QueueInterval()
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance void set_QueueInterval(valuetype [mscorlib]System.TimeSpan 'value')
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance int32 get_QueueSize()
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance void set_QueueSize(int32 'value')
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance bool get_DelayOutputReceiveEvents()
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance void set_DelayOutputReceiveEvents(bool 'value')
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance bool get_AccumulateOutput()
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance void set_AccumulateOutput(bool 'value')
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void BeginOutputRead()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void BeginErrorOutputRead()
		{
			ret
		}
		.method famorassem virtual hidebysig 
			instance void Initialize<(liblinux.RemoteSystemBase, liblinux.IRemoteSystemBare) 'T'>(!!0 remoteSystem, object state)
		{
			ret
		}
		.method famorassem virtual hidebysig 
			instance void Run()
		{
			ret
		}
		.method famorassem virtual hidebysig 
			instance void RunAsynchronously()
		{
			ret
		}
		.method family virtual hidebysig newslot abstract 
			instance void ThrowIfFaulted()
		{
		}
		.method family virtual hidebysig newslot 
			instance void Interrupt()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnRun()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnRunAsynchronously()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnBeforeStart()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnStart()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnTick()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnTick(bool asynchronousContext)
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnTimeout()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnTimeout(bool asynchronousContext)
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnTimeoutAsynchronously()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnTimeoutAsynchronously(bool asynchronousContext)
		{
			ret
		}
		.method famorassem virtual hidebysig newslot 
			instance void OnFinishedSuccessfully()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnEnd()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnEndAsynchronously()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnDisconnected()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnOutputReceived(string text)
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnErrorOutputReceived(string text)
		{
			ret
		}
		.method family hidebysig 
			instance void EnsureCommandNotRunning()
		{
			ret
		}
		.method family hidebysig 
			instance bool HasOutputLineReceivedEventHandlers()
		{
			ret
		}
		.method family hidebysig 
			instance bool HasErrorOutputLineReceivedEventHandlers()
		{
			ret
		}
		.method family hidebysig 
			instance void NotifyOutputReceived(object sender, string text)
		{
			ret
		}
		.method family hidebysig 
			instance void NotifyOutputLineReceived(object sender, string text)
		{
			ret
		}
		.method family hidebysig 
			instance void NotifyErrorOutputReceived(object sender, string text)
		{
			ret
		}
		.method family hidebysig 
			instance void NotifyErrorOutputLineReceived(object sender, string text)
		{
			ret
		}
		.method family hidebysig 
			instance void NotifyCommandFinished(object sender)
		{
			ret
		}
		.event class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> ErrorOutputLineReceived
		{
			.addon instance void liblinux.Shell.CommonCommandBase::add_ErrorOutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
			.removeon instance void liblinux.Shell.CommonCommandBase::remove_ErrorOutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
		}
		.event class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> ErrorOutputReceived
		{
			.addon instance void liblinux.Shell.CommonCommandBase::add_ErrorOutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
			.removeon instance void liblinux.Shell.CommonCommandBase::remove_ErrorOutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
		}
		.event [mscorlib]System.EventHandler Finished
		{
			.addon instance void liblinux.Shell.CommonCommandBase::add_Finished(class [mscorlib]System.EventHandler)
			.removeon instance void liblinux.Shell.CommonCommandBase::remove_Finished(class [mscorlib]System.EventHandler)
		}
		.event class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> OutputLineReceived
		{
			.addon instance void liblinux.Shell.CommonCommandBase::add_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
			.removeon instance void liblinux.Shell.CommonCommandBase::remove_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
		}
		.event class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> OutputReceived
		{
			.addon instance void liblinux.Shell.CommonCommandBase::add_OutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
			.removeon instance void liblinux.Shell.CommonCommandBase::remove_OutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
		}
		.property instance bool AccumulateOutput()
		{
			.get instance bool liblinux.Shell.CommonCommandBase::get_AccumulateOutput()
			.set instance void liblinux.Shell.CommonCommandBase::set_AccumulateOutput(bool)
		}
		.property instance bool Cancelled()
		{
			.get instance bool liblinux.Shell.CommonCommandBase::get_Cancelled()
			.set instance void liblinux.Shell.CommonCommandBase::set_Cancelled(bool)
		}
		.property instance string CommandText()
		{
			.get instance string liblinux.Shell.CommonCommandBase::get_CommandText()
			.set instance void liblinux.Shell.CommonCommandBase::set_CommandText(string)
		}
		.property instance bool Completed()
		{
			.get instance bool liblinux.Shell.CommonCommandBase::get_Completed()
			.set instance void liblinux.Shell.CommonCommandBase::set_Completed(bool)
		}
		.property instance bool DelayOutputReceiveEvents()
		{
			.get instance bool liblinux.Shell.CommonCommandBase::get_DelayOutputReceiveEvents()
			.set instance void liblinux.Shell.CommonCommandBase::set_DelayOutputReceiveEvents(bool)
		}
		.property instance valuetype [mscorlib]System.TimeSpan Elapsed()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.Shell.CommonCommandBase::get_Elapsed()
			.set instance void liblinux.Shell.CommonCommandBase::set_Elapsed(valuetype [mscorlib]System.TimeSpan)
		}
		.property instance int32 ExitCode()
		{
			.get instance int32 liblinux.Shell.CommonCommandBase::get_ExitCode()
			.set instance void liblinux.Shell.CommonCommandBase::set_ExitCode(int32)
		}
		.property instance bool IsFaulted()
		{
			.get instance bool liblinux.Shell.CommonCommandBase::get_IsFaulted()
			.set instance void liblinux.Shell.CommonCommandBase::set_IsFaulted(bool)
		}
		.property instance bool IsTimeout()
		{
			.get instance bool liblinux.Shell.CommonCommandBase::get_IsTimeout()
			.set instance void liblinux.Shell.CommonCommandBase::set_IsTimeout(bool)
		}
		.property instance string Output()
		{
			.get instance string liblinux.Shell.CommonCommandBase::get_Output()
			.set instance void liblinux.Shell.CommonCommandBase::set_Output(string)
		}
		.property instance bool Queue()
		{
			.get instance bool liblinux.Shell.CommonCommandBase::get_Queue()
			.set instance void liblinux.Shell.CommonCommandBase::set_Queue(bool)
		}
		.property instance valuetype [mscorlib]System.TimeSpan QueueInterval()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.Shell.CommonCommandBase::get_QueueInterval()
			.set instance void liblinux.Shell.CommonCommandBase::set_QueueInterval(valuetype [mscorlib]System.TimeSpan)
		}
		.property instance int32 QueueSize()
		{
			.get instance int32 liblinux.Shell.CommonCommandBase::get_QueueSize()
			.set instance void liblinux.Shell.CommonCommandBase::set_QueueSize(int32)
		}
		.property instance valuetype [mscorlib]System.TimeSpan QueueTimeout()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.Shell.CommonCommandBase::get_QueueTimeout()
			.set instance void liblinux.Shell.CommonCommandBase::set_QueueTimeout(valuetype [mscorlib]System.TimeSpan)
		}
		.property instance class liblinux.IRemoteSystemBare RemoteSystem()
		{
			.get instance class liblinux.IRemoteSystemBare liblinux.Shell.CommonCommandBase::get_RemoteSystem()
		}
		.property instance bool Running()
		{
			.get instance bool liblinux.Shell.CommonCommandBase::get_Running()
			.set instance void liblinux.Shell.CommonCommandBase::set_Running(bool)
		}
		.property instance bool RunningSyncronously()
		{
			.get instance bool liblinux.Shell.CommonCommandBase::get_RunningSyncronously()
		}
		.property instance bool ThrowOnTimeout()
		{
			.get instance bool liblinux.Shell.CommonCommandBase::get_ThrowOnTimeout()
			.set instance void liblinux.Shell.CommonCommandBase::set_ThrowOnTimeout(bool)
		}
		.property instance valuetype [mscorlib]System.TimeSpan Timeout()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.Shell.CommonCommandBase::get_Timeout()
			.set instance void liblinux.Shell.CommonCommandBase::set_Timeout(valuetype [mscorlib]System.TimeSpan)
		}
	}
	.class public interface abstract IAsyncCommand
		implements liblinux.Shell.ICommand, [mscorlib]System.IDisposable
	{
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]System.Threading.Tasks.Task WaitAsync()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<bool> WaitAsync(valuetype [mscorlib]System.TimeSpan timeout)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<bool> WaitAsync(valuetype [mscorlib]System.TimeSpan timeout, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void add_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void remove_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void add_ErrorOutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void remove_ErrorOutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract 
			instance void BeginOutputRead()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void BeginErrorOutputRead()
		{
		}
		.event class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> ErrorOutputLineReceived
		{
			.addon instance void liblinux.Shell.IAsyncCommand::add_ErrorOutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
			.removeon instance void liblinux.Shell.IAsyncCommand::remove_ErrorOutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
		}
		.event class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> OutputLineReceived
		{
			.addon instance void liblinux.Shell.IAsyncCommand::add_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
			.removeon instance void liblinux.Shell.IAsyncCommand::remove_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
		}
	}
	.class public interface abstract IAsyncronousCommand
		implements liblinux.Shell.IAsyncCommand, liblinux.Shell.ICommand, [mscorlib]System.IDisposable
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance void add_Finished(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void remove_Finished(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_Running()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_Completed()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_Cancelled()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_IsTimeout()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_ThrowOnTimeout()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void set_ThrowOnTimeout(bool 'value')
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Wait()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool Wait(int32 timeout)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool Wait(valuetype [mscorlib]System.TimeSpan timeout)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Cancel()
		{
		}
		.event [mscorlib]System.EventHandler Finished
		{
			.addon instance void liblinux.Shell.IAsyncronousCommand::add_Finished(class [mscorlib]System.EventHandler)
			.removeon instance void liblinux.Shell.IAsyncronousCommand::remove_Finished(class [mscorlib]System.EventHandler)
		}
		.property instance bool Cancelled()
		{
			.get instance bool liblinux.Shell.IAsyncronousCommand::get_Cancelled()
		}
		.property instance bool Completed()
		{
			.get instance bool liblinux.Shell.IAsyncronousCommand::get_Completed()
		}
		.property instance bool IsTimeout()
		{
			.get instance bool liblinux.Shell.IAsyncronousCommand::get_IsTimeout()
		}
		.property instance bool Running()
		{
			.get instance bool liblinux.Shell.IAsyncronousCommand::get_Running()
		}
		.property instance bool ThrowOnTimeout()
		{
			.get instance bool liblinux.Shell.IAsyncronousCommand::get_ThrowOnTimeout()
			.set instance void liblinux.Shell.IAsyncronousCommand::set_ThrowOnTimeout(bool)
		}
	}
	.class public interface abstract ICommand
		implements [mscorlib]System.IDisposable
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance string get_CommandText()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void set_CommandText(string 'value')
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance int32 get_ExitCode()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance string get_Output()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance valuetype [mscorlib]System.TimeSpan get_Timeout()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void set_Timeout(valuetype [mscorlib]System.TimeSpan 'value')
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance valuetype [mscorlib]System.TimeSpan get_Elapsed()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_IsFaulted()
		{
		}
		.property instance string CommandText()
		{
			.get instance string liblinux.Shell.ICommand::get_CommandText()
			.set instance void liblinux.Shell.ICommand::set_CommandText(string)
		}
		.property instance valuetype [mscorlib]System.TimeSpan Elapsed()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.Shell.ICommand::get_Elapsed()
		}
		.property instance int32 ExitCode()
		{
			.get instance int32 liblinux.Shell.ICommand::get_ExitCode()
		}
		.property instance bool IsFaulted()
		{
			.get instance bool liblinux.Shell.ICommand::get_IsFaulted()
		}
		.property instance string Output()
		{
			.get instance string liblinux.Shell.ICommand::get_Output()
		}
		.property instance valuetype [mscorlib]System.TimeSpan Timeout()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.Shell.ICommand::get_Timeout()
			.set instance void liblinux.Shell.ICommand::set_Timeout(valuetype [mscorlib]System.TimeSpan)
		}
	}
	.class public interface abstract ICommandHost
		implements class 'liblinux.Shell.Unified.ICommandHost`2'<class liblinux.Shell.INonHostedCommand,class liblinux.Shell.NonHostedCommand>
	{
		.method public virtual hidebysig newslot abstract 
			instance class liblinux.Shell.INonHostedCommand ExecuteCommand(string commandText)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class liblinux.Shell.NonHostedCommand ExecuteCommandAsynchronously(string commandText)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class liblinux.Shell.NonHostedCommand ExecuteCommandAsynchronously(string commandText, int32 timeout)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class liblinux.Shell.NonHostedCommand ExecuteCommandAsynchronously(string commandText, valuetype [mscorlib]System.TimeSpan timeout)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance !!0 ExecuteCommand<.ctor (liblinux.Shell.CommandBase) 'CommandType'>(string commandText)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance !!0 ExecuteCommand<.ctor (liblinux.Shell.CommandBase) 'CommandType'>(string commandText, int32 timeout)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance !!0 ExecuteCommand<.ctor (liblinux.Shell.CommandBase) 'CommandType'>(string commandText, valuetype [mscorlib]System.TimeSpan timeout)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance !!1 ExecuteCommand<.ctor (liblinux.Shell.CommandBase, !!1) 'CommandType', (liblinux.Shell.ICommand) 'CommandInterfaceType'>(string commandText, valuetype [mscorlib]System.TimeSpan timeout)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance !!0 ExecuteCommandAsynchronously<.ctor (liblinux.Shell.CommandBase, liblinux.Shell.IAsyncronousCommand) 'CommandType'>(string commandText)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance !!0 ExecuteCommandAsynchronously<.ctor (liblinux.Shell.CommandBase, liblinux.Shell.IAsyncronousCommand) 'CommandType'>(string commandText, int32 timeout)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance !!0 ExecuteCommandAsynchronously<.ctor (liblinux.Shell.CommandBase, liblinux.Shell.IAsyncronousCommand) 'CommandType'>(string commandText, valuetype [mscorlib]System.TimeSpan timeout)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance !!1 ExecuteCommandAsynchronously<.ctor (liblinux.Shell.CommandBase, !!1) 'CommandType', (liblinux.Shell.ICommand, liblinux.Shell.IAsyncronousCommand) 'CommandInterfaceType'>(string commandText, valuetype [mscorlib]System.TimeSpan timeout)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance !!0 ExecuteCommand<(liblinux.Shell.CommandBase) 'CommandType'>(!!0 command)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance !!1 ExecuteCommand<(liblinux.Shell.CommandBase, !!1) 'CommandType', (liblinux.Shell.ICommand) 'CommandInterfaceType'>(!!0 command)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance !!0 ExecuteCommandAsynchronously<(liblinux.Shell.CommandBase, liblinux.Shell.IAsyncronousCommand) 'CommandType'>(!!0 command)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance !!1 ExecuteCommandAsynchronously<(liblinux.Shell.CommandBase, !!1) 'CommandType', (liblinux.Shell.IAsyncronousCommand) 'CommandInterfaceType'>(!!0 command)
		{
		}
	}
	.class public interface abstract ICommandingShell
		implements liblinux.Shell.ICommandHost, class 'liblinux.Shell.Unified.ICommandHost`2'<class liblinux.Shell.INonHostedCommand,class liblinux.Shell.NonHostedCommand>
	{
		.method public virtual hidebysig newslot abstract 
			instance class liblinux.Shell.IShell Open()
		{
		}
	}
	.class public interface abstract IHostedCommand
		implements liblinux.Shell.ICommand, [mscorlib]System.IDisposable
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance class liblinux.Shell.IShell get_Shell()
		{
		}
		.property instance class liblinux.Shell.IShell Shell()
		{
			.get instance class liblinux.Shell.IShell liblinux.Shell.IHostedCommand::get_Shell()
		}
	}
	.class public interface abstract INonHostedCommand
		implements liblinux.Shell.ICommand, [mscorlib]System.IDisposable
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance string get_ErrorOutput()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_RedirectErrorOutputToOutput()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void set_RedirectErrorOutputToOutput(bool 'value')
		{
		}
		.property instance string ErrorOutput()
		{
			.get instance string liblinux.Shell.INonHostedCommand::get_ErrorOutput()
		}
		.property instance bool RedirectErrorOutputToOutput()
		{
			.get instance bool liblinux.Shell.INonHostedCommand::get_RedirectErrorOutputToOutput()
			.set instance void liblinux.Shell.INonHostedCommand::set_RedirectErrorOutputToOutput(bool)
		}
	}
	.class public interface abstract IShell
		implements liblinux.Shell.IStreamingShell, [mscorlib]System.IDisposable, liblinux.Shell.IShellStream
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance class liblinux.Shell.ShellStream get_InputStream()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance class liblinux.Shell.ShellStream get_OutputStream()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Close()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Write(uint8[] buffer, int32 offset, int32 count)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Write(string text)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void WriteLine(string line)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Flush()
		{
		}
		.property instance class liblinux.Shell.ShellStream InputStream()
		{
			.get instance class liblinux.Shell.ShellStream liblinux.Shell.IShell::get_InputStream()
		}
		.property instance class liblinux.Shell.ShellStream OutputStream()
		{
			.get instance class liblinux.Shell.ShellStream liblinux.Shell.IShell::get_OutputStream()
		}
	}
	.class public interface abstract IShellStream
		implements [mscorlib]System.IDisposable
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_Blocking()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void set_Blocking(bool 'value')
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance int64 get_Length()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_Eof()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance int32 Read(uint8[] buffer, int32 offset, int32 count)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<int32> ReadAsync(uint8[] buffer, int32 offset, int32 count)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance int32 Read(char[] chars, int32 offset, int32 count)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<int32> ReadAsync(char[] chars, int32 offset, int32 count)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance string ReadLine()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<string> ReadLineAsync()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance string ReadToEnd()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<string> ReadToEndAsync()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void WaitOutputReceived()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool WaitOutputReceived(valuetype [mscorlib]System.TimeSpan timeout)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<bool> WaitOutputReceivedAsync(valuetype [mscorlib]System.TimeSpan timeout, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void WaitOutputLineReceived()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool WaitOutputLineReceived(valuetype [mscorlib]System.TimeSpan timeout)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<bool> WaitOutputLineReceivedAsync(valuetype [mscorlib]System.TimeSpan timeout, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool WaitOutputReceived(string text, valuetype [mscorlib]System.TimeSpan timeout)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool WaitOutputReceived(class [System]System.Text.RegularExpressions.Regex regex, valuetype [mscorlib]System.TimeSpan timeout)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool WaitOutputLineReceived(string text, valuetype liblinux.Shell.LineMatchMode mode, valuetype [mscorlib]System.TimeSpan timeout)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool WaitOutputLineReceived(string text, valuetype liblinux.Shell.LineMatchMode mode, valuetype [mscorlib]System.TimeSpan timeout, [out] string& line)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool WaitOutputLineReceived(class [System]System.Text.RegularExpressions.Regex regex, valuetype [mscorlib]System.TimeSpan timeout)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool WaitOutputLineReceived(class [System]System.Text.RegularExpressions.Regex regex, valuetype [mscorlib]System.TimeSpan timeout, [out] string& line)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Write(uint8[] buffer, int32 offset, int32 count)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Write(char[] chars, int32 offset, int32 count)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Write(string text)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Write(string format, object[] args)
		{
			.param [2]
			.custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract 
			instance void WriteLine(string line)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void WriteLine(string format, object[] args)
		{
			.param [2]
			.custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract 
			instance void Flush()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Close()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Clear()
		{
		}
		.property instance bool Blocking()
		{
			.get instance bool liblinux.Shell.IShellStream::get_Blocking()
			.set instance void liblinux.Shell.IShellStream::set_Blocking(bool)
		}
		.property instance bool Eof()
		{
			.get instance bool liblinux.Shell.IShellStream::get_Eof()
		}
		.property instance int64 Length()
		{
			.get instance int64 liblinux.Shell.IShellStream::get_Length()
		}
	}
	.class public interface abstract IStreamableCommand
		implements liblinux.Shell.ICommand, [mscorlib]System.IDisposable
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance void add_OutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void remove_OutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void add_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void remove_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void add_ErrorOutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void remove_ErrorOutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void add_ErrorOutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void remove_ErrorOutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract 
			instance void Write(string text)
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_Completed()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_DelayOutputReceiveEvents()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void set_DelayOutputReceiveEvents(bool 'value')
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_Queue()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void set_Queue(bool 'value')
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance valuetype [mscorlib]System.TimeSpan get_QueueTimeout()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void set_QueueTimeout(valuetype [mscorlib]System.TimeSpan 'value')
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance valuetype [mscorlib]System.TimeSpan get_QueueInterval()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void set_QueueInterval(valuetype [mscorlib]System.TimeSpan 'value')
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance int32 get_QueueSize()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void set_QueueSize(int32 'value')
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_AccumulateOutput()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void set_AccumulateOutput(bool 'value')
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void BeginOutputRead()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void BeginErrorOutputRead()
		{
		}
		.event class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> ErrorOutputLineReceived
		{
			.addon instance void liblinux.Shell.IStreamableCommand::add_ErrorOutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
			.removeon instance void liblinux.Shell.IStreamableCommand::remove_ErrorOutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
		}
		.event class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> ErrorOutputReceived
		{
			.addon instance void liblinux.Shell.IStreamableCommand::add_ErrorOutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
			.removeon instance void liblinux.Shell.IStreamableCommand::remove_ErrorOutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
		}
		.event class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> OutputLineReceived
		{
			.addon instance void liblinux.Shell.IStreamableCommand::add_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
			.removeon instance void liblinux.Shell.IStreamableCommand::remove_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
		}
		.event class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> OutputReceived
		{
			.addon instance void liblinux.Shell.IStreamableCommand::add_OutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
			.removeon instance void liblinux.Shell.IStreamableCommand::remove_OutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
		}
		.property instance bool AccumulateOutput()
		{
			.get instance bool liblinux.Shell.IStreamableCommand::get_AccumulateOutput()
			.set instance void liblinux.Shell.IStreamableCommand::set_AccumulateOutput(bool)
		}
		.property instance bool Completed()
		{
			.get instance bool liblinux.Shell.IStreamableCommand::get_Completed()
		}
		.property instance bool DelayOutputReceiveEvents()
		{
			.get instance bool liblinux.Shell.IStreamableCommand::get_DelayOutputReceiveEvents()
			.set instance void liblinux.Shell.IStreamableCommand::set_DelayOutputReceiveEvents(bool)
		}
		.property instance bool Queue()
		{
			.get instance bool liblinux.Shell.IStreamableCommand::get_Queue()
			.set instance void liblinux.Shell.IStreamableCommand::set_Queue(bool)
		}
		.property instance valuetype [mscorlib]System.TimeSpan QueueInterval()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.Shell.IStreamableCommand::get_QueueInterval()
			.set instance void liblinux.Shell.IStreamableCommand::set_QueueInterval(valuetype [mscorlib]System.TimeSpan)
		}
		.property instance int32 QueueSize()
		{
			.get instance int32 liblinux.Shell.IStreamableCommand::get_QueueSize()
			.set instance void liblinux.Shell.IStreamableCommand::set_QueueSize(int32)
		}
		.property instance valuetype [mscorlib]System.TimeSpan QueueTimeout()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.Shell.IStreamableCommand::get_QueueTimeout()
			.set instance void liblinux.Shell.IStreamableCommand::set_QueueTimeout(valuetype [mscorlib]System.TimeSpan)
		}
	}
	.class public interface abstract IStreamingShell
		implements [mscorlib]System.IDisposable
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance void add_Opened(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void remove_Opened(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void add_Disconnected(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void remove_Disconnected(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void add_OutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void remove_OutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void add_ErrorOccured(class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void remove_ErrorOccured(class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void add_Closed(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void remove_Closed(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_IsOpen()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_DelayOutputReceiveEvents()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void set_DelayOutputReceiveEvents(bool 'value')
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Open(class liblinux.RemoteSystemBase remoteSystem)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Open(class liblinux.RemoteSystemBase remoteSystem, class liblinux.Shell.ShellCreateOptions options)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void BeginOutputRead()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Write(uint8[] buffer, int32 offset, int32 count)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Write(string text)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void WriteLine(string line)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Close()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void Flush()
		{
		}
		.event [mscorlib]System.EventHandler Closed
		{
			.addon instance void liblinux.Shell.IStreamingShell::add_Closed(class [mscorlib]System.EventHandler)
			.removeon instance void liblinux.Shell.IStreamingShell::remove_Closed(class [mscorlib]System.EventHandler)
		}
		.event [mscorlib]System.EventHandler Disconnected
		{
			.addon instance void liblinux.Shell.IStreamingShell::add_Disconnected(class [mscorlib]System.EventHandler)
			.removeon instance void liblinux.Shell.IStreamingShell::remove_Disconnected(class [mscorlib]System.EventHandler)
		}
		.event class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs> ErrorOccured
		{
			.addon instance void liblinux.Shell.IStreamingShell::add_ErrorOccured(class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs>)
			.removeon instance void liblinux.Shell.IStreamingShell::remove_ErrorOccured(class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs>)
		}
		.event [mscorlib]System.EventHandler Opened
		{
			.addon instance void liblinux.Shell.IStreamingShell::add_Opened(class [mscorlib]System.EventHandler)
			.removeon instance void liblinux.Shell.IStreamingShell::remove_Opened(class [mscorlib]System.EventHandler)
		}
		.event class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> OutputReceived
		{
			.addon instance void liblinux.Shell.IStreamingShell::add_OutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
			.removeon instance void liblinux.Shell.IStreamingShell::remove_OutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
		}
		.property instance bool DelayOutputReceiveEvents()
		{
			.get instance bool liblinux.Shell.IStreamingShell::get_DelayOutputReceiveEvents()
			.set instance void liblinux.Shell.IStreamingShell::set_DelayOutputReceiveEvents(bool)
		}
		.property instance bool IsOpen()
		{
			.get instance bool liblinux.Shell.IStreamingShell::get_IsOpen()
		}
	}
	.class public sealed LineMatchMode
		extends [mscorlib]System.Enum
	{
		.field public rtspecialname specialname int32 value__
		.field static public literal valuetype liblinux.Shell.LineMatchMode Contains = int32(0x00000000)
		.field static public literal valuetype liblinux.Shell.LineMatchMode Equals = int32(0x00000001)
	}
	.class public sealed NonHostedCommand
		extends liblinux.Shell.CommonCommandBase
		implements liblinux.Shell.INonHostedCommand, liblinux.Shell.ICommand, [mscorlib]System.IDisposable, liblinux.Shell.IAsyncronousCommand, liblinux.Shell.IAsyncCommand, liblinux.Shell.IStreamableCommand
	{
		.method public final virtual hidebysig newslot specialname 
			instance string get_ErrorOutput()
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance bool get_RedirectErrorOutputToOutput()
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void set_RedirectErrorOutputToOutput(bool 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string commandText)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string commandText, int32 timeout)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string commandText, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public virtual hidebysig 
			instance void Dispose()
		{
			ret
		}
		.method public virtual hidebysig 
			instance void Write(string text)
		{
			ret
		}
		.method public virtual hidebysig 
			instance void BeginOutputRead()
		{
			ret
		}
		.method public virtual hidebysig 
			instance void BeginErrorOutputRead()
		{
			ret
		}
		.method public virtual hidebysig 
			instance void Cancel()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void Interrupt()
		{
			ret
		}
		.method famorassem virtual hidebysig 
			instance void Initialize<(liblinux.RemoteSystemBase, liblinux.IRemoteSystemBare) 'T'>(!!0 remoteSystem, object state)
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnDisconnected()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnRun()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnRunAsynchronously()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnEndAsynchronously()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnOutputReceived(string text)
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnErrorOutputReceived(string text)
		{
			ret
		}
		.method family virtual hidebysig 
			instance void ThrowIfFaulted()
		{
			ret
		}
		.property instance string ErrorOutput()
		{
			.get instance string liblinux.Shell.NonHostedCommand::get_ErrorOutput()
		}
		.property instance bool RedirectErrorOutputToOutput()
		{
			.get instance bool liblinux.Shell.NonHostedCommand::get_RedirectErrorOutputToOutput()
			.set instance void liblinux.Shell.NonHostedCommand::set_RedirectErrorOutputToOutput(bool)
		}
	}
	.class public OutputReceivedEventArgs
		extends [mscorlib]System.EventArgs
	{
		.method public hidebysig specialname 
			instance string get_Output()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string output)
		{
			ret
		}
		.property instance string Output()
		{
			.get instance string liblinux.Shell.OutputReceivedEventArgs::get_Output()
		}
	}
	.class public abstract SecurityContext
		extends [mscorlib]System.Object
	{
		.method family hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
	}
	.class public Shell
		extends liblinux.Shell.StreamingShell
		implements liblinux.Shell.IShell, liblinux.Shell.IStreamingShell, [mscorlib]System.IDisposable, liblinux.Shell.IShellStream
	{
		.method public final virtual hidebysig newslot specialname 
			instance class liblinux.Shell.ShellStream get_InputStream()
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance class liblinux.Shell.ShellStream get_OutputStream()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.Shell.StreamingShell shell)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.RemoteSystemBase remoteSystem)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.RemoteSystemBase remoteSystem, class liblinux.Shell.ShellCreateOptions options)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Open(class liblinux.Shell.StreamingShell shell)
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance bool get_Eof()
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance bool get_Blocking()
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance void set_Blocking(bool 'value')
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance int64 get_Length()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance int32 Read(uint8[] buffer, int32 offset, int32 count)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<int32> ReadAsync(uint8[] buffer, int32 offset, int32 count)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance int32 Read(char[] chars, int32 offset, int32 count)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<int32> ReadAsync(char[] chars, int32 offset, int32 count)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance string ReadLine()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<string> ReadLineAsync()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance string ReadToEnd()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<string> ReadToEndAsync()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void WaitOutputReceived()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance bool WaitOutputReceived(valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<bool> WaitOutputReceivedAsync(valuetype [mscorlib]System.TimeSpan timeout, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance bool WaitOutputReceived(string text, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance bool WaitOutputReceived(class [System]System.Text.RegularExpressions.Regex regex, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void WaitOutputLineReceived()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance bool WaitOutputLineReceived(valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<bool> WaitOutputLineReceivedAsync(valuetype [mscorlib]System.TimeSpan timeout, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance bool WaitOutputLineReceived(string text, valuetype liblinux.Shell.LineMatchMode mode, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance bool WaitOutputLineReceived(string text, valuetype liblinux.Shell.LineMatchMode mode, valuetype [mscorlib]System.TimeSpan timeout, [out] string& line)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance bool WaitOutputLineReceived(class [System]System.Text.RegularExpressions.Regex regex, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance bool WaitOutputLineReceived(class [System]System.Text.RegularExpressions.Regex regex, valuetype [mscorlib]System.TimeSpan timeout, [out] string& line)
		{
			ret
		}
		.method public hidebysig 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<bool> WaitOutputLineReceivedAsync(class [System]System.Text.RegularExpressions.Regex regex, valuetype [mscorlib]System.TimeSpan timeout, valuetype [mscorlib]System.Threading.CancellationToken token)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Clear()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnBeforeOpen()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnOutputReceived(string text)
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnClosed()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnDisconnected()
		{
			ret
		}
		.method family hidebysig 
			instance void WaitOutputReceivedInternal()
		{
			ret
		}
		.method family hidebysig 
			instance bool WaitOutputReceivedInternal(valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.property instance bool Blocking()
		{
			.get instance bool liblinux.Shell.Shell::get_Blocking()
			.set instance void liblinux.Shell.Shell::set_Blocking(bool)
		}
		.property instance bool Eof()
		{
			.get instance bool liblinux.Shell.Shell::get_Eof()
		}
		.property instance class liblinux.Shell.ShellStream InputStream()
		{
			.get instance class liblinux.Shell.ShellStream liblinux.Shell.Shell::get_InputStream()
		}
		.property instance int64 Length()
		{
			.get instance int64 liblinux.Shell.Shell::get_Length()
		}
		.property instance class liblinux.Shell.ShellStream OutputStream()
		{
			.get instance class liblinux.Shell.ShellStream liblinux.Shell.Shell::get_OutputStream()
		}
	}
	.class public abstract ShellBase
		extends [mscorlib]System.Object
		implements [mscorlib]System.IDisposable
	{
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.RemoteSystemBase remoteSystem)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.RemoteSystemBase remoteSystem, class liblinux.Shell.ShellCreateOptions options)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Dispose()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Close()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Write(uint8[] buffer, int32 offset, int32 count)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Write(char[] chars, int32 offset, int32 count)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Write(string text)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Write(string format, object[] args)
		{
			.param [2]
			.custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = { }
			ret
		}
		.method public virtual hidebysig newslot 
			instance void WriteLine(string line)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void WriteLine(string format, object[] args)
		{
			.param [2]
			.custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = { }
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Flush()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void Open(class liblinux.RemoteSystemBase remoteSystem)
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void Open(class liblinux.RemoteSystemBase remoteSystem, class liblinux.Shell.ShellCreateOptions options)
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void WaitOutputReceived()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance bool WaitOutputReceived(valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnBeforeOpen()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnOpened()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnOutputReceived(uint8[] bytes, int32 count)
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnErrorOccured(class [mscorlib]System.Exception ex)
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnDisconnected()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnClosed()
		{
			ret
		}
	}
	.class public ShellCreateOptions
		extends [mscorlib]System.Object
	{
		.method public hidebysig specialname 
			instance bool get_Echo()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_Echo(bool 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance string get_TerminalName()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_TerminalName(string 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance int32 get_Columns()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_Columns(int32 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance int32 get_Rows()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_Rows(int32 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance int32 get_Width()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_Width(int32 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance int32 get_Height()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_Height(int32 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance int32 get_BufferSize()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_BufferSize(int32 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(bool echo)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(int32 columns, int32 rows)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(int32 columns, int32 rows, int32 width, int32 height)
		{
			ret
		}
		.property instance int32 BufferSize()
		{
			.get instance int32 liblinux.Shell.ShellCreateOptions::get_BufferSize()
			.set instance void liblinux.Shell.ShellCreateOptions::set_BufferSize(int32)
		}
		.property instance int32 Columns()
		{
			.get instance int32 liblinux.Shell.ShellCreateOptions::get_Columns()
			.set instance void liblinux.Shell.ShellCreateOptions::set_Columns(int32)
		}
		.property instance bool Echo()
		{
			.get instance bool liblinux.Shell.ShellCreateOptions::get_Echo()
			.set instance void liblinux.Shell.ShellCreateOptions::set_Echo(bool)
		}
		.property instance int32 Height()
		{
			.get instance int32 liblinux.Shell.ShellCreateOptions::get_Height()
			.set instance void liblinux.Shell.ShellCreateOptions::set_Height(int32)
		}
		.property instance int32 Rows()
		{
			.get instance int32 liblinux.Shell.ShellCreateOptions::get_Rows()
			.set instance void liblinux.Shell.ShellCreateOptions::set_Rows(int32)
		}
		.property instance string TerminalName()
		{
			.get instance string liblinux.Shell.ShellCreateOptions::get_TerminalName()
			.set instance void liblinux.Shell.ShellCreateOptions::set_TerminalName(string)
		}
		.property instance int32 Width()
		{
			.get instance int32 liblinux.Shell.ShellCreateOptions::get_Width()
			.set instance void liblinux.Shell.ShellCreateOptions::set_Width(int32)
		}
	}
	.class public ShellException
		extends [mscorlib]System.Exception
	{
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string message, class [mscorlib]System.Exception innerException)
		{
			ret
		}
	}
	.class public abstract ShellStream
		extends [mscorlib]'System.IO.Stream'
		implements liblinux.Shell.IShellStream, [mscorlib]System.IDisposable
	{
		.method public virtual hidebysig newslot specialname 
			instance bool get_Blocking()
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance void set_Blocking(bool 'value')
		{
			ret
		}
		.method public virtual hidebysig specialname 
			instance int64 get_Length()
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance bool get_Eof()
		{
			ret
		}
		.method public virtual hidebysig 
			instance int32 Read(uint8[] buffer, int32 offset, int32 count)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<int32> ReadAsync(uint8[] buffer, int32 offset, int32 count)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance int32 Read(char[] chars, int32 offset, int32 count)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<int32> ReadAsync(char[] chars, int32 offset, int32 count)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance string ReadLine()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<string> ReadLineAsync()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance string ReadToEnd()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<string> ReadToEndAsync()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void WaitOutputReceived()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance bool WaitOutputReceived(valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<bool> WaitOutputReceivedAsync(valuetype [mscorlib]System.TimeSpan timeout, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void WaitOutputLineReceived()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance bool WaitOutputLineReceived(valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<bool> WaitOutputLineReceivedAsync(valuetype [mscorlib]System.TimeSpan timeout, valuetype [mscorlib]System.Threading.CancellationToken cancellationToken)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance bool WaitOutputReceived(string text, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance bool WaitOutputReceived(class [System]System.Text.RegularExpressions.Regex regex, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance bool WaitOutputLineReceived(string text, valuetype liblinux.Shell.LineMatchMode mode, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance bool WaitOutputLineReceived(string text, valuetype liblinux.Shell.LineMatchMode mode, valuetype [mscorlib]System.TimeSpan timeout, [out] string& line)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance bool WaitOutputLineReceived(class [System]System.Text.RegularExpressions.Regex regex, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance bool WaitOutputLineReceived(class [System]System.Text.RegularExpressions.Regex regex, valuetype [mscorlib]System.TimeSpan timeout, [out] string& line)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]'System.Threading.Tasks.Task`1'<bool> WaitOutputLineReceivedAsync(class [System]System.Text.RegularExpressions.Regex regex, valuetype [mscorlib]System.TimeSpan timeout, valuetype [mscorlib]System.Threading.CancellationToken token)
		{
			ret
		}
		.method public virtual hidebysig 
			instance void Write(uint8[] buffer, int32 offset, int32 count)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Write(char[] chars, int32 offset, int32 count)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Write(string text)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Write(string format, object[] args)
		{
			.param [2]
			.custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = { }
			ret
		}
		.method public virtual hidebysig newslot 
			instance void WriteLine(string line)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void WriteLine(string format, object[] args)
		{
			.param [2]
			.custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = { }
			ret
		}
		.method public virtual hidebysig 
			instance void Flush()
		{
			ret
		}
		.method public virtual hidebysig 
			instance void Close()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Clear()
		{
			ret
		}
		.method public virtual hidebysig specialname 
			instance bool get_CanRead()
		{
			ret
		}
		.method public virtual hidebysig specialname 
			instance bool get_CanSeek()
		{
			ret
		}
		.method public virtual hidebysig specialname 
			instance bool get_CanWrite()
		{
			ret
		}
		.method public virtual hidebysig specialname 
			instance int64 get_Position()
		{
			ret
		}
		.method public virtual hidebysig specialname 
			instance void set_Position(int64 'value')
		{
			ret
		}
		.method public virtual hidebysig 
			instance int64 Seek(int64 offset, valuetype [mscorlib]'System.IO.SeekOrigin' origin)
		{
			ret
		}
		.method public virtual hidebysig 
			instance void SetLength(int64 'value')
		{
			ret
		}
		.property instance bool Blocking()
		{
			.get instance bool liblinux.Shell.ShellStream::get_Blocking()
			.set instance void liblinux.Shell.ShellStream::set_Blocking(bool)
		}
		.property instance bool CanRead()
		{
			.get instance bool liblinux.Shell.ShellStream::get_CanRead()
		}
		.property instance bool CanSeek()
		{
			.get instance bool liblinux.Shell.ShellStream::get_CanSeek()
		}
		.property instance bool CanWrite()
		{
			.get instance bool liblinux.Shell.ShellStream::get_CanWrite()
		}
		.property instance bool Eof()
		{
			.get instance bool liblinux.Shell.ShellStream::get_Eof()
		}
		.property instance int64 Length()
		{
			.get instance int64 liblinux.Shell.ShellStream::get_Length()
		}
		.property instance int64 Position()
		{
			.get instance int64 liblinux.Shell.ShellStream::get_Position()
			.set instance void liblinux.Shell.ShellStream::set_Position(int64)
		}
	}
	.class public StreamingShell
		extends liblinux.Shell.ShellBase
		implements liblinux.Shell.IStreamingShell, [mscorlib]System.IDisposable
	{
		.method public final virtual hidebysig newslot specialname 
			instance void add_Opened(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void remove_Opened(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void add_Disconnected(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void remove_Disconnected(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void add_OutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void remove_OutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void add_ErrorOccured(class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void remove_ErrorOccured(class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void add_Closed(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void remove_Closed(class [mscorlib]System.EventHandler 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance bool get_DelayOutputReceiveEvents()
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void set_DelayOutputReceiveEvents(bool 'value')
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance bool get_IsOpen()
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance bool get_IsAttached()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.RemoteSystemBase remoteSystem)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.RemoteSystemBase remoteSystem, class liblinux.Shell.ShellCreateOptions options)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Open(class liblinux.RemoteSystemBase remoteSystem)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Open(class liblinux.RemoteSystemBase remoteSystem, class liblinux.Shell.ShellCreateOptions options)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void BeginOutputRead()
		{
			ret
		}
		.method family hidebysig 
			instance void ThrowIfNotOpen()
		{
			ret
		}
		.method public virtual hidebysig 
			instance void Close()
		{
			ret
		}
		.method public virtual hidebysig 
			instance void Dispose()
		{
			ret
		}
		.method public virtual hidebysig 
			instance void Write(uint8[] buffer, int32 offset, int32 count)
		{
			ret
		}
		.method public virtual hidebysig 
			instance void Write(char[] chars, int32 offset, int32 count)
		{
			ret
		}
		.method public virtual hidebysig 
			instance void Write(string text)
		{
			ret
		}
		.method public virtual hidebysig 
			instance void Write(string format, object[] args)
		{
			.param [2]
			.custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = { }
			ret
		}
		.method public virtual hidebysig 
			instance void WriteLine(string line)
		{
			ret
		}
		.method public virtual hidebysig 
			instance void WriteLine(string format, object[] args)
		{
			.param [2]
			.custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = { }
			ret
		}
		.method public virtual hidebysig 
			instance void Flush()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void WaitOutputReceived()
		{
			ret
		}
		.method family virtual hidebysig 
			instance bool WaitOutputReceived(valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnBeforeOpen()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnOpened()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnOutputReceived(uint8[] bytes, int32 count)
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnErrorOccured(class [mscorlib]System.Exception ex)
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnClosed()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnDisconnected()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void Open<(liblinux.Shell.StreamingShell) 'Shell'>(!!0 shell)
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnAttached<(liblinux.Shell.StreamingShell) 'Shell'>(!!0 shell)
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnDetached<(liblinux.Shell.StreamingShell) 'Shell'>(!!0 shell)
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnOutputReceived(string text)
		{
			ret
		}
		.method famorassem virtual hidebysig newslot 
			instance void OnAttachedBeforeOpen()
		{
			ret
		}
		.method famorassem virtual hidebysig newslot 
			instance void OnAttachedOpened()
		{
			ret
		}
		.method famorassem virtual hidebysig newslot 
			instance void OnAttachedOutputReceived(uint8[] bytes, int32 count)
		{
			ret
		}
		.method famorassem virtual hidebysig newslot 
			instance void OnAttachedOutputReceived(string text)
		{
			ret
		}
		.method famorassem virtual hidebysig newslot 
			instance void OnAttachedErrorOccured(class [mscorlib]System.Exception ex)
		{
			ret
		}
		.method famorassem virtual hidebysig newslot 
			instance void OnAttachedClosed()
		{
			ret
		}
		.method famorassem virtual hidebysig newslot 
			instance void OnAttachedDisconnected()
		{
			ret
		}
		.method family hidebysig 
			instance void NotifyOpened()
		{
			ret
		}
		.method family hidebysig 
			instance void NotifyOutputReceived(string text)
		{
			ret
		}
		.method family hidebysig 
			instance void NotifyErrorOccured(class [mscorlib]System.Exception ex)
		{
			ret
		}
		.method family hidebysig 
			instance void NotifyClosed()
		{
			ret
		}
		.method family hidebysig 
			instance void NotifyDisconnected()
		{
			ret
		}
		.event [mscorlib]System.EventHandler Closed
		{
			.addon instance void liblinux.Shell.StreamingShell::add_Closed(class [mscorlib]System.EventHandler)
			.removeon instance void liblinux.Shell.StreamingShell::remove_Closed(class [mscorlib]System.EventHandler)
		}
		.event [mscorlib]System.EventHandler Disconnected
		{
			.addon instance void liblinux.Shell.StreamingShell::add_Disconnected(class [mscorlib]System.EventHandler)
			.removeon instance void liblinux.Shell.StreamingShell::remove_Disconnected(class [mscorlib]System.EventHandler)
		}
		.event class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs> ErrorOccured
		{
			.addon instance void liblinux.Shell.StreamingShell::add_ErrorOccured(class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs>)
			.removeon instance void liblinux.Shell.StreamingShell::remove_ErrorOccured(class [mscorlib]'System.EventHandler`1'<class liblinux.ErrorOccuredEventArgs>)
		}
		.event [mscorlib]System.EventHandler Opened
		{
			.addon instance void liblinux.Shell.StreamingShell::add_Opened(class [mscorlib]System.EventHandler)
			.removeon instance void liblinux.Shell.StreamingShell::remove_Opened(class [mscorlib]System.EventHandler)
		}
		.event class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> OutputReceived
		{
			.addon instance void liblinux.Shell.StreamingShell::add_OutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
			.removeon instance void liblinux.Shell.StreamingShell::remove_OutputReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
		}
		.property instance bool DelayOutputReceiveEvents()
		{
			.get instance bool liblinux.Shell.StreamingShell::get_DelayOutputReceiveEvents()
			.set instance void liblinux.Shell.StreamingShell::set_DelayOutputReceiveEvents(bool)
		}
		.property instance bool IsAttached()
		{
			.get instance bool liblinux.Shell.StreamingShell::get_IsAttached()
		}
		.property instance bool IsOpen()
		{
			.get instance bool liblinux.Shell.StreamingShell::get_IsOpen()
		}
	}
	.class public SuperUserSecurityContext
		extends liblinux.Shell.SecurityContext
	{
		.method public hidebysig specialname 
			instance class [mscorlib]System.Security.SecureString get_Password()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_Password(class [mscorlib]System.Security.SecureString 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.property instance class [mscorlib]System.Security.SecureString Password()
		{
			.get instance class [mscorlib]System.Security.SecureString liblinux.Shell.SuperUserSecurityContext::get_Password()
			.set instance void liblinux.Shell.SuperUserSecurityContext::set_Password(class [mscorlib]System.Security.SecureString)
		}
	}
	.class public UserSecurityContext
		extends liblinux.Shell.SecurityContext
	{
		.method public hidebysig specialname 
			instance string get_UserName()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_UserName(string 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance class [mscorlib]System.Security.SecureString get_Password()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_Password(class [mscorlib]System.Security.SecureString 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string userName)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.property instance class [mscorlib]System.Security.SecureString Password()
		{
			.get instance class [mscorlib]System.Security.SecureString liblinux.Shell.UserSecurityContext::get_Password()
			.set instance void liblinux.Shell.UserSecurityContext::set_Password(class [mscorlib]System.Security.SecureString)
		}
		.property instance string UserName()
		{
			.get instance string liblinux.Shell.UserSecurityContext::get_UserName()
			.set instance void liblinux.Shell.UserSecurityContext::set_UserName(string)
		}
	}
}
.namespace liblinux.Shell.Extensions
{
	.class public abstract sealed ShellExtensions
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = { }
		.method public static hidebysig 
			class liblinux.Shell.NonHostedCommand CreateCommandNoHistory(string commandString, [opt] int32 timeout)
		{
			.param [2] = int32(0xFFFFFFFF)
			ret
		}
		.method public static hidebysig 
			string CreateCommandTextNoHistory(string commandLine)
		{
			ret
		}
		.method public static hidebysig 
			string CreateCommandRedirectStderr(string commandLine)
		{
			ret
		}
		.method public static hidebysig 
			void WriteLineNoHistory(class liblinux.Shell.Extensions.ShellWithLines shell, string line)
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = { }
			ret
		}
	}
	.class public ShellWithLines
		extends liblinux.Shell.Shell
	{
		.method public hidebysig specialname 
			instance void add_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void remove_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.RemoteSystemBase remoteSystem)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.RemoteSystemBase remoteSystem, class liblinux.Shell.ShellCreateOptions options)
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnOutputReceived(string text)
		{
			ret
		}
		.event class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> OutputLineReceived
		{
			.addon instance void liblinux.Shell.Extensions.ShellWithLines::add_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
			.removeon instance void liblinux.Shell.Extensions.ShellWithLines::remove_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
		}
	}
	.class public StreamingShellWithLines
		extends liblinux.Shell.StreamingShell
	{
		.method public hidebysig specialname 
			instance void add_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void remove_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.RemoteSystemBase remoteSystem)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(class liblinux.RemoteSystemBase remoteSystem, class liblinux.Shell.ShellCreateOptions options)
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnOutputReceived(string text)
		{
			ret
		}
		.event class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs> OutputLineReceived
		{
			.addon instance void liblinux.Shell.Extensions.StreamingShellWithLines::add_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
			.removeon instance void liblinux.Shell.Extensions.StreamingShellWithLines::remove_OutputLineReceived(class [mscorlib]'System.EventHandler`1'<class liblinux.Shell.OutputReceivedEventArgs>)
		}
	}
}
.namespace liblinux.Shell.Unified
{
	.class public interface abstract 'ICommandHost`2'<+ (liblinux.Shell.ICommand) 'TCommand', + (liblinux.Shell.IAsyncCommand) 'TAsyncCommand'>
	{
		.method public virtual hidebysig newslot abstract 
			instance !0 ExecuteCommand(string commandText)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance !0 ExecuteCommand(string commandText, int32 timeout)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance !0 ExecuteCommand(string commandText, valuetype [mscorlib]System.TimeSpan timeout)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance !1 ExecuteCommandAsynchronously(string commandText, valuetype [mscorlib]System.TimeSpan timeout)
		{
		}
	}
}
