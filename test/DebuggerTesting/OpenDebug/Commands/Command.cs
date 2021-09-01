// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using DebugAdapterRunner;
using DebuggerTesting.OpenDebug.Commands.Responses;
using Newtonsoft.Json;
using DarCommand = DebugAdapterRunner.Command;
using DarRunner = DebugAdapterRunner.DebugAdapterRunner;

namespace DebuggerTesting.OpenDebug.Commands
{
    /// <summary>
    /// Base class for all Debug Adapter commands. Commands are issued and expect a response.
    /// </summary>
    /// <typeparam name="T">The class containing arguments to the command</typeparam>
    public abstract class Command<T> : DarCommand, ICommand
        where T : new()
    {
        public Command(string name)
        {
            Parameter.ThrowIfNull(name, nameof(name));
            base.Name = name;
            this.Name = name;
            this.Args = new T();
            this.SetExpectedResponse(success: true);
            this.Timeout = TimeSpan.Zero;
        }

        /// <summary>
        /// Call to set the expected success response of the command
        /// </summary>
        private void SetExpectedResponse(bool success)
        {
            this.ExpectedResponse = new CommandResponse(this.Name, success);
        }

        #region ICommand

        public new string Name { get; private set; }

        object ICommand.DynamicArgs { get { return Args; } }

        IResponse ICommand.ExpectedResponse { get { return this.ExpectedResponse; } }

        public virtual void ProcessActualResponse(IActualResponse response)
        {
            Parameter.ThrowIfNull(response, nameof(response));
            CommandResponseValue commandResponse = response.Convert<CommandResponseValue>();
            Parameter.ThrowIfNull(commandResponse, nameof(commandResponse));
            this.Success = commandResponse.success;
            this.Message = commandResponse.message;
        }

        #endregion

        /// <summary>
        /// Arguments to the command.
        /// </summary>
        public T Args { get; protected set; }

        #region Response Handling

        protected IResponse ExpectedResponse { get; set; }

        public bool ExpectsSuccess
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                this.SetExpectedResponse(success: value);
            }
        }

        public bool Success { get; private set; }

        public string Message { get; private set; }

        #endregion

        public TimeSpan Timeout { get; set; }

        #region Running

        public override void Run(DarRunner darRunner)
        {
            this.Run(darRunner, null);
        }

        public void Run(IDebuggerRunner runner, params IEvent[] expectedEvents)
        {
            Parameter.ThrowIfNull(runner, nameof(runner));

            if (runner.ErrorEncountered)
            {
                runner.WriteLine("Previous error. Skipping command '{0}'.", this.Name);
                return;
            }

            try
            {
                this.Run(runner.DarRunner, runner, expectedEvents);
            }
            catch (Exception)
            {
                runner.ErrorEncountered = true;
                  throw;
            }
        }

        /// <summary>
        /// Runs a command against the debugger.
        /// </summary>
        /// <param name="command">The command to run.</param>
        /// <param name="expectedEvents">[OPTIONAL] If the command causes an event to occur, pass the expected event(s)</param>
        private void Run(DarRunner darRunner, ILoggingComponent log, params IEvent[] expectedEvents)
        {
            Parameter.ThrowIfNull(darRunner, nameof(darRunner));

            log?.WriteLine("Running command {0}", this.ToString());
            log?.WriteLine("Command '{0}' expecting response: {1}", this.Name, this.ExpectedResponse.ToString());

            DebugAdapterResponse darCommandResponse = GetDarResponse(this.ExpectedResponse);
            DebugAdapterCommand darCommand = new DebugAdapterCommand(
                this.Name,
                this.Args,
                new[] { darCommandResponse });
            List<Tuple<DebugAdapterResponse, IEvent>> darEventMap = new List<Tuple<DebugAdapterResponse, IEvent>>(expectedEvents.Length);

            // Add additional expected events to match if requested
            if (expectedEvents != null && expectedEvents.Length > 0)
            {
                if (expectedEvents.Length > 1)
                    log?.WriteLine("Command '{0}' expecting {1} events:", this.Name, expectedEvents.Length);

                foreach (var expectedEvent in expectedEvents)
                {
                    DebugAdapterResponse darEventResponse = GetDarResponse(expectedEvent);
                    darCommand.ExpectedResponses.Add(darEventResponse);
                    darEventMap.Add(Tuple.Create(darEventResponse, expectedEvent));

                    // Debug info for expected response
                    string eventMessage = expectedEvents.Length > 1 ? "  - {1}" : "Command '{0}' expecting event: {1}";
                    log?.WriteLine(eventMessage, this.Name, expectedEvent.ToString());
                }
            }

            // Allow the command to override the timeout
            int overrideTimeout = Convert.ToInt32(this.Timeout.TotalMilliseconds);
            int savedTimeout = darRunner.ResponseTimeout;

            try
            {
                if (overrideTimeout > 0)
                {
                    darRunner.ResponseTimeout = overrideTimeout;
                    log?.WriteLine("Command '{0}' timeout set to {1:n0} seconds.", this.Name, this.Timeout.TotalSeconds);
                }

                darCommand.Run(darRunner);

                // Allow the command to retrieve properties from the actual matched response.
                string responseJson = JsonConvert.SerializeObject(darCommandResponse.Match);
                if (!string.IsNullOrWhiteSpace(responseJson))
                {
                    this.ProcessActualResponse(new ActualResponse(responseJson));
                }

                // Allow the events to retrieve properties from the actual event.
                foreach (var darEvent in darEventMap)
                {
                    string eventJson = JsonConvert.SerializeObject(darEvent.Item1.Match);
                    darEvent.Item2.ProcessActualResponse(new ActualResponse(eventJson));
                }
            }
            catch (Exception ex)
            {
                // Add information to the log when the exception occurs
                log?.WriteLine("ERROR: Running command '{0}'. Exception thrown.", this.Name);
                log?.WriteLine(UDebug.ExceptionToString(ex));

                // The DARException is not serializable, create a new exception
                if (ex is DARException)
                    throw new RunnerException(ex.Message);
                else
                    throw;
            }
            finally
            {
                if (overrideTimeout > 0)
                    darRunner.ResponseTimeout = savedTimeout;
            }
        }

        // Create a DAR Response from an expected response
        private static DebugAdapterResponse GetDarResponse(IResponse response)
        {
            return new DebugAdapterResponse(response.DynamicResponse, response.IgnoreOrder, response.IgnoreResponseOrder);
        }

        #region ActualResponse

        /// <summary>
        /// Provides a way to get the command to interperet the actual result
        /// that comes back from the Debug Adapter.
        /// </summary>
        private class ActualResponse : IActualResponse
        {
            private string responseJson;

            public ActualResponse(string responseJson)
            {
                this.responseJson = responseJson;
            }

            public R Convert<R>()
            {
                try
                {
                    return JsonConvert.DeserializeObject<R>(responseJson);
                }
                catch (JsonReaderException ex)
                {
                    throw new FormatException("Malformed JSON: " + responseJson, ex);
                }
            }
        }

        #endregion

        #endregion

        public override string ToString()
        {
            return this.Name;
        }
    }

    public abstract class CommandWithResponse<T, R> : Command<T>, ICommandWithResponse<R>
        where T : new()
        where R : new()
    {
        public CommandWithResponse(string name) : base(name)
        {
        }

        public R ActualResponse { get; protected set; }

        public override void ProcessActualResponse(IActualResponse response)
        {
            base.ProcessActualResponse(response);
            this.ActualResponse = response.Convert<R>();
        }

        public new R Run(IDebuggerRunner runner, params IEvent[] expectedEvents)
        {
            base.Run(runner, expectedEvents);
            return this.ActualResponse;
        }
    }
}
