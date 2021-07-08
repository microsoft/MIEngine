// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.IO;

// This code is based on https://github.com/Microsoft/vscode-mono-debug/blob/master/src/common/PathUtilities.cs

namespace OpenDebug
{
    public class PathUtilities
    {
        public static string NormalizePath(string path)
        {
            return new Uri(path).LocalPath;
            //return Path.GetFullPath(new Uri(path).LocalPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static string CombineAndNormalize(string path1, string path2)
        {
            var p = Path.Combine(path1, path2);
            return PathUtilities.NormalizePath(p);
        }

        /**
         * Make path relative to target.
         * Finds common prefix of both paths and then converts path into a path that is relative to the prefix.
         * If there is no common prefix, null is returned.
         */
        public static string MakeRelative(string target, string path)
        {
            var t = target.Split(Path.DirectorySeparatorChar);
            var p = path.Split(Path.DirectorySeparatorChar);

            var i = 0;
            for (; i < Math.Min(t.Length, p.Length) && t[i] == p[i]; i++)
            {
            }

            var result = "";
            for (; i < p.Length; i++)
            {
                result = Path.Combine(result, p[i]);
            }
            return result;
        }

        public static string RemoveFirstSegment(string path)
        {
            if (path[0] == Path.DirectorySeparatorChar)
            {
                path = path.Substring(1);
            }
#if NET462
            int pos = path.IndexOf(Path.DirectorySeparatorChar);
#else
            int pos = path.IndexOf(Path.DirectorySeparatorChar, StringComparison.Ordinal);
#endif
            if (pos >= 0)
            {
                path = path.Substring(pos + 1);
            }
            else
            {
                return null;
            }
            if (path.Length > 0)
            {
                return path;
            }
            return null;
        }

        public static string MakePathAbsolute(string path, string path2)
        {
            if (path != null && !Path.IsPathRooted(path))
            {
                var dir = Path.GetDirectoryName(path2);
                path = Path.Combine(dir, path);
                path = new Uri(path).LocalPath;     // normalize
            }
            return path;
        }

        public static string MakeRelativePath(string dir_path, string path)
        {
            /*
            if (!dir_path.EndsWith(""+Path.DirectorySeparatorChar)) {
                dir_path += Path.DirectorySeparatorChar;
            }
            */
            Uri uri1 = new Uri(path);
            Uri uri2 = new Uri(dir_path);
            return uri2.MakeRelativeUri(uri1).ToString();
        }
    }
}

