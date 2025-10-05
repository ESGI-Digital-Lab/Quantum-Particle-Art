public class Operation : IProblem
{
    public Operation(int maxValue)
    {
        _maxValue = maxValue;
    }

    private readonly int _maxValue;

    public int CreateNewInput() => UnityEngine.Random.Range(1, (_maxValue + 1) / 2);

    public int Expected(int input) => input * 2 - 1;
}