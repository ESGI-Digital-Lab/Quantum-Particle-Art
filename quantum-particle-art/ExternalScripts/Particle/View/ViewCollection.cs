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
    private TView[] _views;
    private Func<ParticleWorld, IEnumerable<T>> _selector;
    private PackedScene scene;

    public static ViewCollection<T, TView> Create(Node worldRoot, string prefab, ParticleWorld world,
        Func<ParticleWorld, IEnumerable<T>> selector, Func<T, Color> colorSelector)
    {
        var coll = new ViewCollection<T, TView>(worldRoot, prefab, selector);
        var toView = selector(world).ToArray();
        coll._views = new TView[toView.Length];
        int i = 0;
        foreach (var v in toView)
        {
            var view = View.Instantiate<TView>(coll.scene, worldRoot);
            view.Name = $"{typeof(T).Name} View {i}";
            view.InitView(v, world, colorSelector(v));
            coll._views[i++] = view;
        }

        return coll;
    }

    private ViewCollection(Node worldRoot, string prefab,
        Func<ParticleWorld, IEnumerable<T>> selector)
    {
        scene = GD.Load<PackedScene>(prefab);
        _selector = selector;
        _worldRoot = worldRoot;
    }

    public async Task HandleParticles(ParticleWorld entry, float delay)
    {
        int i = 0;
        foreach (var p in _selector.Invoke(entry))
        {
            var view = _views[i++];
            view.UpdateView(p);
            if (delay > 0)
                await WaitForSeconds.Delay(delay);
        }
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