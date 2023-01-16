using Myra.Graphics2D.UI;

namespace SpaceMiner;

public static class UI
{
    public static Desktop Desktop = new ();
    public static void Render() => Desktop.Render();
}