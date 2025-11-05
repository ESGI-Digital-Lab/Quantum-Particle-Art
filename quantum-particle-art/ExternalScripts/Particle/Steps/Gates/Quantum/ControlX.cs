using Godot;

[GlobalClass]
public partial class ControlX : DualInputAGate<ControlX>
{
    protected override void Resolve(Particle particle, Particle first)
    {
        first.Orientation.Control(particle.Orientation);
    }

    public override ControlX ID => this;

    public override Color Color => ViewHelpers.CTR;

    public override string ShortName => "Cx";
}