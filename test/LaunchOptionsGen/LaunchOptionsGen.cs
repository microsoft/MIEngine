using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LaunchOptionsGen
{
    class LaunchOptionsGen
    {
        static int Main(string[] args)
        {
            string templatePath;
            Dictionary<string, string> properties;
            if (!ValidateCommandLine(args, out templatePath, out properties))
            {
                Console.WriteLine("Syntax error: arguments to LaunchOptionsGen.exe are incorrect.");
                return -1;
            }

            if (!File.Exists(templatePath))
            {
                Console.WriteLine("Error: File {0} does not exist.");
                return -1;
            }

            string outputFile = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(templatePath)), "LaunchOptions.xml");

            string[] lines = File.ReadAllLines(templatePath);

            string pattern = @"\$(.*)\$";
            Regex regex = new Regex(pattern);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                Match m = regex.Match(line);
                if (m.Success)
                {
                    string key = m.Groups[1].Captures[0].Value;
                    string value;
                    if (properties.TryGetValue(key, out value))
                    {
                        line = line.Replace(m.Value, value);
                    }
                    else
                    {
                        Console.WriteLine("Error: LaunchOptions template file contains properties that were not specified on the command line to LaunchOptionsGen.exe");
                        return -1;
                    }
                }
                lines[i] = line;
            }
            File.WriteAllLines(outputFile, lines);

            return 0;
        }

        private static bool ValidateCommandLine(string[] args, out string templatePath, out Dictionary<string, string> properties)
        {
            templatePath = null;
            properties = new Dictionary<string, string>();
            if (args.Length < 1)
            {
                return false;
            }

            templatePath = args[0];

            if (templatePath.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            {
                return false;
            }

            if (args.Length > 1)
            {
                for (int i = 1; i < args.Length; i++)
                {
                    string arg = args[i];
                    string[] pair = arg.Split('=');
                    if (pair.Length != 2)
                    {
                        return false;
                    }
                    string key = pair[0];
                    string value = pair[1];
                    value.Replace("\"", ""); //strip single quotes in the case of paths
                    properties.Add(pair[0].Trim(), pair[1].Trim());
                }
            }

            return true;
        }

    }
}
