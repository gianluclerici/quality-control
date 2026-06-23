using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace Ficep.AnyCut.ConNet
{
    public class RobServerTcpConnection
    {
        public Process RobServerProcess { get; private set; }

        private TcpClient _client;
        private NetworkStream _stream;
        private string _assemblyDirectory;
        private int _portNumber;
        private string _serverIP;
        private bool _isRobServerRemote; 

        public enum MessageCode { GetStatus = 0, GetVersion = 1, ValidateTool = 2, PathExtraction = 3, ValidateGeometry}
        public readonly int[] minimumRobServerVersion = { 1, 0, 0, 0 };

        public RobServerTcpConnection(int portNumber, string serverIP)
        {
            _portNumber = portNumber;
            _serverIP = serverIP;

            RobServerProcess = null;
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Get the location of the assembly (DLL file)
            string assemblyLocation = assembly.Location;

            // Get the directory containing the assembly
            _assemblyDirectory = Path.GetDirectoryName(assemblyLocation);

            _client = null;

            _isRobServerRemote = _serverIP != "127.0.0.1";
        }

        public bool StartRobServer()
        {
            string processName = _assemblyDirectory + @"\RobServer\Ficep.RobServer.exe",
                   arguments = "/srv " + _portNumber;

            if (_isRobServerRemote)
            {
                if (!IsRobServerAlive())
                    return false;
                else
                    return true;
            }
            else if (!IsRobserverRunningLocally())
            {
                RobServerProcess = new Process
                {
                    StartInfo =
                    {
                      UseShellExecute = false,
                      FileName = processName,
                      Arguments = arguments,
                      WorkingDirectory = _assemblyDirectory + @"\RobServer",
                      CreateNoWindow = true
                    }
                };

                if (!RobServerProcess.Start())
                    return false;

                // Chiedo al sever di comunicarmi la sua versione
                string srvMessage;
                SendReceiveToRobserverOverTcp(((int)MessageCode.GetVersion).ToString(), out srvMessage);

                if (srvMessage == null)
                    return false;

                // Controllo che la versione del server sia compatibile con il client
                string[] tokens = srvMessage.Split('.');

                if (tokens == null || tokens.Length != minimumRobServerVersion.Length)
                    return false;

                for (int i = 0; i < tokens.Length; i++)
                {
                    string currToken = tokens[i];
                    int minServerVersionNumber = minimumRobServerVersion[i];
                    int serverVersionNumber;
                    
                    if (!int.TryParse(currToken, out serverVersionNumber))
                        return false;

                    if (serverVersionNumber < minServerVersionNumber)
                        return false;
                }
            }

            return true;
        }

        public bool CloseRobServer()
        {
            if (IsRobserverRunningLocally())
                KillProcessAndChildrens(RobServerProcess.Id);

            return true;
        }

        public void CloseRobServerConnection()
        {
            _stream?.Close();
            _client?.Close();
            _stream = null;
        }

        public bool SendReceiveToRobserverOverTcp(string message, out string srvMessage)
        {
            srvMessage = null;

            try
            {
                if (_client != null && _client.Connected)
                {
                    SendReceiveTcpMessage(message, out srvMessage);
                }
                else
                {
                    _client = new TcpClient();

                    if (_client.ConnectAsync(_serverIP, _portNumber).Wait(TimeSpan.FromSeconds(10)))
                    {
                        _stream = _client.GetStream();
                       
                        // Setto i secondi di timeout oltre il quale la read o la write falliscono 
                        int timeoutMs = 10000;

#if DEBUG
                        timeoutMs = int.MaxValue;
#endif

                        _stream.ReadTimeout = timeoutMs;
                        _stream.WriteTimeout = timeoutMs;
                    }
                    else
                        return false;

                    SendReceiveTcpMessage(message, out srvMessage);
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        private void SendReceiveTcpMessage(string clntMessage, out string srvMessage)
        {
            srvMessage = null;

            if (_stream == null)
                _stream = _client.GetStream();

            SendMessage(clntMessage);

            ReceiveMessage(out srvMessage);
         }

        private void SendMessage(string message)
        {
            byte[] data = Encoding.ASCII.GetBytes(message);
            byte[] lengthBytes = BitConverter.GetBytes(data.Length);

            _stream.Write(lengthBytes, 0, lengthBytes.Length);
            _stream.Write(data, 0, data.Length);
        }

        private void ReceiveMessage(out string message)
        {
            message = "";

            byte[] byteMessageSize = new byte[4];
            int bytesRead = _stream.Read(byteMessageSize, 0, byteMessageSize.Length);

            int messageSize = BitConverter.ToInt32(byteMessageSize, 0);

            byte[] data = new byte[messageSize];

            //
            //  Read può leggere meno byte di quelli richiesti perché TCP è uno stream.
            //  Per ricevere tutto un messaggio di lunghezza nota:
            //  -   leggi a loop finché non hai ricevuto tutti i byte
            //  -   controlla bytesRead == 0 per rilevare connessione chiusa
            //  -   evita di dare per scontato che Read restituisca tutto e subito
            //
            int totalRead = 0;
            while (totalRead < messageSize)
            {
                bytesRead = _stream.Read(data, totalRead, messageSize - totalRead);
                if (bytesRead == 0)
                {
                    // La connessione è stata chiusa dal server
                    throw new IOException("Connessione chiusa prematuramente");
                }
                totalRead += bytesRead;
            }

            message = Encoding.ASCII.GetString(data, 0, totalRead);
        }

        // Restituisce vero se Robserver è in esecuzione nella macchina locale
        private bool IsRobserverRunningLocally()
        {
            // Se non ho instanziato io il processo, guardo nella lista dei processi se c'è attivo il RobServer
            if (RobServerProcess == null)
            {
                var processes = Process.GetProcesses();

                foreach (var process in Process.GetProcesses())
                {
                    if (process.ProcessName == "Ficep.RobServer")
                    {
                        RobServerProcess = process;
                        break;
                    }
                }
            }

            bool IsProcessNotRunning = RobServerProcess == null || RobServerProcess.HasExited;

            if (IsProcessNotRunning)
                return false;
            else if (!IsRobServerAlive())
                return false;
            else 
                return true;
        }

        private bool IsRobServerAlive()
        {
            // Manda un messaggio al server se ti risponde il processo è vivo 
            string srvMessage,
                   message = ((int)MessageCode.GetStatus).ToString();

            if (!SendReceiveToRobserverOverTcp(message, out srvMessage))
                return false;
            else if (srvMessage != message)
                return false;
            else
                return true;
        }

        private void KillProcessAndChildrens(int pid)
        {
            ManagementObjectSearcher processSearcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection processCollection = processSearcher.Get();

            try
            {
                Process proc = Process.GetProcessById(pid);
                if (!proc.HasExited) proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }

            if (processCollection != null)
            {
                foreach (ManagementObject mo in processCollection)
                {
                    KillProcessAndChildrens(Convert.ToInt32(mo["ProcessID"])); //kill child processes(also kills childrens of childrens etc.)
                }
            }
        }
    }
}
