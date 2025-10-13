using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class GlobalTick : ParticleStep
{
    private readonly float _timeSteps;
    private readonly int _maxSteps;
    private int _nbSteps = 0;
    private IColorPicker _colorPicker;
    private bool _rearmed = true;
    public event Action onAllDead;

    public GlobalTick(float timeSteps, int maxTimeBeforeForcingDead = -1)
    {
        _timeSteps = timeSteps;
        _maxSteps = maxTimeBeforeForcingDead;
        onAllDead += () => { _rearmed = false; };
    }

    public record struct MovementData(
        Vector2 fromNormalized,
        Vector2 toNormalize,
        Color color,
        Orientation orientation);

    public event System.Action<MovementData> onMovement;

    public int NbSteps => _nbSteps;

    public override async Task Init(WorldInitializer init)
    {
        _colorPicker = init.Init.Colors;
        _nbSteps = 0;
        _rearmed = true;
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
                    _colorPicker.GetColor(info.particle, entry.Ruleset.NbSpecies) / (info.depth + 1),
                    info.particle.Orientation);
                onMovement?.Invoke(data);
            }

            if (delay > 0)
                await Task.Delay((int)(delay * 1000));
        }

        if (_maxSteps > 0 && _nbSteps % (_maxSteps / 2) == 0)
        {
            //Debug.Log($"[GlobalTick] Step {_nbSteps}/{_maxSteps} moved:{moved}");
        }

        if (!moved || (_maxSteps > 0 && _nbSteps >= _maxSteps))
        {
            if (_rearmed)
                onAllDead?.Invoke();
        }

        _nbSteps++;
    }
}