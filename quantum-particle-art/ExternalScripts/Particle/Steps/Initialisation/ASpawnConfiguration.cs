using System.Collections.Generic;
using Godot;
using UnityEngine.ExternalScripts.Particle.Simulation;
[GlobalClass]
public abstract partial class ASpawnConfiguration : Resource
{
    [Export] private bool _skip = false;
    public bool Skip => _skip;
    [ExportGroup("Particles")] [Export] protected int _nbParticles = 1;
    [Export] private int _specyIndex = -1;
    [ExportGroup("Speed")] [Export] private float _velocityScale = 1f;
    public abstract IGates Gates { get; }


    public abstract IEnumerable<UnityEngine.Vector2> Particles(System.Random random);

    public virtual int GetSpecy(Vector2 pos, ISpecyPicker backup)
    {
        return _specyIndex >= 0 ? _specyIndex : backup.SpeciyIndex(pos);
    }

    public UnityEngine.Vector2 Velocity => BaseVelocity() * _velocityScale;
    protected abstract UnityEngine.Vector2 BaseVelocity();

    protected static IEnumerable<UnityEngine.Vector2> LinearReparition(Vector2 lb, Vector2 ub, int nb)
    {
        float prop = nb > 0 ? 1f / (nb) : 1f;
        for (int i = 0; i < nb; i++)
        {
            float t = (i + .5f) * (prop);
            Vector2 normalizedPos = new Vector2(Mathf.Lerp(lb.X, ub.X, t), Mathf.Lerp(lb.Y, ub.Y, t));
            yield return normalizedPos;
        }
    }
}