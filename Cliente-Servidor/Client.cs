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

    public class Client
    {
        private static TcpClient session;
        private static string? clientName = null;
        private static bool closingCon = false;
        private static NetworkStream? _stream = null;

        public static void Main(string[] args)
        {
            Connect();
        }

        /// <summary>
        /// Conexión al servidor
        /// </summary>
        public static void Connect()
        {
            Thread writeThread = null;
            Thread readThread = null;
            try
            {
                Console.Write($"Input server address :");
                string? inputIP = Console.ReadLine();

                Console.Write($"Input server port: ");
                string? inputPORT = Console.ReadLine();
                Int32 port = Int32.Parse(inputPORT);

                session = new TcpClient(inputIP, port);
                Console.WriteLine("Attempting connection...");
                _stream = session.GetStream();
                StreamWriter writer = new StreamWriter(_stream);
                writer.AutoFlush = true;

                Console.WriteLine("Connection established!");

                do
                {
                    Console.Write("Input your name, please note that names are UNIQUE: ");
                    clientName = Console.ReadLine();

                    if (String.IsNullOrEmpty(clientName))
                        Console.WriteLine("Error, you left name field empty.");
                    else 
                    {
                        writer.Write($"/isNameTaken {clientName}");
                        if (ReadStringFromStream(_stream) == "true") 
                        {
                            Console.WriteLine("That name is already taken.");
                            clientName = null;
                        }   
                    }

                } while (String.IsNullOrEmpty(clientName));

                Console.WriteLine("Access granted to server! To exit type '/exit' \n" +
                    "Send messages using /msg <target client> <message>. \n" +
                    "To list clients connected use /list");

                //Initialize threads for writing and reading
                writeThread = new Thread(new ParameterizedThreadStart(WriteResponse));
                writeThread.Start(writer);
                readThread = new Thread(new ParameterizedThreadStart(ReadResponse));
                readThread.Start(new StreamReader(_stream));

                do
                {
                    if (closingCon)
                    {
                        readThread.Join();
                        writeThread.Join();
                    }
                } while (!closingCon);

            }
            catch (ArgumentNullException e)
            {
                string ans = "";
                Console.WriteLine($"Server connection error: one or both fields are empty.\n"
                                    + "Do you wish to exit? (Y/N)");
                ans = Console.ReadLine().ToUpper();
                while (ans != "Y" && ans != "N")
                {
                    if (ans != "Y" && ans != "N")
                    {
                        Console.WriteLine("Please, Type a valid option (Y/N)");
                    }
                    else
                    {
                        Connect();
                    }
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine($"Error, unable to connect to server: {e.Message} \n"
                    + "Verify address and port, if you want to exit leave one or both fields empty.");
                Connect();
            }
            finally 
            {
                if(writeThread != null)
                    writeThread.Join();
                if (readThread != null)
                    readThread.Join();
                if (_stream != null)
                    _stream.Close();
                session.Close();
                Console.WriteLine("Connection closed.");
            }

        }

        /// <summary>
        /// Thread to show server response
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
                    if (bytesRead == 0) break; // Connection closed
                        string response = Encoding.ASCII.GetString(bytes, 0, bytesRead);
                    Console.WriteLine(response);
                }
                catch (IOException e)
                {
                    Console.WriteLine("Unable to read server response.");
                }
            }
        }

        /// <summary>
        /// Read string from stream
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
        /// Thread for client side messaging
        /// </summary>
        /// <param name="_writer"></param>
        private static void WriteResponse(object _writer) 
        {
            StreamWriter writer = _writer as StreamWriter;
            string message = "";
            while (true) // keep messaging until exit
            {
                try
                {
                    message = Console.ReadLine();
                    if (!String.IsNullOrEmpty(message)) 
                    {
                        if (message.StartsWith("/msg")) // Send message to client
                        {
                            string[] aux = message.Split(' ');
                            string msg = message.Substring(message.IndexOf(' ', message.IndexOf(' ') + 1));

                            if (aux[1] != clientName)
                            {
                                writer.Write($"/SendMessageToThisClient {aux[1]} {msg}");
                            }
                            else
                            { 
                                Console.WriteLine("You can't send messages to yourself.");
                            }
                        }
                        else if (message == "/list") // Ask for clients list
                        {
                            Console.WriteLine("Asking for clients list...");
                            writer.Write($"/list");
                        }
                        else if (message == "/exit") // Close connection
                        {
                            Console.WriteLine("Closing connection...");
                            writer.Write($"/exit");
                            closingCon = true;
                        }
                        else
                        {
                            Console.WriteLine("Error, you used a command wrong or this command doesn't exists.");
                        }
                    }
                }
                catch (SocketException e) 
                {
                    Console.WriteLine($"Fatal error has occurred, Closing connection.\n More info: {e}");
                }

            }
        }
    }
}
