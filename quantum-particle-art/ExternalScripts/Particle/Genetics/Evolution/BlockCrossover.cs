using System.Collections.Generic;
using System.Linq;
using GeneticSharp;
using Godot;


public class BlockCrossover : CrossoverBase
{
    private Vector2I _size;

    public BlockCrossover(Vector2I size) : base(4, 4)
    {
        _size = size;
    }

    protected override IList<IChromosome> PerformCross(IList<IChromosome> parents)
    {
        var rnd = RandomizationProvider.Current;
        Vector2I cuts = new Vector2I(rnd.GetInt(0, _size.X), rnd.GetInt(0, _size.Y));
        List<IChromosome> results = new List<IChromosome>();
        for (int c = 0; c < 4; c++)
        {
            var n = parents[c].CreateNew();
            IChromosome[] quadrants = new IChromosome[4];
            for (int q = 0; q < 4; q++)
                quadrants[q] = parents[rnd.GetInt(0, q)];
            for (int x = 0; x < _size.X; x++)
            {
                for (int y = 0; y < _size.Y; y++)
                {
                    var owner = quadrants[QuadrantIndex(x, y, cuts)];
                    var index = x + y * _size.X;
                    n.ReplaceGene(index, owner.GetGene(index));
                }
            }

            results.Add(n);
        }

        return results;
    }

    private int QuadrantIndex(int x, int y, Vector2I _cuts)
    {
        var i = -1;
        if (x < _cuts.X)
        {
            if (y < _cuts.Y)
                i = 0;
            else
                i = 2;
        }
        else
        {
            if (y < _cuts.Y)
                i = 1;
            else
                i = 3;
        }

        return i;
    }
}