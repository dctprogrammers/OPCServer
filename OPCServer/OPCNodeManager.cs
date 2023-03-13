using Opc.Ua;
using Opc.Ua.Server;
using OPCServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCTProgram
{
    internal class OPCNodeManager : CustomNodeManager2
    {
        string[] namespaceUrls;
        private OPCServerConfiguration serverConfiguration;
        public List<BaseDataVariableState> variables;
        BaseDataVariableState[] baseDataVariableStates;
        public OPCNodeManager(IServerInternal server, ApplicationConfiguration configuration)
            : base(server, configuration)
        {
            SystemContext.NodeIdFactory = this;

            namespaceUrls = new string[2];
            namespaceUrls[0] = "http://www.opcfoundation.org/Server/";
            namespaceUrls[1] = "http://www.opcfoundation.org/Server//Instance";
            SetNamespaces(namespaceUrls);

            serverConfiguration = configuration.ParseExtension<OPCServerConfiguration>();
            variables = new List<BaseDataVariableState>();
            if (serverConfiguration != null)
            {
                serverConfiguration = new OPCServerConfiguration();
            }
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                IList<IReference> references = null;

                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }
                Program.SystemContext = SystemContext;

                FolderState root = CreateFolder(null, Program.MachineName, Program.MachineName);
                root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, root.NodeId));
                root.EventNotifier = EventNotifiers.SubscribeToEvents;
                AddRootNotifier(root);

                List<BaseDataVariableState> variables = new List<BaseDataVariableState>();

                try
                {
                    foreach (DataRegister reg in Program.dataRegisters)
                    {
                        BaseDataVariableState baseData;
                        NodeId nodeId;
                        switch (reg.DataType)
                        {
                            case GlobalFunc.DataTypeS.DTUInt:
                                nodeId = DataTypeIds.Int16;
                                break;
                            case GlobalFunc.DataTypeS.DTFloat:
                                nodeId = DataTypeIds.Float;
                                break;
                            case GlobalFunc.DataTypeS.DTString:
                                nodeId = DataTypeIds.String;
                                break;
                            case GlobalFunc.DataTypeS.DTBool:
                                nodeId = DataTypeIds.Boolean;
                                break;
                            default:
                                nodeId = DataTypeIds.Int16;
                                break;
                        }
                        baseData = CreateVariable(root, "/" + reg.Name, reg.Name, nodeId, ValueRanks.Scalar);

                        if (reg.IsWritable)
                            baseData.OnWriteValue += reg.WriteData;
                        else
                            baseData.OnWriteValue += NotWriteble;

                        reg.VariableState = baseData;
                        GlobalFunc.Add<BaseDataVariableState>(baseData, ref baseDataVariableStates);
                    }
                }
                catch (Exception e)
                {
                    Utils.Trace(e, "Error creating the address space.");
                }
                AddPredefinedNode(SystemContext, root);
            }
        }
        public Opc.Ua.ServiceResult NotWriteble(
            ISystemContext context,
            NodeState node,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            ref object value,
            ref StatusCode statusCode,
            ref DateTime timestamp)
        {
            return new Opc.Ua.ServiceResult(StatusCodes.Bad);
        }

        private FolderState CreateFolder(NodeState parent, string browse, string name)
        {
            FolderState folder = new FolderState(parent);

            folder.SymbolicName = name;
            folder.ReferenceTypeId = ReferenceTypes.Organizes;
            folder.TypeDefinitionId = ObjectTypeIds.FolderType;
            folder.NodeId = new NodeId(browse, NamespaceIndex);
            folder.BrowseName = new QualifiedName(browse, NamespaceIndex);
            folder.DisplayName = new LocalizedText("en", name);
            folder.WriteMask = AttributeWriteMask.None;
            folder.UserWriteMask = AttributeWriteMask.None;
            folder.EventNotifier = EventNotifiers.None;
            if (parent != null) parent.AddChild(folder);
            return folder;
        }

        private BaseDataVariableState CreateVariable(NodeState parent, string browse, string name, NodeId dataType, int valueRank)
        {
            BaseDataVariableState variable = new BaseDataVariableState(parent);
            variable.SymbolicName = name;
            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.TypeDefinitionId = VariableTypeIds.BaseDataVariableType;
            variable.NodeId = new NodeId(browse, NamespaceIndex);
            variable.BrowseName = new QualifiedName(browse, NamespaceIndex);
            variable.DisplayName = new LocalizedText("en", name);
            variable.WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            variable.UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            variable.DataType = dataType;
            variable.ValueRank = valueRank;
            variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = true;
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            switch (dataType.Identifier)
            {
                case 1: variable.Value = false; break;
                case 6: variable.Value = 0; break;
                case 10: variable.Value = 0.0f; break;
                case 12: variable.Value = ""; break;


            }

            if (valueRank == ValueRanks.OneDimension)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0 });
            }
            else if (valueRank == ValueRanks.TwoDimensions)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0, 0 });
            }

            if (parent != null)
            {
                parent.AddChild(variable);
            }
            variables.Add(variable);
            return variable;
        }
    }
}
