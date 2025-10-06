using System;
using System.Collections.Generic;
using Godot;
using UnityEngine;
using Color = UnityEngine.Color;
using Mathf = UnityEngine.Mathf;
using Vector2 = Godot.Vector2;

public static class ViewHelpers
{
    public static readonly Color SUP = Color.red;
    public static readonly Color MEA = new Color(0.5f, 0, 0);
    public static readonly Color CTR = Color.green;
    public static readonly Color TEL = Color.blue;

    public static Vector2 Pos(UnityEngine.Vector2 normalized, Node2D root)
    {
        return root.ToGlobal(new Vector2(normalized.x - .5f, normalized.y - .5f));
    }

    public static Color ColorRamp360(Particle particle)
    {
        var deg = particle.Orientation.Degrees;
        Color color = Color.black;
        if (deg > 270) //Left
            color = Color.Lerp(Color.blue, Color.white, Mathf.InverseLerp(270, 360, deg));
        else if (deg < 90) //Right
            color = Color.Lerp(Color.red, Color.white, Mathf.InverseLerp(90, 0, deg));
        else //Back
            color = Color.Lerp(Color.blue, Color.red, Mathf.InverseLerp(270, 90, deg));
        //Debug.Log($" deg {deg} => color {color}");
        return color;
    }

    public static Vector2Int ToPixelCoord(this ImageTexture _drawing, UnityEngine.Vector2 coord)
    {
        return coord.ToPixelCoord(_drawing);
    }

    public static Vector2I ToPixelCoord(this Godot.Vector2 coord, ImageTexture _drawing)
    {
        var x = Mathf.RoundToInt(coord.X * (_drawing.GetWidth() - 1));
        var y = Mathf.RoundToInt(coord.Y * (_drawing.GetHeight() - 1));
        return new Vector2I(x, y);
    }

    public static Vector2Int ToPixelCoord(this UnityEngine.Vector2 coord, ImageTexture _drawing)
    {
        var v = new Godot.Vector2(coord.x, coord.y).ToPixelCoord(_drawing);
        return new Vector2Int(v.X, v.Y);
    }
    public static System.Numerics.Vector2 ToSystemV2(this Godot.Vector2 v) => new System.Numerics.Vector2(v.X, v.Y);
    
    public static Godot.Vector2 ToGodotV2(this System.Numerics.Vector2 v) => new Godot.Vector2(v.X, v.Y);
}