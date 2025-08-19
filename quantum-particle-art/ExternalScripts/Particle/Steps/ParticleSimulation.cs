using System.Collections;
using UnityEngine;

public abstract class ParticleStep : MonoBehaviour, IStep<ParticleWorld>, IInit<WorldInitializer>
{
    [SerializeField] private bool _delayBeetweenEachParticle = false;
    protected ParticleWorld _world;

    public virtual IEnumerator Init(WorldInitializer initializer)
    {
        yield break;
    }

    public IEnumerator Step(ParticleWorld entry, float delay)
    {
        _world = entry;
        yield return HandleParticles(entry, _delayBeetweenEachParticle ? delay : 0f);
    }

    public abstract IEnumerator HandleParticles(ParticleWorld entry, float delay);
    public ParticleWorld Result => _world;

    public virtual void Release()
    {
    }
}

public class ParticleSimulation : APipeline<WorldInitializer, ParticleWorld>
{
    ParticleWorld _world;

    protected override IEnumerator Init(WorldInitializer init)
    {
        var _worldSize = init.Size;
        _world = new ParticleWorld(init.RandomCollection(), init.Points(), _worldSize, init.Init.Rules);
        yield break;
    }

    protected override ParticleWorld GetInput(IStep<ParticleWorld> step)
    {
        return _world;
    }

    protected override IEnumerator Sync(float delay)
    {
        yield break;
    }

    protected override IEnumerator Stepped(IStep<ParticleWorld> step, ParticleWorld result)
    {
        yield break;
    }

    protected override ParticleWorld GetLast(IStep<ParticleWorld> step)
    {
        return _world;
    }

    protected override void Disposed()
    {
    }
}