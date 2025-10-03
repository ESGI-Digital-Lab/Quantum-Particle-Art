using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class Superpose : AGate
{
    public override bool Precondition(HashSet<Particle> setInside)
    {
        return base.Precondition(setInside) && setInside.Count == 0;
    }

    public override bool Resolve(Particle particle)
    {
        particle.Superpose();
        return true;
    }
    public override Color Color => ViewHelpers.SUP;

    public override string ShortName => "H";
}