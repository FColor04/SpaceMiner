using LiteNetLib.Utils;

namespace SpaceMiner.Utils;

public static class SerializingExtensions
{
    public static void Put(this NetDataWriter writer, NetworkPlayerInput input) {
        writer.Put(input.GetByte);
    }

    public static NetworkPlayerInput GetPlayerInput(this NetDataReader reader) {
        return new NetworkPlayerInput(reader.GetByte());
    }
}