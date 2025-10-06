using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class CombinedGates : AGate
{
    [Export] private Godot.Collections.Array<AGate> _gates;
    [Export] private bool _preconditionAllOverAny = true;
    [Export] private bool _resolveAllOverAny = false;
    [Export] private string _nameOverride = null;

    public CombinedGates() : this(true, false, false, null, [])
    {
    }
    public AGate this[int index] => _gates[index];
    public IEnumerable<AGate> Gates => _gates;

    public CombinedGates(bool preconditionAllOverAny, bool resolveAllOverAny, bool copyGates, string nameOverride,
        params AGate[] gates)
    {
        _nameOverride = nameOverride;
        IEnumerable<AGate> en = gates ?? [];
        if (copyGates)
            en = gates.Select(g => g.DeepCopy());
        this._gates = new(gates);
    }

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

    public override string ShortName => string.IsNullOrEmpty(_nameOverride)
        ? string.Join("+", _gates.Select(g => g.ShortName))
        : _nameOverride;

    public override string Label => string.Join("+", _gates.Where(g => g.DynamicName).Select(g => g.Label));
    protected override bool ShowLabelAllowed => _gates.Any(g => g.DynamicName);
}