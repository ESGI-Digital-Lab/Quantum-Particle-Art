using System;
using System.Diagnostics.CodeAnalysis;
using Godot;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Mime;
using System.Runtime.InteropServices;
using KGySoft.CoreLibraries;
using KGySoft.Drawing;
using KGySoft.Drawing.Imaging;
using Bitmap = System.Drawing.Bitmap;
using Image = Godot.Image;

public class QuantizedImage : ATexProvider
{
    private int _paletteSize;
    private Godot.Image _image;

    public QuantizedImage(Texture2D image, int paletteSize)
    {
        this._paletteSize = paletteSize;
        this._image = image.GetImage();
    }

    [SuppressMessage("Interoperability", "CA1416:Valider la compatibilité de la plateforme")]
    public override void Create()
    {
        var format = Image.Format.Rgb8;
        _image.Convert(format);
        var original = _image.GetData();
        using var ms = new MemoryStream(original);
        

        var map = BitmapDataFactory.CreateBitmapData(new Size(_image.GetWidth(), _image.GetHeight()),KnownPixelFormat.Format32bppRgb);
        //TODO iterate through initial image to fill in the map
        map.Quantize();
        
        //bmp.Quantize();
        var conv = new ImageConverter();
        original = (byte[])conv.ConvertTo(bmp, typeof(byte[]));
        _image = Image.CreateFromData(_image.GetWidth(), _image.GetHeight(), false, format,
            original);
        //_image.SetData();
    }

    public override Godot.Image Texture => _image;

    public override string Name => _image.ResourceName;
}