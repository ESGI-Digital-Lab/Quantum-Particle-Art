using Godot;

[GlobalClass]
public partial class GAParams : Resource
{
    [Export] private int _maxGen = 10000;
    [Export] private int _popSize = 12;
    [Export] private float _threshold = 0.9f;

    public int MaxGen => _maxGen;

    public int PopSize => _popSize;

    public float Threshold => _threshold;
}