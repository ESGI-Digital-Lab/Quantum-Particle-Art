using System;
using System.Collections.Generic;
using GeneticSharp;
using UnityEngine.Assertions;

namespace UnityEngine.ExternalScripts.Particle.Genetics;

public static class GatesTypesToInt
{
    public static Type NullType => typeof(EmptyGate);
    public static Type BaseType => typeof(AGate);
    private static Dictionary<Type, byte> _typesMap;
    private static Dictionary<byte, Type> _mapBack;

    public static byte Count
    {
        get
        {
            if (_typesMap == null) CreateMaps();
            return (byte)_typesMap.Count;
        }
    }

    public static byte Id(Type type)
    {
        if (_typesMap == null) CreateMaps();
        return _typesMap[type];
    }

    public static Type Type(byte id)
    {
        if (_mapBack == null) CreateMaps();
        return _mapBack[id];
    }

    private static void CreateMaps()
    {
        //Usinsg abstract type as "default", null gate
       
        OverrideReflection(BaseType.Assembly.GetTypes());
    }

    public static void OverrideReflection(IEnumerable<Type> types)
    {
        _typesMap = new Dictionary<Type, byte>();
        _mapBack = new Dictionary<byte, Type>();
        byte id = 0;
        foreach (var type in types)
        {
            if (!type.IsAbstract && BaseType.IsAssignableFrom(type))
            {
                //In case we also find null class, we only want it once
                _typesMap[type] = id;
                _mapBack[id] = type;


                Assert.IsFalse(id >= byte.MaxValue, $"Too many types {id} for byte encoding");
                id += 1;
            }
        }
    }
}