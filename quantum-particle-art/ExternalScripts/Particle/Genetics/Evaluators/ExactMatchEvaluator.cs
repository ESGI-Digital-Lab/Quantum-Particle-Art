using UnityEngine;

public class ExactMatchEvaluator : IEvaluator
{
    public double Fitness(int specificInput, int result, int expected)
    {
        //Debug.Log("ExactMatchEvaluator: result=" + result + ", expected=" + expected + (result == expected ? " (match gen)" : " (no match)"));
        return result == expected ? 1.0 : 0.0;
    }
}