using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeneticSharp;
using UnityEngine.ExternalScripts.Particle.Genetics;

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