// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JDbg
{
    /// <summary>
    /// JDbg exposes debugging commands that can be used against a JVM at a higher level than JDWP.
    /// </summary>
    public class JDbg
    {
        private string _hostname;
        private int _port;
        private JdwpCommand.IDSizes _idSizes;
        private VersionCommand.Reply _version;
        private List<AllClassesWithGenericCommand.ClassData> _classes;

        private Jdwp _jdwp;

        private JDbg(string hostname, int port)
        {
            _hostname = hostname;
            _port = port;
        }

        /// <summary>
        /// Attach an instance of JDbg to a VM listening on hostname:port
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static JDbg Attach(string hostname, int port)
        {
            JDbg jdbg = new JDbg(hostname, port);
            jdbg._jdwp = Jdwp.Attach(hostname, port);
            return jdbg;
        }

        /// <summary>
        /// Attach an instance of JDbg to a VM listening on localhost:port
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static JDbg Attach(int port)
        {
            return Attach("localhost", port);
        }

        /// <summary>
        /// Runs multiple commands to initialize java debugging
        /// </summary>
        /// <returns></returns>
        public async Task Inititalize()
        {
            _idSizes = await IDSizes();
            _jdwp.SetIDSizes(_idSizes);
            _version = await Version();
            _classes = await AllClassesWithGeneric();
        }

        /// <summary>
        /// Get the sizes of various field from the JVM
        /// </summary>
        /// <returns></returns>
        private async Task<JdwpCommand.IDSizes> IDSizes()
        {
            IDSizesCommand command = new IDSizesCommand();

            await _jdwp.SendCommandAsync(command);

            var reply = command.GetReply();
            return reply;
        }

        /// <summary>
        /// Get the version string of the VM
        /// </summary>
        /// <returns></returns>
        private async Task<VersionCommand.Reply> Version()
        {
            VersionCommand command = new VersionCommand();

            await _jdwp.SendCommandAsync(command);

            var reply = command.GetReply();
            return reply;
        }

        /// <summary>
        /// Get the names of All classes in the JVM with Generics
        /// </summary>
        /// <returns></returns>
        private async Task<List<AllClassesWithGenericCommand.ClassData>> AllClassesWithGeneric()
        {
            AllClassesWithGenericCommand command = new AllClassesWithGenericCommand();

            await _jdwp.SendCommandAsync(command);

            return command.GetClassData();
        }

        /// <summary>
        /// Sends the dispose command to the VM, which is equivalent to detach.
        /// </summary>
        /// <returns></returns>
        private async Task Detach()
        {
            DisposeCommand command = new DisposeCommand();

            await _jdwp.SendCommandAsync(command);
        }

        public void Close()
        {
            ThreadPool.QueueUserWorkItem(async (state) =>
            {
                if (_jdwp != null)
                {
                    try
                    {
                        await Detach();
                    }
                    catch (Exception)
                    {
                        //If we hit exceptions here, catch and do nothing since we're already tearing down and there's nothing we can do anyways.
                    }

                    _jdwp.Close();
                }
            });
        }
    }
}
