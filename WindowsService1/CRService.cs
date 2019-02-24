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
        /// Timestamp for calculating how much time this service ran
        /// </summary>
        private DateTime onStartTimestamp;
        /// <summary>
        /// Thread containing the server
        /// </summary>
        private Thread serverThread;
        /// <summary>
        /// The server
        /// </summary>
        ChatServer server;
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
            server = new ChatServer(this);
            server.writeEvent(Properties.strings.SVC_START);
            serverThread = new Thread(server.RunServer);
            serverThread.Start();
        }

        /// <summary>
        /// Controls the actions when this service stops
        /// </summary>
        protected override void OnStop()
        {
            TimeSpan elapsedTimestamp = DateTime.Now - onStartTimestamp;
            server.writeEvent(string.Format(Properties.strings.SVC_STOP, elapsedTimestamp.TotalSeconds));
            serverThread.Abort();
        }

        
    }
}
