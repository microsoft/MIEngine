// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.DebugEngineHost;
using Microsoft.SSHDebugPS.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.SSHDebugPS.Docker
{
    public class ContainerInstance : IContainerInstance
    {
        /// <summary>
        /// Create a ContainerInstance from the results of docker ps in JSON format
        /// </summary>
        public static bool TryCreate(string json, out ContainerInstance instance)
        {
            instance = null;
            try
            {
                JObject obj = JObject.Parse(json);
                instance = obj.ToObject<ContainerInstance>();
            }
            catch (Exception e)
            {
                HostTelemetry.SendEvent(TelemetryHelper.Event_DockerPSParseFailure, new KeyValuePair<string, object>[] {
                    new KeyValuePair<string, object>(TelemetryHelper.Property_ExceptionName, e.GetType().Name)
                });

                string error = e.ToString();
                VsOutputWindowWrapper.WriteLine(StringResources.Error_DockerPSParseFailed.FormatCurrentCultureWithArgs(json, error), StringResources.Docker_PSName);
                Debug.Fail(error);
            }
            return instance != null;
        }

        protected ContainerInstance() { }

        #region JsonProperties

        [JsonProperty("ID")]
        public virtual string Id { get; set; }

        [JsonProperty("Names")]
        public virtual string Name { get; set; }

        [JsonProperty(nameof(Image))]
        public virtual string Image { get; protected set; }

        [JsonProperty(nameof(Ports))]
        public virtual string Ports { get; set; }

        [JsonProperty(nameof(Command))]
        public virtual string Command { get; protected set; }

        [JsonProperty(nameof(Status))]
        public virtual string Status { get; protected set; }

        [JsonProperty("CreatedAt")]
        public virtual string Created { get; protected set; }

        [JsonIgnore]
        public string Platform { get; set; }

        #endregion

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
            if (instance is ContainerInstance container)
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

        // Container names: only [a-zA-Z0-9][a-zA-Z0-9_.-] are allowed. It is also case sensitive
        protected virtual bool EqualsInternal(ContainerInstance instance)
        {
            if (GetType() != instance.GetType())
            {
                return false;
            }

            // the id can be a partial on a container
            return String.Equals(Id, instance.Id, StringComparison.Ordinal) ||
                Id.StartsWith(instance.Id, StringComparison.Ordinal) ||
                instance.Id.StartsWith(Id, StringComparison.Ordinal);
        }

        protected virtual int GetHashCodeInternal()
        {
            // Since IDs can be partial, we don't have a good way to get a good hash code.
            return string.IsNullOrWhiteSpace(Id) ? 0 : Id.Substring(0,1).GetHashCode();
        }

        #endregion
    }
}
