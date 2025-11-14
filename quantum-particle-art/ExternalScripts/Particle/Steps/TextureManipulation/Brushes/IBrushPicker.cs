using System;
using UnityEngine;
using Color = UnityEngine.Color;

public interface IBrushPicker
{
    public record struct StrokePoint(Vector2Int coords, Color color, float relativeSize);
    public void Init(int maxNbSpecies);
    public Brush GetBrush(int specy);
}