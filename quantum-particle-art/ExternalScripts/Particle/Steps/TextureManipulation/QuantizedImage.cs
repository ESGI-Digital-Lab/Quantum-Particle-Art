using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices;
using KGySoft.CoreLibraries;
using KGySoft.Drawing;
using KGySoft.Drawing.Imaging;
using Bitmap = System.Drawing.Bitmap;
using Color = Godot.Color;
using Image = Godot.Image;

public class QuantizedImage : ATexProvider,IColorPicker
{
	private int _paletteSize;
	private Godot.Image _image;
	private Godot.Color[] _colors;

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

		var map = BitmapDataFactory.CreateBitmapData(new Size(_image.GetWidth(), _image.GetHeight()),
			KnownPixelFormat.Format32bppRgb);
		var row = map.FirstRow;
		do
		{
			for (int x = 0; x < row.Width; x++)
			{
				var i = (row.Index * row.Width + x) * 3;
				row.SetColor32(x, new Color32(original[i], original[i + 1], original[i + 2]));
			}
		} while (row.MoveNextRow());

		//TODO iterate through initial image to fill in the map
		map.Quantize(OptimizedPaletteQuantizer.Octree(_paletteSize));

		row = map.FirstRow;
		HashSet<Color> palette = new HashSet<Color>(_paletteSize);
		do
		{
			for (int x = 0; x < row.Width; x++)
			{
				var c = row.GetColor(x);
				var idx = (row.Index * row.Width + x) * 3;
				original[idx] = c.R;
				original[idx + 1] = c.G;
				original[idx + 2] = c.B;
				if (palette.Count < _paletteSize)
					palette.Add(new Color(c.R / 255f, c.G / 255f, c.B / 255f));
			}
		} while (row.MoveNextRow());

		_image = Image.CreateFromData(_image.GetWidth(), _image.GetHeight(), false, format, original);
		_colors = palette.ToArray();
	}

	public override Godot.Image Texture => _image;

	public override string Name => _image.ResourceName;
	public Color GetColor(Particle particle, int totalNbSpecies)
	{
		return _colors[particle.Species];
	}
}
