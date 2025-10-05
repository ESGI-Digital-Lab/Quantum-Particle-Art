public class ExactMatchEvaluator : IEvaluator
{
    public double Fitness(int specificInput, int result, int expected)
    {
        return result == expected ? 1.0 : 0.0;
    }
}