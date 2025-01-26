using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;

namespace BetterSpritebatch;

public struct Sprite
{
    public Texture2D Texture;
    public Rectangle Source;
    public float Depth;


    void M(Batcher b)
    {
        b.Tint().G().G().G();
    }
}