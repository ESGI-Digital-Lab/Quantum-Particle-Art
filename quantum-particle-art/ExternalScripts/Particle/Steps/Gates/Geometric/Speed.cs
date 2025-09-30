using Godot;

[GlobalClass]
public partial class Speed : AGate
{
    [Export] private bool _rawSetOverMult = false;
    [Export] private float _value;
    public Speed() : this(1f)
    {
    }

    public Speed(float value)
    {
        _value = value;
    }
    public override bool Resolve(Particle particle)
    {
        if (_rawSetOverMult)
            particle.Orientation.Speed = _value;
        else
            particle.Orientation.Speed *= _value;
        return true;
    }

    protected override AGate CopyA()
    {
        var s = new Speed();
        s._rawSetOverMult = _rawSetOverMult;
        s._value = _value;
        return s;
    }

    public override Color Color => Colors.IndianRed;

    public override string ShortName => (_rawSetOverMult ? "S=" : "Sx");// + _value.ToString("0.##");
}