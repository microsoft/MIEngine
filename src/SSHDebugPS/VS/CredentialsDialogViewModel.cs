// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SSHDebugPS.VS
{
    internal class CredentialsDialogViewModel
    {
        private readonly string _hostName;
        private string _headerText;

        public CredentialsDialogViewModel(string hostName)
        {
            _hostName = hostName;
        }

        public string HostName => _hostName;

        public string HeaderText
        {
            get
            {
                if (_headerText == null)
                {
                    _headerText = string.Format(CultureInfo.CurrentCulture, StringResources.HeaderTextFormat, _hostName);
                }
                return _headerText;
            }
        }

        public string UserName { get; set; } = string.Empty;

        public string PrivateKeyFile { get; set; } = string.Empty;

        // NOTE: We don't use DataBinding to update this
        public SecureString Password { get; set; }
    }
}
