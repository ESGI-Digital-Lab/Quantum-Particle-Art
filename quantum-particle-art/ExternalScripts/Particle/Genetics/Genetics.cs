using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GeneticSharp;
using Godot;
using UnityEngine.ExternalScripts.Particle.Genetics;
using Random = UnityEngine.Random;

public class Genetics
{
    private int _maxGen = 10000;
    private int _popSize = 12;
    private readonly Vector2I _size;
    private GeneticAlgorithm _ga;

    private int _finishedCount = 0;
    private int _totalIndex = 0;
    private int _nbGen = 0;
    public event Action<IList<IChromosome>> OnGenerationReady;

    private ParticleSimulatorFitness comparison;
    private IProblem _problem;

    private object _lock = new();

    public Genetics(int nbParticles, Vector2I size, int maxGen, int maxPop, List<GeneticLooper> loopers,
        GeneticLooper viewer,
        IEnumerable<AGate> gatesTemplate)
    {
        GatesTypesToInt.OverrideReflection(new EmptyGate(), gatesTemplate);
        _size = size;
        _maxGen = maxGen;
        _popSize = maxPop;
        var selection = new TournamentSelection();
        var crossover = new UniformCrossover();
        var mutation = new UniformMutation(true);
        float[] w = [1, 10f];
        var max = (int)Mathf.Pow(2, nbParticles) - 1;
        _problem = new Operation(max);
        var proportional = new BitWiseEvaluator(nbParticles);
        var exact = new ExactMatchEvaluator();
        comparison = new ParticleSimulatorFitness(nbParticles, max, loopers, _problem, (proportional, 4f), (exact, 1f));
        var average = new AveragedFitness(comparison, 5);
        IFitness fitness = new CombinedFitness((new MostNullGates(), w[0]), (average, w[1]));
        var chromosome = new Chromosome(_size.X * _size.Y);
        var population = new Population(_popSize, _popSize * 4, chromosome);
        _ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation);
        _ga.Termination = new FitnessThresholdTermination(1f /*w[1] / w.Sum()*/);
        if (_nbGen > 0)
            _ga.Termination = new OrTermination(_ga.Termination, new GenerationNumberTermination(_maxGen));
        _ga.Termination = new OrTermination(_ga.Termination, new FitnessStagnationTermination(200));
        _ga.TaskExecutor = new ParallelTaskExecutor();
        Task.Run(() => _ga.Start());
        Action OnBestFitThreshold = () =>
        {
            //When we reached a good threshold, we want to favor exact matches, we keep bitwise evaluation to discrimnate potential "not matching" chromosomes, especially after this change
            UnityEngine.Debug.Log(" increasing average evaluations and changing weights to favor exact matches");
            comparison.UpdateWeight(proportional, 1f);
            comparison.UpdateWeight(exact, 8f);
            average.NumberEvaluations = (int)(average.NumberEvaluations * 1.5);
        };
        _ga.GenerationRan += (sender, args) =>
        {
            var bestChromosomeFitness = _ga.BestChromosome.Fitness;
            UnityEngine.Debug.Log($"--------------Gen finished {_nbGen}, best fitness: " + bestChromosomeFitness +
                                  "showing it on the view");
            float thresh = 0.9f;
            if (bestChromosomeFitness >= thresh)
            {
                UnityEngine.Debug.Log("Reached fitness of best chromosome threshold of " + thresh + "at gen " + _nbGen);
                OnBestFitThreshold?.Invoke();
                OnBestFitThreshold = null;
            }

            _nbGen++;
            while (viewer.Busy) //We run it till the end
                Task.Delay(100).Wait();
            viewer.Start(_ga.BestChromosome, comparison.Input(_ga.BestChromosome));
            Task.Run(() =>
            {
                while (!viewer.ResultAvailable) //We run it till the end
                    Task.Delay(100).Wait();
                _ = viewer.GetResultAndFreeLooper();
            });
        };
        _ga.TerminationReached += (sender, args) =>
        {
            UnityEngine.Debug.Log($"GA Termination Reached at generation {_nbGen} with best fitness: " +
                                  _ga.BestChromosome.Fitness);
        };
    }
}