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

    public GateConfiguration() : this(null, [])
    {
    }

    public GateConfiguration(AGate gate, Vector2I position) : this(gate, [position])
    {
    }

    public GateConfiguration(AGate gate, IEnumerable<Vector2I> positions)
    {
        this._gate = gate;
        _positions = new(positions);
    }

    public AGate Gate => _gate;

    public Array<Vector2I> Positions => _positions;
}