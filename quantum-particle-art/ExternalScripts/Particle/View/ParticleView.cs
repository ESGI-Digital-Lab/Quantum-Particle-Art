using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Object = System.Object;

public class ParticleView : MonoBehaviour, IView<Particle, ParticleWorld>
{
    [SerializeField, Header("Settings")] private bool _showOnlyChilds = false;

    [Header("References")] [SerializeField]
    private Renderer _renderer;

    [FormerlySerializedAs("_root")] [SerializeField]
    private Transform _pos;

    [SerializeField] private Transform _scale;
    [SerializeField] private LineRenderer _line;
    [SerializeReference] private Particle particle;
    private static Dictionary<Orientation, Particle> mapBack;
    private Tuple<ParticleView, ParticleView> _childs;
    ParticleWorld _world;

    public void InitView(Particle info, ParticleWorld world, Color color)
    {
        this.particle = info;
        _world = world;
        mapBack ??= new();
        mapBack.TryAdd(info.Orientation, particle);
        if (world != null)
        {
            _renderer.material.color = color;
            _scale.localScale = Vector3.one * (world.Ruleset.DiskSize / (world.Bounds.x + world.Bounds.y) / 2);
        }
    }


    public void UpdateView(Particle info)
    {
        if (this.particle.IsSuperposed)
        {
            this.ToggleView(!_showOnlyChilds);
            if (_childs == null)
            {
                var c1 = Instantiate(this, transform.parent);
                c1.InitView(info.Superposition.Item1, null, _renderer.material.color);
                c1.gameObject.name = $"Super1 of {gameObject.name} 1";
                var c2 = Instantiate(this, transform.parent);
                c2.InitView(info.Superposition.Item2, null, _renderer.material.color);
                c2.gameObject.name = $"Super2 of {gameObject.name} 1";
                _childs = new(c1, c2);
            }

            _childs.Item1.UpdateView(info.Superposition.Item1);
            _childs.Item2.UpdateView(info.Superposition.Item2);
            if (!_showOnlyChilds)
                this.UpdateView(info.Orientation);
            if (_showOnlyChilds)
                LineTo(info.Superposition.Item1, info.Superposition.Item2.Orientation, ViewHelpers.SUP);
            else
            {
                _childs.Item1.LineTo(info.Orientation, ViewHelpers.SUP);
                _childs.Item2.LineTo(info.Orientation, ViewHelpers.SUP);
            }
        }
        else
        {
            if (_childs != null)
            {
                _childs.Item1.ToggleView(false);
                _childs.Item2.ToggleView(false);
            }

            UpdateView(info.Orientation);
        }
    }

    public void Dispose()
    {
        mapBack = null;
        if (_childs != null)
        {
            _childs.Item1.Dispose();
            _childs.Item2.Dispose();
            _childs = null;
        }

        GameObject.Destroy(this.gameObject);
    }

    private void ToggleView(bool state)
    {
        _renderer.gameObject.SetActive(state);
    }

    public void UpdateView(Orientation Orientation)
    {
        ToggleView(true);
        _pos.position = ViewHelpers.WorldPosition(particle, _pos);
        ApplyOrientation(Orientation);
        //if (Orientation.NormalizedSpeed <= 0.0f)
        //    _renderer.material.color = Color.gray;
        //_scale.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.8f, 0.1f, 0.1f), Orientation.NormalizedSpeed);
        if (Orientation.IsEntangled)
            LineTo(Orientation.Entanglement, ViewHelpers.ENT);
        else if (Orientation.IsTeleported)
            LineTo(Orientation.Teleportation, ViewHelpers.TEL);
        else
            _line.SetPositions(Array.Empty<Vector3>());
    }

    private void LineTo(Orientation to, Color color)
    {
        LineTo(this.particle, to, color);
    }

    private void LineTo(Particle from, Orientation to, Color color)
    {
        if (mapBack.TryGetValue(to, out var target))
        {
            _line.SetPositions(new[]
            {
                ViewHelpers.WorldPosition(from, _pos),
                ViewHelpers.WorldPosition(target, _pos)
            });
            //_line.colorGradient = new Gradient()
            //{
            //    alphaKeys = new GradientAlphaKey[] { new(1, 0f) },
            //    colorKeys = new GradientColorKey[] { new(color, 0f) }
            //};
            _line.material.color = color;
        }
    }

    private void ApplyOrientation(Orientation or)
    {
        var deg = or.Degrees;
        _pos.localEulerAngles = new Vector3(0f, deg, 0f);
    }
}