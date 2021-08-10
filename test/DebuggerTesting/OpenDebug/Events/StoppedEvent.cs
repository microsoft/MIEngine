// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using DebuggerTesting.OpenDebug.Commands.Responses;
using Newtonsoft.Json;
using Xunit;

namespace DebuggerTesting.OpenDebug.Events
{
    public enum StoppedReason
    {
        Unknown = 0,
        Step,
        Breakpoint,
        Pause,
        Exception,
        Entry,
        InstructionBreakpoint
    }

    #region StoppedEventValue

    public sealed class StoppedEventValue : EventValue
    {
        public sealed class Body
        {
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string reason;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public Source source;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int? line;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string text;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int? threadId;
        }

        public Body body = new Body();
    }

    #endregion

    public interface IStoppedInfo
    {
        StoppedReason Reason { get; }
        string Filename { get; }
        int? Line { get; }
        string Text { get; }
        int? ThreadId { get; }
    }

    /// <summary>
    /// Event that fires when entering break mode
    /// </summary>
    public class StoppedEvent : Event<StoppedEventValue>
    {
        private bool verifyLineRange;
        private int startLine;
        private int endLine;
        private ulong address;

        public StoppedEvent(ulong address)
            : base("stopped")
        {

            this.address = address;

            this.ExpectedResponse.body.reason = FromReason(StoppedReason.InstructionBreakpoint);

            this.verifyLineRange = false;
        }

        public StoppedEvent(StoppedReason? reason = null, string fileName = null, int? lineNumber = null, string text = null)
            : base("stopped")
        {
            this.ExpectedResponse.body.reason = FromReason(reason);
            if (fileName != null)
            {
                this.ExpectedResponse.body.source = new Source();
                this.ExpectedResponse.body.source.name = fileName;
            }
            this.ExpectedResponse.body.line = lineNumber;
            this.ExpectedResponse.body.text = text;
            this.verifyLineRange = false;
        }

        /// <summary>
        /// Create an expected stopped event that works over a range of lines
        /// </summary>
        public StoppedEvent(StoppedReason? reason, string fileName, int startLine, int endLine)
            : base("stopped")
        {
            Parameter.ThrowIfNegativeOrZero(startLine, nameof(startLine));
            Parameter.ThrowIfNegativeOrZero(startLine, nameof(endLine));

            this.ExpectedResponse.body.reason = FromReason(reason);
            if (fileName != null)
            {
                this.ExpectedResponse.body.source = new Source();
                this.ExpectedResponse.body.source.name = fileName;
            }

            this.startLine = startLine;
            this.endLine = endLine;
            this.verifyLineRange = true;
        }


        private static string FromReason(StoppedReason? reason)
        {
            if (reason == null)
                return null;

            Parameter.ThrowIfIsInvalid(reason.Value, StoppedReason.Unknown, nameof(reason));

            if (reason == StoppedReason.InstructionBreakpoint)
            {
                return "instruction breakpoint";
            }

            return Enum.GetName(typeof(StoppedReason), reason.Value).ToLowerInvariant();
        }

        private static StoppedReason? ToReason(string value)
        {
            StoppedReason reason;
            if (Enum.TryParse(value, true, out reason))
                return reason;
            return null;
        }

        /// <summary>
        /// The actual information from the event
        /// </summary>
        public IStoppedInfo ActualEventInfo { get; private set; }

        public int ThreadId
        {
            get { return this.ActualEventInfo.ThreadId ?? -1; }
        }

        public override void ProcessActualResponse(IActualResponse response)
        {
            base.ProcessActualResponse(response);
            this.ActualEventInfo = new StoppedInfo(this.ActualEvent);

            if (this.verifyLineRange)
                VerifyLineRange(this.ActualEventInfo.Line, this.startLine, this.endLine);
        }

        /// <summary>
        /// Validates that the line number was withing an expected range.
        /// This may occur on debuggers that change behavior between different versions or on different platforms.
        /// </summary>
        /// <param name="actualLine">The line number that was encountered.</param>
        /// <param name="expectedStartLine">The first valid line number that could be encountered.</param>
        /// <param name="expectedLineRange">The range of valid line numbers.</param>
        internal static void VerifyLineRange(int? actualLine, int expectedStartLine, int expectedEndLine)
        {
            // If verifying over line range check result and error now
            if (actualLine == null || actualLine < expectedStartLine || actualLine > expectedEndLine)
            {
                string message = "Expected a line within {0}-{1} but actual line was {2}.".FormatInvariantWithArgs(expectedStartLine, expectedEndLine, actualLine);
                Assert.True(false, message);
            }
        }

        private string GetExpectedLine()
        {
            if (this.verifyLineRange)
                return "{0}-{1}".FormatInvariantWithArgs(this.startLine, this.endLine);
            return (this.ExpectedResponse.body.line ?? 0).ToString(CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            string source = null;
            if (this.ExpectedResponse.body.source != null)
            {
                source = " ({0}:{1})".FormatInvariantWithArgs(this.ExpectedResponse.body.source.name, this.GetExpectedLine());
            }
            return "{0} ({1}){2}".FormatInvariantWithArgs(base.ToString(), this.ExpectedResponse.body.reason, source);
        }

        #region StoppedInfo

        private class StoppedInfo : IStoppedInfo
        {
            public StoppedInfo(StoppedEventValue value)
            {
                this.Reason = ToReason(value?.body?.reason) ?? StoppedReason.Unknown;
                this.Filename = value?.body?.source?.name;
                this.Line = value?.body?.line;
                this.Text = value?.body?.text;
                this.ThreadId = value?.body?.threadId;
            }

            public string Filename { get; private set; }

            public int? Line { get; private set; }

            public StoppedReason Reason { get; private set; }

            public string Text { get; private set; }

            public int? ThreadId { get; private set; }
        }

        #endregion
    }
}
