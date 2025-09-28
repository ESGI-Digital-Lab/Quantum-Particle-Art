using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Godot.Collections;
using Debug = UnityEngine.Debug;

[GlobalClass]
public partial class GridGates : Resource, IGates
{
    private List<(AGate, UnityEngine.Vector2)> _gatesList;
    [Export] private bool _wrap = true;
    [Export] private bool _centered = true;
    [Export] private Vector2I _size;
    [Export] private Vector2I _originOffset;
    [Export] private bool _useGlobalOffset;
    [Export] private Vector2 _globalOffset;
    [Export] private Array<GateConfiguration> _gatesConfig;

    private List<(AGate, UnityEngine.Vector2)> BuildList()
    {
        List<(AGate, UnityEngine.Vector2)> tmp = new();
        var size = _size;
        foreach (var gateConfig in _gatesConfig)
        {
            foreach (var pos in gateConfig.Positions)
            {
                Vector2 index = pos;
                //Pos can be over size event before it was offseted
                //if (_wrap)
                //    posNorm %= size;
                var centered = index + _originOffset;
                index = centered;
                if (_wrap)
                    index = (index % size + size) % size; //Always positive modulo [0,size]
                if (_centered)
                    index -= Vector2.One * .5f;
                var final = (index + Vector2.One) / size;
                if (_useGlobalOffset)
                {
                    if (Math.Abs(_globalOffset.X - _globalOffset.Y) > .001f)
                        Debug.Log("Using non linera global offset might produce weird results depending on rotation gates, bigger gates can compensate, offsetting also on y would unalign the particles and gates tho");
                    final *= (Vector2.One - _globalOffset); //Rescale and global offset
                    final += _globalOffset;
                }
                final.Y = 1-final.Y; //Invert Y axis so we are effectively bottom left 0,0
                UnityEngine.Debug.Log(
                    $"Adding gate {gateConfig.Gate.ShortName} from {pos} with {centered} and index {index} => {final}");
                tmp.Add((gateConfig.Gate.Copy(), final));
            }
        }

        return tmp;
    }

    public IEnumerable<(AGate type, UnityEngine.Vector2 pos)> Positions => _gatesList ??= BuildList();
}