using ExoticServer.App;
using ExoticServer.Classes.Server.PacketSystem.Packets;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ExoticServer.Classes.Server.PacketSystem
{
    public class PacketHandler
    {
        private ConcurrentDictionary<string, IPacketHandler> packetHandlers = new ConcurrentDictionary<string, IPacketHandler>();
        private ConcurrentDictionary<string, List<Packet>> packetFragments = new ConcurrentDictionary<string, List<Packet>>();

        public PacketHandler()
        {
            // Initialize packet handlers
            packetHandlers.TryAdd("Client Public Key Packet", new ClientPublicKeyPacket());
            packetHandlers.TryAdd("User Login Packet", new UserLoginPacket());
            packetHandlers.TryAdd("User Registration Packet", new UserRegistrationPacket());
            packetHandlers.TryAdd("User Details Request Packet", new UserDetailsRequestPacket());
        }

        public byte[] SerializePacket(Packet packet)
        {
            try
            {
                if (packet.EncryptionFlag)
                {
                    packet.Data = CryptoUtility.AesEncrypt(packet.Data);  // Your Encrypt method
                }

                packet.GenerateChecksum();

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

        public List<Packet> DeserializePackets(byte[] data)
        {
            try
            {
                List<Packet> packets = new List<Packet>();
                string receivedDataStr = Encoding.UTF8.GetString(data);
                string[] jsonPackets = receivedDataStr.Split(new[] { "}{" }, StringSplitOptions.None);

                foreach (var json in jsonPackets)
                {
                    string validJson = json;
                    if (!json.StartsWith("{")) validJson = "{" + validJson;
                    if (!json.EndsWith("}")) validJson = validJson + "}";

                    Packet packet = JsonConvert.DeserializeObject<Packet>(validJson);

                    // Validate checksum
                    string originalChecksum = packet.Checksum;

                    packet.GenerateChecksum();

                    if (originalChecksum != packet.Checksum)
                    {
                        ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - DeserializePacket(): Checksum validation failed.");
                        return null;
                    }

                    if (packet != null)
                    {
                        if(packet.EncryptionFlag)
                        {
                            packet.Data = CryptoUtility.AesDecrypt(packet.Data);
                        }

                        packets.Add(packet);
                    }
                }

                return packets;
            }
            catch (JsonException jsonEx)
            {
                string receivedDataStr = Encoding.UTF8.GetString(data);
                ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - DeserializePacket(): JSON Deserialization Error: {jsonEx.Message}. Received data: {receivedDataStr}");
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
                ExpirationTime = DateTime.UtcNow.AddMinutes(5),
                SenderID = "Server",
                ReceiverID = "Client",
            };

            if (packet.IsFragmented)
            {
                packet.FragmentID = Guid.NewGuid();
            }

            return packet;
        }

        public List<Packet> CreateChunks(Packet largePacket, int maxChunkSize)
        {
            List<Packet> chunks = new List<Packet>();
            byte[] largeData = largePacket.Data;

            int totalFragments = (int)Math.Ceiling((double)largeData.Length / maxChunkSize);

            for (int i = 0; i < largeData.Length; i += maxChunkSize)
            {
                byte[] chunkData = largeData.Skip(i).Take(maxChunkSize).ToArray();

                Packet chunk = new Packet
                {
                    PacketID = largePacket.PacketID,
                    PacketType = largePacket.PacketType,
                    Timestamp = largePacket.Timestamp,
                    Data = chunkData,
                    EncryptionFlag = largePacket.EncryptionFlag,

                    IsFragmented = true,
                    FragmentID = largePacket.PacketID,
                    SequenceNumber = i / maxChunkSize,
                    TotalFragments = totalFragments,

                    Version = largePacket.Version,
                    ExpirationTime = largePacket.ExpirationTime,
                    SenderID = largePacket.SenderID,
                    ReceiverID = largePacket.ReceiverID,
                };
                chunks.Add(chunk);
            }
            return chunks;
        }

        public async Task SendPacketAsync(Packet packet, NetworkStream stream, int maxChunkSize = 4096)
        {
            try
            {
                if (packet.Data.Length > maxChunkSize)
                {
                    var chunks = CreateChunks(packet, maxChunkSize);
                    foreach (var chunk in chunks)
                    {
                        await SendSinglePacketAsync(stream, chunk);
                    }
                }
                else
                {
                    await SendSinglePacketAsync(stream, packet);
                }
            }
            catch (IOException ioEx)
            {
                ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - SendPacketAsync(): IO Error: {ioEx.Message}");
            }
            catch (ObjectDisposedException objDisposedEx)
            {
                ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - SendPacketAsync(): Object Disposed Error: {objDisposedEx.Message}");
            }
            catch (Exception ex)
            {
                ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - SendPacketAsync(): General Error: {ex.Message}");
            }
        }

        private async Task SendSinglePacketAsync(NetworkStream stream, Packet packet)
        {
            try
            {
                byte[] data = SerializePacket(packet);

                if (data == null)
                {
                    ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - SendPacketAsync(): Serialization Error: Serialization failed data was null");
                }

                await stream.WriteAsync(data, 0, data.Length);
            }
            catch (IOException ioEx)
            {
                ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - SendPacketAsync(): IO Error: {ioEx.Message}");
            }
            catch (ObjectDisposedException objDisposedEx)
            {
                ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - SendPacketAsync(): Object Disposed Error: {objDisposedEx.Message}");
            }
            catch (Exception ex)
            {
                ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - SendPacketAsync(): General Error: {ex.Message}");
            }
        }

        public async Task<List<Packet>> ReceivePacketAsync(NetworkStream stream, byte[] buffer)
        {
            List<Packet> receivedPackets = new List<Packet>();
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

                List<Packet> deserializedPackets = DeserializePackets(receivedData);

                foreach (var deserializedPacket in deserializedPackets)
                {
                    if (deserializedPacket == null)
                    {
                        ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - ReceivePacketAsync(): Deserialize Packet is null.");
                        continue;
                    }

                    //ChronicApplication.Instance.Logger.Information($"Received packet with ID: {deserializedPacket.PacketID}, Sequence: {deserializedPacket.SequenceNumber}");

                    if (deserializedPacket.IsFragmented)
                    {
                        if (!packetFragments.ContainsKey(deserializedPacket.FragmentID.ToString()))
                        {
                            packetFragments.TryAdd(deserializedPacket.FragmentID.ToString(), new List<Packet>());
                        }

                        packetFragments[deserializedPacket.FragmentID.ToString()].Add(deserializedPacket);

                        if (IsLastFragment(deserializedPacket))
                        {
                            HandleMissingOrOutOfOrderFragments(deserializedPacket.FragmentID.ToString());

                            receivedPackets.Add(ReassemblePacket(deserializedPacket.FragmentID.ToString()));
                        }
                    }
                    else
                    {
                        receivedPackets.Add(deserializedPacket);
                    }
                }
            }
            catch (IOException ioEx)
            {
                ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - ReceivePacketAsync(): IO Error: {ioEx.Message}");
            }
            catch (ObjectDisposedException objDisposedEx)
            {
                ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - ReceivePacketAsync(): Object Disposed Error: {objDisposedEx.Message}");
            }
            catch (Exception ex)
            {
                ChronicApplication.Instance.Logger.Error($"(PacketHandler.cs) - ReceivePacketAsync(): General Error: {ex.Message}");
            }
            return receivedPackets;
        }

        public void ProcessPacket(Packet packet, ClientHandler clientHandler)
        {
            if (packetHandlers.TryGetValue(packet.PacketType, out IPacketHandler handler))
            {
                handler.Handle(packet, clientHandler);
            }
            else
            {
                ChronicApplication.Instance.Logger.Warning($"Unknown packet type {packet.PacketType}");
            }
        }

        private Packet ReassemblePacket(string fragmentID)
        {
            var fragments = packetFragments[fragmentID];
            var reassembledData = fragments.OrderBy(f => f.SequenceNumber)
                                           .SelectMany(f => f.Data)
                                           .ToArray();

            var firstFragment = fragments.First();
            firstFragment.Data = reassembledData;
            firstFragment.IsFragmented = false;

            packetFragments.TryRemove(fragmentID, out _);

            return firstFragment;
        }

        private bool IsLastFragment(Packet packet)
        {
            if (packetFragments.TryGetValue(packet.FragmentID.ToString(), out List<Packet> fragments))
            {
                // Check if the number of received fragments equals the total number of fragments
                if (fragments.Count == packet.TotalFragments)
                {
                    // Check for missing or out-of-order fragments
                    var sequenceNumbers = fragments.Select(f => f.SequenceNumber).OrderBy(n => n).ToList();
                    for (int i = 0; i < sequenceNumbers.Count; i++)
                    {
                        if (sequenceNumbers[i] != i)
                        {
                            // Missing or out-of-order fragment detected
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private void HandleMissingOrOutOfOrderFragments(string fragmentID)
        {
            if (packetFragments.TryGetValue(fragmentID, out List<Packet> fragments))
            {
                var sequenceNumbers = fragments.Select(f => f.SequenceNumber).OrderBy(n => n).ToList();
                List<int> missingFragments = new List<int>();

                for (int i = 0; i < fragments.First().TotalFragments; i++)
                {
                    if (!sequenceNumbers.Contains(i))
                    {
                        missingFragments.Add(i);
                    }
                }

                if (missingFragments.Any())
                {
                    // Logic to handle missing fragments
                    string missingFragmentsStr = string.Join(", ", missingFragments);
                    ChronicApplication.Instance.Logger.Warning($"Missing fragments for FragmentID: {fragmentID} are {missingFragmentsStr}");
                    // Optionally, request these specific fragments from the client again
                }
                else
                {
                    // Logic to handle out-of-order but complete fragments
                    fragments.Sort((a, b) => a.SequenceNumber.CompareTo(b.SequenceNumber));
                    // Fragments are now in order and ready for reassembly
                }
            }
        }
    }
}
