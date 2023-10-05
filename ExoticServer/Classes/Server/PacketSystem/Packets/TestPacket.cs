using System.Text;
using System.Windows.Forms;

namespace ExoticServer.Classes.Server.PacketSystem.Packets
{
    public class TestPacket : IPacketHandler
    {
        public void Handle(Packet packet)
        {
            string data = Encoding.UTF8.GetString(packet.Data);
            MessageBox.Show(data);
            MessageBox.Show(packet.Timestamp.ToString());
        }
    }
}
