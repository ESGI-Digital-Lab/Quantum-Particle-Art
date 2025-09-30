using Godot;

public partial class EmptyGate : AGate
{
    public override bool Resolve(Particle particle)
    {
        return false;
    }

    protected override AGate CopyA()
    {
        return new EmptyGate();
    }

    public override Color Color => Colors.Transparent;

    public override string ShortName => "";
}