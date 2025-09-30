using Godot;

[GlobalClass]
public partial class Teleport : DualInputAGate<Teleport>
{
    protected override void Resolve(Particle particle, Particle first)
    {
        first.Orientation.Teleport(particle.Orientation);
    }

    public override Teleport ID => this;
    protected override DualInputAGate<Teleport> Copy(Teleport source)
    {
        return new Teleport();
    }

    public override Color Color => ViewHelpers.TEL;

    public override string ShortName => "T";
}