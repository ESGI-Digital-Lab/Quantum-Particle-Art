using Godot;


[GlobalClass]
public partial class Rotate : AGate
{
    [Export(PropertyHint.Range,"0,360,1")] private float _degrees;
    [Export] private bool _inverted = false;

    public Rotate() : this(45f)
    {
    }

    public Rotate(float degrees, bool inverted = false)
    {
        _inverted = inverted;
        _degrees = degrees;
    }

    public override bool Resolve(Particle particle)
    {
        particle.Orientation.Degrees +=  _inverted ? -_degrees : _degrees;
        return true;
    }

    
    public override string Label => base.Label + (_inverted ? "-" : "+") + _degrees.ToString("0.#");

    public override Color Color => _inverted ? Colors.MediumSlateBlue : Colors.DarkBlue;

    public override string ShortName => "Rz";
}