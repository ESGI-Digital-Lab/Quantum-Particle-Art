using Godot;

[GlobalClass]
public partial class Measure : AGate
{
    public override bool Resolve(Particle particle)
    {
        particle.Collapse();
        return true;
    }
    public override Color Color => ViewHelpers.MEA;

    public override string ShortName => "M";
}