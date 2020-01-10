// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SSHDebugPS
{
    internal class LineBuffer
    {
        private readonly StringBuilder _textBuffer = new StringBuilder();

        public LineBuffer()
        {
        }

        public void ProcessText(string unbufferedText, out IEnumerable<string> newLines)
        {
            List<string> newLineList = null;

            int startLength = _textBuffer.Length;
            _textBuffer.Append(unbufferedText);

            int newStartIndex = 0;
            for (int c = startLength; c < _textBuffer.Length; c++)
            {
                if (_textBuffer[c] == '\n')
                {
                    int lineLength = c - newStartIndex;
                    if (lineLength > 0 && _textBuffer[c - 1] == '\r')
                    {
                        lineLength--;
                    }

                    if (newLineList == null)
                    {
                        newLineList = new List<string>();
                    }

                    string lineToAdd = _textBuffer.ToString(newStartIndex, lineLength);
                    newLineList.Add(lineToAdd);
                    newStartIndex = c + 1;
                }
            }

            if (newStartIndex > 0)
            {
                _textBuffer.Remove(0, newStartIndex);
            }

            newLines = newLineList ?? Enumerable.Empty<string>();
        }
    }
}
