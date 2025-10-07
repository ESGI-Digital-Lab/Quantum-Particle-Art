using System.Linq;
using DefaultNamespace.Tools;
using UnityEngine;

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