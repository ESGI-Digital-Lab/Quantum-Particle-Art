using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class GlobalTick : ParticleStep
{
    private float _timeSteps;
    private bool _autoStop;
    private IColorPicker _colorPicker;
    public event Action onAllDead;
    public GlobalTick(float timeSteps, bool stopOnAllDead = true){
        _timeSteps = timeSteps;
        _autoStop = stopOnAllDead;
    }
    public record struct MovementData(Vector2 fromNormalized, Vector2 toNormalize, Color color, Orientation orientation);

    public event System.Action<MovementData> onMovement;

    public override async Task Init(WorldInitializer init)
    {
        _colorPicker = init.Init.Colors;
        await base.Init(init);
    }

    public override async Task HandleParticles(ParticleWorld entry, float delay)
    {
        float t = _timeSteps; //_useDeltaTime ? Time.deltaTime : _timeSteps;

        bool moved = false;
        for (var index = 0; index < entry.Count; index++)
        {
            var particle = entry[index];
            foreach (var info in particle.Tick(t, entry.Ruleset[particle.Species].Friction))
            {
                moved = true;
                var data = new MovementData(info.fromNormalized, info.particle.NormalizedPosition,
                    _colorPicker.GetColor(info.particle, entry.Ruleset.NbSpecies)/(info.depth+1),
                    info.particle.Orientation);
                onMovement?.Invoke(data);
            }

            if (delay > 0)
                await Task.Delay((int)(delay*1000));
        }

        if (!moved)
        {
            onAllDead?.Invoke();
        }
    }

    
}