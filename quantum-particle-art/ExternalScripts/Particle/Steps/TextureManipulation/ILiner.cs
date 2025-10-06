using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace.Tools;
using Godot;
using UnityEngine;
using Mathf = Godot.Mathf;
using Vector2 = Godot.Vector2;

namespace DefaultNamespace.Particle.Steps.TextureManipulation;

public interface ILiner
{
    public LineCollection.Line CreateLine(GlobalTick.MovementData data);

    public static LineCollection.Line Line(GlobalTick.MovementData data, float width)
    {
        var line = new LineCollection.Line(data.fromNormalized, data.toNormalize,
            data.color, width);
        return line;
    }
}

public class DeltaRotLiner : ILiner
{
    [Export] private float _deltaForMin = 90f;
    private int _cacheSize = 10;
    private readonly Dictionary<Orientation, Queue<float>> _anglesCache;

    public DeltaRotLiner(int cacheSize = 5)
    {
        _cacheSize = cacheSize;
        _anglesCache = new();
        _deltaForMin = Mathf.DegToRad(_deltaForMin);
    }

    public LineCollection.Line CreateLine(GlobalTick.MovementData data)
    {
        float width = 1f;
        Queue<float> q;
        var currentRot = data.orientation.Radians;
        if (_anglesCache.TryGetValue(data.orientation, out var vel)) //Just on very first frame
        {
            var delta = 0f;
            foreach(var a in vel)
                delta += Mathf.Abs(Mathf.AngleDifference(a, currentRot));
            delta /= vel.Count;
            //Maxwidth on no difference, 0 width on _deltaForMin difference or more
            width = Mathf.Clamp(Mathf.InverseLerp(_deltaForMin, 0f, delta), 0f, 1f);
            q = _anglesCache[data.orientation];
            if(q.Count>=_cacheSize)
                q.Dequeue();
        }
        else
        {
            q = new Queue<float>();
            _anglesCache[data.orientation] = q;
        }

        q.Enqueue(currentRot);
        return ILiner.Line(data, width);
    }
}

public class ToggleLiner : ILiner
{
    private float _sineAmplitude = -1f;
    private bool _speedOverSine => _sineAmplitude < 0;
    private bool _dynamicMax;

    public ToggleLiner(bool dynamicMax)
    {
        _dynamicMax = dynamicMax;
        if (_dynamicMax)
            Orientation.MaxSpeed = .1f;
    }

    public ToggleLiner(float sineAmplitude)
    {
        _sineAmplitude = sineAmplitude;
    }

    public LineCollection.Line CreateLine(GlobalTick.MovementData data)
    {
        var width = 0f;
        if (_speedOverSine)
        {
            float speed = data.orientation.Speed;
            if (_dynamicMax && speed > Orientation.MaxSpeed)
                Orientation.MaxSpeed = speed;
            width = data.orientation.NormalizedSpeed;
        }
        else
        {
            width = (MathF.Sin(Godot.Time.GetTicksMsec() * 1000f * _sineAmplitude) + 1f) / 2f;
        }

        var line = ILiner.Line(data, width);
        return line;
    }
}