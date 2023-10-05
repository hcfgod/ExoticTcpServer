using ExoticServer.App;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ExoticServer.Classes.Server.PacketSystem
{
    public class PacketHandler
    {
        private Dictionary<string, IPacketHandler> packetHandlers = new Dictionary<string, IPacketHandler>();

        private Dictionary<string, int> rateLimits = new Dictionary<string, int>();
        private Dictionary<string, DateTime> lastRequestTimes = new Dictionary<string, DateTime>();
        private const int MaxRequestsPerMinute = 60; // Set your limit here

        public PacketHandler()
        {
            // Initialize packet handlers
        }

        public byte[] SerializePacket(Packet packet)
        {
            try
            {
                string jsonString = JsonConvert.SerializeObject(packet);
                return Encoding.UTF8.GetBytes(jsonString);
            }
            catch (JsonException jsonEx)
            {
                // Handle JSON serialization errors
                ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - SerializePacket(): JSON Serialization Error: {jsonEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // Handle other errors
                 ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - SerializePacket(): General Error: {ex.Message}");
                return null;
            }
        }

        public Packet DeserializePacket(byte[] data)
        {
            try
            {
                string jsonString = Encoding.UTF8.GetString(data);
                return JsonConvert.DeserializeObject<Packet>(jsonString);
            }
            catch (JsonException jsonEx)
            {
                 ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - DeserializePacket(): JSON Deserialization Error: {jsonEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                 ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - DeserializePacket(): General Error: {ex.Message}");
                return null;
            }
        }

        public Packet CreateNewPacket(byte[] data, string packetType, bool encryptionFlag = false, string version = "0.1")
        {
            Packet packet = new Packet
            {
                PacketID = Guid.NewGuid(),
                PacketType = packetType,
                Timestamp = DateTime.UtcNow,
                Data = data,
                EncryptionFlag = encryptionFlag,

                Version = version,
                Priority = 1,
                ExpirationTime = DateTime.UtcNow.AddMinutes(5),
                SenderID = "Server",
                ReceiverID = "Client",
            };

            if(packet.IsFragmented)
            {
                packet.FragmentID = Guid.NewGuid();
            }

            return packet;
        }

        public async Task<bool> SendPacketAsync(Packet packet, NetworkStream stream, string clientId = null)
        {
            if(clientId != null)
            {
                if (IsRateLimited(clientId))
                {
                    return false;
                }
            }

            try
            {
                byte[] data = SerializePacket(packet);
                if (data == null)
                {
                    // Serialization failed
                    return false;
                }

                await stream.WriteAsync(data, 0, data.Length);
                return true;
            }
            catch (IOException ioEx)
            {
                 ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - SendPacketAsync(): IO Error: {ioEx.Message}");
                return false;
            }
            catch (ObjectDisposedException objDisposedEx)
            {
                 ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - SendPacketAsync(): Object Disposed Error: {objDisposedEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                 ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - SendPacketAsync(): General Error: {ex.Message}");
                return false;
            }
        }

        public async Task<Packet> ReceivePacketAsync(NetworkStream stream, byte[] buffer)
        {
            try
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    // Connection has been closed
                    return null;
                }

                byte[] receivedData = new byte[bytesRead];
                Array.Copy(buffer, receivedData, bytesRead);

                return DeserializePacket(receivedData);
            }
            catch (IOException ioEx)
            {
                 ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - ReceivePacketAsync(): IO Error: {ioEx.Message}");
                return null;
            }
            catch (ObjectDisposedException objDisposedEx)
            {
                 ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - ReceivePacketAsync(): Object Disposed Error: {objDisposedEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                 ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - ReceivePacketAsync(): General Error: {ex.Message}");
                return null;
            }
        }

        public void ProcessPacket(Packet packet)
        {
            if (packetHandlers.TryGetValue(packet.PacketType, out IPacketHandler handler))
            {
                handler.Handle(packet);
            }
            else
            {
                 ChronicApplication.Instance.Logger.Warning($"Unknown packet type {packet.PacketType}");
            }
        }

        public bool IsRateLimited(string clientId)
        {
            if (!rateLimits.ContainsKey(clientId))
            {
                rateLimits[clientId] = 0;
                lastRequestTimes[clientId] = DateTime.UtcNow;
            }

            // Check if the time window has passed and reset
            if ((DateTime.UtcNow - lastRequestTimes[clientId]).TotalMinutes >= 1)
            {
                rateLimits[clientId] = 0;
                lastRequestTimes[clientId] = DateTime.UtcNow;
            }

            // Check rate limit
            if (rateLimits[clientId] >= MaxRequestsPerMinute)
            {
                ChronicApplication.Instance.Logger.Warning($"(PacketHandler.cs) - IsRateLimited(): Client {clientId} is rate-limited.");
                return true;
            }

            // Increment the rate limit counter
            rateLimits[clientId]++;
            return false;
        }
    }
}
