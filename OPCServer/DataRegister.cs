using Opc.Ua;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OPCServer.GlobalFunc;

namespace OPCServer
{
    internal class DataRegister
    {
        public static byte numbOfDataRegister = 0;
        private byte currentNumbOfDataRegister;
        public string Name { get; private set; }
        public DataTypeS DataType { get; private set; }
        UInt16 Lenght { get; set; }
        public bool IsWritable { get; private set; }
        public BaseDataVariableState VariableState { get; set; }
        public object sendValue;
        public byte DataLenght = 1;
        private MemoryMappedFile memoryMap;
        private byte[] buffer;
        public object Value { get; private set; }

        public DataRegister(string name, string dataType, string isWritable, string lenght)
        {
            DataLenght = Convert.ToByte(lenght);
            Name = name;
            if (isWritable == "0")
                IsWritable = false;
            else
                IsWritable = true;
            switch (dataType)
            {
                case "0":
                    DataType = DataTypeS.DTUInt;
                    Lenght = 2;
                    break;
                case "1":
                    DataType = DataTypeS.DTFloat;
                    Lenght = 8;
                    break;
                case "2":
                    DataType = DataTypeS.DTString;
                    Lenght = (ushort)(2 * DataLenght);
                    break;
                case "3":
                    DataType = DataTypeS.DTBool;
                    Lenght = 1;
                    break;
            }
            buffer = new byte[Lenght]; 
            memoryMap = MemoryMappedFile.OpenExisting(name);
            currentNumbOfDataRegister = numbOfDataRegister++;
        }

        public void GetValue()
        {
            try
            {
                using(MemoryMappedViewStream stream = memoryMap.CreateViewStream(0, Lenght))
                {
                    stream.Read(buffer, 0, Lenght);
                    switch (DataType)
                    {
                        case DataTypeS.DTUInt:
                            Value = BitConverter.ToUInt16(buffer, 0);
                            break;
                        case DataTypeS.DTFloat:
                            Value = BitConverter.ToDouble(buffer, 0);
                            break;
                        case DataTypeS.DTString:
                            string s = "";
                            for (int i = 0; i < Lenght/2; i++)
                                s += BitConverter.ToChar(buffer, i * 2);
                            Value = s;
                            break;
                        case DataTypeS.DTBool:
                            Value = BitConverter.ToBoolean(buffer, 0);
                            break;
                    }
                }
                NodeWrite();
            }
            catch
            {
                //Environment.Exit(0);
                throw new Exception("failoi n data get value");
            }
            
        }

        public void NodeWrite()
        {
            if (VariableState != null)
            {
                VariableState.Value = Value;
                VariableState.Timestamp = DateTime.UtcNow;
                VariableState.ClearChangeMasks(Program.SystemContext, false);
            }
        }

        public void SendWritableValue()
        {
            byte[] sendBuffer = new byte[Lenght+1];
            sendBuffer[0] = currentNumbOfDataRegister;
            using(MemoryMappedViewStream stream = Program.writeMapper.CreateViewStream(0, 0))
            {
                try
                {
                    switch (DataType)
                {
                    case DataTypeS.DTUInt:
                        BitConverter.GetBytes(Convert.ToUInt16(sendValue)).CopyTo(sendBuffer, 1);
                        break;
                    case DataTypeS.DTFloat:
                        BitConverter.GetBytes(Convert.ToDouble(sendValue)).CopyTo(sendBuffer, 1);
                        break;
                    case DataTypeS.DTString:
                        byte stringLenght = 0;
                        foreach (char c in Convert.ToString(sendValue))
                        {
                            BitConverter.GetBytes(c).CopyTo(sendBuffer, stringLenght++ * 2 + 1);
                        }
                        break;
                    case DataTypeS.DTBool:
                        BitConverter.GetBytes(Convert.ToBoolean(sendValue)).CopyTo(sendBuffer, 1);
                        break;
                }
                
                    stream.Write(sendBuffer, 0, sendBuffer.Length);
                }
                catch(Exception ex) { throw ex; }
                
            }
            Program.BeforeAction -= SendWritableValue;
        }

        public Opc.Ua.ServiceResult WriteData(
            ISystemContext context,
            NodeState node,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            ref object value,
            ref StatusCode statusCode,
            ref DateTime timestamp)
        {
            Program.BeforeAction += SendWritableValue;
            sendValue = value;
            return new Opc.Ua.ServiceResult(StatusCodes.Good);
        }
    }

    
}
