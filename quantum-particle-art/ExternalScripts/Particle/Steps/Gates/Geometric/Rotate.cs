using Godot;


[GlobalClass]
public partial class Rotate : AGate
{
    [Export] private float _degrees = 45f;
    [Export] private bool _degInName;

    public override bool Resolve(Particle particle)
    {
        particle.Orientation.Degrees += _degrees;
        return true;
    }

    public override AGate Copy()
    {
        var ret = new Rotate();
        ret._degrees = _degrees;
        return ret;
    }

    public override Color Color => Colors.AliceBlue;

    public override string ShortName => "Rz" + (_degInName ? Mathf.RoundToInt(_degrees).ToString() : "");
}