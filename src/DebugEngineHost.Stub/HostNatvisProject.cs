using System;

namespace Microsoft.DebugEngineHost
{
    public static class HostNatvisProject
    {
        public delegate void NatvisLoader(string path);

        public static void FindNatvisInSolution(NatvisLoader loader)
        {
            throw new NotImplementedException();
        }
    }
}
