using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace DefaultNamespace.Particle.Steps.Gates;

[GlobalClass]
public partial class GateConfiguration : Godot.Resource
{
    [Export] private AGate _gate;
    [Export] private Godot.Collections.Array<Vector2> _positions;

    public AGate Gate => _gate;

    public Array<Vector2> Positions => _positions;
}