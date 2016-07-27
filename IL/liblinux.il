// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
//
// This file is used to create a reference assembly for liblinux.dll. This reference dll is NOT runnable.
// This is not currently a public API. This file is provided to allow the SSHDebugPS to build, but it isn't available for use outside of the MIDebugEngine
.assembly extern System
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly extern System.Xml
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly extern System.Core
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly extern System.Xml.Linq
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly extern Microsoft.CSharp
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 4:0:0:0
}
.assembly extern System.Security
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 4:0:0:0
}
.assembly liblinux
{
	.custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = { bool(true) }
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = { }
	.custom instance void [mscorlib]System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool) = { bool(false) }
    .publickey = (00 24 00 00 04 80 00 00 94 00 00 00 06 02 00 00   // .$..............
                00 24 00 00 52 53 41 31 00 04 00 00 01 00 01 00   // .$..RSA1........
                07 D1 FA 57 C4 AE D9 F0 A3 2E 84 AA 0F AE FD 0D   // ...W............
                E9 E8 FD 6A EC 8F 87 FB 03 76 6C 83 4C 99 92 1E   // ...j.....vl.L...
                B2 3B E7 9A D9 D5 DC C1 DD 9A D2 36 13 21 02 90   // .;.........6.!..
                0B 72 3C F9 80 95 7F C4 E1 77 10 8F C6 07 77 4F   // .r<......w....wO
                29 E8 32 0E 92 EA 05 EC E4 E8 21 C0 A5 EF E8 F1   // ).2.......!.....
                64 5C 4C 0C 93 C1 AB 99 28 5D 62 2C AA 65 2C 1D   // d\L.....(]b,.e,.
                FA D6 3D 74 5D 6F 2D E5 F1 7E 5E AF 0F C4 96 3D   // ..=t]o-..~^....=
                26 1C 8A 12 43 65 18 20 6D C0 93 34 4D 5A D2 93 ) // &...Ce. m..4MZ..

	.hash algorithm 0x00008004
    .ver 15:0:0:0
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
		.method famorassem virtual hidebysig newslot abstract specialname 
			instance valuetype liblinux.AuthenticationMode get_AuthenticationMode()
		{
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
			instance void .ctor(string message, class [mscorlib]System.Exception innerException)
		{
			ret
		}
	}
	.class public interface abstract IRemoteSystem
		implements [mscorlib]System.IDisposable
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_Connected()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance class liblinux.ConnectionInfo get_ConnectionInfo()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance class 'liblinux.IO.IRemoteFileSystem' get_FileSystem()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance class liblinux.Shell.Shell get_Shell()
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
		.property instance bool Connected()
		{
			.get instance bool liblinux.IRemoteSystem::get_Connected()
		}
		.property instance class liblinux.ConnectionInfo ConnectionInfo()
		{
			.get instance class liblinux.ConnectionInfo liblinux.IRemoteSystem::get_ConnectionInfo()
		}
		.property instance class 'liblinux.IO.IRemoteFileSystem' FileSystem()
		{
			.get instance class 'liblinux.IO.IRemoteFileSystem' liblinux.IRemoteSystem::get_FileSystem()
		}
		.property instance class liblinux.Shell.Shell Shell()
		{
			.get instance class liblinux.Shell.Shell liblinux.IRemoteSystem::get_Shell()
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
	.class public RemoteSystem
		extends [mscorlib]System.Object
		implements liblinux.IRemoteSystem, [mscorlib]System.IDisposable
	{
		.method public final virtual hidebysig newslot specialname 
			instance bool get_Connected()
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance class liblinux.ConnectionInfo get_ConnectionInfo()
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance class 'liblinux.IO.IRemoteFileSystem' get_FileSystem()
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance class liblinux.Shell.Shell get_Shell()
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
		.method public final virtual hidebysig newslot 
			instance void Connect(class liblinux.ConnectionInfo connectionInfo)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 port, int32 timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, class [mscorlib]System.Security.SecureString password)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 port, int32 timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Connect(string hostNameOrAddress, int32 port, valuetype [mscorlib]System.TimeSpan timeout, string userName, string privateFileName, class [mscorlib]System.Security.SecureString passphrase)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Disconnect()
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Dispose()
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
			instance void OnAfterConnect(bool succeeded)
		{
			ret
		}
		.property instance bool Connected()
		{
			.get instance bool liblinux.RemoteSystem::get_Connected()
		}
		.property instance class liblinux.ConnectionInfo ConnectionInfo()
		{
			.get instance class liblinux.ConnectionInfo liblinux.RemoteSystem::get_ConnectionInfo()
		}
		.property instance class 'liblinux.IO.IRemoteFileSystem' FileSystem()
		{
			.get instance class 'liblinux.IO.IRemoteFileSystem' liblinux.RemoteSystem::get_FileSystem()
		}
		.property instance class liblinux.Shell.Shell Shell()
		{
			.get instance class liblinux.Shell.Shell liblinux.RemoteSystem::get_Shell()
		}
	}
	.class public sealed SystemId
		extends [mscorlib]System.Enum
	{
		.field public rtspecialname specialname int32 value__
		.field static public literal valuetype liblinux.SystemId Android = int32(0x00000008)
		.field static public literal valuetype liblinux.SystemId Arch = int32(0x00000004)
		.field static public literal valuetype liblinux.SystemId BSD = int32(0x0000001A)
		.field static public literal valuetype liblinux.SystemId CentOS = int32(0x00000019)
		.field static public literal valuetype liblinux.SystemId Debian = int32(0x00000003)
		.field static public literal valuetype liblinux.SystemId Fedora = int32(0x00000016)
		.field static public literal valuetype liblinux.SystemId FreeBSD = int32(0x0000001B)
		.field static public literal valuetype liblinux.SystemId Gentoo = int32(0x00000005)
		.field static public literal valuetype liblinux.SystemId Knoppix = int32(0x0000000D)
		.field static public literal valuetype liblinux.SystemId Kubuntu = int32(0x00000010)
		.field static public literal valuetype liblinux.SystemId Mandrake = int32(0x00000017)
		.field static public literal valuetype liblinux.SystemId Minix = int32(0x0000001F)
		.field static public literal valuetype liblinux.SystemId NetBSD = int32(0x0000001D)
		.field static public literal valuetype liblinux.SystemId OSX = int32(0x0000001C)
		.field static public literal valuetype liblinux.SystemId OpenBSD = int32(0x0000001E)
		.field static public literal valuetype liblinux.SystemId OpenSUSE = int32(0x00000013)
		.field static public literal valuetype liblinux.SystemId PCLinux = int32(0x00000018)
		.field static public literal valuetype liblinux.SystemId Poky = int32(0x00000021)
		.field static public literal valuetype liblinux.SystemId Puppy = int32(0x00000009)
		.field static public literal valuetype liblinux.SystemId Raspbian = int32(0x00000011)
		.field static public literal valuetype liblinux.SystemId RedHat = int32(0x00000007)
		.field static public literal valuetype liblinux.SystemId RedHatEnterprise = int32(0x00000015)
		.field static public literal valuetype liblinux.SystemId 'SUSE' = int32(0x00000012)
		.field static public literal valuetype liblinux.SystemId Slackware = int32(0x00000006)
		.field static public literal valuetype liblinux.SystemId Slax = int32(0x00000014)
		.field static public literal valuetype liblinux.SystemId Sorcerer = int32(0x0000000A)
		.field static public literal valuetype liblinux.SystemId Ubuntu = int32(0x0000000E)
		.field static public literal valuetype liblinux.SystemId UbuntuServer = int32(0x0000000F)
		.field static public literal valuetype liblinux.SystemId Unix = int32(0x00000002)
		.field static public literal valuetype liblinux.SystemId Unknown = int32(0x00000001)
		.field static public literal valuetype liblinux.SystemId Yocto = int32(0x00000020)
		.field static public literal valuetype liblinux.SystemId ddWRT = int32(0x0000000C)
		.field static public literal valuetype liblinux.SystemId openWRT = int32(0x0000000B)
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
	}
	.class public sealed SystemPropertyId
		extends [mscorlib]System.Enum
	{
		.field public rtspecialname specialname int32 value__
		.field static public literal valuetype liblinux.SystemPropertyId Architecture = int32(0x00000002)
		.field static public literal valuetype liblinux.SystemPropertyId Id = int32(0x00000000)
		.field static public literal valuetype liblinux.SystemPropertyId Name = int32(0x00000001)
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
			instance valuetype 'liblinux.IO.SecurityAttributes' get_Group()
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
			instance void .ctor(int32 permissions)
		{
			ret
		}
		.property instance valuetype 'liblinux.IO.SecurityAttributes' Group()
		{
			.get instance valuetype 'liblinux.IO.SecurityAttributes' 'liblinux.IO.FilePermissions'::get_Group()
		}
		.property instance valuetype 'liblinux.IO.SecurityAttributes' Other()
		{
			.get instance valuetype 'liblinux.IO.SecurityAttributes' 'liblinux.IO.FilePermissions'::get_Other()
		}
		.property instance valuetype 'liblinux.IO.SecurityAttributes' Owner()
		{
			.get instance valuetype 'liblinux.IO.SecurityAttributes' 'liblinux.IO.FilePermissions'::get_Owner()
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
			instance void .ctor(string message, class [mscorlib]System.Exception innerException)
		{
			ret
		}
	}
	.class public interface abstract IRemoteFileSystem
	{
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.RemoteFile' UploadFile(string localFileName, string remoteFileName)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.FileInfo' DownloadFile(string remoteFileName, string localFileName)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.RemoteDirectory' CreateDirectory(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.RemoteDirectory' CreateDirectories(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.RemoteDirectory' UploadDirectory(string localPath, string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class [mscorlib]'System.IO.DirectoryInfo' DownloadDirectory(string remotePath, string localPath)
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
			instance class 'liblinux.IO.RemoteFileSystemInfo' ResolvePath(string remotePath)
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
			instance void Delete(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.RemoteDirectory'[] GetDirectories(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.RemoteFile'[] GetFiles(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.RemoteDirectory'[] GetDirectories()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.RemoteFile'[] GetFiles()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.RemoteDirectory' GetDirectory(string remotePath)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.RemoteFile' GetFile(string remoteFileName)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.RemoteDirectory' GetDirectory(valuetype 'liblinux.IO.SpecialDirectory' directory)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class 'liblinux.IO.RemoteFileSystemInfo' Stat(string remotePath)
		{
		}
	}
	.class public RemoteDirectory
		extends 'liblinux.IO.RemoteFileSystemInfo'
	{
		.method public hidebysig 
			instance void Delete()
		{
			ret
		}
		.method public hidebysig 
			instance class [mscorlib]'System.IO.DirectoryInfo' DownloadTo(string localPath)
		{
			ret
		}
		.method public hidebysig 
			instance class 'liblinux.IO.RemoteFile' UploadFile(string localFileName)
		{
			ret
		}
		.method public hidebysig 
			instance class 'liblinux.IO.RemoteDirectory' UploadDirectory(string localPath, string remotePath)
		{
			ret
		}
		.method public hidebysig 
			instance class 'liblinux.IO.RemoteDirectory' CreateDirectory(string remotePath)
		{
			ret
		}
		.method public hidebysig 
			instance class 'liblinux.IO.RemoteDirectory'[] GetDirectories()
		{
			ret
		}
		.method public hidebysig 
			instance class 'liblinux.IO.RemoteFile'[] GetFiles()
		{
			ret
		}
	}
	.class public RemoteFile
		extends 'liblinux.IO.RemoteFileSystemInfo'
	{
		.method public hidebysig 
			instance void Delete()
		{
			ret
		}
		.method public hidebysig 
			instance class [mscorlib]'System.IO.FileInfo' DownloadTo(string localPath)
		{
			ret
		}
	}
	.class public RemoteFileSystemInfo
		extends [mscorlib]System.Object
	{
		.method public hidebysig specialname 
			instance string get_Name()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance string get_FullPath()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance class 'liblinux.IO.FilePermissions' get_Permissions()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance valuetype [mscorlib]System.DateTime get_DateAccessed()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig specialname 
			instance valuetype [mscorlib]System.DateTime get_DateModified()
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public hidebysig 
			instance bool Exists()
		{
			ret
		}
		.method public hidebysig 
			instance bool IsFile()
		{
			ret
		}
		.method public hidebysig 
			instance bool IsDirectory()
		{
			ret
		}
		.property instance valuetype [mscorlib]System.DateTime DateAccessed()
		{
			.get instance valuetype [mscorlib]System.DateTime 'liblinux.IO.RemoteFileSystemInfo'::get_DateAccessed()
		}
		.property instance valuetype [mscorlib]System.DateTime DateModified()
		{
			.get instance valuetype [mscorlib]System.DateTime 'liblinux.IO.RemoteFileSystemInfo'::get_DateModified()
		}
		.property instance string FullPath()
		{
			.get instance string 'liblinux.IO.RemoteFileSystemInfo'::get_FullPath()
		}
		.property instance string Name()
		{
			.get instance string 'liblinux.IO.RemoteFileSystemInfo'::get_Name()
		}
		.property instance class 'liblinux.IO.FilePermissions' Permissions()
		{
			.get instance class 'liblinux.IO.FilePermissions' 'liblinux.IO.RemoteFileSystemInfo'::get_Permissions()
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
			instance void Update(class liblinux.ConnectionInfo connectionInfo, [opt] valuetype liblinux.Persistence.UpdateMode updateHint)
		{
			.param [2] = int32(0x00000000)
			ret
		}
		.method public hidebysig 
			instance void Update(class liblinux.Persistence.StoredConnectionInfo storedConnectionInfo, [opt] valuetype liblinux.Persistence.UpdateMode updateHint)
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
			instance bool TryGetById(int32 id, [out] class liblinux.Persistence.StoredConnectionInfo& connectionInfo)
		{
			ret
		}
		.method public hidebysig 
			instance bool Contains(class liblinux.ConnectionInfo connectionInfo)
		{
			ret
		}
		.method public hidebysig 
			instance bool Contains(int32 id)
		{
			ret
		}
		.method public hidebysig 
			instance void RemoveById(int32 id)
		{
			ret
		}
		.method public hidebysig 
			instance void Load()
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
	.class public sealed PersistenceMode
		extends [mscorlib]System.Enum
	{
		.field public rtspecialname specialname int32 value__
		.field static public literal valuetype liblinux.Persistence.PersistenceMode Disk = int32(0x00000001)
		.field static public literal valuetype liblinux.Persistence.PersistenceMode Memory = int32(0x00000000)
	}
	.class public StoredConnectionInfo
		extends [mscorlib]System.Object
	{
		.method public hidebysig specialname 
			instance int32 get_Id()
		{
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_Id(int32 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance valuetype [mscorlib]System.DateTime get_DateAdded()
		{
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_DateAdded(valuetype [mscorlib]System.DateTime 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance valuetype [mscorlib]System.DateTime get_LastSuccessful()
		{
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_LastSuccessful(valuetype [mscorlib]System.DateTime 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance valuetype [mscorlib]System.DateTime get_LastUpdated()
		{
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_LastUpdated(valuetype [mscorlib]System.DateTime 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance valuetype [mscorlib]System.DateTime get_LastFailed()
		{
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_LastFailed(valuetype [mscorlib]System.DateTime 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance valuetype liblinux.Persistence.PersistenceMode get_PersistenceMode()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_PersistenceMode(valuetype liblinux.Persistence.PersistenceMode 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance object get_Tag()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void set_Tag(object 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance class [System]System.Collections.Specialized.NameValueCollection get_Properties()
		{
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
			instance int32 get_LocalPort()
		{
			ret
		}
		.method public hidebysig specialname 
			instance int32 get_RemotePort()
		{
			ret
		}
		.method public hidebysig specialname 
			instance class liblinux.Shell.IShellStream get_Shell()
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
		.property instance int32 LocalPort()
		{
			.get instance int32 liblinux.Services.GdbServer::get_LocalPort()
		}
		.property instance int32 RemotePort()
		{
			.get instance int32 liblinux.Services.GdbServer::get_RemotePort()
		}
		.property instance class liblinux.Shell.IShellStream Shell()
		{
			.get instance class liblinux.Shell.IShellStream liblinux.Services.GdbServer::get_Shell()
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
		.method public hidebysig 
			instance class liblinux.Services.GdbServer Attach(int32 pid)
		{
			ret
		}
		.method public hidebysig 
			instance class liblinux.Services.GdbServer Attach(string preLaunchCommand, int32 pid)
		{
			ret
		}
		.method public hidebysig 
			instance class liblinux.Services.GdbServer Attach(int32 pid, int32 localPort, int32 remotePort)
		{
			ret
		}
		.method public hidebysig 
			instance class liblinux.Services.GdbServer Attach(string preLaunchCommand, int32 pid, int32 localPort, int32 remotePort)
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
	.class public GdbServerShell
		extends [mscorlib]System.Object
		implements liblinux.Shell.IShellStream, [mscorlib]System.IDisposable
	{
		.method public final virtual hidebysig newslot specialname 
			instance void add_OutputReceived(class liblinux.Shell.OnOutputReceived 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void remove_OutputReceived(class liblinux.Shell.OnOutputReceived 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance int64 get_Length()
		{
			ret
		}
		.method private final virtual hidebysig newslot 
			instance void System.IDisposable.Dispose()
		{
			.override [mscorlib]System.IDisposable::Dispose
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Write(string text)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void WriteLine(string line)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance int32 Read(uint8[] buffer, int32 offset, int32 count)
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance string ReadToEnd()
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Close()
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Flush()
		{
			ret
		}
		.event liblinux.Shell.OnOutputReceived OutputReceived
		{
			.addon instance void liblinux.Services.GdbServerShell::add_OutputReceived(class liblinux.Shell.OnOutputReceived)
			.removeon instance void liblinux.Services.GdbServerShell::remove_OutputReceived(class liblinux.Shell.OnOutputReceived)
		}
		.property instance int64 Length()
		{
			.get instance int64 liblinux.Services.GdbServerShell::get_Length()
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
}
.namespace liblinux.Shell
{
	.class public abstract 'AsyncronousCommandHostBase`1'<(liblinux.Shell.CommandBase) 'T'>
		extends class 'liblinux.Shell.CommandHostBase`1'<!0>
	{
		.method public virtual hidebysig newslot 
			instance void ExecuteCommandAsyncronously(!0 command)
		{
			ret
		}
		.method family hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
	}
	.class public sealed Command
		extends liblinux.Shell.TextCommandBase
		implements liblinux.Shell.IHostedCancellableCommand, liblinux.Shell.ISelfHostedCommand, liblinux.Shell.ICommand, liblinux.Shell.IStreamableCommand
	{
		.method public final virtual hidebysig newslot specialname 
			instance void add_Update(class liblinux.Shell.OnOutputReceived 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void remove_Update(class liblinux.Shell.OnOutputReceived 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance bool get_Queue()
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void set_Queue(bool 'value')
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance valuetype [mscorlib]System.TimeSpan get_QueueTimeout()
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void set_QueueTimeout(valuetype [mscorlib]System.TimeSpan 'value')
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance valuetype [mscorlib]System.TimeSpan get_QueueInterval()
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void set_QueueInterval(valuetype [mscorlib]System.TimeSpan 'value')
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance bool get_AccumulateResults()
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void set_AccumulateResults(bool 'value')
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance int32 get_BufferSize()
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void set_BufferSize(int32 'value')
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string commandText)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string commandText, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string commandText, int32 timeout)
		{
			ret
		}
		.method public virtual hidebysig 
			instance void Wait()
		{
			ret
		}
		.method public virtual hidebysig 
			instance bool Wait(valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public virtual hidebysig 
			instance bool Wait(int32 timeout)
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnBeforeStart()
		{
			ret
		}
		.method famorassem virtual hidebysig 
			instance void OnEnd()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnStop()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnRun()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnRunAsyncronously()
		{
			ret
		}
		.event liblinux.Shell.OnOutputReceived Update
		{
			.addon instance void liblinux.Shell.Command::add_Update(class liblinux.Shell.OnOutputReceived)
			.removeon instance void liblinux.Shell.Command::remove_Update(class liblinux.Shell.OnOutputReceived)
		}
		.property instance bool AccumulateResults()
		{
			.get instance bool liblinux.Shell.Command::get_AccumulateResults()
			.set instance void liblinux.Shell.Command::set_AccumulateResults(bool)
		}
		.property instance int32 BufferSize()
		{
			.get instance int32 liblinux.Shell.Command::get_BufferSize()
			.set instance void liblinux.Shell.Command::set_BufferSize(int32)
		}
		.property instance bool Queue()
		{
			.get instance bool liblinux.Shell.Command::get_Queue()
			.set instance void liblinux.Shell.Command::set_Queue(bool)
		}
		.property instance valuetype [mscorlib]System.TimeSpan QueueInterval()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.Shell.Command::get_QueueInterval()
			.set instance void liblinux.Shell.Command::set_QueueInterval(valuetype [mscorlib]System.TimeSpan)
		}
		.property instance valuetype [mscorlib]System.TimeSpan QueueTimeout()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.Shell.Command::get_QueueTimeout()
			.set instance void liblinux.Shell.Command::set_QueueTimeout(valuetype [mscorlib]System.TimeSpan)
		}
	}
	.class public abstract CommandBase
		extends [mscorlib]System.Object
		implements liblinux.Shell.ICommand, liblinux.Shell.IAsyncronousCommand
	{
		.method public final virtual hidebysig newslot specialname 
			instance string get_CommandText()
		{
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_CommandText(string 'value')
		{
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance int32 get_ExitCode()
		{
			ret
		}
		.method famorassem virtual hidebysig newslot specialname 
			instance void set_ExitCode(int32 'value')
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance string get_Output()
		{
			ret
		}
		.method famorassem hidebysig specialname 
			instance void set_Output(string 'value')
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance valuetype [mscorlib]System.TimeSpan get_Timeout()
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance valuetype [mscorlib]System.TimeSpan get_Elapsed()
		{
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
			instance bool get_IsTimeout()
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
		.method public virtual hidebysig newslot 
			instance void Cancel()
		{
			ret
		}
		.method famorassem virtual hidebysig newslot 
			instance void Run()
		{
			ret
		}
		.method famorassem virtual hidebysig newslot 
			instance void RunAsyncronously()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnRun()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnRunAsyncronously()
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
			instance void OnUpdate()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnTimeout()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnTimeoutAsyncronously()
		{
			ret
		}
		.method famorassem virtual hidebysig newslot 
			instance void OnEnd()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnStop()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance void OnStopAsyncronously()
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
		.property instance bool Completed()
		{
			.get instance bool liblinux.Shell.CommandBase::get_Completed()
			.set instance void liblinux.Shell.CommandBase::set_Completed(bool)
		}
		.property instance valuetype [mscorlib]System.TimeSpan Elapsed()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.Shell.CommandBase::get_Elapsed()
		}
		.property instance int32 ExitCode()
		{
			.get instance int32 liblinux.Shell.CommandBase::get_ExitCode()
			.set instance void liblinux.Shell.CommandBase::set_ExitCode(int32)
		}
		.property instance bool IsTimeout()
		{
			.get instance bool liblinux.Shell.CommandBase::get_IsTimeout()
		}
		.property instance string Output()
		{
			.get instance string liblinux.Shell.CommandBase::get_Output()
			.set instance void liblinux.Shell.CommandBase::set_Output(string)
		}
		.property instance bool Running()
		{
			.get instance bool liblinux.Shell.CommandBase::get_Running()
			.set instance void liblinux.Shell.CommandBase::set_Running(bool)
		}
		.property instance bool ThrowOnTimeout()
		{
			.get instance bool liblinux.Shell.CommandBase::get_ThrowOnTimeout()
			.set instance void liblinux.Shell.CommandBase::set_ThrowOnTimeout(bool)
		}
		.property instance valuetype [mscorlib]System.TimeSpan Timeout()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.Shell.CommandBase::get_Timeout()
		}
	}
	.class public abstract 'CommandHostBase`1'<(liblinux.Shell.CommandBase) 'T'>
		extends [mscorlib]System.Object
	{
		.method public virtual hidebysig newslot 
			instance void ExecuteCommand(!0 command)
		{
			ret
		}
		.method family hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
	}
	.class public CommandShell
		extends [mscorlib]System.Object
	{
		.method public hidebysig specialname 
			instance bool get_IsOpen()
		{
			ret
		}
		.method public hidebysig 
			instance void Close()
		{
			ret
		}
		.property instance bool IsOpen()
		{
			.get instance bool liblinux.Shell.CommandShell::get_IsOpen()
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
	.class public CommonCommandHost
		extends class 'liblinux.Shell.AsyncronousCommandHostBase`1'<class liblinux.Shell.CommandBase>
	{
		.method public hidebysig 
			instance class liblinux.Shell.INonHostedCommand ExecuteCommand(string commandText)
		{
			ret
		}
		.method public hidebysig 
			instance class liblinux.Shell.INonHostedCommand ExecuteCommand(string commandText, int32 timeout)
		{
			ret
		}
		.method public hidebysig 
			instance class liblinux.Shell.INonHostedCommand ExecuteCommand(string commandText, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public hidebysig 
			instance class liblinux.Shell.ICommand ExecuteCommand(string commandText, class liblinux.Shell.SecurityContext context)
		{
			ret
		}
		.method public hidebysig 
			instance class liblinux.Shell.ICommand ExecuteCommand(string commandText, int32 timeout, class liblinux.Shell.SecurityContext context)
		{
			ret
		}
		.method public hidebysig 
			instance class liblinux.Shell.ICommand ExecuteCommand(string commandText, valuetype [mscorlib]System.TimeSpan timeout, class liblinux.Shell.SecurityContext context)
		{
			ret
		}
		.method public virtual hidebysig 
			instance void ExecuteCommand(class liblinux.Shell.CommandBase command)
		{
			ret
		}
		.method public hidebysig 
			instance void ExecuteCommand(class liblinux.Shell.SelfHostedCommandBase command, class liblinux.Shell.SecurityContext context)
		{
			ret
		}
		.method public virtual hidebysig 
			instance void ExecuteCommandAsyncronously(class liblinux.Shell.CommandBase command)
		{
			ret
		}
		.method public hidebysig 
			instance void ExecuteCommandAsyncronously(class liblinux.Shell.SelfHostedCommandBase command, class liblinux.Shell.SecurityContext context)
		{
			ret
		}
	}
	.class public abstract HostedCommandBase
		extends liblinux.Shell.CommandBase
	{
		.method famorassem hidebysig specialname 
			instance class liblinux.Shell.ShellHost get_Host()
		{
			ret
		}
		.method family hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.property instance class liblinux.Shell.ShellHost Host()
		{
			.get instance class liblinux.Shell.ShellHost liblinux.Shell.HostedCommandBase::get_Host()
		}
	}
	.class public interface abstract IAsyncronousCommand
		implements liblinux.Shell.ICommand
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_Running()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_Completed()
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
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance string get_CommandText()
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
			instance valuetype [mscorlib]System.TimeSpan get_Elapsed()
		{
		}
		.property instance string CommandText()
		{
			.get instance string liblinux.Shell.ICommand::get_CommandText()
		}
		.property instance valuetype [mscorlib]System.TimeSpan Elapsed()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.Shell.ICommand::get_Elapsed()
		}
		.property instance int32 ExitCode()
		{
			.get instance int32 liblinux.Shell.ICommand::get_ExitCode()
		}
		.property instance string Output()
		{
			.get instance string liblinux.Shell.ICommand::get_Output()
		}
		.property instance valuetype [mscorlib]System.TimeSpan Timeout()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.Shell.ICommand::get_Timeout()
		}
	}
	.class public interface abstract IHostedCancellableCommand
		implements liblinux.Shell.ISelfHostedCommand, liblinux.Shell.ICommand
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_Cancelled()
		{
		}
		.property instance bool Cancelled()
		{
			.get instance bool liblinux.Shell.IHostedCancellableCommand::get_Cancelled()
		}
	}
	.class public interface abstract INonHostedAsyncronousCommand
		implements liblinux.Shell.INonHostedCommand, liblinux.Shell.ICommand, liblinux.Shell.IAsyncronousCommand, liblinux.Shell.IStreamableCommand
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance void add_ErrorOutputReceived(class liblinux.Shell.OnOutputReceived 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void remove_ErrorOutputReceived(class liblinux.Shell.OnOutputReceived 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.event liblinux.Shell.OnOutputReceived ErrorOutputReceived
		{
			.addon instance void liblinux.Shell.INonHostedAsyncronousCommand::add_ErrorOutputReceived(class liblinux.Shell.OnOutputReceived)
			.removeon instance void liblinux.Shell.INonHostedAsyncronousCommand::remove_ErrorOutputReceived(class liblinux.Shell.OnOutputReceived)
		}
	}
	.class public interface abstract INonHostedCommand
		implements liblinux.Shell.ICommand
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance string get_ErrorOutput()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance bool get_AppendErrorOutputToOutput()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void set_AppendErrorOutputToOutput(bool 'value')
		{
		}
		.property instance bool AppendErrorOutputToOutput()
		{
			.get instance bool liblinux.Shell.INonHostedCommand::get_AppendErrorOutputToOutput()
			.set instance void liblinux.Shell.INonHostedCommand::set_AppendErrorOutputToOutput(bool)
		}
		.property instance string ErrorOutput()
		{
			.get instance string liblinux.Shell.INonHostedCommand::get_ErrorOutput()
		}
	}
	.class public interface abstract ISelfHostedCommand
		implements liblinux.Shell.ICommand
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance class liblinux.Shell.CommandShell get_Shell()
		{
		}
		.property instance class liblinux.Shell.CommandShell Shell()
		{
			.get instance class liblinux.Shell.CommandShell liblinux.Shell.ISelfHostedCommand::get_Shell()
		}
	}
	.class public interface abstract IShellStream
		implements [mscorlib]System.IDisposable
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance void add_OutputReceived(class liblinux.Shell.OnOutputReceived 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void remove_OutputReceived(class liblinux.Shell.OnOutputReceived 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance int64 get_Length()
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
			instance int32 Read(uint8[] buffer, int32 offset, int32 count)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance string ReadToEnd()
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
		.event liblinux.Shell.OnOutputReceived OutputReceived
		{
			.addon instance void liblinux.Shell.IShellStream::add_OutputReceived(class liblinux.Shell.OnOutputReceived)
			.removeon instance void liblinux.Shell.IShellStream::remove_OutputReceived(class liblinux.Shell.OnOutputReceived)
		}
		.property instance int64 Length()
		{
			.get instance int64 liblinux.Shell.IShellStream::get_Length()
		}
	}
	.class public interface abstract IStreamableCommand
		implements liblinux.Shell.ICommand
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance void add_Update(class liblinux.Shell.OnOutputReceived 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void remove_Update(class liblinux.Shell.OnOutputReceived 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
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
			instance bool get_AccumulateResults()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void set_AccumulateResults(bool 'value')
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance int32 get_BufferSize()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance void set_BufferSize(int32 'value')
		{
		}
		.event liblinux.Shell.OnOutputReceived Update
		{
			.addon instance void liblinux.Shell.IStreamableCommand::add_Update(class liblinux.Shell.OnOutputReceived)
			.removeon instance void liblinux.Shell.IStreamableCommand::remove_Update(class liblinux.Shell.OnOutputReceived)
		}
		.property instance bool AccumulateResults()
		{
			.get instance bool liblinux.Shell.IStreamableCommand::get_AccumulateResults()
			.set instance void liblinux.Shell.IStreamableCommand::set_AccumulateResults(bool)
		}
		.property instance int32 BufferSize()
		{
			.get instance int32 liblinux.Shell.IStreamableCommand::get_BufferSize()
			.set instance void liblinux.Shell.IStreamableCommand::set_BufferSize(int32)
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
		.property instance valuetype [mscorlib]System.TimeSpan QueueTimeout()
		{
			.get instance valuetype [mscorlib]System.TimeSpan liblinux.Shell.IStreamableCommand::get_QueueTimeout()
			.set instance void liblinux.Shell.IStreamableCommand::set_QueueTimeout(valuetype [mscorlib]System.TimeSpan)
		}
	}
	.class public NonHostedCommand
		extends liblinux.Shell.CommandBase
		implements liblinux.Shell.INonHostedCommand, liblinux.Shell.ICommand, liblinux.Shell.IAsyncronousCommand
	{
		.class nested famorassem NonHostedCommandSyncExecutionResults
			extends [mscorlib]System.Object
		{
			.method public hidebysig specialname 
				instance string get_Output()
			{
				.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
				ret
			}
			.method public hidebysig specialname 
				instance string get_ErrorOutput()
			{
				.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
				ret
			}
			.method public hidebysig specialname 
				instance int32 get_ExitCode()
			{
				.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
				ret
			}
			.method public hidebysig specialname 
				instance void .ctor()
			{
				ret
			}
			.property instance string ErrorOutput()
			{
				.get instance string liblinux.Shell.NonHostedCommand/NonHostedCommandSyncExecutionResults::get_ErrorOutput()
			}
			.property instance int32 ExitCode()
			{
				.get instance int32 liblinux.Shell.NonHostedCommand/NonHostedCommandSyncExecutionResults::get_ExitCode()
			}
			.property instance string Output()
			{
				.get instance string liblinux.Shell.NonHostedCommand/NonHostedCommandSyncExecutionResults::get_Output()
			}
		}
		.method public final virtual hidebysig newslot specialname 
			instance string get_ErrorOutput()
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance bool get_AppendErrorOutputToOutput()
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance void set_AppendErrorOutputToOutput(bool 'value')
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnRun()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnRunAsyncronously()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnTimeout()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance class liblinux.Shell.NonHostedCommand/NonHostedCommandSyncExecutionResults Execute(string commandText, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.property instance bool AppendErrorOutputToOutput()
		{
			.get instance bool liblinux.Shell.NonHostedCommand::get_AppendErrorOutputToOutput()
			.set instance void liblinux.Shell.NonHostedCommand::set_AppendErrorOutputToOutput(bool)
		}
		.property instance string ErrorOutput()
		{
			.get instance string liblinux.Shell.NonHostedCommand::get_ErrorOutput()
		}
	}
	.class public sealed OnOutputReceived
		extends [mscorlib]System.MulticastDelegate
	{
		.method public hidebysig specialname 
			instance void .ctor(object 'object', native int 'method')
			runtime 
		{
		}
		.method public virtual hidebysig newslot 
			instance void Invoke(object sender, string text)
			runtime 
		{
		}
		.method public virtual hidebysig newslot 
			instance class [mscorlib]System.IAsyncResult BeginInvoke(object sender, string text, class [mscorlib]System.AsyncCallback callback, object 'object')
			runtime 
		{
		}
		.method public virtual hidebysig newslot 
			instance void EndInvoke(class [mscorlib]System.IAsyncResult result)
			runtime 
		{
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
	.class public abstract SelfHostedCommandBase
		extends liblinux.Shell.HostedCommandBase
		implements liblinux.Shell.ISelfHostedCommand, liblinux.Shell.ICommand, liblinux.Shell.IHostedCancellableCommand
	{
		.method public final virtual hidebysig newslot specialname 
			instance bool get_Cancelled()
		{
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance class liblinux.Shell.CommandShell get_Shell()
		{
			ret
		}
		.method family virtual hidebysig 
			instance void OnStop()
		{
			ret
		}
		.method famorassem virtual hidebysig newslot 
			instance void OnCancelled()
		{
			ret
		}
		.method family hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.property instance bool Cancelled()
		{
			.get instance bool liblinux.Shell.SelfHostedCommandBase::get_Cancelled()
		}
		.property instance class liblinux.Shell.CommandShell Shell()
		{
			.get instance class liblinux.Shell.CommandShell liblinux.Shell.SelfHostedCommandBase::get_Shell()
		}
	}
	.class public Shell
		extends liblinux.Shell.CommonCommandHost
	{
		.method public hidebysig 
			instance class liblinux.Shell.IShellStream OpenStream()
		{
			ret
		}
		.method public hidebysig 
			instance class liblinux.Shell.IShellStream OpenStream(bool enableEcho)
		{
			ret
		}
	}
	.class public ShellHost
		extends [mscorlib]System.Object
		implements [mscorlib]System.IDisposable
	{
		.method public hidebysig specialname 
			instance bool get_DataAvailable()
		{
			ret
		}
		.method public hidebysig 
			instance void Close()
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Dispose()
		{
			ret
		}
		.method public hidebysig 
			instance void Write(string text)
		{
			ret
		}
		.method public hidebysig 
			instance void WriteLine(string line)
		{
			ret
		}
		.method public hidebysig 
			instance void Flush()
		{
			ret
		}
		.method public hidebysig 
			instance int32 Read(uint8[] buffer, int32 offset, int32 count)
		{
			ret
		}
		.method public hidebysig 
			instance class [mscorlib]System.IAsyncResult BeginRead(uint8[] buffer, int32 offset, int32 count, class [mscorlib]System.AsyncCallback callback, object state)
		{
			ret
		}
		.method public hidebysig 
			instance int32 EndRead(class [mscorlib]System.IAsyncResult result)
		{
			ret
		}
		.method public hidebysig 
			instance string Read()
		{
			ret
		}
		.method public hidebysig 
			instance string Read(int32 count)
		{
			ret
		}
		.method public hidebysig 
			instance int32 Read(int32 count, [out] string& result)
		{
			ret
		}
		.method public hidebysig 
			instance string Expect(string text)
		{
			ret
		}
		.method public hidebysig 
			instance string Expect(string text, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public hidebysig 
			instance string Expect(class [System]System.Text.RegularExpressions.Regex regex)
		{
			ret
		}
		.method public hidebysig 
			instance string Expect(class [System]System.Text.RegularExpressions.Regex regex, valuetype [mscorlib]System.TimeSpan timeout)
		{
			ret
		}
		.method public hidebysig 
			instance void SwitchSecurityContext(class liblinux.Shell.SecurityContext context)
		{
			ret
		}
		.property instance bool DataAvailable()
		{
			.get instance bool liblinux.Shell.ShellHost::get_DataAvailable()
		}
	}
	.class public ShellStream
		extends [mscorlib]System.Object
		implements liblinux.Shell.IShellStream, [mscorlib]System.IDisposable
	{
		.method public virtual hidebysig newslot specialname 
			instance void add_OutputReceived(class liblinux.Shell.OnOutputReceived 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public virtual hidebysig newslot specialname 
			instance void remove_OutputReceived(class liblinux.Shell.OnOutputReceived 'value')
		{
			.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = { }
			ret
		}
		.method public final virtual hidebysig newslot specialname 
			instance int64 get_Length()
		{
			ret
		}
		.method public final virtual hidebysig newslot 
			instance void Dispose()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Write(string text)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void WriteLine(string line)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance int32 Read(uint8[] buffer, int32 offset, int32 count)
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance string ReadToEnd()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Flush()
		{
			ret
		}
		.method public virtual hidebysig newslot 
			instance void Close()
		{
			ret
		}
		.event liblinux.Shell.OnOutputReceived OutputReceived
		{
			.addon instance void liblinux.Shell.ShellStream::add_OutputReceived(class liblinux.Shell.OnOutputReceived)
			.removeon instance void liblinux.Shell.ShellStream::remove_OutputReceived(class liblinux.Shell.OnOutputReceived)
		}
		.property instance int64 Length()
		{
			.get instance int64 liblinux.Shell.ShellStream::get_Length()
		}
	}
	.class public SuperUserSecurityContext
		extends liblinux.Shell.SecurityContext
	{
		.method public hidebysig specialname 
			instance string get_Password()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor()
		{
			ret
		}
		.method public hidebysig specialname 
			instance void .ctor(string password)
		{
			ret
		}
		.property instance string Password()
		{
			.get instance string liblinux.Shell.SuperUserSecurityContext::get_Password()
		}
	}
	.class public abstract TextCommandBase
		extends liblinux.Shell.SelfHostedCommandBase
	{
		.method family virtual hidebysig newslot specialname 
			instance string get_AdjustedCommandText()
		{
			ret
		}
		.method family virtual hidebysig newslot 
			instance string AdjustCommandText(string commandText)
		{
			ret
		}
		.property instance string AdjustedCommandText()
		{
			.get instance string liblinux.Shell.TextCommandBase::get_AdjustedCommandText()
		}
	}
}
