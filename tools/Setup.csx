#!/usr/bin/env dotnet-script
// To run dotnet-scripts. Install dotnet at https://dotnet.microsoft.com/download and then run 'dotnet tool install -g dotnet-script'

/*
    This setup script is to install OpenDebugAD7/MIEngine bits for VS Code C++ Extension or for the VS CodeSpaces CMake Debug scenario.
*/

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections;

enum Client {
    None,
    VS,
    VSCode
};

enum Configuration {
    Debug,
    Release
};

static class Utils {
    public static string GetScriptFolder([CallerFilePath] string path = null) => Path.GetDirectoryName(path);
}

class Setup {
    Client Client { get; set;  }

    Configuration Configuration { get; set;  }

    string TargetPath { get; set; }

    public Setup()
    {
        Client = Client.None;
        Configuration = Configuration.Debug;
    }

    private void PrintHelp()
    {
        Console.WriteLine("USAGE: Setup.[sh|cmd] -- <Target-Path> [-vs|-vscode] [-debug|-release]");
        Console.WriteLine("");
        Console.WriteLine("\t<Target-Path> is the path to the VS Code C/C++ Extension or root path of Visual Studio.");
        Console.WriteLine("\tFor Example:");
        Console.WriteLine("\t\tC:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Preview");
        Console.WriteLine("\t\tC:\\Users\\<USERNAME>\\.vscode\\extensions\\ms-vscode.cpptools-1.0.0");
    }

    public void ParseArguments(IList<string> args)
    {
        if (args.Count == 0)
        {
            PrintHelp();
            System.Environment.Exit(1);
        }

        foreach (string arg in args)
        {
            if (arg.StartsWith("-"))
            {
                switch(arg.Substring(1).ToLower())
                {
                    case "help":
                    case "?":
                        PrintHelp();
                        System.Environment.Exit(1);
                        break;
                    case "vs":
                        Client = Client.VS;
                        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            throw new InvalidOperationException("Patching MIEngine with VS bits is not applicable for non-Windows Platforms.");
                        }
                        break;
                    case "vscode":
                        Client = Client.VSCode;
                        break;
                    case "debug":
                        Configuration = Configuration.Debug;
                        break;
                    case "release":
                        Configuration = Configuration.Release;
                        break;
                    default:
                        throw new ArgumentException(string.Format("Unknown flag '{0}'", arg));
                }
            }
            else
            {
                TargetPath = arg;
            }
        }
    }

    private class ListFileFormat
    {
        public string FileName { get; }
        public string SourceRoot { get; }
        public string SourceDir { get; }
        public string InstallDir { get; }

        public ListFileFormat(
            string fileName,
            string sourceRoot,
            string sourceDir,
            string installDir
        )
        {
            FileName = fileName;
            SourceRoot = sourceRoot;
            SourceDir = sourceDir;
            InstallDir = installDir;
        }
    }

    private IList<ListFileFormat> ParseListFiles(string filePath)
    {
        List<ListFileFormat> fileFormats = new List<ListFileFormat>();

        using (StreamReader fs = new StreamReader(filePath))
        {
            string line;

            while((line = fs.ReadLine()) != null)  
            {
                line = line.Trim();

                // Skip blank or commented out lines
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    continue;
                }

                string[] data = line.Split(',');
                if (data.Length < 4)
                {
                    Console.Error.WriteLine(string.Format("Invalid line in {0}: '{1}'", filePath, line));
                    continue;
                }

                ListFileFormat listFile = new ListFileFormat(
                    data[0],
                    data[1],
                    data[2],
                    data[3]
                );
                
                fileFormats.Add(listFile);
            }  
        }

        return fileFormats;
    }

    public void Run()
    {
        string scriptDirectoryPath = Utils.GetScriptFolder();
        string srcDirectoryPath = Path.GetFullPath(Path.Join(scriptDirectoryPath, "..", "src"));
        string binDirectoryPath = Path.GetFullPath(Path.Join(scriptDirectoryPath, "..", "bin"));

        // No client flag provided, try to auto detect.
        if (Client == Client.None)
        {
            string devenvPath = Path.Join(TargetPath, "Common7", "IDE", "devenv.exe");
            string packageJsonPath = Path.Join(TargetPath, "package.json");

            if (File.Exists(devenvPath))
            {
                Client = Client.VS;
                Console.WriteLine("Detected VS Client");
            }
            else if (File.Exists(packageJsonPath))
            {
                Client = Client.VSCode;
                Console.WriteLine("Detected VS Code Client");
            }
            else
            {
                throw new ArgumentNullException(nameof(Client));
            }
        }

        if (string.IsNullOrWhiteSpace(TargetPath))
        {
            if (Client == Client.VSCode)
            {
                string vscodeExtensionPath = string.Empty;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    vscodeExtensionPath = Environment.ExpandEnvironmentVariables("%USERPROFILE%\\.vscode\\extensions");
                }
                else
                {
                    vscodeExtensionPath = Path.Join(Environment.GetEnvironmentVariable("HOME"), ".vscode/extensions");
                }
                IEnumerable<string> extensions = Directory.EnumerateDirectories(vscodeExtensionPath);

                foreach (string extension in extensions)
                {
                    if (extension.Contains("ms-vscode.cpptools"))
                    {
                        TargetPath = extension;
                        break;
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(TargetPath))
        {
            throw new ArgumentNullException(nameof(TargetPath));
        }

        string listFilePath = string.Empty;

        if (Client == Client.VS)
        {
            listFilePath = Path.Join(scriptDirectoryPath, "VS.CodeSpaces.list");
            // Use <Configuration> folder.
            binDirectoryPath = Path.Join(binDirectoryPath, Configuration.ToString());
        }
        else if (Client == Client.VSCode)
        {
            listFilePath = Path.Join(scriptDirectoryPath, "VSCode.list");
            // Use Desktop.<Configuration> folder.
            binDirectoryPath = Path.Join(binDirectoryPath, "Desktop." + Configuration.ToString());
        }

        if (!Directory.Exists(binDirectoryPath))
        {
            string configurationToUse = Client == Client.VS ? Configuration.ToString() : "Desktop." + Configuration.ToString();
            throw new InvalidOperationException(string.Format("'{0}' does not exist. Did you build {1}?", binDirectoryPath, configurationToUse));
        }

        IList<ListFileFormat> lffList = this.ParseListFiles(listFilePath);

        foreach (ListFileFormat lff in lffList)
        {
            string srcPath = string.Empty;
            if (lff.SourceRoot == "src")
            {
                srcPath = Path.Join(srcDirectoryPath, lff.SourceDir, lff.FileName);
            }
            else
            {
                srcPath = Path.Join(binDirectoryPath, lff.SourceDir, lff.FileName);
            }
            string destPath = Path.Join(TargetPath, lff.InstallDir, lff.FileName);

            // Normalize Paths for OS
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                srcPath = srcPath.Replace('/', '\\');
                destPath = destPath.Replace('/', '\\');
            }
            else 
            {
                srcPath = srcPath.Replace('\\', '/');
                destPath = destPath.Replace('\\', '/');
            }

            Console.WriteLine(string.Format("Copying {0} to {1}.", srcPath, destPath));

            // TODO: Support symlinking
            File.Copy(srcPath, destPath, overwrite: true);
        }
    }

}

Setup setup = new Setup();
setup.ParseArguments(Args);
setup.Run();

System.Environment.Exit(0);