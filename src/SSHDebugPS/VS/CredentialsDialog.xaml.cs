// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Microsoft.SSHDebugPS.VS
{
    /// <summary>
    /// Interaction logic for CredentialsDialog.xaml
    /// </summary>
    public partial class CredentialsDialog : DialogWindow
    {
        private Connection _connection;

        public CredentialsDialog(string hostName) : base()
        {
            this.DataContext = new CredentialsDialogViewModel(hostName);
            InitializeComponent();

            this.UserName.Loaded += UserName_Loaded;
        }

        private void UserName_Loaded(object sender, RoutedEventArgs e)
        {
            // Set the user name field to have focus by default
            this.UserName.Focus();
            this.UserName.Loaded -= UserName_Loaded;
        }

        internal static Connection Show(string hostName, ConnectionReason reason)
        {
            ClearKeyboardMessages();

            CredentialsDialog @this = new CredentialsDialog(hostName);
            @this.ShowModal();

            if (@this._connection != null)
            {
                return @this._connection;
            }
            else
            {
                throw new AD7ConnectCanceledException(reason);
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            CredentialsDialogViewModel viewModel = (CredentialsDialogViewModel)this.DataContext;
            viewModel.Password = this.Password.SecurePassword;

            liblinux.UnixSystem remoteSystem = null;
            try
            {
                VSOperationWaiter.Wait(string.Format(CultureInfo.CurrentCulture, StringResources.WaitingOp_Connecting, viewModel.HostName), throwOnCancel: false, action: () =>
                  {
                      remoteSystem = new liblinux.UnixSystem();
                      if (string.IsNullOrEmpty(viewModel.PrivateKeyFile))
                      {
                          remoteSystem.Connect(viewModel.HostName, viewModel.UserName, viewModel.Password);
                      }
                      else
                      {
                          remoteSystem.Connect(viewModel.HostName, viewModel.UserName, viewModel.PrivateKeyFile, viewModel.Password);
                      }
                  });
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, ex.Message, null, OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            // NOTE: This will be null if connect is canceled
            if (remoteSystem != null)
            {
                _connection = new Connection(remoteSystem);
                this.Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private static void ClearKeyboardMessages()
        {
            // Our dialog has a default button, and the attach to process dialog doesn't seem to fully handle keyboard message
            // before bringing our code up. So we want to remove any keyboard messages from the queue to make sure that our
            // dialog doesn't seem them.
            NativeMethods.MSG msg;
            while (NativeMethods.PeekMessage(out msg, IntPtr.Zero, NativeMethods.WM_KEYFIRST, NativeMethods.WM_CHAR, wRemoveMsg: 1))
            { }
        }
    }
}
