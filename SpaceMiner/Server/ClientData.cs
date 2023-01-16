using System.Collections.Generic;

namespace SpaceMiner.Server;

public class ClientData
{
    public readonly Dictionary<int, Player> Players = new ();
    
    public void ParsePlayerData(Player player)
    {
        if (!Players.ContainsKey(player.Id))
            Players.Add(player.Id, player);
        else
        {
            player.SimulatedPosition = Players[player.Id].SimulatedPosition;
            Players[player.Id] = player;
        }
    }
}