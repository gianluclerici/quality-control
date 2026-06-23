using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.IO;

//
//  Classe TcpServer per la gestione del server TCP che fornisce servizi a client TCP
//
namespace Ficep.AnyCut.ConNet
{
    // Define a delegate with a compatible signature
    public delegate void MainThreadDelegateType(string notification, out string returnMessage);

    ////////////////////////////////////////////
    //
    //  Gestione Server TCP tramite thread
    //
    ////////////////////////////////////////////
    public class TcpServer
    {
        private string logFilePath = "RobServer.log";
        private IPAddress ipAddress = IPAddress.Any;
        private int ipPort = 0;
        private SynchronizationContext syncContext;
        private ManualResetEvent workcompletedEvent = new ManualResetEvent(false);
        private MainThreadDelegateType mainThreadDelegate = null;
        string returnMessage = null;

        //
        //  Funzione di Thread del server di gestione connessione con client
        //
        private void ServerThreadWaitingClientConnection()
        {
            // Create a TCP listener
            TcpListener server = new TcpListener(IPAddress.Any, ipPort);
            server.Start();

            try
            {
                while (true)
                {
                    // Accept client connection
                    TcpClient client = server.AcceptTcpClient();
                    string clientIP = ((IPEndPoint)(client.Client.RemoteEndPoint)).Address.ToString();
                    // Header log file
                    string logString = string.Format("Server listening on port {0}", ipPort);
                    Console.WriteLine(logString);
                    WriteLogString("------------------------------------------------------", clientIP);
                    WriteLogString(logString, clientIP);
                    WriteLogString("------------------------------------------------------", clientIP);
                    logString = "Client connected";
                    Console.WriteLine(logString);
                    WriteLogString("", clientIP);
                    WriteLogString(logString, clientIP);

                    // Create a new thread to handle client communication
                    Thread clientThread = new Thread(() => ServerThreadHandleClientFunc(client));
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                // The exception if captured will not be written in the log file with the client ipAddress
                // because it is not known if the clieant is already connected 
                // Header log file without ipAddress
                string logString = string.Format("Server listening on port {0}", ipPort);
                Console.WriteLine(logString);
                WriteLogString("------------------------------------------------------");
                WriteLogString(logString);
                WriteLogString("------------------------------------------------------");

                logString = string.Format("Error: " + ex.Message);
                Console.WriteLine(logString);
                WriteLogString(logString);
            }
            finally
            {
                server.Stop();
            }
        }

        //
        //  Thread to handle client communication
        //
        private void ServerThreadHandleClientFunc(TcpClient client)
        {
            string clientIP = ((IPEndPoint)(client.Client.RemoteEndPoint)).Address.ToString();

            try
            {
                NetworkStream stream = client.GetStream();

                while (true)
                {
                    
                    //  I primi 4 byte mi danno la dimensione del buffer dati successivo
                    byte[] byteMessageSize = new byte[4];
                    int bytesRead = stream?.Read(byteMessageSize, 0, byteMessageSize.Length) ?? 0;

                    if (bytesRead <= 0)
                        break;

                    int messageSize = BitConverter.ToInt32(byteMessageSize, 0);

                    byte[] buffer = new byte[messageSize];

                    bytesRead = stream?.Read(buffer, 0, buffer.Length)?? 0;
                    if (bytesRead <= 0)
                        break;

                    string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    string logString = string.Format("Received: " + data);
                    Console.WriteLine(logString);
                    WriteLogString(logString, clientIP);

                    // After completing some work, send a notification to the main thread
                    string workerMessage = data;
                    int workerParameter = 1;

                    workcompletedEvent.Reset();

                    //  Notify the main thread the command to be executed
                    WorkingThreadSendNotificationToMainThread(workerMessage, workerParameter);
                    Console.WriteLine("WorkerThread: Paused.");

                    //  Waiting for notification from the main thread that the command has been completed
                    workcompletedEvent.WaitOne();  // Wait until resumed
                    Console.WriteLine("WorkerThread: Resumed.");

                    // Echo back the received data to the client
                    byte[] response;

                    if (returnMessage == string.Empty)
                        response = Encoding.ASCII.GetBytes("Server: " + data);
                    else
                        response = Encoding.ASCII.GetBytes(returnMessage);

                    //  Scrivo nei primi 4 bytes la lunghezza del buffer successivo di dati che andrò a scrivere
                    byte[] lengthBytes = BitConverter.GetBytes(response.Length);
                    stream.Write(lengthBytes, 0, lengthBytes.Length);

                    stream.Write(response, 0, response.Length);
                }
            }
            catch (Exception ex)
            {
                string logString = string.Format("Client error: " + ex.Message);
                Console.WriteLine(logString);
                WriteLogString(logString, clientIP);
            }
            finally
            {
                client.Close();
                returnMessage = null;
                string logString = "Client disconnected";
                Console.WriteLine(logString);
                WriteLogString(logString, clientIP);
            }
        }

        //
        //  Notify the main thread the command to be executed
        //
        private void WorkingThreadSendNotificationToMainThread(string message, int parameter)
        {
            syncContext.Post(state =>
            {
                string notification = (string)((object[])state)[0];
                int param = (int)((object[])state)[1];

                mainThreadDelegate(notification, out returnMessage);

                Console.WriteLine($"MainThread: Notification received: {notification}, Parameter: {param}");
            }, new object[] { message, parameter });
        }

        private IPAddress GetIPv4Address()
        {
            IPAddress iPv4Address = null;
            // Get host name of the local machine
            string hostName = Dns.GetHostName();

            // Get IP addresses associated with the host name
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);

            Console.WriteLine("IP Addresses for " + hostName + ":");

            foreach (IPAddress address in addresses)
            {
                // Check if the address is IPv4
                if (address.AddressFamily == AddressFamily.InterNetwork)
                    return address;
            }

            return iPv4Address;
        }

        protected void WriteLogString(string logString, string ipAddress = "")
        {
            string customLogFilePath = logFilePath;
            if (ipAddress != "")
                customLogFilePath = logFilePath.Split('.')[0] + ipAddress + ".log"; 
            //
            //  Se la dimensione del file di log supera 1Mb, lo cancello
            //  Serve per saturare a 1 MB l'occupazione su disco
            //
            int maxLengthBytes = 1000000;
            if (File.Exists(customLogFilePath) && new FileInfo(customLogFilePath).Length >= maxLengthBytes)
            {
                // Delete the file
                File.Delete(customLogFilePath);
            }

            // Create or append to the log file
            using (StreamWriter writer = File.AppendText(customLogFilePath))
            {
                LogToLogFile(writer, logString);
            }
        }
        protected void LogToLogFile(StreamWriter writer, string message)
        {
            string logEntry = $"{DateTime.Now} {message}";
            writer.WriteLine(logEntry);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ipPort"></IP port>
        /// <param name="mainThreadDelegate"></external function to be called by the thread>
        /// <returns></returns>
        public bool Start(int ipPort, MainThreadDelegateType mainThreadDelegate)
        {
            this.ipPort = ipPort;
            this.mainThreadDelegate = mainThreadDelegate;

            syncContext = SynchronizationContext.Current;
            Thread threadTCPServer = new Thread(ServerThreadWaitingClientConnection);
            threadTCPServer.Start();
            workcompletedEvent.Reset();

            return true;
        }
        public void WorkCompleted()
        {
            //  Segnalo al WorkingThread che la funzione è stata completata
            workcompletedEvent.Set();
        }
    }
}
