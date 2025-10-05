
public class Operation : IProblem
{
    public Operation(int maxValue)
    {
        _maxValue = maxValue;
    }
    private readonly int _maxValue;

    public int CreateNewInput() => (int)UnityEngine.Random.Range(1, _maxValue/2f);

    public int Expected(int input) => input * 2;
}