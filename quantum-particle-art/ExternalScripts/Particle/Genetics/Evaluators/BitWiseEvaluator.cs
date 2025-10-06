using System.Linq;

public class BitWiseEvaluator : IEvaluator
{
    private int _nbBits;
    public BitWiseEvaluator(int nbBits)
    {
        _nbBits = nbBits;
    }

    public double Fitness(int specificInput, int result, int expected)
    {
        double tot = _nbBits;
        int found = result;
        foreach (var couple in found.Bits(_nbBits).Zip(expected.Bits(_nbBits)))
        {
            if (couple.First != couple.Second) tot--;
        }

        return tot / _nbBits;
    }
}