using Godot;


[GlobalClass]
public partial class Rotate : AGate
{
    [Export] private float _degrees;
    [Export] private bool _degInName;
    public Rotate() : this(45f)
    {
    }
    public Rotate(float degrees, bool degInName = false)
    {
        _degrees = degrees;
        _degInName = degInName;
    }
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

    public override Color Color => Colors.SteelBlue;

    public override string ShortName => "Rz" + (_degInName ? Mathf.RoundToInt(_degrees).ToString() : "");
}