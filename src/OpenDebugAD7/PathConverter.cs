using OpenDebug;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDebugAD7
{
    internal class PathConverter
    {
        internal PathMapper m_pathMapper { get; set; }
        internal bool DebuggerLinesStartAt1 { get; set; }
        internal bool ClientLinesStartAt1 { get; set; }
        internal bool DebuggerPathsAreURI { get; set; }
        internal bool ClientPathsAreURI { get; set; }

        internal PathConverter()
        {
            ClientLinesStartAt1 = true;
            ClientPathsAreURI = true;
        }

        internal int ConvertDebuggerLineToClient(int line)
        {
            if (DebuggerLinesStartAt1)
            {
                return ClientLinesStartAt1 ? line : line - 1;
            }
            else
            {
                return ClientLinesStartAt1 ? line + 1 : line;
            }
        }

        internal int ConvertClientLineToDebugger(int line)
        {
            if (DebuggerLinesStartAt1)
            {
                return ClientLinesStartAt1 ? line : line + 1;
            }
            else
            {
                return ClientLinesStartAt1 ? line - 1 : line;
            }
        }

        internal int ConvertDebuggerColumnToClient(int column)
        {
            // TODO@AW same as line
            return column;
        }

        internal string ConvertDebuggerPathToClient(string path)
        {
            path = m_pathMapper.ResolveSymbolPath(path);

            if (Path.DirectorySeparatorChar == '/')
            {
                path = path.Replace('\\', '/');
            }
            else if (!DebuggerPathsAreURI)
            {
                path = path.Replace('/', '\\');
            }

            path = Utilities.NormalizeFileName(path, fixCasing: (Utilities.IsWindows() || Utilities.IsOSX()));

            if (DebuggerPathsAreURI)
            {
                if (ClientPathsAreURI)
                {
                    return path;
                }
                else
                {
                    Uri uri = new Uri(path);
                    return uri.LocalPath;
                }
            }
            else
            {
                if (ClientPathsAreURI)
                {
                    try
                    {
                        var uri = new System.Uri(path);
                        return uri.AbsoluteUri;
                    }
                    catch
                    {
                        return null;
                    }
                }
                else
                {
                    return path;
                }
            }
        }

        internal string ConvertClientPathToDebugger(string clientPath)
        {
            if (clientPath == null)
            {
                return null;
            }

            if (Path.DirectorySeparatorChar == '/')
            {
                clientPath = clientPath.Replace('\\', '/');
            }
            else if (!ClientPathsAreURI)
            {
                clientPath = clientPath.Replace('/', '\\');
            }

            if (DebuggerPathsAreURI)
            {
                if (ClientPathsAreURI)
                {
                    return clientPath;
                }
                else
                {
                    var uri = new System.Uri(clientPath);
                    return uri.AbsoluteUri;
                }
            }
            else
            {
                if (ClientPathsAreURI)
                {
                    if (Uri.IsWellFormedUriString(clientPath, UriKind.Absolute))
                    {
                        Uri uri = new Uri(clientPath);
                        return uri.LocalPath;
                    }
                    Console.Error.WriteLine("path not well formed: '{0}'", clientPath);
                    return null;
                }
                else
                {
                    return clientPath;
                }
            }
        }

        internal string ConvertLaunchPathForVsCode(string clientPath)
        {
            string path = ConvertClientPathToDebugger(clientPath);

            if (Path.DirectorySeparatorChar == '/')
            {
                // TODO/HACK: VSCode tries to take all paths and make them workspace relative. This works around this until
                // we have a VSCode side fix.
                int slashTiltaSlashIndex = path.LastIndexOf("/~/", StringComparison.Ordinal);
                if (slashTiltaSlashIndex >= 0)
                {
                    string homePath = Environment.GetEnvironmentVariable("HOME");
                    if (string.IsNullOrEmpty(homePath))
                        throw new Exception("Environment variable 'HOME' is not defined.");

                    path = Path.Combine(homePath, path.Substring(slashTiltaSlashIndex + 3));
                }
            }

            return path;
        }
    }
}
