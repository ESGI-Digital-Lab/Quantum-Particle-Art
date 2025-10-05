using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.ExternalScripts.Particle.Genetics;
using Mathf = Godot.Mathf;
using Random = System.Random;

[GlobalClass]
public partial class EncodedConfiguration : ASpawnConfiguration
{
    [Export] private Count _countTemplate;
    [Export] private Kill _killTemplate;
    [Export] private int _encoded;
    private GridGates _grid;
    public override IGates Gates => _grid;

    public void UpdateEncoded(int encoded)
    {
        //Debug.Log("Updating encoded to " + encoded + (_last==null?" no last":" has last") + (_grid==null?" no grid":" has grid"));
        _encoded = encoded;
    }

    public void UpdateDynamicGates(IEnumerable<GateConfiguration> gates)
    {
        _grid.SetDynamicGates(gates);
    }

    private GridGates GenerateGates()
    {
        //So we have a fresh, unique to this set of gate, edition of the templates
        _killTemplate = _killTemplate.DeepCopy<Kill>();
        _countTemplate = _countTemplate.DeepCopy<Count>();
        IEnumerable<GateConfiguration> baseGates = [
            new(_killTemplate, Enumerable.Range(0, NbParticles).Select(i => new Vector2I(0, i))),
            new(_countTemplate, Enumerable.Range(0, NbParticles).Select(i => new Vector2I(-1, i)))
        ];
        _grid = new GridGates(new(NbParticles, NbParticles), new(NbParticles - 1, 0), baseGates);
        return _grid;
    }

    public int Result()
    {
        //Debug.Log("Asking for result with last grid " + (_grid != null));
        var copies = _grid.Copies(_countTemplate);
        //Assert.IsTrue(copies.Length == NbParticles,$"Count gate copies {copies.Length} does not match number of particles {NbParticles}");
        var acc = copies.Select(c=>c.Value).DecodeBits(2);

        return acc;
    }

    public override IEnumerable<UnityEngine.Vector2> Particles(Random random)
    {
        var bits = _encoded.Bits(NbParticles);

        int cnt = 0;
        foreach (var p in LinearReparition(new(0, 0), new(0, 1), NbParticles))
        {
            if (bits[cnt])
                yield return p;
            cnt++;
        }
    }

    protected override UnityEngine.Vector2 BaseVelocity()
    {
        return new(1, 0f);
    }

    public void Reset()
    {
        _grid = GenerateGates();
    }
}