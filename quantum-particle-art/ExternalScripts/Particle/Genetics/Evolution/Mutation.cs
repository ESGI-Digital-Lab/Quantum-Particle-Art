using GeneticSharp;
using Godot;
using UnityEngine.ExternalScripts.Particle.Genetics;

public class Mutation : MutationBase
{
    private float _emptyProbability;
    private float _blockResetProbability = 0.1f;
    private Vector2I _size;

    public Mutation(float emptyProbability, float blockResetProbability, Vector2I size) : base()
    {
        _emptyProbability = emptyProbability;
        _blockResetProbability = blockResetProbability;
        _size = size;
    }

    protected override void PerformMutate(IChromosome chromosome, float probability)
    {
        var rnd = RandomizationProvider.Current;
        if (rnd.GetFloat() > _blockResetProbability)
        {
            var block = new Vector2I(rnd.GetInt(0, _size.X), rnd.GetInt(0, _size.Y));
            int blockSize = rnd.GetInt(1, 3);
            for (int x = block.X; x < blockSize; x++)
            {
                for (int y = block.Y; y < blockSize; y++)
                {
                    int index = ((block.X + x) % _size.X) * _size.Y + ((block.Y + y) % _size.Y);
                    chromosome.ReplaceGene(index, new Gene(new GeneContent(GatesTypesToInt.NullId)));
                }
            }
        }

        if (rnd.GetFloat() < probability)
        {
            int index = rnd.GetInt(0, chromosome.Length);
            if (rnd.GetDouble() < _emptyProbability)
                chromosome.ReplaceGene(index, new Gene(new GeneContent(GatesTypesToInt.NullId)));
            else
                chromosome.ReplaceGene(index, chromosome.GenerateGene(index));
        }
    }
}