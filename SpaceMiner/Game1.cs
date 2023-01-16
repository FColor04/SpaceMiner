using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using SpaceMiner.Server;
using SpaceMiner.Utils;

namespace SpaceMiner;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D playerTexture;
    private NetworkPlayerInput _lastInput;
    private Vector2 _cameraPosition;
    private TextBox _ping;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
        Console.WriteLine($"{DateTime.Now:T} - Game initialized.");
    }

    protected override void LoadContent()
    {
        MyraEnvironment.Game = this;
        UI.Desktop.Widgets.Clear();
        
        var hostButton = new TextButton() {Text = "Host", Left = 24, Top = 0, Padding = new Thickness(8)};
        hostButton.Click += (_, _) => Networking.StartServer();
        UI.Desktop.Widgets.Add(hostButton);
        
        var connectButton = new TextButton() {Text = "Connect", Left = 24, Top = 36, Padding = new Thickness(8)};
        connectButton.Click += (_, _) => Networking.StartClient(Networking.GlobalAddress);
        UI.Desktop.Widgets.Add(connectButton);
        
        _ping = new TextBox(){Top = 0, Left = _graphics.PreferredBackBufferWidth - 48, Text = "0"};
        UI.Desktop.Widgets.Add(_ping);
        
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        playerTexture = Content.Load<Texture2D>("player"); // load texture

        Debug.WriteLine($"{DateTime.Now:T} - Content loaded.");
    }

    protected override void Update(GameTime gameTime)
    {
        var deltaTime = (float) gameTime.ElapsedGameTime.TotalSeconds;
        if (GamePad.GetState(PlayerIndex.One).Buttons.Start == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        if (Networking.ClientData != null)
        {
            var playerInput = Input.ReadPlayerInput();
            
            if (!playerInput.Equals(_lastInput))
                Networking.UpdatePlayerInput();
            _lastInput = playerInput;

            foreach (var peerId in Networking.ClientData.Players.Keys)
            {
                var player = Networking.ClientData.Players[peerId];
                player.SimulatedPosition = Vector2.Lerp(player.SimulatedPosition, player.Position, deltaTime * 16f);
                player.SimulatedPosition += player.NetworkPlayerInput.GetMovementVector() * deltaTime * 256;
                Debug.WriteLine(player.Position);
                Networking.ClientData.Players[peerId] = player;
                if (Networking.MyPeer.RemoteId == peerId)
                {
                    _cameraPosition = Vector2.Lerp(_cameraPosition, player.Position, deltaTime * 4f);
                }
            }

            _ping.Text = Networking.MyPeer.Ping.ToString();
        }

        base.Update(gameTime);
    }

    protected override async void OnExiting(object sender, EventArgs args)
    {
        await Task.Run(Networking.StopServer).ContinueWith(async _ =>
        {
            Debug.WriteLine($"{DateTime.Now:T} - Server shut down.");
            await Task.Delay(25);
            base.OnExiting(sender, args);
            await Task.Delay(25);
            Environment.Exit(Environment.ExitCode);
        });
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        if (Networking.ClientData != null)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            foreach (var player in Networking.ClientData.Players)
            {
                _spriteBatch.Draw(playerTexture, new Rectangle(
                    WorldToScreenPosition(player.Value.SimulatedPosition), 
                    new Point(64, 64)), 
                    null, 
                    Color.White, 
                    0, 
                    new Vector2(0.5f, 0f),
                    SpriteEffects.None, 
                    0);
            }

            _spriteBatch.End();
        }

        UI.Render();

        base.Draw(gameTime);
    }

    private Point WorldToScreenPosition(Vector2 input)
    {
        return ((input + new Vector2(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight) / 2f) - _cameraPosition).ToPoint();
    }
}