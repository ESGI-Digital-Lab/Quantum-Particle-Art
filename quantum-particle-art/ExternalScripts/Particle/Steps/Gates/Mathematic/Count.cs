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
    public override string ShortName => (_countAsName && _count>0 ? _count.ToString() : "Cnt") + _nameAppendix;

    public override bool DynamicName => base.DynamicName || (_countAsName && _count>0);
}