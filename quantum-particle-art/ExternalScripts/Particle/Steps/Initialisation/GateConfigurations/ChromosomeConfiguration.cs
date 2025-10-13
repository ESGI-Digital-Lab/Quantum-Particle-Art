using System.Collections.Generic;
using GeneticSharp;
using Godot;
using UnityEngine.ExternalScripts.Particle.Genetics;

[GlobalClass]
public partial class ChromosomeConfiguration : Resource
{
    [Export] private Godot.Collections.Array<GateConfiguration> _gates;
    [Export] protected Vector2I _size;

    private int? _max = null;
    public virtual IEnumerable<GateConfiguration> GatesConfig => _gates;

    public int RandomInput
    {
        get
        {
            //We are in a ressource, created and used in different runs, where each non exported variable is flushed on game stop
            _max ??= (int)Mathf.Pow(2, _size.Y) - 1;
            return UnityEngine.Random.Range(1, _max.Value + 1);
        }
    }

    public Vector2I Size => _size;

    public ChromosomeConfiguration() : this([])
    {
    }

    public ChromosomeConfiguration(Vector2I size) : this([], size)
    {
    }

    public ChromosomeConfiguration(IEnumerable<GateConfiguration> gateChromosome) : this(gateChromosome, new Vector2I(10, 10))
    {
    }

    public ChromosomeConfiguration(IEnumerable<GateConfiguration> gateChromosome, Vector2I size)
    {
        _gates = new(gateChromosome);
        _size = size;
    }

    public ChromosomeConfiguration(GateChromosome gateChromosome, Vector2I size) : this(
        BitHelpers.GetGates(gateChromosome, size), size)
    {
    }
}