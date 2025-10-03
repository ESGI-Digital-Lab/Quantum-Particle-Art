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
    private readonly int _nbParticles;
    private readonly Vector2I _size;
    private GeneticAlgorithm _ga;

    private int _finishedCount = 0;
    private int _totalIndex = 0;
    private int _nbGen = 0;
    public event Action<IList<IChromosome>> OnGenerationReady;

    private BitwiseComparisonFitness comparison;
    private object _lock = new();
    private const int Input = 79;

    public Genetics(int nbParticles, Vector2I size, int maxGen, int maxPop, List<GeneticLooper> loopers,
        IEnumerable<AGate> gatesTemplate)
    {
        GatesTypesToInt.OverrideReflection(new EmptyGate(), gatesTemplate);
        _nbParticles = nbParticles;
        _size = size;
        _maxGen = maxGen;
        _popSize = maxPop;
        var selection = new TournamentSelection();
        var crossover = new UniformCrossover();
        var mutation = new UniformMutation(true);
        float[] w = [.15f, .85f];
        comparison = new BitwiseComparisonFitness(nbParticles, Input, Input * 2, loopers);
        IFitness fitness;
        fitness = new CombinedFitness((new MostNullGates(), w[0]), (comparison, w[1]));
        //fitness = comparison;
        var chromosome = new Chromosome(_size.X * _size.Y);
        var population = new Population(_popSize / 4, _popSize, chromosome);
        _ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation);
        _ga.Termination = new FitnessThresholdTermination(1f /*w[1] / w.Sum()*/);
        if (_nbGen > 0)
            _ga.Termination = new OrTermination(_ga.Termination, new GenerationNumberTermination(_maxGen));
        //new FitnessStagnationTermination(50),
        _ga.TaskExecutor = new ParallelTaskExecutor();
        Task.Run(() => _ga.Start());
        _ga.GenerationRan += (sender, args) =>
        {
            UnityEngine.Debug.Log($"--------------Gen finished {_nbGen}, best fitness: " + _ga.BestChromosome.Fitness);
            _nbGen++;
        };
    }
}