using Godot;

[GlobalClass]
public partial class GAParams : Resource
{
    [Export] private int _maxGen = 10000;
    [Export] private int _popSize = 12;
    [Export] private float _threshold = 0.9f;
    [Export] private float _mutation = 0.1f;
    [Export] private float _crossover = 0.75f;

    public int MaxGen => _maxGen;

    public int PopSize => _popSize;

    public float Threshold => _threshold;
    public float CrossoverProb => _crossover;
    public float MutationProb => _mutation;
}