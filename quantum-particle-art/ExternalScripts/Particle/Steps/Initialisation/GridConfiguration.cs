using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public abstract partial class GridConfiguration : ASpawnConfiguration
{
    [Export] protected Count _countTemplate;
    [Export] protected Kill _killTemplate;
    protected CombinedGates _combined;
    protected GridGates _grid;
    public override IGates Gates => _grid;
    
    public void UpdateDynamicGates(IEnumerable<GateConfiguration> gates)
    {
        _grid.SetDynamicGates(gates);
    }
    protected GridGates GenerateGates()
    {
        //So we have a fresh, unique to this set of gate, edition of the templates
        _killTemplate = _killTemplate.DeepCopy<Kill>();
        _countTemplate = _countTemplate.DeepCopy<Count>();
        //IEnumerable<GateConfiguration> baseGates = [
        //    new(_killTemplate)),
        //    new(_countTemplate, Enumerable.Range(0, NbParticles).Select(i => new Vector2I(-1, i)))
        //];
        _combined =
            new CombinedGates(true, false, false, "X", _countTemplate,
                _killTemplate); //We don't deep copy, so the base templates remain the parent holders
        var width = NbParticles;
        var positions = Enumerable.Range(0, NbParticles).Select(i => new Vector2I(width-1, i));
        _grid = new GridGates(new(width, NbParticles), new(0, 0), [new(_combined, positions)]);
        return _grid;
    }

    public override IEnumerable<UnityEngine.Vector2> Particles(Random random)
    {
        var bits = Bits();

        int cnt = 0;
        foreach (var p in LinearReparition(new(0, 0), new(0, 1), NbParticles))
        {
            if (bits[cnt])
                yield return p;
            cnt++;
        }
    }

    protected abstract bool[] Bits();

    protected override UnityEngine.Vector2 BaseVelocity()
    {
        return new(1, 0f);
    }

    public void Reset()
    {
        _grid = GenerateGates();
    }
}