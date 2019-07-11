// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SSHDebugPS.Docker
{
    public interface IContainerInstance : IEquatable<IContainerInstance>
    {
        string Id { get; }
        string Name { get; }
    }

    public abstract class ContainerInstance : IContainerInstance
    {
        public abstract string Id { get; set; }
        public abstract string Name { get; set; }

        #region IEquatable

        public static bool operator ==(ContainerInstance left, ContainerInstance right)
        {
            if (left is null || right is null)
            {
                return ReferenceEquals(left, right);
            }

            return left.Equals(right);
        }

        public static bool operator !=(ContainerInstance left, ContainerInstance right)
        {
            return !(left == right);
        }

        public bool Equals(IContainerInstance instance)
        {
            if (!ReferenceEquals(null, instance) && instance is ContainerInstance container)
            {
                return this.EqualsInternal(container);
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is IContainerInstance instance)
            {
                return this.Equals(instance);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return GetHashCodeInternal();
        }

        #endregion

        #region Helper Methods

        protected abstract bool EqualsInternal(ContainerInstance instance);
        protected abstract int GetHashCodeInternal();

        #endregion
    }
}
