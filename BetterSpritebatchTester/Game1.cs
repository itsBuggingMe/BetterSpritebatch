using BetterSpritebatch;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Reflection;

namespace BetterSpritebatchTester;
public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private Batcher _batcher;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    Texture2D _square;

    protected override void LoadContent()
    {
        _batcher = new Batcher(GraphicsDevice);
        _colors = typeof(Color).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Select(x => (Color)x.GetValue(null))
            .ToArray();

        _square = new Texture2D(GraphicsDevice, 1, 1);
        _square.SetData([Color.White]);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // TODO: Add your update logic here

        base.Update(gameTime);
    }

    private Color[] _colors;

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        
        for(int i = 0; i < 100; i++)
        {
            _batcher.Draw(_square, new Vector2(Random.Shared.Next(0, 100), Random.Shared.Next(0, 100)))
                .Scale(2);
        }

        _batcher.Submit();

        base.Draw(gameTime);
    }
}
