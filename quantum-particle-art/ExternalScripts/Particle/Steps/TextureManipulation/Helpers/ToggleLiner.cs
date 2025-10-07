using System;
using DefaultNamespace.Tools;
using Godot;

public class ToggleLiner : ILiner, IWidther
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
        var width = (this as IWidther).DetermineWidth(data.orientation.Owner);

        var line = ILiner.Line(data, width);
        return line;
    }

    public float DetermineWidth(Vector2 data)
    {
        var width = 0f;
        if (_speedOverSine)
        {
            float speed = data.Length();
            if (_dynamicMax && speed > Orientation.MaxSpeed)
                Orientation.MaxSpeed = speed;
            width = speed / Orientation.MaxSpeed;
        }
        else
        {
            width = (MathF.Sin(Godot.Time.GetTicksMsec() * 1000f * _sineAmplitude) + 1f) / 2f;
        }

        return width;
    }
}