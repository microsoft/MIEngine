// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DebuggerTesting
{
    /// <summary>
    /// Base class for disposable objects.
    /// </summary>
    public abstract class DisposableObject : IDisposable
    {
        #region Constructor

        protected DisposableObject()
        {
        }

        #endregion

        #region Destructor

        ~DisposableObject()
        {
            if (!this.IsDisposed)
            {
                this.Dispose(isDisposing: false);
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            UDebug.Assert(!this.IsDisposed, "This was already disposed");
            if (!this.IsDisposed)
            {
                this.Dispose(isDisposing: true);
                GC.SuppressFinalize(this);
            }
        }

        #endregion

        #region Methods

        protected virtual void Dispose(bool isDisposing)
        {
            this.IsDisposed = true;
        }

        /// <summary>
        /// Throws an exception if the object is disposed.
        /// </summary>
        protected void VerifyNotDisposed()
        {
            if (this.IsDisposed)
                throw new ObjectDisposedException(this.GetType().FullName);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether the object is disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        #endregion
    }
}