// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IOSDebugLauncher
{
    internal class VcRemoteClient : HttpClient
    {
        private IOSLaunchOptions _launchOptions;

        private VcRemoteClient()
            : base()
        {
        }

        private VcRemoteClient(HttpMessageHandler handler)
            : base(handler, true)
        {
        }

        public static VcRemoteClient GetInstance(IOSLaunchOptions options)
        {
            VcRemoteClient client = null;
            string baseAddressFormat = string.Empty;
            if (options.Secure)
            {
                var handler = new RequestAuthHandler();
                client = new VcRemoteClient(handler);
                client.ServerCertificateValidationCallback = handler.ServerCertificateValidationCallback;
                baseAddressFormat = @"https://{0}:{1}/";
            }
            else
            {
                client = new VcRemoteClient();
                baseAddressFormat = @"http://{0}:{1}/";
            }
            client.BaseAddress = new Uri(string.Format(CultureInfo.InvariantCulture, baseAddressFormat, options.RemoteMachineName, options.VcRemotePort));
            client.Timeout = new TimeSpan(0, 0, 10); //10 second timeout
            client.Secure = options.Secure;
            client._launchOptions = options;

            return client;
        }

        public Launcher.RemotePorts StartDebugListener()
        {
            string remotePortsJsonString;
            CallVcRemote(new Uri("debug/setupForDebugging?target=" + _launchOptions.IOSDebugTarget.ToString() + "&deviceUdid=" + _launchOptions.DeviceUdid, UriKind.Relative), LauncherResources.Info_StartingDebugListener, out remotePortsJsonString);

            try
            {
                return JsonConvert.DeserializeObject<Launcher.RemotePorts>(remotePortsJsonString);
            }
            catch (JsonException)
            {
                Telemetry.SendLaunchError(Telemetry.LaunchFailureCode.BadJson.ToString(), _launchOptions.IOSDebugTarget);
                throw new LauncherException(LauncherResources.Error_BadJSon);
            }
        }

        public string GetRemoteAppPath()
        {
            string appPath = string.Empty;
            var response = CallVcRemote(new Uri("debug/appRemotePath?package" + _launchOptions.PackageId + "&deviceUdid=" + _launchOptions.DeviceUdid, UriKind.Relative), LauncherResources.Info_GettingInfo, out appPath);

            if (string.IsNullOrWhiteSpace(appPath))
            {
                Telemetry.SendLaunchError(Telemetry.LaunchFailureCode.BadPackageId.ToString(), _launchOptions.IOSDebugTarget);
                Debug.Fail("Invalid return from vcremote for packageId");
                throw new InvalidOperationException();
            }

            return appPath;
        }

        private HttpResponseMessage CallVcRemote(Uri endpoint, string waitLoopMessage, out string responseBody)
        {
            ManualResetEvent doneEvent = new ManualResetEvent(false);
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var waitLoop = new MICore.WaitLoop(waitLoopMessage);
            ExceptionDispatchInfo exceptionDispatchInfo = null;

            HttpResponseMessage response = null;
            string content = null;
            ThreadPool.QueueUserWorkItem(async (object o) =>
            {
                string failureCode = Telemetry.VcRemoteFailureCode.VcRemoteSucces.ToString();

                try
                {
                    response = await this.GetAsync(endpoint, cancellationTokenSource.Token);
                    response.EnsureSuccessStatusCode();

                    content = await response.Content.ReadAsStringAsync();
                }
                catch (HttpRequestException)
                {
                    if (response != null)
                    {
                        if (response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            exceptionDispatchInfo = ExceptionDispatchInfo.Capture(new LauncherException(LauncherResources.Error_Unauthorized));
                            failureCode = Telemetry.VcRemoteFailureCode.VcRemoteUnauthorized.ToString();
                        }
                        else
                        {
                            exceptionDispatchInfo = ExceptionDispatchInfo.Capture(new LauncherException(string.Format(LauncherResources.Error_VcRemoteUnknown, response.StatusCode.ToString())));
                            failureCode = Telemetry.VcRemoteFailureCode.VcRemoteUnkown.ToString();
                        }
                    }
                    else
                    {
                        exceptionDispatchInfo = ExceptionDispatchInfo.Capture(new LauncherException(LauncherResources.Error_UnableToReachServer));
                        failureCode = Telemetry.VcRemoteFailureCode.VcRemoteNoConnection.ToString();
                    }
                }
                catch (TaskCanceledException)
                {
                    //timeout 
                    exceptionDispatchInfo = ExceptionDispatchInfo.Capture(new LauncherException(LauncherResources.Error_UnableToReachServer));
                    failureCode = Telemetry.VcRemoteFailureCode.VcRemoteNoConnection.ToString();
                }
                catch (Exception e)
                {
                    exceptionDispatchInfo = ExceptionDispatchInfo.Capture(e);
                    failureCode = e.GetType().FullName;
                }

                doneEvent.Set();

                Telemetry.SendLaunchError(failureCode, _launchOptions.IOSDebugTarget);
            });

            waitLoop.Wait(doneEvent, cancellationTokenSource);

            if (exceptionDispatchInfo != null)
            {
                exceptionDispatchInfo.Throw();
            }

            if (response == null)
            {
                Debug.Fail("Null resposne? Should be impossible.");
                throw new InvalidOperationException();
            }

            responseBody = content;
            return response;
        }

        public RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; private set; }
        public bool Secure { get; private set; }
    }
}
