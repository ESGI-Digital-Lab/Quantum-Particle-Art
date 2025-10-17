using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

[GlobalClass]
public partial class PatternConfiguration : ChromosomeConfigurationBase
{
    [Export] private Godot.Collections.Array<GateConfiguration> _pattern;
    [Export] protected Vector2I _size;
    [Export] private Vector2I _patternCenter;
    [Export] private Vector2I _patternRepeat;
    [Export] private Vector2I _patternOffsetX;
    [Export] private Vector2I _patternOffsetY;
    public override IEnumerable<GateConfiguration> GatesConfig
    {
        get
        {
            if(_patternRepeat.X == 0 || _patternRepeat.Y == 0)
                UnityEngine.Debug.LogWarning($"PatternConfiguration with zero repeat {_patternRepeat}, is it on purpose or did you mean 1?");
            var pattern = _pattern;
            List<GateConfiguration> repeated = new List<GateConfiguration>(pattern.Count * _patternRepeat.X * _patternRepeat.Y);
            foreach (var g in pattern)
            {
                for (int x = 0; x < _patternRepeat.X; x++)
                {
                    for (int y = 0; y < _patternRepeat.Y; y++)
                    {
                        var offset = x * _patternOffsetX + y * _patternOffsetY - _patternCenter;
                        repeated.Add(new GateConfiguration(g.Gate, g.Positions.Select(p =>
                        {
                            var off = p + offset;
                            var posMod = (off % _size + _size) % _size;
                            return posMod;
                        })));
                    }
                }
            }
            return repeated;
        }
    }

    public override Vector2I Size => _size;
}