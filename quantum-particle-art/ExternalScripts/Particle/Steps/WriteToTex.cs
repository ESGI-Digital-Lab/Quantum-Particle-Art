using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using DefaultNamespace.Tools;
using NaughtyAttributes;
using UnityEngine;

public class WriteToTex : ASaveStep<WorldInitializer, ParticleWorld>
{
    [Header("References")] [SerializeField]
    private FilterMode _filterMode;

    [SerializeField] private Renderer _renderer;

    [Header("Settings")] [SerializeField] private bool _shouldSuperposedTrace = true;
    [SerializeField] private bool _enableDistorsion = true;
    
    private ATexProvider _texProvider;

    private Texture _baseTexture
    {
        get => _texProvider.Texture;
    }

    private Texture2D _toSave;
    private Texture2D _drawing;
    private readonly int _texProperty = Shader.PropertyToID("_MainTex");
    private readonly int _drawingProperty = Shader.PropertyToID("_drawing");
    private readonly int _offsetProperty = Shader.PropertyToID("_offset");
    private Color[] pixels;

    public override IEnumerator Init(WorldInitializer init)
    {
        _texProvider = init.Texture;
        _texProvider.Create();
        //We need to have the tex before initializing the saving
        _saver.Name = _texProvider.Name + "_" + init.Init.Rules.Name;
        if (!_enableDistorsion)
            _renderer.material.SetFloat(_offsetProperty, 0f);
        yield return base.Init(init);
        _toSave = new Texture2D(_baseTexture.width, _baseTexture.height, TextureFormat.ARGB32, false);
        var copy = _texProvider.GetPixels();
        _toSave.SetPixels(copy);
        _drawing = new Texture2D(_baseTexture.width, _baseTexture.height, TextureFormat.ARGB32, false);
        _drawing.filterMode = _filterMode;
        pixels = _drawing.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.black;
        }

        _drawing.SetPixels(pixels);
        _renderer.material.SetTexture(_texProperty, _baseTexture);
        _renderer.material.SetTexture(_drawingProperty, _drawing);
        yield break;
    }

    public override IEnumerator Step(ParticleWorld entry, float delay)
    {
        yield return HandleParticles(entry, delay);
        yield return base.Step(entry, delay);
    }

    public IEnumerator HandleParticles(ParticleWorld entry, float delay)
    {
        Draw(entry);
        //Task.Run(() => Draw(entry));
        yield break;
    }

    private Task Draw(ParticleWorld entry)
    {
        foreach (var line in entry.Drawer.GetLines())
        {
            var start = ToPixelCoord(line.Start);
            var end = ToPixelCoord(line.End);
            var points = Drawer.Line.GetPixels(start, end);
            foreach (var coords in points)
            {
                //This is raw results on a blank tex
                _drawing.SetPixel(coords.x, coords.y, line.Color);
                //This is results overwriting the base texture
                _toSave.SetPixel(coords.x, coords.y, line.Color);
            }
        }

        entry.Drawer.Clear();
        _drawing.Apply();
        _toSave.Apply();
        return Task.CompletedTask;
    }

    private Vector2Int ToPixelCoord(Vector2 coord)
    {
        var x = Mathf.RoundToInt(coord.x * _drawing.width);
        var y = Mathf.RoundToInt(coord.y * _drawing.height);
        return new Vector2Int(x, y);
    }

    protected override Vector2Int GetSize(WorldInitializer init)
    {
        return new Vector2Int(_baseTexture.width, _baseTexture.height);
    }


    protected override void GetFrame(ParticleWorld entry, in float[] buffer)
    {
        pixels = _toSave.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            var bufferIndex = i * _nbColorChannels;
            buffer[bufferIndex] = pixels[i].r;
            if (_nbColorChannels > 1)
                buffer[bufferIndex + 1] = pixels[i].g;
            if (_nbColorChannels > 2)
                buffer[bufferIndex + 2] = pixels[i].b;
            if (_nbColorChannels > 3)
                buffer[bufferIndex + 3] = pixels[i].a;
        }
    }
}