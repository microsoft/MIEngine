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
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SSHDebugPS.Docker
{
    public interface IContainerInstance
    {
        string Id { get; }
        string Name { get; }
        bool GetResult(out string selectedQualifier);
    }

    public abstract class ContainerInstance<T> : IContainerInstance, IEquatable<T>
        where T : IContainerInstance
    {
        public abstract string Id { get; set; }
        public abstract string Name { get; set; }

        public abstract bool Equals(T other);
        public abstract bool GetResult(out string selectedQualifier);
    }

    public class DockerContainerInstance : ContainerInstance<DockerContainerInstance>
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

        public DockerContainerInstance() { }

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

        public override bool Equals(DockerContainerInstance other)
        {
            // the id can be a partial on a container
            return String.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase) ? true :
                Id.StartsWith(other.Id, StringComparison.OrdinalIgnoreCase) ? true :
                other.Id.StartsWith(Id, StringComparison.OrdinalIgnoreCase) ? true : false;
        }

        public override bool GetResult(out string selectedQualifier)
        {
            selectedQualifier = Name;
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

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
    }
}
