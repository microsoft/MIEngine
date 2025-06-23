using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MakePIAPortable
{
    class Program
    {
#if DEBUG
        // Set this to a valid number to break the processing when we get to it
        public const int BreakLine = -1;
#endif

        const string System_Runtime = "System.Runtime";
        const string System_Runtime_InteropServices = "System.Runtime.InteropServices";
        const string System_Threading = "System.Threading";

        static List<Tuple<string, string>> s_contractAssemblies = new List<Tuple<string, string>>
        {
            new Tuple<string, string>(System_Runtime, "4:0:20:0"),
            new Tuple<string, string>(System_Runtime_InteropServices, "4:0:20:0"),
            new Tuple<string, string>(System_Threading, "4:0:20:0")
        };

        static Dictionary<string, string> s_typeToContractAssemblyMap = new Dictionary<string, string> {

            // System.Runtime
            { "System.Byte", System_Runtime },
            { "System.Decimal", System_Runtime },
            { "System.Enum", System_Runtime },
            { "System.FlagsAttribute", System_Runtime },
            { "System.Guid", System_Runtime },
            { "System.Object", System_Runtime },
            { "System.Type", System_Runtime },
            { "System.ValueType", System_Runtime },
            { "System.Collections.Generic.IEnumerable", System_Runtime },
            { "System.Reflection.DefaultMemberAttribute", System_Runtime },
            { "System.Reflection.AssemblyDelaySignAttribute", System_Runtime},
            { "System.Reflection.AssemblyKeyFileAttribute", System_Runtime },
            { "System.Reflection.AssemblySignatureKeyAttribute", System_Runtime },
            { "System.Runtime.CompilerServices.CompilationRelaxationsAttribute", System_Runtime },
            { "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute", System_Runtime },
            { "System.Diagnostics.DebuggableAttribute", System_Runtime },
            { "System.Reflection.AssemblyTitleAttribute", System_Runtime },
            { "System.Runtime.Versioning.TargetFrameworkAttribute", System_Runtime },
            { "System.Reflection.AssemblyCompanyAttribute", System_Runtime },
            { "System.Reflection.AssemblyConfigurationAttribute", System_Runtime },
            { "System.Reflection.AssemblyCopyrightAttribute", System_Runtime },
            { "System.Reflection.AssemblyFileVersionAttribute", System_Runtime },
            { "System.Reflection.AssemblyInformationalVersionAttribute", System_Runtime },
            { "System.Reflection.AssemblyProductAttribute", System_Runtime },
            { "System.Runtime.CompilerServices.CompilerGeneratedAttribute", System_Runtime },
            { "System.MulticastDelegate", System_Runtime },
            { "System.IAsyncResult", System_Runtime },
            { "System.AsyncCallback", System_Runtime },
            { "System.IDisposable", System_Runtime },
            { "System.Runtime.CompilerServices.RuntimeHelpers", System_Runtime },
            { "System.Array", System_Runtime },
            { "System.RuntimeFieldHandle", System_Runtime },
            { "System.Threading.Monitor", System_Threading },
            { "System.GC", System_Runtime },
            { "System.Exception", System_Runtime },
            { "System.Collections.IEnumerable", System_Runtime },
            { "System.Collections.IEnumerator", System_Runtime },
            { "System.DateTime", System_Runtime },
            { "System.ObsoleteAttribute", System_Runtime },
            { "System.Attribute", System_Runtime },
            { "System.AttributeUsageAttribute", System_Runtime },
            { "System.AttributeTargets", System_Runtime },

            // System.Runtime.InteropServices
            { "System.Runtime.InteropServices.ClassInterfaceAttribute", System_Runtime_InteropServices },
            { "System.Runtime.InteropServices.ClassInterfaceType", System_Runtime_InteropServices },
            { "System.Runtime.InteropServices.CoClassAttribute", System_Runtime_InteropServices },
            { "System.Runtime.InteropServices.ComInterfaceType", System_Runtime_InteropServices },
            { "System.Runtime.InteropServices.DispIdAttribute", System_Runtime_InteropServices },
            { "System.Runtime.InteropServices.GuidAttribute", System_Runtime_InteropServices},
            { "System.Runtime.InteropServices.InterfaceTypeAttribute", System_Runtime_InteropServices },
            { "System.Runtime.InteropServices.ComVisibleAttribute", System_Runtime_InteropServices },
            { "System.Runtime.InteropServices.ComSourceInterfacesAttribute", System_Runtime_InteropServices },
            { "System.Runtime.InteropServices.ComEventInterfaceAttribute", System_Runtime_InteropServices },
            { "System.Runtime.InteropServices.ComTypes.IConnectionPointContainer", System_Runtime_InteropServices },
            { "System.Runtime.InteropServices.ComTypes.IConnectionPoint", System_Runtime_InteropServices },
            { "System.Runtime.InteropServices.Marshal", System_Runtime_InteropServices },
            { "System.Runtime.InteropServices.InAttribute", System_Runtime_InteropServices },
            { "System.Runtime.InteropServices.ComTypes.ITypeLib", System_Runtime_InteropServices },
            { "System.Runtime.InteropServices.TypeIdentifierAttribute", System_Runtime_InteropServices },
        };

        static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("ERROR: unexpected syntax to MakePIAPortable.exe. Expected: MakePIAPortable.exe <input-il-file> <output-il-file>");
                return -1;
            }

            string inputFile = args[0];
            string outputFile = args[1];
            if (!File.Exists(inputFile))
            {
                Console.WriteLine("ERROR: Input file '{0}' does not exist.", inputFile);
                return -1;
            }

            try
            {
                return TransformIL(inputFile, outputFile);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: {0} while processing input file '{1}'. {2}", e.GetType(), inputFile, e.Message);
                return -1;
            }
        }

        private static int TransformIL(string inputFilePath, string outputFile)
        {
            bool isInterface = false;
            Regex mscorlibType = new Regex(@"\[mscorlib\][A-Za-z][A-Za-z\.]+");
            Regex paramCustomAttributeRegex = new Regex(@"^ *.param \[[0-9]+\]$");
            HashSet<string> emittedUnknownTypes = new HashSet<string>();

            using (InputFile inputFile = new InputFile(inputFilePath))
            using (StreamWriter output = new StreamWriter(File.OpenWrite(outputFile)))
            {
                while (true)
                {
                    string inputLine = inputFile.ReadLine();
                    if (inputLine == null)
                        break;


                    if (isInterface)
                    {
                        if (inputLine.Contains(" internalcall"))
                        {
                            inputLine = inputLine.Replace(" internalcall", "");
                        }

                        if (inputLine.Contains(" runtime "))
                        {
                            inputLine = inputLine.Replace(" runtime ", " ");
                        }
                    }

                    if (inputLine == ".assembly extern mscorlib")
                    {
                        if (!IsAssemblyReferenceBody(inputFile))
                        {
                            Error.Emit(Error.Code.BadMscorlibReference, inputFile, "Unexpected text near '.assembly extern mscorlib'. Unable to transform.");
                            return -1;
                        }

                        foreach (var assembly in s_contractAssemblies)
                        {
                            output.Write(".assembly extern ");
                            output.WriteLine(assembly.Item1);
                            output.WriteLine("{");
                            output.WriteLine("  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )");
                            output.Write("  .ver ");
                            output.WriteLine(assembly.Item2);
                            output.WriteLine("}");
                        }

                        continue;
                    }
                    else if (inputLine == ".assembly extern stdole")
                    {
                        if (!IsAssemblyReferenceBody(inputFile))
                        {
                            Error.Emit(Error.Code.BadStdoleReference, inputFile, "Unexpected text near '.assembly extern stdole'. Unable to transform.");
                            return -1;
                        }

                        // Drop the stdole reference on the floor. It wasn't actually used.
                        continue;
                    }
                    else if (inputLine.StartsWith("  .publickey = (00 24 00 00 04 80 00 00 94 00 00 00 06 02 00 00", StringComparison.Ordinal))
                    {
                        output.WriteLine("  .custom instance void [System.Runtime]System.Reflection.AssemblyDelaySignAttribute::.ctor(bool)");
                        output.WriteLine("           = { bool(true)}");
                        output.WriteLine("  .custom instance void [System.Runtime]System.Reflection.AssemblySignatureKeyAttribute::.ctor(string, string)");
                        output.WriteLine("           = {string('002400000c800000140100000602000000240000525341310008000001000100613399aff18ef1a2c2514a273a42d9042b72321f1757102df9ebada69923e2738406c21e5b801552ab8d200a65a235e001ac9adc25f2d811eb09496a4c6a59d4619589c69f5baf0c4179a47311d92555cd006acc8b5959f2bd6e10e360c34537a1d266da8085856583c85d81da7f3ec01ed9564c58d93d713cd0172c8e23a10f0239b80c96b07736f5d8b022542a4e74251a5f432824318b3539a5a087f8e53d2f135f9ca47f3bb2e10aff0af0849504fb7cea3ff192dc8de0edad64c68efde34c56d302ad55fd6e80f302d5efcdeae953658d3452561b5f36c542efdbdd9f888538d374cef106acf7d93a4445c3c73cd911f0571aaf3d54da12b11ddec375b3')");
                        output.WriteLine("              string('a5a866e1ee186f807668209f3b11236ace5e21f117803a3143abb126dd035d7d2f876b6938aaf2ee3414d5420d753621400db44a49c486ce134300a2106adb6bdb433590fef8ad5c43cba82290dc49530effd86523d9483c00f458af46890036b0e2c61d077d7fbac467a506eba29e467a87198b053c749aa2a4d2840c784e6d')}");

                        // fall through to outputting the input line
                    }
                    else if (inputLine.Contains("[mscorlib]"))
                    {
                        if (IsIgnoredCustomAttributeLine(inputLine))
                        {
                            SkipCustomAttribute(inputLine, inputFile);
                            continue;
                        }

                        StringBuilder lineBuilder = new StringBuilder();
                        Match match = mscorlibType.Match(inputLine);
                        int nextStartPos = 0;
                        while (match.Success)
                        {
                            lineBuilder.Append(inputLine, nextStartPos, match.Index - nextStartPos);
                            nextStartPos = match.Index + match.Length;

                            string typeName = match.Value.Substring("[mscorlib]".Length);
                            string assemblyName;
                            if (!s_typeToContractAssemblyMap.TryGetValue(typeName, out assemblyName))
                            {
                                if (!emittedUnknownTypes.Contains(typeName))
                                {
                                    emittedUnknownTypes.Add(typeName);
                                    Error.Emit(Error.Code.UnknownType, inputFile, "Unknown type '{0}'. Unable to map to contract assembly.", typeName);
                                }

                                assemblyName = "unknown_assembly";
                            }

                            lineBuilder.Append('[');
                            lineBuilder.Append(assemblyName);
                            lineBuilder.Append(']');
                            lineBuilder.Append(typeName);

                            match = match.NextMatch();
                        }

                        lineBuilder.Append(inputLine, nextStartPos, inputLine.Length - nextStartPos);
                        output.WriteLine(lineBuilder);
                        continue;
                    }
                    else if (paramCustomAttributeRegex.IsMatch(inputLine))
                    {
                        string customAttributeLine = inputFile.TestNextLine(IsIgnoredCustomAttributeLine);
                        if (customAttributeLine != null)
                        {
                            SkipCustomAttribute(customAttributeLine, inputFile);

                            inputFile.SkipBlankLines();

                            // The '.param' directive contains an integer with the custom attribute index. We currently don't have code to rewrite that index, so 
                            // error out if we detect we need such code. If we need to implement this in the future, we could do so by getting a bunch of lines, rewriting them
                            // and then unreading them (InputFile.UnreadLine)
                            string[] problemLines = inputFile.TestNextLines(
                                l1 => paramCustomAttributeRegex.IsMatch(l1),
                                l2 => !IsIgnoredCustomAttributeLine(l2));
                            if (problemLines != null)
                            {
                                Error.Emit(Error.Code.UnsupportedCustomAttribute, inputFile, "Non-ignored custom attribute found after ignored custom attribute. This is not currently supported by this script.");
                            }

                            continue;
                        }
                    }
                    else if (inputLine.StartsWith("// Metadata version: v", StringComparison.Ordinal))
                    {
                        // This line doesn't hurt, but it is slightly confusing since this isn't the metadata version we will use, so strip it.
                        continue;
                    }
                    // Remove 'import' from the interfaces
                    else if (inputLine.StartsWith(".class ", StringComparison.Ordinal))
                    {
                        // Remove '_EventProvider' classes from MS.VS.Interop since they
                        // use ArrayList which is not in netstandard 1.3
                        if (inputLine.Contains("_EventProvider"))
                        {
                            inputLine = "";
                            SkipClass(inputFile);
                        }
                        else
                        {
                            isInterface = inputLine.Contains(" interface");

                            if (inputLine.Contains(" import "))
                            {
                                inputLine = inputLine.Replace(" import ", " ");
                            }
                        }
                    }
                    output.WriteLine(inputLine);
                }
            }

            return Error.HasError ? 1 : 0;
        }

        private static bool IsAssemblyReferenceBody(InputFile inputFile)
        {
            return inputFile.TestNextLines(
                                        (x) => x == "{",
                                        (x) => true,
                                        (x) => true,
                                        (x) => x == "}"
                                        ) != null;
        }

        private static bool IsIgnoredCustomAttributeLine(string line)
        {
            return line.Contains(".custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(") ||
                line.Contains(".custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor()") ||
                line.Contains(".custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibTypeAttribute::.ctor(") ||
                line.Contains(".custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor(") ||
                line.Contains(".custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor()") ||
                line.Contains(".custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibFuncAttribute::.ctor(") ||
                line.Contains(".custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibFuncAttribute::.ctor(") ||
                line.Contains(".custom instance void [mscorlib]System.Resources.SatelliteContractVersionAttribute::.ctor(") ||
                line.Contains(".custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(") ||
                line.Contains(".custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(");
        }

        private static void SkipClass(InputFile inputFile)
        {
            int startLineNumber = inputFile.LineNumber;

            // Find first open brace for class.
            while (true)
            {
                string nextLine = inputFile.ReadLine();
                if (nextLine == null)
                {
                    Error.Emit(Error.Code.BadCustomAttribute, inputFile, "Unexpected end-of-file while looking for start of class brace starting on line {0}", startLineNumber);
                    return;
                }

                if (nextLine.IndexOf('{') >= 0)
                    break;
            }

            int braceCount = 1;

            // Look for end class brace.
            while (true)
            {
                string nextLine = inputFile.ReadLine();
                if (nextLine == null)
                {
                    Error.Emit(Error.Code.BadCustomAttribute, inputFile, "Unexpected end-of-file while looking for end of class starting on line {0}", startLineNumber);
                    return;
                }

                // Count for '{' within inner methods.
                if (nextLine.IndexOf('{') >= 0)
                    braceCount++;

                // Decrease count for '{' seen.
                if (nextLine.IndexOf('}') >= 0)
                    braceCount--;

                // If we met the name number of '}' as '{', we finished reading the class lines.
                if (braceCount == 0)
                    break;
            }
        }

        private static void SkipCustomAttribute(string firstLine, InputFile inputFile)
        {
            // Handle cases where the custom attribute ends on the same line, but with trailing comments.
            // E.g.
            //    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 08 4F 4C 45 2E 42 4F 4F 4C 00 00 )          // ...OLE.BOOL..
            int startOfCommentIndex = firstLine.IndexOf("//", StringComparison.Ordinal);
            if (startOfCommentIndex != -1)
            {
                firstLine = firstLine.Substring(0, startOfCommentIndex);
            }

            // This is a single line attribute, no need to search for the end of it.
            if (firstLine.TrimEnd(' ', '\t').EndsWith(")", StringComparison.Ordinal))
                return;

            int startLineNumber = inputFile.LineNumber;

            while (true)
            {
                string nextLine = inputFile.ReadLine();
                if (nextLine == null)
                {
                    Error.Emit(Error.Code.BadCustomAttribute, inputFile, "Unexpected end-of-file while looking for end of custom attribute starting on line {0}", startLineNumber);
                    return;
                }

                if (nextLine.IndexOf(')') >= 0)
                    break;
            }
        }
    }

    static class Error
    {
        static public bool HasError
        {
            get;
            private set;
        }

        public enum Code
        {
            BadMscorlibReference = 1,
            UnknownType = 2,
            BadCustomAttribute = 3,
            UnsupportedCustomAttribute = 4,
            BadStdoleReference = 5
        }

        public static void Emit(Code code, InputFile inputFile, string message)
        {
            HasError = true;
            Console.WriteLine("{0}({1}) : error MIP{2:000} : {3}", inputFile.FilePath, inputFile.LineNumber, (int)code, message);
        }

        public static void Emit(Code code, InputFile inputFile, string formatString, params object[] args)
        {
            Emit(code, inputFile, string.Format(CultureInfo.InvariantCulture, formatString, args));
        }
    }

    class InputFile : IDisposable
    {
        public readonly string FilePath;
        public int LineNumber
        {
            get;
            private set;
        }

        StreamReader _input;
        readonly LinkedList<string> _peekedLines = new LinkedList<string>();

        public InputFile(string filePath)
        {
            this.FilePath = filePath;
            _input = File.OpenText(filePath);
        }

        public string ReadLine()
        {
            this.LineNumber++;
#if DEBUG
            if (this.LineNumber == Program.BreakLine)
            {
                Debugger.Break();
            }
#endif

            return InternalGetLine();
        }

        /// <summary>
        /// Return a line the the input file. This is pushed as a stack.
        /// </summary>
        /// <param name="lineToReturn">Line to push</param>
        public void UnreadLine(string lineToReturn)
        {
            this.LineNumber--;
            InternalUngetLine(lineToReturn);
        }

        /// <summary>
        /// Test if the next line matches a predicate. If so, the line is returned and the current line number is increased.
        /// </summary>
        /// <param name="isMatch">Function to test if the next line matches. Returns true if it is a match</param>
        /// <returns>null if the next line is the end of file, or the line didn't match, otherwise the line</returns>
        public string TestNextLine(Func<string, bool> isMatch)
        {
            string nextLine = InternalGetLine();
            if (nextLine == null)
            {
                return null;
            }

            if (isMatch(nextLine))
            {
                LineNumber++;
                return nextLine;
            }
            else
            {
                InternalUngetLine(nextLine);
                return null;
            }
        }

        /// <summary>
        /// Test if the next lines matches a predicate. If so, the lines are returned and the current line number is increased.
        /// </summary>
        /// <param name="isMatchArray">Array of functions used to test if the next lines match. Each function returns true if that line matches.</param>
        /// <returns>null if the end of file is reached, or the lines didn't match, otherwise the array of lines</returns>
        public string[] TestNextLines(params Func<string, bool>[] isMatchArray)
        {
            string[] nextLines = new string[isMatchArray.Length];
            for (int c = 0; c < isMatchArray.Length; c++)
            {
                string nextLine = InternalGetLine();
                nextLines[c] = nextLine;
                if (nextLine == null || !isMatchArray[c](nextLine))
                {
                    // Put back the lines that we obtained
                    for (int j = c; j >= 0; j--)
                    {
                        InternalUngetLine(nextLines[j]);
                    }
                    return null;
                }
            }

            LineNumber += nextLines.Length;
            return nextLines;
        }

        private string InternalGetLine()
        {
            if (_peekedLines.Count != 0)
            {
                string lineToReturn = _peekedLines.First.Value;
                _peekedLines.RemoveFirst();
                return lineToReturn;
            }

            return _input.ReadLine();
        }

        private void InternalUngetLine(string lineToReturn)
        {
            if (lineToReturn != null)
            {
                _peekedLines.AddFirst(lineToReturn);
            }
        }

        public void Dispose()
        {
            _input.Close();
        }

        public void SkipBlankLines()
        {
            while (TestNextLine(x => string.IsNullOrWhiteSpace(x)) != null)
            {
            }
        }
    }
}