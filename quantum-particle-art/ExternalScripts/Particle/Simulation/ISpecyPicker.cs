using Godot;

namespace UnityEngine.ExternalScripts.Particle.Simulation;

public interface ISpecyPicker
{
    public int SpeciyIndex(Vector2 position);
}
public class UniformSpecyPicker : ISpecyPicker
{
    private int _nbSpecies;
    private System.Random _random = new System.Random();
    public UniformSpecyPicker(int nbSpecies)
    {
        _nbSpecies = nbSpecies;
    }
    public int SpeciyIndex(Vector2 position)
    {
        return _random.Next(_nbSpecies);
    }
}