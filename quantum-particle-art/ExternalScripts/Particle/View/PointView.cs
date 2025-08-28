using Godot;
using UnityEngine.Assertions;
using Color = UnityEngine.Color;
using Vector2 = UnityEngine.Vector2;

public partial class PointView : Node2D, IView<Area2D, ParticleWorld>
{
    [Export] private Sprite2D[] _sprites;
    private Vector2 bounds;

    public void InitView(Area2D info, ParticleWorld w, Color color)
    {
        bounds = w.Size;
        foreach (var r in _sprites)
            r.Modulate = color;
    }

    public void UpdateView(Area2D info)
    {
        this.GlobalPosition = ViewHelpers.Pos(info.Center / bounds, this.GetParent() as Node2D);
        this.GlobalScale = new Godot.Vector2(info.Radius / bounds.x, info.Radius / bounds.y);
        //_root.position = ViewHelpers.WorldPosition(info.Center / bounds, _root);
        //_scale.localScale = new Vector3(info.Radius / bounds.x, 0.1f, info.Radius / bounds.y);
    }

    public void Dispose()
    {
        //GameObject.Destroy(this.gameObject);
    }
}