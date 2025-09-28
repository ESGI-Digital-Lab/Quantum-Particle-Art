using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using Godot.Collections;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

[GlobalClass]
public partial class GridGates : Resource, IGates
{
    private List<(AGate, UnityEngine.Vector2)> _gatesList;
    [Export] private bool _wrap;
    [Export] private bool _centered;
    [Export] private Vector2I _size;
    [Export] private Vector2I _originOffset;
    [Export] private bool _useGlobalOffset;
    [Export] private Vector2 _globalOffset;
    [Export] private Array<GateConfiguration> _gatesConfig;
    private readonly System.Collections.Generic.Dictionary<AGate, List<AGate>> _copies = new();

    public IEnumerable<T> Copies<T>(T source) where T : AGate
    {
        return _copies.TryGetValue(source, out var list) ? list.Cast<T>() : [];
    }

    public GridGates() : this(new Vector2I(10, 10))
    {
    }

    public GridGates(Vector2I size, Vector2I originOffset = default,
        IEnumerable<GateConfiguration> gatesConfig = null,
        bool wrap = true,
        bool centered = true,
        bool useGlobalOffset = false, Vector2 globalOffset = default)
    {
        this._size = size;
        this._originOffset = originOffset;
        this._wrap = wrap;
        this._centered = centered;
        this._useGlobalOffset = useGlobalOffset;
        this._globalOffset = globalOffset;
        this._gatesConfig = new Array<GateConfiguration>(gatesConfig ?? []);
        this._gatesList = BuildList(); //Rebuild
    }

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
                        Debug.Log(
                            "Using non linera global offset might produce weird results depending on rotation gates, bigger gates can compensate, offsetting also on y would unalign the particles and gates tho");
                    final *= (Vector2.One - _globalOffset); //Rescale and global offset
                    final += _globalOffset;
                }

                final.Y = 1 - final.Y; //Invert Y axis so we are effectively bottom left 0,0
                Assert.IsNotNull(gateConfig.Gate, "Gate cannot be null in configuration");
                UnityEngine.Debug.Log(
                    $"Adding gate {gateConfig.Gate.ShortName} from {pos} with {centered} and index {index} => {final}");
                var copy = gateConfig.Gate.Copy();
                if (_copies.TryGetValue(gateConfig.Gate, out var list))
                    list.Add(copy);
                else
                    _copies.Add(gateConfig.Gate, [copy]);
                tmp.Add((copy, final));
            }
        }

        return tmp;
    }

    public IEnumerable<(AGate type, UnityEngine.Vector2 pos)> Positions => _gatesList ??= BuildList();
}