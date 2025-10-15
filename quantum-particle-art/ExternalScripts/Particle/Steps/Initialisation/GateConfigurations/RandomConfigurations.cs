using System;
using System.Collections.Generic;
using Godot;
using KGySoft.CoreLibraries;
using UnityEngine.ExternalScripts.Particle.Genetics;

[GlobalClass]
public partial class RandomConfigurations : ChromosomeConfigurationBase
{
    [Export] private int _repeats = -1;
    [Export] private Vector2I _nbParticlesRange;

    private int _currentRepeat = 0;
    private ChromosomeConfigurationBase _current;

    public override bool MoveNext()
    {
        var b = base.MoveNext();
        _currentRepeat++;
        if (_repeats > 0 && _currentRepeat >= _repeats)
            return b && true; //For clarity, we return true, only if the base returned true
        //OnMoveNext, we discard the current and create a new one
        _current = New();
        return false;
    }

    public ChromosomeConfigurationBase New()
    {
        var size = Random.Shared.NextInt32(_nbParticlesRange.X, _nbParticlesRange.Y,true);
        var v = new Vector2I(size-1, size);
        return new ChromosomeConfiguration(new GateChromosome(v), v);
    }

    public override string Name => "random_" + Size + "_i" + _currentRepeat;

    //Very first call will be null, create and store a new
    public override IEnumerable<GateConfiguration> GatesConfig => (_current ??= New()).GatesConfig;

    public override Vector2I Size => _current.Size;
}