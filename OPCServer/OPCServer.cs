using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc.Ua.Server;
using Opc.Ua;

namespace DCTProgram
{
    internal class OPCServerStandart : StandardServer
    {
        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            List<INodeManager> nodes = new List<INodeManager>();
            nodes.Add(new OPCNodeManager(server, configuration));

            return new MasterNodeManager(server, configuration, null, nodes.ToArray());
        }

        protected override RequestManager CreateRequestManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            return base.CreateRequestManager(server, configuration);
        }

        protected override ServerProperties LoadServerProperties()
        {
            ServerProperties properties = new ServerProperties();

            properties.ManufacturerName = "DCT s.r.o.";
            properties.ProductName = "OPC UA Server";
            properties.ProductUri = "https://github.com/dctprogrammers/OPCServer";
            properties.SoftwareVersion = Utils.GetAssemblySoftwareVersion();
            properties.BuildNumber = Utils.GetAssemblyBuildNumber();
            properties.BuildDate = Utils.GetAssemblyTimestamp();

            return properties;
        }
    }
}
