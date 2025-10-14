using Godot;
using UnityEngine;
using Color = Godot.Color;

[GlobalClass]
public partial class SpecyChange : AGate
{
    [Export] private int _seed = 159;
    private float _target = -1;
    private System.Random _random;

    public override bool Resolve(Particle particle)
    {
        particle.UpdateSpecies(_target);
        return true;
    }

    public override T DeepCopy<T>()
    {
        var t = base.DeepCopy<T>();
        //We define the target on gate creation from copy, using inerhited cyclic chains of seed, to ensure deterministic values accros runs but with a pseudo random distribution
        var ch = (t as SpecyChange);
        ch._seed = this._seed + 1;
        ch._random = new System.Random(ch._seed);
        ch._target = (float)_random.NextDouble();

        return t;
    }

    public override Color Color => Colors.AntiqueWhite;

    public override string ShortName => "Cl=";
}