using System.Collections.Generic;
using Godot;

[GlobalClass]
public abstract partial class ChromosomeConfigurationBase : Resource
{
    private int? _max = null;
    public abstract IEnumerable<GateConfiguration> GatesConfig { get; }

    public int RandomInput
    {
        get
        {
            //We are in a ressource, created and used in different runs, where each non exported variable is flushed on game stop
            _max ??= (int)Mathf.Pow((float)2, (float)Size.Y) - 1;
            return UnityEngine.Random.Range(1, _max.Value + 1);
        }
    }

    public abstract Vector2I Size { get; }

    public virtual string Name => this.FileName();

    public virtual bool MoveNext() => true;
}