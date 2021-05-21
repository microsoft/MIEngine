// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DebuggerTesting.Utilities
{
    internal static class HashUtilities
    {
        #region Methods

        public static int CombineHashCodes(int h1, int h2)
        {
            return (h1 << 5) + h1 ^ h2;
        }

        internal static int CombineHashCodes(int h1, int h2, int h3)
        {
            return HashUtilities.CombineHashCodes(HashUtilities.CombineHashCodes(h1, h2), h3);
        }

        internal static int CombineHashCodes(int h1, int h2, int h3, int h4)
        {
            return HashUtilities.CombineHashCodes(HashUtilities.CombineHashCodes(h1, h2), HashUtilities.CombineHashCodes(h3, h4));
        }

        internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5)
        {
            return HashUtilities.CombineHashCodes(HashUtilities.CombineHashCodes(h1, h2, h3, h4), h5);
        }

        #endregion
    }
}