using System;
using System.Collections.Generic;
using Godot;

namespace UnityEngine.ExternalScripts.Particle.Steps.TextureManipulation.Brushes;

[GlobalClass]
public partial class BrushList : Resource, IBrushPicker
{
    [ExportCategory("Look")]
    [Export] private bool _shuffle;
    [Export] private Godot.Collections.Array<CompressedTexture2D> _brushes;
    [ExportCategory("Measures")]
    [Export(PropertyHint.Range, "1,1000,1")]
    private int _minStrokeSize = 1;
    [Export(PropertyHint.Range, "1,1000,1")]
    private int _maxStrokeSize = 10;
    [Export]
    private float _minSpaceBeetweenStrokes = .25f;

    [Export(PropertyHint.Range, "0,1000,1")]
    private float _lateBrushSizeMultiplier = 10;

    [Export] private float _relativeRandomBrushOffset = 0.1f;
    private Brush[] _brushesCache;
    private CompressedTexture2D DefaultBrushTexture => _brushes[0];
    public int MaxStrokeSize => _maxStrokeSize;

    public Brush CreateDetailled() => BuildBrush(0, true);

    public Brush GetBrush(int specy)
    {
        return _brushesCache[specy];
    }

    public void Init(int maxNbSpecies)
    {
        _brushesCache = BuildCache(maxNbSpecies);
        foreach(var b in _brushesCache)
            b.Init(maxNbSpecies);
    }

    private Brush[] BuildCache(int size)
    {
        var pool = new List<int>(size);
        for (int i = 0; i < size; i++)
            pool.Add(i % _brushes.Count);

        var cache = new Brush[size];
        for (int i = 0; i < cache.Length; i++)
        {
            var idx = _shuffle ? UnityEngine.Random.Range(0, pool.Count) : 0;
            var brushIdx = pool[idx];
            pool.RemoveAt(idx);
            cache[i] = BuildBrush(brushIdx);
        }

        return cache;
    }

    private Brush BuildBrush(int i, bool detailled = false)
    {
        var image = _brushes[i];
        var brushName = image.FileName(); //Last part without extension
        var smallBrushSize = Math.Max(_minStrokeSize, (int)(_maxStrokeSize * (!detailled ? 1f : _lateBrushSizeMultiplier)));
        //if (smallBrushSize > 2)
        //    Debug.LogWarning("Small brush size for live drawing is " + smallBrushSize +
        //                     ", if performance is low consider increasing the live brush size divider from " +
        //                     _lateBrushSizeMultiplier + " to reach something closer to 1");
        return new Brush(image.GetImage(), _minStrokeSize,smallBrushSize, _relativeRandomBrushOffset,_minSpaceBeetweenStrokes, brushName);
    }
}