using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ThreadingTasks = System.Threading.Tasks;

namespace Microsoft.SSHDebugPS.Utilities
{
    internal class VSMessageBoxHelper
    {
        /// <summary>
        /// Switches to main thread and shows a VS message box.
        /// </summary>
        public static void PostUIMessage(string title, string message, OLEMSGICON icon, OLEMSGBUTTON button, OLEMSGDEFBUTTON defaultButton)
        {
            ThreadingTasks.Task.Run(async () => await PostMessageInternal(title, message, icon, button, defaultButton));
        }

        public static void PostErrorMessage(string title, string message)
        {
            PostUIMessage(
                title,
                message,
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private static async ThreadingTasks.Task PostMessageInternal(string title, string message, OLEMSGICON icon, OLEMSGBUTTON button, OLEMSGDEFBUTTON defaultButton)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
                message,
                title,
                icon,
                button,
                defaultButton);
        }
    }
}
