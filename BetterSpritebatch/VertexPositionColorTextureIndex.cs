using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace BetterSpritebatch;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexPositionColorTextureIndex : IVertexType
{
    //8
    public Vector2 Position;
    //8
    public Vector2 TextureCoordinate;

    //4
    public Color Color;
    //2
    public short TextureIndex;

    public VertexDeclaration VertexDeclaration => _vertexDeclaration;
    private static readonly VertexDeclaration _vertexDeclaration = new VertexDeclaration(
        new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0), 
        new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        new VertexElement(20, VertexElementFormat.Short2, VertexElementUsage.TextureCoordinate, 1)
        );


    public VertexPositionColorTextureIndex(Vector2 position, Color color, Vector2 textureCoordinate, short index)
    {
        Position = position;
        Color = color;
        TextureCoordinate = textureCoordinate;
        TextureIndex = index;
    }

    public override string ToString() => $"{{ Position: {Position} Color: {Color} TextureCoordinate: {TextureCoordinate} TextureIndex: {TextureIndex} }}";
}