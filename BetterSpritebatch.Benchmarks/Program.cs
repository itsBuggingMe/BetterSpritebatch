using BenchmarkDotNet;
using BenchmarkDotNet.Running;

namespace BetterSpritebatch.Benchmarks;

public class Program
{
    private Batcher _batcher;
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<Program>();
    }

    public void Shuffle()
    {

    }
}
