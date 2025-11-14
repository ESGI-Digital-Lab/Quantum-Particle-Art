using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using UnityEngine;
using UnityEngine.Assertions;
using Color = UnityEngine.Color;

public class Brush : IBrushPicker
{
    private Image[] _brushes;
    private int _minSize;
    private int _size;
    private float _randomOffset = 0.1f;
    private string _name;
    private float _spaceProportion;
    public Brush GetBrush(int specy) => this;

    public void Init(int maxNbSpecies)
    {
        _lastPoints = new();
    }

    public Brush(int minSize, int size, float randomOffset, float minSpace, string name, params Image[] brushes)
    {
        _minSize = minSize;
        _size = size;
        _brushes = brushes.Where(b=>b != null).ToArray();
        _randomOffset = randomOffset;
        _name = name;
        _spaceProportion = minSpace;
    }

    public override string ToString()
    {
        return (string.IsNullOrEmpty(_name) ? "" : _name + "_") + _size +
               (_randomOffset > 0 ? "_" + _randomOffset : "");
    }

    public void DrawWithBrush(Image target, IEnumerable<Vector2Int> points, Color baseColor, object key,
        float strokeRelativeSize = 1f)
    {
        DrawWithBrush(target, points.Select(p => new IBrushPicker.StrokePoint(p, baseColor, strokeRelativeSize)), key,
            null);
    }

    Dictionary<object, IBrushPicker.StrokePoint> _lastPoints = new();

    public void DrawWithBrush(Image target, IEnumerable<IBrushPicker.StrokePoint> points, object key,
        object[][] locks = null)
    {
        int width = target.GetWidth();
        int height = target.GetHeight();
        Assert.IsTrue(locks == null,
            "Approach isn't correct, go single threaded only, we would need to use a buffer with thread safe access and then flush it to the image\n");
        Assert.IsTrue(locks == null || locks.Length == height && locks[0].Length == width,
            $"Locks size {(locks?.Length, locks?[0].Length)} does not match target size {(width, height)}");

        void Body(IBrushPicker.StrokePoint point, bool threadSafe)
        {
            var relSize = point.relativeSize;
            var finalWidth = (int)Godot.Mathf.Lerp(_minSize, _size, relSize) / 2;
            var totalSize = finalWidth * 2 + 1;
            var bColor = point.color;
            if (_lastPoints.TryGetValue(key, out var last) && !Distance(last, point, finalWidth))
            {
                return;
            }

            _lastPoints[key] = point;

            for (int x = -finalWidth; x <= finalWidth; x++)
            {
                for (int y = -finalWidth; y <= finalWidth; y++)
                {
                    if (x * x + y * y <= finalWidth * finalWidth)
                    {
                        var bigX = point.coords.x + x;
                        var bigY = point.coords.y + y;
                        if (bigX >= 0 && bigX < width && bigY >= 0 &&
                            bigY < height)
                        {
                            //Manually converting UV to a raw uninterpolated pixel position
                            var u = (x + finalWidth) / (1f * totalSize);
                            var v = (y + finalWidth) / (1f * totalSize);
                            foreach (var brush in _brushes)
                            {
                                var sampledX = (int)(u * brush.GetWidth());
                                var sampledY = (int)(v * brush.GetHeight());
                                var color = ComputeColorFromBrush(sampledX, sampledY, bColor, brush);
                                if (color.A > .1f)
                                {
                                    if (threadSafe)
                                        lock (locks[bigX][bigY])
                                            target.SetPixel(bigX, bigY, color);
                                    else
                                        target.SetPixel(bigX, bigY, color);
                                }
                            }
                        }
                    }
                }
            }
        }

        if (locks != null)
        {
            Parallel.ForEach(points, point => Body(point, true));
        }
        else
        {
            foreach (var point in points)
            {
                Body(point, false);
            }
        }
    }

    private bool Distance(IBrushPicker.StrokePoint lastPoint, IBrushPicker.StrokePoint point, int dist)
    {
        return lastPoint.coords.DistanceI(point.coords) > (dist * _spaceProportion);
    }

    private Godot.Color ComputeColorFromBrush(int texX, int texY, Color lineColor,Image brush)
    {
        var rdSize = (int)(_size * _randomOffset);
        texX += UnityEngine.Random.Range(-rdSize, rdSize);
        texY += UnityEngine.Random.Range(-rdSize, rdSize);
        texX = Godot.Mathf.Clamp(texX, 0, brush.GetWidth() - 1);
        texY = Godot.Mathf.Clamp(texY, 0, brush.GetHeight() - 1);
        var brushColor = brush.GetPixel(texX, texY);
        //We asssume brush is black on transparent, we want the fully black part to display the base color untouched, the brighter part (up to transparent) to dim a bit the base color
        Godot.Color invertedBrush = new(1f - brushColor.R, 1f - brushColor.G, 1f - brushColor.B, brushColor.A);
        var brushGS = (invertedBrush.R + invertedBrush.G + invertedBrush.B) / 3f;
        var gs = new Godot.Color(brushGS, brushGS, brushGS, invertedBrush.A * brushGS);
        var baseC = new Godot.Color(lineColor.r, lineColor.g, lineColor.b, lineColor.a);
#if FALSE //With some black grain added no matter the original trait
        baseC *= brushGS * invertedBrush.A;
#else //The brush has almost no impact the stroke points are too close
        baseC.A *= brushGS * invertedBrush.A;
#endif
        //return new Godot.Color((1f - brushColor.R) * lineColor.r, (1f - brushColor.G) * lineColor.g,
        //    (1f - brushColor.B) * lineColor.b,
        //    brushColor.A * lineColor.a);
        return baseC;
        return baseC * invertedBrush;
    }
}