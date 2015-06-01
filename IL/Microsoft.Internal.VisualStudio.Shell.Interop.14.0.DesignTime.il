// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
//
// This file is used to create a reference assembly for Microsoft.Internal.VisualStudio.Shell.Interop.14.0.DesignTime.dll. This reference dll is NOT runable.
// This is not currently a public API. This file is provided to allow the MIDebugEngine to build, but it isn't available for use outside of the MIDebugEngine

// Metadata version: v2.0.50727
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 1:0:3300:0
}
.assembly extern 'Microsoft.VisualStudio.Imaging.Interop.14.0.DesignTime'
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 14:0:0:0
}
.assembly extern 'Microsoft.Internal.VisualStudio.Shell.Interop.10.0.DesignTime'
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 10:0:0:0
}
.assembly extern 'Microsoft.VisualStudio.Shell.Interop.11.0'
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 11:0:0:0
}
.assembly extern Microsoft.VisualStudio.Shell.Interop
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 7:1:40304:0
}
.assembly extern 'Microsoft.Internal.VisualStudio.Shell.Interop.12.0.DesignTime'
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 12:0:0:0
}
.assembly extern Microsoft.VisualStudio.OLE.Interop
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 7:1:40304:0
}
.assembly 'Microsoft.Internal.VisualStudio.Shell.Interop.14.0.DesignTime'
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 46 30 43 46 38 44 38 2D 34 32 45 39   // ..$0F0CF8D8-42E9
                                                                                                  2D 34 31 35 41 2D 41 33 32 38 2D 30 42 38 36 44   // -415A-A328-0B86D
                                                                                                  45 33 36 31 36 46 43 00 00 )                      // E3616FC..
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibVersionAttribute::.ctor(int32,
                                                                                                int32) = ( 01 00 0E 00 00 00 00 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ImportedFromTypeLibAttribute::.ctor(string) = ( 01 00 11 56 73 53 68 65 6C 6C 50 72 69 76 61 74   // ...VsShellPrivat
                                                                                                                 65 31 34 30 00 00 )                               // e140..
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
  .ver 14:0:0:0
}
.module 'Microsoft.Internal.VisualStudio.Shell.Interop.14.0.DesignTime'
// MVID: {A99448BC-3594-4F21-9E48-95B7158BC752}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x01170000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi sealed Microsoft.Internal.VisualStudio.Shell.Interop.__VSCOMMANDFLAGS2
       extends [mscorlib]System.Enum
{
  .custom instance void [mscorlib]System.FlagsAttribute::.ctor() = ( 01 00 00 00 ) 
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.Internal.VisualStudio.Shell.Interop.__VSCOMMANDFLAGS2 cCommandFlagAllowVisibilityChangeOnToolBar = int32(0x01000000)
  .field public static literal valuetype Microsoft.Internal.VisualStudio.Shell.Interop.__VSCOMMANDFLAGS2 cCommandFlagIconIsMoniker = int32(0x80000000)
  .field public static literal valuetype Microsoft.Internal.VisualStudio.Shell.Interop.__VSCOMMANDFLAGS2 ValidFlags2 = int32(0xFFFFFFFF)
} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.__VSCOMMANDFLAGS2

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.IVsPlaceExtensionInternal
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 34 32 33 31 37 32 30 37 2D 33 32 41 31   // ..$42317207-32A1
                                                                                                  2D 34 44 30 42 2D 41 30 36 35 2D 32 37 43 36 35   // -4D0B-A065-27C65
                                                                                                  34 31 33 46 33 41 30 00 00 )                      // 413F3A0..
  .method public hidebysig newslot specialname abstract virtual 
          instance bool  get_IsRemovable() runtime managed internalcall
  {
  } // end of method IVsPlaceExtensionInternal::get_IsRemovable

  .property bool IsRemovable()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 01 60 00 00 )                         // .....`..
    .get instance bool Microsoft.Internal.VisualStudio.Shell.Interop.IVsPlaceExtensionInternal::get_IsRemovable()
  } // end of property IVsPlaceExtensionInternal::IsRemovable
} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.IVsPlaceExtensionInternal

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.IVsImageServicePrivate
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 33 34 45 46 35 42 34 2D 35 42 35 42   // ..$C34EF5B4-5B5B
                                                                                                  2D 34 39 46 38 2D 42 33 38 46 2D 41 34 37 41 31   // -49F8-B38F-A47A1
                                                                                                  35 41 44 36 39 32 38 00 00 )                      // 5AD6928..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  ConvertRGBtoHSL([in] uint8 r,
                                         [in] uint8 g,
                                         [in] uint8 b,
                                         [out] float64& h,
                                         [out] float64& s,
                                         [out] float64& l) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 10 54 65 78 74 4D 61 6E 61 67 65 72 2E 42   // ...TextManager.B
                                                                                                            59 54 45 00 00 )                                  // YTE..
    .param [2]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 10 54 65 78 74 4D 61 6E 61 67 65 72 2E 42   // ...TextManager.B
                                                                                                            59 54 45 00 00 )                                  // YTE..
    .param [3]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 10 54 65 78 74 4D 61 6E 61 67 65 72 2E 42   // ...TextManager.B
                                                                                                            59 54 45 00 00 )                                  // YTE..
  } // end of method IVsImageServicePrivate::ConvertRGBtoHSL

  .method public hidebysig newslot abstract virtual 
          instance void  ConvertHSLtoRGB([in] float64 h,
                                         [in] float64 s,
                                         [in] float64 l,
                                         [out] uint8& r,
                                         [out] uint8& g,
                                         [out] uint8& b) runtime managed internalcall
  {
    .param [4]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 10 54 65 78 74 4D 61 6E 61 67 65 72 2E 42   // ...TextManager.B
                                                                                                            59 54 45 00 00 )                                  // YTE..
    .param [5]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 10 54 65 78 74 4D 61 6E 61 67 65 72 2E 42   // ...TextManager.B
                                                                                                            59 54 45 00 00 )                                  // YTE..
    .param [6]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 10 54 65 78 74 4D 61 6E 61 67 65 72 2E 42   // ...TextManager.B
                                                                                                            59 54 45 00 00 )                                  // YTE..
  } // end of method IVsImageServicePrivate::ConvertHSLtoRGB

  .method public hidebysig newslot abstract virtual 
          instance bool  IsOptOutPixelSet([in] int32 bits,
                                          [in] int32 width,
                                          [in] int32 height,
                                          [in] bool isTopDownBitmap) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0F 56 73 53 68 65 6C 6C 2E 49 4E 54 5F 50   // ...VsShell.INT_P
                                                                                                            54 52 00 00 )                                     // TR..
  } // end of method IVsImageServicePrivate::IsOptOutPixelSet

  .method public hidebysig newslot abstract virtual 
          instance void  ThemePixel([in][out] uint32& pixel,
                                    [in] float64 backgroundHue,
                                    [in] float64 backgroundSaturation,
                                    [in] float64 backgroundLuminosity) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 09 4F 4C 45 2E 44 57 4F 52 44 00 00 )       // ...OLE.DWORD..
  } // end of method IVsImageServicePrivate::ThemePixel

  .method public hidebysig newslot abstract virtual 
          instance class ['Microsoft.VisualStudio.Imaging.Interop.14.0.DesignTime']Microsoft.VisualStudio.Imaging.Interop.IImageHandle 
          marshal( interface ) 
          AddMemorySectionBitmap([in] class ['Microsoft.Internal.VisualStudio.Shell.Interop.10.0.DesignTime']Microsoft.Internal.VisualStudio.Shell.Interop.IVsUIMemorySectionBitmapPrivate  marshal( interface ) section) runtime managed internalcall
  {
  } // end of method IVsImageServicePrivate::AddMemorySectionBitmap

} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.IVsImageServicePrivate

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.IVsFeatureFlags
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 41 44 34 34 42 38 42 39 2D 42 36 34 36   // ..$AD44B8B9-B646
                                                                                                  2D 34 42 31 38 2D 38 38 34 37 2D 31 35 30 36 39   // -4B18-8847-15069
                                                                                                  35 41 45 43 34 38 30 00 00 )                      // 5AEC480..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance bool  IsFeatureEnabled([in] string  marshal( lpwstr) featureName,
                                          [in] bool defaultValue) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 4C 50 43 57 53 54 52 00 00 ) // ...OLE.LPCWSTR..
  } // end of method IVsFeatureFlags::IsFeatureEnabled

} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.IVsFeatureFlags

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.SVsFeatureFlags
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 37 38 41 36 37 46 33 33 2D 32 32 43 46   // ..$78A67F33-22CF
                                                                                                  2D 34 32 36 43 2D 38 43 39 30 2D 42 36 45 31 38   // -426C-8C90-B6E18
                                                                                                  46 44 33 35 45 30 46 00 00 )                      // FD35E0F..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.SVsFeatureFlags

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.IVsEnumerablePropertyBag
       implements ['Microsoft.VisualStudio.Shell.Interop.11.0']Microsoft.VisualStudio.Shell.Interop.IVsPropertyBag
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 31 37 39 41 39 38 42 46 2D 36 30 42 33   // ..$179A98BF-60B3
                                                                                                  2D 34 34 35 42 2D 42 31 30 45 2D 39 46 35 45 33   // -445B-B10E-9F5E3
                                                                                                  30 34 46 45 44 45 41 00 00 )                      // 04FEDEA..
  .method public hidebysig newslot abstract virtual 
          instance object 
          marshal( struct) 
          GetValue([in] string  marshal( lpwstr) szName) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsEnumerablePropertyBag::GetValue

  .method public hidebysig newslot abstract virtual 
          instance void  SetValue([in] string  marshal( lpwstr) szName,
                                  [in] object&  marshal( struct) pVarValue) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsEnumerablePropertyBag::SetValue

  .method public hidebysig newslot specialname abstract virtual 
          instance class [mscorlib]System.Array 
          marshal( safearray bstr) 
          get_Keys() runtime managed internalcall
  {
  } // end of method IVsEnumerablePropertyBag::get_Keys

  .property class [mscorlib]System.Array Keys()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 02 60 00 00 )                         // .....`..
    .get instance class [mscorlib]System.Array Microsoft.Internal.VisualStudio.Shell.Interop.IVsEnumerablePropertyBag::get_Keys()
  } // end of property IVsEnumerablePropertyBag::Keys
} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.IVsEnumerablePropertyBag

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.IVsSettingsListChangeEvents
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 36 35 35 35 35 33 31 2D 31 44 37 32   // ..$06555531-1D72
                                                                                                  2D 34 35 35 37 2D 38 41 31 37 2D 45 41 36 36 35   // -4557-8A17-EA665
                                                                                                  46 35 37 39 36 36 42 00 00 )                      // F57966B..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance class ['Microsoft.VisualStudio.Shell.Interop.11.0']Microsoft.VisualStudio.Shell.Interop.IVsTask 
          marshal( interface ) 
          OnItemAddedToFrontAsync([in] string  marshal( lpwstr) listName,
                                  [in] string  marshal( lpwstr) key,
                                  [in] object  marshal( struct) 'value',
                                  [in] object  marshal( struct) state) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 4C 50 43 57 53 54 52 00 00 ) // ...OLE.LPCWSTR..
    .param [2]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 4C 50 43 57 53 54 52 00 00 ) // ...OLE.LPCWSTR..
  } // end of method IVsSettingsListChangeEvents::OnItemAddedToFrontAsync

  .method public hidebysig newslot abstract virtual 
          instance class ['Microsoft.VisualStudio.Shell.Interop.11.0']Microsoft.VisualStudio.Shell.Interop.IVsTask 
          marshal( interface ) 
          OnItemsRemovedAsync([in] string  marshal( lpwstr) listName,
                              [in] class Microsoft.Internal.VisualStudio.Shell.Interop.IVsEnumerablePropertyBag  marshal( interface ) pItemsRemoved,
                              [in] object  marshal( struct) state) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 4C 50 43 57 53 54 52 00 00 ) // ...OLE.LPCWSTR..
  } // end of method IVsSettingsListChangeEvents::OnItemsRemovedAsync

} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.IVsSettingsListChangeEvents

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.IVsSettingsList
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 33 44 32 42 32 34 39 35 2D 45 45 41 44   // ..$3D2B2495-EEAD
                                                                                                  2D 34 34 32 31 2D 41 32 38 32 2D 37 44 45 42 37   // -4421-A282-7DEB7
                                                                                                  46 38 34 43 42 45 44 00 00 )                      // F84CBED..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance object 
          marshal( struct) 
          GetValue([in] string  marshal( lpwstr) key) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 4C 50 43 57 53 54 52 00 00 ) // ...OLE.LPCWSTR..
  } // end of method IVsSettingsList::GetValue

  .method public hidebysig newslot abstract virtual 
          instance class ['Microsoft.VisualStudio.Shell.Interop.11.0']Microsoft.VisualStudio.Shell.Interop.IVsTask 
          marshal( interface ) 
          AddToFrontAsync([in] string  marshal( lpwstr) key,
                          [in] object&  marshal( struct) pValue,
                          [in] object  marshal( struct) state) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 4C 50 43 57 53 54 52 00 00 ) // ...OLE.LPCWSTR..
  } // end of method IVsSettingsList::AddToFrontAsync

  .method public hidebysig newslot abstract virtual 
          instance class ['Microsoft.VisualStudio.Shell.Interop.11.0']Microsoft.VisualStudio.Shell.Interop.IVsTask 
          marshal( interface ) 
          ClearAsync([in] object  marshal( struct) state) runtime managed internalcall
  {
  } // end of method IVsSettingsList::ClearAsync

  .method public hidebysig newslot abstract virtual 
          instance class ['Microsoft.VisualStudio.Shell.Interop.11.0']Microsoft.VisualStudio.Shell.Interop.IVsTask 
          marshal( interface ) 
          RemoveAsync([in] string  marshal( lpwstr) key,
                      [in] object  marshal( struct) state) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 4C 50 43 57 53 54 52 00 00 ) // ...OLE.LPCWSTR..
  } // end of method IVsSettingsList::RemoveAsync

  .method public hidebysig newslot abstract virtual 
          instance uint32  AdviseListChangeEvents([in] class Microsoft.Internal.VisualStudio.Shell.Interop.IVsSettingsListChangeEvents  marshal( interface ) 'handler') runtime managed internalcall
  {
    .param [0]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 10 45 6E 76 44 54 45 2E 55 4C 4F 4E 47 5F   // ...EnvDTE.ULONG_
                                                                                                            50 54 52 00 00 )                                  // PTR..
  } // end of method IVsSettingsList::AdviseListChangeEvents

  .method public hidebysig newslot abstract virtual 
          instance void  UnadviseListChangeEvents([in] uint32 cookie) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 10 45 6E 76 44 54 45 2E 55 4C 4F 4E 47 5F   // ...EnvDTE.ULONG_
                                                                                                            50 54 52 00 00 )                                  // PTR..
  } // end of method IVsSettingsList::UnadviseListChangeEvents

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.Internal.VisualStudio.Shell.Interop.IVsEnumerablePropertyBag 
          marshal( interface ) 
          GetEnum() runtime managed internalcall
  {
  } // end of method IVsSettingsList::GetEnum

} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.IVsSettingsList

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.IVsPropertyChangeEvents
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 45 33 38 46 39 42 36 39 2D 41 33 31 43   // ..$E38F9B69-A31C
                                                                                                  2D 34 30 39 36 2D 39 31 33 45 2D 31 44 31 31 37   // -4096-913E-1D117
                                                                                                  33 33 39 36 37 33 43 00 00 )                      // 339673C..
  .method public hidebysig newslot abstract virtual 
          instance class ['Microsoft.VisualStudio.Shell.Interop.11.0']Microsoft.VisualStudio.Shell.Interop.IVsTask 
          marshal( interface ) 
          OnPropertyChangedAsync([in] string  marshal( lpwstr) name) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 4C 50 43 57 53 54 52 00 00 ) // ...OLE.LPCWSTR..
  } // end of method IVsPropertyChangeEvents::OnPropertyChangedAsync

} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.IVsPropertyChangeEvents

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.IVsSettingsPersistenceManager
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 36 39 41 43 44 37 45 33 2D 36 41 31 32   // ..$69ACD7E3-6A12
                                                                                                  2D 34 41 42 35 2D 41 33 31 45 2D 43 36 37 34 43   // -4AB5-A31E-C674C
                                                                                                  34 42 41 32 44 41 36 00 00 )                      // 4BA2DA6..
  .method public hidebysig newslot abstract virtual 
          instance class ['Microsoft.VisualStudio.Shell.Interop.11.0']Microsoft.VisualStudio.Shell.Interop.IVsTask 
          marshal( interface ) 
          SetValueAsync([in] string  marshal( lpwstr) name,
                        [in] object&  marshal( struct) pValue,
                        [in] int32 isMachineLocal) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 4C 50 43 57 53 54 52 00 00 ) // ...OLE.LPCWSTR..
    .param [3]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 08 4F 4C 45 2E 42 4F 4F 4C 00 00 )          // ...OLE.BOOL..
  } // end of method IVsSettingsPersistenceManager::SetValueAsync

  .method public hidebysig newslot abstract virtual 
          instance object 
          marshal( struct) 
          GetValue([in] string  marshal( lpwstr) name) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 4C 50 43 57 53 54 52 00 00 ) // ...OLE.LPCWSTR..
  } // end of method IVsSettingsPersistenceManager::GetValue

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.Internal.VisualStudio.Shell.Interop.IVsSettingsList 
          marshal( interface ) 
          GetOrCreateList([in] string  marshal( lpwstr) name,
                          [in] int32 isMachineLocal) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 4C 50 43 57 53 54 52 00 00 ) // ...OLE.LPCWSTR..
    .param [2]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 08 4F 4C 45 2E 42 4F 4F 4C 00 00 )          // ...OLE.BOOL..
  } // end of method IVsSettingsPersistenceManager::GetOrCreateList

  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Array 
          marshal( safearray bstr) 
          GetNamesStartingWithPrefix([in] string  marshal( lpwstr) prefix) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 4C 50 43 57 53 54 52 00 00 ) // ...OLE.LPCWSTR..
  } // end of method IVsSettingsPersistenceManager::GetNamesStartingWithPrefix

  .method public hidebysig newslot abstract virtual 
          instance uint32  AdvisePropertyChangeEvents([in] class Microsoft.Internal.VisualStudio.Shell.Interop.IVsPropertyChangeEvents  marshal( interface ) 'handler',
                                                      string  marshal( lpwstr) namePattern) runtime managed internalcall
  {
    .param [0]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 10 45 6E 76 44 54 45 2E 55 4C 4F 4E 47 5F   // ...EnvDTE.ULONG_
                                                                                                            50 54 52 00 00 )                                  // PTR..
    .param [2]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 4C 50 43 57 53 54 52 00 00 ) // ...OLE.LPCWSTR..
  } // end of method IVsSettingsPersistenceManager::AdvisePropertyChangeEvents

  .method public hidebysig newslot abstract virtual 
          instance void  UnadvisePropertyChangeEvents([in] uint32 cookie) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 10 45 6E 76 44 54 45 2E 55 4C 4F 4E 47 5F   // ...EnvDTE.ULONG_
                                                                                                            50 54 52 00 00 )                                  // PTR..
  } // end of method IVsSettingsPersistenceManager::UnadvisePropertyChangeEvents

} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.IVsSettingsPersistenceManager

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.SVsSettingsPersistenceManager
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 39 42 31 36 34 45 34 30 2D 43 33 41 32   // ..$9B164E40-C3A2
                                                                                                  2D 34 33 36 33 2D 39 42 43 35 2D 45 42 34 30 33   // -4363-9BC5-EB403
                                                                                                  39 44 45 46 36 35 33 00 00 )                      // 9DEF653..
} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.SVsSettingsPersistenceManager

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.IVsPackageManagerPrivate
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 37 44 39 41 33 37 37 44 2D 32 39 33 45   // ..$7D9A377D-293E
                                                                                                  2D 34 34 30 31 2D 42 36 44 33 2D 43 41 41 30 43   // -4401-B6D3-CAA0C
                                                                                                  37 33 36 38 45 42 43 00 00 )                      // 7368EBC..
  .method public hidebysig newslot abstract virtual 
          instance class [Microsoft.VisualStudio.Shell.Interop]Microsoft.VisualStudio.Shell.Interop.IVsPackage 
          marshal( interface ) 
          LoadPackageWithContext([in] valuetype [mscorlib]System.Guid& packageGuid,
                                 [in] int32 reason,
                                 [in] valuetype [mscorlib]System.Guid& context) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 52 45 46 47 55 49 44 00 00 ) // ...OLE.REFGUID..
    .param [3]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 52 45 46 47 55 49 44 00 00 ) // ...OLE.REFGUID..
  } // end of method IVsPackageManagerPrivate::LoadPackageWithContext

  .method public hidebysig newslot abstract virtual 
          instance class ['Microsoft.VisualStudio.Shell.Interop.11.0']Microsoft.VisualStudio.Shell.Interop.IVsTask 
          marshal( interface ) 
          LoadPackageWithContextAsync([in] valuetype [mscorlib]System.Guid& packageGuid,
                                      [in] int32 reason,
                                      [in] valuetype [mscorlib]System.Guid& context) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 52 45 46 47 55 49 44 00 00 ) // ...OLE.REFGUID..
    .param [3]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 52 45 46 47 55 49 44 00 00 ) // ...OLE.REFGUID..
  } // end of method IVsPackageManagerPrivate::LoadPackageWithContextAsync

} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.IVsPackageManagerPrivate

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.SVsPackageManagerPrivate
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 45 45 46 46 34 36 35 41 2D 45 35 31 37   // ..$EEFF465A-E517
                                                                                                  2D 34 31 46 44 2D 38 30 32 30 2D 46 39 31 39 38   // -41FD-8020-F9198
                                                                                                  36 39 42 35 43 32 46 00 00 )                      // 69B5C2F..
} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.SVsPackageManagerPrivate

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.IVsWindowManager4
       implements ['Microsoft.Internal.VisualStudio.Shell.Interop.12.0.DesignTime']Microsoft.Internal.VisualStudio.Shell.Interop.IVsWindowManager3
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 31 45 46 30 32 35 33 46 2D 34 43 36 43   // ..$1EF0253F-4C6C
                                                                                                  2D 34 46 39 32 2D 42 36 31 42 2D 30 33 45 46 43   // -4F92-B61B-03EFC
                                                                                                  41 42 33 42 38 32 30 00 00 )                      // AB3B820..
  .method public hidebysig newslot abstract virtual 
          instance void  SaveActiveFrameLayoutEx([in] uint32 grfOptions) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 22 56 73 53 68 65 6C 6C 50 72 69 76 61 74   // .."VsShellPrivat
                                                                                                            65 31 31 30 2E 56 53 53 41 56 45 4C 41 59 4F 55   // e110.VSSAVELAYOU
                                                                                                            54 4F 50 54 53 00 00 )                            // TOPTS..
  } // end of method IVsWindowManager4::SaveActiveFrameLayoutEx

  .method public hidebysig newslot abstract virtual 
          instance native int  GetDialogOwnerHwnd() runtime managed internalcall
  {
  } // end of method IVsWindowManager4::GetDialogOwnerHwnd

  .method public hidebysig newslot abstract virtual 
          instance void  OnFrameAndDocDataInitialized([in] class [Microsoft.VisualStudio.Shell.Interop]Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame  marshal( interface ) pWindowFrame) runtime managed internalcall
  {
  } // end of method IVsWindowManager4::OnFrameAndDocDataInitialized

  .method public hidebysig newslot abstract virtual 
          instance object 
          marshal( iunknown ) 
          GetResourceKeyReferenceType([in] object  marshal( iunknown ) requestedResource) runtime managed internalcall
  {
  } // end of method IVsWindowManager4::GetResourceKeyReferenceType

  .method public hidebysig newslot abstract virtual 
          instance bool  IsInitializingDocument([in] string  marshal( lpwstr) filename) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsWindowManager4::IsInitializingDocument

  .method public hidebysig newslot abstract virtual 
          instance bool  IsInitializingLogicalViewForDocument([in] string  marshal( lpwstr) filename,
                                                              [in] valuetype [mscorlib]System.Guid& logicalView) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
    .param [2]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 52 45 46 47 55 49 44 00 00 ) // ...OLE.REFGUID..
  } // end of method IVsWindowManager4::IsInitializingLogicalViewForDocument

  .method public hidebysig newslot abstract virtual 
          instance bool  IsInitializingPhysicalViewForDocument([in] string  marshal( lpwstr) filename,
                                                               [in] valuetype [mscorlib]System.Guid& editorType,
                                                               [in] string  marshal( lpwstr) physicalView) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
    .param [2]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 52 45 46 47 55 49 44 00 00 ) // ...OLE.REFGUID..
    .param [3]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsWindowManager4::IsInitializingPhysicalViewForDocument

  .method public hidebysig newslot abstract virtual 
          instance bool  GetPrimaryDocumentWellScreenRect([out] int32& screenLeft,
                                                          [out] int32& screenTop,
                                                          [out] int32& screenWidth,
                                                          [out] int32& screenHeight) runtime managed internalcall
  {
  } // end of method IVsWindowManager4::GetPrimaryDocumentWellScreenRect

  .method public hidebysig newslot abstract virtual 
          instance void  SaveFrameLayoutCollection([in] class [Microsoft.VisualStudio.OLE.Interop]Microsoft.VisualStudio.OLE.Interop.IStream  marshal( interface ) 'stream',
                                                   [in] uint32 options) runtime managed internalcall
  {
    .param [2]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 22 56 73 53 68 65 6C 6C 50 72 69 76 61 74   // .."VsShellPrivat
                                                                                                            65 31 31 30 2E 56 53 53 41 56 45 4C 41 59 4F 55   // e110.VSSAVELAYOU
                                                                                                            54 4F 50 54 53 00 00 )                            // TOPTS..
  } // end of method IVsWindowManager4::SaveFrameLayoutCollection

  .method public hidebysig newslot abstract virtual 
          instance void  RestoreFrameLayoutCollection([in] class [Microsoft.VisualStudio.OLE.Interop]Microsoft.VisualStudio.OLE.Interop.IStream  marshal( interface ) 'stream') runtime managed internalcall
  {
  } // end of method IVsWindowManager4::RestoreFrameLayoutCollection

} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.IVsWindowManager4

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryEvent
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 35 44 43 35 46 38 35 45 2D 43 34 39 42   // ..$5DC5F85E-C49B
                                                                                                  2D 34 43 46 36 2D 39 37 42 37 2D 42 44 45 46 37   // -4CF6-97B7-BDEF7
                                                                                                  41 44 30 41 44 45 33 00 00 )                      // AD0ADE3..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance string 
          marshal( bstr) 
          GetName() runtime managed internalcall
  {
  } // end of method IVsTelemetryEvent::GetName

  .method public hidebysig newslot abstract virtual 
          instance object 
          marshal( struct) 
          GetProperty([in] string  marshal( lpwstr) szPropertyName) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryEvent::GetProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetProperty([in] string  marshal( lpwstr) szPropertyName,
                                     [in] object  marshal( struct) varValue) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryEvent::SetProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetBoolProperty([in] string  marshal( lpwstr) szPropertyName,
                                         [in] bool 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryEvent::SetBoolProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetDoubleProperty([in] string  marshal( lpwstr) szPropertyName,
                                           [in] float64 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryEvent::SetDoubleProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetIntProperty([in] string  marshal( lpwstr) szPropertyName,
                                        [in] int32 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryEvent::SetIntProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetLongProperty([in] string  marshal( lpwstr) szPropertyName,
                                         [in] int64 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryEvent::SetLongProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetShortProperty([in] string  marshal( lpwstr) szPropertyName,
                                          [in] int16 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryEvent::SetShortProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetStringProperty([in] string  marshal( lpwstr) szPropertyName,
                                           [in] string  marshal( lpwstr) szValue) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
    .param [2]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryEvent::SetStringProperty

  .method public hidebysig newslot abstract virtual 
          instance bool  ContainsProperty([in] string  marshal( lpwstr) szPropertyName) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryEvent::ContainsProperty

  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Array 
          marshal( safearray bstr) 
          GetAllPropertiesNames() runtime managed internalcall
  {
  } // end of method IVsTelemetryEvent::GetAllPropertiesNames

} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryEvent

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryActivity
       implements Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryEvent
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 38 46 30 45 33 37 46 39 2D 42 31 46 36   // ..$8F0E37F9-B1F6
                                                                                                  2D 34 32 34 37 2D 39 43 39 31 2D 44 38 30 31 46   // -4247-9C91-D801F
                                                                                                  39 32 37 31 30 41 35 00 00 )                      // 92710A5..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance string 
          marshal( bstr) 
          GetName() runtime managed internalcall
  {
  } // end of method IVsTelemetryActivity::GetName

  .method public hidebysig newslot abstract virtual 
          instance object 
          marshal( struct) 
          GetProperty([in] string  marshal( lpwstr) szPropertyName) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryActivity::GetProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetProperty([in] string  marshal( lpwstr) szPropertyName,
                                     [in] object  marshal( struct) varValue) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryActivity::SetProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetBoolProperty([in] string  marshal( lpwstr) szPropertyName,
                                         [in] bool 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryActivity::SetBoolProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetDoubleProperty([in] string  marshal( lpwstr) szPropertyName,
                                           [in] float64 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryActivity::SetDoubleProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetIntProperty([in] string  marshal( lpwstr) szPropertyName,
                                        [in] int32 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryActivity::SetIntProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetLongProperty([in] string  marshal( lpwstr) szPropertyName,
                                         [in] int64 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryActivity::SetLongProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetShortProperty([in] string  marshal( lpwstr) szPropertyName,
                                          [in] int16 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryActivity::SetShortProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetStringProperty([in] string  marshal( lpwstr) szPropertyName,
                                           [in] string  marshal( lpwstr) szValue) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
    .param [2]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryActivity::SetStringProperty

  .method public hidebysig newslot abstract virtual 
          instance bool  ContainsProperty([in] string  marshal( lpwstr) szPropertyName) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryActivity::ContainsProperty

  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Array 
          marshal( safearray bstr) 
          GetAllPropertiesNames() runtime managed internalcall
  {
  } // end of method IVsTelemetryActivity::GetAllPropertiesNames

  .method public hidebysig newslot abstract virtual 
          instance void  Start() runtime managed internalcall
  {
  } // end of method IVsTelemetryActivity::Start

  .method public hidebysig newslot abstract virtual 
          instance void  End() runtime managed internalcall
  {
  } // end of method IVsTelemetryActivity::End

  .method public hidebysig newslot abstract virtual 
          instance void  EndWithDuration([in] int64 eventDuration) runtime managed internalcall
  {
  } // end of method IVsTelemetryActivity::EndWithDuration

  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.Guid 
          get_CorrelationId() runtime managed internalcall
  {
  } // end of method IVsTelemetryActivity::get_CorrelationId

  .property valuetype [mscorlib]System.Guid
          CorrelationId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 02 60 00 00 )                         // .....`..
    .get instance valuetype [mscorlib]System.Guid Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryActivity::get_CorrelationId()
  } // end of property IVsTelemetryActivity::CorrelationId
} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryActivity

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryContext
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 38 32 43 42 31 38 45 41 2D 35 33 33 30   // ..$82CB18EA-5330
                                                                                                  2D 34 41 44 42 2D 42 44 37 39 2D 45 42 39 36 34   // -4ADB-BD79-EB964
                                                                                                  46 33 30 37 34 39 44 00 00 )                      // F30749D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_ContextName() runtime managed internalcall
  {
  } // end of method IVsTelemetryContext::get_ContextName

  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.Guid 
          get_ContextId() runtime managed internalcall
  {
  } // end of method IVsTelemetryContext::get_ContextId

  .method public hidebysig newslot abstract virtual 
          instance void  PostProperty([in] string  marshal( lpwstr) szPropertyName,
                                      [in] object  marshal( struct) varValue) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryContext::PostProperty

  .method public hidebysig newslot abstract virtual 
          instance void  PostBoolProperty([in] string  marshal( lpwstr) szPropertyName,
                                          [in] bool 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryContext::PostBoolProperty

  .method public hidebysig newslot abstract virtual 
          instance void  PostDoubleProperty([in] string  marshal( lpwstr) szPropertyName,
                                            [in] float64 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryContext::PostDoubleProperty

  .method public hidebysig newslot abstract virtual 
          instance void  PostIntProperty([in] string  marshal( lpwstr) szPropertyName,
                                         [in] int32 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryContext::PostIntProperty

  .method public hidebysig newslot abstract virtual 
          instance void  PostLongProperty([in] string  marshal( lpwstr) szPropertyName,
                                          [in] int64 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryContext::PostLongProperty

  .method public hidebysig newslot abstract virtual 
          instance void  PostShortProperty([in] string  marshal( lpwstr) szPropertyName,
                                           [in] int16 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryContext::PostShortProperty

  .method public hidebysig newslot abstract virtual 
          instance void  PostStringProperty([in] string  marshal( lpwstr) szPropertyName,
                                            [in] string  marshal( lpwstr) szValue) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
    .param [2]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryContext::PostStringProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetSharedProperty([in] string  marshal( lpwstr) szPropertyName,
                                           [in] object  marshal( struct) varValue) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryContext::SetSharedProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetSharedBoolProperty([in] string  marshal( lpwstr) szPropertyName,
                                               [in] bool 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryContext::SetSharedBoolProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetSharedDoubleProperty([in] string  marshal( lpwstr) szPropertyName,
                                                 [in] float64 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryContext::SetSharedDoubleProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetSharedIntProperty([in] string  marshal( lpwstr) szPropertyName,
                                              [in] int32 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryContext::SetSharedIntProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetSharedLongProperty([in] string  marshal( lpwstr) szPropertyName,
                                               [in] int64 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryContext::SetSharedLongProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetSharedShortProperty([in] string  marshal( lpwstr) szPropertyName,
                                                [in] int16 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryContext::SetSharedShortProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetSharedStringProperty([in] string  marshal( lpwstr) szPropertyName,
                                                 [in] string  marshal( lpwstr) szValue) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
    .param [2]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryContext::SetSharedStringProperty

  .method public hidebysig newslot abstract virtual 
          instance void  RemoveSharedProperty([in] string  marshal( lpwstr) szPropertyName) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryContext::RemoveSharedProperty

  .method public hidebysig newslot abstract virtual 
          instance void  Close() runtime managed internalcall
  {
  } // end of method IVsTelemetryContext::Close

  .property string ContextName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 01 60 00 00 )                         // .....`..
    .get instance string Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryContext::get_ContextName()
  } // end of property IVsTelemetryContext::ContextName
  .property valuetype [mscorlib]System.Guid
          ContextId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 01 60 00 00 )                         // .....`..
    .get instance valuetype [mscorlib]System.Guid Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryContext::get_ContextId()
  } // end of property IVsTelemetryContext::ContextId
} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryContext

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetrySession
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 42 35 36 37 44 30 43 30 2D 39 33 33 34   // ..$B567D0C0-9334
                                                                                                  2D 34 45 46 42 2D 38 31 31 42 2D 46 31 32 41 45   // -4EFB-811B-F12AE
                                                                                                  45 34 30 38 41 33 44 00 00 )                      // E408A3D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance string 
          marshal( bstr) 
          GetSessionId() runtime managed internalcall
  {
  } // end of method IVsTelemetrySession::GetSessionId

  .method public hidebysig newslot abstract virtual 
          instance string 
          marshal( bstr) 
          SerializeSettings() runtime managed internalcall
  {
  } // end of method IVsTelemetrySession::SerializeSettings

  .method public hidebysig newslot abstract virtual 
          instance void  PostSimpleEvent([in] string  marshal( lpwstr) szEventName) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetrySession::PostSimpleEvent

  .method public hidebysig newslot abstract virtual 
          instance void  PostEvent([in] class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryEvent  marshal( interface ) eventObject) runtime managed internalcall
  {
  } // end of method IVsTelemetrySession::PostEvent

  .method public hidebysig newslot abstract virtual 
          instance void  PostProperty([in] string  marshal( lpwstr) szPropertyName,
                                      [in] object  marshal( struct) varValue) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetrySession::PostProperty

  .method public hidebysig newslot abstract virtual 
          instance void  PostBoolProperty([in] string  marshal( lpwstr) szPropertyName,
                                          [in] bool 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetrySession::PostBoolProperty

  .method public hidebysig newslot abstract virtual 
          instance void  PostDoubleProperty([in] string  marshal( lpwstr) szPropertyName,
                                            [in] float64 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetrySession::PostDoubleProperty

  .method public hidebysig newslot abstract virtual 
          instance void  PostIntProperty([in] string  marshal( lpwstr) szPropertyName,
                                         [in] int32 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetrySession::PostIntProperty

  .method public hidebysig newslot abstract virtual 
          instance void  PostLongProperty([in] string  marshal( lpwstr) szPropertyName,
                                          [in] int64 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetrySession::PostLongProperty

  .method public hidebysig newslot abstract virtual 
          instance void  PostShortProperty([in] string  marshal( lpwstr) szPropertyName,
                                           [in] int16 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetrySession::PostShortProperty

  .method public hidebysig newslot abstract virtual 
          instance void  PostStringProperty([in] string  marshal( lpwstr) szPropertyName,
                                            [in] string  marshal( lpwstr) szValue) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
    .param [2]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetrySession::PostStringProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetSharedProperty([in] string  marshal( lpwstr) szPropertyName,
                                           [in] object  marshal( struct) varValue) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetrySession::SetSharedProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetSharedBoolProperty([in] string  marshal( lpwstr) szPropertyName,
                                               [in] bool 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetrySession::SetSharedBoolProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetSharedDoubleProperty([in] string  marshal( lpwstr) szPropertyName,
                                                 [in] float64 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetrySession::SetSharedDoubleProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetSharedIntProperty([in] string  marshal( lpwstr) szPropertyName,
                                              [in] int32 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetrySession::SetSharedIntProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetSharedLongProperty([in] string  marshal( lpwstr) szPropertyName,
                                               [in] int64 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetrySession::SetSharedLongProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetSharedShortProperty([in] string  marshal( lpwstr) szPropertyName,
                                                [in] int16 'value') runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetrySession::SetSharedShortProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetSharedStringProperty([in] string  marshal( lpwstr) szPropertyName,
                                                 [in] string  marshal( lpwstr) szValue) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
    .param [2]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetrySession::SetSharedStringProperty

  .method public hidebysig newslot abstract virtual 
          instance void  RemoveSharedProperty([in] string  marshal( lpwstr) szPropertyName) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetrySession::RemoveSharedProperty

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryContext 
          marshal( interface ) 
          CreateContext([in] string  marshal( lpwstr) szContextName) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetrySession::CreateContext

} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetrySession

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.SVsTelemetryService
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 39 35 39 30 36 39 43 39 2D 39 45 39 33   // ..$959069C9-9E93
                                                                                                  2D 34 46 41 31 2D 42 46 31 36 2D 33 46 38 31 32   // -4FA1-BF16-3F812
                                                                                                  30 44 37 44 42 30 43 00 00 )                      // 0D7DB0C..
} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.SVsTelemetryService

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryService
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 35 43 37 45 37 30 32 39 2D 41 30 30 43   // ..$5C7E7029-A00C
                                                                                                  2D 34 46 35 37 2D 42 45 31 35 2D 36 41 43 35 44   // -4F57-BE15-6AC5D
                                                                                                  34 33 45 37 38 43 42 00 00 )                      // 43E78CB..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetrySession 
          marshal( interface ) 
          GetDefaultSession() runtime managed internalcall
  {
  } // end of method IVsTelemetryService::GetDefaultSession

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryEvent 
          marshal( interface ) 
          CreateEvent([in] string  marshal( lpwstr) szEventName) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryService::CreateEvent

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryActivity 
          marshal( interface ) 
          CreateActivity([in] string  marshal( lpwstr) szActivityName) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
  } // end of method IVsTelemetryService::CreateActivity

  .method public hidebysig newslot abstract virtual 
          instance class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryActivity 
          marshal( interface ) 
          CreateActivityWithParentCorrelationId([in] string  marshal( lpwstr) szActivityName,
                                                [in] valuetype [mscorlib]System.Guid& parentCorrelationId) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0D 4F 4C 45 2E 4C 50 43 4F 4C 45 53 54 52   // ...OLE.LPCOLESTR
                                                                                                            00 00 ) 
    .param [2]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 0B 4F 4C 45 2E 52 45 46 47 55 49 44 00 00 ) // ...OLE.REFGUID..
  } // end of method IVsTelemetryService::CreateActivityWithParentCorrelationId

} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.IVsTelemetryService

.class interface public abstract auto ansi import Microsoft.Internal.VisualStudio.Shell.Interop.IVsCoreTelemetry
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 41 34 42 31 42 37 43 42 2D 43 34 38 34   // ..$A4B1B7CB-C484
                                                                                                  2D 34 37 41 44 2D 41 38 46 35 2D 38 33 30 44 39   // -47AD-A8F5-830D9
                                                                                                  33 35 37 39 38 39 44 00 00 )                      // 357989D..
  .method public hidebysig newslot abstract virtual 
          instance void  RecordCommandData([in] valuetype [mscorlib]System.Guid& pguidCmdGroup,
                                           [in] uint32 dwCmdId,
                                           [in] uint32 dwVal) runtime managed internalcall
  {
    .param [2]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 09 4F 4C 45 2E 44 57 4F 52 44 00 00 )       // ...OLE.DWORD..
    .param [3]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 09 4F 4C 45 2E 44 57 4F 52 44 00 00 )       // ...OLE.DWORD..
  } // end of method IVsCoreTelemetry::RecordCommandData

  .method public hidebysig newslot abstract virtual 
          instance void  RecordExtensionDataInstall([in] uint32 dwType,
                                                    [in] string  marshal( bstr) strName,
                                                    [in] uint32 dwSqmId,
                                                    [in] string  marshal( bstr) strVersion,
                                                    [in] string  marshal( bstr) strIdentifier) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 09 4F 4C 45 2E 44 57 4F 52 44 00 00 )       // ...OLE.DWORD..
    .param [3]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 09 4F 4C 45 2E 44 57 4F 52 44 00 00 )       // ...OLE.DWORD..
  } // end of method IVsCoreTelemetry::RecordExtensionDataInstall

  .method public hidebysig newslot abstract virtual 
          instance void  RecordExtensionDataUsage([in] uint32 dwType,
                                                  [in] string  marshal( bstr) strName,
                                                  [in] uint32 dwSqmId,
                                                  [in] string  marshal( bstr) strVersion,
                                                  [in] string  marshal( bstr) strIdentifier) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 09 4F 4C 45 2E 44 57 4F 52 44 00 00 )       // ...OLE.DWORD..
    .param [3]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 09 4F 4C 45 2E 44 57 4F 52 44 00 00 )       // ...OLE.DWORD..
  } // end of method IVsCoreTelemetry::RecordExtensionDataUsage

} // end of class Microsoft.Internal.VisualStudio.Shell.Interop.IVsCoreTelemetry


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file Microsoft.Internal.VisualStudio.Shell.Interop.14.0.DesignTime.res
