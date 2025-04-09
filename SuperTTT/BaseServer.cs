namespace SuperTicTacToe
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    abstract class BaseServer
    {
        private TcpListener _listener;
        private readonly int _port;

        public BaseServer(int port)
        {
            _port = port;
            _listener = new TcpListener(IPAddress.Any, _port);
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine($"Server started on port {_port}");
            while (true)
            {
                var client = _listener.AcceptTcpClient();
                Console.WriteLine("Client connected.");
                ThreadPool.QueueUserWorkItem(HandleClient, client);
            }
        }

        private void HandleClient(object clientObj)
        {
            var client = (TcpClient)clientObj;
            var stream = client.GetStream();
            var buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    OnMessageReceived(client, message);

                    // Optionally echo the message back
                    //Send(client, $"Server received: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                stream.Close();
                client.Close();
                Console.WriteLine("Client disconnected.");
            }
        }

        public virtual void OnMessageReceived(TcpClient client, string message)
        {
            // Override this method in derived classes to handle incoming messages
            Console.WriteLine($"Received: {message}");
        }

        public void Send(TcpClient client, string message)
        {
            var stream = client.GetStream();
            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length);
        }
    }
}


