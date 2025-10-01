using System;
using System.Collections;
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

public class IntComparisonFitness : IFitness, IEqualityComparer<Gene[]>
{
    private int _maxValue;
    private int _target;
    private Dictionary<Gene[], (int obtained, int target)> _result;

    public IntComparisonFitness(int maxValue)
    {
        _result = new Dictionary<Gene[], (int obtained, int target)>();
        _maxValue = maxValue;
    }

    public void UpdateResult(Gene[] genetics, int result, int target)
    {
        _result[genetics] = (result, target);
    }

    public double Evaluate(IChromosome chromosome)
    {
        if (_result.TryGetValue(chromosome.GetGenes(), out var values))
        {
            var delta = System.Math.Abs(values.obtained - values.target);
            Debug.Log("Found");
            return 1f - delta / (1f * _maxValue);
        }

        Debug.LogError("Not found");
        return 0f;
    }

    public bool Equals(Gene[] x, Gene[] y)
    {
        return !(x != null ^ y != null) && x.SequenceEqual(y);
    }

    public int GetHashCode(Gene[] obj)
    {
        return ((IStructuralEquatable) obj).GetHashCode();
    }
}