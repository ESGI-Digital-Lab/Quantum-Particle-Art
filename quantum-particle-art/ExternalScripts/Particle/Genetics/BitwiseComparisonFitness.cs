using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeneticSharp;
using UnityEngine;

public class BitwiseComparisonFitness : IFitness, IEqualityComparer<Gene[]>
{
    private int _maxValue;
    private int _input;
    private int _target;
    private int _nbBits;
    private List<GeneticLooper> _loopers;

    public BitwiseComparisonFitness(int nbBits, int input, int target, List<GeneticLooper> loopers)
    {
        _maxValue = (int) Mathf.Pow(2, nbBits)-1;
        _nbBits = nbBits;
        _loopers = loopers;
        _input = input%_maxValue;
        _target = target%_maxValue;
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
                if (looper.ResultAvailable)
                {
                    result = looper.GetResultAndFreeLooper();
                    //Debug.Log("Looper finished with fitness as result " + result);
                }
            }

            Task.Delay(refreshDelay).Wait();
        }

        //float delta = Mathf.Abs(result.Value - _target);
        //return 1f - delta / _maxValue;
        float tot = _nbBits;
        foreach(var couple in result.Value.Bits(_nbBits).Zip(_target.Bits(_nbBits)))
        {
            if (couple.First != couple.Second) tot--;
        }

        return tot / _nbBits;
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