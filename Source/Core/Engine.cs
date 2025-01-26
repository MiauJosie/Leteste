using System;
using Fishbowcat.Levels;
using Leteste.Entities;
using Leteste.Levels;
using Leteste.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Leteste.Core;

public class Engine : Game
{

    // Virtual resolution - perfect 16:9 aspect ratio
    private const int VIRTUAL_WIDTH = 320;
    private const int VIRTUAL_HEIGHT = 180;

    // Default to 4x scale (720p)
    private const int SCALE = 4;
    private const int PreferredBackBufferWidth = VIRTUAL_WIDTH * SCALE;
    private const int PreferredBackBufferHeight = VIRTUAL_HEIGHT * SCALE;

    private GraphicsDeviceManager _graphics;
    private RenderTarget2D _renderTarget;

    private Level currentLevel;

    public Engine()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        Window.AllowUserResizing = true;

        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0f / 60.0f);

        // Default window size
        _graphics.PreferredBackBufferWidth = PreferredBackBufferWidth;
        _graphics.PreferredBackBufferHeight = PreferredBackBufferHeight;
        _graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        _renderTarget = new RenderTarget2D(
            GraphicsDevice,
            VIRTUAL_WIDTH,
            VIRTUAL_HEIGHT
        );

        Globals.GraphicsDevice = GraphicsDevice;
        Globals.SpriteBatch = new SpriteBatch(GraphicsDevice);
        Globals.Content = Content;

        currentLevel = new Level();

        var player = new Player(currentLevel, new Vector2(100, 100));
        currentLevel.AddActor(player);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        TileMap.LoadFromCSV(currentLevel, "../../../Content/Maps/MAP.csv", "tileset_atlas", true);
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        if (keyboard.IsKeyDown(Keys.F11))
        {
            _graphics.IsFullScreen = !_graphics.IsFullScreen;
            if (_graphics.IsFullScreen)
            {
                _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            }
            else
            {
                _graphics.PreferredBackBufferWidth = PreferredBackBufferWidth;
                _graphics.PreferredBackBufferHeight = PreferredBackBufferHeight;
            }
            _graphics.ApplyChanges();
        }

        if (keyboard.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        currentLevel.Update();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Draw game to render target at virtual resolution
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(Color.CornflowerBlue);

        Globals.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        currentLevel.Draw();
        Globals.SpriteBatch.End();

        // Draw render target to window with scaling
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        var scale = CalculateScale();
        var position = CalculatePosition(scale);

        Globals.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        Globals.SpriteBatch.Draw(
            _renderTarget,
            position,
            null,
            Color.White,
            0f,
            Vector2.Zero,
            scale,
            SpriteEffects.None,
            0f
        );
        Globals.SpriteBatch.End();

        base.Draw(gameTime);
    }

    private Vector2 CalculateScale()
    {
        float scaleX = (float)GraphicsDevice.Viewport.Width / VIRTUAL_WIDTH;
        float scaleY = (float)GraphicsDevice.Viewport.Height / VIRTUAL_HEIGHT;
        float scale = Math.Min(scaleX, scaleY);
        return new Vector2(scale);
    }

    private Vector2 CalculatePosition(Vector2 scale)
    {
        float x = (GraphicsDevice.Viewport.Width - (VIRTUAL_WIDTH * scale.X)) / 2f;
        float y = (GraphicsDevice.Viewport.Height - (VIRTUAL_HEIGHT * scale.Y)) / 2f;
        return new Vector2(x, y);
    }
}
