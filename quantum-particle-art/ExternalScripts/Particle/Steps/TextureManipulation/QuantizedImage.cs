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
using System.Threading.Tasks;
using KGySoft.CoreLibraries;
using KGySoft.Drawing;
using KGySoft.Drawing.Imaging;
using UnityEngine;
using UnityEngine.ExternalScripts.Particle.Simulation;
using Bitmap = System.Drawing.Bitmap;
using Color = Godot.Color;
using Color32 = KGySoft.Drawing.Imaging.Color32;
using Image = Godot.Image;
using Mathf = Godot.Mathf;
using Vector2 = UnityEngine.Vector2;

public class QuantizedImage : ATexProvider, ISpecyPicker, IColorPicker
{
	private int _paletteSize;
	private Godot.Image _image;
	private Color32[] _colors;
	private Dictionary<Color32, int> _mapBack;
	private Vector2I _targetRes;
	private IColorPicker _colorPickerImplementation;

	public QuantizedImage(Image image, int paletteSize, Vector2I targetRes)
	{
		this._paletteSize = paletteSize;
		this._image = image;
		_targetRes = targetRes;
	}


	[SuppressMessage("Interoperability", "CA1416:Valider la compatibilité de la plateforme")]
	public override bool Create()
	{
		while (_image == null || _image.IsEmpty())
		{
			Debug.Log("Waiting for image to be loaded...");
			return false;
		}
		Debug.Log("Image loaded, preparing it...");
		_image.Resize(_targetRes.X, _targetRes.Y, Image.Interpolation.Trilinear);
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
		HashSet<Color32> palette = new(_paletteSize);
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
					palette.Add(new Color32(c.R, c.G, c.B));
			}
		} while (row.MoveNextRow());

		_image = Image.CreateFromData(_image.GetWidth(), _image.GetHeight(), false, format, original);
		_colors = palette.ToArray();
		_colorPickerImplementation = ColorPicker.FromScheme(ColorScheme);
		_mapBack = palette.Select((v, i) => (v, i)).ToDictionary(x => x.v, x => x.i);
		return true;
	}

	private Color[] ColorScheme => _colors.Select(c => new Color(c.R / 255f, c.G / 255f, c.B / 255f)).ToArray();

	public override Godot.Image Texture => _image;

	public override string Name => _image.ResourceName;

	public int SpeciyIndex(Vector2 position)
	{
		var color = _image.GetPixel(Mathf.FloorToInt(position.x * _image.GetWidth()), Mathf.FloorToInt(position.y * _image.GetHeight()));
		var col32 = new Color32((byte)(color.R * 255), (byte)(color.G * 255), (byte)(color.B * 255));
		if (_mapBack.TryGetValue(col32, out var idx))
			return idx;
		throw new KeyNotFoundException("Speciied color in image not found in palette");
	}

	public Color GetColor(Particle particle, int totalNbSpecies)
	{
		return _colorPickerImplementation.GetColor(particle, totalNbSpecies);
	}
}
