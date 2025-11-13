using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using UnityEngine;
using UnityEngine.Assertions;
using Color = UnityEngine.Color;

public class Brush : IBrushPicker
{
    private Image _brush;
    private int _size;
    private float _randomOffset = 0.1f;
    private string _name;


    public Brush(Image brush, int size, float randomOffset, string name)
    {
        _size = size;
        _brush = brush;
        _randomOffset = randomOffset;
        _brush.Resize(size, size);
        _name = name;
    }

    public override string ToString()
    {
        return (string.IsNullOrEmpty(_name) ? "" : _name + "_") + _size +
               (_randomOffset > 0 ? "_" + _randomOffset : "");
    }

    public void DrawWithBrush(Image target, IEnumerable<Vector2Int> points, Color baseColor,
        float strokeRelativeSize = 1f)
    {
        DrawWithBrush(target, points.Select(p => new IBrushPicker.StrokePoint(p, baseColor, strokeRelativeSize)), null);
    }

    public void DrawWithBrush(Image target, IEnumerable<IBrushPicker.StrokePoint> points, object[][] locks = null)
    {
        int width = target.GetWidth();
        int height = target.GetHeight();
        Assert.IsTrue(locks == null,
            "Approach isn't correct, go single threaded only, we would need to use a buffer with thread safe access and then flush it to the image\n");
        Assert.IsTrue(locks == null || locks.Length == height && locks[0].Length == width,
            $"Locks size {(locks?.Length, locks?[0].Length)} does not match target size {(width, height)}");

        void Body(IBrushPicker.StrokePoint point, bool threadSafe)
        {
            var finalWidth = (int)(point.relativeSize * _brush.GetWidth() / 2);
            var bColor = point.color;
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
                            var color = ComputeColorFromBrush(x + finalWidth, y + finalWidth, bColor);
                            if (color.A > .001f)
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

    private Godot.Color ComputeColorFromBrush(int texX, int texY, Color lineColor)
    {
        var rdSize = (int)(_size * _randomOffset);
        texX += UnityEngine.Random.Range(-rdSize, rdSize);
        texY += UnityEngine.Random.Range(-rdSize, rdSize);
        texX = Godot.Mathf.Clamp(texX, 0, _brush.GetWidth() - 1);
        texY = Godot.Mathf.Clamp(texY, 0, _brush.GetHeight() - 1);
        var brushColor = _brush.GetPixel(texX, texY);
        //We asssume brush is black on transparent, we want the fully black part to display the base color untouched, the brighter part (up to transparent) to dim a bit the base color
        return new Godot.Color((1f - brushColor.R) * lineColor.r, (1f - brushColor.G) * lineColor.g,
            (1f - brushColor.B) * lineColor.b,
            brushColor.A * lineColor.a);
    }

    public Brush GetBrush(int specy) => this;
    public void Init(int maxNbSpecies) { }
}