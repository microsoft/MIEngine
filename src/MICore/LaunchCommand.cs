// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Collections.ObjectModel;

namespace MICore
{
    /// <summary>
    /// A {command, description, ignore failure} tuple for a launch/setup command. These are either read from an XML launch options blob, or returned from a launcher.
    /// </summary>
    public class LaunchCommand
    {
        public readonly string CommandText;
        public readonly string Description;
        public readonly bool IgnoreFailures;
        public readonly bool IsMICommand;
        public /*OPTIONAL*/ Action<string> FailureHandler { get; private set; }
        public /*OPTIONAL*/ Func<string, Task> SuccessHandler { get; private set; }

        public LaunchCommand(string commandText, string description = null, bool ignoreFailures = false, Action<string> failureHandler = null, Func<string, Task> successHandler = null)
        {
            if (commandText == null)
                throw new ArgumentNullException("commandText");
            commandText = commandText.Trim();
            if (commandText.Length == 0)
                throw new ArgumentOutOfRangeException("commandText");
            this.IsMICommand = commandText[0] == '-';
            this.CommandText = commandText;
            this.Description = description;
            if (string.IsNullOrWhiteSpace(description))
                this.Description = this.CommandText;

            this.IgnoreFailures = ignoreFailures;
            this.FailureHandler = failureHandler;
            this.SuccessHandler = successHandler;
        }

        public static ReadOnlyCollection<LaunchCommand> CreateCollection(List<Json.LaunchOptions.SetupCommand> source)
        {
            IList<LaunchCommand> commands = source?.Select(x => new LaunchCommand(x.Text, x.Description, x.IgnoreFailures.GetValueOrDefault(false))).ToList();
            if(commands == null)
            {
                commands = new List<LaunchCommand>(0);
            }

            return new ReadOnlyCollection<LaunchCommand>(commands);
        }

        public static ReadOnlyCollection<LaunchCommand> CreateCollection(Xml.LaunchOptions.Command[] source)
        {
            LaunchCommand[] commandArray = source?.Select(x => new LaunchCommand(x.Value, x.Description, x.IgnoreFailures)).ToArray();
            if (commandArray == null)
            {
                commandArray = new LaunchCommand[0];
            }

            return new ReadOnlyCollection<LaunchCommand>(commandArray);
        }
    }
}
