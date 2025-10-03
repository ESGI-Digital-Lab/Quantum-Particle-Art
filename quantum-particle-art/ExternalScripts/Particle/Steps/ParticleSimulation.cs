using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public abstract class ParticleStep : IStep<ParticleWorld>, IInit<WorldInitializer>
{
    [SerializeField] private bool _delayBeetweenEachParticle = false;
    protected ParticleWorld _world;

    public virtual async Task Init(WorldInitializer initializer)
    {
    }

    public async Task Step(ParticleWorld entry, float delay)
    {
        _world = entry;
        //Debug.LogWarning("Running step " + GetType().Name);
        await HandleParticles(entry, _delayBeetweenEachParticle ? delay : 0f);
    }

    public abstract Task HandleParticles(ParticleWorld entry, float delay);
    public ParticleWorld Result => _world;

    public virtual void Release()
    {
    }
}

public class ParticleSimulation : APipeline<WorldInitializer, ParticleWorld>
{
    ParticleWorld _world;

    protected override async Task Init(WorldInitializer init)
    {
        var _worldSize = init.Size;
        _world = new ParticleWorld(init.RandomCollection(), init.Points(), _worldSize, init.Init.Rules);
    }

    protected override ParticleWorld GetInput(IStep<ParticleWorld> step)
    {
        return _world;
    }

    protected override async Task Sync(float delay)
    {
        await Task.CompletedTask;
        //await Task.Delay((int)(delay*1000));
    }

    protected override async Task Stepped(IStep<ParticleWorld> step, ParticleWorld result)
    {
    }

    protected override ParticleWorld GetLast(IStep<ParticleWorld> step)
    {
        return _world;
    }

    protected override void Disposed()
    {
    }
}