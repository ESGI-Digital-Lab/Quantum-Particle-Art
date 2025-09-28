using Godot;

[GlobalClass]
public partial class Kill : AGate
{
    [Export] private bool _safe = false;

    public override bool Resolve(Particle particle)
    {
        if (_safe)
            return false;
        particle.MarkDead();
        return true;
    }

    public override AGate Copy()
    { 
        var k =new Kill();
        k._safe = _safe;
        return k;
    }

    public override Color Color => Colors.DarkRed;

    public override string ShortName => "X";
}