using System.Collections.Generic;
using System.Linq;
using Godot;
using UnityEngine;
using Color = Godot.Color;

[GlobalClass]
public partial class Union : AGate
{
    private Particle _thrash;

    public override bool Precondition(HashSet<Particle> setInside)
    {
        if (setInside.Count > 0)
            Debug.Log("Union Precondition with " + setInside.Count + " particles");
        if (!base.Precondition(setInside))
            return false;
        else if (setInside.Count >= 2) //If we already have more than 2, we don't accept more
            return false;
        else if (setInside.Count < 1) //If we have 0 we accept it
            return true;
        else if (setInside.Count == 1) //If we have 1, we'll accept it and use the two we have to merge them
        {
            _thrash = setInside.First();
            return true;
        }

        throw new System.Exception("Unreachable code in Union Precondition");
    }

    public override bool Resolve(Particle particle)
    {
        if (_thrash == null || particle == _thrash)
            return false; //We do it only on one of the two
        //Average velocity into first one, kill second
        //Average velocity
        particle.Orientation.AddForce(-particle.Orientation.Velocity / 2f + _thrash.Orientation.Velocity / 2f);
        _thrash.MarkDead();
        return true;
    }

    public override AGate Copy()
    {
        return new Union(); //We don't need to copy "live" references
    }

    public override Color Color => Colors.SpringGreen;

    public override string ShortName => "U";
}