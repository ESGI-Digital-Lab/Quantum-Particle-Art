using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class EncodedConfiguration : ASpawnConfiguration
{
    [Export] private int _encoded;
    private IGates _gates;
    public override IGates Gates => _gates ??= GenerateGates();

    private IGates GenerateGates()
    {
        GridGates gates = new GridGates(new(_nbParticles, _nbParticles), new(_nbParticles - 1, 0), [
            new(new Kill(), Enumerable.Range(0, _nbParticles).Select(i => new Vector2I(0, i))),
            new(new Count(), Enumerable.Range(0, _nbParticles).Select(i => new Vector2I(-1, i)))
        ]);
        return gates;
    }

    public override IEnumerable<UnityEngine.Vector2> Particles(Random random)
    {
        var _encoded = Mathf.Clamp(this._encoded, 0, (int)Mathf.Pow(2, _nbParticles) - 1);
        bool[] bits = new bool[_nbParticles];
        for (int i = 0; i < _nbParticles; i++)
        {
            bits[i] = (_encoded & 1) == 1; //Check last bit is 1
            _encoded >>= 1; //Bit shift
        }

        int cnt = 0;
        foreach (var p in LinearReparition(new(0, 0), new(0, 1), _nbParticles))
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
}