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
    public override string Label => base.Label + (_rawSetOverMult ? "=" : "x") + _value.ToString("0.##");


    public override Color Color => Colors.IndianRed;

    public override string ShortName => (_rawSetOverMult ? "S=" : "Sx");// + _value.ToString("0.##");
}