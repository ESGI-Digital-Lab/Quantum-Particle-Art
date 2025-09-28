using System.Collections.Generic;
using Godot;

[GlobalClass]
public abstract partial class AGate : Godot.Resource
{
    public virtual bool Precondition(HashSet<Particle> setInside) => true;
    public abstract bool Resolve(Particle particle);
    public abstract AGate Copy();
    public abstract Color Color { get; }
    public abstract string ShortName { get; }
    public virtual string Label => null;
    public bool DynamicName => !string.IsNullOrEmpty(Label);
}
public abstract partial class DualInputAGate<SharedTypeID> : AGate where SharedTypeID : DualInputAGate<SharedTypeID>
{
    private static (SharedTypeID a, Particle p) _lastControl = (null, null);
    [Export] private bool _forceDifferentSpecy = false;

    public override bool Resolve(Particle particle)
    {
        var first = _lastControl.p;
        if (first == null)
            _lastControl = (ID, particle);
        else if (first != particle && !_lastControl.a.Equals(this) &&
                 SpecyCondition(first, particle))
        {
            Resolve(particle, first);
            _lastControl = (default, null);
            return true;
        }

        return false;
    }

    protected abstract void Resolve(Particle particle, Particle first);

    public abstract SharedTypeID
        ID { get; } //This on any inerhiting class with itself as ID, would share the same static _lastControl

    protected bool SpecyCondition(Particle toBeTeleported, Particle particle)
    {
        return !_forceDifferentSpecy || toBeTeleported.Species != particle.Species;
    }
    protected abstract DualInputAGate<SharedTypeID> Copy(SharedTypeID source);
    public override AGate Copy()
    {
        var temp = this.Copy(ID);
        temp._forceDifferentSpecy = ID._forceDifferentSpecy;
        return temp;
    }
}