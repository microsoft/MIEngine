using System;

namespace Microsoft.MIDebugEngine
{
    internal class LaunchErrorException : Exception
    {
        public LaunchErrorException(string message) : base(message)
        {
        }
    }
}