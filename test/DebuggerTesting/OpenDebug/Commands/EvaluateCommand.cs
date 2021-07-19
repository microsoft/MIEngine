// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using DebuggerTesting.OpenDebug.Commands.Responses;
using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.Commands
{
    public enum EvaluateContext
    {
        Unset = 0,
        None,
        DataTip,
        Watch,
    }

    #region EvaluateCommandArgs

    public sealed class EvaluateCommandArgs : JsonValue
    {
        public string expression;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? frameId;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string context;
    }

    #endregion

    /// <summary>
    /// Evaluates an expression
    /// </summary>
    public class EvaluateCommand : CommandWithResponse<EvaluateCommandArgs, EvaluateResponseValue>
    {
        public EvaluateCommand(string expression, int frameId, EvaluateContext context = EvaluateContext.None)
            : base("evaluate")
        {
            Parameter.ThrowIfIsInvalid(context, EvaluateContext.Unset, nameof(context));
            this.Args.expression = expression;
            this.Args.frameId = frameId;
            this.Args.context = ContextToName(context);
        }

        private static string ContextToName(EvaluateContext context)
        {
            switch (context)
            {
                case EvaluateContext.None:
                    return null;
                case EvaluateContext.DataTip:
                    return "hover";
                case EvaluateContext.Watch:
                    return "watch";
                default:
                    throw new ArgumentOutOfRangeException(nameof(context));
            }
        }

        public string ActualResult { get; private set; }

        public override void ProcessActualResponse(IActualResponse response)
        {
            base.ProcessActualResponse(response);
            this.ActualResult = this.ActualResponse?.body?.result;
        }

        public override string ToString()
        {
            return "{0} ({1})".FormatInvariantWithArgs(base.ToString(), this.Args.expression);
        }
    }
}
