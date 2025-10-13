using Godot;

[GlobalClass]
public partial class GAParams : Resource
{
    [ExportSubgroup("Pop settings")]
    [Export] private int _maxGen = 10000;
    [Export] private int _popSize = 12;
    [ExportSubgroup("Evolution settings")]
    [Export] private float _mutation = 0.1f;
    [Export] private float _mutateToEmpty = .25f;
    [Export] private float _mutateBlock = 0.1f;
    [Export] private float _crossover = 0.75f;
    [ExportSubgroup("Meta evolution")] 
    [Export] private Vector2 _nbEvalRange = new(1, 20);
    [Export] private Curve _exactWeight;
    [ExportSubgroup("Viewer settings")]
    [Export] private bool _randomizeViewerInputs = true;
    

    public int MaxGen => _maxGen;

    public int PopSize => _popSize;

    public float CrossoverProb => _crossover;
    public float MutationProb => _mutation;

    public int NbEvaluationsPerIndividual(float t) => (int)Mathf.Lerp(NbEvalRange.Y, NbEvalRange.X, t);
    
    public float MutateToEmpty => _mutateToEmpty;

    public float ExactWeight(float t) => _exactWeight.Sample(t);
    public float ProportionalWeight(float t) => 1-ExactWeight(t);

    public Vector2 NbEvalRange => _nbEvalRange;

    public float MutateBlock => _mutateBlock;

    public bool RandomizeViewerInputs => _randomizeViewerInputs;
}