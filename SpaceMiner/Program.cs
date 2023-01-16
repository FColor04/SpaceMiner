using var initializer = new SpaceMiner.Server.GameInitializer();
if (initializer.ShouldRunGame())
{
    using var game = new SpaceMiner.Game1();
    game.Run();
}