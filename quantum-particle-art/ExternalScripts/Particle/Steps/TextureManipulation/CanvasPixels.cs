using System;
using System.Threading.Tasks;
using Godot;
using UnityEngine;
using UnityEngine.Assertions;
using Color = Godot.Color;
using Mathf = Godot.Mathf;

public class CanvasPixels : ATexProvider
{
    [SerializeField, Range(1, 25),
     Tooltip(
         "Every steps doubles the side of texture (and quadruple total size) : 10=>1024x1024, 13=>4096x4096 (size of first ever yellow green image generated using this technique), 15 => 32768x32768...")]
    private int _powerOf2 = 8;

    private Vector2I _size;
    [SerializeField] private Color _color;
    private Godot.Image _texture;

    public CanvasPixels(int x, int y, Color color) : this(new Vector2I(x, y), color)
    {
    }

    public CanvasPixels(Vector2I size, Color color)
    {
        _size = size;
        _color = color;
    }

    private byte[] pixels;
    private string name = null;

    public override string Name => name ??=
        ColorString(_color) + (_size.X != _size.Y ? $"{ResString(_size.X)}x{ResString(_size.Y)}" : ResString(_size.X));

    private string ColorString(Color c)
    {
        foreach (var namedColor in typeof(Colors).GetProperties())
        {
            if (!namedColor.CanWrite && namedColor.PropertyType == typeof(Color))
            {
                var stat = namedColor.GetValue(null);
                if (stat != null && (Color)stat == c)
                    return namedColor.Name;
            }
        }

        return $"R{(int)(c.R * 255)}G{(int)(c.G * 255)}B{(int)(c.B * 255)}A{(int)(c.A * 255)}";
    }

    private string ResString(int value)
    {
        if (value < 1 << 12 || !UnityEngine.Mathf.IsPowerOfTwo(value)) return value.ToString();
        for (int i = 12; i <= 24; i ++)
            if (value == (1 << i))
                return (1 << (i - 10)) + "K";
        return value.ToString();
    }
    

    public override Task Create()
    {
        Assert.IsTrue(_size.X > 0 && _size.Y > 0, "Size must be positive");
        pixels = new byte[_size.X * _size.Y * 4];
        var r = (byte)(_color.R * 255);
        var g = (byte)(_color.G * 255);
        var b = (byte)(_color.B * 255);
        var a = (byte)(_color.A * 255);
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = r;
            pixels[i + 1] = g;
            pixels[i + 2] = b;
            pixels[i + 3] = a;
        }

        _texture = Image.CreateFromData(_size.X, _size.Y, false, Image.Format.Rgba8, pixels);
        return Task.CompletedTask;
    }

    public override Image Texture
    {
        get => _texture;
    }

}