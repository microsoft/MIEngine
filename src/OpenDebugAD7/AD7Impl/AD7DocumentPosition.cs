// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Microsoft.DebugEngineHost;
using Microsoft.VisualStudio.Debugger.Interop;

namespace OpenDebugAD7.AD7Impl
{
    internal sealed class AD7DocumentPosition : IDebugDocumentPosition2, IDebugDocumentPosition110
    {
        public string Path
        {
            get
            {
                return _document?.Path;
            }
        }
        public int Line { get; private set; }

        private SessionConfiguration _config;
        private Document _document;

        public AD7DocumentPosition(SessionConfiguration config, string path, int line)
        {
            _config = config;
            _document = Document.GetOrCreate(path);
            Line = line;
        }

        public int GetFileName(out string pbstrFileName)
        {
            pbstrFileName = Path;
            return 0;
        }

        public int GetDocument(out IDebugDocument2 ppDoc)
        {
            throw new NotImplementedException();
        }

        public int IsPositionInDocument(IDebugDocument2 pDoc)
        {
            throw new NotImplementedException();
        }

        public int GetRange(TEXT_POSITION[] pBegPosition, TEXT_POSITION[] pEndPosition)
        {
            pBegPosition[0].dwLine = (uint)Line;
            pBegPosition[0].dwColumn = 0;

            pEndPosition[0].dwLine = (uint)Line;
            pEndPosition[0].dwColumn = 0;

            return 0;
        }

        public int GetChecksum(ref Guid guidAlgorithm, CHECKSUM_DATA[] checksumData)
        {
            checksumData[0].ByteCount = 0;
            checksumData[0].pBytes = IntPtr.Zero;

            return HRConstants.E_NOTIMPL;
        }

        public int IsChecksumEnabled(out int fChecksumEnabled)
        {
            fChecksumEnabled = 0;

            if (_config.RequireExactSource)
            {
                // TODO: see comment in GetChecksum
                fChecksumEnabled = 0;
            }
            return HRConstants.S_OK;
        }

        public int GetLanguage(out Guid pguidLanguage)
        {
            throw new NotImplementedException();
        }

        public int GetText(out string pbstrText)
        {
            throw new NotImplementedException();
        }
    }

    internal class ChecksumNormalizationException : Exception
    {
        public ChecksumNormalizationException(string message)
            : base(message)
        {
        }
    }

    internal class AD7FunctionPosition : IDebugFunctionPosition2
    {
        public string Name { get; private set; }

        public AD7FunctionPosition(string name)
        {
            Name = name;
        }

        public int GetFunctionName(out string pbstrFunctionName)
        {
            pbstrFunctionName = Name;
            return HRConstants.S_OK;
        }

        public int GetOffset(TEXT_POSITION[] pPosition)
        {
            return HRConstants.E_NOTIMPL;
        }
    }
}
