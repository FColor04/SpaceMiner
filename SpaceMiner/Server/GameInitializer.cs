using System;
using System.Linq;
using System.Threading;

namespace SpaceMiner.Server;

public class GameInitializer : IDisposable
{
    public bool KeepAlive = true;
    public bool ShouldRunGame()
    {
        if (Environment.GetCommandLineArgs().Any(flag => flag == "--server"))
        {
            Networking.StartServer();
            while (KeepAlive && !Environment.HasShutdownStarted)
            {
                Thread.Sleep(10);
            }
            Environment.Exit(0);
            return false;
        }

        return true;
    }

    ~GameInitializer() => Dispose();
    
    
    public void Dispose()
    {
        KeepAlive = false;
        Environment.Exit(0);
    }
}