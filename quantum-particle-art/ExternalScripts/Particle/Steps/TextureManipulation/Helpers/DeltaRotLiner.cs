using System.Collections.Generic;
using DefaultNamespace.Tools;
using Godot;

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