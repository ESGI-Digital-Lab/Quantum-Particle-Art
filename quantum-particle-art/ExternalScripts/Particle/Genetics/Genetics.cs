using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    public event Action<IPopulation> OnGenerationReady;

    private IntComparisonFitness comparison;
    private object _lock = new();

    public Genetics(int nbParticles, Vector2I size, int maxGen, int maxPop, ref Action dataReadyTrigger)
    {
        GatesTypesToInt.OverrideReflection([typeof(Rotate), typeof(Union), typeof(EmptyGate), typeof(Speed)]);
        _nbParticles = nbParticles;
        _size = size;
        _maxGen = maxGen;
        _popSize = maxPop;
        var selection = new TournamentSelection();
        var crossover = new UniformCrossover();
        var mutation = new UniformMutation(true);
        float[] w = [.15f, .85f];
        comparison = new IntComparisonFitness();
        IFitness fitness;
        fitness = new CombinedFitness((new MostNullGates(), w[0]), (comparison, w[1]));
        //fitness = comparison;
        var chromosome = new Chromosome(_size.X * _size.Y);
        var population = new Population(_popSize / 4, _popSize, chromosome);
        _ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation);
        _ga.Termination = new OrTermination(new FitnessThresholdTermination(1f /*w[1] / w.Sum()*/),
            //new FitnessStagnationTermination(50),
            new GenerationNumberTermination(_maxGen));
        //We start the GA when the trigger is activated
        dataReadyTrigger += _ga.Start;
        _ga.GenerationRan += (sender, args) =>
        {
            //When we finished a generation, we stop the GA and wait for the next trigger
            _ga.Stop();
            UnityEngine.Debug.Log("--------------Gen finished, best fitness: " + _ga.BestChromosome.Fitness);
            OnGenerationReady?.Invoke(_ga.Population);
        };
    }

    public GeneticAlgorithm GA => _ga;

    public int FinishedCount => _finishedCount;

    public int TotalIndex => _totalIndex;

    public object Lock => _lock;


    public int GetInput()
    {
        return 79;
    }

    public void SetResult(IChromosome current, int result)
    {
        var valuesArray =
            ""; //string.Join(", ",current.GetGenes().Select(g=> "["+((GeneContent) g.Value).ToString())+"]").ToArray();
        UnityEngine.Debug.Log("setting result " + result + " for chromosome " + valuesArray);
        comparison.UpdateResult(current.GetGenes(), result, GetInput() * 2);
    }

    public IEnumerable<GateConfiguration> GetGates(IChromosome current)
    {
        return current.GetGenes().Select((g, i) =>
        {
            var c = (GeneContent)g.Value;
            //GetConstructor([typeof(byte)]).Invoke([c.Input]);
            var type = GatesTypesToInt.Type(c.TypeId);
            return new GateConfiguration(type.GetConstructor([]).Invoke([]) as AGate,
                new Vector2I(i % _size.X + 1, i / _size.X));
        });
        return Enumerable.Range(1, _nbParticles - 2)
            .Select<int, GateConfiguration>(i =>
                new(new Rotate(45), [new(i, (int)Random.Range(0, _nbParticles))]));
    }

    public void IncrementTotalIndex() => _totalIndex++;

    public void IncrementFinishedCount() => _finishedCount++;

    public void ResetCounts()
    {
        _finishedCount = 0;
        _totalIndex = 0;
    }
}