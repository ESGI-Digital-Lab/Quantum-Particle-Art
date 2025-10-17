using System.Collections.Generic;
using System.Linq;
using GeneticSharp;
using Godot;
using UnityEngine.ExternalScripts.Particle.Genetics;

public static class BitHelpers
{
    public static bool[] Bits(this int number, int nbBits)
    {
        number = Godot.Mathf.Clamp(number, 0, (int)Godot.Mathf.Pow(2, nbBits) - 1);
        bool[] bits = new bool[nbBits];
        for (int i = 0; i < nbBits; i++)
        {
            bits[nbBits - (i + 1)] = (number & 1) == 1; //Check last bit is 1
            number >>= 1; //Bit shift
        }

        return bits;
    }

    public static int DecodeBits(this IEnumerable<bool> bits)
    {
        return DecodeBits(bits.Select(b => b ? 1 : 0), 2);
    }

    public static int DecodeBits(this IEnumerable<int> copies, int baseNum)
    {
        int acc = 0;
        int i = 0;
        foreach (var copy in copies)
        {
            acc += copy * (int)Godot.Mathf.Pow(baseNum, i);
            i++;
        }

        return acc;
    }

    public static double WeightedSum(this IEnumerable<(double, double)> vws)
    {
        double sum = 0;
        double totalWeight = 0;
        foreach (var (value, weight) in vws)
        {
            sum += value * weight;
            totalWeight += weight;
        }

        //Debug.LogError("Use then logic, using only second fitness only if first is full");

        if (totalWeight > 0)
        {
            return sum / totalWeight;
        }
        else
        {
            return 0;
        }
    }

    public static IEnumerable<GateConfiguration> GetGates(IChromosome current, Vector2I size)
    {
        return current.GetGenes().Select((g, i) =>
        {
            var c = (GeneContent)g.Value;
            var type = GatesTypesToInt.Type(c.TypeId);
            return new GateConfiguration(type.DeepCopy(),
                new Vector2I(i % size.X, i / size.X));
        });
    }
}