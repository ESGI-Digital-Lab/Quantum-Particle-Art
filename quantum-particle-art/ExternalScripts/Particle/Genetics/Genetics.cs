using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GeneticSharp;
using Godot;
using UnityEngine.ExternalScripts.Particle.Genetics;
using Random = UnityEngine.Random;

[GlobalClass]
public partial class GAParams : Resource
{
    [Export] private int _maxGen = 10000;
    [Export] private int _popSize = 12;
    [Export] private float _threshold = 0.9f;

    public int MaxGen => _maxGen;

    public int PopSize => _popSize;

    public float Threshold => _threshold;
}

public class Genetics
{
    private readonly Vector2I _size;
    private readonly GeneticAlgorithm _ga;
    private int _genFinished = 0;

    private ParticleSimulatorFitness comparison;
    private IProblem _problem;

    private object _lock = new();
    private readonly GAParams _gaParams;
    private bool _thresholdReached = false;
    private readonly GeneticLooper _viewer;
    
    public Genetics(int nbParticles, Vector2I size, GAParams param, List<GeneticLooper> loopers,
        GeneticLooper viewer,
        IEnumerable<AGate> gatesTemplate)
    {
        _gaParams = param;
        _viewer = viewer;
        GatesTypesToInt.OverrideReflection(new EmptyGate(), gatesTemplate);
        _size = size;
        _ga = CreateGA(nbParticles, loopers, out var proportional, out var exact, out var average);
        _ga.Termination = CreateTermination();
        _ga.TaskExecutor = new ParallelTaskExecutor();
        _ga.GenerationRan += (s, a) =>
        {
            if (!_thresholdReached && _ga.BestChromosome.Fitness >= _gaParams.Threshold)
            {
                _thresholdReached = true;
                UnityEngine.Debug.Log("Reached fitness of best chromosome threshold of " + _gaParams.Threshold +
                                      "at gen " + _genFinished);
                //When we reached a good threshold, we want to favor exact matches, we keep bitwise evaluation to discrimnate potential "not matching" chromosomes, especially after this change
                UnityEngine.Debug.Log(" increasing average evaluations and changing weights to favor exact matches");
                comparison.UpdateWeight(proportional, 1f);
                comparison.UpdateWeight(exact, 8f);
                average.NumberEvaluations = (int)(average.NumberEvaluations * 3);
            }
        };
        _ga.GenerationRan += (s, a) => GenerationFinished();
        _ga.TerminationReached += (sender, args) => UnityEngine.Debug.Log($"GA Termination Reached at generation {_genFinished} with best fitness: " + _ga.BestChromosome.Fitness);
        
        Task.Run(() => _ga.Start());
    }

    private void GenerationFinished()
    {
        _genFinished++;
        UnityEngine.Debug.Log($"--------------Gen finished {_genFinished}, best fitness: " + bestChromosomeFitness +
                              "showing it on the view");
        while (viewer.Busy) //We run it till the end
            Task.Delay(100).Wait();
        viewer.Start(_ga.BestChromosome, comparison.Input(_ga.BestChromosome));
        Task.Run(() =>
        {
            while (!viewer.ResultAvailable) //We run it till the end
                Task.Delay(100).Wait();
            _ = viewer.GetResultAndFreeLooper();
        });
    }

    private ITermination CreateTermination()
    {
        ITermination term = new FitnessThresholdTermination(1f /*w[1] / w.Sum()*/);
        if (_gaParams.MaxGen > 0)
            term = new OrTermination(term, new GenerationNumberTermination(_gaParams.MaxGen));
        term = new OrTermination(term, new FitnessStagnationTermination(200));
        return term;
    }

    private GeneticAlgorithm CreateGA(int nbParticles, List<GeneticLooper> loopers, out BitWiseEvaluator proportional,
        out ExactMatchEvaluator exact,
        out AveragedFitness average)
    {
        var selection = new TournamentSelection();
        var crossover = new UniformCrossover();
        var mutation = new UniformMutation(true);
        float[] w = [1, 10f];
        var max = (int)Mathf.Pow(2, nbParticles) - 1;
        _problem = new Operation(max);
        proportional = new BitWiseEvaluator(nbParticles);
        exact = new ExactMatchEvaluator();
        comparison = new ParticleSimulatorFitness(nbParticles, max, loopers, _problem, (proportional, 4f), (exact, 1f));
        average = new AveragedFitness(comparison, 7);
        IFitness fitness = new CombinedFitness((new MostNullGates(), w[0]), (average, w[1]));
        var chromosome = new Chromosome(_size.X * _size.Y);
        var population = new Population(_gaParams.PopSize, _gaParams.PopSize * 4, chromosome);
        return new GeneticAlgorithm(population, fitness, selection, crossover, mutation);
    }
}