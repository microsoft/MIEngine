// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IOSDebugLauncher
{
    internal class RequestAuthHandler : WebRequestHandler
    {
        /// <summary>
        /// Creates a new instance of the RequestAuthHandler class.
        /// </summary>
        public RequestAuthHandler()
        {
            this.ClientCertificateOptions = ClientCertificateOption.Automatic;
            this.UseDefaultCredentials = false;
            this.UseProxy = false;
            this.ServerCertificateValidationCallback = delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                if (sslPolicyErrors == SslPolicyErrors.None)
                    return true;
                if ((sslPolicyErrors & System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
                {
                    if (sender is HttpWebRequest)
                    {
                        var request2 = sender as HttpWebRequest;
                        var requestUri = request2.RequestUri;
                        if (requestUri.HostNameType == UriHostNameType.IPv4 || requestUri.HostNameType == UriHostNameType.IPv6 || requestUri.HostNameType == UriHostNameType.Dns)
                        {
                            X509Extension ext2 = (certificate as X509Certificate2).Extensions["Subject Alternative Name"];
                            if (ext2 != null)
                            {
                                string subjAltName = ext2.Format(false);
                                if (subjAltName.IndexOf(requestUri.Host, StringComparison.OrdinalIgnoreCase) == -1)
                                    return false;
                            }
                            else
                                return false;
                        }
                        else
                            return false;
                    }
                    else if (sender is SslStream)
                    {
                        //nothing to do, fall through
                    }
                    else
                        return false;
                }
                if ((sslPolicyErrors & System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors) != 0)
                {
                    if (chain != null && chain.ChainStatus != null)
                    {
                        foreach (System.Security.Cryptography.X509Certificates.X509ChainStatus status in chain.ChainStatus)
                        {
                            if (status.Status == System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.UntrustedRoot)
                            {
                                // Self-signed certificates with an untrusted root are valid.
                                continue;
                            }
                            else
                            {
                                if (status.Status != System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.NoError)
                                {
                                    // If there are any other errors in the certificate chain, the certificate is invalid,
                                    // so the method returns false.
                                    return false;
                                }
                            }
                        }
                        return true;
                    }
                }
                return false;
            };
        }
    }
}
