using Godot;

[GlobalClass]
public partial class Speed : AGate
{
    [Export] private bool _rawSetOverMult = false;
    [Export] private float _value;

    public override bool Resolve(Particle particle)
    {
        if (_rawSetOverMult)
            particle.Orientation.Speed = _value;
        else
            particle.Orientation.Speed *= _value;
        return true;
    }

    public override AGate Copy()
    {
        var s = new Speed();
        s._rawSetOverMult = _rawSetOverMult;
        s._value = _value;
        return s;
    }

    public override Color Color => Colors.IndianRed;

    public override string ShortName => (_rawSetOverMult ? "S=" : "Sx");// + _value.ToString("0.##");
}