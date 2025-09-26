using System;
using System.Collections.Generic;
using Godot;
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
        return root.ToGlobal(new Vector2(normalized.x-.5f, normalized.y-.5f));
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
}