using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DefaultNamespace.Tools;
using Godot;
using NaughtyAttributes;
using UnityEngine;
using Color = UnityEngine.Color;
using Mathf = UnityEngine.Mathf;
using Vector2 = UnityEngine.Vector2;

public class WriteToTex : ParticleStep
{
    private float _viewSize;

    public float ViewSize
    {
        get => _viewSize;
        set => _viewSize = value;
    }
    private Saver _saver;

    public WriteToTex(Sprite2D renderer, float viewSize, Saver saver = null)
    {
        _renderer = renderer;
        _saver = saver;
        _viewSize = viewSize;
    }

    private Sprite2D _renderer;
    private ATexProvider _texProvider;
    //[SerializeField] private bool _enableDistorsion = true;


    private Image _toSaveImage;
    private Image _drawingImage;
    private ImageTexture _toSave;
    private ImageTexture _drawing;
    private Color[] pixels;

    public void RefreshTex()
    {
        _toSave.SetImage(_toSaveImage);
        _drawing.SetImage(_drawingImage);
        _renderer.Texture = _toSave;
    }

    public override async Task Init(WorldInitializer init)
    {
        await base.Init(init);
        _texProvider = init.Texture;
        //We need to have the tex before initializing the saving
        var original = _texProvider.Texture;
        _toSaveImage = original.Duplicate() as Image;
        _drawingImage = Image.CreateEmpty(original.GetWidth(), original.GetHeight(), false, original.GetFormat());
        _drawingImage.Fill(Color.black);
        _toSave = ImageTexture.CreateFromImage(_toSaveImage);
        _drawing = ImageTexture.CreateFromImage(_drawingImage);
        var stretch =  _viewSize / _drawingImage.GetHeight();
        //We keep aspect ratio of image (defined in the texture and not the scale of the sprited2D) but we make sure it takes full space on height axis
        _renderer.Scale = new Vector2(stretch, stretch);
        if (_saver != null)
        {
            _saver.Init(_toSaveImage, _texProvider.Name + "_" + init.Init.Rules.Name);
        }
        RefreshTex();
    }

    public override void Release()
    {
        base.Release();
        if (_saver != null)
            _saver.SaveTexToDisk();
    }

    public override async Task HandleParticles(ParticleWorld entry, float delay)
    {
        await Draw(entry);
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
                _drawingImage.SetPixel(coords.x, coords.y, line.Color);
                //This is results overwriting the base texture
                _toSaveImage.SetPixel(coords.x, coords.y, line.Color);
            }
        }

        entry.Drawer.Clear();
        RefreshTex();
        return Task.CompletedTask;
    }

    private Vector2Int ToPixelCoord(Vector2 coord)
    {
        var x = Mathf.RoundToInt(coord.x * (_drawing.GetWidth() - 1));
        var y = Mathf.RoundToInt(coord.y * (_drawing.GetHeight() - 1));
        return new Vector2Int(x, y);
    }


    protected void GetFrame(ParticleWorld entry, in float[] buffer)
    {
        //pixels = _toSave.GetPixels();
        //for (int i = 0; i < pixels.Length; i++)
        //{
        //    var bufferIndex = i * _nbColorChannels;
        //    buffer[bufferIndex] = pixels[i].r;
        //    if (_nbColorChannels > 1)
        //        buffer[bufferIndex + 1] = pixels[i].g;
        //    if (_nbColorChannels > 2)
        //        buffer[bufferIndex + 2] = pixels[i].b;
        //    if (_nbColorChannels > 3)
        //        buffer[bufferIndex + 3] = pixels[i].a;
        //}
    }
}