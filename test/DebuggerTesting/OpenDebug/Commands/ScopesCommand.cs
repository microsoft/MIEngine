// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DebuggerTesting.OpenDebug.Commands.Responses;

namespace DebuggerTesting.OpenDebug.Commands
{
    public sealed class ScopesArgs : JsonValue
    {
        public int frameId;
    }

    public class ScopesCommand : CommandWithResponse<ScopesArgs, ScopesResponseValue>
    {
        public ScopesCommand(int frameId) : base("scopes")
        {
            this.Args.frameId = frameId;
            this.ExpectedResponse = new ScopesResponse(this.Name);
        }

        public int VariablesReference { get; private set; }

        public override void ProcessActualResponse(IActualResponse response)
        {
            base.ProcessActualResponse(response);
            this.VariablesReference = this.ActualResponse?.body?.scopes?[0]?.variablesReference ?? -1;
        }
    }
}
