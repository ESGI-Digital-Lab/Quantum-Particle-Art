using System;
using Godot;

namespace UnityEngine.ExternalScripts.Particle.Steps.TextureManipulation.Brushes;

[GlobalClass]
public partial class BrushList : Resource, IBrushPicker
{
    [Export] private Godot.Collections.Array<CompressedTexture2D> _brushes;
    [Export(PropertyHint.Range, "0,1000,1")]
    private int _maxStrokeSize = 10;

    [Export(PropertyHint.Range, "0,1000,1")]
    private float _liveBrushSizeDivider = 10;

    [Export] private float _relativeRandomBrushOffset = 0.1f;
    private Brush[] _brushesCache;
    private CompressedTexture2D DefaultBrushTexture => _brushes[0];
    public int MaxStrokeSize => _maxStrokeSize;

    public Brush CreateDetailled() => BuildBrush(0, true);

    public Brush GetBrush(int specy)
    {
        return _brushesCache[specy];
    }

    public void Init(int maxNbSpecies) => _brushesCache = BuildCache(maxNbSpecies);
    private Brush[] BuildCache(int size)
    {
        var cache = new Brush[size];
        for (int i = 0; i < cache.Length; i++)
        {
            cache[i] = BuildBrush(i%_brushes.Count);
        }

        return cache;
    }

    private Brush BuildBrush(int i, bool detailled = false)
    {
        var image = _brushes[i];
        var brushName = image.FileName(); //Last part without extension
        var smallBrushSize = Math.Max(1, (int)(_maxStrokeSize / (detailled ? 1f : _liveBrushSizeDivider)));
        if (smallBrushSize > 2)
            Debug.LogWarning("Small brush size for live drawing is " + smallBrushSize +
                             ", if performance is low consider increasing the live brush size divider from " +
                             _liveBrushSizeDivider + " to reach something closer to 1");
        return new Brush(image.GetImage(), smallBrushSize, _relativeRandomBrushOffset, brushName);
    }
}