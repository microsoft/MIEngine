using System;

namespace Microsoft.DebugEngineHost
{
    internal sealed class HostConfirigurationException : Exception
    {
        const int E_DEBUG_ENGINE_NOT_REGISTERED = unchecked((int)0x80040019);

        public HostConfirigurationException(string missingLocation) : base(string.Format("Missing configuration section '{0}'", missingLocation))
        {
            this.HResult = E_DEBUG_ENGINE_NOT_REGISTERED;
        }
    }
}