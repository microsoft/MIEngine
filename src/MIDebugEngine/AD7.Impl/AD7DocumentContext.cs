// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;
using MICore;

namespace Microsoft.MIDebugEngine
{
    // This class represents a document context to the debugger. A document context represents a location within a source file. 
    internal class AD7DocumentContext : IDebugDocumentContext2
    {
        private readonly MITextPosition _textPosition;
        private AD7MemoryAddress _codeContext;


        public AD7DocumentContext(MITextPosition textPosition, AD7MemoryAddress codeContext)
        {
            _textPosition = textPosition;
            _codeContext = codeContext;
        }

        #region IDebugDocumentContext2 Members

        // Compares this document context to a given array of document contexts.
        int IDebugDocumentContext2.Compare(enum_DOCCONTEXT_COMPARE Compare, IDebugDocumentContext2[] rgpDocContextSet, uint dwDocContextSetLen, out uint pdwDocContext)
        {
            dwDocContextSetLen = 0;
            pdwDocContext = 0;

            return Constants.E_NOTIMPL;
        }

        // Retrieves a list of all code contexts associated with this document context.
        // The engine sample only supports one code context per document context and 
        // the code contexts are always memory addresses.
        int IDebugDocumentContext2.EnumCodeContexts(out IEnumDebugCodeContexts2 ppEnumCodeCxts)
        {
            ppEnumCodeCxts = null;

            if (_codeContext == null)
            {
                return Constants.E_FAIL;
            }

            try
            {
                AD7MemoryAddress[] codeContexts = new AD7MemoryAddress[1];
                codeContexts[0] = _codeContext;
                ppEnumCodeCxts = new AD7CodeContextEnum(codeContexts);
                return Constants.S_OK;
            }
            catch (MIException e)
            {
                return e.HResult;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        // Gets the document that contains this document context.
        // This method is for those debug engines that supply documents directly to the IDE. Since the sample engine
        // does not do this, this method returns E_NOTIMPL.
        int IDebugDocumentContext2.GetDocument(out IDebugDocument2 ppDocument)
        {
            ppDocument = null;
            return Constants.E_FAIL;
        }

        // Gets the language associated with this document context.
        int IDebugDocumentContext2.GetLanguageInfo(ref string pbstrLanguage, ref Guid pguidLanguage)
        {
            // CLRDBG TODO: Add 'language' to the MI

            string fileExtension = _textPosition.GetFileExtension();

            if (fileExtension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                pbstrLanguage = "C#";
                pguidLanguage = AD7Guids.guidLanguageCs;
            }
            // NOTE: Use a case sensitive comparison, since '.C' can be used for C++ on unix
            else if (fileExtension == ".c")
            {
                pbstrLanguage = "C";
                pguidLanguage = AD7Guids.guidLanguageC;
            }
            else
            {
                pbstrLanguage = "C++";
                pguidLanguage = AD7Guids.guidLanguageCpp;
            }

            return Constants.S_OK;
        }

        // Gets the displayable name of the document that contains this document context.
        int IDebugDocumentContext2.GetName(enum_GETNAME_TYPE gnType, out string pbstrFileName)
        {
            pbstrFileName = _textPosition.FileName;
            return Constants.S_OK;
        }

        // Gets the source code range of this document context.
        // A source range is the entire range of source code, from the current statement back to just after the previous s
        // statement that contributed code. The source range is typically used for mixing source statements, including 
        // comments, with code in the disassembly window.
        // Sincethis engine does not support the disassembly window, this is not implemented.
        int IDebugDocumentContext2.GetSourceRange(TEXT_POSITION[] pBegPosition, TEXT_POSITION[] pEndPosition)
        {
            throw new NotImplementedException("This method is not implemented");
        }

        // Gets the file statement range of the document context.
        // A statement range is the range of the lines that contributed the code to which this document context refers.
        int IDebugDocumentContext2.GetStatementRange(TEXT_POSITION[] pBegPosition, TEXT_POSITION[] pEndPosition)
        {
            try
            {
                pBegPosition[0].dwColumn = _textPosition.BeginPosition.dwColumn;
                pBegPosition[0].dwLine = _textPosition.BeginPosition.dwLine;

                pEndPosition[0].dwColumn = _textPosition.EndPosition.dwColumn;
                pEndPosition[0].dwLine = _textPosition.EndPosition.dwLine;
            }
            catch (MIException e)
            {
                return e.HResult;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }

            return Constants.S_OK;
        }

        // Moves the document context by a given number of statements or lines.
        // This is used primarily to support the Autos window in discovering the proximity statements around 
        // this document context. 
        int IDebugDocumentContext2.Seek(int nCount, out IDebugDocumentContext2 ppDocContext)
        {
            ppDocContext = null;
            return Constants.E_NOTIMPL;
        }

        #endregion
    }
}
