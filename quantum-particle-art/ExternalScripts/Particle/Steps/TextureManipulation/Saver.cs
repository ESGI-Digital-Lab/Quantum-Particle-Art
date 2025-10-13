using System.IO;
using Godot;
using UnityEngine;

public class Saver
{
	private string _path;
	private string _name;
	private Image _image;

	public Saver(string path)
	{
		_path = path;
	}

	public string Name => _name;

	public void Init(Image image, string name)
	{
		this._image = image;
		this._name = name;
	}

	private string UniquePath(out int freeIndex, string addon = "", string ext = "mp4")
	{
		string finalPath;
		freeIndex = 0;
		do
		{
			finalPath = IndexedPath(addon, freeIndex, ext);
			freeIndex++;
		} while (File.Exists(finalPath));

		freeIndex--;
		return finalPath;
	}

	private string IndexedPath(string addon, int forceIndex, string ext)
	{
		string root = _path;
		if (File.Exists(root))
			throw new System.Exception("File " + root +
									   " already exists! Delete or rename this file, it's base file doesn't exist anymore which means the naming is broken.");
		return root + '/' + _name + "_" + addon + "_" + forceIndex + "." + ext;
	}

	public void SaveTexToDisk(string addon = "")
	{
		_name += addon;
		var full = UniquePath(out int _, "final", "png");
		using (FileStream fs = new FileStream(full, FileMode.Create))
		{
			fs.Write(_image.SavePngToBuffer());
		}

		Debug.Log("Saved png to \n" + full + "\n"
				  + ProjectSettings.LocalizePath(full));
	}
}
