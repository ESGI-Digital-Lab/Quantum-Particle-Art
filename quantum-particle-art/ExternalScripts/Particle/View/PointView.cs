using UnityEngine.Assertions;
using UnityEngine;

public class PointView : MonoBehaviour, IView<Area2D, ParticleWorld>
{
    [SerializeField] private Transform _root;
    [SerializeField] private Transform _scale;
    [SerializeField] private Renderer[] _renderer;
    private Vector2 bounds;

    public void InitView(Area2D info, ParticleWorld w,Color color)
    {
        bounds = w.Size;
        foreach (var r in _renderer)
            r.material.color = color;
    }

    public void UpdateView(Area2D info)
    {
        _root.position = ViewHelpers.WorldPosition(info.Center / bounds, _root);
        _scale.localScale = new Vector3(info.Radius / bounds.x, 0.1f, info.Radius / bounds.y);
    }

    public void Dispose()
    {
        GameObject.Destroy(this.gameObject);
    }
}