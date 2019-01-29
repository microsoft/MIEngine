using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Newtonsoft.Json;

namespace Microsoft.SSHDebugPS.Docker
{
    public interface IContainerInstance
    {
        string ContainerId { get; }
        string Name { get; }

    }

    public abstract class ContainerInstance<T> : IContainerInstance, IEquatable<T>
        where T : IContainerInstance
    {
        public abstract string ContainerId { get; }
        public abstract string Name { get; }

        public abstract bool Equals(T other);
    }

    public class DockerContainerInstance : ContainerInstance<DockerContainerInstance>
    {

        private string _name;
        private string _containerId;

        public override string ContainerId { get => _containerId; }
        public override string Name { get => _name; }

        public string Image { get; private set; }
        public string[] Ports { get; private set; }
        public string Command { get; private set; }
        public string Status { get; private set; }
        public string Created { get; private set; }

        public DockerContainerInstance(string json)
        {
            //do something with the json to poplate the current object
        }


        public override bool Equals(DockerContainerInstance other)
        {
            return String.Equals(ContainerId, other.ContainerId, StringComparison.OrdinalIgnoreCase) ? true :
                ContainerId.StartsWith(other.ContainerId, StringComparison.OrdinalIgnoreCase) ? true :
                other.ContainerId.StartsWith(ContainerId, StringComparison.OrdinalIgnoreCase) ? true : false;
        }
    }
}
