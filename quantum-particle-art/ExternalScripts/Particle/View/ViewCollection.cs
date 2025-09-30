using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using UnityEngine;
using Color = UnityEngine.Color;
using Object = Godot.Node;
using Vector3 = UnityEngine.Vector3;

public interface IView<T, TInit>
{
    void InitView(T info, TInit init, Color color);
    void UpdateView(T info);
    void Cleanup();
}

public class ViewCollection<T, TView>
    where TView : Object, IView<T, ParticleWorld>
{
    private Node _worldRoot;
    private ParticleWorld _world;
    private TView[] _views;
    private T[] _toView;
    private Func<ParticleWorld, IEnumerable<T>> _selector;
    private Func<T, Color> _colorSelector;
    private PackedScene _scene;

    public static ViewCollection<T, TView> Create(Node worldRoot, string prefab, ParticleWorld world,
        Func<ParticleWorld, IEnumerable<T>> selector, Func<T, Color> colorSelector)
    {
        var coll = new ViewCollection<T, TView>(worldRoot, prefab, selector, colorSelector)
        {
            _world = world,
            _toView = selector(world).ToArray(),
        };
        coll._views = new TView[coll._toView.Length];
        coll.FillDeffered();
        return coll;
    }

    private void FillDeffered()
    {
        View.CallDeferred(FillCollection, _worldRoot);
    }

    private void FillCollection()
    {
        int i = 0;
        foreach (var v in _toView)
        {
            var view = View.Instantiate<TView>(this._scene, _worldRoot);
            view.Name = $"{typeof(T).Name} View {i}";
            view.InitView(v, _world, _colorSelector(v));
            this._views[i++] = view;
        }
    }

    private ViewCollection(Node worldRoot, string prefab,
        Func<ParticleWorld, IEnumerable<T>> selector, Func<T, Color> colorSelector)
    {
        _scene = GD.Load<PackedScene>(prefab);
        _selector = selector;
        _worldRoot = worldRoot;
        _colorSelector = colorSelector;
    }


    public async Task HandleParticles(ParticleWorld entry, float delay)
    {
        View.CallDeferred(() => UpdateViews(), _worldRoot);
    }

    private Task UpdateViews()
    {
        if (_views == null || _views.Length == 0)
            return Task.CompletedTask;
        int i = 0;
        foreach (var p in _selector.Invoke(_world))
        {
            var view = _views[i++];
            view.UpdateView(p);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        for (int i = 0; i < _views.Length; i++)
        {
            if (_views[i] != null)
            {
                _views[i].Cleanup();
            }
        }
    }
}