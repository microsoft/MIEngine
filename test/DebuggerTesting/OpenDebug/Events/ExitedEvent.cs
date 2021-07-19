// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.Events
{
    #region ExitedEventValue

    public sealed class ExitedEventValue : EventValue
    {
        public sealed class Body
        {
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int? exitCode;
        }

        public Body body = new Body();
    }

    #endregion

    /// <summary>
    /// Event that fires when the process exits
    /// </summary>
    public class ExitedEvent : Event<ExitedEventValue>
    {
        public ExitedEvent(int? exitCode = null)
            : base("exited")
        {
            this.ExpectedResponse.body.exitCode = exitCode;
        }

        public int ActualExitCode { get; private set; }

        public override void ProcessActualResponse(IActualResponse response)
        {
            base.ProcessActualResponse(response);
            this.ActualExitCode = this.ActualEvent?.body?.exitCode ?? -1;
        }

        public override string ToString()
        {
            return "{0} ({1})".FormatInvariantWithArgs(base.ToString(), this.ExpectedResponse.body.exitCode);
        }
    }
}
