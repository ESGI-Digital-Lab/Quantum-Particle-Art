using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GeneticSharp;
using UnityEngine;
using UnityEngine.ExternalScripts.Particle.Genetics;
using static BitHelpers;


public class ParticleSimulatorFitness
{
    private readonly int _nbBits;
    private List<GeneticLooper> _loopers;
    private IProblem _problem;
    private readonly Dictionary<IEvaluator, double> _evaluatorWeights;

    /// <summary>
    /// One evaluation of this fiteness method represents one full particle simulation with ONE input and it's evaluation result against the evaluators. It's possible to combine this fitness with other that don't require to be run and to combine and use multiple isntances of this fitness to aggregate the result of multiple runes
    /// </summary>
    /// <param name="nbBits"></param>
    /// <param name="maxValue"></param>
    /// <param name="loopers"></param>
    /// <param name="problem"></param>
    /// <param name="weightedEvaluator">This simulation fitness will run the whole particle simulation, and decode final value, and then let the evaluators evaluate the fitness based on the input, obtained and expected, evaluators don't depends on chromosomes and are simple weighted fitness evaluations, after a single run, </param>
    public ParticleSimulatorFitness(int nbBits, int maxValue, List<GeneticLooper> loopers, IProblem problem,
        params (IEvaluator, double)[] weightedEvaluator)
    {
        _nbBits = nbBits;
        _loopers = loopers;
        _problem = problem;
        _evaluatorWeights = weightedEvaluator.ToDictionary();
    }

    public void UpdateWeight(IEvaluator evaluator, double weight) => _evaluatorWeights[evaluator] = weight;

    private const int refreshDelay = 100;


    public async Task<double[]> Evaluate(IChromosome chromosome, int times)
    {
        double[] values = new double[times];
        Stopwatch sw = new Stopwatch();
        sw.Start();

        void Log(string s)
        {
            //UnityEngine.Debug.Log(s + " xxx " + sw.Elapsed);
        }

        Log("Start");
        GeneticLooper looper = null;
        int specificInput = _problem.CreateNewInput();
        while (looper == null)
        {
            //Debug.Log("Waiting for free looper");
            foreach (var l in _loopers)
            {
                lock (l.Lock)
                {
                    if (!l.Busy)
                    {
                        looper = l;
                        break;
                    }
                }
            }

            if (looper == null)
            {
                Log("waiting 1");
                await Task.Delay(refreshDelay);
            }
        }

        Log("Found and starting + " + looper.ToString());
        for (int i = 0; i < times; i++)
        {
            lock (looper.Lock)
                looper.Start(chromosome, specificInput);
            
            (chromosome as Chromosome).AddInputTestedOn(specificInput);
            int? result = null;
            while (!result.HasValue)
            {
                lock (looper.Lock)
                {
                    if (looper.ResultAvailable)
                    {
                        bool freeLooper = i == times - 1;
                        result = looper.GetResult(freeLooper); //We keep hook on the looper until the last run, to avoid it being used by another thread
                    }
                }

                if (!result.HasValue)
                {
                    Log("waiting 1 for result");
                    await Task.Delay(refreshDelay);
                }
            }

            Log("Got result " + looper.ToString());

            var weighted = _evaluatorWeights
                .Select(e => (e.Key.Fitness(specificInput, result.Value, _problem.Expected(specificInput)), e.Value))
                .WeightedSum();
            values[i]= weighted;
        }
        return values;
    }

    public bool Equals(Gene[] x, Gene[] y)
    {
        return !(x != null ^ y != null) && x.SequenceEqual(y);
    }

    public int Input(IChromosome chromosome, bool withSameInput = true)
    {
        return withSameInput
            ? (chromosome as Chromosome)?.Input ?? _problem.CreateNewInput()
            : _problem.CreateNewInput();
    }
}