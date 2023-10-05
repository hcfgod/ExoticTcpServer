using Serilog;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ExoticServer.App;

namespace ExoticServer.Classes.Server
{
    public class ExoticTcpServer
    {
        private TcpListener _serverTcpListener;
        private readonly int _port;
        private ServerState _state = ServerState.Stopped;

        private ConcurrentDictionary<string, ClientHandler> _clients = new ConcurrentDictionary<string, ClientHandler>();
        private ConcurrentDictionary<string, Task> _clientTasks = new ConcurrentDictionary<string, Task>();

        private CancellationTokenSource _cts = new CancellationTokenSource();

        public ExoticTcpServer(int port)
        {
            if (!IsValidServerPort(port))
            {
                ChronicApplication.Instance.Logger.Error("(ExoticTcpServer) ExoticTcpServer(): Invalid server port. ", nameof(port));
                throw new ArgumentException("Invalid server port. ", nameof(port));
            }

            _port = port;
        }

        public async Task StartServer()
        {
            if (_state == ServerState.Running)
            {
                ChronicApplication.Instance.Logger.Error("(ExoticTcpServer) StartServer: Server is already running.");
                throw new InvalidOperationException("Server is already running.");
            }

            _serverTcpListener = _serverTcpListener ?? new TcpListener(IPAddress.Any, _port);
            _serverTcpListener.Start();
            _state = ServerState.Running;
             ChronicApplication.Instance.Logger.Information($"(ExoticTcpServer) StartServer - Started Server On Port: " + _port);
            await ListenForClients(_cts.Token);
        }

        public void StopServer()
        {
            if (_state == ServerState.Stopped)
            {
                 ChronicApplication.Instance.Logger.Error("(ExoticTcpServer) StopServer: Server is already stopped");
                throw new InvalidOperationException("Server is already stopped.");
            }

            foreach (var clientHandler in _clients.Values)
            {
                clientHandler.DisconnectedClient();
            }

            _cts.Cancel(); // Signal all operations to cancel
            _serverTcpListener.Stop();
            _state = ServerState.Stopped;

             ChronicApplication.Instance.Logger.Information($"(ExoticTcpServer) StopServer - Stopped Server On Port: " + _port);

        }

        public void Dispose()
        {
            _serverTcpListener?.Stop();
            _serverTcpListener = null;
            _cts.Dispose();
        }

        public void HandleClientDisconnection(ClientHandler clientHandler)
        {
            // Remove the associated task.
            if (clientHandler.ClientTask != null)
            {
                string clientKey = clientHandler.GetTcpClient().Client.RemoteEndPoint.ToString();
                _clients.TryRemove(clientKey, out _);
                _clientTasks.TryRemove(clientKey, out _);
            }

             ChronicApplication.Instance.Logger.Information($"(ExoticTcpServer) HandleClientDisconnection - Client Disconnected.");
        }

        private async Task ListenForClients(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _state == ServerState.Running)
            {
                try
                {
                    CleanupCompletedTasks();

                    if (_serverTcpListener == null || !_serverTcpListener.Server.IsBound)
                    {
                        // Log or handle the situation
                        ChronicApplication.Instance.Logger.Information($"(ExoticTcpServer) ListenForClientsAsync() - Server is Bound.");

                        return;
                    }

                    // Using a timeout with AcceptTcpClientAsync allows you to periodically check for cancellation.
                    if (_serverTcpListener.Pending())
                    {
                        TcpClient newClient = await _serverTcpListener.AcceptTcpClientAsync();

                        if (newClient == null || newClient.Client == null || !newClient.Client.Connected)
                        {
                            ChronicApplication.Instance.Logger.Information($"(ExoticTcpServer) ListenForClientsAsync() - Null Client");
                            return;
                        }

                        ChronicApplication.Instance.Logger.Information($"(ExoticTcpServer) ListenForClientsAsync() - Client Connected");

                        ClientHandler clientHandler = new ClientHandler(this, newClient);

                        Task clientTask = clientHandler.HandleClientAsync(token);
                        clientHandler.ClientTask = clientTask;

                        string clientKey = newClient.Client.RemoteEndPoint.ToString();

                        _clients.TryAdd(clientKey, clientHandler);
                        _clientTasks.TryAdd(clientKey, clientTask);
                    }
                    else
                    {
                        await Task.Delay(100); // Short delay before checking again
                    }
                }
                catch (SocketException socketEx)
                {
                    // Handle socket exceptions, which might occur if the listener gets closed.
                     ChronicApplication.Instance.Logger.Error($"(ExoticTcpServer) ListenForClientsAsync() - Socket error: {socketEx.Message}");
                }
                catch (Exception ex)
                {
                    // Handle other general exceptions.
                     ChronicApplication.Instance.Logger.Error($"(ExoticTcpServer) ListenForClientsAsync() - Listening error: {ex.Message}");
                }
            }
        }

        private bool IsValidServerPort(int port)
        {
            return port >= 1024 && port <= 65535;
        }

        private void CleanupCompletedTasks()
        {
            foreach (var key in _clientTasks.Keys)
            {
                if (_clientTasks.TryGetValue(key, out Task task) && task.IsCompleted)
                {
                    task.Dispose();
                    _clientTasks.TryRemove(key, out _);
                }
            }
        }

        public ConcurrentDictionary<string, ClientHandler> GetClients()
        {
            return _clients;
        }
    }

    public enum ServerState
    {
        Stopped,
        Running
    }
}
