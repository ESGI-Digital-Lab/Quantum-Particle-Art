using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class RepeatedConfiguration : ChromosomeConfigurationBase
{
    [Export] private int _repeats = -1;
    [Export] private ChromosomeConfigurationBase _baseConfig;

    private int _currentRepeat = 0;

    public override bool MoveNext()
    {
        var b = base.MoveNext();
        if (_baseConfig.MoveNext())
            _currentRepeat++;
        if (_repeats > 0 && _currentRepeat >= _repeats)
            return b && true; //For clarity, we return true, only if the base returned true
        return false;
    }
    public override string Name => _baseConfig.Name +"_i"+ _currentRepeat;

    public override IEnumerable<GateConfiguration> GatesConfig => _baseConfig.GatesConfig;

    public override Vector2I Size => _baseConfig.Size;
}