using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Cliente_Servidor
{

    public class Cliente
    {
        private static string PORT = "8080";
        private static TcpClient session;
        private static string? clientName = null;
        private static bool closingCon = false;
        private static NetworkStream? _stream = null;

        public static void Main(string[] args)
        {
            Console.Write("Introduce tu nombre: ");
            clientName = Console.ReadLine();
            do
            {
                Console.Write("Error, se ha dejado el campo de nombre vacio, introduce un nombre: ");
                clientName = Console.ReadLine();
            } while (String.IsNullOrEmpty(clientName));
            Connect();
            Console.WriteLine("Conexión finalizada.");
        }

        /// <summary>
        /// Conexión al servidor
        /// </summary>
        public static void Connect()
        {
            try
            {
                Console.Write($"Introduce la dirección IP del servidor:");
                string? inputIP = Console.ReadLine();

                Console.Write($"Introduce el puerto del servidor: (Puerto por defecto {PORT})");
                string? inputPORT = Console.ReadLine();
                Int32 port = Int32.Parse(inputPORT);

                session = new TcpClient(inputIP, port);
                Console.WriteLine("Conectado al servidor");
                _stream = session.GetStream();
                StreamReader reader = new StreamReader(_stream);
                StreamWriter writer = new StreamWriter(_stream);
                writer.AutoFlush = true;

                Console.WriteLine("¡Se ha realizado la conexión correctamente! Para salir introduzca '/exit' \n" +
                    "Para enviar mensajes a un cliente usa el comando /msg <nombre de destinatario> <mensaje>. \n" + 
                    "Para ver la lista de clientes en el servidor, use el comando /list");

                Thread readThread = new Thread(new ParameterizedThreadStart(ReadResponse));
                Thread writeThread = new Thread(new ParameterizedThreadStart(WriteResponse));

                if (!closingCon)
                {
                    readThread.Start(reader);
                    writeThread.Start(writer);
                }
                else
                {
                    readThread.Join();
                    writeThread.Join();
                }

                while (!closingCon) 
                {
                    if (closingCon)
                        break;
                }

                _stream.Close();
                session.Close();
            }
            catch (ArgumentNullException e)
            {
                string ans = "";
                Console.WriteLine($"Error al conectar al servidor: no se ha introducido alguno de los 2 campos necesarios para la conexión. \n"
                                    + "¿Desea finalizar la ejecución (S/N)?");
                ans = Console.ReadLine().ToUpper();
                while (ans != "S" && ans != "N")
                {
                    if (ans != "S" && ans != "N")
                    {
                        Console.WriteLine("Por favor, introduzca una opción válida (S/N)");
                    }
                    else
                    {
                        Connect();
                    }
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine($"Error en la conexión al servidor: {e.Message} \n"
                                    + "Verifique que la dirección IP y el puerto sean correctos, si desea finalizar deje ambos campos o uno vacio.");
                Connect();
            }

        }

        /// <summary>
        /// Mostrar respuesta del servidor
        /// </summary>
        /// <param name="_reader"></param>
        private static void ReadResponse(object _reader)
        {
            StreamReader? reader = _reader as StreamReader;
            Byte[] bytes = new byte[256];
            while (true)
            {
                try
                {
                    int bytesRead = reader.Read();
                    if (bytesRead == 0) break; // Conexion cerrada
                        string response = Encoding.ASCII.GetString(bytes, 0, bytesRead);
                    Console.WriteLine($"Servidor: {response}");
                }
                catch (IOException e)
                {
                    Console.WriteLine("Error al leer la respuesta del servidor.");
                    break;
                }
            }
        }

        /// <summary>
        /// Escritura del lado del cliente
        /// </summary>
        /// <param name="_writer"></param>
        private static void WriteResponse(object _writer) 
        {
            StreamWriter writer = _writer as StreamWriter;
            string message = "";
            while (true) // Bucle para mensajeria con el servidor
            {
                try
                {
                    message = Console.ReadLine();
                    writer.WriteLine($"{clientName}: {message}");

                    if (!String.IsNullOrEmpty(message)) 
                    {
                        if (message.StartsWith("/msg"))
                        {
                            string[] aux = message.Split(' ');
                            string msg = message.Substring(message.IndexOf(' ', message.IndexOf(' ') + 1));

                            if (aux[1] != clientName)
                            {
                                writer.Write($"/SendMessageToThisClient {aux[1]} {clientName}: {msg}");
                            }
                            else
                            { 
                                Console.WriteLine("No puedes enviarte mensajes a ti mismo.");
                            }
                        }
                        else if (message == "/list")
                        {
                            Console.WriteLine("Solicitando lista de clientes...");
                            writer.Write($"/list {session.Client.LocalEndPoint}");
                        }
                        else if (message == "/exit") // Cerrar conexión
                        {
                            Console.WriteLine("Cerrando conexión...");
                            writer.Write($"/exit {session.Client.LocalEndPoint}");
                            closingCon = true;
                        }
                        else
                        {
                            Console.WriteLine("Error, se ha usado un comando incorrectamente o el comando introducido no existe.");
                        }
                    }
                }
                catch (SocketException e) 
                {
                    Console.WriteLine($"Ha ocurrido un error inesperado, se cerrará la conexión.\n Mas Información: {e}");
                }

            }
        }
    }
}
