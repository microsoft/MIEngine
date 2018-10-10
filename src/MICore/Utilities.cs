using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MICore
{
    internal static class Utilities
    {
        private const string TempNamePrefix = "Microsoft-MIEngine-";
        private const string Separator = "-";
        internal static string GetMIEngineTemporaryFilename(string identifier = null)
        {
            // add the identifier + separator if the identifier exists
            string optionalIdentifier = string.IsNullOrEmpty(identifier) ? string.Empty : identifier + Separator;
            string filename = String.Concat(TempNamePrefix, optionalIdentifier, Path.GetRandomFileName());

            return filename;
        }
    }
}
