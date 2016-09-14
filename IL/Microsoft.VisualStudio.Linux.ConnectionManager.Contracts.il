.assembly extern liblinux
{
	.publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
	.ver 14:0:0:0
}
.assembly extern mscorlib
{
	.publickeytoken = (B7 7A 5C 56 19 34 E0 89)
	.ver 4:0:0:0
}
.assembly Microsoft.VisualStudio.Linux.ConnectionManager.Contracts
{
	.custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = { bool(true) }
	.custom instance void [mscorlib]System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool) = { bool(false) }
	.publickey = (00 24 00 00 04 80 00 00 94 00 00 00 06 02 00 00 00 24 00 00 52 53 41 31 00 04 00 00 01 00 01 00 07 D1 FA 57 C4 AE D9 F0 A3 2E 84 AA 0F AE FD 0D E9 E8 FD 6A EC 8F 87 FB 03 76 6C 83 4C 99 92 1E B2 3B E7 9A D9 D5 DC C1 DD 9A D2 36 13 21 02 90 0B 72 3C F9 80 95 7F C4 E1 77 10 8F C6 07 77 4F 29 E8 32 0E 92 EA 05 EC E4 E8 21 C0 A5 EF E8 F1 64 5C 4C 0C 93 C1 AB 99 28 5D 62 2C AA 65 2C 1D FA D6 3D 74 5D 6F 2D E5 F1 7E 5E AF 0F C4 96 3D 26 1C 8A 12 43 65 18 20 6D C0 93 34 4D 5A D2 93)
	.hash algorithm 0x00008004
	.ver 14:0:0:0
}
.namespace Microsoft.VisualStudio.Linux.ConnectionManager
{
	.class public sealed ConnectionManagerDialogOptions
		extends [mscorlib]System.Enum
	{
		.field public rtspecialname specialname int32 value__
		.field static public literal valuetype Microsoft.VisualStudio.Linux.ConnectionManager.ConnectionManagerDialogOptions CheckPrerequisites = int32(0x00000002)
		.field static public literal valuetype Microsoft.VisualStudio.Linux.ConnectionManager.ConnectionManagerDialogOptions None = int32(0x00000001)
		.field static public literal valuetype Microsoft.VisualStudio.Linux.ConnectionManager.ConnectionManagerDialogOptions UpdateCredentials = int32(0x00000003)
	}
	.class public sealed ConnectionManagerDialogResult
		extends [mscorlib]System.Enum
	{
		.custom instance void [mscorlib]System.FlagsAttribute::.ctor() = { }
		.field public rtspecialname specialname int32 value__
		.field static public literal valuetype Microsoft.VisualStudio.Linux.ConnectionManager.ConnectionManagerDialogResult AuthenticationFailure = int32(0x00000008)
		.field static public literal valuetype Microsoft.VisualStudio.Linux.ConnectionManager.ConnectionManagerDialogResult Cancelled = int32(0x00000002)
		.field static public literal valuetype Microsoft.VisualStudio.Linux.ConnectionManager.ConnectionManagerDialogResult ConnectionUpdated = int32(0x00000400)
		.field static public literal valuetype Microsoft.VisualStudio.Linux.ConnectionManager.ConnectionManagerDialogResult ConnectivityFailure = int32(0x00000010)
		.field static public literal valuetype Microsoft.VisualStudio.Linux.ConnectionManager.ConnectionManagerDialogResult CredentialsStoreFailure = int32(0x00000040)
		.field static public literal valuetype Microsoft.VisualStudio.Linux.ConnectionManager.ConnectionManagerDialogResult Failed = int32(0x00000004)
		.field static public literal valuetype Microsoft.VisualStudio.Linux.ConnectionManager.ConnectionManagerDialogResult ManageConnections = int32(0x00000200)
		.field static public literal valuetype Microsoft.VisualStudio.Linux.ConnectionManager.ConnectionManagerDialogResult PrivateKeyFailure = int32(0x00000020)
		.field static public literal valuetype Microsoft.VisualStudio.Linux.ConnectionManager.ConnectionManagerDialogResult Succeeded = int32(0x00000001)
		.field static public literal valuetype Microsoft.VisualStudio.Linux.ConnectionManager.ConnectionManagerDialogResult UnknownFailure = int32(0x00000080)
		.field static public literal valuetype Microsoft.VisualStudio.Linux.ConnectionManager.ConnectionManagerDialogResult ValidationFailure = int32(0x00000100)
	}
	.class public interface abstract IConnectionManagerResult
	{
		.method public virtual hidebysig newslot abstract specialname 
			instance class [liblinux]liblinux.ConnectionInfo get_ConnectionInfo()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance int32 get_StoredConnectionId()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance valuetype Microsoft.VisualStudio.Linux.ConnectionManager.ConnectionManagerDialogResult get_DialogResult()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance valuetype [liblinux]liblinux.SystemId get_SystemId()
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance valuetype [liblinux]liblinux.SystemPlatform get_SystemPlatform()
		{
		}
		.property instance class [liblinux]liblinux.ConnectionInfo ConnectionInfo()
		{
			.get instance class [liblinux]liblinux.ConnectionInfo Microsoft.VisualStudio.Linux.ConnectionManager.IConnectionManagerResult::get_ConnectionInfo()
		}
		.property instance valuetype Microsoft.VisualStudio.Linux.ConnectionManager.ConnectionManagerDialogResult DialogResult()
		{
			.get instance valuetype Microsoft.VisualStudio.Linux.ConnectionManager.ConnectionManagerDialogResult Microsoft.VisualStudio.Linux.ConnectionManager.IConnectionManagerResult::get_DialogResult()
		}
		.property instance int32 StoredConnectionId()
		{
			.get instance int32 Microsoft.VisualStudio.Linux.ConnectionManager.IConnectionManagerResult::get_StoredConnectionId()
		}
		.property instance valuetype [liblinux]liblinux.SystemId SystemId()
		{
			.get instance valuetype [liblinux]liblinux.SystemId Microsoft.VisualStudio.Linux.ConnectionManager.IConnectionManagerResult::get_SystemId()
		}
		.property instance valuetype [liblinux]liblinux.SystemPlatform SystemPlatform()
		{
			.get instance valuetype [liblinux]liblinux.SystemPlatform Microsoft.VisualStudio.Linux.ConnectionManager.IConnectionManagerResult::get_SystemPlatform()
		}
	}
	.class public interface abstract IVsConnectionManager
	{
		.method public virtual hidebysig newslot abstract 
			instance class Microsoft.VisualStudio.Linux.ConnectionManager.IConnectionManagerResult ShowDialog()
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class Microsoft.VisualStudio.Linux.ConnectionManager.IConnectionManagerResult ShowDialog(class [liblinux]liblinux.ConnectionInfo connectionInfo)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class Microsoft.VisualStudio.Linux.ConnectionManager.IConnectionManagerResult ShowDialog(string title)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class Microsoft.VisualStudio.Linux.ConnectionManager.IConnectionManagerResult ShowDialog(string title, string description)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class Microsoft.VisualStudio.Linux.ConnectionManager.IConnectionManagerResult ShowDialog(string title, string description, class [liblinux]liblinux.ConnectionInfo connectionInfo)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class Microsoft.VisualStudio.Linux.ConnectionManager.IConnectionManagerResult ShowDialog(string title, string description, valuetype Microsoft.VisualStudio.Linux.ConnectionManager.ConnectionManagerDialogOptions dialogOptions)
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class Microsoft.VisualStudio.Linux.ConnectionManager.IConnectionManagerResult ShowDialog(string title, string description, class [liblinux]liblinux.ConnectionInfo connectionInfo, valuetype Microsoft.VisualStudio.Linux.ConnectionManager.ConnectionManagerDialogOptions dialogOptions)
		{
		}
	}
}
