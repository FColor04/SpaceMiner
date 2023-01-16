using System.Collections.Generic;
using System.Linq;

namespace SpaceMiner.Server;

public class ServerData
{
    public readonly List<Player> Players = new ();
    private List<Player> _lastSentData = new ();

    public List<Player> PlayerDataToSend
    {
        get
        {
            var dataToSend = Players.Except(_lastSentData).ToList();
            _lastSentData = dataToSend;
            return dataToSend;
        }
    }
}