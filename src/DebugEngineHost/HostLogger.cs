// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DebugEngineHost
{
    public sealed class HostLogger
    {
        readonly StreamWriter _streamWriter;

        internal HostLogger(StreamWriter streamWriter)
        {
            _streamWriter = streamWriter;
        }

        public void WriteLine(string line)
        {
            lock (_streamWriter)
            {
                _streamWriter.WriteLine(line);
            }
        }

        public void Flush()
        {
            lock (_streamWriter)
            {
                _streamWriter.Flush();
            }
        }
    }
}
