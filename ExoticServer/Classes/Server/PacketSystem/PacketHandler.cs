using Newtonsoft.Json;
using Serilog;
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
                Log.Error($"(PacketHandler.cs) - SerializePacket(): JSON Serialization Error: {jsonEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // Handle other errors
                Log.Error($"(PacketHandler.cs) - SerializePacket(): General Error: {ex.Message}");
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
                Log.Error($"(PacketHandler.cs) - DeserializePacket(): JSON Deserialization Error: {jsonEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"(PacketHandler.cs) - DeserializePacket(): General Error: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> SendPacketAsync(Packet packet, NetworkStream stream)
        {
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
                Log.Error($"(PacketHandler.cs) - SendPacketAsync(): IO Error: {ioEx.Message}");
                return false;
            }
            catch (ObjectDisposedException objDisposedEx)
            {
                Log.Error($"(PacketHandler.cs) - SendPacketAsync(): Object Disposed Error: {objDisposedEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"(PacketHandler.cs) - SendPacketAsync(): General Error: {ex.Message}");
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
                Log.Error($"(PacketHandler.cs) - ReceivePacketAsync(): IO Error: {ioEx.Message}");
                return null;
            }
            catch (ObjectDisposedException objDisposedEx)
            {
                Log.Error($"(PacketHandler.cs) - ReceivePacketAsync(): Object Disposed Error: {objDisposedEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"(PacketHandler.cs) - ReceivePacketAsync(): General Error: {ex.Message}");
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
                Log.Error($"(PacketHandler.cs) - SegmentPacket(): General Error: {ex.Message}");
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
                Log.Error($"(PacketHandler.cs) - ReassemblePacket(): General Error: {ex.Message}");
                return null;
            }
        }
    }
}
