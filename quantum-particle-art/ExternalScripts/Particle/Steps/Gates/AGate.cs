using System.Collections.Generic;
using System.Linq;
using Godot;
using UnityEngine.Assertions;

[GlobalClass]
public abstract partial class AGate : Godot.Resource
{
    public static bool ShowLabelDefault = true;
    protected virtual bool ShowLabelAllowed => ShowLabelDefault;
    public virtual bool Precondition(HashSet<Particle> setInside) => true;
    public abstract bool Resolve(Particle particle);
    public abstract Color Color { get; }
    public abstract string ShortName { get; }

    public virtual T DeepCopy<T>() where T : AGate
    {
        return this.Duplicate(true) as T;
    }

    public AGate DeepCopy() => this.DeepCopy<AGate>();
    public virtual string Label => null;
    public bool DynamicName => ShowLabelAllowed && !string.IsNullOrEmpty(Label);
}

public abstract partial class DualInputAGate<SharedTypeID> : AGate where SharedTypeID : DualInputAGate<SharedTypeID>
{
    [Export] private bool _forceDifferentSpecy = false;
    private (SharedTypeID a, Particle p) _lastInput = (null, null);
    public override T DeepCopy<T>()
    {
        var cop = base.DeepCopy<T>();
        var cast = cop as DualInputAGate<SharedTypeID>;
        Assert.IsTrue(cast._lastInput==(null,null), () =>
        {
            return "Deep copy resulted in a non null last input this might cause really weird behaviors";
        });
        return cop;
    }


    public override bool Resolve(Particle particle)
    {
        var first = _lastInput.p;
        if (first == null)
            _lastInput = (ID, particle);
        else if (first != particle && SpecyCondition(first, particle))
        {
            Resolve(particle, first);
            _lastInput = (null, null);
            return true;
        }

        return false;
    }

    protected abstract void Resolve(Particle particle, Particle first);

    public abstract SharedTypeID
        ID { get; } //This on any inerhiting class with itself as ID, would share the same static _lastControl

    protected bool SpecyCondition(Particle initialControl, Particle particle)
    {
        return !_forceDifferentSpecy || initialControl.Species != particle.Species;
    }
    
}