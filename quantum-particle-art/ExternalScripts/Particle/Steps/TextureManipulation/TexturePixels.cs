using NUnit.Framework;
using UnityEngine;

[CreateAssetMenu(fileName = "ImageTexture", menuName = "Particle/Textures/Texture from image", order = 0)]
public class TexturePixels : ATexProvider
{
    [SerializeField] private Texture2D _texture;

    public override void Create()
    {
        if (_texture == null)
        {
            Debug.LogError("Texture is not assigned.");
            return;
        }
        Assert.IsTrue(_texture.isReadable, "Texture is not readable, go to the asset and enable read/write.");
        _texture.Apply();
    }
    public override Texture Texture
    {
        get => _texture;
    }

    public override Color[] GetPixels()
    {
        return _texture.GetPixels();
    }
}