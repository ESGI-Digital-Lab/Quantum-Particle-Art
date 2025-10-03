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
    private Dictionary<Gene[], (int obtained, int target)> _result;
    private List<GeneticLooper> _loopers;

    public BitwiseComparisonFitness(int maxValue, int input, int target, List<GeneticLooper> loopers)
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
                if (looper.ResultAvailable)
                {
                    result = looper.GetResultAndFreeLooper();
                    //Debug.Log("Looper finished with fitness as result " + result);
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