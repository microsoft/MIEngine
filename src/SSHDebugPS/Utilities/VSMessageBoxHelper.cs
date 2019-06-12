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
        public static void ShowMessage(string title, string message, OLEMSGICON icon, OLEMSGBUTTON button, OLEMSGDEFBUTTON defaultButton)
        {
            ThreadingTasks.Task.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
                    message,
                    title,
                    icon,
                    button,
                    defaultButton);
                });
        }

        public static void ShowErrorMessage(string title, string message)
        {
            ShowMessage(
                title,
                message,
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
