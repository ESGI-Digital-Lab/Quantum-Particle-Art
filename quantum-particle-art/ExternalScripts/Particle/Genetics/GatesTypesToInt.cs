using System;
using System.Collections.Generic;
using System.Linq;
using GeneticSharp;
using UnityEngine.Assertions;

namespace UnityEngine.ExternalScripts.Particle.Genetics;

public static class GatesTypesToInt
{
    private static AGate NullGate;
    private static Dictionary<AGate, byte> _typesMap;
    private static Dictionary<byte, AGate> _mapBack;

    public static byte Count
    {
        get
        {
            if (_typesMap == null) CreateMaps();
            return (byte)_typesMap.Count;
        }
    }
    public static bool IsNullId(byte geneTypeId)
    {
        return geneTypeId == NullId;
    }
    public static byte NullId => _typesMap[NullGate];

    public static AGate Type(byte id)
    {
        if (_mapBack == null) CreateMaps();
        return _mapBack[id];
    }

    private static void CreateMaps()
    {
        OverrideReflection(null,[]);
    }

    public static void OverrideReflection(AGate nullGate, IEnumerable<AGate> types)
    {
        NullGate = nullGate;
        _typesMap = new Dictionary<AGate, byte>();
        _mapBack = new Dictionary<byte, AGate>();
        byte id = 0;
        foreach (var type in types.Append(NullGate))
        {
            if (type == null)
            {
                Debug.LogError($"Null gate passed in type list, probably mistake in the inspector");
                continue;
            }
            //In case we also find null class, we only want it once
            _typesMap[type] = id;
            _mapBack[id] = type;


            Assert.IsFalse(id >= byte.MaxValue, $"Too many types {id} for byte encoding");
            id += 1;
        }
    }
}