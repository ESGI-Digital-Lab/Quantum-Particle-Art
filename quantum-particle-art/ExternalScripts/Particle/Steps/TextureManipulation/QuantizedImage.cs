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
	private Godot.Image _sourceImage;
	private Godot.Image _image;
	private Color32[] _colors;
	private Dictionary<Color32, int> _mapBack;
	private Vector2I _targetRes;
	private IColorPicker _colorPickerImplementation;

	public QuantizedImage(Image image, int paletteSize, Vector2I targetRes)
	{
		this._paletteSize = paletteSize;
		this._sourceImage = image;
		_targetRes = targetRes;
		Debug.Log("QuantizedImage created. with image ref null ?" + (_sourceImage == null));
	}


	[SuppressMessage("Interoperability", "CA1416:Valider la compatibilité de la plateforme")]
	public override bool Create()
	{
		if (_sourceImage.IsEmpty())
		{
			//Debug.Log("Waiting for image to be loaded...");
			return false;
		}

		Debug.Log("Image loaded, preparing it...");
		_image = _sourceImage.Duplicate() as Image;
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
		map.Quantize(OptimizedPaletteQuantizer.Octree(_paletteSize));

		row = map.FirstRow;
		int colorIndex = 0;
		_colors = new Color32[_paletteSize];
		_mapBack = new Dictionary<Color32, int>(_paletteSize);
		do
		{
			for (int x = 0; x < row.Width; x++)
			{
				var c = row.GetColor(x);
				var idx = (row.Index * row.Width + x) * 3;
				original[idx] = c.R;
				original[idx + 1] = c.G;
				original[idx + 2] = c.B;
				if (_mapBack.Count < _paletteSize)
				{
					var col = new Color32(c.R, c.G, c.B);
					if (!_mapBack.ContainsKey(col))
					{
						_mapBack.Add(col, colorIndex);
						_colors[colorIndex] = col;
						colorIndex++;
					}
				}
			}
		} while (row.MoveNextRow());

		_image = Image.CreateFromData(_image.GetWidth(), _image.GetHeight(), false, format, original);
		var color1f = _colors.Select(c => new Color(c.R / 255f, c.G / 255f, c.B / 255f)).ToArray();
		_colorPickerImplementation = ColorPicker.FromScheme(color1f);
		Debug.Log("Quantized image prepared with " + _colors.Length + " colors.");
		return true;
	}

	public override Godot.Image Texture => _image;

	public override string Name => _image.ResourceName;

	public int SpeciyIndex(Vector2 position)
	{
		var color = _image.GetPixel(Mathf.FloorToInt(position.x * _image.GetWidth()),
			Mathf.FloorToInt(position.y * _image.GetHeight()));
		var col32 = new Color32((byte)(color.R * 255), (byte)(color.G * 255), (byte)(color.B * 255));
		if (_mapBack.TryGetValue(col32, out var idx))
			return idx;
		Debug.LogError($"Speciied color {col32} in image not found in palette {string.Join(',', _mapBack.Keys.Select(c=> c.ToString()))}");
		return UnityEngine.Random.Range(0, _colors.Length);
	}

	public Color GetColor(Particle particle, int totalNbSpecies)
	{
		return _colorPickerImplementation.GetColor(particle, totalNbSpecies);
	}
}
