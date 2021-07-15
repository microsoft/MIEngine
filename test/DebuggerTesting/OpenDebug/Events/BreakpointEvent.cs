// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.Events
{
    public enum BreakpointReason
    {
        Unset = 0,
        Changed,
        New,
    }

    #region BreakpointEventValue

    public sealed class BreakpointEventValue : EventValue
    {
        public sealed class Body
        {
            public sealed class Breakpoint
            {
                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public int? id;

                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public bool? verified;

                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public string message;

                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public int? line;

                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public int? column;
            }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string reason;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public Breakpoint breakpoint;
        }

        public Body body = new Body();
    }

    #endregion

    /// <summary>
    /// Event that fires when a breakpoint has been modified
    /// </summary>
    public class BreakpointEvent : Event<BreakpointEventValue>
    {
        private bool verifyLineRange;
        private int startLine;
        private int endLine;

        public BreakpointEvent(BreakpointReason reason, int? line)
            : base("breakpoint")
        {
            this.ExpectedResponse.body.reason = GetReason(reason);

            if (line != null)
            {
                this.ExpectedResponse.body.breakpoint = new BreakpointEventValue.Body.Breakpoint();
                this.ExpectedResponse.body.breakpoint.line = line;
            }
        }

        /// <summary>
        /// Create an expected breakpoint event that works over a range of lines
        /// </summary>
        public BreakpointEvent(BreakpointReason reason, int startLine, int endLine)
            : this(reason, line: null)
        {
            Parameter.ThrowIfNegativeOrZero(startLine, nameof(startLine));
            Parameter.ThrowIfNegativeOrZero(startLine, nameof(endLine));

            this.startLine = startLine;
            this.endLine = endLine;
            this.verifyLineRange = true;
        }

        private static string GetReason(BreakpointReason reason)
        {
            Parameter.ThrowIfIsInvalid(reason, BreakpointReason.Unset, nameof(reason));
            return reason.ToString().ToLowerInvariant();
        }

        public override void ProcessActualResponse(IActualResponse response)
        {
            base.ProcessActualResponse(response);

            if (this.verifyLineRange)
                StoppedEvent.VerifyLineRange(this?.ActualEvent?.body?.breakpoint?.line, this.startLine, this.endLine);
        }

        private string GetExpectedLine()
        {
            if (this.verifyLineRange)
                return "{0}-{1}".FormatInvariantWithArgs(this.startLine, this.endLine);
            return (this.ExpectedResponse.body.breakpoint?.line ?? 0).ToString(CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            return "{0} ({1}, line {2})".FormatInvariantWithArgs(base.ToString(), this.ExpectedResponse.body.reason, this.GetExpectedLine());
        }
    }
}
