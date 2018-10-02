using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DebugEngineHost
{

    /// <summary>
    /// Provides ability to launch in a VS Code protocol supported terminal
    /// </summary>
    public static class HostRunInTerminal
    {
        public static bool IsRunInTerminalAvailable()
        {
            return false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "cwd")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static void RunInTerminal(string title, string workingDirectory, bool useExternalConsole, IReadOnlyList<string> commandArgs, IReadOnlyDictionary<string, string> environmentVariables, Action<int?> success, Action<string> failure)
        {
            throw new NotImplementedException();
        }
    }
}
