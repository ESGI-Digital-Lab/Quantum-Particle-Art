using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class CombinedGates : AGate
{
    [Export] private Godot.Collections.Array<AGate> _gates;
    [Export] private bool _preconditionAllOverAny = true;
    [Export] private bool _resolveAllOverAny = false;
    public override bool Precondition(HashSet<Particle> setInside)
    {
        bool any = false;
        bool all = true;
        foreach (var gate in _gates)
            if (gate.Precondition(setInside))
                any = true;
            else
                all = false;
        return _preconditionAllOverAny ? all : any;
    }
    public override bool Resolve(Particle particle)
    {
        bool any = false;
        bool all = true;
        foreach (var gate in _gates)
            if (gate.Resolve(particle))
                any = true;
            else
                all = false;
        return _resolveAllOverAny ? all : any;
    }

    public override T DeepCopy<T>()
    {
        var baseCopy = base.DeepCopy<CombinedGates>();
        //Default godot duplicate does not deep copy arrays nor their contents
        baseCopy._gates = new Godot.Collections.Array<AGate>(_gates.Select(g => g.DeepCopy()));
        return baseCopy as T;
    }

    public override Color Color => _gates.Select(g => g.Color).Aggregate((g, acc) => acc + g) / _gates.Count;

    public override string ShortName => string.Join("+", _gates.Select(g => g.ShortName));

    public override string Label => string.Join("+", _gates.Where(g => g.DynamicName).Select(g => g.Label));
    protected override bool ShowLabelAllowed => _gates.Any(g => g.DynamicName);
}