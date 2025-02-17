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
    public class Servidor
    {
        private static string IP = GetLocalIPAddress();
        private static string PORT = "8080";
        private static TcpListener server = null;
        private static List<Session> _clients = new List<Session>();

        public static void Main(string[] args)
        {
            StartServer();
        }

        public static void StartServer()
        {
            try
            {
                IPAddress localAddr = IPAddress.Parse(IP);
                Int32 port = Int32.Parse(PORT);

                server = new TcpListener(localAddr, port);
                server.Start();

                Console.WriteLine($"¡Servidor iniciado! ahora puede usar los comandos de servidor, para saber mas usa /help.\n" +
                    $"El servidor se ha iniciado con dirección {IP} y en el puerto {PORT}");

                Thread writeThread = new Thread(WriteResponse);

                while (true)
                {
                    Console.Write($"Esperando conexión... (Dirección IP {IP})");
                    var client = server.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();
                    string newClientName = ReadStringFromStream(stream);

                    lock (_clients)
                    {
                        _clients.Add(new Session(client, newClientName));
                    }
                    Console.WriteLine("¡Cliente conectado!");
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("Servidor cerrado.");
            }
            finally
            {
                server.Stop();
            }
        }

        /// <summary>
        /// Introducir comandos en el servidor
        /// </summary>
        /// <param name="_writer"></param>
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
                                Console.WriteLine("Uso: /kick <indice del cliente>");
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
                            DisconnectAll();
                            server.Stop();
                        }
                        else
                        {
                            Console.WriteLine("Comando desconocido, para ver la lista de comandos utilice /help.");
                        }
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine($"Ha surgido un error: {e}");
                    break;
                }
            }
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
                    Console.WriteLine("Clientes Conectados:     Nombre          Dirección");
                    for (int i = 0; i < _clients.Count; i++)
                    {
                        Console.WriteLine($"Cliente {i + 1}:      {_clients[i]._name} {_clients[i]._session.Client.RemoteEndPoint}");
                    }
                }
                else
                    Console.Write("No hay clientes conectados");
            }
        }

        /// <summary>
        /// Mostrar solo los nombre d
        /// </summary>
        private static void ListAndSendClients(TcpClient sessionRequest)
        {
            string? aux = "";
            lock (_clients)
            {
                for (int i = 0; i < _clients.Count; i++)
                {
                    aux += ($"{_clients[i]._name} \n");
                }
                sessionRequest.GetStream().Write(Encoding.ASCII.GetBytes(aux)); //Enviar lista de clientes
            }
        }

        /// <summary>
        /// Enviar mensaje del servidor a todos los clientes
        /// </summary>
        /// <param name="message"></param>
        private static void WriteAllClients(string message)
        {
            lock (_clients)
            {
                foreach (Session s in _clients)
                {
                    s._session.GetStream().Write(Encoding.ASCII.GetBytes(message));
                }
            }
        }

        /// <summary>
        /// Desconectar todos los clientes y limpiar la lista de clientes
        /// </summary>
        private static void DisconnectAll()
        {
            lock (_clients)
            {
                for (int i = 0; i < _clients.Count; i++)
                {
                    _clients[i]._session.GetStream().Write(Encoding.ASCII.GetBytes("Se ha cerrado el servidor, por lo que seras expulsado."));
                    _clients[i]._session.Close();
                }
                _clients.Clear();
            }
        }

        /// <summary>
        /// Desconectar cliente
        /// </summary>
        /// <param name="clientIndex"></param>
        /// <param name="writer"></param>
        private static void DisconnectClient(int clientIndex)
        {
            clientIndex--;
            lock (_clients)
            {
                if (clientIndex >= 0 && clientIndex < _clients.Count)
                {
                    TcpClient aux = _clients[clientIndex]._session;
                    aux.Close();
                    _clients.RemoveAt(clientIndex);
                    aux.GetStream().Write(Encoding.ASCII.GetBytes("Has sido expulsado del servidor."));
                    Console.WriteLine($"Cliente {clientIndex + 1} desconectado.");
                }
                else
                    Console.WriteLine("Error, índice de cliente invalido");
            }
        }

        /// <summary>
        /// Desconectar cliente, usado cuando el cliente tiene un error
        /// </summary>
        /// <param name="clientDisc"></param>
        private static void DisconnectClient(TcpClient clientDisc)
        {
            lock (_clients)
            {
                int aux = -1;
                for (int i = 0; i < _clients.Count; i++)
                {
                    if (_clients[i]._session == clientDisc)
                        aux = i;
                }
                if (aux != -1)
                {
                    Console.WriteLine($"{_clients[aux].ToString()} se ha desconectado.");
                    _clients.RemoveAt(aux);
                }

            }
        }

        /// <summary>
        /// Listar ayuda de comandos
        /// </summary>
        private static void Help()
        {
            Console.WriteLine("Comandos disponibles:\n" +
                "/list - Listar clientes conectados.\n" +
                "/kick <indice> - Desconectar cliente.\n" +
                "/msg <mensaje> - Enviar mensaje a todos los clientes.\n" +
                "/close - Cerrar servidor.");
        }

        /// <summary>
        /// Recoger IP local para iniciar el servidor
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
            throw new Exception("Error, no se puede crear servidor en este dispositivo " +
                "no existen adaptadores con direccion IPv4 en el sistema.");
        }

        /// <summary>
        /// Busca un cliente usando su EndPoint
        /// </summary>
        /// <param name="clientEndPoint"></param>
        /// <returns></returns>
        private static TcpClient? SearchFromClientsList(string clientEndPoint)
        {
            lock (_clients) 
            {
                foreach (Session s in _clients)
                {
                    string aux = s._session.Client.RemoteEndPoint.ToString();
                    if (aux == clientEndPoint)
                        return s._session;
                }
            }
            return null;
        }

        /// <summary>
        /// Busca un cliente por nombre
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
        /// Comunicacion entre clientes
        /// </summary>
        /// <param name="textRecieved"></param>
        private static void SendMessageClientToClient(string textRecieved) 
        {
            //estructura --> <comando> <destinatario> <remitente> <mensaje>
            var request = SearchFromClientsListByName(textRecieved.Split(' ')[1]);
            if (request != null)
            {
                string messageToSend = textRecieved.Substring(textRecieved.IndexOf(' ', textRecieved.IndexOf(' ') + 1) + 1);
                request.GetStream().Write(Encoding.ASCII.GetBytes(messageToSend));
            }
            else
            {
                var sender = SearchFromClientsListByName(textRecieved.Split(new char[] {' ', ':' }, StringSplitOptions.RemoveEmptyEntries)[2]);
                sender.GetStream().Write(Encoding.ASCII.GetBytes("El cliente con quien intenta comunicarse no existe," +
                    "utilice el comando /list para ver que clientes estan conectados."));
            }
        }

        /// <summary>
        /// Retorna la cadena de caracteres que haya llegado al servidor
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private static string ReadStringFromStream(NetworkStream stream)
        {
            byte[] buffer = new byte[256];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            return Encoding.ASCII.GetString(buffer, 0, bytesRead);
        }

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
                _readThread.Start(_session.GetStream());

            }

            private static void ReadResponse(object _reader)
            {
                while (true)
                {
                    try
                    {
                        string response = ReadStringFromStream(_reader as NetworkStream);

                        if (response.StartsWith("/SendMessageToThisClient"))
                        {
                            SendMessageClientToClient(response);
                        }
                        else if (response.StartsWith("/list"))
                        {
                            var request = SearchFromClientsList(response.Split(' ')[1]);
                            ListAndSendClients(request);
                        }
                        else if (response.StartsWith("/exit"))
                        {
                            var clientToExit = SearchFromClientsList(response.Split()[1]);
                            DisconnectClient(clientToExit);
                        }

                    }
                    catch (IOException)
                    {
                        Console.WriteLine("Error al leer la respuesta del cliente.");
                        break;
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
