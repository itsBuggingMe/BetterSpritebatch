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
    private SpriteBatch _sb;
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        _sb = new(GraphicsDevice);
        base.Initialize();
    }

    Texture2D _square;
    SpriteEffect _se;
    protected override void LoadContent()
    {
        _square = new Texture2D(GraphicsDevice, 1, 1);
        _square.SetData([Color.White]);

        _batcher = new Batcher(GraphicsDevice, Content);
        _colors = typeof(Color).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Select(x => (Color)x.GetValue(null))
            .ToArray();

        _se = new(GraphicsDevice);
    }

    Random r = new();

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        InputHelper.TickUpdate(true);
        if(InputHelper.DeltaScroll != 0)
        {
            Point size = new Point(r.Next(10, 100),r.Next(10, 100));
            Texture2D texture = new Texture2D(GraphicsDevice, size.X, size.Y);

            Color[] data = new Color[size.X * size.Y];
            data.AsSpan().Fill(_colors[r.Next(_colors.Length)]);
            texture.SetData(data);
            Vector2 position = new Vector2(r.Next(0, GraphicsDevice.Viewport.Width - size.X), r.Next(0, GraphicsDevice.Viewport.Height - size.Y));

            _textures.Add((texture, position));
        }

        base.Update(gameTime);
    }

    private Color[] _colors;
    private readonly List<(Texture2D, Vector2)> _textures = [];

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
       
        
        if(InputHelper.Down(MouseButton.Right))
        {
            //new QuadRenderer(Content, GraphicsDevice).Draw(_batcher._atlas, default);
            //return;
            _sb.Begin();
            _sb.Draw(_batcher._atlas, new Vector2(0, 0), Color.White);
            _sb.End();
        }
        else
        {
            foreach (var (t, p) in _textures)
            {
                _batcher.Draw(t, p)
                    .Tint(Color.White);
            }

            _batcher.Submit();
        }

        base.Draw(gameTime);
    }
}
