// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using DebuggerTesting.OpenDebug.Commands;
using DebuggerTesting.OpenDebug.Commands.Responses;
using DebuggerTesting.Utilities;

namespace DebuggerTesting.OpenDebug.Extensions
{
    internal class ThreadInspector : DisposableObject, IThreadInspector
    {
        private int threadId;
        private IList<FrameInspector> frameInspectors = new List<FrameInspector>(20);

        #region Constructor/Dispose

        public ThreadInspector(IDebuggerRunner runner, int? threadId = null)
        {
            Parameter.ThrowIfNull(runner, nameof(runner));
            this.DebuggerRunner = runner;
            this.threadId = threadId ?? runner.StoppedThreadId;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                this.Refresh();
                this.DebuggerRunner = null;
            }
            base.Dispose(isDisposing);
        }

        #endregion

        public IDebuggerRunner DebuggerRunner { get; private set; }

        public int ThreadId
        {
            get { return this.threadId; }
        }

        public IEnumerable<IFrameInspector> Stack
        {
            get
            {
                this.VerifyNotDisposed();
                int startFrame = 0;

                while (true)
                {
                    StackTraceCommand stackTraceCommand = new StackTraceCommand(this.threadId, startFrame);
                    StackTraceResponseValue response = this.DebuggerRunner.RunCommand(stackTraceCommand);
                    if (response?.body?.stackFrames == null || response.body.stackFrames.Length <= 0)
                        yield break;

                    startFrame += response.body.stackFrames.Length;
                    foreach (var stackFrame in response.body.stackFrames)
                    {
                        string name = stackFrame.name;
                        int id = stackFrame.id ?? -1;
                        string sourceName = stackFrame.source?.name;
                        string sourcePath = stackFrame.source?.path;
                        int? sourceReference = stackFrame.source?.sourceReference;
                        int? line = stackFrame.line;
                        int? column = stackFrame.column;

                        FrameInspector frame = new FrameInspector(this.DebuggerRunner, name, id, sourceName, sourcePath, sourceReference, line, column);
                        this.frameInspectors.Add(frame);
                        yield return frame;
                    }
                }
            }
        }

        public void Refresh()
        {
            this.frameInspectors.SafeDisposeAll();
            this.frameInspectors.Clear();
        }
    }
}
