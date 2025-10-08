using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeneticSharp;
using UnityEngine.ExternalScripts.Particle.Genetics;

public class AveragedFitness : IFitness
{
    private IFitness _fitness;
    private int _numberEvaluations;

    public AveragedFitness(IFitness fitness, int numberEvaluations)
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
        object accLock = new();
        //Parallel.ForAsync(0, _numberEvaluations, (i,c)  =>
        //{
        //    return ValueTask.CompletedTask;
        //}).Wait();
        Task[] tasks = new Task[_numberEvaluations];
        for (int i = 0; i < _numberEvaluations; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                var val = _fitness.Evaluate(chromosome);
                lock (accLock)
                    acc += val;
            });
        }

        Task.WaitAll(tasks);
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