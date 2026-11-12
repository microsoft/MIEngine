// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MICore;
using Microsoft.DebugEngineHost;
using Xunit;

namespace MICoreUnitTests
{
    /// <summary>
    /// Tests for how the engine reacts to '*stopped,reason="exited-normally"'. gdb sends this record whenever
    /// *any* inferior exits and the record does not say which one, so the record must only end the debug
    /// session when the inferior which exited was the last one.
    /// See https://github.com/microsoft/vscode-cpptools/issues/13893.
    /// </summary>
    public class MultiInferiorExitTests
    {
        [Fact]
        public void TestSingleInferiorExitEndsSession()
        {
            using (TestHarness harness = new TestHarness())
            {
                harness.Send("=thread-group-added,id=\"i1\"");
                harness.Send("=thread-group-started,id=\"i1\",pid=\"1000\"");
                harness.Send("=thread-created,id=\"1\",group-id=\"i1\"");

                // gdb sends '=thread-group-exited' before the '*stopped' record, so by the time the '*stopped'
                // record is processed there are no thread groups left.
                harness.Send("=thread-exited,id=\"1\",group-id=\"i1\"");
                harness.Send("=thread-group-exited,id=\"i1\",exit-code=\"0\"");
                harness.Send("*stopped,reason=\"exited-normally\"");

                Assert.True(harness.WaitForProcessExit(), "The debug session should end when the only inferior exits.");
                Assert.Equal(0, harness.InferiorExitedCount);
                Assert.Equal(ProcessState.Exited, harness.Debugger.ProcessState);
            }
        }

        [Fact]
        public void TestNonFinalInferiorExitKeepsSessionAlive()
        {
            using (TestHarness harness = new TestHarness())
            {
                harness.Send("=thread-group-added,id=\"i1\"");
                harness.Send("=thread-group-started,id=\"i1\",pid=\"1000\"");
                harness.Send("=thread-created,id=\"1\",group-id=\"i1\"");

                // The debuggee forked with 'set detach-on-fork off', so gdb added a second inferior.
                harness.Send("=thread-group-added,id=\"i2\"");
                harness.Send("=thread-group-started,id=\"i2\",pid=\"1001\"");
                harness.Send("=thread-created,id=\"2\",group-id=\"i2\"");

                // The child exits. Thread group 'i1' is still alive.
                harness.Send("=thread-exited,id=\"2\",group-id=\"i2\"");
                harness.Send("=thread-group-exited,id=\"i2\",exit-code=\"0\"");
                harness.Send("*stopped,reason=\"exited-normally\"");

                Assert.True(harness.WaitForInferiorExited(), "The exit of one of two inferiors should be reported as an inferior exit.");
                Assert.Equal(0, harness.ProcessExitCount);
                Assert.Equal(ProcessState.Stopped, harness.Debugger.ProcessState);
                Assert.Equal(1, harness.SurvivingThreadId);

                // gdb leaves the exited inferior current, so the engine must select a live thread to make
                // subsequent commands (e.g. a plain '-exec-continue') work.
                Assert.Contains("-thread-select 1", harness.CommandsSent);
            }
        }

        [Fact]
        public void TestLastInferiorExitEndsSession()
        {
            using (TestHarness harness = new TestHarness())
            {
                harness.Send("=thread-group-added,id=\"i1\"");
                harness.Send("=thread-group-started,id=\"i1\",pid=\"1000\"");
                harness.Send("=thread-group-added,id=\"i2\"");
                harness.Send("=thread-group-started,id=\"i2\",pid=\"1001\"");

                harness.Send("=thread-group-exited,id=\"i2\",exit-code=\"0\"");
                harness.Send("*stopped,reason=\"exited-normally\"");
                Assert.True(harness.WaitForInferiorExited(), "The first of two inferiors to exit should not end the session.");
                Assert.Equal(0, harness.ProcessExitCount);

                // Now the remaining inferior exits. HandleThreadGroupExited schedules the synthetic
                // '*stopped,reason="exited"' record which ends the session.
                harness.Send("=thread-group-exited,id=\"i1\",exit-code=\"0\"");

                Assert.True(harness.WaitForProcessExit(), "The debug session should end when the last inferior exits.");
            }
        }

        private sealed class TestHarness : IDisposable
        {
            private const int TimeoutMs = 10000;

            private readonly ScriptedTransport _transport;

            public MICore.Debugger Debugger { get; }
            public int ProcessExitCount { get; private set; }
            public int InferiorExitedCount { get; private set; }
            public int SurvivingThreadId { get; private set; }
            public IReadOnlyList<string> CommandsSent => _transport.Commands;

            public TestHarness()
            {
                LaunchOptions launchOptions = LaunchOptions.GetInstance(
                    null,
                    "bogus-exe-path",
                    null,
                    null,
                    GetLaunchOptionsXml(),
                    false,
                    null,
                    TargetEngine.Native,
                    null);

                this.Debugger = new MICore.Debugger(launchOptions, Logger.EnsureInitialized());
                this.Debugger.ProcessExitEvent += (o, args) => this.ProcessExitCount++;
                this.Debugger.InferiorExitedEvent += (o, args) =>
                {
                    this.SurvivingThreadId = ((MICore.Debugger.InferiorExitedEventArgs)args).SurvivingThreadId;
                    this.InferiorExitedCount++;
                };

                _transport = new ScriptedTransport();
                this.Debugger.Init(_transport, launchOptions);
            }

            public void Send(string line)
            {
                ((ITransportCallback)this.Debugger).OnStdOutLine(line);
            }

            public bool WaitForProcessExit()
            {
                return Wait(() => this.ProcessExitCount > 0);
            }

            public bool WaitForInferiorExited()
            {
                return Wait(() => this.InferiorExitedCount > 0);
            }

            // The MI records are processed by 'async void' methods, so the effects may not be visible by the
            // time Send returns.
            private static bool Wait(Func<bool> condition)
            {
                System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
                while (stopwatch.ElapsedMilliseconds < TimeoutMs)
                {
                    if (condition())
                    {
                        return true;
                    }

                    Task.Delay(10).Wait();
                }

                return condition();
            }

            private static string GetLaunchOptionsXml()
            {
                string fakeFilePath = typeof(MultiInferiorExitTests).Assembly.Location;
                return string.Concat("<LocalLaunchOptions xmlns=\"http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014\"\n",
                    "MIDebuggerPath=\"", fakeFilePath, "\"\n",
                    "ExePath=\"", fakeFilePath, "\"\n",
                    "TargetArchitecture=\"x64\"\n",
                    "MIMode=\"gdb\"\n",
                    "/>");
            }

            public void Dispose()
            {
                _transport.Close();
            }
        }

        /// <summary>
        /// A transport which answers the handful of commands this test's code path sends, instead of replaying
        /// a captured log the way MockTransport does.
        /// </summary>
        private sealed class ScriptedTransport : ITransport
        {
            private ITransportCallback? _callback;
            private readonly List<string> _commands = new List<string>();

            /// <summary>
            /// Commands sent by the engine, with the MI token stripped off.
            /// </summary>
            public IReadOnlyList<string> Commands => _commands;

            public bool IsClosed { get; private set; }

            public int DebuggerPid => 0;

            public void Init(ITransportCallback transportCallback, LaunchOptions options, Logger logger, HostWaitLoop? waitLoop = null)
            {
                _callback = transportCallback;
            }

            public void Send(string cmd)
            {
                if (string.IsNullOrEmpty(cmd))
                {
                    return;
                }

                int tokenLength = 0;
                while (tokenLength < cmd.Length && char.IsDigit(cmd[tokenLength]))
                {
                    tokenLength++;
                }

                string token = cmd.Substring(0, tokenLength);
                string command = cmd.Substring(tokenLength);
                _commands.Add(command);

                string response;
                if (command.StartsWith("-thread-info", StringComparison.Ordinal))
                {
                    // Note that gdb reports no 'current-thread-id' when the current thread has exited.
                    response = "^done,threads=[{id=\"1\",target-id=\"Thread 1\",state=\"stopped\"}]";
                }
                else
                {
                    response = "^done";
                }

                _callback?.OnStdOutLine(string.Concat(token, response));
            }

            public void Close()
            {
                this.IsClosed = true;
            }

            public int ExecuteSyncCommand(string commandDescription, string commandText, int timeout, out string output, out string error)
            {
                throw new NotImplementedException();
            }

            public bool CanExecuteCommand()
            {
                return false;
            }
        }
    }
}
