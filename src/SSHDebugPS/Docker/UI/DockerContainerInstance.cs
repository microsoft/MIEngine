using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Newtonsoft.Json;

namespace Microsoft.SSHDebugPS.Docker
{
    public class DockerContainerInstance : IEquatable<DockerContainerInstance>
    {
        public DockerContainerInstance(string json) { }

        public string ContainerId { get; private set; }
        public string Name { get; private set; }
        public string Image { get; private set; }
        public string[] Ports { get; private set; }
        public string Command { get; private set; }
        public string Status { get; private set; }
        public string Created { get; private set; }

        bool IEquatable<DockerContainerInstance>.Equals(DockerContainerInstance other)
        {
            return String.Equals(ContainerId, other.ContainerId, StringComparison.OrdinalIgnoreCase) ? true :
                ContainerId.StartsWith(other.ContainerId, StringComparison.OrdinalIgnoreCase) ? true :
                other.ContainerId.StartsWith(ContainerId, StringComparison.OrdinalIgnoreCase) ? true : false;
        }
    }
}
