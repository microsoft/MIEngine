// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.Commands.Responses
{
    public class CommandResponseValue : JsonValue
    {
        public bool success;
        public string command;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string message;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? request_seq;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? seq;
    }

    public abstract class CommandResponse<T> : Response<T>
        where T : CommandResponseValue, new()
    {
        public CommandResponse(string commandName, bool success = true)
        {
            this.ExpectedResponse.command = commandName;
            this.ExpectedResponse.success = success;
            this.ExpectedResponse.message = null;
        }

        public CommandResponse(string commandName, string message)
        {
            this.ExpectedResponse.command = commandName;
            this.ExpectedResponse.success = false;
            this.ExpectedResponse.message = message;
        }

        public override string ToString()
        {
            if (this.ExpectedResponse.message != null)
                return "{0} (Fail '{1}')".FormatInvariantWithArgs(base.ToString(), this.ExpectedResponse.message);
            else
                return "{0} ({1})".FormatInvariantWithArgs(base.ToString(), this.ExpectedResponse.success ? "Success" : "Fail");
        }
    }

    /// <summary>
    /// The most common command response, returns the command name and a bool with success result.
    /// </summary>
    public sealed class CommandResponse : CommandResponse<CommandResponseValue>
    {
        public CommandResponse(string commandName, bool success)
            : base(commandName, success)
        {
        }
    }
}
