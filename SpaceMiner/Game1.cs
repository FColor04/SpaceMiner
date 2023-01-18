using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    private Texture2D groundTexture;
    private Texture2D treeTexture;
    private NetworkPlayerInput _lastInput;
    private Vector2 _cameraPosition;
    private TextBox _ping;
    public const int Scale = 2;
    private const int Seed = 310351;
    private Random _random;
    private List<Vector2> trees = new ();
    private List<Entity> _entities = new();
    
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _random = new Random(Seed);
        for (int i = 0; i < 15; i++)
        {
            trees.Add(new Vector2(_random.NextSingle() * 640 - 320, _random.NextSingle() * 640 - 320));
        }
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
        
        var connectButton = new TextButton() {Text = "Connect", Left = 24, Top = 48, Padding = new Thickness(8)};
        connectButton.Click += (_, _) => Networking.StartClient(Networking.GlobalAddress);
        UI.Desktop.Widgets.Add(connectButton);
        
        _ping = new TextBox(){Top = 0, Left = _graphics.PreferredBackBufferWidth - 48, Text = "0"};
        UI.Desktop.Widgets.Add(_ping);
        
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        
        playerTexture = Content.Load<Texture2D>("Player");
        groundTexture = Content.Load<Texture2D>("Ground");
        treeTexture = Content.Load<Texture2D>("Tree");
        
        
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
                var movementVector = (peerId == Networking.MyPeer.Id ? playerInput : player.NetworkPlayerInput).GetMovementVector();
                player.SimulatedPosition += movementVector * deltaTime * 256;

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

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        
        for (int x = -groundTexture.Width * Scale; x < _graphics.PreferredBackBufferWidth + groundTexture.Width * Scale; x += groundTexture.Width * Scale)
        {
            for (int y = -groundTexture.Height * Scale; y < _graphics.PreferredBackBufferHeight + groundTexture.Height * Scale; y += groundTexture.Height * Scale)
            {
                var tilePosition = new Vector2(x - _cameraPosition.X % (groundTexture.Width * Scale), y - _cameraPosition.Y % (groundTexture.Height * Scale));
                _spriteBatch.Draw(groundTexture, new Rectangle(tilePosition.ToPoint(), new Point(groundTexture.Width * Scale, groundTexture.Height * Scale)), Color.White);
            }
        }
        
        if (Networking.ClientData != null)
        {
            _entities = Networking.ClientData.Players.Select(player => new Entity()
            {
                texture = playerTexture,
                position = player.Value.SimulatedPosition,
                origin = new Vector2(playerTexture.Width / 2f, playerTexture.Height)
            }).Concat(trees.Select(tree => new Entity()
            {
                texture = treeTexture,
                position = tree,
                origin = new Vector2(treeTexture.Width / 2f, treeTexture.Height)
            })).OrderBy(entity => entity.position.Y).ToList();
            foreach (var entity in _entities)
            {
                _spriteBatch.Draw(
                    entity.texture, 
                    new Rectangle(WorldToScreenPosition(entity.position), entity.GetSize), 
                    null, 
                    Color.White, 
                    0, 
                    entity.Origin, 
                    SpriteEffects.None, 
                    0);
            }
        }
        
        _spriteBatch.End();

        UI.Render();

        base.Draw(gameTime);
    }

    private Point WorldToScreenPosition(Vector2 input)
    {
        return ((input + new Vector2(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight) / 2f) - _cameraPosition).ToPoint();
    }
}

internal struct Entity
{
    public Texture2D texture;
    public Vector2 position;
    public Point GetSize => (texture.Bounds.Size.ToVector2() * Game1.Scale).ToPoint();
    public Vector2? origin;
    public Vector2 Origin => origin ?? (GetSize.ToVector2() / 2f);
}