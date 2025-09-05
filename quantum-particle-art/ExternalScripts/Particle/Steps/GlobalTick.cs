using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class GlobalTick : ParticleStep
{
    private IColorPicker _colorPicker;

    public override async Task Init(WorldInitializer init)
    {
        _colorPicker = init.Init.Colors;
        await base.Init(init);
    }
    public override async Task HandleParticles(ParticleWorld entry, float delay)
    {
        float t = 1f; //_useDeltaTime ? Time.deltaTime : _timeSteps;

        for (var index = 0; index < entry.Count; index++)
        {
            var particle = entry[index];
            foreach (var info in particle.Tick(t, entry.Ruleset[particle.Species].Friction))
            {
                _world.Drawer.AddLine(info.fromNormalized, info.particle.NormalizedPosition,
                    _colorPicker.GetColor(info.particle, entry.Ruleset.NbSpecies));
            }

            if (delay > 0)
                await Task.Delay((int)(delay*1000));
        }
    }

    
}