using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using Mathf = Godot.Mathf;
using Random = System.Random;

[GlobalClass]
public partial class EncodedConfiguration : ASpawnConfiguration
{
    [Export] private Count _countTemplate;
    [Export] private Kill _killTemplate;
    [Export] private int _encoded;
    private GridGates _grid;
    public override IGates Gates => _grid ??= GenerateGates();

    public void UpdateEncoded(int encoded)
    {
        //Debug.Log("Updating encoded to " + encoded + (_last==null?" no last":" has last") + (_grid==null?" no grid":" has grid"));
        _encoded = encoded;
        _grid.SetDynamicGates(Enumerable.Range(1, _nbParticles-2)
            .Select<int, GateConfiguration>(i => new(new Rotate(45), [new(i, Random.Shared.Next(_nbParticles))])));
    }

    private GridGates GenerateGates()
    {
        _grid = new GridGates(new(_nbParticles, _nbParticles), new(_nbParticles - 1, 0), [
            new(_killTemplate, Enumerable.Range(0, _nbParticles).Select(i => new Vector2I(0, i))),
            new(_countTemplate, Enumerable.Range(0, _nbParticles).Select(i => new Vector2I(-1, i))),
            //new(new Rotate(45), [new(-3,0)])
        ]);
        return _grid;
    }

    public int Result()
    {
        Debug.Log("Asking for result with last grid " + (_grid != null));
        var copies = _grid.Copies(_countTemplate).ToArray();
        Assert.IsTrue(copies.Length == _nbParticles,
            $"Count gate copies {copies.Length} does not match number of particles {_nbParticles}");
        int acc = 0;
        int i = 0;
        foreach (var copy in copies)
        {
            acc += copy.Value * (int)Mathf.Pow(2, i);
            i++;
        }

        return acc;
    }

    public override IEnumerable<UnityEngine.Vector2> Particles(Random random)
    {
        var _encoded = Mathf.Clamp(this._encoded, 0, (int)Mathf.Pow(2, _nbParticles) - 1);
        bool[] bits = new bool[_nbParticles];
        for (int i = 0; i < _nbParticles; i++)
        {
            bits[_nbParticles - (i + 1)] = (_encoded & 1) == 1; //Check last bit is 1
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