using Microsoft.Xna.Framework;

namespace BetterSpritebatch;

/// <summary>
/// Packs rectangles into larger rectangles
/// </summary>
/// <remarks>Based off of <i>https://www.researchgate.net/publication/221049934_A_Skyline-Based_Heuristic_for_the_2D_Rectangular_Strip_Packing_Problem</i></remarks>
internal class SkylinePacker
{
    private int _width, _height;
    private readonly Func<Point, Point> _resizeStrat;
    private readonly List<int> _skyline = [];

    public SkylinePacker(int maxWidth, int maxHeight, Func<Point, Point> resizingStrategy)
    {
        _width = maxWidth;
        _height = maxHeight;
        _resizeStrat = resizingStrategy;
        _skyline.Add(_width);
    }

    public Point Pack(int width, int height)
    {
        int score = ;
        for ()
        {

        }
    }
}