// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;
using MICore;

namespace Microsoft.MIDebugEngine
{
    // This class implements IDebugThread2 which represents a thread running in a program.
    internal class AD7Thread : IDebugThread2
    {
        private readonly AD7Engine _engine;
        private readonly DebuggedThread _debuggedThread;

        public int Id
        {
            get
            {
                return _debuggedThread.Id;
            }
        }

        public AD7Thread(AD7Engine engine, DebuggedThread debuggedThread)
        {
            _engine = engine;
            _debuggedThread = debuggedThread;
        }

        private ThreadContext GetThreadContext()
        {
            ThreadContext threadContext = null;
            _engine.DebuggedProcess.WorkerThread.RunOperation(async () => threadContext = await _engine.DebuggedProcess.ThreadCache.GetThreadContext(_debuggedThread));

            return threadContext;
        }

        private string GetCurrentLocation(bool fIncludeModuleName)
        {
            ThreadContext cxt = GetThreadContext();
            string location = null;
            if (cxt != null)
            {
                location = "";
                if (fIncludeModuleName)
                {
                    if (cxt.From != null)
                    {
                        location = cxt.From + '!';
                    }
                    else
                    {
                        DebuggedModule module = cxt.FindModule(_engine.DebuggedProcess);
                        if (module != null)
                        {
                            location = module.Name + '!';
                        }
                    }
                }
                if (cxt.Function == null)
                {
                    location += _engine.GetAddressDescription(cxt.pc.Value);
                }
                else
                {
                    location += cxt.Function;
                }
            }

            return location;
        }

        internal DebuggedThread GetDebuggedThread()
        {
            return _debuggedThread;
        }

        #region IDebugThread2 Members

        // Determines whether the next statement can be set to the given stack frame and code context.
        int IDebugThread2.CanSetNextStatement(IDebugStackFrame2 stackFrame, IDebugCodeContext2 codeContext)
        {
            // CLRDBG TODO: This implementation should be changed to compare the method token
            ulong addr = ((AD7MemoryAddress)codeContext).Address;
            AD7StackFrame frame = ((AD7StackFrame)stackFrame);
            if (frame.ThreadContext.Level != 0 || frame.Thread != this || !frame.ThreadContext.pc.HasValue || _engine.DebuggedProcess.MICommandFactory.Mode == MIMode.Clrdbg)
            {
                return Constants.S_FALSE;
            }
            if (addr == frame.ThreadContext.pc)
            {
                return Constants.S_OK;
            }
            string toFunc = EngineUtils.GetAddressDescription(_engine.DebuggedProcess, addr);
            string fromFunc = EngineUtils.GetAddressDescription(_engine.DebuggedProcess, frame.ThreadContext.pc.Value);
            if (toFunc != fromFunc)
            {
                return Constants.S_FALSE;
            }
            return Constants.S_OK;
        }

        // Retrieves a list of the stack frames for this thread.
        // For the sample engine, enumerating the stack frames requires walking the callstack in the debuggee for this thread
        // and coverting that to an implementation of IEnumDebugFrameInfo2. 
        // Real engines will most likely want to cache this information to avoid recomputing it each time it is asked for,
        // and or construct it on demand instead of walking the entire stack.
        int IDebugThread2.EnumFrameInfo(enum_FRAMEINFO_FLAGS dwFieldSpec, uint nRadix, out IEnumDebugFrameInfo2 enumObject)
        {
            enumObject = null;
            try
            {
                // get the thread's stack frames
                System.Collections.Generic.List<ThreadContext> stackFrames = null;
                _engine.DebuggedProcess.WorkerThread.RunOperation(async () => stackFrames = await _engine.DebuggedProcess.ThreadCache.StackFrames(_debuggedThread));
                int numStackFrames = stackFrames != null ? stackFrames.Count : 0;
                FRAMEINFO[] frameInfoArray;

                if (numStackFrames == 0)
                {
                    // failed to walk any frames. Return an empty stack.
                    frameInfoArray = new FRAMEINFO[0];
                }
                else
                {
                    uint low = stackFrames[0].Level;
                    uint high = stackFrames[stackFrames.Count - 1].Level;
                    FilterUnknownFrames(stackFrames);
                    numStackFrames = stackFrames.Count;
                    frameInfoArray = new FRAMEINFO[numStackFrames];
                    List<ArgumentList> parameters = null;

                    if ((dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS) != 0 && !_engine.DebuggedProcess.MICommandFactory.SupportsFrameFormatting)
                    {
                        _engine.DebuggedProcess.WorkerThread.RunOperation(async () => parameters = await _engine.DebuggedProcess.GetParameterInfoOnly(this, (dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_VALUES) != 0,
                            (dwFieldSpec & enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_TYPES) != 0, low, high));
                    }

                    for (int i = 0; i < numStackFrames; i++)
                    {
                        var p = parameters != null ? parameters.Find((ArgumentList t) => t.Item1 == stackFrames[i].Level) : null;
                        AD7StackFrame frame = new AD7StackFrame(_engine, this, stackFrames[i]);
                        frame.SetFrameInfo(dwFieldSpec, out frameInfoArray[i], p != null ? p.Item2 : null);
                    }
                }

                enumObject = new AD7FrameInfoEnum(frameInfoArray);
                return Constants.S_OK;
            }
            catch (MIException e)
            {
                return e.HResult;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        private void FilterUnknownFrames(System.Collections.Generic.List<ThreadContext> stackFrames)
        {
            bool lastWasQuestion = false;
            for (int i = 0; i < stackFrames.Count;)
            {
                // replace sequences of "??" with one UnknownCode frame
                if (stackFrames[i].Function == null || stackFrames[i].Function.Equals("??", StringComparison.Ordinal))
                {
                    if (lastWasQuestion)
                    {
                        stackFrames.RemoveAt(i);
                        continue;
                    }
                    lastWasQuestion = true;
                    stackFrames[i] = new ThreadContext(stackFrames[i].pc, stackFrames[i].TextPosition, ResourceStrings.UnknownCode, stackFrames[i].Level, null);
                }
                else
                {
                    lastWasQuestion = false;
                }
                i++;
            }
        }

        // Get the name of the thread. For the sample engine, the name of the thread is always "Sample Engine Thread"
        int IDebugThread2.GetName(out string threadName)
        {
            threadName = _debuggedThread.Name;
            return Constants.S_OK;
        }

        // Return the program that this thread belongs to.
        int IDebugThread2.GetProgram(out IDebugProgram2 program)
        {
            program = _engine;
            return Constants.S_OK;
        }

        // Gets the system thread identifier.
        int IDebugThread2.GetThreadId(out uint threadId)
        {
            threadId = _debuggedThread.TargetId;
            return Constants.S_OK;
        }

        // Gets properties that describe a thread.
        int IDebugThread2.GetThreadProperties(enum_THREADPROPERTY_FIELDS dwFields, THREADPROPERTIES[] ptp)
        {
            try
            {
                THREADPROPERTIES props = new THREADPROPERTIES();

                if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_ID) != 0)
                {
                    props.dwThreadId = _debuggedThread.TargetId;
                    props.dwFields |= enum_THREADPROPERTY_FIELDS.TPF_ID;
                }
                if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_SUSPENDCOUNT) != 0)
                {
                    // sample debug engine doesn't support suspending threads
                    props.dwFields |= enum_THREADPROPERTY_FIELDS.TPF_SUSPENDCOUNT;
                }
                if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_STATE) != 0)
                {
                    props.dwThreadState = (uint)enum_THREADSTATE.THREADSTATE_RUNNING;
                    props.dwFields |= enum_THREADPROPERTY_FIELDS.TPF_STATE;
                }
                if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_PRIORITY) != 0)
                {
                    props.bstrPriority = "Normal";
                    props.dwFields |= enum_THREADPROPERTY_FIELDS.TPF_PRIORITY;
                }
                if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_NAME) != 0)
                {
                    props.bstrName = _debuggedThread.Name;
                    props.dwFields |= enum_THREADPROPERTY_FIELDS.TPF_NAME;
                }
                if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_LOCATION) != 0)
                {
                    props.bstrLocation = GetCurrentLocation(true);
                    props.dwFields |= enum_THREADPROPERTY_FIELDS.TPF_LOCATION;
                    if (props.bstrLocation == null)
                    {
                        // Thread deletion events may be delayed, in which case the thread object may still be present in the cache
                        // but the engine is unable to retrieve new data for it. So handle failure to get info for a dead thread.
                        props.dwThreadState = (uint)enum_THREADSTATE.THREADSTATE_DEAD;
                        props.dwFields |= enum_THREADPROPERTY_FIELDS.TPF_STATE;
                        props.bstrLocation = ResourceStrings.ThreadExited;
                    }
                }

                ptp[0] = props;
                return Constants.S_OK;
            }
            catch (MIException e)
            {
                return e.HResult;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        // Resume a thread.
        // This is called when the user chooses "Unfreeze" from the threads window when a thread has previously been frozen.
        int IDebugThread2.Resume(out uint suspendCount)
        {
            // The sample debug engine doesn't support suspending/resuming threads
            suspendCount = 0;
            return Constants.E_NOTIMPL;
        }

        // Sets the next statement to the given stack frame and code context.
        int IDebugThread2.SetNextStatement(IDebugStackFrame2 stackFrame, IDebugCodeContext2 codeContext)
        {
            // CLRDBG TODO: This implementation should be changed to call an MI command
            ulong addr = ((AD7MemoryAddress)codeContext).Address;
            AD7StackFrame frame = ((AD7StackFrame)stackFrame);
            if (frame.ThreadContext.Level != 0 || frame.Thread != this || !frame.ThreadContext.pc.HasValue || _engine.DebuggedProcess.MICommandFactory.Mode == MIMode.Clrdbg)
            {
                return Constants.S_FALSE;
            }
            string toFunc = EngineUtils.GetAddressDescription(_engine.DebuggedProcess, addr);
            string fromFunc = EngineUtils.GetAddressDescription(_engine.DebuggedProcess, frame.ThreadContext.pc.Value);
            if (toFunc != fromFunc)
            {
                return Constants.S_FALSE;
            }
            string result = frame.EvaluateExpression("$pc=" + EngineUtils.AsAddr(addr));
            if (result != null)
            {
                _engine.DebuggedProcess.ThreadCache.MarkDirty();
                return Constants.S_OK;
            }
            return Constants.S_FALSE;
        }

        // suspend a thread.
        // This is called when the user chooses "Freeze" from the threads window
        int IDebugThread2.Suspend(out uint suspendCount)
        {
            // The sample debug engine doesn't support suspending/resuming threads
            suspendCount = 0;
            return Constants.E_NOTIMPL;
        }

        #endregion

        #region Uncalled interface methods
        // These methods are not currently called by the Visual Studio debugger, so they don't need to be implemented

        int IDebugThread2.GetLogicalThread(IDebugStackFrame2 stackFrame, out IDebugLogicalThread2 logicalThread)
        {
            Debug.Fail("This function is not called by the debugger");

            logicalThread = null;
            return Constants.E_NOTIMPL;
        }

        int IDebugThread2.SetThreadName(string name)
        {
            Debug.Fail("This function is not called by the debugger");

            return Constants.E_NOTIMPL;
        }

        #endregion
    }
}
