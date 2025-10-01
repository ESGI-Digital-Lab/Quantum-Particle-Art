using System;
using System.Collections.Generic;
using GeneticSharp;

namespace UnityEngine.ExternalScripts.Particle.Genetics;

public class OneLatePopulation : IPopulation
{
    private Population _populationImplementation;

    public OneLatePopulation(int size, Chromosome chromosome)
    {
        _populationImplementation = new Population(size/4, size, chromosome);
    }
    /// <summary>
    /// Only override
    /// </summary>
    public Generation CurrentGeneration => _populationImplementation.Generations[^1];

    public void CreateInitialGeneration()
    {
        _populationImplementation.CreateInitialGeneration();
    }

    public void CreateNewGeneration(IList<IChromosome> chromosomes)
    {
        _populationImplementation.CreateNewGeneration(chromosomes);
    }

    public void EndCurrentGeneration()
    {
        _populationImplementation.EndCurrentGeneration();
    }

    public DateTime CreationDate => _populationImplementation.CreationDate;

    public IList<Generation> Generations => _populationImplementation.Generations;
    
    public int GenerationsNumber => _populationImplementation.GenerationsNumber;

    public int MinSize
    {
        get => _populationImplementation.MinSize;
        set => _populationImplementation.MinSize = value;
    }

    public int MaxSize
    {
        get => _populationImplementation.MaxSize;
        set => _populationImplementation.MaxSize = value;
    }

    public IChromosome BestChromosome => _populationImplementation.BestChromosome;

    public IGenerationStrategy GenerationStrategy
    {
        get => _populationImplementation.GenerationStrategy;
        set => _populationImplementation.GenerationStrategy = value;
    }

    public event EventHandler BestChromosomeChanged
    {
        add => _populationImplementation.BestChromosomeChanged += value;
        remove => _populationImplementation.BestChromosomeChanged -= value;
    }
}