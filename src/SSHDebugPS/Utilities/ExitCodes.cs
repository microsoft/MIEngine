using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SSHDebugPS.Utilities
{
    internal class ExitCodes
    {
        // Official codes
        public static int SUCCESS = 0;
        public static int LINUX_COMMANDCANNOTEXECUTE = 126;
        public static int LINUX_COMMANDNOTFOUND = 127;
        public static int OPERATION_TIMEDOUT = 1490;

        // Custom codes
        internal static int OBJECTDISPOSED = 1999;
    }
}
