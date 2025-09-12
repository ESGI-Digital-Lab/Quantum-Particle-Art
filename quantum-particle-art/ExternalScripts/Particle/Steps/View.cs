using System.Collections;
using System.Threading.Tasks;
using Godot;
using UnityEngine;
using UnityEngine.Assertions;
using Color = UnityEngine.Color;

public class View : ParticleStep, IInit<ParticleWorld>
{
    [SerializeField] private Node _worldRoot;
    [SerializeField] private string _viewPrefab;
    [SerializeField] private string _pointPrefab;
    private ViewCollection<Particle, ParticleView> _particleViewCollection;
    private ViewCollection<Area2D, PointView> _pointViewCollection;
    private IColorPicker _colorPicker;

    public View(Node root, string viewPrefab, string pointPrefab)
    {
        _worldRoot = root;
        _viewPrefab = viewPrefab;
        _pointPrefab = pointPrefab;
    }

    public override async Task Init(WorldInitializer initializer)
    {
        _colorPicker = initializer.Init.Colors;
        await base.Init(initializer);
    }

    public Task Init(ParticleWorld init)
    {
        _particleViewCollection = ViewCollection<Particle, ParticleView>.Create(_worldRoot, _viewPrefab, init,
            w => w.Particles,
            p => _colorPicker.GetColor(p, init.Ruleset.NbSpecies));
        _pointViewCollection = ViewCollection<Area2D, PointView>.Create(_worldRoot, _pointPrefab, init, w => w.PointsOfInterest,
                AreaColor);
        return Task.CompletedTask;
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
            case Area2D.AreaType.Control:
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

    public override async Task HandleParticles(ParticleWorld entry, float delay)
    {
        await _particleViewCollection?.HandleParticles(entry, delay);
        await _pointViewCollection?.HandleParticles(entry, delay);
    }


    public override void Release()
    {
        base.Release();
        _particleViewCollection?.Dispose();
        _pointViewCollection?.Dispose();
    }

    public static TView Instantiate<TView>(PackedScene scene, Node worldRoot) where TView : Node
    {
        TView script = null;
        var instance = scene.Instantiate();
        worldRoot.AddChild(instance);
        script = instance as TView;
        Assert.IsNotNull(script, $"The scene does not have the expected");
        return script;
    }
}