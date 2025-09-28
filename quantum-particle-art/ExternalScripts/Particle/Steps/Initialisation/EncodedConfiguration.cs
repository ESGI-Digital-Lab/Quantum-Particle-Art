using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class EncodedConfiguration : ASpawnConfiguration
{
    [Export] private int _encoded;

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