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

    private ChromosomeConfigurationBase[] _chromosomes;
    private InitConditions[] _conditions;

    public ReplayLooper( IEnumerable<InitConditions> conditions,
        IEnumerable<IInit<WorldInitializer>> inits,
        IEnumerable<IStep<ParticleWorld>> step,
        IEnumerable<IInit<ParticleWorld>> prewarm,
        IEnumerable<ChromosomeConfigurationBase> chromosomes,
        int texHeight)
    {
        _duration = -1;
        _inits = inits.ToArray();
        _steps = step.ToArray();
        _prewarm = prewarm.ToArray();
        _chromosomes = chromosomes.ToArray();
        _conditions = conditions.ToArray();
        _init = _conditions[0];
        _texHeight = texHeight;
    }
    private int _currentLoop = 0;
    private ChromosomeConfigurationBase chromosomeConfiguration = null;

    protected override async Task<bool> UpdateInitializer(WorldInitializer init, int loop)
    {
        _init = _conditions[loop % _conditions.Length];
        init.Init = _init;
        if (_texHeight > 0)
        {
            if(!init.Init.Texture.Create())
                return false;
            init.Init.Texture.Texture.Resize((int)(_texHeight * init.Init.Ratio), _texHeight,
                Image.Interpolation.Trilinear);
        }
        if(chromosomeConfiguration == null) 
            this.chromosomeConfiguration = _chromosomes[0];
        else
        {
            if (chromosomeConfiguration.MoveNext())
            {
                _currentLoop++;
                if (_currentLoop >= _chromosomes.Length)
                {
                    _shouldRestart = false;
                    return false;
                }
                chromosomeConfiguration = _chromosomes[_currentLoop];
            }
                
        }
        
        init.SetName(chromosomeConfiguration.Name);
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