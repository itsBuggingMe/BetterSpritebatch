using Microsoft.Xna.Framework;
using System;
using System.Runtime.InteropServices;

namespace BetterSpritebatch;

/// <summary>
/// Packs rectangles into larger rectangles
/// </summary>
/// <remarks>Based off of <i>https://www.researchgate.net/publication/221049934_A_Skyline-Based_Heuristic_for_the_2D_Rectangular_Strip_Packing_Problem</i></remarks>
internal class SkylinePacker
{
    private int _width, _height;
    private readonly Func<Point, Point> _resizeStrat;
    private readonly List<SkylineNode> _skyline = [];

    public SkylinePacker(int maxWidth, int maxHeight, Func<Point, Point> resizingStrategy)
    {
        _width = maxWidth;
        _height = maxHeight;
        _resizeStrat = resizingStrategy;
        _skyline.Add(new SkylineNode(0, maxWidth));
    }

    public Point Pack(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        // we move from top to bottom
        // smaller -> better
        int bestScore = int.MaxValue;
        int bestScoreIndex = -1;
        Point bestScorePosition = default;

        Span<SkylineNode> skyline = CollectionsMarshal.AsSpan(_skyline);

        int left = 0;
        for (int i = 0; i < skyline.Length; left += skyline[i].Width, i++)
        {
            SkylineNode node = skyline[i];

            if (node.Width >= width && node.Down < bestScore)
            {
                bestScore = node.Down;
                bestScoreIndex = i;
                bestScorePosition = new Point(left, node.Down);
            }
        }

        ref SkylineNode winner = ref skyline[bestScoreIndex];

        if(winner.Width == width)
        {// perfect fit!
            winner.Down = bestScorePosition.Y;

            TryMergeNodes(bestScoreIndex + 1);
            TryMergeNodes(bestScoreIndex);
        }
        else
        {
            int newHeight = bestScorePosition.Y + height;
            if(newHeight > _height)
            {
                int oldWidth = _width;
                var newSize = _resizeStrat.Invoke(new Point(_width, _height));
                _width = newSize.X;
                _height = newSize.Y;
                _skyline.Add(new SkylineNode(0, _width - oldWidth));
                return Pack(width, height);
            }

            // old winner node on the right
            winner = new SkylineNode(winner.Down, winner.Width - width);

            // place into far left side
            _skyline.Insert(bestScoreIndex, new SkylineNode(newHeight, width));
            // ref winner now outdated

            TryMergeNodes(bestScoreIndex);
        }

        return bestScorePosition;
    }

    /// <summary>
    /// Merges node at index and index - 1
    /// </summary>
    private bool TryMergeNodes(int index)
    {
        Span<SkylineNode> skylineNodes = CollectionsMarshal.AsSpan(_skyline);
        if(index >= skylineNodes.Length)
            return false;
        if (index == 0)
            return false;

        ref var left = ref skylineNodes[index - 1];
        ref var right = ref skylineNodes[index];

        if (left.Down == right.Down)
        {
            left.Width += right.Width;
            _skyline.RemoveAt(index);
            // span outdated
            return true;
        }

        return false;
    }

    private struct SkylineNode(int down, int width)
    {
        public int Down = down;
        public int Width = width;
    }
}