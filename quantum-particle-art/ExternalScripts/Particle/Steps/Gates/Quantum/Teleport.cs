using Godot;

[GlobalClass]
public partial class Teleport : DualInputAGate<Teleport>
{
    protected override void Resolve(Particle particle, Particle first)
    {
        first.Orientation.Teleport(particle.Orientation);
    }

    public override Teleport ID => this;

    public override Color Color => ViewHelpers.TEL;

    public override string ShortName => "T";
}