using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Cliente_Servidor
{
    public class Server
    {
        private static string IP = GetLocalIPAddress();
        private static string PORT = "8080";
        private static bool closingServer = false;
        private static TcpListener server = null;
        private static List<Session> _clients = new List<Session>();

        public static void Main(string[] args)
        {
            StartServer();
        }

        public static async void StartServer()
        {
            Thread writeThread = null;
            try
            {
                IPAddress localAddr = IPAddress.Parse(IP);
                Int32 port = Int32.Parse(PORT);

                server = new TcpListener(localAddr, port);
                server.Start();

                Console.WriteLine($"Server initialized! now you can use server commands, to know more type /help.\n" +
                    $"Server initialized with address {IP} port {PORT}");

                writeThread = new Thread(WriteResponse);
                writeThread.Start();

                while (!closingServer)
                {
                    var clientTask = server.AcceptTcpClientAsync();
                    if (await Task.WhenAny(clientTask, Task.Delay(1000)) == clientTask)
                    {
                        var client = clientTask.Result;

                        lock (_clients)
                        {
                            _clients.Add(new Session(client, null)); // Name null, will be set later on by the same client
                        }
                        Console.WriteLine("Client connected!");
                    }

                }
            }
            catch (SocketException)
            {
                Console.WriteLine("Server closed.");
            }
            finally
            {
                if(writeThread != null)
                    writeThread.Join();
                server.Stop();
            }
        }

        /// <summary>
        /// Writing thread for server commands
        /// </summary>
        private static void WriteResponse()
        {
            while (true)
            {
                try
                {
                    string? message = Console.ReadLine();
                    if (!string.IsNullOrEmpty(message))
                    {
                        if (message.StartsWith("/list"))
                        {
                            ListClients();
                        }
                        else if (message.StartsWith("/kick"))
                        {
                            string[] parts = message.Split(' ');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int clientIndex))
                            {
                                DisconnectClient(clientIndex);
                            }
                            else
                            {
                                Console.WriteLine("Use: /kick <client index>");
                            }
                        }
                        else if (message.StartsWith("/msg"))
                        {
                            message = message.Substring(5);
                            WriteAllClients(message);
                        }
                        else if (message.StartsWith("/help"))
                        {
                            Help();
                        }
                        else if (message == "/close")
                        {
                            Console.WriteLine("Server is closing, all clients will be disconnected.");
                            DisconnectAll();
                            closingServer = true;
                        }
                        else
                        {
                            Console.WriteLine("Command unknown, to list all commands available type /help.");
                        }
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine($"An error has occurred: {e}");
                    break;
                }
            }
        }

        /// <summary>
        /// Checks if the name passed is taken or not
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static bool isNameTaken(string name)
        {
            lock (_clients) 
            {
                for (int i = 0; i < _clients.Count; i++) 
                {
                    if (_clients[i]._name == name)
                        return true;
                    
                }
            }
            return false;
        }

        /// <summary>
        /// Mostrar clientes en el servidor
        /// </summary>
        private static void ListClients()
        {
            lock (_clients)
            {
                if (_clients.Count > 0)
                {
                    Console.WriteLine("Clients connected (Name & EndPoint):");
                    for (int i = 0; i < _clients.Count; i++)
                    {
                        Console.WriteLine($"Client {i + 1}:      {_clients[i]._name} | {_clients[i]._session.Client.RemoteEndPoint}");
                    }
                }
                else
                    Console.WriteLine("There are no clients connected.");
            }
        }

        /// <summary>
        /// List and send all clients connected to the client that requested it
        /// </summary>
        private static void ListAndSendClients(Session sessionRequest)
        {
            string? aux = "";
            lock (_clients)
            {
                for (int i = 0; i < _clients.Count; i++)
                {
                    aux += ($"{_clients[i]._name} \n");
                }
                sessionRequest._session.GetStream().Write(Encoding.ASCII.GetBytes(aux)); // Finally send the string which contains all clients
            }
        }

        /// <summary>
        /// Send a message to all clients
        /// </summary>
        /// <param name="message"></param>
        private static void WriteAllClients(string message)
        {
            lock (_clients)
            {
                foreach (Session s in _clients)
                {
                    s._session.GetStream().Write(Encoding.ASCII.GetBytes($"Servidor: {message}"));
                }
            }
        }

        /// <summary>
        /// Disconnect and clear all clients
        /// </summary>
        private static void DisconnectAll()
        {
            lock (_clients)
            {
                for (int i = 0; i < _clients.Count; i++)
                {
                    _clients[i]._session.GetStream().Write(Encoding.ASCII.GetBytes("Server has been closed, all clients has been disconnected."));
                    _clients[i]._session.Close();
                }
                _clients.Clear();
            }
        }

        /// <summary>
        /// Kick client
        /// </summary>
        /// <param name="clientIndex"></param>
        private static void DisconnectClient(int clientIndex)
        {
            clientIndex--;
            lock (_clients)
            {
                if (clientIndex > 0 && clientIndex < _clients.Count)
                {
                    TcpClient aux = _clients[clientIndex]._session;
                    aux.Close();
                    _clients.RemoveAt(clientIndex);
                    aux.GetStream().Write(Encoding.ASCII.GetBytes("You have been kicked out of the server."));
                    Console.WriteLine($"Client {clientIndex + 1} kicked.");
                }
                else
                    Console.WriteLine("Error, index out of bounds.");
            }
        }

        /// <summary>
        /// Disconnect and remove a client from the list
        /// </summary>
        /// <param name="clientToDisconnect"></param>
        private static void DisconnectClient(Session clientToDisconnect) 
        {
            lock (_clients) 
            {
                _clients.Remove(clientToDisconnect);
                clientToDisconnect._session.Close();
            }
        }

        /// <summary>
        /// Help, list all commands available
        /// </summary>
        private static void Help()
        {
            Console.WriteLine("Available commands:\n" +
                "/list - List clients connected.\n" +
                "/kick <index> - kick client.\n" +
                "/msg <message> - Send message to all clients.\n" +
                "/close - close server (disconnects all clients and then close connection).");
        }

        /// <summary>
        /// Get local address to initialize server
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Error, is not possible to create a server in this device " +
                "there are no IPv4 adapters within the system.");
        }

        /// <summary>
        /// search for a client by its name
        /// </summary>
        /// <param name="clientName"></param>
        /// <returns></returns>
        private static TcpClient? SearchFromClientsListByName(string clientName) 
        {
            lock (_clients) 
            {
                foreach (Session s in _clients)
                {
                    if (s._name == clientName)
                        return s._session;
                }
            }
            return null;
        }

        /// <summary>
        /// Communitacion between clients
        /// </summary>
        /// <param name="textRecieved"></param>
        private static void SendMessageClientToClient(Session sender, string textRecieved)
        {
            //message --> <command> <target client> <message>
            //target client sees --> <sender>: <message>
            var request = SearchFromClientsListByName(textRecieved.Split(' ')[1]);
            if (request != null)
            {
                string messageToSend = $"{sender._name}: {textRecieved.Substring(textRecieved.IndexOf(' ', textRecieved.IndexOf(' ') + 1)) + 1}";
                request.GetStream().Write(Encoding.ASCII.GetBytes(messageToSend));
            }
            else
            {
                sender._session.GetStream().Write(Encoding.ASCII.GetBytes("You are trying to send a message to a client that is not connected or doesn't exist," +
                    "type /list to see which clients are connected."));
            }
        }

        /// <summary>
        /// returns a string from stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private static string ReadStringFromStream(NetworkStream stream)
        {
            byte[] buffer = new byte[256];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            return Encoding.ASCII.GetString(buffer, 0, bytesRead);
        }

        /// <summary>
        /// Internal class to manage sessions
        /// </summary>
        internal class Session
        {
            public TcpClient _session;
            public string _name;
            public Thread _readThread;

            public Session(TcpClient session, string name)
            {
                _session = session;
                _name = name;
                _readThread = new Thread(new ParameterizedThreadStart(ReadResponse));
                _readThread.Start(this); // Send this session as an object

            }

            /// <summary>
            /// Thread to read responses from clients
            /// </summary>
            /// <param name="sessionObj"></param>
            private static void ReadResponse(object sessionObj)
            {
                Session sessionInstance = (Session)sessionObj; // Since it's a static method, we need to cast the object to a Session object
                var _reader = sessionInstance._session.GetStream();
                while (true)
                {
                    try
                    {
                        string response = ReadStringFromStream(_reader);

                        if (response.StartsWith("/SendMessageToThisClient")) // Send messages between clients
                        {
                            SendMessageClientToClient(sessionInstance, response);
                        }
                        else if (response.StartsWith("/list")) // List and send clients
                        {
                            ListAndSendClients(sessionInstance);
                        }
                        else if (response.StartsWith("/exit")) // Client disconnecting
                        {
                            DisconnectClient(sessionInstance);
                        }
                        else if (response.StartsWith("/isNameTaken"))
                        {
                            string nameToCheck = response.Substring(response.IndexOf(' ') + 1);
                            if (isNameTaken(nameToCheck))
                                sessionInstance._session.GetStream().Write(Encoding.ASCII.GetBytes("true"));
                            else
                                sessionInstance._session.GetStream().Write(Encoding.ASCII.GetBytes("false"));
                        }
                        else if (response.StartsWith("/setName")) 
                        { 
                            sessionInstance._name = response.Substring(response.IndexOf(' ') + 1);
                        }

                    }
                    catch (IOException)
                    {
                        Console.WriteLine("Unable to read client response.");
                        break;
                    }
                    finally 
                    {
                        sessionInstance._readThread.Join();
                    }
                }
            }

            public string ToString()
            {
                return $"{_name}, {_session}";
            }
        }
    }
}
