// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenDebug;

namespace DebugAdapterRunner
{
    /// <summary>Represents a command that is sent to the debug adapter</summary>
    public class DebugAdapterCommand : Command
    {
        public dynamic args;
        private const int REQUEST_BYTES = 4096;

        public DebugAdapterCommand(string cmd, dynamic args, IEnumerable<DebugAdapterResponse> expectedResponses = null)
        {
            this.Name = cmd;
            this.args = args;

            if (expectedResponses == null)
            {
                this.ExpectedResponses.Add(new DebugAdapterResponse(new { success = true, command = cmd }));
            }
            else
            {
                this.ExpectedResponses.AddRange(expectedResponses);
            }
        }

        private string CreateDispatcherRequest(DebugAdapterRunner runner)
        {
            DispatcherRequest request = new DispatcherRequest(runner.GetNextSequenceNumber(), this.Name, this.args);
            return runner.SerializeMessage(request);
        }

        private void VerifyValueOfBuffer(byte[] buffer, string expectedValue)
        {
            string actualValue = Encoding.UTF8.GetString(buffer);
            if (actualValue != expectedValue)
            {
                string errorMessage = string.Format(CultureInfo.CurrentCulture, "Unexpected response from debug adapter. Was expecting {0} but got {1}", expectedValue, actualValue);
                throw new DARException(errorMessage);
            }
        }

        private string GetMessage(Stream stdout, int timeout, Process debugAdapter, DebugAdapterRunner runner)
        {
            // Read header of message
            byte[] header = ReadBlockFromStream(stdout, debugAdapter, DAPConstants.ContentLength.Length, timeout);
            VerifyValueOfBuffer(header, DAPConstants.ContentLength);

            // Read the length (in bytes) of the json message
            int messageLengthBytes = 0;
            int nextByte = -1;
            while (true)
            {
                nextByte = stdout.ReadByte();
                if (nextByte >= '0' && nextByte <= '9')
                {
                    messageLengthBytes = messageLengthBytes * 10 + nextByte - '0';
                }
                else
                {
                    break;
                }
            }

            // Read and verify TWO_CRLF
            byte[] twoCRLF = new byte[DAPConstants.TwoCrLf.Length];
            twoCRLF[0] = (byte)nextByte;

            // Read one less byte because we have the first byte and copy into twoCRLF byte array
            byte[] oneMinustwoCRLF = ReadBlockFromStream(stdout, debugAdapter, DAPConstants.TwoCrLf.Length - 1, timeout);
            Array.Copy(oneMinustwoCRLF, 0, twoCRLF, 1, oneMinustwoCRLF.Length);

            VerifyValueOfBuffer(twoCRLF, DAPConstants.TwoCrLf);

            // Read the message contents
            byte[] messageBuffer = ReadBlockFromStream(stdout, debugAdapter, messageLengthBytes, timeout);

            return Encoding.UTF8.GetString(messageBuffer, 0, messageLengthBytes);
        }

        private byte[] ReadBlockFromStream(Stream stream, Process debugAdapter, int length, int timeout)
        {
            int bytesRead = 0;
            byte[] messageBuffer = new byte[length];

            while (bytesRead < length)
            {
                // Read as many bytes as we need or as many bytes as we can fit
                int requestedReadBytes = Math.Min(length - bytesRead, DebugAdapterCommand.REQUEST_BYTES);

                Task<int> readMessageTask = stream.ReadAsync(messageBuffer, bytesRead, requestedReadBytes);
                if (!readMessageTask.Wait(timeout))
                {
                    if (bytesRead == 0)
                    {
                        throw new TimeoutException("Timeout waiting for message from Debug Adapter");
                    }
                    else
                    {
                        throw new DARException(String.Format(CultureInfo.InvariantCulture, "Timeout reading expected bytes. Expected:{0} Actual:{1} Timeout:{2}", length, bytesRead, timeout));
                    }
                }

                if (debugAdapter.HasExited && readMessageTask.Result == 0)
                {
                    throw new DARException(String.Format(CultureInfo.InvariantCulture, "The debugger process has exited without sending all expected bytes. Expected: {0} Actual:{1}", length, bytesRead));
                }

                bytesRead += readMessageTask.Result;

                if (length > bytesRead)
                {
                    // Give the process some time to fill the stdout buffer again
                    Thread.Sleep(10);
                }
            }

            return messageBuffer;
        }

        private struct ResponsePair
        {
            /// <summary>
            /// Boolean to indicate if this response has a match.
            /// </summary>
            public bool FoundMatch { get; set; }

            /// <summary>
            /// The response
            /// </summary>
            public object Response { get; set; }
        }

        public override void Run(DebugAdapterRunner runner)
        {
            // Send the request
            string request = CreateDispatcherRequest(runner);
            // VSCode doesn't send /n at the end. If this is writeline, then concord hangs
            runner.DebugAdapter.StandardInput.Write(request);

            // Process + validate responses
            List<ResponsePair> responseList = new List<ResponsePair>();
            int currentExpectedResponseIndex = 0;
            int previousExpectedResponseIndex = 0;

            // Loop until we have received as many expected responses as expected
            while (currentExpectedResponseIndex < this.ExpectedResponses.Count)
            {
                // Check if previous messages contained the expected response
                if (previousExpectedResponseIndex != currentExpectedResponseIndex)
                {
                    DebugAdapterResponse expected = this.ExpectedResponses[currentExpectedResponseIndex];
                    // Only search responses in history list if we can ignore the response order.
                    if (expected.IgnoreResponseOrder)
                    {
                        for (int i = 0; i < responseList.Count; i++)
                        {
                            ResponsePair responsePair = responseList[i];
                            // Make sure we have not seen this response and check to see if it the response we are expecting.
                            if (!responsePair.FoundMatch && Utils.CompareObjects(expected.Response, responsePair.Response, expected.IgnoreOrder))
                            {
                                expected.Match = responsePair.Response;
                                responsePair.FoundMatch = true;
                                break;
                            }
                        }

                        // We found an expected response from a previous response.
                        // Continue to next expectedResponse.
                        if (expected.Match != null)
                        {
                            currentExpectedResponseIndex++;
                            continue;
                        }
                    }
                }

                previousExpectedResponseIndex = currentExpectedResponseIndex;

                string receivedMessage = null;
                Exception getMessageExeception = null;
                try
                {
                    receivedMessage = this.GetMessage(
                        runner.DebugAdapter.StandardOutput.BaseStream,
                        runner.ResponseTimeout,
                        runner.DebugAdapter,
                        runner);
                }
                catch (Exception e)
                {
                    getMessageExeception = e;
                }

                if (getMessageExeception != null)
                {
                    if (!runner.DebugAdapter.HasExited)
                    {
                        // If it hasn't exited yet, wait a little bit longer to make sure it isn't just about to exit
                        try
                        {
                            runner.DebugAdapter.WaitForExit(500);
                        }
                        catch
                        { }
                    }

                    string messageStart;
                    string messageSuffix = string.Empty;
                    if (runner.DebugAdapter.HasExited)
                    {
                        if (runner.HasAsserted())
                        {
                            messageStart = string.Format(CultureInfo.CurrentCulture, "The debugger process has asserted and exited with code '{0}' without sending all expected responses. See test log for assert details.", runner.DebugAdapter.ExitCode);
                        }
                        else
                        {
                            int exitCode = runner.DebugAdapter.ExitCode;
                            messageStart = string.Format(CultureInfo.CurrentCulture, "The debugger process has exited with code '{0}' without sending all expected responses.", exitCode);

                            // OSX will normally write out nice crash reports that we can include if the debug adapter crashes.
                            // Try to include them in the exception.
                            // NOTE that OSX uses exit code 128+<signal_number> if there is a crash. The highest documented signal
                            // number is 31 (User defined signal 2). See 'man signal' for more numbers.
                            // The most common is 139 (SigSegV).
                            if (exitCode > 128 && exitCode <= 128+31 && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                            {
                                if (TryFindOSXCrashReport(runner, out string crashReport))
                                {
                                    messageSuffix = "\n\nCrash report:\n" + crashReport;
                                }
                            }
                        }
                    }
                    else if (getMessageExeception is TimeoutException)
                    {
                        if (runner.HasAsserted())
                        {
                            messageStart = "The debugger process has asserted. See test log for assert details.";
                        }
                        else
                        {
                            messageStart = "Expected response not found before timeout.";
                        }
                    }
                    else
                    {
                        messageStart = "Exception while reading message from debug adpter. " + getMessageExeception.Message;
                    }

                    string expectedResponseText = string.Empty;
                    for (int i = 0; i < ExpectedResponses.Count; i++)
                    {
                        string status;
                        if (i < currentExpectedResponseIndex)
                            status = "Found";
                        else if (i == currentExpectedResponseIndex)
                            status = "Not Found";
                        else
                            status = "Not searched yet";
                        expectedResponseText += string.Format(CultureInfo.CurrentCulture, "{0}. {1}: {2}\n", (i + 1), status, JsonConvert.SerializeObject(ExpectedResponses[i].Response));
                    }

                    string actualResponseText = string.Empty;

                    for (int i = 0; i < responseList.Count; i++)
                    {
                        actualResponseText += string.Format(CultureInfo.CurrentCulture, "{0}. {1}\n", (i + 1), JsonConvert.SerializeObject(responseList[i]));
                    }

                    string errorMessage = string.Format(CultureInfo.CurrentCulture, "{0}\nExpected =\n{1}\nActual Responses =\n{2}{3}",
                        messageStart, expectedResponseText, actualResponseText, messageSuffix);

                    throw new DARException(errorMessage);
                }

                try
                {
                    DispatcherMessage dispatcherMessage = JsonConvert.DeserializeObject<DispatcherMessage>(receivedMessage);

                    if (dispatcherMessage.type == "event")
                    {
                        DispatcherEvent dispatcherEvent = JsonConvert.DeserializeObject<DispatcherEvent>(receivedMessage);

                        if (dispatcherEvent.eventType == "stopped")
                        {
                            runner.CurrentThreadId = dispatcherEvent.body.threadId;
                        }

                        var expected = this.ExpectedResponses[currentExpectedResponseIndex];
                        if (Utils.CompareObjects(expected.Response, dispatcherEvent, expected.IgnoreOrder))
                        {
                            expected.Match = dispatcherEvent;
                            currentExpectedResponseIndex++;
                        }

                        responseList.Add(new ResponsePair()
                        {
                            FoundMatch = expected.Match != null,
                            Response = dispatcherEvent
                        });
                    }
                    else if (dispatcherMessage.type == "response")
                    {
                        DispatcherResponse dispatcherResponse = JsonConvert.DeserializeObject<DispatcherResponse>(receivedMessage);

                        var expected = this.ExpectedResponses[currentExpectedResponseIndex];
                        if (Utils.CompareObjects(expected.Response, dispatcherResponse, expected.IgnoreOrder))
                        {
                            expected.Match = dispatcherResponse;
                            currentExpectedResponseIndex++;
                        }

                        responseList.Add(new ResponsePair()
                        {
                            FoundMatch = expected.Match != null,
                            Response = dispatcherResponse
                        });
                    }
                    else if (dispatcherMessage.type == "request")
                    {
                        runner.HandleCallbackRequest(receivedMessage);
                    }
                    else
                    {
                        throw new DARException(String.Format(CultureInfo.CurrentCulture, "Unknown Dispatcher Message type: '{0}'", dispatcherMessage.type));
                    }
                }
                catch (JsonReaderException)
                {
                    runner.AppendLineToDebugAdapterOutput("Response could not be parsed as json. This was the response:");
                    runner.AppendLineToDebugAdapterOutput(receivedMessage);
                    throw;
                }
            }
        }

        private static bool TryFindOSXCrashReport(DebugAdapterRunner runner, out string crashReport)
        {
            crashReport = null;

            string homeDir = Environment.GetEnvironmentVariable("HOME");
            if (String.IsNullOrEmpty(homeDir))
                return false;

            string crashReportDirectory = Path.Combine(homeDir, "Library/Logs/DiagnosticReports/");
            if (!Directory.Exists(crashReportDirectory))
                return false;

            string debugAdapterFilePath = runner.DebugAdapter?.StartInfo?.FileName;
            if (String.IsNullOrEmpty(debugAdapterFilePath))
                return false;
            string debugAdapterFileName = Path.GetFileName(debugAdapterFilePath);

            string crashReportPath = null;
            for (int retry = 0; retry < 15; retry++)
            {
                IEnumerable<string> crashReportFiles = Directory.EnumerateFiles(crashReportDirectory, debugAdapterFileName + "*")
                    .Select(name => Path.Combine(crashReportDirectory, name));

                (string Path, DateTime CreationTime) latestCrashReport = crashReportFiles
                    .Select<string, (string Path, DateTime CreationTime)>(path => new(path, File.GetCreationTime(path)))
                    .OrderByDescending(tuple => tuple.CreationTime)
                    .FirstOrDefault();
                if (latestCrashReport.Path == null ||
                    latestCrashReport.CreationTime < runner.StartTime)
                {
                    // It can take a little while for the crash report to get written, so wait a second and try again
                    // NOTE: From what I have seen, crash logs are written in one shot, so, unless we get very unlucky,
                    // we should get the whole thing.
                    Thread.Sleep(1000);
                    continue;
                }

                crashReportPath = latestCrashReport.Path;
                break;
            }

            if (crashReportPath == null)
            {
                // Crash log was never found
                return false;
            }

            crashReport = File.ReadAllText(crashReportPath);
            return true;
        }
    }
}
