using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using UnityEngine;

public class GeneticLooper : PipelineLooper<WorldInitializer, ParticleWorld, ParticleSimulation>
{
    private int _nbLoops = 10;
    private InitConditions _init;

    private IInit<WorldInitializer>[] _inits;
    private IStep<ParticleWorld>[] _steps;
    private IInit<ParticleWorld>[] _prewarm;
    private int _texHeight;
    private EncodedConfiguration _spawn;

    public void ExternalRestart()
    {
        _shouldRestart = true;
    }

    public GeneticLooper(float duration, int nbLoops, InitConditions init,
        EncodedConfiguration spawn,
        IEnumerable<IInit<WorldInitializer>> inits,
        IEnumerable<IStep<ParticleWorld>> step,
        IEnumerable<IInit<ParticleWorld>> prewarm,
        int texHeight)
    {
        _duration = duration;
        _nbLoops = nbLoops;
        _init = init;
        _inits = inits.ToArray();
        _steps = step.ToArray();
        _prewarm = prewarm.ToArray();
        _texHeight = texHeight;
        _spawn = spawn;
    }

    protected override int Loops => _nbLoops;

    protected override async Task UpdateInitializer(WorldInitializer init, int loop)
    {
        _spawn.UpdateEncoded((int)Random.Range(0, 255));
        init.Init = _init;
        await init.Init.Texture.Create();
        init.Init.Texture.Texture.Resize((int)(_texHeight * init.Init.Ratio), _texHeight,
            Image.Interpolation.Trilinear);
    }

    protected override void OnFinished(ParticleSimulation pipeline)
    {
        Debug.LogWarning("Pipeline finished with encoder result : " + _spawn.Result());
    }

    protected override IEnumerable<IInit<ParticleWorld>> GetPrewarms() => _prewarm;

    protected override IEnumerable<IInit<WorldInitializer>> GetInits() => _inits;


    protected override IEnumerable<IStep<ParticleWorld>> GetSteps() => _steps;


    protected override ParticleSimulation GetPipeline()
    {
        return new ParticleSimulation();
    }
}