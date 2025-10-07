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

    private Image _base;
    private ImageTexture _dynamic;
    private ParticleHistory _history;
    private readonly Saver _saver;
    private readonly Brush _brush;
    private readonly IWidther _width;
    private readonly int _curveRes = 1000;
    private bool _saveRequested;
    private bool _saveAll = false;
    private string _cumulatedInfos = "";

    public void RequestSave(string info)
    {
        if (!string.IsNullOrEmpty(info))
            _cumulatedInfos += "_" + info;
        _saveRequested = true;
    }

    public LateWriteToTex(Saver saver, Brush brush, IWidther width, int curveRes, bool saveAll = false)
    {
        _saver = saver;
        _curveRes = curveRes;
        _brush = brush;
        _width = width;
        _saveAll = saveAll;
        _saveRequested = false;
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
            _saver.Init(_base, aTexProvider.Name + "_" + _brush.ToString() + "_" + _curveRes);
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
        if (!_saveAll && !_saveRequested)
            return;
        _saveRequested = false;
        foreach (var kvp in _history.Entries)
        {
            List<System.Numerics.Vector2> pts = new();
            var startInfo = kvp.Value.First();
            foreach (var point in kvp.Value)
            {
                if (point.continuous) //End of continuous line, draw what we have and start a new one
                {
                    pts.Add(point.position.ToSystemV2());
                }
                else
                {
                    if (pts.Count >= 2) //Else we don't draw anything
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
                        var color = startInfo.color;
                        var vel = startInfo.velocity;
                        var sampled = Enumerable.Range(0, _curveRes).Select(i => i / (nbPoints - 1f)).Select(t =>
                        {
                            var coords = curve.Position(t).ToUnityV2().ToPixelCoord(_dynamic);
                            var width =
                                //curve.Tangent(t).Length();
                                _width.DetermineWidth(point.velocity * t + vel * (1 - t));
                            return new Brush.StrokePoint(coords, point.color * t + color * (1 - t), width);
                        });
                        _brush.DrawWithBrush(_base, sampled);
                    }

                    pts = [point.position.ToSystemV2()];
                    startInfo = point;
                }
            }
        }

        _dynamic.SetImage(_base);
        if (_saver != null)
            _saver.SaveTexToDisk("-Fit" + _cumulatedInfos);
        _cumulatedInfos = "";
    }
}