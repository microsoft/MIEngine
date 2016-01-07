// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication12
{
    internal class CertTool
    {
        private const int RemotePort = 3030;
        private const string MachineName = "Chucks-mac-mini";

        public static bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyError
            )
        {
            return true;
        }

        private static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                Console.WriteLine("Enter PIN: ");
                string pin = Console.ReadLine();

                WebRequestHandler handler = new WebRequestHandler();
                handler.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(ValidateServerCertificate);

                using (HttpClient client = new HttpClient(handler, true))
                {
                    client.BaseAddress = new Uri(string.Format(@"https://{0}:{1}", MachineName, RemotePort));

                    X509Store store = null;

                    try
                    {
                        var response = await client.GetAsync("certs/" + pin);
                        response.EnsureSuccessStatusCode();

                        byte[] rawCert = await response.Content.ReadAsByteArrayAsync();

                        X509Certificate2Collection certs = new X509Certificate2Collection();
                        certs.Import(rawCert, "", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.UserKeySet);

                        store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                        store.Open(OpenFlags.ReadWrite);

                        X509Certificate2Collection oldCerts = new X509Certificate2Collection();

                        foreach (var cert in certs)
                        {
                            oldCerts.AddRange(store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, cert.Subject, false));
                        }
                        store.RemoveRange(certs);
                        store.AddRange(certs);
                        store.Close();

                        Console.WriteLine("Success");
                    }
                    catch (HttpRequestException e)
                    {
                        Console.WriteLine("Error communicating with vcremote. Make sure that vcremote is running in secure mode and that a new client cert has been generated.");
                    }
                    finally
                    {
                        if (store != null)
                        {
                            store.Close();
                        }
                    }
                }
            }).Wait();
        }
    }
}
