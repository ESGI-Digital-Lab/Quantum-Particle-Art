using Godot;
using UnityEngine;
using Color = UnityEngine.Color;
using Texture = Godot.Texture;

public abstract class ATexProvider : ScriptableObject
{
    public abstract void Create();
    public abstract Texture Texture { get; }
    public abstract byte[] GetPixels();
    public virtual string Name => name;
}

public class CanvasPixels : ATexProvider
{
    [SerializeField, Range(1, 25),
     Tooltip(
         "Every steps doubles the side of texture (and quadruple total size) : 10=>1024x1024, 13=>4096x4096 (size of first ever yellow green image generated using this technique), 15 => 32768x32768...")]
    private int _powerOf2 = 8;

    private Vector2Int _size;
    [SerializeField] private Color _color;
    private Godot.ImageTexture _texture;

    public CanvasPixels(int x, int y, Color color) : this(new Vector2Int(x, y), color) { }
    public CanvasPixels(Vector2Int size, Color color)
    {
        _size = size;
        _color = color;
    }

    private byte[] pixels;

    public override void Create()
    {
        pixels = new byte[_size.x * _size.y * 4];
        var r = (byte)(_color.r * 255);
        var g = (byte)(_color.g * 255);
        var b = (byte)(_color.b * 255);
        var a = (byte)(_color.a * 255);
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = r;
            pixels[i + 1] = g;
            pixels[i + 2] = b;
            pixels[i + 3] = a;
        }

        var img = Image.CreateFromData(_size.x, _size.y, false, Image.Format.Rgba8, pixels);
        _texture.SetImage(img);
    }

    public override Texture Texture
    {
        get => _texture;
    }

    public override byte[] GetPixels()
    {
        return pixels;
    }
}