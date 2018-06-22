using Microsoft.VisualStudio.Debugger.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDebugAD7
{
    public class ThreadFrameEnumInfo
    {
        internal IEnumDebugFrameInfo2 FrameEnum { get; private set; }
        internal uint TotalFrames { get; private set; }
        internal uint CurrentPosition { get; set; }

        internal ThreadFrameEnumInfo(IEnumDebugFrameInfo2 frameEnum, uint totalFrames)
        {
            FrameEnum = frameEnum;
            TotalFrames = totalFrames;
            CurrentPosition = 0;
        }
    }
}
