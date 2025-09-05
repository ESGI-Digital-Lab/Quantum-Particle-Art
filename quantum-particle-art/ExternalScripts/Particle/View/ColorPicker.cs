using System.Collections.Generic;
using Godot;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Assertions;
using Color = Godot.Color;

public interface IColorPicker
{
    Color GetColor(Particle particle, int totalNbSpecies);
}

[CreateAssetMenu(fileName = "ColorPicker", menuName = "Particle/ColorPicker", order = 0)]
public class ColorPicker : ScriptableObject, IColorPicker
{
    public static ColorPicker Random(int amount)
    {
        var picked = new ColorPicker(amount);
        picked.RandomColors();
        return picked;
    }

    public static Godot.Color Random() => UnityEngine.Random.ColorHSV();

    public static ColorPicker FromScheme(Godot.Color[] scheme)
    {
        var picked = new ColorPicker(scheme.Length);
        picked._scheme = scheme;
        return picked;
    }

    private ColorPicker(int nb)
    {
        _nbColors = nb;
    }

    private Godot.Color[] _scheme;


    public Color GetColor(Particle particle, int totalNbSpecies)
    {
        Assert.IsTrue(_scheme.Length >= totalNbSpecies,
            "Current color scheme length does not match total number of species.");
        var color = _scheme[particle.Species];
        return color;
    }

    [Header("Generation helpers")] [SerializeField]
    private Color _baseColor;

    [Range(1, 20)] private int _nbColors = 10;

    [Button]
    private void ProceduralColors()
    {
        _scheme = new Godot.Color[_nbColors];
        int baseColor = (int)(_baseColor.R * 0xFF) << 16 | (int)(_baseColor.G * 0xFF) << 8 |
                        (int)(_baseColor.B * 0xFF); //Convert to hex
        for (int i = 0; i < _nbColors; i++)
            _scheme[i] = ColorFromSpeciesIndex(i, _nbColors, baseColor);
    }

    [Button]
    private void RandomColors()
    {
        _scheme = new Color[_nbColors];
        for (int i = 0; i < _nbColors; i++)
            _scheme[i] = Random();
    }

    private Godot.Color ColorFromSpeciesIndex(int infoSpecies, int rulesetNbSpecies, int baseColor)
    {
        var key = (infoSpecies, rulesetNbSpecies);
        int hex = ((int)((infoSpecies * 1f / (rulesetNbSpecies)) * 0xFFFFFF) + baseColor) %
                  0xFFFFFF; // "Random" fixed base color to avoid having only greys or other repeating patterns
        return new Godot.Color(
            ((hex >> 2 * 8) & 0xFF) / 255f,
            ((hex >> 1 * 8) & 0xFF) / 255f,
            (hex & 0xFF) / 255f
        );
    }
}