using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsService1
{
    /// <summary>
    /// ChatRoom service by Tomás Cardenal López
    /// </summary>
    public partial class CRService : ServiceBase
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
        /// Timestamp for calculating how much time this service ran
        /// </summary>
        private DateTime onStartTimestamp;
        /// <summary>
        /// The port this service is on
        /// </summary>
        private int currentPort;
        /// <summary>
        /// Thread containing the server
        /// </summary>
        private Thread serverThread;

        /// <summary>
        /// Default constructor for this service
        /// </summary>
        public CRService()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Controls the actions on the start of this service
        /// </summary>
        /// <param name="args">The arguments for the startup</param>
        protected override void OnStart(string[] args)
        {
            this.onStartTimestamp = DateTime.Now;
            this.writeEvent("Chatroom service started, trying to load on config port");
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
                        if (int.TryParse(port, out this.currentPort)){
                            this.writeEvent("Port sucessfully loaded from cfg file: " + currentPort);
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                this.writeEvent("Config file not found, trying to load on default port");
                this.currentPort = DEFAULT_PORT;
            }

            if (this.currentPort != DEFAULT_PORT)
            {
                ChatServer server = new ChatServer(currentPort);
                if (server.BindServer())
                {
                    server.RunServer();
                }
                else
                {
                    this.writeEvent("Failed to connect on cfg port, trying default port");
                    this.currentPort = DEFAULT_PORT;
                }
            }
            if(this.currentPort == DEFAULT_PORT)
            {
                ChatServer server = new ChatServer(currentPort);
                if (server.BindServer())
                {
                    server.RunServer();
                }
                else
                {
                    this.writeEvent("Failed to connect on default port, ending service...");
                    this.Stop();
                }
            }


        }

        /// <summary>
        /// Controls the actions when this service stops
        /// </summary>
        protected override void OnStop()
        {
            TimeSpan elapsedTimestamp = DateTime.Now - onStartTimestamp;
            this.writeEvent("Service stopped, ran for " + elapsedTimestamp.TotalSeconds + "s");
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
