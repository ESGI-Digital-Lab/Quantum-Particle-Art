using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class FakeChromosomeConfiguration : ChromosomeConfiguration
{
    public override IEnumerable<GateConfiguration> GatesConfig
    {
        get
        {
            return base.GatesConfig.SelectMany(g =>
            {
                return Enumerable.Range(0, _size.Y).Select(i =>
                {
                    return new GateConfiguration(g.Gate, g.Positions.Select(p => new Vector2I(p.X, i)));
                });
            });
        }
    }
}