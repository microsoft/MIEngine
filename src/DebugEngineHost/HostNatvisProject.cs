// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Workspace;
using Microsoft.VisualStudio.Workspace.Indexing;
using Microsoft.VisualStudio.Workspace.VSIntegration.Contracts;
using Microsoft.Win32;

namespace Microsoft.DebugEngineHost
{
    internal class RegisterMonitorWrapper : IDisposable
    {
        public RegistryMonitor CurrentMonitor { get; set; }

        internal RegisterMonitorWrapper(RegistryMonitor currentMonitor)
        {
            CurrentMonitor = currentMonitor;
        }

        public void Dispose()
        {
            CurrentMonitor.Dispose();
            CurrentMonitor = null;
        }
    }

    /// <summary>
    /// Provides interactions with the host's source workspace to locate and load any natvis files
    /// in the project.
    /// </summary>
    public static class HostNatvisProject
    {
        public delegate void NatvisLoader(string path);

        /// <summary>
        /// Searches the solution and VSIXs for natvis files, invoking the loader on any which are found.
        /// </summary>
        /// <param name="loader">Natvis loader method to invoke</param>
        public static void FindNatvis(NatvisLoader loader)
        {
            List<string> paths = new List<string>();
            try
            {
                ThreadHelper.JoinableTaskFactory.Run(async () => {
                    await Internal.FindNatvisInSolutionImplAsync(paths);
                    Internal.FindNatvisInVSIXImpl(paths);
                });
            }
            catch (Exception)
            {
            }
            paths.ForEach((s) => loader(s));
        }

        public static IDisposable WatchNatvisOptionSetting(HostConfigurationStore configStore, ILogChannel natvisLogger)
        {
            RegisterMonitorWrapper rmw = null;

            HostConfigurationSection natvisDiagnosticSection = configStore.GetNatvisDiagnosticSection();
            if (natvisDiagnosticSection != null)
            {
                // DiagnosticSection exists, set current log level and watch for changes.
                SetNatvisLogLevel(natvisDiagnosticSection);

                rmw = new RegisterMonitorWrapper(CreateAndStartNatvisDiagnosticMonitor(natvisDiagnosticSection, natvisLogger));
            }
            else
            {
                // NatvisDiagnostic section has not been created, we need to watch for the creation.
                HostConfigurationSection debuggerSection = configStore.GetCurrentUserDebuggerSection();

                if (debuggerSection != null)
                {
                    // We only care about the debugger subkey's keys since we are waiting for the NatvisDiagnostics
                    // section to be created.
                    RegistryMonitor rm = new RegistryMonitor(debuggerSection, false, natvisLogger);

                    rmw = new RegisterMonitorWrapper(rm);

                    rm.RegChanged += (sender, e) =>
                    {
                        HostConfigurationSection checkForSection = configStore.GetNatvisDiagnosticSection();

                        if (checkForSection != null)
                        {
                            // NatvisDiagnostic section found. Update the logger
                            SetNatvisLogLevel(checkForSection);

                            // Remove debugger section tracking 
                            IDisposable disposable = rmw.CurrentMonitor;

                            // Watch NatvisDiagnostic section
                            rmw = new RegisterMonitorWrapper(CreateAndStartNatvisDiagnosticMonitor(checkForSection, natvisLogger));

                            disposable.Dispose();
                        }
                    };

                    rm.Start();
                }
            }


            return rmw;
        }

        private static RegistryMonitor CreateAndStartNatvisDiagnosticMonitor(HostConfigurationSection natvisDiagnosticSection, ILogChannel natvisLogger)
        {
            RegistryMonitor rm = new RegistryMonitor(natvisDiagnosticSection, true, natvisLogger);

            rm.RegChanged += (sender, e) =>
            {
                SetNatvisLogLevel(natvisDiagnosticSection);
            };

            rm.Start();

            return rm;
        }

        private static void SetNatvisLogLevel(HostConfigurationSection natvisDiagnosticSection)
        {
            string level = natvisDiagnosticSection.GetValue("Level") as string;
            if (level != null)
            {
                level = level.ToLower(CultureInfo.InvariantCulture);
            }
            LogLevel logLevel;
            switch (level)
            {
                case "off":
                    logLevel = LogLevel.None;
                    break;
                case "error":
                    logLevel = LogLevel.Error;
                    break;
                case "warning":
                    logLevel = LogLevel.Warning;
                    break;
                case "verbose":
                    logLevel = LogLevel.Verbose;
                    break;
                default: // Unknown, default to Warning
                    logLevel = LogLevel.Warning;
                    break;
            }

            if (logLevel == LogLevel.None)
            {
                HostLogger.DisableNatvisDiagnostics();
            }
            else
            {
                HostLogger.EnableNatvisDiagnostics((message) => {
                    string formattedMessage = string.Format(CultureInfo.InvariantCulture, "Natvis: {0}", message);
                    HostOutputWindow.WriteLaunchError(formattedMessage);
                }, logLevel);
                HostLogger.GetNatvisLogChannel().SetLogLevel(logLevel);
            }
        }

        public static string FindSolutionRoot()
        {
            string path = null;
            try
            {
                ThreadHelper.JoinableTaskFactory.Run(async () => 
                { 
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    path = Internal.FindSolutionRootImpl(); 
                });
            }
            catch (Exception)
            {
            }
            return path;
        }

        private static class Internal
        {
            // from vsshell.idl
            internal enum VSENUMPROJFLAGS
            {
                EPF_LOADEDINSOLUTION = 0x00000001, // normal projects referenced in the solution file and currently loaded
                EPF_UNLOADEDINSOLUTION = 0x00000002, // normal projects referenced in the solution file and currently NOT loaded
                EPF_ALLINSOLUTION = (EPF_LOADEDINSOLUTION | EPF_UNLOADEDINSOLUTION),
                //  all normal projects referenced in the solution file
                EPF_MATCHTYPE = 0x00000004, // projects with project type GUID matching parameter
                EPF_VIRTUALVISIBLEPROJECT = 0x00000008, // 'virtual' projects visible as top-level projects in Solution Explorer
                                                        //  (NOTE: these are projects that are not directly referenced in the solution file;
                                                        //  instead they are projects that are created programmatically via a non-standard UI.)
                EPF_VIRTUALNONVISIBLEPROJECT = 0x00000010, //   'virtual' projects NOT visible as top-level projects in Solution Explorer
                                                           //  (NOTE: these are projects that are not directly referenced in the solution file
                                                           //  and are usually displayed as nested (a.k.a. sub) projects in Solution Explorer)
                EPF_ALLVIRTUAL = (EPF_VIRTUALVISIBLEPROJECT | EPF_VIRTUALNONVISIBLEPROJECT),
                //  all 'virtual' projects of any kind
                EPF_ALLPROJECTS = (EPF_ALLINSOLUTION | EPF_ALLVIRTUAL),
                //  all projects including normal projects directly referenced in the solution
                //  file as well as all virtual projects including nested (a.k.a. sub) projects
            };

            /// <summary>
            /// Gets the WorkspaceService from Microsoft.VisualStudio.Workspace
            /// </summary>
            /// <remarks>This package won't be automatically loaded by MEF, so we need to manually acquire exported MEF Parts.</remarks>
            private static IVsFolderWorkspaceService GetWorkspaceService()
            {
                IComponentModel componentModel = ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel).GUID) as IComponentModel;
                if (componentModel != null)
                {
                    var workspaceServices = componentModel.DefaultExportProvider.GetExports<IVsFolderWorkspaceService>();

                    if (workspaceServices != null && workspaceServices.Any())
                    {
                        return workspaceServices.First().Value;
                    }
                }
                return null;
            }

            private async static Task<IEnumerable<string>> GetOpenFolderSourceLocationsAsync(string bstrFileName)
            {
                var workspaceService = GetWorkspaceService();
                IEnumerable<string> sourcesArray = new List<string>();

                if (workspaceService != null)
                {
                    IWorkspace currentWorkspace = workspaceService.CurrentWorkspace;
                    IIndexWorkspaceService indexWorkspaceService = currentWorkspace?.GetService<IIndexWorkspaceService>(throwIfNotFound: false);
                    if (indexWorkspaceService != null)
                    {
                        if (indexWorkspaceService.State != IndexWorkspaceState.Completed)
                        {
                            HostOutputWindow.WriteLaunchError(Resource.IndexIncomplete);
                        }

                        var findFilesService = indexWorkspaceService as IFindFilesService;
                        if (findFilesService != null)
                        {
                            FindFileServiceProgress progress = new FindFileServiceProgress();
                            string toSearch = Path.GetFileName(bstrFileName);
                            await findFilesService.FindFilesAsync(toSearch, progress);
                            if (progress.strings.Any())
                            {
                                sourcesArray = progress.strings;
                            }
                        }
                    }
                }
                return sourcesArray;
            }

            /// <summary>
            /// Custom IProgress class used for FindFileService.FindFilesAsync needed
            /// to force synchronous clalbacks. 
            /// </summary>
            private class FindFileServiceProgress : IProgress<string>
            {
                public List<string> strings { get; } = new List<string>();

                public void Report(string value)
                {
                    this.strings.Add(value);
                }
            }

            public async static System.Threading.Tasks.Task FindNatvisInSolutionImplAsync(List<string> paths)
            {
                var solution = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));
                if (solution == null)
                {
                    return; // failed to find a solution
                }

                object openFolderMode;
                solution.GetProperty((int)__VSPROPID7.VSPROPID_IsInOpenFolderMode, out openFolderMode);
                bool isOpenFolderActive = (bool)openFolderMode;

                if (isOpenFolderActive)
                {
                    IEnumerable<string> filenames;
                    filenames = await GetOpenFolderSourceLocationsAsync(".natvis");
                    paths.AddRange(filenames);
                    return;
                }

                IEnumHierarchies enumProjects;
                int hr = solution.GetProjectEnum((uint)VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION | (uint)VSENUMPROJFLAGS.EPF_MATCHTYPE, new Guid("{8bc9ceb8-8b4a-11d0-8d11-00a0c91bc942}"), out enumProjects);
                if (hr != VSConstants.S_OK) return;

                IVsHierarchy[] proj = new IVsHierarchy[1];
                uint count;
                while (VSConstants.S_OK == enumProjects.Next(1, proj, out count))
                {
                    LoadNatvisFromProject(proj[0], paths, solutionLevel: false);
                }

                // Also, look for natvis files in top-level solution items
                hr = solution.GetProjectEnum((uint)VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION | (uint)VSENUMPROJFLAGS.EPF_MATCHTYPE, new Guid("{2150E333-8FDC-42A3-9474-1A3956D46DE8}"), out enumProjects);
                if (hr != VSConstants.S_OK) return;

                while (VSConstants.S_OK == enumProjects.Next(1, proj, out count))
                {
                    LoadNatvisFromProject(proj[0], paths, solutionLevel: true);
                }
            }

            public static void FindNatvisInVSIXImpl(List<string> paths)
            {
                var extManager = (IVsExtensionManagerPrivate)Package.GetGlobalService(typeof(SVsExtensionManager));
                if (extManager == null)
                {
                    return; // failed to find the extension manager
                }

                BuildEnvironmentPath("NativeCrossPlatformVisualizer", extManager, paths);
            }

            public static string FindSolutionRootImpl()
            {
                string root = null;
                string slnFile;
                string slnUserFile;
                var solution = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));
                if (solution == null)
                {
                    return null; // failed to find a solution
                }
                solution.GetSolutionInfo(out root, out slnFile, out slnUserFile);
                return root;
            }

            private static void BuildEnvironmentPath(string name, IVsExtensionManagerPrivate pem, List<string> paths)
            {
                pem.GetEnabledExtensionContentLocations(name, 0, null, null, out var contentLocations);
                if (contentLocations > 0)
                {
                    var rgStrings = new string[contentLocations];
                    var rgbstrContentLocations = new string[contentLocations];
                    var rgbstrUniqueStrings = new string[contentLocations];

                    var hr = pem.GetEnabledExtensionContentLocations(name, contentLocations, rgbstrContentLocations, rgbstrUniqueStrings, out var actualContentLocations);
                    if (hr == VSConstants.S_OK && actualContentLocations > 0)
                    {
                        paths.AddRange(rgbstrContentLocations);
                    }
                }
            }

            private static void LoadNatvisFromProject(IVsHierarchy hier, List<string> paths, bool solutionLevel)
            {
                IVsProject4 proj = hier as IVsProject4;
                if (proj == null)
                {
                    return;
                }

                // Retrieve up to 10 natvis files in the first pass.  This avoids the need to iterate
                // through the file list a second time if we don't have more than 10 solution-level natvis files.
                uint cActual;
                uint[] itemIds = new uint[10];

                if (!GetNatvisFiles(solutionLevel, proj, 10, itemIds, out cActual))
                {
                    return;
                }

                // If the pre-allocated buffer of 10 natvis files was not enough, reallocate the buffer and repeat.
                if (cActual > 10)
                {
                    itemIds = new uint[cActual];
                    if (!GetNatvisFiles(solutionLevel, proj, cActual, itemIds, out cActual))
                    {
                        return;
                    }
                }

                // Now, obtain the full path to each of our natvis files and return it.
                for (uint i = 0; i < cActual; i++)
                {
                    string document;
                    if (VSConstants.S_OK == proj.GetMkDocument(itemIds[i], out document))
                    {
                        paths.Add(document);
                    }
                }
            }

            private static bool GetNatvisFiles(bool solutionLevel, IVsProject4 proj, uint celt, uint[] rgitemids, out uint cActual)
            {
                if (solutionLevel)
                {
                    return VSConstants.S_OK == proj.GetFilesEndingWith(".natvis", celt, rgitemids, out cActual);
                }
                else
                {
                    return VSConstants.S_OK == proj.GetFilesWithItemType("natvis", celt, rgitemids, out cActual);
                }
            }
        }
    }
}
