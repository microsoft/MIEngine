// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

namespace OpenDebugAD7
{
    internal class TextPositionTuple
    {
        public readonly Source Source;
        public readonly int Line;
        public readonly int Column;
        public static readonly TextPositionTuple Nil = new TextPositionTuple(null, 0, 0);

        private TextPositionTuple(Source source, int line, int column)
        {
            this.Source = source;
            this.Line = line;
            this.Column = column;
        }

        public static TextPositionTuple GetTextPositionOfFrame(PathConverter converter, IDebugStackFrame2 frame)
        {
            IDebugDocumentContext2 documentContext;
            if (frame.GetDocumentContext(out documentContext) == 0 &&
                documentContext != null)
            {
                TEXT_POSITION[] beginPosition = new TEXT_POSITION[1];
                TEXT_POSITION[] endPosition = new TEXT_POSITION[1];
                documentContext.GetStatementRange(beginPosition, endPosition);

                string filePath;
                documentContext.GetName(enum_GETNAME_TYPE.GN_FILENAME, out filePath);

                string convertedFilePath = converter.ConvertDebuggerPathToClient(filePath);

                Source source = new Source()
                {
                    Path = convertedFilePath,
                    Name = Path.GetFileName(convertedFilePath)
                };
                int line = converter.ConvertDebuggerLineToClient((int)beginPosition[0].dwLine);
                int column = unchecked((int)(beginPosition[0].dwColumn + 1));

                return new TextPositionTuple(source, line, column);
            }

            return null;
        }
    }
}
