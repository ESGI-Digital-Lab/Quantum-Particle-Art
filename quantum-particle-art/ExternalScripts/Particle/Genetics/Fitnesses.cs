using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeneticSharp;
using UnityEngine.ExternalScripts.Particle.Genetics;

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

public class CombinedFitness : IFitness
{
    private (IFitness, double)[] _fitnesses;

    public CombinedFitness(params (IFitness, double)[] fitnesses)
    {
        _fitnesses = fitnesses.ToArray();
    }

    public double Evaluate(IChromosome chromosome)
    {
        var vws = _fitnesses.Select(fw => (fw.Item1.Evaluate(chromosome), fw.Item2));
        return vws.WeightedSum();
    }
}

public class MostNullGates : IFitness
{
    public double Evaluate(IChromosome chromosome)
    {
        var genes = chromosome.GetGenes();
        double cnt = genes.Length;
        foreach (var ch in genes)
        {
            var gene = (GeneContent)ch.Value;
            if (!GatesTypesToInt.IsNullId(gene.TypeId)) cnt--;
        }

        return cnt / genes.Length;
    }
}