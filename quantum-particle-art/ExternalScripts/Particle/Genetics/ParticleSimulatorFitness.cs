using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeneticSharp;
using UnityEngine;
using UnityEngine.ExternalScripts.Particle.Genetics;
using static BitHelpers;


public class ParticleSimulatorFitness : IFitness
{
    private readonly int _nbBits;
    private List<GeneticLooper> _loopers;
    private IProblem _problem;
    private readonly Dictionary<IEvaluator,double> _evaluatorWeights;

    /// <summary>
    /// One evaluation of this fiteness method represents one full particle simulation with ONE input and it's evaluation result against the evaluators. It's possible to combine this fitness with other that don't require to be run and to combine and use multiple isntances of this fitness to aggregate the result of multiple runes
    /// </summary>
    /// <param name="nbBits"></param>
    /// <param name="maxValue"></param>
    /// <param name="loopers"></param>
    /// <param name="problem"></param>
    /// <param name="weightedEvaluator">This simulation fitness will run the whole particle simulation, and decode final value, and then let the evaluators evaluate the fitness based on the input, obtained and expected, evaluators don't depends on chromosomes and are simple weighted fitness evaluations, after a single run, </param>
    public ParticleSimulatorFitness(int nbBits, int maxValue, List<GeneticLooper> loopers, IProblem problem, params (IEvaluator,double)[] weightedEvaluator)
    {
        _nbBits = nbBits;
        _loopers = loopers;
        _problem = problem;
        _evaluatorWeights = weightedEvaluator.ToDictionary();
    }
    public void UpdateWeight(IEvaluator evaluator, double weight) => _evaluatorWeights[evaluator] = weight;

    private const int refreshDelay = 100;


    public double Evaluate(IChromosome chromosome)
    {
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
                        l.Start(chromosome, specificInput);
                        (chromosome as Chromosome).AddInputTestedOn(specificInput);
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

        var weights = _evaluatorWeights
            .Select(e => (e.Key.Fitness(specificInput, result.Value, _problem.Expected(specificInput)), e.Value))
            .WeightedSum();
        return weights;
    }

    public bool Equals(Gene[] x, Gene[] y)
    {
        return !(x != null ^ y != null) && x.SequenceEqual(y);
    }

    public int Input(IChromosome chromosome, bool withSameInput = true)
    {
        return withSameInput ? (chromosome as Chromosome).Input : _problem.CreateNewInput();
    }
}