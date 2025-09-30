using System;
using System.Collections.Generic;
using System.Linq;
using GeneticSharp;
using UnityEngine;
using UnityEngine.ExternalScripts.Particle.Genetics;

public class CombinedFitness : IFitness
{
    private (IFitness, float)[] _fitnesses;

    public CombinedFitness(params (IFitness, float)[] fitnesses)
    {
        _fitnesses = fitnesses.ToArray();
    }

    public double Evaluate(IChromosome chromosome)
    {
        double sum = 0;
        float totalWeight = 0;
        foreach (var (fitness, weight) in _fitnesses)
        {
            sum += fitness.Evaluate(chromosome) * weight;
            totalWeight += weight;
        }

        //Debug.LogError("Use then logic, using only second fitness only if first is full");

        if (totalWeight > 0)
        {
            return sum / totalWeight;
        }
        else
        {
            return 0;
        }
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
            if (gene.TypeId != GatesTypesToInt.Id(GatesTypesToInt.NullType)) cnt--;
        }

        return cnt / genes.Length;
    }
}

public class IntComparisonFitness : IFitness
{
    private int _target;
    private Dictionary<Gene[], (int obtained, int target)> _result;

    public IntComparisonFitness()
    {
        _result = new Dictionary<Gene[], (int obtained, int target)>();
    }

    public void UpdateResult(Gene[] genetics, int result, int target)
    {
        _result[genetics] = (result, target);
    }

    public double Evaluate(IChromosome chromosome)
    {
        if (_result.TryGetValue(chromosome.GetGenes(), out var values))
        {
            return 1.0 / (1.0 + System.Math.Abs(values.obtained - values.target));
        }

        return 0f;
    }
}