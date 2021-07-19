// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using DebuggerTesting.OpenDebug.Commands.Responses;
using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.Commands
{
    #region InitializeCommandArgs

    public sealed class InitializeCommandArgs : JsonValue
    {
        public string adapterID;
        public bool linesStartAt1;
        public bool columnsStartAt1;
        public string pathFormat;
    }

    #endregion

    public sealed class InitializeResponseValue : CommandResponseValue
    {
        public sealed class Body
        {
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool? supportsConfigurationDoneRequest;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool? supportsFunctionBreakpoints;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool? supportsConditionalBreakpoints;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool? supportsEvaluateForHovers;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool? supportsSetVariable;
        }

        public Body body = new Body();
    }

    internal class InitializeResponse : CommandResponse<InitializeResponseValue>
    {
        public InitializeResponse(string commandName)
            : base(commandName)
        { }
    }

    /// <summary>
    /// Required initialization information passed to the debugger.
    /// </summary>
    public class InitializeCommand : CommandWithResponse<InitializeCommandArgs, InitializeResponseValue>
    {
        public InitializeCommand(string adapterId) : base("initialize")
        {
            this.Args.adapterID = adapterId;
            this.Args.linesStartAt1 = true;
            this.Args.columnsStartAt1 = true;
            this.Args.pathFormat = "path";
        }
    }
}
