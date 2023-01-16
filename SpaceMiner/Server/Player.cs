using System;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using SpaceMiner.Utils;

namespace SpaceMiner.Server;

public struct Player : IEquatable<Player>, INetSerializable
{
    public NetPeer Peer;
    public int Id;
    public Vector2 Position;
    public Vector2 SimulatedPosition;
    public NetworkPlayerInput NetworkPlayerInput;

    public Player(NetPeer peer)
    {
        Peer = peer;
        Id = Peer.Id;
        Position = default;
        SimulatedPosition = Position;
        NetworkPlayerInput = default;
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Position.X);
        writer.Put(Position.Y);
        writer.Put(Peer.Id);
        writer.Put(NetworkPlayerInput);
    }

    public void Deserialize(NetDataReader reader)
    {
        Position.X = reader.GetFloat();
        Position.Y = reader.GetFloat();
        Id = reader.GetInt();
        NetworkPlayerInput = reader.GetPlayerInput();
    }

    public bool Equals(Player other)
    {
        return Id == other.Id && Position.Equals(other.Position) && NetworkPlayerInput.Equals(other.NetworkPlayerInput);
    }

    public override bool Equals(object obj)
    {
        return obj is Player other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Position, NetworkPlayerInput);
    }

    public NetDataWriter GetDisconnectWriter()
    {
        var output = new NetDataWriter();
        output.Put((int) Networking.MessageType.PlayerRemove);
        output.Put(this);
        return output;
    }
}