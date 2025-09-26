using Godot;

[GlobalClass]
public partial class ControlX : DualInputAGate<ControlX>
{
    protected override void Resolve(Particle particle, Particle first)
    {
        first.Orientation.Control(particle.Orientation);
    }

    public override ControlX ID => this;
    protected override DualInputAGate<ControlX> Copy(ControlX source)
    {
        return new ControlX();
    }
}