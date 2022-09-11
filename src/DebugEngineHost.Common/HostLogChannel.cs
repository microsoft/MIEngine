// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*
 * NOTE: This file is shared between DebugEngineHost and DebugEngineHost.VSCode
 */

using System;
using System.Globalization;
using System.IO;

namespace Microsoft.DebugEngineHost
{
    public class HostLogChannel
    {
        private readonly Action<string> _log;
        private readonly StreamWriter _logFile;

        private HostLogChannel() { }

        public HostLogChannel(Action<string> logAction, string file)
        {
            _log = logAction;

            if (!string.IsNullOrEmpty(file))
            {
                _logFile = File.CreateText(file);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="verbosity"></param>
        /// <param name="message"></param>
        public void WriteLine(string message)
        {
            _log?.Invoke(message);
            _logFile?.WriteLine(message);
            _logFile?.Flush();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="verbosity"></param>
        /// <param name="format"></param>
        /// <param name="values"></param>
        public void WriteLine(string format, params object[] values)
        {
            string message = string.Format(CultureInfo.InvariantCulture, format, values);
            _log?.Invoke(message);
            _logFile?.WriteLine(message);
            _logFile?.Flush();
        }

        public void Flush()
        {
            _logFile?.Flush();
        }

        public void Close()
        {
            _logFile?.Close();
        }
    }
}
