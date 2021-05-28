// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using DebuggerTesting.OpenDebug.Commands;
using DebuggerTesting.OpenDebug.Commands.Responses;
using DebuggerTesting.Utilities;

namespace DebuggerTesting.OpenDebug.Extensions
{
    /// <summary>
    /// Returns info on a variable, and allows expanded evaluation of variable info.
    /// </summary>
    internal class VariableInspector : DisposableObject, IVariableInspector
    {
        private Dictionary<string, IVariableInspector> variables;
        private string name;
        private string value;
        private int parentVariablesReference;
        private int? variablesReference;

        #region Constructor/Create/Dispose

        public VariableInspector(IDebuggerRunner runner, int parentVariablesReference, string name, string value, int? variablesReference)
        {
            Parameter.ThrowIfNull(runner, nameof(runner));
            this.DebuggerRunner = runner;
            this.parentVariablesReference = parentVariablesReference;
            this.name = name;
            this.value = value;
            this.variablesReference = variablesReference;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                DisposableHelper.SafeDisposeAll(this.variables?.Values);
                this.variables = null;
                this.DebuggerRunner = null;
            }
            base.Dispose(isDisposing);
        }

        #endregion

        public IDebuggerRunner DebuggerRunner { get; private set; }

        #region Variable Info

        public string Name
        {
            get
            {
                this.VerifyNotDisposed();
                return this.name;
            }
        }

        public string Value
        {
            get
            {
                this.VerifyNotDisposed();
                return this.value;
            }
            set
            {
                this.VerifyNotDisposed();
                this.DebuggerRunner.RunCommand(this.GetSetVariableCommand(value));
            }
        }

        public int? VariablesReference
        {
            get
            {
                this.VerifyNotDisposed();
                return this.variablesReference;
            }
        }

        #endregion

        public void SetVariableValueExpectFailure(string expression)
        {
            this.DebuggerRunner.RunCommandExpectFailure(this.GetSetVariableCommand(expression));
        }

        public IDictionary<string, IVariableInspector> Variables
        {
            get
            {
                this.VerifyNotDisposed();
                if (this.VariablesReference == null || this.VariablesReference == 0)
                    return null;

                if (this.variables == null)
                {
                    this.variables = GetChildVariables(this.DebuggerRunner, this.VariablesReference.Value);
                }
                return this.variables;
            }
        }

        internal static Dictionary<string, IVariableInspector> GetChildVariables(IDebuggerRunner runner, int variablesReference)
        {
            VariablesResponseValue variablesResponse = runner.RunCommand(new VariablesCommand(variablesReference));
            return variablesResponse.body.variables.ToDictionary<VariablesResponseValue.Body.Variable, string, IVariableInspector>(v => v.name, v => new VariableInspector(runner, variablesReference, v.name, v.value, v.variablesReference));
        }

        private ICommandWithResponse<SetVariableResponseValue> GetSetVariableCommand(string expression)
        {
            return new SetVariableCommand(this.parentVariablesReference, this.name, expression);
        }

        public override string ToString()
        {
            return "{0}={1}".FormatInvariantWithArgs(this.Name, this.Value);
        }
    }
}
