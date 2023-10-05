using ExoticServer.App;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExoticServer.Classes.Server.PacketSystem
{
    public class PacketHandler
    {
        private long lastSequenceNumber = 0;
        private long expectedSequenceNumber = 1;
        private SortedList<long, Packet> receivedPackets = new SortedList<long, Packet>();

        private Dictionary<int, IPacketHandler> packetHandlers = new Dictionary<int, IPacketHandler>();

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

        public Packet CreateNewPacket(byte[] data)
        {
            return new Packet
            {
                Data = data,
                SequenceNumber = ++lastSequenceNumber,
                // ... other fields
            };
        }

        public async Task<bool> SendPacketAsync(Packet packet, NetworkStream stream, string clientId)
        {
            if (IsRateLimited(clientId))
            {
                return false;
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

        public async Task<Packet> ReceivePacketAsync(NetworkStream stream, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];

            try
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, bufferSize);

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

        public List<Packet> SegmentPacket(Packet largePacket, int maxPacketSize)
        {
            try
            {
                List<Packet> packetSegments = new List<Packet>();
                byte[] largeData = largePacket.Data;
                Guid fragmentID = Guid.NewGuid(); // Unique ID for this set of fragments

                for (int i = 0; i < largeData.Length; i += maxPacketSize)
                {
                    Packet segment = new Packet
                    {
                        // Copy metadata from the original packet
                        PacketType = largePacket.PacketType,
                        SequenceNumber = largePacket.SequenceNumber,
                        Timestamp = DateTime.UtcNow,

                        // Set fragment-specific fields
                        IsFragmented = true,
                        FragmentID = fragmentID,

                        // Extract a segment of data
                        Data = new byte[Math.Min(maxPacketSize, largeData.Length - i)]
                    };
                    Array.Copy(largeData, i, segment.Data, 0, segment.Data.Length);

                    packetSegments.Add(segment);
                }

                return packetSegments;
            }
            catch (Exception ex)
            {
                 ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - SegmentPacket(): General Error: {ex.Message}");
                return null;
            }
        }

        public Packet ReassemblePacket(List<Packet> packetSegments)
        {
            try
            {
                if (packetSegments == null || !packetSegments.Any())
                {
                    return null;
                }

                // Validate that all segments belong to the same FragmentID
                Guid fragmentID = packetSegments[0].FragmentID;

                if (packetSegments.Any(p => p.FragmentID != fragmentID))
                {
                    MessageBox.Show("Error: Mismatched Fragment IDs");
                    return null;
                }

                // Combine all segment data into one large data array
                int totalSize = packetSegments.Sum(p => p.Data.Length);
                byte[] largeData = new byte[totalSize];
                int offset = 0;

                foreach (Packet segment in packetSegments)
                {
                    Array.Copy(segment.Data, 0, largeData, offset, segment.Data.Length);
                    offset += segment.Data.Length;
                }

                // Create the reassembled packet
                Packet largePacket = new Packet
                {
                    // Copy metadata from one of the segments
                    PacketType = packetSegments[0].PacketType,
                    SequenceNumber = packetSegments[0].SequenceNumber,
                    Timestamp = DateTime.UtcNow,

                    // Set the reassembled data
                    Data = largeData,

                    // Clear fragment-specific fields
                    IsFragmented = false,
                    FragmentID = Guid.Empty
                };

                return largePacket;
            }
            catch (Exception ex)
            {
                 ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - ReassemblePacket(): General Error: {ex.Message}");
                return null;
            }
        }

        public void ProcessPackets()
        {
            foreach (var packet in receivedPackets.Values)
            {
                ProcessPacket(packet);  // Assume ProcessPacket is your method to handle each packet
            }
            receivedPackets.Clear();
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

        public void ReceiveAndOrderPacket(Packet receivedPacket)
        {
            receivedPackets.Add(receivedPacket.SequenceNumber, receivedPacket);
        }

        public void CheckForMissingPackets()
        {
            foreach (var packet in receivedPackets.Values)
            {
                if (packet.SequenceNumber != expectedSequenceNumber)
                {
                     ChronicApplication.Instance.Logger.Warning($"(PacketHandler.cs) - CheckForMissingPackets(): Missing packets from {expectedSequenceNumber} to {packet.SequenceNumber - 1}");
                    // Here, you can add code to request retransmission of missing packets
                }

                expectedSequenceNumber = packet.SequenceNumber + 1;
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
