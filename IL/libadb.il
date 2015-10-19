// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
//
// This file is used to create a reference assembly for libadb.dll. This reference dll is NOT runable.
// Libadb is not currently a public API. This file is provided to allow the AndroidDebugLauncher to build, but it isn't available for use outside of the AndroidDebugLauncher.


// Metadata version: v4.0.30319
.assembly extern System
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly extern WindowsBase
{
  .publickeytoken = (31 BF 38 56 AD 36 4E 35 )                         // 1.8V.6N5
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly libadb
{
  .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = ( 01 00 01 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool) = ( 01 00 00 00 00 ) 
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
.module libadb.dll
// MVID: {4BD082B3-93C4-4AEA-B4B2-4B69EA059E80}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00860000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi libadb.Activity
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance string 
          get_Name() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Activity::get_Name

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string name) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Activity::.ctor

  .property instance string Name()
  {
    .get instance string libadb.Activity::get_Name()
  } // end of property Activity::Name
} // end of class libadb.Activity

.class public auto ansi libadb.Adb
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance class [mscorlib]System.Version 
          get_Version() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Adb::get_Version

  .method public hidebysig specialname instance bool 
          get_IsAdbRunning() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Adb::get_IsAdbRunning

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Adb::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string sdkLocation) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Adb::.ctor

  .method public hidebysig instance class [mscorlib]System.Collections.Generic.IEnumerable`1<class libadb.Device> 
          GetDevices() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Adb::GetDevices

  .method public hidebysig instance class libadb.Device 
          GetDeviceById(string deviceId) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Adb::GetDeviceById

  .method public hidebysig instance bool 
          TryGetDeviceById(string deviceId,
                           [out] class libadb.Device& device) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Adb::TryGetDeviceById

  .method public hidebysig instance void 
          Start() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Adb::Start

  .method public hidebysig instance void 
          Stop() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Adb::Stop

  .property instance bool IsAdbRunning()
  {
    .get instance bool libadb.Adb::get_IsAdbRunning()
  } // end of property Adb::IsAdbRunning
  .property instance class [mscorlib]System.Version
          Version()
  {
    .get instance class [mscorlib]System.Version libadb.Adb::get_Version()
  } // end of property Adb::Version
} // end of class libadb.Adb

.class public auto ansi libadb.Device
       extends [mscorlib]System.Object
       implements [mscorlib]System.IDisposable,
                  [mscorlib]System.ICloneable
{
  .method public hidebysig specialname instance string 
          get_Id() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::get_Id

  .method public hidebysig specialname instance string 
          get_DisplayName() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::get_DisplayName

  .method public hidebysig specialname instance string 
          get_Manufacturer() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::get_Manufacturer

  .method public hidebysig specialname instance string 
          get_Name() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::get_Name

  .method public hidebysig specialname instance string 
          get_Model() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::get_Model

  .method public hidebysig specialname instance bool 
          get_IsEmulator() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::get_IsEmulator

  .method public hidebysig specialname instance valuetype libadb.EmulatorId 
          get_EmulatorId() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::get_EmulatorId

  .method public hidebysig specialname instance valuetype libadb.DeviceAbi 
          get_Abi() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::get_Abi

  .method public hidebysig specialname instance valuetype libadb.DeviceAbi 
          get_Abi2() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::get_Abi2

  .method public hidebysig specialname instance valuetype libadb.ApiLevel 
          get_ApiLevel() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::get_ApiLevel

  .method public hidebysig specialname instance class [mscorlib]System.Version 
          get_SdkVersion() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::get_SdkVersion

  .method public hidebysig specialname instance valuetype libadb.DeviceState 
          get_LastState() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::get_LastState

  .method public hidebysig specialname instance class libadb.DeviceProperties 
          get_Properties() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::get_Properties

  .method public hidebysig specialname instance class libadb.AdbShell 
          get_Shell() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::get_Shell

  .method public hidebysig specialname instance class libadb.IO.FileSystem 
          get_FileSystem() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::get_FileSystem

  .method public hidebysig specialname instance class libadb.AppCollection 
          get_Apps() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::get_Apps

  .method public hidebysig specialname instance class libadb.ProcessCollection 
          get_Processes() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::get_Processes

  .method public hidebysig specialname instance class libadb.Logcat 
          get_Logcat() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::get_Logcat

  .method public hidebysig instance void 
          Forward(string local,
                  string remote) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::Forward

  .method public hidebysig instance valuetype libadb.DeviceState 
          GetState() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::GetState

  .method public hidebysig instance void 
          Refresh() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::Refresh

  .method public hidebysig instance void 
          Unlock() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::Unlock

  .method public hidebysig newslot virtual final 
          instance object  Clone() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::Clone

  .method public hidebysig newslot virtual final 
          instance void  Dispose() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Device::Dispose

  .property instance valuetype libadb.DeviceAbi
          Abi()
  {
    .get instance valuetype libadb.DeviceAbi libadb.Device::get_Abi()
  } // end of property Device::Abi
  .property instance valuetype libadb.DeviceAbi
          Abi2()
  {
    .get instance valuetype libadb.DeviceAbi libadb.Device::get_Abi2()
  } // end of property Device::Abi2
  .property instance valuetype libadb.ApiLevel
          ApiLevel()
  {
    .get instance valuetype libadb.ApiLevel libadb.Device::get_ApiLevel()
  } // end of property Device::ApiLevel
  .property instance class libadb.AppCollection
          Apps()
  {
    .get instance class libadb.AppCollection libadb.Device::get_Apps()
  } // end of property Device::Apps
  .property instance string DisplayName()
  {
    .get instance string libadb.Device::get_DisplayName()
  } // end of property Device::DisplayName
  .property instance valuetype libadb.EmulatorId
          EmulatorId()
  {
    .get instance valuetype libadb.EmulatorId libadb.Device::get_EmulatorId()
  } // end of property Device::EmulatorId
  .property instance class libadb.IO.FileSystem
          FileSystem()
  {
    .get instance class libadb.IO.FileSystem libadb.Device::get_FileSystem()
  } // end of property Device::FileSystem
  .property instance string Id()
  {
    .get instance string libadb.Device::get_Id()
  } // end of property Device::Id
  .property instance bool IsEmulator()
  {
    .get instance bool libadb.Device::get_IsEmulator()
  } // end of property Device::IsEmulator
  .property instance valuetype libadb.DeviceState
          LastState()
  {
    .get instance valuetype libadb.DeviceState libadb.Device::get_LastState()
  } // end of property Device::LastState
  .property instance class libadb.Logcat Logcat()
  {
    .get instance class libadb.Logcat libadb.Device::get_Logcat()
  } // end of property Device::Logcat
  .property instance string Manufacturer()
  {
    .get instance string libadb.Device::get_Manufacturer()
  } // end of property Device::Manufacturer
  .property instance string Model()
  {
    .get instance string libadb.Device::get_Model()
  } // end of property Device::Model
  .property instance string Name()
  {
    .get instance string libadb.Device::get_Name()
  } // end of property Device::Name
  .property instance class libadb.ProcessCollection
          Processes()
  {
    .get instance class libadb.ProcessCollection libadb.Device::get_Processes()
  } // end of property Device::Processes
  .property instance class libadb.DeviceProperties
          Properties()
  {
    .get instance class libadb.DeviceProperties libadb.Device::get_Properties()
  } // end of property Device::Properties
  .property instance class [mscorlib]System.Version
          SdkVersion()
  {
    .get instance class [mscorlib]System.Version libadb.Device::get_SdkVersion()
  } // end of property Device::SdkVersion
  .property instance class libadb.AdbShell
          Shell()
  {
    .get instance class libadb.AdbShell libadb.Device::get_Shell()
  } // end of property Device::Shell
} // end of class libadb.Device

.class public auto ansi libadb.AdbException
       extends [mscorlib]System.Exception
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AdbException::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string message) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AdbException::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string message,
                               class [mscorlib]System.Exception innerException) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AdbException::.ctor

} // end of class libadb.AdbException

.class public auto ansi libadb.AdbShell
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance string 
          get_Out() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AdbShell::get_Out

  .method public hidebysig instance bool 
          Exec(string command) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AdbShell::Exec

  .method public hidebysig instance bool 
          Exec(string command,
               int32 millisecondsTimeout) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AdbShell::Exec

  .method public hidebysig instance class [mscorlib]System.Threading.Tasks.Task 
          ExecAsync(string command,
                    valuetype [mscorlib]System.Threading.CancellationToken cancellationToken,
                    class [mscorlib]System.Action`1<string> callback) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AdbShell::ExecAsync

  .property instance string Out()
  {
    .get instance string libadb.AdbShell::get_Out()
  } // end of property AdbShell::Out
} // end of class libadb.AdbShell

.class public auto ansi libadb.AdbStartException
       extends [mscorlib]System.Exception
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AdbStartException::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string message) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AdbStartException::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string message,
                               class [mscorlib]System.Exception innerException) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AdbStartException::.ctor

} // end of class libadb.AdbStartException

.class public auto ansi sealed libadb.ApiLevel
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype libadb.ApiLevel Level10 = int32(0x0000000A)
  .field public static literal valuetype libadb.ApiLevel Level11 = int32(0x0000000B)
  .field public static literal valuetype libadb.ApiLevel Level12 = int32(0x0000000C)
  .field public static literal valuetype libadb.ApiLevel Level13 = int32(0x0000000D)
  .field public static literal valuetype libadb.ApiLevel Level14 = int32(0x0000000E)
  .field public static literal valuetype libadb.ApiLevel Level15 = int32(0x0000000F)
  .field public static literal valuetype libadb.ApiLevel Level16 = int32(0x00000010)
  .field public static literal valuetype libadb.ApiLevel Level17 = int32(0x00000011)
  .field public static literal valuetype libadb.ApiLevel Level18 = int32(0x00000012)
  .field public static literal valuetype libadb.ApiLevel Level19 = int32(0x00000013)
  .field public static literal valuetype libadb.ApiLevel Level20 = int32(0x00000014)
  .field public static literal valuetype libadb.ApiLevel Level21 = int32(0x00000015)
  .field public static literal valuetype libadb.ApiLevel Level3 = int32(0x00000003)
  .field public static literal valuetype libadb.ApiLevel Level4 = int32(0x00000004)
  .field public static literal valuetype libadb.ApiLevel Level7 = int32(0x00000007)
  .field public static literal valuetype libadb.ApiLevel Level8 = int32(0x00000008)
} // end of class libadb.ApiLevel

.class public auto ansi libadb.ApkInfo
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance string 
          get_FullPath() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ApkInfo::get_FullPath

  .method public hidebysig specialname instance string 
          get_Name() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ApkInfo::get_Name

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string apkPath,
                               string appName) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ApkInfo::.ctor

  .property instance string FullPath()
  {
    .get instance string libadb.ApkInfo::get_FullPath()
  } // end of property ApkInfo::FullPath
  .property instance string Name()
  {
    .get instance string libadb.ApkInfo::get_Name()
  } // end of property ApkInfo::Name
} // end of class libadb.ApkInfo

.class public auto ansi libadb.App
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance string 
          get_Name() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method App::get_Name

  .method public hidebysig specialname instance valuetype [mscorlib]System.DateTime 
          get_InstallationTime() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method App::get_InstallationTime

  .method public hidebysig instance class libadb.ProcessInfo 
          Start(class libadb.Activity activity) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method App::Start

  .property instance valuetype [mscorlib]System.DateTime
          InstallationTime()
  {
    .get instance valuetype [mscorlib]System.DateTime libadb.App::get_InstallationTime()
  } // end of property App::InstallationTime
  .property instance string Name()
  {
    .get instance string libadb.App::get_Name()
  } // end of property App::Name
} // end of class libadb.App

.class public auto ansi libadb.ProcessInfo
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance int32 
          get_Id() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ProcessInfo::get_Id

  .method public hidebysig specialname instance string 
          get_Name() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ProcessInfo::get_Name

  .method public hidebysig specialname instance valuetype libadb.ProcessState 
          get_State() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ProcessInfo::get_State

  .property instance int32 Id()
  {
    .get instance int32 libadb.ProcessInfo::get_Id()
  } // end of property ProcessInfo::Id
  .property instance string Name()
  {
    .get instance string libadb.ProcessInfo::get_Name()
  } // end of property ProcessInfo::Name
  .property instance valuetype libadb.ProcessState
          State()
  {
    .get instance valuetype libadb.ProcessState libadb.ProcessInfo::get_State()
  } // end of property ProcessInfo::State
} // end of class libadb.ProcessInfo

.class public auto ansi libadb.AppCollection
       extends [mscorlib]System.Object
       implements class [mscorlib]System.Collections.Generic.IEnumerable`1<class libadb.App>,
                  [mscorlib]System.Collections.IEnumerable
{
  .method public hidebysig instance class libadb.App 
          Install(class libadb.ApkInfo apk) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AppCollection::Install

  .method public hidebysig instance bool 
          IsInstalled(string appName) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AppCollection::IsInstalled

  .method public hidebysig instance class libadb.App 
          GetAppByName(string appName) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AppCollection::GetAppByName

  .method public hidebysig instance bool 
          TryGetAppByName(string appName,
                          [out] class libadb.App& app) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AppCollection::TryGetAppByName

  .method public hidebysig instance void 
          Refresh() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AppCollection::Refresh

  .method public hidebysig newslot virtual final 
          instance class [mscorlib]System.Collections.Generic.IEnumerator`1<class libadb.App> 
          GetEnumerator() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AppCollection::GetEnumerator

  .method private hidebysig newslot virtual final 
          instance class [mscorlib]System.Collections.IEnumerator 
          System.Collections.IEnumerable.GetEnumerator() cil managed
  {
    .override [mscorlib]System.Collections.IEnumerable::GetEnumerator
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AppCollection::System.Collections.IEnumerable.GetEnumerator

} // end of class libadb.AppCollection

.class public auto ansi sealed libadb.AppInstallErrorCode
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype libadb.AppInstallErrorCode AlreadyExists = int32(0x00000008)
  .field public static literal valuetype libadb.AppInstallErrorCode ConfictingProvider = int32(0x0000000F)
  .field public static literal valuetype libadb.AppInstallErrorCode CpuAbiIncompatible = int32(0x0000000B)
  .field public static literal valuetype libadb.AppInstallErrorCode InconsistentCertificates = int32(0x00000009)
  .field public static literal valuetype libadb.AppInstallErrorCode InsufficientStorage = int32(0x00000003)
  .field public static literal valuetype libadb.AppInstallErrorCode InternalError = int32(0x0000000E)
  .field public static literal valuetype libadb.AppInstallErrorCode InvalidApk = int32(0x00000002)
  .field public static literal valuetype libadb.AppInstallErrorCode InvalidInstallLocation = int32(0x0000000D)
  .field public static literal valuetype libadb.AppInstallErrorCode InvalidUri = int32(0x0000000C)
  .field public static literal valuetype libadb.AppInstallErrorCode MissingSharedLibrary = int32(0x00000007)
  .field public static literal valuetype libadb.AppInstallErrorCode NoCertificates = int32(0x0000000A)
  .field public static literal valuetype libadb.AppInstallErrorCode None = int32(0x00000000)
  .field public static literal valuetype libadb.AppInstallErrorCode OlderSdk = int32(0x00000004)
  .field public static literal valuetype libadb.AppInstallErrorCode SharedUserIncompatible = int32(0x00000006)
  .field public static literal valuetype libadb.AppInstallErrorCode Unknown = int32(0x00000001)
  .field public static literal valuetype libadb.AppInstallErrorCode VerificationFailure = int32(0x00000005)
} // end of class libadb.AppInstallErrorCode

.class public auto ansi libadb.AppInstallException
       extends libadb.AdbException
{
  .method public hidebysig specialname instance valuetype libadb.AppInstallErrorCode 
          get_ErrorCode() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AppInstallException::get_ErrorCode

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AppInstallException::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(valuetype libadb.AppInstallErrorCode errorCode) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AppInstallException::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string message) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AppInstallException::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string message,
                               valuetype libadb.AppInstallErrorCode errorCode) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AppInstallException::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string message,
                               class [mscorlib]System.Exception innerException) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method AppInstallException::.ctor

  .property instance valuetype libadb.AppInstallErrorCode
          ErrorCode()
  {
    .get instance valuetype libadb.AppInstallErrorCode libadb.AppInstallException::get_ErrorCode()
  } // end of property AppInstallException::ErrorCode
} // end of class libadb.AppInstallException

.class public auto ansi sealed libadb.EmulatorId
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype libadb.EmulatorId Genymotion = int32(0x00000004)
  .field public static literal valuetype libadb.EmulatorId Microsoft = int32(0x00000003)
  .field public static literal valuetype libadb.EmulatorId Unknown = int32(0x00000001)
  .field public static literal valuetype libadb.EmulatorId Xamarin = int32(0x00000005)
  .field public static literal valuetype libadb.EmulatorId qemu = int32(0x00000002)
} // end of class libadb.EmulatorId

.class public auto ansi sealed libadb.DeviceAbi
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype libadb.DeviceAbi unknown = int32(0x00000000)
  .field public static literal valuetype libadb.DeviceAbi 'x86' = int32(0x00000001)
  .field public static literal valuetype libadb.DeviceAbi armeabi = int32(0x00000002)
  .field public static literal valuetype libadb.DeviceAbi armeabiv7a = int32(0x00000003)
  .field public static literal valuetype libadb.DeviceAbi mips = int32(0x00000004)
  .field public static literal valuetype libadb.DeviceAbi mipsr2 = int32(0x00000005)
  .field public static literal valuetype libadb.DeviceAbi x64 = int32(0x00000006)
  .field public static literal valuetype libadb.DeviceAbi arm64v8a = int32(0x00000007)
} // end of class libadb.DeviceAbi

.class public auto ansi sealed libadb.DeviceState
       extends [mscorlib]System.Enum
{
  .custom instance void [mscorlib]System.FlagsAttribute::.ctor() = ( 01 00 00 00 ) 
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype libadb.DeviceState Booting = int32(0x00000004)
  .field public static literal valuetype libadb.DeviceState Offline = int32(0x00000002)
  .field public static literal valuetype libadb.DeviceState Online = int32(0x00000001)
  .field public static literal valuetype libadb.DeviceState Unknown = int32(0x00000000)
} // end of class libadb.DeviceState

.class public auto ansi libadb.DeviceProperties
       extends [mscorlib]System.Object
       implements class [mscorlib]System.Collections.Generic.IEnumerable`1<valuetype [mscorlib]System.Collections.Generic.KeyValuePair`2<string,string>>,
                  [mscorlib]System.Collections.IEnumerable
{
  .method public hidebysig instance string 
          GetPropertyById(class libadb.DevicePropertyId propertyId) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceProperties::GetPropertyById

  .method public hidebysig instance string 
          GetPropertyByName(string propertyName) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceProperties::GetPropertyByName

  .method public hidebysig instance string 
          GetNewPropertyById(class libadb.DevicePropertyId propertyId) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceProperties::GetNewPropertyById

  .method public hidebysig instance string 
          GetNewPropertyByName(string propertyName) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceProperties::GetNewPropertyByName

  .method public hidebysig instance bool 
          TryGetPropertyById(class libadb.DevicePropertyId propertyId,
                             [out] string& propertyValue) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceProperties::TryGetPropertyById

  .method public hidebysig instance bool 
          TryGetPropertyByName(string propertyName,
                               [out] string& propertyValue) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceProperties::TryGetPropertyByName

  .method public hidebysig instance bool 
          TryGetUpdatedExistingPropertyById(class libadb.DevicePropertyId propertyId,
                                            [out] string& propertyValue) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceProperties::TryGetUpdatedExistingPropertyById

  .method public hidebysig instance bool 
          TryGetUpdatedExistingPropertyByName(string propertyName,
                                              [out] string& propertyValue) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceProperties::TryGetUpdatedExistingPropertyByName

  .method public hidebysig instance bool 
          HasProperty(class libadb.DevicePropertyId propertyId) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceProperties::HasProperty

  .method public hidebysig instance bool 
          HasProperty(string propertyName) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceProperties::HasProperty

  .method public hidebysig newslot virtual final 
          instance class [mscorlib]System.Collections.Generic.IEnumerator`1<valuetype [mscorlib]System.Collections.Generic.KeyValuePair`2<string,string>> 
          GetEnumerator() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceProperties::GetEnumerator

  .method private hidebysig newslot virtual final 
          instance class [mscorlib]System.Collections.IEnumerator 
          System.Collections.IEnumerable.GetEnumerator() cil managed
  {
    .override [mscorlib]System.Collections.IEnumerable::GetEnumerator
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceProperties::System.Collections.IEnumerable.GetEnumerator

} // end of class libadb.DeviceProperties

.class public auto ansi libadb.IO.FileSystem
       extends [mscorlib]System.Object
{
  .field public static literal string DirectorySeparator = "/"
  .method public hidebysig instance bool 
          Exists(string remotePath) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FileSystem::Exists

  .method public hidebysig instance class libadb.IO.FileInfo 
          GetFile(string remotePath) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FileSystem::GetFile

  .method public hidebysig instance class libadb.IO.DirectoryInfo 
          GetDirectory(string remotePath) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FileSystem::GetDirectory

  .method public hidebysig instance class libadb.IO.FileInfo[] 
          GetFiles() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FileSystem::GetFiles

  .method public hidebysig instance class libadb.IO.FileInfo[] 
          GetFiles(string path) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FileSystem::GetFiles

  .method public hidebysig instance class libadb.IO.DirectoryInfo[] 
          GetDirectories() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FileSystem::GetDirectories

  .method public hidebysig instance class libadb.IO.DirectoryInfo[] 
          GetDirectories(string path) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FileSystem::GetDirectories

  .method public hidebysig instance class [mscorlib]System.IO.FileInfo 
          Download(string remotePath,
                   string localPath,
                   bool overwriteExistingFiles) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FileSystem::Download

  .method public hidebysig instance class libadb.IO.FileInfo 
          Upload(string localPath,
                 string remotePath,
                 bool overwriteExistingFiles) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FileSystem::Upload

} // end of class libadb.IO.FileSystem

.class public auto ansi libadb.ProcessCollection
       extends [mscorlib]System.Object
       implements class [mscorlib]System.Collections.Generic.IEnumerable`1<class libadb.ProcessInfo>,
                  [mscorlib]System.Collections.IEnumerable
{
  .method public hidebysig instance class libadb.ProcessInfo 
          GetProcessById(int32 pid) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ProcessCollection::GetProcessById

  .method public hidebysig instance bool 
          TryGetProcessById(int32 pid,
                            [out] class libadb.ProcessInfo& processInfo) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ProcessCollection::TryGetProcessById

  .method public hidebysig instance class libadb.ProcessInfo[] 
          GetProcessesByName(string name) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ProcessCollection::GetProcessesByName

  .method public hidebysig instance class libadb.ProcessInfo 
          Start(class libadb.StartIntent startIntent) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ProcessCollection::Start

  .method public hidebysig newslot virtual final 
          instance class [mscorlib]System.Collections.Generic.IEnumerator`1<class libadb.ProcessInfo> 
          GetEnumerator() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ProcessCollection::GetEnumerator

  .method private hidebysig newslot virtual final 
          instance class [mscorlib]System.Collections.IEnumerator 
          System.Collections.IEnumerable.GetEnumerator() cil managed
  {
    .override [mscorlib]System.Collections.IEnumerable::GetEnumerator
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method ProcessCollection::System.Collections.IEnumerable.GetEnumerator

} // end of class libadb.ProcessCollection

.class public auto ansi libadb.Logcat
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance void 
          add_NewDataAvailable(class libadb.LogcatNewDataAvailableDelegate 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Logcat::add_NewDataAvailable

  .method public hidebysig specialname instance void 
          remove_NewDataAvailable(class libadb.LogcatNewDataAvailableDelegate 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Logcat::remove_NewDataAvailable

  .method public hidebysig specialname instance void 
          add_NewLineAvailable(class libadb.LogcatNewLineAvailableDelegate 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Logcat::add_NewLineAvailable

  .method public hidebysig specialname instance void 
          remove_NewLineAvailable(class libadb.LogcatNewLineAvailableDelegate 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Logcat::remove_NewLineAvailable

  .method public hidebysig specialname instance class libadb.LogcatStream 
          get_Stream() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Logcat::get_Stream

  .method public hidebysig instance void 
          StartMonitoring() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Logcat::StartMonitoring

  .method public hidebysig instance void 
          StopMonitoring() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Logcat::StopMonitoring

  .method public hidebysig instance void 
          Clear() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Logcat::Clear

  .method public hidebysig instance void 
          Close() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Logcat::Close

  .event libadb.LogcatNewDataAvailableDelegate NewDataAvailable
  {
    .addon instance void libadb.Logcat::add_NewDataAvailable(class libadb.LogcatNewDataAvailableDelegate)
    .removeon instance void libadb.Logcat::remove_NewDataAvailable(class libadb.LogcatNewDataAvailableDelegate)
  } // end of event Logcat::NewDataAvailable
  .event libadb.LogcatNewLineAvailableDelegate NewLineAvailable
  {
    .removeon instance void libadb.Logcat::remove_NewLineAvailable(class libadb.LogcatNewLineAvailableDelegate)
    .addon instance void libadb.Logcat::add_NewLineAvailable(class libadb.LogcatNewLineAvailableDelegate)
  } // end of event Logcat::NewLineAvailable
  .property instance class libadb.LogcatStream
          Stream()
  {
    .get instance class libadb.LogcatStream libadb.Logcat::get_Stream()
  } // end of property Logcat::Stream
} // end of class libadb.Logcat

.class public auto ansi sealed libadb.DeviceAddedRemovedDelegate
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method DeviceAddedRemovedDelegate::.ctor

  .method public hidebysig newslot virtual 
          instance void  Invoke(class libadb.Device device) runtime managed
  {
  } // end of method DeviceAddedRemovedDelegate::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class libadb.Device device,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method DeviceAddedRemovedDelegate::BeginInvoke

  .method public hidebysig newslot virtual 
          instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method DeviceAddedRemovedDelegate::EndInvoke

} // end of class libadb.DeviceAddedRemovedDelegate

.class public auto ansi libadb.DeviceDiscovery
       extends [mscorlib]System.Object
       implements [mscorlib]System.IDisposable
{
  .method public hidebysig specialname instance void 
          add_DeviceAdded(class libadb.DeviceAddedRemovedDelegate 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::add_DeviceAdded

  .method public hidebysig specialname instance void 
          remove_DeviceAdded(class libadb.DeviceAddedRemovedDelegate 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::remove_DeviceAdded

  .method public hidebysig specialname instance void 
          add_DeviceRemoved(class libadb.DeviceAddedRemovedDelegate 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::add_DeviceRemoved

  .method public hidebysig specialname instance void 
          remove_DeviceRemoved(class libadb.DeviceAddedRemovedDelegate 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::remove_DeviceRemoved

  .method public hidebysig specialname instance void 
          add_DevicesAdded(class libadb.DevicesAddedRemovedDelegate 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::add_DevicesAdded

  .method public hidebysig specialname instance void 
          remove_DevicesAdded(class libadb.DevicesAddedRemovedDelegate 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::remove_DevicesAdded

  .method public hidebysig specialname instance void 
          add_DevicesRemoved(class libadb.DevicesAddedRemovedDelegate 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::add_DevicesRemoved

  .method public hidebysig specialname instance void 
          remove_DevicesRemoved(class libadb.DevicesAddedRemovedDelegate 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::remove_DevicesRemoved

  .method public hidebysig specialname instance class [System]System.Collections.ObjectModel.ObservableCollection`1<class libadb.Device> 
          get_Devices() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::get_Devices

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(class [WindowsBase]System.Windows.Threading.Dispatcher dispatcher) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(class [System]System.ComponentModel.ISynchronizeInvoke invoker) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string sdkLocation) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string sdkLocation,
                               class [WindowsBase]System.Windows.Threading.Dispatcher dispatcher) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string sdkLocation,
                               class [System]System.ComponentModel.ISynchronizeInvoke invoker) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::.ctor

  .method public hidebysig instance class [mscorlib]System.Collections.Generic.IEnumerable`1<class libadb.Device> 
          GetDevices() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::GetDevices

  .method public hidebysig instance class libadb.Device 
          GetDeviceById(string deviceId) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::GetDeviceById

  .method public hidebysig instance bool 
          TryGetDeviceById(string deviceId,
                           [out] class libadb.Device& device) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::TryGetDeviceById

  .method public hidebysig instance class [mscorlib]System.Threading.Tasks.Task`1<bool> 
          WaitForDeviceAsync(class libadb.Device device,
                             valuetype [mscorlib]System.Threading.CancellationToken cancellationToken) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::WaitForDeviceAsync

  .method public hidebysig instance class [mscorlib]System.Threading.Tasks.Task`1<bool> 
          WaitForDeviceByIdAsync(string deviceId,
                                 valuetype [mscorlib]System.Threading.CancellationToken cancellationToken) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::WaitForDeviceByIdAsync

  .method public hidebysig newslot virtual final 
          instance void  Dispose() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DeviceDiscovery::Dispose

  .event libadb.DeviceAddedRemovedDelegate DeviceAdded
  {
    .addon instance void libadb.DeviceDiscovery::add_DeviceAdded(class libadb.DeviceAddedRemovedDelegate)
    .removeon instance void libadb.DeviceDiscovery::remove_DeviceAdded(class libadb.DeviceAddedRemovedDelegate)
  } // end of event DeviceDiscovery::DeviceAdded
  .event libadb.DeviceAddedRemovedDelegate DeviceRemoved
  {
    .removeon instance void libadb.DeviceDiscovery::remove_DeviceRemoved(class libadb.DeviceAddedRemovedDelegate)
    .addon instance void libadb.DeviceDiscovery::add_DeviceRemoved(class libadb.DeviceAddedRemovedDelegate)
  } // end of event DeviceDiscovery::DeviceRemoved
  .event libadb.DevicesAddedRemovedDelegate DevicesAdded
  {
    .addon instance void libadb.DeviceDiscovery::add_DevicesAdded(class libadb.DevicesAddedRemovedDelegate)
    .removeon instance void libadb.DeviceDiscovery::remove_DevicesAdded(class libadb.DevicesAddedRemovedDelegate)
  } // end of event DeviceDiscovery::DevicesAdded
  .event libadb.DevicesAddedRemovedDelegate DevicesRemoved
  {
    .removeon instance void libadb.DeviceDiscovery::remove_DevicesRemoved(class libadb.DevicesAddedRemovedDelegate)
    .addon instance void libadb.DeviceDiscovery::add_DevicesRemoved(class libadb.DevicesAddedRemovedDelegate)
  } // end of event DeviceDiscovery::DevicesRemoved
  .property instance class [System]System.Collections.ObjectModel.ObservableCollection`1<class libadb.Device>
          Devices()
  {
    .get instance class [System]System.Collections.ObjectModel.ObservableCollection`1<class libadb.Device> libadb.DeviceDiscovery::get_Devices()
  } // end of property DeviceDiscovery::Devices
} // end of class libadb.DeviceDiscovery

.class public auto ansi sealed libadb.DevicesAddedRemovedDelegate
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method DevicesAddedRemovedDelegate::.ctor

  .method public hidebysig newslot virtual 
          instance void  Invoke(class [mscorlib]System.Collections.Generic.IReadOnlyCollection`1<class libadb.Device> devices) runtime managed
  {
  } // end of method DevicesAddedRemovedDelegate::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class [mscorlib]System.Collections.Generic.IReadOnlyCollection`1<class libadb.Device> devices,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method DevicesAddedRemovedDelegate::BeginInvoke

  .method public hidebysig newslot virtual 
          instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method DevicesAddedRemovedDelegate::EndInvoke

} // end of class libadb.DevicesAddedRemovedDelegate

.class public auto ansi libadb.DevicePropertyId
       extends [mscorlib]System.Object
       implements class [mscorlib]System.IEquatable`1<class libadb.DevicePropertyId>
{
  .field public static initonly class libadb.DevicePropertyId ApiLevel
  .field public static initonly class libadb.DevicePropertyId CpuAbi
  .field public static initonly class libadb.DevicePropertyId CpuAbi2
  .field public static initonly class libadb.DevicePropertyId Manufacturer
  .field public static initonly class libadb.DevicePropertyId Model
  .field public static initonly class libadb.DevicePropertyId Name
  .field public static initonly class libadb.DevicePropertyId Product
  .field public static initonly class libadb.DevicePropertyId SdkVersion
  .field public static initonly class libadb.DevicePropertyId SerialNo
  .method public hidebysig specialname static 
          bool  op_Equality(class libadb.DevicePropertyId prop1,
                            class libadb.DevicePropertyId prop2) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DevicePropertyId::op_Equality

  .method public hidebysig specialname static 
          bool  op_Inequality(class libadb.DevicePropertyId prop1,
                              class libadb.DevicePropertyId prop2) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DevicePropertyId::op_Inequality

  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DevicePropertyId::ToString

  .method public hidebysig virtual instance bool 
          Equals(object obj) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DevicePropertyId::Equals

  .method public hidebysig newslot virtual final 
          instance bool  Equals(class libadb.DevicePropertyId other) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DevicePropertyId::Equals

  .method public hidebysig virtual instance int32 
          GetHashCode() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DevicePropertyId::GetHashCode

} // end of class libadb.DevicePropertyId

.class public auto ansi libadb.Intent
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance string 
          get_Action() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Intent::get_Action

  .method public hidebysig specialname instance void 
          set_Action(string 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Intent::set_Action

  .method public hidebysig specialname instance string 
          get_Data() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Intent::get_Data

  .method public hidebysig specialname instance void 
          set_Data(string 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Intent::set_Data

  .method public hidebysig specialname instance string 
          get_Category() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Intent::get_Category

  .method public hidebysig specialname instance void 
          set_Category(string 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Intent::set_Category

  .method public hidebysig specialname instance string 
          get_Type() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Intent::get_Type

  .method public hidebysig specialname instance void 
          set_Type(string 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Intent::set_Type

  .method public hidebysig specialname instance string 
          get_Component() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Intent::get_Component

  .method public hidebysig specialname instance void 
          set_Component(string 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Intent::set_Component

  .method public hidebysig specialname instance string 
          get_Extras() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Intent::get_Extras

  .method public hidebysig specialname instance void 
          set_Extras(string 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Intent::set_Extras

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Intent::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string action,
                               string category) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Intent::.ctor

  .property instance string Action()
  {
    .get instance string libadb.Intent::get_Action()
    .set instance void libadb.Intent::set_Action(string)
  } // end of property Intent::Action
  .property instance string Category()
  {
    .get instance string libadb.Intent::get_Category()
    .set instance void libadb.Intent::set_Category(string)
  } // end of property Intent::Category
  .property instance string Component()
  {
    .get instance string libadb.Intent::get_Component()
    .set instance void libadb.Intent::set_Component(string)
  } // end of property Intent::Component
  .property instance string Data()
  {
    .get instance string libadb.Intent::get_Data()
    .set instance void libadb.Intent::set_Data(string)
  } // end of property Intent::Data
  .property instance string Extras()
  {
    .set instance void libadb.Intent::set_Extras(string)
    .get instance string libadb.Intent::get_Extras()
  } // end of property Intent::Extras
  .property instance string Type()
  {
    .get instance string libadb.Intent::get_Type()
    .set instance void libadb.Intent::set_Type(string)
  } // end of property Intent::Type
} // end of class libadb.Intent

.class public auto ansi sealed libadb.LogcatNewDataAvailableDelegate
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method LogcatNewDataAvailableDelegate::.ctor

  .method public hidebysig newslot virtual 
          instance void  Invoke(uint8[] data,
                                int32 count) runtime managed
  {
  } // end of method LogcatNewDataAvailableDelegate::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(uint8[] data,
                      int32 count,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method LogcatNewDataAvailableDelegate::BeginInvoke

  .method public hidebysig newslot virtual 
          instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method LogcatNewDataAvailableDelegate::EndInvoke

} // end of class libadb.LogcatNewDataAvailableDelegate

.class public auto ansi sealed libadb.LogcatNewLineAvailableDelegate
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method LogcatNewLineAvailableDelegate::.ctor

  .method public hidebysig newslot virtual 
          instance void  Invoke(string line) runtime managed
  {
  } // end of method LogcatNewLineAvailableDelegate::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(string line,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method LogcatNewLineAvailableDelegate::BeginInvoke

  .method public hidebysig newslot virtual 
          instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method LogcatNewLineAvailableDelegate::EndInvoke

} // end of class libadb.LogcatNewLineAvailableDelegate

.class public auto ansi libadb.LogcatStream
       extends [mscorlib]System.IO.TextReader
       implements [mscorlib]System.IDisposable
{
  .method public hidebysig specialname instance int64 
          get_Length() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatStream::get_Length

  .method public hidebysig virtual instance int32 
          Read(char[] chars,
               int32 index,
               int32 count) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatStream::Read

  .method public hidebysig virtual instance int32 
          Peek() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatStream::Peek

  .method public hidebysig virtual instance int32 
          Read() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatStream::Read

  .method public hidebysig virtual instance string 
          ReadToEnd() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatStream::ReadToEnd

  .method public hidebysig virtual instance string 
          ReadLine() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatStream::ReadLine

  .method public hidebysig virtual instance void 
          Close() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatStream::Close

  .method family hidebysig virtual instance void 
          Dispose(bool disposing) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatStream::Dispose

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatStream::.ctor

  .property instance int64 Length()
  {
    .get instance int64 libadb.LogcatStream::get_Length()
  } // end of property LogcatStream::Length
} // end of class libadb.LogcatStream

.class public auto ansi sealed libadb.LogcatCategory
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype libadb.LogcatCategory Debug = int32(0x00000003)
  .field public static literal valuetype libadb.LogcatCategory Error = int32(0x00000001)
  .field public static literal valuetype libadb.LogcatCategory Information = int32(0x00000002)
  .field public static literal valuetype libadb.LogcatCategory Verbose = int32(0x00000004)
  .field public static literal valuetype libadb.LogcatCategory Warning = int32(0x00000005)
} // end of class libadb.LogcatCategory

.class public auto ansi libadb.LogcatEntry
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance int32 
          get_ProcessId() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatEntry::get_ProcessId

  .method public hidebysig specialname instance valuetype libadb.LogcatCategory 
          get_CategoryId() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatEntry::get_CategoryId

  .method public hidebysig specialname instance string 
          get_CategoryShortId() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatEntry::get_CategoryShortId

  .method public hidebysig specialname instance string 
          get_Name() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatEntry::get_Name

  .method public hidebysig specialname instance string 
          get_Description() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatEntry::get_Description

  .property instance valuetype libadb.LogcatCategory
          CategoryId()
  {
    .get instance valuetype libadb.LogcatCategory libadb.LogcatEntry::get_CategoryId()
  } // end of property LogcatEntry::CategoryId
  .property instance string CategoryShortId()
  {
    .get instance string libadb.LogcatEntry::get_CategoryShortId()
  } // end of property LogcatEntry::CategoryShortId
  .property instance string Description()
  {
    .get instance string libadb.LogcatEntry::get_Description()
  } // end of property LogcatEntry::Description
  .property instance string Name()
  {
    .get instance string libadb.LogcatEntry::get_Name()
  } // end of property LogcatEntry::Name
  .property instance int32 ProcessId()
  {
    .get instance int32 libadb.LogcatEntry::get_ProcessId()
  } // end of property LogcatEntry::ProcessId
} // end of class libadb.LogcatEntry

.class public auto ansi libadb.LogcatParser
       extends [mscorlib]System.Object
       implements [mscorlib]System.IDisposable
{
  .method public hidebysig specialname instance class [System]System.Collections.ObjectModel.ObservableCollection`1<class libadb.LogcatEntry> 
          get_Entries() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatParser::get_Entries

  .method public hidebysig specialname instance class libadb.Device 
          get_Device() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatParser::get_Device

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatParser::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(class [WindowsBase]System.Windows.Threading.Dispatcher dispatcher) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatParser::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(class [System]System.ComponentModel.ISynchronizeInvoke invoker) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatParser::.ctor

  .method public hidebysig instance void 
          SwitchToDevice(class libadb.Device device) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatParser::SwitchToDevice

  .method public hidebysig instance void 
          Invalidate() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatParser::Invalidate

  .method public hidebysig instance void 
          Clear() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatParser::Clear

  .method public hidebysig newslot virtual final 
          instance void  Dispose() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method LogcatParser::Dispose

  .property instance class libadb.Device Device()
  {
    .get instance class libadb.Device libadb.LogcatParser::get_Device()
  } // end of property LogcatParser::Device
  .property instance class [System]System.Collections.ObjectModel.ObservableCollection`1<class libadb.LogcatEntry>
          Entries()
  {
    .get instance class [System]System.Collections.ObjectModel.ObservableCollection`1<class libadb.LogcatEntry> libadb.LogcatParser::get_Entries()
  } // end of property LogcatParser::Entries
} // end of class libadb.LogcatParser

.class public auto ansi libadb.StartIntent
       extends libadb.Intent
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(class libadb.App application,
                               class libadb.Activity activity) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method StartIntent::.ctor

  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method StartIntent::ToString

} // end of class libadb.StartIntent

.class public auto ansi sealed libadb.ProcessState
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype libadb.ProcessState Dead = int32(0x00000006)
  .field public static literal valuetype libadb.ProcessState InterruptibleSleep = int32(0x00000003)
  .field public static literal valuetype libadb.ProcessState Paging = int32(0x00000005)
  .field public static literal valuetype libadb.ProcessState Running = int32(0x00000002)
  .field public static literal valuetype libadb.ProcessState Stopped = int32(0x00000004)
  .field public static literal valuetype libadb.ProcessState UninterruptibleSleep = int32(0x00000001)
  .field public static literal valuetype libadb.ProcessState Zombie = int32(0x00000007)
} // end of class libadb.ProcessState

.class public auto ansi libadb.IO.DirectoryInfo
       extends libadb.IO.FileSystemInfo
{
  .method public hidebysig instance class libadb.IO.FileInfo[] 
          GetFiles() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DirectoryInfo::GetFiles

  .method public hidebysig instance class libadb.IO.DirectoryInfo[] 
          GetDirectories() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DirectoryInfo::GetDirectories

  .method public hidebysig instance class libadb.IO.FileInfo 
          UploadFile(string localPath,
                     bool overwriteExistingFiles) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method DirectoryInfo::UploadFile

} // end of class libadb.IO.DirectoryInfo

.class public auto ansi libadb.IO.FileSystemInfo
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance valuetype libadb.IO.FileAttributes 
          get_Attributes() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FileSystemInfo::get_Attributes

  .method public hidebysig specialname instance valuetype libadb.IO.FilePermissions 
          get_Permissions() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FileSystemInfo::get_Permissions

  .method public hidebysig specialname instance int32 
          get_Size() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FileSystemInfo::get_Size

  .method public hidebysig specialname instance valuetype [mscorlib]System.DateTime 
          get_CreationTime() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FileSystemInfo::get_CreationTime

  .method public hidebysig specialname instance string 
          get_FullPath() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FileSystemInfo::get_FullPath

  .method public hidebysig specialname instance string 
          get_Name() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FileSystemInfo::get_Name

  .method public hidebysig specialname instance bool 
          get_IsFile() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FileSystemInfo::get_IsFile

  .method public hidebysig specialname instance bool 
          get_IsDirectory() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FileSystemInfo::get_IsDirectory

  .property instance valuetype libadb.IO.FileAttributes
          Attributes()
  {
    .get instance valuetype libadb.IO.FileAttributes libadb.IO.FileSystemInfo::get_Attributes()
  } // end of property FileSystemInfo::Attributes
  .property instance valuetype [mscorlib]System.DateTime
          CreationTime()
  {
    .get instance valuetype [mscorlib]System.DateTime libadb.IO.FileSystemInfo::get_CreationTime()
  } // end of property FileSystemInfo::CreationTime
  .property instance string FullPath()
  {
    .get instance string libadb.IO.FileSystemInfo::get_FullPath()
  } // end of property FileSystemInfo::FullPath
  .property instance bool IsDirectory()
  {
    .get instance bool libadb.IO.FileSystemInfo::get_IsDirectory()
  } // end of property FileSystemInfo::IsDirectory
  .property instance bool IsFile()
  {
    .get instance bool libadb.IO.FileSystemInfo::get_IsFile()
  } // end of property FileSystemInfo::IsFile
  .property instance string Name()
  {
    .get instance string libadb.IO.FileSystemInfo::get_Name()
  } // end of property FileSystemInfo::Name
  .property instance valuetype libadb.IO.FilePermissions
          Permissions()
  {
    .get instance valuetype libadb.IO.FilePermissions libadb.IO.FileSystemInfo::get_Permissions()
  } // end of property FileSystemInfo::Permissions
  .property instance int32 Size()
  {
    .get instance int32 libadb.IO.FileSystemInfo::get_Size()
  } // end of property FileSystemInfo::Size
} // end of class libadb.IO.FileSystemInfo

.class public auto ansi libadb.IO.FileInfo
       extends libadb.IO.FileSystemInfo
{
  .method public hidebysig instance class [mscorlib]System.IO.FileInfo 
          DownloadTo(string localPath) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FileInfo::DownloadTo

  .method public hidebysig instance class [mscorlib]System.IO.FileInfo 
          DownloadTo(string localPath,
                     bool overwriteExistingFiles) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method FileInfo::DownloadTo

} // end of class libadb.IO.FileInfo

.class public auto ansi sealed libadb.IO.FileAttributes
       extends [mscorlib]System.Enum
{
  .custom instance void [mscorlib]System.FlagsAttribute::.ctor() = ( 01 00 00 00 ) 
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype libadb.IO.FileAttributes Directory = int32(0x00000001)
  .field public static literal valuetype libadb.IO.FileAttributes File = int32(0x00000002)
  .field public static literal valuetype libadb.IO.FileAttributes None = int32(0x00000000)
  .field public static literal valuetype libadb.IO.FileAttributes SymbolicLink = int32(0x00000004)
} // end of class libadb.IO.FileAttributes

.class public auto ansi sealed libadb.IO.FilePermissions
       extends [mscorlib]System.Enum
{
  .custom instance void [mscorlib]System.FlagsAttribute::.ctor() = ( 01 00 00 00 ) 
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype libadb.IO.FilePermissions GroupAll = int32(0x00000038)
  .field public static literal valuetype libadb.IO.FilePermissions GroupExecute = int32(0x00000020)
  .field public static literal valuetype libadb.IO.FilePermissions GroupRead = int32(0x00000008)
  .field public static literal valuetype libadb.IO.FilePermissions GroupWrite = int32(0x00000010)
  .field public static literal valuetype libadb.IO.FilePermissions None = int32(0x00000000)
  .field public static literal valuetype libadb.IO.FilePermissions OthersAll = int32(0x000001C0)
  .field public static literal valuetype libadb.IO.FilePermissions OthersExecute = int32(0x00000100)
  .field public static literal valuetype libadb.IO.FilePermissions OthersRead = int32(0x00000040)
  .field public static literal valuetype libadb.IO.FilePermissions OthersWrite = int32(0x00000080)
  .field public static literal valuetype libadb.IO.FilePermissions OwnerAll = int32(0x00000007)
  .field public static literal valuetype libadb.IO.FilePermissions OwnerExecute = int32(0x00000004)
  .field public static literal valuetype libadb.IO.FilePermissions OwnerRead = int32(0x00000001)
  .field public static literal valuetype libadb.IO.FilePermissions OwnerWrite = int32(0x00000002)
} // end of class libadb.IO.FilePermissions


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
