using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Geometry;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using UnityEngine.ExternalScripts.Particle.Simulation;
using Color = Godot.Color;
using Image = Godot.Image;

public class LateWriteToTex : ParticleStep
{
    private class ParticleHistory
    {
        public record struct HistoryEntry(Vector2 position, Vector2 velocity, Godot.Color color, bool continuous);

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
            var entry = new HistoryEntry(p.NormalizedPosition, p.Orientation.Velocity,
                _picker.GetColor(p, _nbSpecies), !p.WrappedLastTick);
            if (!_history.ContainsKey(p))
                _history[p] = [entry];
            else
                _history[p].Add(entry);
        }
    }

    private Saver _saver;
    private Image _base;
    private ImageTexture _dynamic;
    private ParticleHistory _history;
    private Brush _brush;
    private int _curveRes = 1000;

    public LateWriteToTex(Saver saver, Brush brush, int curveRes)
    {
        _saver = saver;
        _curveRes = curveRes;
        _brush = brush;
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
            List<System.Numerics.Vector2> pts = new();
            Godot.Color startColor = kvp.Value.First().color;
            foreach (var point in kvp.Value)
            {
                if (point.continuous) //End of continuous line, draw what we have and start a new one
                {
                    pts.Add(point.position.ToSystemV2());
                }
                else
                {
                    if (pts.Count >= 2)//Else we don't draw anything
                    {
                        Bezier curve = null;
                        try
                        {
                            curve = new Bezier(pts.ToArray());
                        }
                        catch (Exception e)
                        {
                            UnityEngine.Debug.Log("Bezier creation failed with pts: " + string.Join(", ", pts) +
                                                  " error: " + e);
                            return;
                        }

                        int nbPoints = _curveRes;
                        var sampled = Enumerable.Range(0,_curveRes).Select(i => i / (nbPoints - 1f)).Select(t => curve.Position(t).ToUnityV2().ToPixelCoord(_dynamic));
                        var color = startColor;
                        _brush.DrawWithBrush(_base, sampled, i =>
                        {
                            float  t = i / (nbPoints - 1f);
                            return point.color * t + color * (1 - t);
                        }, 1f);
                    }

                    pts = [point.position.ToSystemV2()];
                    startColor = point.color;
                }
            }
        }

        _dynamic.SetImage(_base);
        if (_saver != null)
            _saver.SaveTexToDisk();
    }
}