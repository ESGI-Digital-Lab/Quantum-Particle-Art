using System;
using System.Collections.Generic;
using Godot;
using UnityEngine;
using Color = UnityEngine.Color;

public class Brush
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
        DrawWithBrush(target, points, _ => baseColor, strokeRelativeSize);
    }


    public void DrawWithBrush(Image target, IEnumerable<Vector2Int> points, Func<int, Color> baseColor,
        float strokeRelativeSize = 1f)
    {
        int width = target.GetWidth();
        int height = target.GetHeight();
        var finalWidth = (int)(strokeRelativeSize * _brush.GetWidth() / 2);
        int i = 0;
        foreach (var coords in points)
        {
            var bColor = baseColor(i++);
            for (int x = -finalWidth; x <= finalWidth; x++)
            {
                for (int y = -finalWidth; y <= finalWidth; y++)
                {
                    if (x * x + y * y <= finalWidth * finalWidth)
                    {
                        var bigX = coords.x + x;
                        var bigY = coords.y + y;
                        if (bigX >= 0 && bigX < width && bigY >= 0 &&
                            bigY < height)
                        {
                            var color = ComputeColorFromBrush(x + finalWidth, y + finalWidth, bColor);
                            if (color.A > .001f)
                            {
                                //This is raw results on a blank tex
                                target.SetPixel(bigX, bigY, color);
                            }
                        }
                    }
                }
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
}