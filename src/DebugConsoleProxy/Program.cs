using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebugConsoleProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            // Need to send the entire local launch options and reparse them
            // TODO: arg validation
            string miDebuggerPath = args[0];
            string breakEventName = args[1];
            string breakEventResponseName = args[2];
            string cwd = args[3];

            ConsoleProxy consoleProxy = new ConsoleProxy();
            consoleProxy.LaunchAndExecuteDebugger(miDebuggerPath, breakEventName, breakEventResponseName, cwd);
        }
    }
}
