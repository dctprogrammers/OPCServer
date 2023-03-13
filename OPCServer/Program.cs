using DCTProgram;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using static OPCServer.GlobalFunc;

namespace OPCServer
{
    internal static class Program
    {
        public static MemoryMappedFile writeMapper;
        public delegate void AfterRead();
        public static event AfterRead BeforeAction;
        private static Stopwatch stopwatch;
        public static MemoryMappedFile CommunicationMapper;
        public static MemoryMappedFile ConfigMapper;

        public static DataRegister[] dataRegisters;
        public static string MachineName { get; set; }
        public static ServerSystemContext SystemContext { get; set; }
        public static ApplicationInstance application;
        public static OPCServerStandart server;
        public static string ip4Address;
        public static Form1 form;
        /// <summary>
        /// Hlavní vstupní bod aplikace.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            MachineName = "Machine";
            try
            {
                CommunicationMapper = MemoryMappedFile.OpenExisting("communic");
                _ = Communication();
            }
            catch
            {

                //throw new Exception("no communic");
                End();
            }
            if (args.Length == 0)
                throw new Exception("failo no args");
            //Environment.Exit(0);
            foreach (string arg in args)
            {
                string[] s = arg.Split('~');
                if (s.Length == 4)
                    Add(new DataRegister(s[0], s[1], s[2], s[3]), ref dataRegisters);
                else
                    throw new Exception("failo addo regi");
               //Environment.Exit(0);
                
            }
            //Add(new DataRegister("CleanSet", "0", "0"), ref dataRegisters);

            writeMapper = MemoryMappedFile.CreateNew("DataWriter", 100);
            
            _ = OpcStart();
            _ = ReadingFromMemory();
            Application.Run(form = new Form1());
            End();
        }

        public static void End()
        {
            if(writeMapper!= null)
                writeMapper.Dispose();
            if(server != null)
                server.Dispose();
            if(form != null)
                form.Dispose();
            if (ConfigMapper != null)
                ConfigMapper.Dispose();
            Environment.Exit(0);

        }

        public static async Task GetIp(bool o)
        {
            while (true)
            {
                
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up)
                    {
                        await Task.Delay(2000);
                        continue;
                    }
                        
                    await Task.Delay(2000);
#if DEBUG
                ip4Address = "127.0.0.1";
                    
#else
                    if (ni.Name != "LAN 2")
                        continue;
                    var h = ni.GetIPProperties();
                    var k = ni.GetIPProperties().UnicastAddresses.FirstOrDefault(x => x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    
                    ip4Address = k.Address.ToString();
#endif
                    using (var config = ConfigMapper.CreateViewStream(0, 45))
                    {
                        byte[] bytes = new byte[40];
                        byte stringLenght = 0;
                        foreach (char c in ip4Address)
                        {
                            BitConverter.GetBytes(c).CopyTo(bytes, stringLenght++ * 2);
                        }
                        config.Write(bytes, 0, bytes.Length);
                    }
                    return;

                }
            }
            
        }

        public static async Task GetIp()
        {
            ConfigMapper = MemoryMappedFile.CreateNew("ConfigM", 50);
            while (true)
            {

                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
#if !DEBUG
                    if (ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet)
                        continue;
                    if (ni.GetIPProperties().UnicastAddresses.FirstOrDefault(x => x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString() == "192.168.0.21")
                        continue;
#endif
                    if (ni.OperationalStatus != OperationalStatus.Up)
                    {
                        await Task.Delay(2000);
                        continue;
                    }

                    await Task.Delay(2000);
#if DEBUG
                    ip4Address = "127.0.0.1";

#else
                    
                    var k = ni.GetIPProperties().UnicastAddresses.FirstOrDefault(x => x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    
                    ip4Address = k.Address.ToString();
#endif
                    using (var config = ConfigMapper.CreateViewStream(0, 0))
                    {
                        byte[] bytes = new byte[45];
                        byte stringLenght = 0;
                        foreach (char c in ip4Address)
                        {
                            BitConverter.GetBytes(c).CopyTo(bytes, stringLenght++ * 2);
                        }
                        config.Write(bytes, 0, bytes.Length);
                    }
                    return;

                }
            }

        }


        public static async Task ReadingFromMemory()
        {
            stopwatch = new Stopwatch();
            while (true)
            {
                stopwatch.Restart();
                BeforeAction?.Invoke();
                foreach(DataRegister data in dataRegisters)
                {
                    data.GetValue();
                }
                long delay = stopwatch.ElapsedMilliseconds;
                if(delay < 250)
                    await Task.Delay((int)(250 - delay));
            }
        }

        public static async Task Communication(bool o)
        {
            byte[] bufferToWrite = new byte[] { 1 };
            byte[] buf = new byte[50];
            Stopwatch watchdog = new Stopwatch();
            watchdog.Start();
            
            
            while (true)
            {
                try
                {
                    if (watchdog.ElapsedMilliseconds > 6000)
                        End();
                    byte[] buffer = new byte[1];

                    using (var stream = CommunicationMapper.CreateViewStream(0, 0))
                    {
                        stream.Read(buffer, 0, 1);
                        if (buffer[0] == 0)
                        {
                            stream.Write(bufferToWrite, 0, 1);
                            stream.Read(buf, 0, 49);
                            watchdog.Restart();
                        }
                    }



                    await Task.Delay(3000);
                }
                catch
                {
                    End();
                }
                
            }
            
              
        }

        public static async Task Communication()
        {
            byte oldchecking = 0;
            Stopwatch watchdog = new Stopwatch();
            watchdog.Start();
            
            
                while (true)
                {
                    using (var stream = CommunicationMapper.CreateViewStream(0, 0))
                    {
                        byte[] buffer = new byte[1];


                        stream.Read(buffer, 0, 1);
                        if (buffer[0] != oldchecking)
                        {
                            watchdog.Restart();
                            oldchecking = buffer[0];
                        }


                        if (watchdog.ElapsedMilliseconds > 7000)
                            Application.Exit();
                        //Environment.Exit(0);
                        await Task.Delay(3000);
                    }
                        
                }
            
               
        }

        public static async Task OpcStart()
        {
            await GetIp();
            application = new ApplicationInstance();

            application.ApplicationType = ApplicationType.Server;
            application.ConfigSectionName = "OPCServer";
            if (!Environment.UserInteractive)
                application.StartAsService(server = new OPCServerStandart());

            application.LoadApplicationConfiguration(System.AppDomain.CurrentDomain.BaseDirectory + "/OPCServer.Config.xml", false).Wait();
            StringCollection myIpAddress = new StringCollection
                {
                    "opc.tcp://" + IPAddress.Parse(ip4Address) + ":50001"//192.168.0.100
                    };
            application.ApplicationConfiguration.ServerConfiguration.BaseAddresses = myIpAddress;
            _ = application.CheckApplicationInstanceCertificate(false, 0);
            await application.Start(server = new OPCServerStandart());
        }

    }
}
