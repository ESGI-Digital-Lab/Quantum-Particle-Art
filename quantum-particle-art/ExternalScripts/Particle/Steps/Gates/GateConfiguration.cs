using System.Collections.Generic;
using Godot;
using Godot.Collections;
using UnityEngine;
using Vector2 = Godot.Vector2;

[GlobalClass]
public partial class GateConfiguration : Godot.Resource
{
    [Export] private AGate _gate;
    [Export] private Godot.Collections.Array<Vector2I> _positions;

    public AGate Gate => _gate;

    public Array<Vector2I> Positions => _positions;
}