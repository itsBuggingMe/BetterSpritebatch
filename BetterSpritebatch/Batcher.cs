using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;

namespace BetterSpritebatch;

public class Batcher
{
    public Batcher(GraphicsDevice graphicsDevice)
    {
        _graphics = graphicsDevice;
    }

    private GraphicsDevice _graphics;
    private VertexPositionColorTexture[] _verticies;
    private short[] _indicies;

    public Batcher Draw(Texture2D texture, Vector2 position)
    {
        throw new NotImplementedException();
    }

    public Batcher Draw(Texture2D texture, Rectangle destination)
    {
        throw new NotImplementedException();
    }

    public Batcher Rotate(float radians)
    {
        throw new NotImplementedException();
    }

    public Batcher SetSource(Rectangle sourceRectangle)
    {
        throw new NotImplementedException();
    }

    public Batcher Submit()
    {
        throw new NotImplementedException();
    }

    public Batcher FlipVertically()
    {
        throw new NotImplementedException();
    }
    public Batcher FlipHorzontally()
    {
        throw new NotImplementedException();
    }

    public Batcher SetEffect(SpriteEffects effects)
    {
        throw new NotImplementedException();
    }

    public Batcher Scale(Vector2 multipler)
    {
        throw new NotImplementedException();
    }

    public Batcher Scale(float multipler)
    {
        throw new NotImplementedException();
    }

    public Batcher Tint(Color color)
    {
        throw new NotImplementedException();
    }

    public void End()
    {
        throw new NotImplementedException();
    }
}
