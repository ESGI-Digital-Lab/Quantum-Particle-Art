using System.Collections;
using UnityEngine;

public class GlobalTick : ParticleStep
{
    private ColorPicker _colorPicker;

    public override IEnumerator Init(WorldInitializer init)
    {
        _colorPicker = init.Init.Colors;
        yield return base.Init(init);
    }
    public override IEnumerator HandleParticles(ParticleWorld entry, float delay)
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
                yield return new WaitForSeconds(delay);
        }
    }

    
}