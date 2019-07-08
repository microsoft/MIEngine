// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public string ShortId { get => Id.Substring(0, 10); }

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

        public override bool GetResult(out string selectedQualifier)
        {
            selectedQualifier = Name;
            return true;
        }

        public override event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string FormattedListOfPorts
        {
            get
            {
                return string.IsNullOrWhiteSpace(Ports) ?
                    UIResources.NoPortsText :
                    Ports.Replace(", ", "\r\n");
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        protected override bool EqualsInternal(ContainerInstance instance)
        {
            if (instance is DockerContainerInstance other)
            {
                // the id can be a partial on a container
                return String.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase) ? true :
                    Id.StartsWith(other.Id, StringComparison.OrdinalIgnoreCase) ? true :
                    other.Id.StartsWith(Id, StringComparison.OrdinalIgnoreCase) ? true : false;
            }

            return false;
        }

        protected override int GetHashCodeInternal()
        {
            // Since IDs can be partial, we don't have a good way to get a good hash code.
            return string.IsNullOrWhiteSpace(Id) ? 0 : Id.Substring(0,1).ToLowerInvariant().GetHashCode();
        }
    }
}
