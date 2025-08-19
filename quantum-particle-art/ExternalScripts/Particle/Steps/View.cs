using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

public class View : ParticleStep, IInit<ParticleWorld>
{
    [SerializeField] private Transform _worldRoot;
    [SerializeField] private ParticleView _viewPrefab;
    [SerializeField] private PointView _pointPrefab;
    private ViewCollection<Particle, ParticleView> _particleViewCollection;
    private ViewCollection<Area2D, PointView> _pointViewCollection;
    private ColorPicker _colorPicker;
    public override IEnumerator Init(WorldInitializer initializer)
    {
        _colorPicker = initializer.Init.Colors;
        yield return base.Init(initializer);
    }

    public IEnumerator Init(ParticleWorld init)
    {
        _particleViewCollection =
            new ViewCollection<Particle, ParticleView>(_worldRoot, _viewPrefab, init, w => w.Particles,  p=> _colorPicker.GetColor(p, init.Ruleset.NbSpecies));
        _pointViewCollection =
            new ViewCollection<Area2D, PointView>(_worldRoot, _pointPrefab, init, w => w.PointsOfInterest, AreaColor);
        yield break;
    }

    private static Color AreaColor(Area2D p)
    {
        var color = Color.black;
        switch (p.Type)
        {
            case Area2D.AreaType.None:
                color = Color.gray;
                break;
            case Area2D.AreaType.Superpose:
                color = ViewHelpers.SUP;
                break;
            case Area2D.AreaType.Entangle:
                color = ViewHelpers.ENT;
                break;
            case Area2D.AreaType.Teleport:
                color = ViewHelpers.TEL;
                break;
            case Area2D.AreaType.Measure:
                color = ViewHelpers.MEA;
                break;
            default:
                Assert.IsTrue(false, $"Unhandled point type: {p.Type}");
                break;
        }

        return color;
    }

    public override IEnumerator HandleParticles(ParticleWorld entry, float delay)
    {
        yield return _particleViewCollection?.HandleParticles(entry, delay);
        yield return _pointViewCollection?.HandleParticles(entry, delay);
    }


    public override void Release()
    {
        base.Release();
        _particleViewCollection?.Dispose();
        _pointViewCollection?.Dispose();
    }
}