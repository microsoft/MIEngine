using System;

namespace Microsoft.DebugEngineHost
{
    public static class HostOutputWindow
    {
        private static class VsImpl
        {
            internal static void SetText(string outputMessage)
            {
                throw new NotImplementedException();
            }
        }

        public static void WriteLaunchError(string outputMessage)
        {
            throw new NotImplementedException();
        }
    }
}
