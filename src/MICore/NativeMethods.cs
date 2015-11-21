using System;
using System.Runtime.InteropServices;

namespace MICore
{
    internal class NativeMethods
    {
        [DllImport("System.Native", SetLastError = true)]
        internal static extern int Kill(int pid, int mode);

        [DllImport("System.Native", SetLastError = true)]
        internal static extern int MkFifo(string name, int mode);

        [DllImport("System.Native", SetLastError = true)]
        internal static extern uint GetEUid();
    }
}
