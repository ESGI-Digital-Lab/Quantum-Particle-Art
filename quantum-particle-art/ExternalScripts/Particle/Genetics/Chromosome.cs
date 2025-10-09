using System.Collections.Generic;
using GeneticSharp;

namespace UnityEngine.ExternalScripts.Particle.Genetics;

public class Chromosome : ChromosomeBase
{
    private int _length;
    private List<int> inputsTestedOn = new();
    public int? Input => inputsTestedOn!= null && inputsTestedOn.Count > 0 ? inputsTestedOn[Random.Range(0, inputsTestedOn.Count)] : null;

    public IReadOnlyList<int> InputsTestedOn => inputsTestedOn;

    public void AddInputTestedOn(int input)
    {
        inputsTestedOn.Add(input);
    }

    public Chromosome(int length) : base(length)
    {
        _length = length;
        for (int i = 0; i < length; i++)
        {
            ReplaceGene(i, RandomGene());
        }
    }

    public override Gene GenerateGene(int geneIndex)
    {
        return RandomGene();
    }

    private static Gene RandomGene()
    {
        return new Gene(new GeneContent((byte)RandomizationProvider.Current.GetInt(0, GatesTypesToInt.Count)));
        //,(byte)RandomizationProvider.Current.GetInt(0,255)));
    }

    public override IChromosome CreateNew()
    {
        return new Chromosome(_length);
    }
}