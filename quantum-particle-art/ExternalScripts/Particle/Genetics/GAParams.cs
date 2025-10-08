using Godot;

[GlobalClass]
public partial class GAParams : Resource
{
    [ExportSubgroup("Pop settings")]
    [Export] private int _maxGen = 10000;
    [Export] private int _popSize = 12;
    [Export] private int _nbEvaluationsPerIndividual = 7;
    [Export] private float _threshold = 0.9f;
    [Export] private float _postThresholdFactor = 3;
    [ExportSubgroup("Evolution settings")]
    [Export] private float _mutation = 0.1f;

    [Export] private float _mutateToEmpty = .25f;
    [Export] private float _crossover = 0.75f;
    

    public int MaxGen => _maxGen;

    public int PopSize => _popSize;

    public float Threshold => _threshold;
    public float CrossoverProb => _crossover;
    public float MutationProb => _mutation;

    public int NbEvaluationsPerIndividual => _nbEvaluationsPerIndividual;

    public float PostThresholdFactor => _postThresholdFactor;

    public float MutateToEmpty => _mutateToEmpty;
}