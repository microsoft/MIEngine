// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.SSHDebugPS.Docker
{
    public class DockerContainerInstance : ContainerInstance
    {
        /// <summary>
        /// Create a DockerContainerInstance from the results of docker ps in JSON format
        /// </summary>
        public static DockerContainerInstance Create(string json)
        {
            try
            {
                JObject obj = JObject.Parse(json);
                var instance = obj.ToObject<DockerContainerInstance>();
                if (instance != null)
                    return instance;
            }
            catch (Exception e)
            {
                Debug.Fail(e.ToString());
            }
            return null;
        }

        private DockerContainerInstance() { }

        #region JsonProperties

        [JsonProperty("ID")]
        public override string Id { get; set; }

        [JsonProperty("Names")]
        public override string Name { get; set; }

        [JsonProperty("Image")]
        public string Image { get; private set; }

        [JsonProperty("Ports")]
        public string Ports { get; set; }

        [JsonProperty("Command")]
        public string Command { get; private set; }

        [JsonProperty("Status")]
        public string Status { get; private set; }

        [JsonProperty("CreatedAt")]
        public string Created { get; private set; }

        #endregion

        // Docker container names: only [a-zA-Z0-9][a-zA-Z0-9_.-] are allowed. It is also case sensitive
        protected override bool EqualsInternal(ContainerInstance instance)
        {
            if (instance is DockerContainerInstance other)
            {
                // the id can be a partial on a container
                return String.Equals(Id, other.Id, StringComparison.Ordinal) ||
                    Id.StartsWith(other.Id, StringComparison.Ordinal) ||
                    other.Id.StartsWith(Id, StringComparison.Ordinal);
            }

            return false;
        }

        protected override int GetHashCodeInternal()
        {
            // Since IDs can be partial, we don't have a good way to get a good hash code.
            return string.IsNullOrWhiteSpace(Id) ? 0 : Id.Substring(0,1).GetHashCode();
        }
    }
}
