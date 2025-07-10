using BetterSpritebatch;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Data;
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
        base.Initialize();
    }

    Texture2D _square;

    protected override void LoadContent()
    {
        _batcher = new Batcher(GraphicsDevice, Content);
        _colors = typeof(Color).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Select(x => (Color)x.GetValue(null))
            .ToArray();
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        if(Random.Shared.Next() % 30 == 0)
        {
            Point size = new Point(Random.Shared.Next(10, 100), Random.Shared.Next(10, 100));
            Texture2D texture = new Texture2D(GraphicsDevice, size.X, size.Y);
            Color[] data = new Color[size.X * size.Y];
            data.AsSpan().Fill(_colors[Random.Shared.Next(_colors.Length)]);
            texture.SetData(data);
            Vector2 position = new Vector2(Random.Shared.Next(0, GraphicsDevice.Viewport.Width - size.X), Random.Shared.Next(0, GraphicsDevice.Viewport.Height - size.Y));

            _textures.Add((texture, position));
        }

        base.Update(gameTime);
    }

    private Color[] _colors;
    private List<(Texture2D, Vector2)> _textures = [];

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        
        foreach(var (t, p) in _textures)
        {
            _batcher.Draw(t, p)
                .Tint(Color.White);
        }

        _batcher.Submit();

        base.Draw(gameTime);
    }
}
