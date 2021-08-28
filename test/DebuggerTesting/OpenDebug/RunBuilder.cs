// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using DebuggerTesting.OpenDebug.Commands;
using DebuggerTesting.OpenDebug.Events;

namespace DebuggerTesting.OpenDebug
{
    /// <summary>
    /// A builder for running a command with a set of expected events
    /// </summary>
    public interface IRunBuilder
    {
        IRunBuilder Event<T>(T expectedEvent, Action<T> postSatisfyAction = null) where T : IEvent;

        void AfterCommand(ICommand command);

        /// <summary>
        /// Specify the amount of time to wait for the command and all the events
        /// </summary>
        IRunBuilder WithTimeout(TimeSpan timeout);
    }

    /// <summary>
    /// Extension methods for specific events and commands
    /// </summary>
    public static class RunBuilderExtensions
    {
        /// <summary>
        /// Check for event only if the condition is true
        /// </summary>
        public static IRunBuilder ConditionalEvent(this IRunBuilder runBuilder, bool condition, Func<IRunBuilder, IRunBuilder> thenEvent)
        {
            return (condition) ?
                thenEvent(runBuilder) :
                runBuilder;
        }

        #region Specific Events

        public static IRunBuilder StoppedEvent(this IRunBuilder runBuilder, StoppedReason reason, string fileName = null, int? lineNumber = null, string text = null)
        {
            return runBuilder.Event(new StoppedEvent(reason, fileName, lineNumber, text));
        }

        public static IRunBuilder HitInstructionBreakpointEvent(this IRunBuilder runBuilder, string address)
        {
            ulong nextAddress;

            if (address.StartsWith("0x", StringComparison.Ordinal))
            {
                nextAddress = Convert.ToUInt64(address.Substring(2), 16);
            }
            else
            {
                nextAddress = Convert.ToUInt64(address, 10);
            }

            return runBuilder.Event(new StoppedEvent(nextAddress));
        }

        public static IRunBuilder HitBreakpointEvent(this IRunBuilder runBuilder, string fileName = null, int? lineNumber = null, string text = null)
        {
            return runBuilder.StoppedEvent(StoppedReason.Breakpoint, fileName, lineNumber, text);
        }

        public static IRunBuilder HitStepEvent(this IRunBuilder runBuilder, string fileName = null, int? lineNumber = null, string text = null)
        {
            return runBuilder.StoppedEvent(StoppedReason.Step, fileName, lineNumber, text);
        }

        public static IRunBuilder HitEntryEvent(this IRunBuilder runBuilder, string fileName = null, int? lineNumber = null, string text = null)
        {
            return runBuilder.StoppedEvent(StoppedReason.Entry, fileName, lineNumber, text);
        }

        public static IRunBuilder HitStopEventWithinRange(this IRunBuilder runBuilder, StoppedReason reason, string fileName, int startLine, int endLine, Action<StoppedEvent> postSatisfyAction = null)
        {
            return runBuilder.Event(new StoppedEvent(reason, fileName, startLine, endLine), postSatisfyAction);
        }

        public static IRunBuilder ExitedEvent(this IRunBuilder runBuilder, int? exitCode = null)
        {
            return runBuilder.Event(new ExitedEvent(exitCode));
        }

        public static IRunBuilder TerminatedEvent(this IRunBuilder runBuilder)
        {
            return runBuilder.Event(new TerminatedEvent());
        }

        public static IRunBuilder BreakpointChangedEvent(this IRunBuilder runBuilder, BreakpointReason reason, int line)
        {
            return runBuilder.Event(new BreakpointEvent(reason, line));
        }

        /// <summary>
        /// Function breakpoints may resolve to different lines depending on the compiler/debugger combination.
        /// Sometimes they resolve to the curly brace line. Other times on the first line of code.
        /// </summary>
        public static IRunBuilder FunctionBreakpointChangedEvent(this IRunBuilder runBuilder, BreakpointReason reason, int startLine, int endLine)
        {
            return runBuilder.Event(new BreakpointEvent(reason, startLine, endLine));
        }

        public static IRunBuilder OutputEvent(this IRunBuilder runBuilder, string text, CategoryValue category)
        {
            return runBuilder.Event(new OutputEvent(text, category));
        }

        #endregion

        #region Specific Commands

        private static int GetStoppedThreadId(IRunBuilder runBuilder)
        {
            return ((RunBuilder)runBuilder).Runner.StoppedThreadId;
        }

        public static void AfterContinue(this IRunBuilder runBuilder)
        {
            runBuilder.AfterCommand(new ContinueCommand(GetStoppedThreadId(runBuilder)));
        }

        public static void AfterStepIn(this IRunBuilder runBuilder)
        {
            runBuilder.AfterCommand(new StepInCommand(GetStoppedThreadId(runBuilder)));
        }

        public static void AfterStepOver(this IRunBuilder runBuilder)
        {
            runBuilder.AfterCommand(new StepOverCommand(GetStoppedThreadId(runBuilder)));
        }

        public static void AfterStepOut(this IRunBuilder runBuilder)
        {
            runBuilder.AfterCommand(new StepOutCommand(GetStoppedThreadId(runBuilder)));
        }

        public static void AfterConfigurationDone(this IRunBuilder runBuilder)
        {
            runBuilder.AfterCommand(new ConfigurationDoneCommand());
        }

        public static void AfterAsyncBreak(this IRunBuilder runBuilder)
        {
            runBuilder.AfterCommand(new AsyncBreakCommand());
        }

        public static void AfterSetBreakpoints(this IRunBuilder runBuilder, SourceBreakpoints sourceBreakpoints)
        {
            runBuilder.AfterCommand(new SetBreakpointsCommand(sourceBreakpoints));
        }

        public static void AfterSetFunctionBreakpoints(this IRunBuilder runBuilder, FunctionBreakpoints functionBreakpoints)
        {
            runBuilder.AfterCommand(new SetFunctionBreakpointsCommand(functionBreakpoints));
        }

        #endregion
    }

    #region RunBuilder

    public class RunBuilder : DisposableObject, IRunBuilder
    {
        private List<IEvent> expectedEvents;
        private Stack<Action> postSatisfyActions;
        private TimeSpan? timeout;

        #region Constructor/Dispose

        public RunBuilder(IDebuggerRunner runner)
        {
            this.Runner = runner;
            this.expectedEvents = new List<IEvent>(1);
            this.postSatisfyActions = new Stack<Action>();
        }

        protected override void Dispose(bool isDisposing)
        {
            // If the composed statement doesn't have a command at the end,
            // it will not run. So throw an exception if this is detected.
            if (!isDisposing && !this.IsDisposed)
            {
                throw new RunnerException("RunBuilder requires a call to AfterCommand to execute.");
            }
            base.Dispose(isDisposing);
        }

        #endregion

        public IDebuggerRunner Runner { get; private set; }

        public IRunBuilder Event<T>(T expectedEvent, Action<T> postSatisfyAction) where T : IEvent
        {
            this.VerifyNotDisposed();
            this.expectedEvents.Add(expectedEvent);
            if (null != postSatisfyAction)
            {
                this.postSatisfyActions.Push(() => postSatisfyAction(expectedEvent));
            }
            return this;
        }

        public IRunBuilder WithTimeout(TimeSpan timeout)
        {
            this.timeout = timeout;
            return this;
        }

        public void AfterCommand(ICommand command)
        {
            this.VerifyNotDisposed();
            try
            {
                if (this.timeout != null)
                    command.Timeout = this.timeout.Value;
                this.Runner.RunCommand(command, this.expectedEvents.ToArray());
                // At this point, the command has been executed and the expected events have been satisfied.
                // Run the actions that should occur after the events have been satisfied.
                while (this.postSatisfyActions.Count > 0)
                    this.postSatisfyActions.Pop().Invoke();
            }
            finally
            {
                this.Dispose();
            }
        }
    }

    #endregion
}
