using GeneticSharp;

public class AveragedFitness : IFitness
{
    private ParticleSimulatorFitness _fitness;
    private int _numberEvaluations;

    public AveragedFitness(ParticleSimulatorFitness fitness, int numberEvaluations)
    {
        _fitness = fitness;
        _numberEvaluations = numberEvaluations;
    }

    public int NumberEvaluations
    {
        get => _numberEvaluations;
        set => _numberEvaluations = value;
    }

    public double Evaluate(IChromosome chromosome)
    {
        double acc = 0;
        var task = _fitness.Evaluate(chromosome, _numberEvaluations);
        task.Wait();
        var val = task.Result;
        for (int i = 0; i < _numberEvaluations; i++)
            acc += val[i];

        return acc / _numberEvaluations;
    }
}