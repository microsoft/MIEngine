// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Debugger.Interop;

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
