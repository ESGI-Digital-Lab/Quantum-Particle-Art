using Godot;

[GlobalClass]
public partial class Count : AGate
{
    [Export] private string _nameAppendix;
    [Export] private int count = 0;
    public override bool Resolve(Particle particle)
    {
        count++;
        return true;
    }

    public override AGate Copy()
    {
        var cnt = new Count();
        cnt.count = count;
        return cnt;
    }

    public override Color Color => Colors.LightGray;
    public override string ShortName => "Cnt"+_nameAppendix;
}