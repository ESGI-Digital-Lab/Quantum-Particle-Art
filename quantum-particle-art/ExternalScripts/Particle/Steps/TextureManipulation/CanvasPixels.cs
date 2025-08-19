using System;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "CanvasTexture", menuName = "Particle/Textures/Canvas", order = 0)]
public class CanvasPixels : ATexProvider
{
    [SerializeField, Range(1, 25), Tooltip("Every steps doubles the side of texture (and quadruple total size) : 10=>1024x1024, 13=>4096x4096 (size of first ever yellow green image generated using this technique), 15 => 32768x32768...")]
    private int _powerOf2 = 8;

    [SerializeField, ReadOnly,Tooltip("Size of the texture in pixels, higher size => higher resolution (we can zoom more before seeing pixels) => thinner trace left by the particles")] private Vector2Int _size;
    [SerializeField] private Color _color;
    private Texture2D _texture;

    private void OnValidate()
    {
        _size = new Vector2Int((int)Mathf.Pow(2, _powerOf2), (int)Mathf.Pow(2, _powerOf2));
    }
    public override void Create()
    {
        //Safe
        OnValidate();
        _texture = new Texture2D(_size.x, _size.y);
        var pixels = new Color[_size.x * _size.y];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = _color;
        _texture.SetPixels(pixels);
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