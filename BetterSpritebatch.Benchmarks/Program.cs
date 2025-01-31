using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace BetterSpritebatch.Benchmarks;

public class Program
{
    private Batcher _batcher;
    private Texture2D _dummyTexture;

    static void Main()
    {
        BenchmarkRunner.Run<Program>();
    }

    [GlobalSetup]
    public void Setup()
    {
        _batcher = new((GraphicsDevice)RuntimeHelpers.GetUninitializedObject(typeof(GraphicsDevice)));
        _dummyTexture = (Texture2D)RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
    }

    [Benchmark]
    public void Draw()
    {
        _batcher.Draw(_dummyTexture, Vector2.Zero);
    }

    [Benchmark]
    public void Scale()
    {
        _batcher.Draw(_dummyTexture, Vector2.Zero)
            .Scale(Vector2.One);
    }

    [Benchmark]
    public void Rotate()
    {
        _batcher.Draw(_dummyTexture, Vector2.Zero)
            .Rotate(1f);
    }
}
