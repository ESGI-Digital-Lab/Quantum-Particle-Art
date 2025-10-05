using System;

public interface IEvaluator
{
    public double Fitness(int specificInput, int result, int expected);
}
public class DelegateEvaluator : IEvaluator
{
    private Func<int, int, int, double> _func;
    public DelegateEvaluator(Func<int, int, int, double> func)
    {
        _func = func;
    }

    public double Fitness(int specificInput, int result, int expected)
    {
        return _func(specificInput, result, expected);
    }
}
