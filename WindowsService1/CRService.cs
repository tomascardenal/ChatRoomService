using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace WindowsService1
{
    /// <summary>
    /// ChatRoom service by Tomás Cardenal López
    /// </summary>
    public partial class CRService : ServiceBase
    {
        /// <summary>
        /// Default constructor
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
        }

        /// <summary>
        /// Controls the actions when this service stops
        /// </summary>
        protected override void OnStop()
        {
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
