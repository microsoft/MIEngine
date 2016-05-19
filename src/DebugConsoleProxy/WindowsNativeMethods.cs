using System;
using System.Runtime.InteropServices;

namespace DebugConsoleProxy
{
    internal class WindowsNativeMethods
    {
        internal enum ConsoleCtrlValues
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GenerateConsoleCtrlEvent(ConsoleCtrlValues dwCtrlEvent, uint dwProcessGroupId);
    }
}
