using Godot;

[GlobalClass]
public partial class Count : AGate
{
    [Export] private bool _countAsName = true;
    [Export] private string _nameAppendix;
    [Export] private int _count = 0;

    public override bool Resolve(Particle particle)
    {
        _count++;
        return true;
    }

    public override AGate Copy()
    {
        var cnt = new Count();
        cnt._count = _count;
        return cnt;
    }

    public override Color Color => Colors.LightGray;
    public override string ShortName => "Cnt" + _nameAppendix;

    public override string Label => base.Label + ((_countAsName && _count>0) ? _count.ToString() :"");

    public int Value => _count;
}