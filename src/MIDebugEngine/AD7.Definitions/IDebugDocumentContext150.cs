using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Debugger.Interop
{

    /// <summary>
    /// This interface was originally defined in Microsoft.VisualStudio.Debugger.Interop.15.0.dll
    /// We redefine it here because the deubgger interop assemblies are not portable,
    /// and we need this to be available in dev14 (when we do not have Interop.15.0.dll available)
    /// </summary>
    [Guid("3cfd5762-425b-4a0b-a962-3a6cacbcaef5")]
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDebugDocumentContext150
    {
        [PreserveSig]
        Int32 UseDefaultSourceSearchDirectories(out Int32 pfUseDefaultSearchDirectories);
    }
}
