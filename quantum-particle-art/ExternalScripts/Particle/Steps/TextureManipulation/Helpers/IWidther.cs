using Godot;

public interface IWidther
{
    public float DetermineWidth(Particle data)
    {
        return DetermineWidth(data.Orientation.Velocity);
    }
    public float DetermineWidth(Vector2 velocity);
}