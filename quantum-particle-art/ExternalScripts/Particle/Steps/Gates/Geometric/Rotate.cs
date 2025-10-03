using Godot;


[GlobalClass]
public partial class Rotate : AGate
{
    [Export] private float _degrees;

    public Rotate() : this(45f)
    {
    }

    public Rotate(float degrees)
    {
        _degrees = degrees;
    }

    public override bool Resolve(Particle particle)
    {
        particle.Orientation.Degrees += _degrees;
        return true;
    }

    public override string Label => base.Label + (_degrees > 0 ? "+" : "") + _degrees.ToString("0.#");


    public override Color Color => Colors.SteelBlue;

    public override string ShortName => "Rz";
}