// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MICore;
using Microsoft.VisualStudio.Debugger.Interop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.MIDebugEngine
{
    internal class MITextPosition
    {
        public string FileName { get; private set; }
        public TEXT_POSITION BeginPosition { get; private set; }
        public TEXT_POSITION EndPosition { get; private set; }

        public MITextPosition(string filename, TEXT_POSITION beginPosition, TEXT_POSITION endPosition)
        {
            this.FileName = filename;
            this.BeginPosition = beginPosition;
            this.EndPosition = endPosition;
        }

        public static MITextPosition TryParse(TupleValue miTuple)
        {
            string filename = miTuple.TryFindString("fullname");
            if (string.IsNullOrEmpty(filename))
            {
                filename = miTuple.TryFindString("file");
            }
            if (!string.IsNullOrEmpty(filename))
            {
                filename = DebuggedProcess.UnixPathToWindowsPath(filename);
            }

            if (string.IsNullOrWhiteSpace(filename))
                return null;

            uint? line = miTuple.TryFindUint("line");
            if (!line.HasValue || line.Value == 0)
                return null;

            uint lineValue = line.Value;

            Microsoft.VisualStudio.Debugger.Interop.TEXT_POSITION startPosition = new Microsoft.VisualStudio.Debugger.Interop.TEXT_POSITION();
            startPosition.dwLine = lineValue - 1;
            startPosition.dwColumn = 0;

            uint? startColumn = miTuple.TryFindUint("col");
            if (startColumn > 0)
            {
                startPosition.dwColumn = startColumn.Value - 1;
            }

            Microsoft.VisualStudio.Debugger.Interop.TEXT_POSITION endPosition = startPosition;
            uint? endLine = miTuple.TryFindUint("end-line");
            if (endLine > 0)
            {
                endPosition.dwLine = endLine.Value - 1;

                uint? endCol = miTuple.TryFindUint("end-col");
                if (endCol > 0)
                {
                    endPosition.dwColumn = endCol.Value - 1;
                }
            }

            return new MITextPosition(filename, startPosition, endPosition);
        }

        public string GetFileExtension()
        {
            int lastDotIndex = this.FileName.LastIndexOf('.');
            if (lastDotIndex < 0)
                return string.Empty;
            if (this.FileName.IndexOf('\\', lastDotIndex) >= 0)
                return string.Empty;

            return this.FileName.Substring(lastDotIndex);
        }
    }
}
