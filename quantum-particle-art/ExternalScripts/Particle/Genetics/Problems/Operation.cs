public class Operation : IProblem
{
    public Operation(int maxValue)
    {
        _maxValue = maxValue;
    }

    private readonly int _maxValue;

    public int CreateNewInput() => UnityEngine.Random.Range(1, _maxValue);

    public int Expected(int input) => (input * 2) % _maxValue;
}