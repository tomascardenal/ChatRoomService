using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsService1
{
    /// <summary>
    /// Represents a multithreaded chat server
    /// </summary>
    class ChatServer
    {
        /// <summary>
        /// The default port
        /// </summary>
        public const int DEFAULT_PORT = 1337;
        /// <summary>
        /// Path to the configuration
        /// </summary>
        private string configPath;
        /// <summary>
        /// The server endpoint
        /// </summary>
        private IPEndPoint serverIE;
        /// <summary>
        /// The server socket
        /// </summary>
        private Socket serverSocket;
        /// <summary>
        /// The server port
        /// </summary>
        private int serverPort;
        /// <summary>
        /// Connected user list
        /// </summary>
        public Dictionary<string, Socket> ClientList;
        /// <summary>
        /// Lock for multithreading
        /// </summary>
        public object Locker = new object();
        /// <summary>
        /// The service controlling this server
        /// </summary>
        public CRService Service;
        /// <summary>
        /// Controls when to run the server
        /// </summary>
        public bool Run = true;

        /// <summary>
        /// Initalizes a chatserver on the indicated port
        /// </summary>
        /// <param name="port">Port to start the server</param>
        /// <param name="service">Callback to the service</param>
        public ChatServer(CRService service)
        {
            ClientList = new Dictionary<string, Socket>();
            this.Service = service;
        }

        public bool BindServer()
        {
            writeEvent(string.Format(Properties.strings.SRV_WELCOME, this.serverPort));
            serverIE = new IPEndPoint(IPAddress.Any, this.serverPort);
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                serverSocket.Bind(serverIE);
            }
            catch (SocketException e)
            {
                writeEvent(string.Format(Properties.strings.SRV_SOCKETEXCEPTION, e.Message));
                return false;
            }
            return true;
        }

        /// <summary>
        /// Runs the server and starts waiting for clients
        /// </summary>
        public void RunServer()
        {
            this.configPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);
            this.configPath = Path.Combine(this.configPath, "config.cfg");
            try
            {
                using (StreamReader reader = new StreamReader(configPath))
                {
                    string cfg = reader.ReadLine();
                    string port;
                    if (cfg.Contains("Port="))
                    {
                        port = cfg.Substring(5);
                        if (int.TryParse(port, out this.serverPort))
                        {
                            this.writeEvent(string.Format(Properties.strings.SVC_PORTFILE_OK, this.serverPort));
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                this.writeEvent(Properties.strings.SVC_FILE_NOT_FOUND);
                this.serverPort = DEFAULT_PORT;
            }

            if (this.serverPort != DEFAULT_PORT)
            {
                if (this.BindServer())
                {
                    this.writeEvent(Properties.strings.SVC_SERVER_ENDED);
                }
                else
                {
                    this.writeEvent(Properties.strings.SVC_FAILED_PORT_CFG);
                    this.serverPort = DEFAULT_PORT;
                }
            }
            if (this.serverPort == DEFAULT_PORT)
            {
                if (this.BindServer())
                {
                    this.writeEvent(Properties.strings.SVC_SERVER_ENDED);
                }
                else
                {
                    this.writeEvent(Properties.strings.SVC_FAILED_PORT_DEFAULT);
                    this.Service.Stop();
                }
            }
            try
            {
                serverSocket.Listen(10);
                //Wait for clients, throw a new thread with the client
                while (Run)
                {
                    Socket client = serverSocket.Accept();
                    ChatRoomServiceManager chatClient = new ChatRoomServiceManager(client, this);
                    Thread clientThread = new Thread(chatClient.run);
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
            }
            catch (SocketException e)
            {
                writeEvent(string.Format(Properties.strings.SRV_SOCKETEXCEPTION, e.Message));
                this.Service.Stop();
            }
        }

        /// <summary>
        /// Writes an event on the Event Viewer
        /// </summary>
        /// <param name="msg">The message for the event</param>
        public void writeEvent(string msg)
        {
            string source = "ChatRoomServiceTCL";
            string logName = "Application";
            if (!EventLog.SourceExists(source))
            {
                EventLog.CreateEventSource(source, logName);
            }
            EventLog.WriteEntry(source, msg);
        }
    }
}