// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Interop.DAP;

namespace Microsoft.MIDebugEngine
{
    // This class represents a succesfully parsed expression to the debugger. 
    // It is returned as a result of a successful call to IDebugExpressionContext2.ParseText
    // It allows the debugger to obtain the values of an expression in the debuggee. 
    // For the purposes of this sample, this means obtaining the values of locals and parameters from a stack frame.
    public class AD7Expression : IDebugExpression2, IDebugExpressionDAP
    {
        private AD7Engine _engine;
        private IVariableInformation _var;

        internal AD7Expression(AD7Engine engine, IVariableInformation var)
        {
            _engine = engine;
            _var = var;
        }

        #region IDebugExpression2 Members

        // This method cancels asynchronous expression evaluation as started by a call to the IDebugExpression2::EvaluateAsync method.
        int IDebugExpression2.Abort()
        {
            throw new NotImplementedException();
        }

        // This method evaluates the expression asynchronously.
        // This method should return immediately after it has started the expression evaluation. 
        // When the expression is successfully evaluated, an IDebugExpressionEvaluationCompleteEvent2 
        // must be sent to the IDebugEventCallback2 event callback
        //
        // This is primarily used for the immediate window
        int IDebugExpression2.EvaluateAsync(enum_EVALFLAGS dwFlags, IDebugEventCallback2 pExprCallback)
        {
            if (((dwFlags & enum_EVALFLAGS.EVAL_NOSIDEEFFECTS) != 0 && (dwFlags & enum_EVALFLAGS.EVAL_ALLOWBPS) == 0) && _var.IsVisualized)
            {
                IVariableInformation variable = _engine.DebuggedProcess.Natvis.Cache.Lookup(_var);
                if (variable == null)
                {
                    _var.AsyncError(pExprCallback, new AD7ErrorProperty(_var.Name, ResourceStrings.NoSideEffectsVisualizerMessage));
                }
                else
                {
                    _var = variable;    // use the old value
                    Task.Run(() =>
                    {
                        new EngineCallback(_engine, pExprCallback).OnExpressionEvaluationComplete(variable);
                    });
                }
            }
            else
            {
                _var.AsyncEval(pExprCallback);
            }
            return Constants.S_OK;
        }

        // This method evaluates the expression synchronously.
        int IDebugExpression2.EvaluateSync(enum_EVALFLAGS dwFlags, uint dwTimeout, IDebugEventCallback2 pExprCallback, out IDebugProperty2 ppResult)
        {
            return EvaluateSyncInternal(dwFlags, DAPEvalFlags.NONE, dwTimeout, pExprCallback, out ppResult);
        }

        #endregion

        #region IDebugExpressionDAP

        int IDebugExpressionDAP.EvaluateSync(enum_EVALFLAGS dwFlags, DAPEvalFlags dapFlags, uint dwTimeout, IDebugEventCallback2 pExprCallback, out IDebugProperty2 ppResult)
        {
            return EvaluateSyncInternal(dwFlags, dapFlags, dwTimeout, pExprCallback, out ppResult);
        }

        #endregion

        private int EvaluateSyncInternal(enum_EVALFLAGS dwFlags, DAPEvalFlags dapFlags, uint dwTimeout, IDebugEventCallback2 pExprCallback, out IDebugProperty2 ppResult)
        {
            ppResult = null;
            if ((dwFlags & enum_EVALFLAGS.EVAL_NOSIDEEFFECTS) != 0 && _var.IsVisualized)
            {
                IVariableInformation variable = _engine.DebuggedProcess.Natvis.Cache.Lookup(_var);
                if (variable == null)
                {
                    ppResult = new AD7ErrorProperty(_var.Name, ResourceStrings.NoSideEffectsVisualizerMessage);
                }
                else
                {
                    _var = variable;
                    ppResult = new AD7Property(_engine, _var);
                }
                return Constants.S_OK;
            }

            _var.SyncEval(dwFlags, dapFlags);
            ppResult = new AD7Property(_engine, _var);
            return Constants.S_OK;
        }

    }
}
