using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WindowsService1
{
    /// <summary>
    /// Contains the necessary actions for managing a client connected to the chatroom
    /// </summary>
    class ChatRoomServiceManager
    {
        /// <summary>
        /// Thread controlling flag
        /// </summary>
        public bool RunChat = true;
        /// <summary>
        /// Socket connected to our current client
        /// </summary>
        private Socket clientSocket;
        /// <summary>
        /// The client's userName
        /// </summary>
        private string userName;
        /// <summary>
        /// Our server socket reference
        /// </summary>
        private ChatServer server;

        /// <summary>
        /// Initializes a manager for the clientsocket indicated on it's parameters on the referenced server
        /// </summary>
        /// <param name="socket">The client socket</param>
        /// <param name="server">The server instance (this)</param>
        public ChatRoomServiceManager(Socket socket, ChatServer server)
        {
            this.clientSocket = socket;
            this.server = server;
        }

        /// <summary>
        /// Controls the actions of the chatroom for each client
        /// </summary>
        public void run()
        {
            //Get the EndPoint for our client
            IPEndPoint ieClient = (IPEndPoint)clientSocket.RemoteEndPoint;
            //Console info is locked because of being a shared resource between all clients
            lock (server.Locker)
            {
                server.writeEvent(string.Format(Properties.strings.SRV_USERCONNECT, ieClient.Address, ieClient.Port));
            }
            //Pointers to reader, writer, and stream
            StreamReader reader = null;
            StreamWriter writer = null;
            NetworkStream ns = null;
            //Catching IOException in case the client disconnects
            try
            {
                //Initialize streams
                ns = new NetworkStream(clientSocket);
                reader = new StreamReader(ns);
                writer = new StreamWriter(ns);
                //Protocol starts

                //Asking for userName
                writer.WriteLine(Properties.strings.CHAT_WELCOME);
                writer.WriteLine(Properties.strings.CHAT_ASKUSERNAME);
                writer.Flush();
                userName = reader.ReadLine();
                //Reask for userName if it's empty
                while (string.IsNullOrWhiteSpace(userName))
                {
                    writer.WriteLine(Properties.strings.CHAT_REASKUSERNAME);
                    writer.Flush();
                    userName = reader.ReadLine();
                }
                //
                writer.WriteLine(string.Format(Properties.strings.CHAT_GOAHEAD, userName));
                writer.WriteLine(Properties.strings.CHAT_COMMANDS);
                writer.Flush();
                //Compose the username and check for ArgumentException if two keys from our Dictionary are the same
                lock (server.Locker)
                {
                    userName = userName + "@" + ieClient.Address;
                    try
                    {
                        server.ClientList.Add(userName, clientSocket);
                    }
                    catch (ArgumentException)
                    {
                        writer.WriteLine(Properties.strings.CHAT_MULTIPLEUSERSKICKED);
                        writer.Flush();
                        //KICK HIM!
                        RunChat = false;
                        throw new IOException();
                    }
                }
                //Tell all users about our user joining the room
                broadcastMsg(string.Format(Properties.strings.CHAT_JOINNOTICE, userName));
                //Chat routine
                while (RunChat)
                {
                    //Read the message, if available, if it's null, means our user disconnected
                    string msg = reader.ReadLine();
                    if (msg != null)
                    {
                        //Watch out for commands, do the command or, if it's a regular message, broadcast it to all users
                        switch (msg.Trim())
                        {
                            //Quits the chat
                            case "#quit":
                                lock (server.Locker)
                                {
                                    RunChat = false;
                                }
                                break;
                            //Lists the users
                            case "#list":
                                lock (server.Locker)
                                {
                                    writer.WriteLine(Properties.strings.CHAT_LIST);
                                    foreach (KeyValuePair<string, Socket> client in server.ClientList)
                                    {
                                        writer.WriteLine(client.Key);
                                    }
                                    writer.Flush();
                                }
                                break;
                            //Broadcasts the message
                            default:
                                broadcastMsg(userName + Properties.strings.CHAT_MSGSEPARATOR + msg);
                                break;
                        }
                    }
                    else
                    {
                        lock (server.Locker)
                        {
                            RunChat = false;
                        }
                    }
                }
                //Bye message
                writer.WriteLine(String.Format(Properties.strings.CHAT_BYEMSG, userName));
                writer.Flush();
                //Closing streams
                writer.Close();
                reader.Close();
                ns.Close();
            }
            catch (IOException)
            {
                server.writeEvent(string.Format(Properties.strings.SRV_EXCEPTION, userName));
            }
            //Close the client socket, and remove it from the list (if previously added)
            clientSocket.Close();
            if (userName != null)
            {
                lock (server.Locker)
                {
                    if (server.ClientList.ContainsKey(userName))
                    {
                        server.ClientList.Remove(userName);
                    }
                }
                //Tell all users who left
                broadcastMsg(string.Format(Properties.strings.CHAT_LEFTNOTICE, userName));
            }
            //Inform the server of a user leaving
            server.writeEvent(string.Format(Properties.strings.SRV_USERDISCONNECT, ieClient.Address, ieClient.Port));
        }

        /// <summary>
        /// Broadcasts a message to all users in the chatroom added to the Dictionary
        /// </summary>
        /// <param name="msg">The message to broadcast</param>
        private void broadcastMsg(string msg)
        {
            lock (server.Locker)
            {
                try
                {
                    foreach (KeyValuePair<string, Socket> client in server.ClientList)
                    {
                        Socket broadcSocket = client.Value;
                        NetworkStream ns = new NetworkStream(broadcSocket);
                        StreamWriter writer = new StreamWriter(ns);
                        writer.WriteLine(msg);
                        writer.Flush();
                    }
                }
                catch (IOException)
                {
                    server.writeEvent(string.Format(Properties.strings.SRV_EXCEPTION, userName));
                }
            }
        }
    }
}
