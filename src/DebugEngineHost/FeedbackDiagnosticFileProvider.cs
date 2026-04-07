// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Internal.VisualStudio.Shell.Embeddable.Feedback;

namespace Microsoft.DebugEngineHost
{
    [Export(typeof(IFeedbackDiagnosticFileProvider))]
    public class FeedbackDiagnosticFileProvider : IFeedbackDiagnosticFileProvider
    {
        public IReadOnlyCollection<string> GetFiles()
        {
            string logFileName = HostLogger.GetFeedbackLogFilePath(Process.GetCurrentProcess().Id);

            try
            {
                IReadOnlyCollection<string> entries = HostLogger.GetNewFeedbackEntries();
                if (entries.Count > 0)
                {
                    using (var fs = new FileStream(logFileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    using (var logWriter = new StreamWriter(fs, Encoding.UTF8))
                    {
                        foreach (string logLine in entries)
                        {
                            logWriter.WriteLine(logLine);
                        }

                        logWriter.Flush();
                    }
                }
            }
            catch
            {
            }

            if (File.Exists(logFileName))
            {
                return new[] { logFileName };
            }

            return new string[0];
        }
    }
}
