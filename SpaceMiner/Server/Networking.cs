using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using SpaceMiner.Utils;

namespace SpaceMiner.Server;

public static class Networking
{
    public const string GlobalAddress = "51.195.45.78";
    //public const string GlobalAddress = "localhost";
    private const int Port = 25564;
    private const string Key = "SuperSecretKey";
    private const int TickRate = 1000 / 60;
    private static float TickDelta = TickRate / 1000f;

    public static ClientData ClientData;
    
    private static readonly EventBasedNetListener ClientListener = new();
    private static readonly NetManager Client = new(ClientListener);
    
    private static bool _isClientActive;
    private static bool _shutdownClient;

    private static ServerData _serverData;
    
    private static readonly EventBasedNetListener ServerListener = new ();
    private static readonly NetManager Server = new (ServerListener);
    
    private static bool _isServerActive;
    private static bool _shutdownServer;
    public static NetPeer MyPeer;
    private static DateTime _lastTickDate;

    [Serializable]
    public enum MessageType
    {
        ChatMessage,
        PlayerData,
        PlayerRemove,
        PlayerInput
    }

    public static NetDataWriter GetChatMessageWriter(string message)
    {
        NetDataWriter writer = new NetDataWriter();
        writer.Put((int) MessageType.ChatMessage);
        writer.Put(message);
        return writer;
    }
    
    public static NetDataWriter GetPlayerWriter(Player player)
    {
        NetDataWriter writer = new NetDataWriter(true);
        writer.Put((int) MessageType.PlayerData);
        writer.Put(player);
        return writer;
    }

    public static void UpdatePlayerInput()
    {
        if (Client == null) return;
        if (Client.FirstPeer == null) return;
        
        var writer = new NetDataWriter();
        writer.Put((int) MessageType.PlayerInput);
        writer.Put(Input.ReadPlayerInput());
        Client.FirstPeer.Send(writer, DeliveryMethod.ReliableOrdered);
    }
    
    public static bool StartClient(string address)
    {
        if (_isClientActive)
        {
            Debug.WriteLine($"{DateTime.Now:T} [CLIENT] - Client is already listening on port {Port}.");
            return false;
        }

        Client.Start();
        MyPeer = Client.Connect(address, Port, Key);

        ClientData = new ClientData();
        
        ClientListener.NetworkReceiveEvent += (fromPeer, dataReader, channel, deliveryMethod) =>
        {
            var incomingMessageType = (MessageType) dataReader.GetInt();
            Player? playerData = null;
            switch (incomingMessageType)
            {
                case MessageType.PlayerData:
                case MessageType.PlayerRemove:
                    playerData = dataReader.Get<Player>();
                    break;
            }

            switch (incomingMessageType)
            {
                case MessageType.ChatMessage:
                    Debug.WriteLine($"{DateTime.Now:T} [CLIENT] Received: {dataReader.GetString(100)}");
                    break;
                case MessageType.PlayerData:
                    ClientData.ParsePlayerData(playerData!.Value);
                    break;
                case MessageType.PlayerRemove:
                    ClientData.Players.Remove(playerData!.Value.Id);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            //Debug.WriteLine($"{DateTime.Now:T} [CLIENT] Received {incomingMessageType}: {((playerData.HasValue) ? $"{playerData.Value.Id} {playerData.Value.Position}" : -1)}");
            dataReader.Recycle();
        };
        
        _isClientActive = true;
        var clientThread = new Thread(() =>
        {
            while (!_shutdownClient)
            {
                if (Client == null) break;
                Client.PollEvents();
                Thread.Sleep(TickRate);
            }
            Client.Stop();
            _isClientActive = false;
        });
        clientThread.Start();
        Debug.WriteLine($"{DateTime.Now:T} [CLIENT] - Client started, listening on port {Port}.");
        return true;
    }
    
    public static async Task StopClient()
    {
        Debug.WriteLine($"{DateTime.Now:T} [CLIENT] - Shutting down client.");
    }
    
    public static bool StartServer()
    {
        if (_isServerActive)
        {
            Debug.WriteLine($"{DateTime.Now:T} [SERVER] - Server is already running on port {Port}.");
            return false;
        }

        _serverData = new ServerData();
        
        Server.Start(Port);
        ServerListener.ConnectionRequestEvent += request =>
        {
            if(Server.ConnectedPeersCount < 10 /* max connections */)
                request.AcceptIfKey(Key);
            else
                request.Reject();
        };
        ServerListener.PeerConnectedEvent += peer =>
        {
            var newPlayer = new Player(peer);
            _serverData.Players.Add(newPlayer);
            Debug.WriteLine($"{DateTime.Now:T} [SERVER] - Connected: {peer.EndPoint}");
            peer.Send(GetChatMessageWriter("Hello!"), DeliveryMethod.ReliableOrdered);
            Server.SendToAll(GetPlayerWriter(newPlayer), DeliveryMethod.ReliableOrdered);
        };
        ServerListener.NetworkReceiveEvent += (peer, reader, channel, method) =>
        {
            switch ((MessageType) reader.GetInt())
            {
                case MessageType.PlayerInput:
                    var player = _serverData.Players[peer.Id];
                    player.NetworkPlayerInput = reader.GetPlayerInput();
                    _serverData.Players[peer.Id] = player;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            reader.Recycle();
        };
        ServerListener.PeerDisconnectedEvent += (peer, info) =>
        {
            if (_serverData.Players.TryGetFirst(p => p.Id == peer.Id, out var player))
            {
                Server.SendToAll(player.GetDisconnectWriter(), DeliveryMethod.ReliableOrdered);
                _serverData.Players.Remove(player);
            }

        };
        _isServerActive = true;
        var serverThread = new Thread(() =>
        {
            while (!_shutdownServer)
            {
                TickDelta = (float) (DateTime.Now - _lastTickDate).TotalSeconds;
                
                Server.PollEvents();
                for (var i = 0; i < _serverData.Players.Count; i++)
                {
                    var player = _serverData.Players[i];
                    player.Position += player.NetworkPlayerInput.GetMovementVector() * TickDelta * 256;
                    _serverData.Players[i] = player;
                }

                foreach (var player in _serverData.PlayerDataToSend)
                {
                    Server.SendToAll(GetPlayerWriter(player), DeliveryMethod.ReliableOrdered);
                }
                
                _lastTickDate = DateTime.Now;
                Thread.Sleep(TickRate);
            }
            Server.Stop();
            _isServerActive = false;
        });
        serverThread.Start();
        Debug.WriteLine($"{DateTime.Now:T} [SERVER] - Server started on port {Port}.");
        return true;
    }

    public static async Task StopServer()
    {
        Debug.WriteLine($"{DateTime.Now:T} [SERVER] - Shutting down server.");
        _shutdownServer = true;
        while (_isServerActive)
            await Task.Delay(25);
    }
}