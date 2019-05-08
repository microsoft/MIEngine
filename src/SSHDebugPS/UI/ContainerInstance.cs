using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public DockerContainerInstance() { }

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

        public static List<DockerContainerInstance> GetMockInstances()
        {
            List<DockerContainerInstance> mock = new List<DockerContainerInstance>();
            //        CONTAINER ID        IMAGE COMMAND                   CREATED STATUS              PORTS NAMES
            //3e2e785dfe0d localhost:5000 / samplemodule:0.0.1 - amd64.debug                     "dotnet SampleModule…"    5 months ago        Up 20 hours target
            //ad13fa21534d mcr.microsoft.com / azureiotedge - simulated - temperature - sensor:1.0   "/bin/sh -c 'echo \"$…"   5 months ago        Up 20 hours input
            //93c25069c23b mcr.microsoft.com / azureiotedge - hub:1.0                            "/bin/sh -c 'echo \"$…"   5 months ago        Up 20 hours         0.0.0.0:443->443 / tcp, 0.0.0.0:5671->5671 / tcp, 0.0.0.0:8883->8883 / tcp   edgeHubDev
            //302d1603b595 registry:2                                                        "/entrypoint.sh /etc…"    6 months ago        Up 20 hours         0.0.0.0:5000->5000 / tcp                                                 registry
            mock.Add(new DockerContainerInstance()
            {
                _containerId = "3e2e785dfe0d",
                _name = "target",
                Image = "localhost:5000 / samplemodule:0.0.1 - amd64.debug ",
                Command = "dotnet SampleModule…",
                Status = "Up 20 hours",
                Created = "Up 20 hours"
            });
            mock.Add(new DockerContainerInstance()
            {
                _containerId = "ad13fa21534d",
                _name = "input",
                Image = "mcr.microsoft.com / azureiotedge - simulated - temperature - sensor:1.0",
                Command = "/bin/sh -c 'echo \"$…",
                Status = "Up 20 hours",
                Created = "5 months ago"
            });
            mock.Add(new DockerContainerInstance()
            {
                _containerId = "93c25069c23b",
                _name = "edgeHubDev",
                Image = "mcr.microsoft.com / azureiotedge - hub:1.0",
                Command = "/bin/sh -c 'echo \"$…",
                Status = "Up 20 hours",
                Ports = new string[3] { "0.0.0.0:443->443 / tcp", "0.0.0.0:5671->5671 / tcp", "0.0.0.0:8883->8883 / tcp" },
                Created = "5 months ago"
            });
            mock.Add(new DockerContainerInstance()
            {
                _containerId = "302d1603b595",
                _name = "registry",
                Image = "registry:2",
                Command = "/entrypoint.sh /etc…",
                Status = "Up 20 hours",
                Ports = new string[1] { "0.0.0.0:5000->5000 / tcp" },
                Created = "6 months ago"
            });
            mock.Add(new DockerContainerInstance()
            {
                _containerId = "ad13fa21534d",
                _name = "adfasdfadgafgafdsgasdgasdgasdgadsgaffffffffffffffffffffffffffffffffffffffffffffffffffsdg",
                Image = "mcr.microsoft.com / azureiotedge - simulated - temperature - sensor:1.0",
                Command = "/bin/sh -c 'echo \"$…",
                Status = "Up 20 hours",
                Created = "5 months ago"
            });
            mock.Add(new DockerContainerInstance()
            {
                _containerId = "93c25069c23b",
                _name = "asdfasdgasdgadsgasdgasdgasdgasdgasdgasdgasdgasdgasg",
                Image = "mcr.microsoft.com / azureiotedge - hub:1.0",
                Command = "/bin/sh -c 'echo \"$…",
                Status = "Up 20 hours",
                Ports = new string[3] { "0.0.0.0:443->443 / tcp", "0.0.0.0:5671->5671 / tcp", "0.0.0.0:8883->8883 / tcp" },
                Created = "5 months ago"
            });
            mock.Add(new DockerContainerInstance()
            {
                _containerId = "302d1603b595",
                _name = "target4",
                Image = "registry:2",
                Command = "/entrypoint.sh /etc…",
                Status = "Up 20 hours",
                Ports = new string[1] { "0.0.0.0:5000->5000 / tcp" },
                Created = "6 months ago"
            });

            return mock;
        }

        // TODO: Change to give correct port formatted string
        public bool GetResult(out string selectedQualifier)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(ContainerId);

            selectedQualifier = sb.ToString();

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
                StringBuilder list = new StringBuilder();

                if (Ports != null && Ports.Length > 0)
                {
                    for (int i = 0; i < Ports.Length; i++)
                    {
                        if (i == Ports.Length - 1)
                        {
                            list.Append(Ports[i]);
                        }
                        else
                        {
                            list.Append(Ports[i] + ", ");
                        }
                    }
                }
                else
                {
                    list.Append(UIResources.NoPortsText);
                }

                return list.ToString();
            }
        }

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
                    OnPropertyChanged("IsSelected");
                    OnPropertyChanged("PortsAreVisible");
                }
            }
        }

        private bool _isSelected;
    }
}
