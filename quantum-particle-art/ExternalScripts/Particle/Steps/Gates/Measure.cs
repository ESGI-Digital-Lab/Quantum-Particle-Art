using Godot;

[GlobalClass]
public partial class Measure : AGate
{
    public override bool Resolve(Particle particle)
    {
        particle.Collapse();
        return true;
    }

    public override AGate Copy()
    {
        return new Measure();
    }
}