// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using DebuggerTesting.OpenDebug.Commands;
using DebuggerTesting.OpenDebug.Commands.Responses;
using DebuggerTesting.Utilities;

namespace DebuggerTesting.OpenDebug.Extensions
{
    internal class FrameInspector : DisposableObject, IFrameInspector
    {
        private int? variablesReference;
        private Dictionary<string, IVariableInspector> variables;
        private string name;
        private int id;
        private string sourceName;
        private string sourcePath;
        private int? line;
        private int? column;
        private int? sourceReference;
        private string instructionPointerReference;

        #region Constructor/Dispose

        public FrameInspector(IDebuggerRunner runner, string name, int id, string sourceName, string sourcePath, int? sourceReference, int? line, int? column, string instructionPointerReference)
        {
            Parameter.ThrowIfNull(runner, nameof(runner));
            this.DebuggerRunner = runner;
            this.name = name;
            this.id = id;
            this.sourceName = sourceName;
            this.sourcePath = sourcePath;
            this.sourceReference = sourceReference;
            this.line = line;
            this.column = column;
            this.instructionPointerReference = instructionPointerReference;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                DisposableHelper.SafeDisposeAll(this.variables?.Values);
                this.variables = null;
                this.DebuggerRunner = null;
            }
            base.Dispose(IsDisposed);
        }

        #endregion

        #region Frame Info

        public IDebuggerRunner DebuggerRunner { get; private set; }

        public string Name
        {
            get
            {
                this.VerifyNotDisposed();
                return this.name;
            }
        }

        public int Id
        {
            get
            {
                this.VerifyNotDisposed();
                return this.id;
            }
        }

        public string SourceName
        {
            get
            {
                this.VerifyNotDisposed();
                return this.sourceName;
            }
        }

        public string SourcePath
        {
            get
            {
                this.VerifyNotDisposed();
                return this.sourcePath;
            }
        }

        public int? SourceReference
        {
            get
            {
                this.VerifyNotDisposed();
                return this.sourceReference;
            }
        }

        public int? Line
        {
            get
            {
                this.VerifyNotDisposed();
                return this.line;
            }
        }

        public int? Column
        {
            get
            {
                this.VerifyNotDisposed();
                return this.column;
            }
        }

        public string InstructionPointerReference
        {
            get
            {
                this.VerifyNotDisposed();
                return this.instructionPointerReference;
            }
        }

        #endregion

        /// <summary>
        /// Gets the local variables on this frame.
        /// </summary>
        public IDictionary<string, IVariableInspector> Variables
        {
            get
            {
                this.VerifyNotDisposed();
                if (this.variables == null)
                {
                    // Ensure the scope has been created
                    if (this.variablesReference == null)
                    {
                        ScopesCommand scopesCommand = new ScopesCommand(this.Id);
                        this.DebuggerRunner.RunCommand(scopesCommand);
                        this.variablesReference = scopesCommand.VariablesReference;
                    }

                    if (this.variablesReference == null || this.variablesReference == 0)
                        return null;

                    // Get the variables
                    this.variables = VariableInspector.GetChildVariables(this.DebuggerRunner, this.variablesReference.Value);
                }
                return this.variables;
            }
        }

        /// <summary>
        /// Evaluates an expression on this frame.
        /// WARNING: if an expression has side effects, you will need to Refresh the IInspector and
        /// get the stack information to get up to date info.
        /// </summary>
        public string Evaluate(string expression, EvaluateContext context = EvaluateContext.None)
        {
            this.VerifyNotDisposed();
            EvaluateResponseValue response = this.DebuggerRunner.RunCommand(new EvaluateCommand(expression, this.Id, context));
            return response.body.result;
        }

        public string GetSourceContent()
        {
            this.VerifyNotDisposed();
            if (this.sourceReference == null)
                return null;
            return this.DebuggerRunner.RunCommand(new SourceCommand(this.sourceReference.Value))?.body?.content;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(this.Name);
            sb.Append(" (");
            sb.Append(this.Id);
            sb.Append(")");
            if (this.SourceName != null)
            {
                sb.Append(" [");
                sb.Append(this.SourceName);
                if (this.Line != null)
                {
                    sb.Append(":");
                    sb.Append(this.Line.Value);
                }
                sb.Append("]");
            }
            return sb.ToString();
        }

    }
}
