using System.Collections.Generic;
using System.Drawing;
using System.Geometry;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using UnityEngine.ExternalScripts.Particle.Simulation;
using Image = Godot.Image;

public class LateWriteToTex : ParticleStep
{
    private class ParticleHistory
    {
        public record struct HistoryEntry(Vector2 position, Vector2 velocity, Godot.Color color);

        private Dictionary<Particle, List<HistoryEntry>> _history = new();
        private IColorPicker _picker;
        private int _nbSpecies;
        public IEnumerable<KeyValuePair<Particle, List<HistoryEntry>>> Entries => _history;

        public ParticleHistory(IColorPicker picker, int nbSpecies)
        {
            _picker = picker;
            _nbSpecies = nbSpecies;
        }

        public void AddRecord(Particle p)
        {
            if (!_history.ContainsKey(p))
                _history[p] = [];
            _history[p].Add(new HistoryEntry(p.NormalizedPosition, p.Orientation.Velocity,
                _picker.GetColor(p, _nbSpecies)));
        }
    }

    private Saver _saver;
    private Image _base;
    private ImageTexture _dynamic;
    private ParticleHistory _history;
    private int _curveRes = 1000;

    public LateWriteToTex(Saver saver, int curveRes)
    {
        _saver = saver;
        _curveRes = curveRes;
    }

    public override async Task Init(WorldInitializer initializer)
    {
        await base.Init(initializer);
        _history = new(initializer.Init.Colors, initializer.Init.Rules.NbSpecies);
        var aTexProvider = initializer.Texture;
        _base = aTexProvider.Texture.Duplicate() as Image;
        _dynamic = ImageTexture.CreateFromImage(_base);
        if (_saver != null)
        {
            _saver.Init(_base, aTexProvider.Name + "_");
        }
    }

    public override Task HandleParticles(ParticleWorld entry, float delay)
    {
        foreach (var p in entry.Particles.SelectMany(p => p.Pivots(false, true)))
        {
            _history.AddRecord(p.p);
        }

        return Task.CompletedTask;
    }

    public override void Release()
    {
        base.Release();
        foreach (var kvp in _history.Entries)
        {
            Bezier curve = new Bezier(kvp.Value.Select(v => v.position.ToSystemV2()).ToArray());
            int nbPoints = _curveRes;
            for (int i = 0; i < nbPoints; i++)
            {
                float t = i / (nbPoints - 1f);
                var size = Vector2I.One * 10;
                _base.FillRect(new Rect2I(curve.Position(t).ToGodotV2().ToPixelCoord(_dynamic), size), kvp.Value.First().color);
            }
        }

        _dynamic.SetImage(_base);
        if (_saver != null)
            _saver.SaveTexToDisk();
    }
}