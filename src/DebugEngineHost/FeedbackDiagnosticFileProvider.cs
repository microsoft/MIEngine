// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Internal.VisualStudio.Shell.Embeddable.Feedback;

namespace Microsoft.DebugEngineHost
{
    [Export(typeof(IFeedbackDiagnosticFileProvider))]
    public class FeedbackDiagnosticFileProvider : IFeedbackDiagnosticFileProvider
    {
        public IReadOnlyCollection<string> GetFiles()
        {
            if (!HostLogger.HasFeedbackEntries)
            {
                return Array.Empty<string>();
            }

            string logFileName = HostLogger.GetFeedbackLogFilePath(Process.GetCurrentProcess().Id);

            IReadOnlyCollection<string> entries = HostLogger.GetNewFeedbackEntries();
            if (entries.Count > 0)
            {
                _ = Task.Run(() => WriteFeedbackEntries(logFileName, entries));
            }

            return new[] { logFileName };
        }

        private static void WriteFeedbackEntries(string logFileName, IReadOnlyCollection<string> entries)
        {
            try
            {
                using (StreamWriter logWriter = FeedbackLogBuffer.OpenLogFile(logFileName))
                {
                    FeedbackLogBuffer.WriteEntries(logWriter, entries);
                }
            }
            catch
            {
            }
        }
    }
}
