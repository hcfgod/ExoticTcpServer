using System;

namespace ExoticServer.Classes.Server.PacketSystem
{
    public class Packet
    {
        // Metadata
        public Guid PacketID { get; set; }
        public string PacketType { get; set; }
        public long SequenceNumber { get; set; }
        public DateTime Timestamp { get; set; }

        // Payload
        public byte[] Data { get; set; }

        // Security
        public string Checksum { get; set; }
        public bool EncryptionFlag { get; set; }

        // Flow Control
        public bool IsFragmented { get; set; }
        public Guid FragmentID { get; set; }

        // Error Handling
        public int RetryCount { get; set; }

        // Additional fields
        public string Version { get; set; }
        public int Priority { get; set; }
        public DateTime? ExpirationTime { get; set; }
        public string SenderID { get; set; }
        public string ReceiverID { get; set; }
    }
}
