using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    private int _input;
    private int _target;
    private Dictionary<Gene[], (int obtained, int target)> _result;
    private List<GeneticLooper> _loopers;

    public IntComparisonFitness(int maxValue, int input, int target, List<GeneticLooper> loopers)
    {
        _result = new Dictionary<Gene[], (int obtained, int target)>();
        _maxValue = maxValue;
        _loopers = loopers;
        _input = input%_maxValue;
        _target = target%_maxValue;
    }

    public void UpdateResult(Gene[] genetics, int result, int target)
    {
        _result[genetics] = (result, target);
    }

    private const int refreshDelay = 100;

    public double Evaluate(IChromosome chromosome)
    {
        GeneticLooper looper = null;
        while (looper == null)
        {
            //Debug.Log("Waiting for free looper");
            looper = null;
            foreach (var l in _loopers)
            {
                lock (l.Lock)
                {
                    if (!l.Busy)
                    {
                        l.Start(chromosome, _input);
                        looper = l;
                        //Debug.Log("Assigned looper " + l.ToString());
                        break;
                    }
                }
            }

            Task.Delay(refreshDelay).Wait();
        }

        int? result = null;
        while (!result.HasValue)
        {
            lock (looper.Lock)
            {
                //Debug.Log("Waiting for looper to finish");
                if (looper.Finished)
                {
                    result = looper.GetResultAndFreeLooper();
                    Debug.Log("Looper finished with fitness as result " + result);
                }
            }

            Task.Delay(refreshDelay).Wait();
        }

        float delta = Mathf.Abs(result.Value - _target);
        return 1f - delta / _maxValue;
    }

    public bool Equals(Gene[] x, Gene[] y)
    {
        return !(x != null ^ y != null) && x.SequenceEqual(y);
    }

    public int GetHashCode(Gene[] obj)
    {
        return ((IStructuralEquatable)obj).GetHashCode();
    }
}