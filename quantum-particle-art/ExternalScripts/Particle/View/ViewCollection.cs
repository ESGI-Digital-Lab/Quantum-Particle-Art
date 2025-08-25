using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.MonoBehaviour;

public static class ViewHelpers
{
    
    public static readonly Color SUP = Color.red;
    public static readonly Color MEA = new Color(0.5f, 0, 0);
    public static readonly Color ENT = Color.green;
    public static readonly Color TEL = Color.blue;

    public static Color ColorRamp360(Particle particle)
    {
        var deg = particle.Orientation.Degrees;
        Color color = Color.black;
        if (deg > 270) //Left
            color = Color.Lerp(Color.blue, Color.white, Mathf.InverseLerp(270, 360, deg));
        else if (deg < 90) //Right
            color = Color.Lerp(Color.red, Color.white, Mathf.InverseLerp(90, 0, deg));
        else //Back
            color = Color.Lerp(Color.blue, Color.red, Mathf.InverseLerp(270, 90, deg));
        //Debug.Log($" deg {deg} => color {color}");
        return color;
    }
}

public interface IView<T, TInit>
{
    void InitView(T info, TInit init,Color color);
    void UpdateView(T info);
    void Dispose();
}

public class ViewCollection<T, TView>
    where TView : Object, IView<T, ParticleWorld>
{
    private Transform _worldRoot;
    private TView[] _views;
    private Func<ParticleWorld, IEnumerable<T>> _selector;

    public ViewCollection(Transform worldRoot, TView prefab, ParticleWorld world,
        Func<ParticleWorld, IEnumerable<T>> selector, Func<T,Color> colorSelector)
    {
        _selector = selector;
        _worldRoot = worldRoot;
        var toView = selector(world).ToArray();
        _views = new TView[toView.Length];
        int i = 0;
        foreach (var v in toView)
        {
            var view = Object.Instantiate(prefab, _worldRoot);
            //view.name = $"{typeof(T).Name} View {i}";
            view.InitView(v, world,colorSelector(v));
            _views[i++] = view;
        }
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
                _views[i].Dispose();
            }
        }
    }
}