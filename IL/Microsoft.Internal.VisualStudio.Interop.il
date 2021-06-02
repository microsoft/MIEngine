.assembly extern Microsoft.VisualStudio.Interop
{
	.publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
	.ver 17:0:0:0
}
.assembly extern mscorlib
{
	.publickeytoken = (B7 7A 5C 56 19 34 E0 89)
	.ver 4:0:0:0
}
.assembly Microsoft.Internal.VisualStudio.Interop
{
	.publickey = (00 24 00 00 04 80 00 00 94 00 00 00 06 02 00 00 00 24 00 00 52 53 41 31 00 04 00 00 01 00 01 00 07 D1 FA 57 C4 AE D9 F0 A3 2E 84 AA 0F AE FD 0D E9 E8 FD 6A EC 8F 87 FB 03 76 6C 83 4C 99 92 1E B2 3B E7 9A D9 D5 DC C1 DD 9A D2 36 13 21 02 90 0B 72 3C F9 80 95 7F C4 E1 77 10 8F C6 07 77 4F 29 E8 32 0E 92 EA 05 EC E4 E8 21 C0 A5 EF E8 F1 64 5C 4C 0C 93 C1 AB 99 28 5D 62 2C AA 65 2C 1D FA D6 3D 74 5D 6F 2D E5 F1 7E 5E AF 0F C4 96 3D 26 1C 8A 12 43 65 18 20 6D C0 93 34 4D 5A D2 93)
	.hash algorithm 0x00008004
	.ver 17:0:0:0
}
.namespace Microsoft.Internal.VisualStudio.Shell.Interop
{
    .class public interface abstract import IVsTelemetryService
	{
		.custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = { string('5C7E7029-A00C-4F57-BE15-6AC5D43E78CB') }
		.custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = { int32(1) }
		.method public virtual hidebysig newslot abstract 
			instance class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetrySession marshal(interface) GetDefaultSession()
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryEvent marshal(interface) CreateEvent([in] string marshal(lpwstr) szEventName)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryActivity marshal(interface) CreateActivity([in] string marshal(lpwstr) szActivityName)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryActivity marshal(interface) CreateActivityWithParentCorrelationId([in] string marshal(lpwstr) szActivityName, [in] valuetype [mscorlib]System.Guid& parentCorrelationId)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
			.param [2]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.REFGUID') }
		}
		.method public virtual hidebysig newslot abstract 
			instance class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryPropertyBag marshal(interface) CreatePropertyBag()
			internalcall 
		{
		}
	}
    .class public interface abstract import IVsTelemetrySession
	{
		.custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = { string('B567D0C0-9334-4EFB-811B-F12AEE408A3D') }
		.custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = { int32(1) }
		.method public virtual hidebysig newslot abstract 
			instance string marshal(bstr) GetSessionId()
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance string marshal(bstr) SerializeSettings()
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostSimpleEvent([in] string marshal(lpwstr) szEventName)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostEvent([in] class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryEvent marshal(interface) eventObject)
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostProperty([in] string marshal(lpwstr) szPropertyName, [in] object marshal(struct) varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostBoolProperty([in] string marshal(lpwstr) szPropertyName, [in] bool 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostDoubleProperty([in] string marshal(lpwstr) szPropertyName, [in] float64 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostIntProperty([in] string marshal(lpwstr) szPropertyName, [in] int32 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostLongProperty([in] string marshal(lpwstr) szPropertyName, [in] int64 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostShortProperty([in] string marshal(lpwstr) szPropertyName, [in] int16 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostStringProperty([in] string marshal(lpwstr) szPropertyName, [in] string marshal(lpwstr) szValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
			.param [2]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedProperty([in] string marshal(lpwstr) szPropertyName, [in] object marshal(struct) varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedBoolProperty([in] string marshal(lpwstr) szPropertyName, [in] bool 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedDoubleProperty([in] string marshal(lpwstr) szPropertyName, [in] float64 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedIntProperty([in] string marshal(lpwstr) szPropertyName, [in] int32 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedLongProperty([in] string marshal(lpwstr) szPropertyName, [in] int64 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedShortProperty([in] string marshal(lpwstr) szPropertyName, [in] int16 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedStringProperty([in] string marshal(lpwstr) szPropertyName, [in] string marshal(lpwstr) szValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
			.param [2]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void RemoveSharedProperty([in] string marshal(lpwstr) szPropertyName)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryContext marshal(interface) CreateContext([in] string marshal(lpwstr) szContextName)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance bool IsUserOptedIn()
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool IsUserMicrosoftInternal()
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance bool CanCollectPrivateInformation()
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetUserOptedIn([in] bool IsUserOptedIn)
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void RegisterPropertyBag([in] string marshal(lpwstr) szPropertyBagName, [in] class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryPropertyBag marshal(interface) pPropertyBag)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryPropertyBag marshal(interface) GetPropertyBag([in] string marshal(lpwstr) szPropertyBagName)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void UnregisterPropertyBag([in] string marshal(lpwstr) szPropertyBagName)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] object marshal(struct) varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostIntPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] int32 varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostLongPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] int64 varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostDoublePiiProperty([in] string marshal(lpwstr) szPropertyName, [in] float64 varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostStringPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] string marshal(lpwstr) varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
			.param [2]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] object marshal(struct) varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedIntPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] int32 varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedLongPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] int64 varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedDoublePiiProperty([in] string marshal(lpwstr) szPropertyName, [in] float64 varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedStringPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] string marshal(lpwstr) varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
			.param [2]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance object marshal(struct) GetSharedProperty([in] string marshal(lpwstr) szPropertyName)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryContext marshal(interface) GetContext([in] string marshal(lpwstr) szContextName)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
	}
    	.class public interface abstract import IVsTelemetryEvent
	{
		.custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = { string('5DC5F85E-C49B-4CF6-97B7-BDEF7AD0ADE3') }
		.custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = { int32(1) }
		.method public virtual hidebysig newslot abstract 
			instance string marshal(bstr) GetName()
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance object marshal(struct) GetProperty([in] string marshal(lpwstr) szPropertyName)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetProperty([in] string marshal(lpwstr) szPropertyName, [in] object marshal(struct) varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetBoolProperty([in] string marshal(lpwstr) szPropertyName, [in] bool 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetDoubleProperty([in] string marshal(lpwstr) szPropertyName, [in] float64 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetIntProperty([in] string marshal(lpwstr) szPropertyName, [in] int32 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetLongProperty([in] string marshal(lpwstr) szPropertyName, [in] int64 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetShortProperty([in] string marshal(lpwstr) szPropertyName, [in] int16 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetStringProperty([in] string marshal(lpwstr) szPropertyName, [in] string marshal(lpwstr) szValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
			.param [2]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance bool ContainsProperty([in] string marshal(lpwstr) szPropertyName)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance class [mscorlib]System.Array marshal(safearray bstr) get_AllPropertyNames()
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetOptOutFriendlyFlag([in] bool bOptOutFriendly)
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void AddPropertyBag([in] class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryPropertyBag marshal(interface) pPropertyBag)
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void RemovePropertyBag([in] class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryPropertyBag marshal(interface) pPropertyBag)
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] object marshal(struct) varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetIntPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] int32 varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetLongPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] int64 varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetDoublePiiProperty([in] string marshal(lpwstr) szPropertyName, [in] float64 varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetStringPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] string marshal(lpwstr) varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
			.param [2]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.property instance class [mscorlib]System.Array AllPropertyNames()
		{
			.get instance class [mscorlib]System.Array Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryEvent::get_AllPropertyNames()
		}
	}
    	.class public interface abstract import IVsTelemetryActivity
		implements Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryEvent
	{
		.custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = { string('8F0E37F9-B1F6-4247-9C91-D801F92710A5') }
		.custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = { int32(1) }
		.method public virtual hidebysig newslot abstract 
			instance string marshal(bstr) GetName()
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance object marshal(struct) GetProperty([in] string marshal(lpwstr) szPropertyName)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetProperty([in] string marshal(lpwstr) szPropertyName, [in] object marshal(struct) varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetBoolProperty([in] string marshal(lpwstr) szPropertyName, [in] bool 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetDoubleProperty([in] string marshal(lpwstr) szPropertyName, [in] float64 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetIntProperty([in] string marshal(lpwstr) szPropertyName, [in] int32 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetLongProperty([in] string marshal(lpwstr) szPropertyName, [in] int64 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetShortProperty([in] string marshal(lpwstr) szPropertyName, [in] int16 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetStringProperty([in] string marshal(lpwstr) szPropertyName, [in] string marshal(lpwstr) szValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
			.param [2]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance bool ContainsProperty([in] string marshal(lpwstr) szPropertyName)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance class [mscorlib]System.Array marshal(safearray bstr) get_AllPropertyNames()
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetOptOutFriendlyFlag([in] bool bOptOutFriendly)
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void AddPropertyBag([in] class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryPropertyBag marshal(interface) pPropertyBag)
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void RemovePropertyBag([in] class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryPropertyBag marshal(interface) pPropertyBag)
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] object marshal(struct) varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetIntPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] int32 varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetLongPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] int64 varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetDoublePiiProperty([in] string marshal(lpwstr) szPropertyName, [in] float64 varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetStringPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] string marshal(lpwstr) varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
			.param [2]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void Start()
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void End()
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void EndWithDuration([in] int64 eventDuration)
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance valuetype [mscorlib]System.Guid get_CorrelationId()
			internalcall 
		{
		}
		.property instance class [mscorlib]System.Array AllPropertyNames()
		{
			.get instance class [mscorlib]System.Array Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryActivity::get_AllPropertyNames()
		}
		.property instance valuetype [mscorlib]System.Guid CorrelationId()
		{
			.get instance valuetype [mscorlib]System.Guid Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryActivity::get_CorrelationId()
		}
	}
    	.class public interface abstract import IVsTelemetryContext
	{
		.custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = { string('82CB18EA-5330-4ADB-BD79-EB964F30749D') }
		.custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = { int32(1) }
		.method public virtual hidebysig newslot abstract specialname 
			instance string marshal(bstr) get_ContextName()
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance object marshal(struct) GetProperty([in] string marshal(lpwstr) szPropertyName)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostProperty([in] string marshal(lpwstr) szPropertyName, [in] object marshal(struct) varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostBoolProperty([in] string marshal(lpwstr) szPropertyName, [in] bool 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostDoubleProperty([in] string marshal(lpwstr) szPropertyName, [in] float64 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostIntProperty([in] string marshal(lpwstr) szPropertyName, [in] int32 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostLongProperty([in] string marshal(lpwstr) szPropertyName, [in] int64 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostShortProperty([in] string marshal(lpwstr) szPropertyName, [in] int16 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostStringProperty([in] string marshal(lpwstr) szPropertyName, [in] string marshal(lpwstr) szValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
			.param [2]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedProperty([in] string marshal(lpwstr) szPropertyName, [in] object marshal(struct) varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedBoolProperty([in] string marshal(lpwstr) szPropertyName, [in] bool 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedDoubleProperty([in] string marshal(lpwstr) szPropertyName, [in] float64 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedIntProperty([in] string marshal(lpwstr) szPropertyName, [in] int32 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedLongProperty([in] string marshal(lpwstr) szPropertyName, [in] int64 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedShortProperty([in] string marshal(lpwstr) szPropertyName, [in] int16 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedStringProperty([in] string marshal(lpwstr) szPropertyName, [in] string marshal(lpwstr) szValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
			.param [2]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void RemoveSharedProperty([in] string marshal(lpwstr) szPropertyName)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void Close()
			internalcall 
		{
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] object marshal(struct) varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostIntPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] int32 varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostLongPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] int64 varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostDoublePiiProperty([in] string marshal(lpwstr) szPropertyName, [in] float64 varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void PostStringPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] string marshal(lpwstr) varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
			.param [2]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] object marshal(struct) varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedIntPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] int32 varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedLongPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] int64 varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedDoublePiiProperty([in] string marshal(lpwstr) szPropertyName, [in] float64 varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetSharedStringPiiProperty([in] string marshal(lpwstr) szPropertyName, [in] string marshal(lpwstr) varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
			.param [2]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.property instance string ContextName()
		{
			.get instance string Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryContext::get_ContextName()
		}
	}
    	.class public interface abstract import IVsTelemetryPropertyBag
	{
		.custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = { string('FC1510D1-1CE5-4EE9-B861-66714C95A242') }
		.custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = { int32(1) }
		.method public virtual hidebysig newslot abstract 
			instance object marshal(struct) GetProperty([in] string marshal(lpwstr) szPropertyName)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetProperty([in] string marshal(lpwstr) szPropertyName, [in] object marshal(struct) varValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void RemoveProperty([in] string marshal(lpwstr) szPropertyName)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetBoolProperty([in] string marshal(lpwstr) szPropertyName, [in] bool 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetDoubleProperty([in] string marshal(lpwstr) szPropertyName, [in] float64 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetIntProperty([in] string marshal(lpwstr) szPropertyName, [in] int32 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetLongProperty([in] string marshal(lpwstr) szPropertyName, [in] int64 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetShortProperty([in] string marshal(lpwstr) szPropertyName, [in] int16 'value')
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance void SetStringProperty([in] string marshal(lpwstr) szPropertyName, [in] string marshal(lpwstr) szValue)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
			.param [2]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract 
			instance bool ContainsProperty([in] string marshal(lpwstr) szPropertyName)
			internalcall 
		{
			.param [1]
			.custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = { string('OLE.LPCOLESTR') }
		}
		.method public virtual hidebysig newslot abstract specialname 
			instance class [mscorlib]System.Array marshal(safearray bstr) get_AllPropertyNames()
			internalcall 
		{
		}
		.property instance class [mscorlib]System.Array AllPropertyNames()
		{
			.get instance class [mscorlib]System.Array Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryPropertyBag::get_AllPropertyNames()
		}
	}
}