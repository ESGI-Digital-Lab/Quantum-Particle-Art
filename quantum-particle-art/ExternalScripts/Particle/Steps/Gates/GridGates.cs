using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class GridGates : Resource, IGates
{
    private List<(AGate, UnityEngine.Vector2)> _gatesList;
    [Export] private bool _wrap = true;
    [Export] private bool _centerd = true;
    [Export] private Vector2I _size;
    [Export] private Vector2I _originOffset;
    [Export] private Array<GateConfiguration> _gatesConfig;

    private List<(AGate, UnityEngine.Vector2)> BuildList()
    {
        List<(AGate, UnityEngine.Vector2)> tmp = new();
        foreach (var gateConfig in _gatesConfig)
        {
            foreach (var pos in gateConfig.Positions)
            {
                Vector2 index = pos;
                //Pos can be over size event before it was offseted
                //if (_wrap)
                //    posNorm %= _size;
                var centered = index + _originOffset;
                index = centered;
                if (_wrap)
                    index = (index % _size + _size) % _size; //Always positive modulo [0,size]
                if (_centerd)
                    index -= Vector2.One * .5f;
                var final = (index + Vector2.One) / _size;
                UnityEngine.Debug.Log($"Adding gate {gateConfig.Gate.ShortName} from {pos} with {centered} and index {index} => {final}");
                tmp.Add((gateConfig.Gate.Copy(), final));
            }
        }

        return tmp;
    }

    public IEnumerable<(AGate type, UnityEngine.Vector2 pos)> Positions => _gatesList ??= BuildList();
}