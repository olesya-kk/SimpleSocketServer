using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleSocketServer
{
    public partial class ServerForm : Form
    {
        private TcpListener server;
        private Thread listenerThread;
        private bool isRunning = true;
        private List<TcpClient> clients = new List<TcpClient>();
        private List<string> messageHistory = new List<string>();



        public ServerForm()
        {
            InitializeComponent();
            StartServer();
        }

        private void StartServer()
        {
            try
            {
                server = new TcpListener(IPAddress.Any, 8888);
                server.Start();
                listenerThread = new Thread(new ThreadStart(ListenForClients));
                listenerThread.Start();
                Log("Server started.");
            }
            catch (Exception ex)
            {
                Log("Error starting server: " + ex.Message);
            }
        }

        private void ListenForClients()
        {
            try
            {
                while (isRunning)
                {
                    TcpClient client = server.AcceptTcpClient();
                    clients.Add(client);
                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                    clientThread.Start(client);

                    // Отправка истории сообщений новому клиенту
                    foreach (string message in messageHistory)
                    {
                        SendMessageToClient(client, message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error accepting client connection: " + ex.Message);
            }
        }

        private void HandleClientComm(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();
            byte[] message = new byte[4096];
            int bytesRead;
            try
            {
                while ((bytesRead = clientStream.Read(message, 0, 4096)) > 0)
                {
                    string receivedMessage = Encoding.ASCII.GetString(message, 0, bytesRead);
                    BroadcastMessage(receivedMessage);
                    messageHistory.Add(receivedMessage); 
                }
            }
            catch (Exception ex)
            {
                Log("Error handling client communication: " + ex.Message);
                tcpClient.Close();
                clients.Remove(tcpClient);
            }
        }

        private void BroadcastMessage(string message)
        {
            foreach (TcpClient client in clients)
            {
                SendMessageToClient(client, message);
            }
        }

        private void SendMessageToClient(TcpClient client, string message)
        {
            NetworkStream clientStream = client.GetStream();
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();
        }

        private void Log(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(Log), message);
                return;
            }
            richTextBoxLog.AppendText(message + Environment.NewLine);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            isRunning = false;
            server.Stop();
            foreach (TcpClient client in ClientList.Clients)
            {
                client.Close();
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
    public static class ClientList
    {
        public static readonly System.Collections.Generic.List<TcpClient> Clients = new System.Collections.Generic.List<TcpClient>();
    }
}
