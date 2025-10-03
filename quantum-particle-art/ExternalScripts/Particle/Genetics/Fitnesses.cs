using System;
using System.Linq;
using GeneticSharp;
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
            if (GatesTypesToInt.IsNullId(gene.TypeId)) cnt--;
        }

        return cnt / genes.Length;
    }
}