// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.SSHDebugPS.WSL
{
    [ComVisible(true)]
    [Guid("B8587A49-00BD-4DEE-94B9-6EBF49003E04")]
    internal class WSLPortSupplier : AD7PortSupplier
    {
        private readonly Guid _id = new Guid("267B1341-AC92-44DC-94DF-2EE4205DD17E");

        protected override Guid Id { get { return _id; } }
        protected override string Name { get { return StringResources.WSL_PSName; } }
        protected override string Description { get { return StringResources.WSL_PSDescription; } }
        IEnumerable<string> _distros;

        public WSLPortSupplier() : base()
        {
        }

        public override int CanAddPort()
        {
            // Indicate that ports cannot be added
            return HR.S_FALSE;
        }

        public override int CanPersistPorts()
        {
            // Opt-out of the SDM remembering ports
            return HR.S_OK;
        }

        public override int EnumPorts(out IEnumDebugPorts2 ppEnum)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_distros == null)
            {
                IVsUIShell shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;

                try
                {
                    WSLCommandLine.EnsureInitialized();
                    _distros = WSLCommandLine.GetInstalledDistros();
                }
                catch (Exception ex)
                {
                    shell.SetErrorInfo(ex.HResult, ex.Message, 0, null, null);
                    shell.ReportErrorInfo(ex.HResult);
                    ppEnum = null;
                    return VSConstants.E_ABORT;
                }
            }

            WSLPort[] ports = _distros.Select(name => new WSLPort(this, name, isInAddPort: false)).ToArray();
            ppEnum = new AD7PortEnum(ports);
            return HR.S_OK;
        }

        public override int EnumPersistedPorts(BSTR_ARRAY portNames, out IEnumDebugPorts2 portEnum)
        {
            // This should never be called since CanPersistPorts returns S_OK
            Debug.Fail("Why is EnumPersistedPorts called?");
            throw new NotImplementedException();
        }
        public override int AddPort(IDebugPortRequest2 request, out IDebugPort2 port)
        {
            // This should never be called
            Debug.Fail("Why is AddPort called?");
            throw new NotImplementedException();
        }
    }

}
