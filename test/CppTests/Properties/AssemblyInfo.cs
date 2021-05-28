// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.InteropServices;
using CppTests;
using DebuggerTesting.Attribution;
using Xunit;

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("ae286353-4fc0-4fb3-bf6a-2677252b96b2")]

// Turn off parallel threads, xunit didn't seem to be handling this correctly
[assembly: CollectionBehavior(DisableTestParallelization = true)]

// This is located by the test settings resolver and specifies the settings provider for C++ tests
[assembly: TestSettingsProvider(typeof(CppTestSettingsProvider))]