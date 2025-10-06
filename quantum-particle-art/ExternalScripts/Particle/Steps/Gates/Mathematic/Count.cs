using Godot;

[GlobalClass]
public partial class Count : AGate
{
    [Export] private int _max = 1;
    [Export] private int _count = 0;
    [Export] private bool _countAsName = true;
    [Export] private string _nameAppendix;

    public override bool Resolve(Particle particle)
    {
        if (_count >= _max)
            return false;
        _count++;
        particle.MarkDead();
        return true;
    }

    public override Color Color => Colors.LightGray;
    public override string ShortName => "Cnt" + _nameAppendix;

    public override string Label => base.Label + ((_countAsName && _count>0) ? _count.ToString() :"");

    public int Value => _count;

    protected override bool ShowLabelAllowed => true;
}