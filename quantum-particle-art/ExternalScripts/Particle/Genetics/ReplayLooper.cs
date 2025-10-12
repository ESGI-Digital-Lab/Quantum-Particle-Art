using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeneticSharp;
using Godot;
using KGySoft.CoreLibraries;
using UnityEngine;
using UnityEngine.ExternalScripts.Particle.Genetics;
using Vector2 = Godot.Vector2;

public class ReplayLooper : PipelineLooper<WorldInitializer, ParticleWorld, ParticleSimulation>
{
    private InitConditions _init;

    private IInit<WorldInitializer>[] _inits;
    private IStep<ParticleWorld>[] _steps;
    private IInit<ParticleWorld>[] _prewarm;
    private int _texHeight;
    private EncodedConfiguration _spawn => _init.Spawn;

    private ChromosomeConfiguration[] _chromosomes;

    public ReplayLooper(InitConditions init,
        IEnumerable<IInit<WorldInitializer>> inits,
        IEnumerable<IStep<ParticleWorld>> step,
        IEnumerable<IInit<ParticleWorld>> prewarm,
        IEnumerable<ChromosomeConfiguration> chromosomes,
        int texHeight)
    {
        _duration = -1;
        _init = init;
        _inits = inits.ToArray();
        _steps = step.ToArray();
        _prewarm = prewarm.ToArray();
        _chromosomes = chromosomes.ToArray();
        _texHeight = texHeight;
    }

    protected override async Task<bool> UpdateInitializer(WorldInitializer init, int loop)
    {
        if (loop >= _chromosomes.Length)
        {
            _shouldRestart = false;
            return false;
        }
        init.Init = _init;
        if (_texHeight > 0)
        {
            await init.Init.Texture.Create();
            init.Init.Texture.Texture.Resize((int)(_texHeight * init.Init.Ratio), _texHeight,
                Image.Interpolation.Trilinear);
        }
        ChromosomeConfiguration chromosomeConfiguration = _chromosomes[loop];
        init.SetName(chromosomeConfiguration.FileName());
        _spawn.UpdateEncoded(chromosomeConfiguration.RandomInput, chromosomeConfiguration.Size.Y);
        _spawn.UpdateDynamicGates(chromosomeConfiguration.GatesConfig);
        return true;
    }

    protected override void OnFinished(ParticleSimulation pipeline)
    {
        
        _shouldRestart = true;
    }

    protected override IEnumerable<IInit<ParticleWorld>> GetPrewarms() => _prewarm;

    protected override IEnumerable<IInit<WorldInitializer>> GetInits() => _inits;


    protected override IEnumerable<IStep<ParticleWorld>> GetSteps() => _steps;


    protected override ParticleSimulation GetPipeline()
    {
        return new ParticleSimulation();
    }
}