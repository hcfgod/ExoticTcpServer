namespace ExoticServer.Classes.Server.PacketSystem
{
    public interface IPacketHandler
    {
        void Handle(Packet packet);
    }
}
