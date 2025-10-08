using GeneticSharp;
using UnityEngine.ExternalScripts.Particle.Genetics;

public class Mutation : MutationBase
{
    private float _emptyProbability;
    public Mutation(float emptyProbability) : base()
    {
        _emptyProbability = emptyProbability;
    }
    protected override void PerformMutate(IChromosome chromosome, float probability)
    {
        var rnd = RandomizationProvider.Current;
        if(rnd.GetFloat() >= probability)
            return;
        int index = rnd.GetInt(0, chromosome.Length);
        if(rnd.GetDouble() < _emptyProbability)
            chromosome.ReplaceGene(index, new Gene(new GeneContent(GatesTypesToInt.NullId)));
        else
            chromosome.ReplaceGene(index, chromosome.GenerateGene(index));
    }
}