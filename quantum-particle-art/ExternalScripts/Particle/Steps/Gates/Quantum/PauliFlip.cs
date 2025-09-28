using Godot;

[GlobalClass]
public partial class PauliFlip : AGate
{
    private enum Axis
    {
        X = 0,y = 1
    }

    [Export] private Axis _axis;
    public override bool Resolve(Particle particle)
    {
        if (_axis == Axis.X)
            particle.FlipX();
        else if (_axis == Axis.y)
            particle.FlipY();
        return true;
    }

    public override AGate Copy()
    {
        var ret = new PauliFlip();
        ret._axis = _axis;
        return ret;
    }

    public override Color Color => Colors.Azure;

    public override string ShortName => "P" + _axis.ToString().ToLower();
}