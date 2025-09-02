using System.Diagnostics.CodeAnalysis;
using Godot;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Mime;
using KGySoft.Drawing;
using KGySoft.Drawing.Imaging;
using Bitmap = System.Drawing.Bitmap;

public class QuantizedImage : ATexProvider
{
    private int _paletteSize;
    private Godot.Image _image;

    public QuantizedImage(Godot.Image image, int paletteSize)
    {
        this._paletteSize = paletteSize;
        this._image = image;
    }

    [SuppressMessage("Interoperability", "CA1416:Valider la compatibilité de la plateforme")]
    public override void Create()
    {
        Bitmap bmp;
        using var ms = new MemoryStream(_image.GetData());
        bmp = new Bitmap(ms);
        bmp.Quantize(PredefinedColorsQuantizer.Argb8888());
        throw new System.NotImplementedException("Not finished yet");
        //_image.SetData();
    }

    public override Godot.Image Texture => _image;

    public override string Name => _image.ResourceName;
}