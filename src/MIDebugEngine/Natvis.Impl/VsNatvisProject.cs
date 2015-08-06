// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Microsoft.MIDebugEngine.Natvis
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

    public class VsNatvisProject
    {
        public delegate void NatvisLoader(string path);

        public static void FindNatvisInSolution(NatvisLoader loader)
        {
            List<string> paths = new List<string>();
            try
            {
                ThreadHelper.Generic.Invoke(() => Internal.FindNatvisInSolutionImpl(paths));
            }
            catch (Exception)
            {
            }
            paths.ForEach((s) => loader(s));
        }


        private static class Internal
        {
            public static void FindNatvisInSolutionImpl(List<string> paths)
            {
                var solution = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));
                if (solution == null)
                {
                    return; // failed to find a solution
                }

                IEnumHierarchies enumProjects;
                int hr = solution.GetProjectEnum((uint)VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION | (uint)VSENUMPROJFLAGS.EPF_MATCHTYPE, new Guid("{8bc9ceb8-8b4a-11d0-8d11-00a0c91bc942}"), out enumProjects);
                if (hr != VSConstants.S_OK) return;

                IVsHierarchy[] proj = new IVsHierarchy[1];
                uint count;
                while (Constants.S_OK == enumProjects.Next(1, proj, out count))
                {
                    LoadNatvisFromProject(proj[0], paths, solutionLevel: false);
                }

                // Also, look for natvis files in top-level solution items
                hr = solution.GetProjectEnum((uint)VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION | (uint)VSENUMPROJFLAGS.EPF_MATCHTYPE, new Guid("{2150E333-8FDC-42A3-9474-1A3956D46DE8}"), out enumProjects);
                if (hr != VSConstants.S_OK) return;

                while (Constants.S_OK == enumProjects.Next(1, proj, out count))
                {
                    LoadNatvisFromProject(proj[0], paths, solutionLevel: true);
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
                    if (Constants.S_OK == proj.GetMkDocument(itemIds[i], out document))
                    {
                        paths.Add(document);
                    }
                }
            }

            private static bool GetNatvisFiles(bool solutionLevel, IVsProject4 proj, uint celt, uint[] rgitemids, out uint cActual)
            {
                if (solutionLevel)
                {
                    return Constants.S_OK == proj.GetFilesEndingWith(".natvis", celt, rgitemids, out cActual);
                }
                else
                {
                    return Constants.S_OK == proj.GetFilesWithItemType("natvis", celt, rgitemids, out cActual);
                }
            }
        }
    }
}
