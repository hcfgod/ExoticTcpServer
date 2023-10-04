using Serilog;
using System;
using System.Buffers;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ExoticServer.Classes.Server
{
    public class ClientHandler : IDisposable
    {
        private ExoticTcpServer _server;
        private TcpClient _client;
        private NetworkStream _clientStream;

        private bool _disposed;

        // This will store a reference to the task handling the client, as per your TcpServer design.
        public Task ClientTask { get; set; }

        public ClientHandler(ExoticTcpServer server, TcpClient client)
        {
            _server = server;
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _clientStream = _client.GetStream();
        }

        public async Task HandleClientAsync(CancellationToken token)
        {
            ArrayPool<byte> pool = ArrayPool<byte>.Shared;
            byte[] dataBuffer = pool.Rent(4096);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    // Process Packet Here
                }
            }
            catch (IOException ioEx) when (ioEx.InnerException is SocketException)
            {
                // Handle potential socket exceptions here. This might occur if the client forcibly closes the connection.
            }
            catch (Exception ex)
            {
                // Handle other exceptions as necessary.
                Log.Logger.Information($"(ClientHandler.cs) - HandleClientAsync(): {ex.Message}");
            }
            finally
            {
                pool.Return(dataBuffer);
                DisconnectedClient();
            }
        }

        public void DisconnectedClient()
        {
            if(_client != null)
{
                _client.Close();
                _client = null;
            }
            if (_clientStream != null)
            {
                _clientStream.Dispose();
                _clientStream = null;
            }
        }

        public ExoticTcpServer GetTcpServer()
        {
            return _server;
        }

        public TcpClient GetTcpClient()
        {
            return _client;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _client?.Close();
                    _clientStream?.Dispose();
                }

                _disposed = true;
            }
        }

        ~ClientHandler()
        {
            Dispose(false);
        }
    }
}
