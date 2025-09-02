using System.Collections.Generic;
using Godot;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Assertions;
using Color = Godot.Color;

[CreateAssetMenu(fileName = "ColorPicker", menuName = "Particle/ColorPicker", order = 0)]
public class ColorPicker : ScriptableObject
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
        picked._colorMode = ColorModes.Scheme;
        picked._scheme = scheme;
        return picked;
    }

    private ColorPicker(int nb)
    {
        _nbColors = nb;
    }
    [SerializeField] private ColorModes _colorMode;

    [SerializeField, ShowIf(nameof(_colorMode), ColorModes.Identical),
     Tooltip("Base/first color for fixed or specie mode")]
    private Godot.Color _color;

    [SerializeField, ShowIf(nameof(_colorMode), ColorModes.Scheme),
     Tooltip("Assigned in order to each species in the scheme mode")]
    private Godot.Color[] _scheme;

    public enum ColorModes
    {
        [Tooltip("Same custom color for all particles")]
        Identical = 0,
        [Tooltip("Color based on particle orientation, using a color ramp")]
        Direction = 1,
        [Tooltip("Random color provided every time a color is requested")]
        Random = 3,
        [Tooltip("Color scheme based on species index, using a fixed color scheme, helpers below to generate schemes")]
        Scheme = 4
    }

    public Color GetColor(Particle particle, int totalNbSpecies)
    {
        Color color = Colors.Black;
        switch (_colorMode)
        {
            case ColorModes.Identical: color = Colors.White; break;
            case ColorModes.Direction:
                color = ViewHelpers.ColorRamp360(particle);
                break;
            case ColorModes.Random:
                color = UnityEngine.Random.ColorHSV();
                break;
            case ColorModes.Scheme:
                Assert.IsTrue(_scheme.Length >= totalNbSpecies,
                    "Current color scheme length does not match total number of species.");
                color = _scheme[particle.Species];
                break;
            default:
                throw new System.ArgumentOutOfRangeException();
        }

        return color;
    }

    [Header("Generation helpers")] [SerializeField]
    private Color _baseColor;

    [Range(1, 20)] private int _nbColors = 10;

    [Button]
    private void ProceduralColors()
    {
        _colorMode = ColorModes.Scheme;
        _scheme = new Godot.Color[_nbColors];
        int baseColor = (int)(_baseColor.R * 0xFF) << 16 | (int)(_baseColor.G * 0xFF) << 8 | (int)(_baseColor.B * 0xFF); //Convert to hex
        for (int i = 0; i < _nbColors; i++)
            _scheme[i] = ColorFromSpeciesIndex(i, _nbColors,baseColor);
    }

    [Button]
    private void RandomColors()
    {
        _colorMode = ColorModes.Scheme;
        _scheme = new Color[_nbColors];
        for (int i = 0; i < _nbColors; i++)
            _scheme[i] = Random();
    }

    public Godot.Color ColorFromSpeciesIndex(int infoSpecies, int rulesetNbSpecies, int baseColor)
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