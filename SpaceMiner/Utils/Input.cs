using Microsoft.Xna.Framework.Input;

namespace SpaceMiner.Utils;

public static class Input
{
    public static NetworkPlayerInput ReadPlayerInput()
    {
        var keyboardState = Keyboard.GetState();
        return new NetworkPlayerInput(
            keyboardState.IsKeyDown(Keys.W), 
            keyboardState.IsKeyDown(Keys.S), 
            keyboardState.IsKeyDown(Keys.A), 
            keyboardState.IsKeyDown(Keys.D)
            );
    }
}