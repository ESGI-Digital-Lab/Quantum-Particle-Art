using System;
using DefaultNamespace.Tools;
using UnityEngine;

namespace DefaultNamespace.Particle.Steps.TextureManipulation;

public interface ILiner
{
    public LineCollection.Line CreateLine(GlobalTick.MovementData data);
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

        var line = new LineCollection.Line(data.fromNormalized, data.toNormalize,
            data.color, width);
        return line;
    }
}