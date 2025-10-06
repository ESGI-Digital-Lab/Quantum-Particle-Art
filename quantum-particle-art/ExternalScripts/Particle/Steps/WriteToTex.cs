using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DefaultNamespace.Tools;
using Godot;
using NaughtyAttributes;
using UnityEngine.Assertions;
using Color = UnityEngine.Color;
using Mathf = UnityEngine.Mathf;
using Texture = Godot.Texture;
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
    private LineCollection _lineCollection;

    public WriteToTex(Sprite2D renderer, float viewSize, Saver saver, LineCollection lineCollection, Brush brush)
    {
        _renderer = renderer;
        _saver = saver;
        _viewSize = viewSize;
        _lineCollection = lineCollection;
        _brush = brush;
    }

    private Sprite2D _renderer;
    private ATexProvider _texProvider;
    //[SerializeField] private bool _enableDistorsion = true;


    private Image _toSaveImage;
    private Image _drawingImage;
    private ImageTexture _toSave;
    private ImageTexture _drawing;
    private Color[] pixels;
    private readonly Brush _brush;

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
        var original = _texProvider.Texture.Duplicate() as Image;
        _toSaveImage = original;
        _drawingImage = Image.CreateEmpty(original.GetWidth(), original.GetHeight(), false, original.GetFormat());
        _drawingImage.Fill(Color.black);
        _toSave = ImageTexture.CreateFromImage(_toSaveImage);
        _drawing = ImageTexture.CreateFromImage(_drawingImage);
        var stretch = _viewSize / _drawingImage.GetHeight();
        //We keep aspect ratio of image (defined in the texture and not the scale of the sprited2D) but we make sure it takes full space on height axis
        if (_saver != null)
        {
            _saver.Init(_toSaveImage, _texProvider.Name + "_" + init.Init.Rules.Name);
        }

        View.CallDeferred(() =>
        {
            _renderer.Scale = new Vector2(stretch, stretch);
            RefreshTex();
        });
        //Debug.Log($"WriteToTex initialized on {_toSave.GetWidth()}x{_toSave.GetHeight()} texture with stroke size {_lineRadius}");
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
        foreach (var line in _lineCollection.GetLines())
        {
            var start = line.Start.ToPixelCoord(_drawing);
            var end = line.End.ToPixelCoord(_drawing);
            var points = LineCollection.Line.GetPixels(start, end).ToArray();//One enumeration
            _brush.DrawWithBrush(_drawingImage, points, line.Color, line.RelativeWidth);
            _brush.DrawWithBrush(_toSaveImage, points, line.Color, line.RelativeWidth);
        }

        _lineCollection.Clear();
        View.CallDeferred(RefreshTex);
        return Task.CompletedTask;
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