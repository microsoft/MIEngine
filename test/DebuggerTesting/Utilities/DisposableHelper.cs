// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DebuggerTesting.Utilities
{
    public static class DisposableHelper
    {
        /// <summary>
        /// Calls dispose on the object if it implements IDisposable.
        /// Ignores a null object.
        /// </summary>
        public static void SafeDispose(object o)
        {
            SafeDispose(o as IDisposable);
        }

        /// <summary>
        /// Calls dispose on the object.
        /// Ignores a null object.
        /// </summary>
        public static void SafeDispose(this IDisposable o)
        {
            o?.Dispose();
        }

        /// <summary>
        /// Calls dispose on all the objects in the collection that implement IDisposable.
        /// Ignores any null values.
        /// </summary>
        public static void SafeDisposeAll(IEnumerable objects)
        {
            SafeDisposeAll(objects?.OfType<IDisposable>());
        }

        /// <summary>
        /// Calls dispose on all the objects in the collection.
        /// Ignores any null values.
        /// </summary>
        public static void SafeDisposeAll(this IEnumerable<IDisposable> objects)
        {
            if (objects == null)
                return;

            foreach (IDisposable o in objects)
                SafeDispose(o);
        }
    }
}
