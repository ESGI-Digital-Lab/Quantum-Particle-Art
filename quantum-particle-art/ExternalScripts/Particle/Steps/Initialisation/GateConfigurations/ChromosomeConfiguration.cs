using System.Collections.Generic;
using GeneticSharp;
using Godot;
using UnityEngine.ExternalScripts.Particle.Genetics;

[GlobalClass]
public partial class ChromosomeConfiguration : ChromosomeConfigurationBase
{
    [Export] private Godot.Collections.Array<GateConfiguration> _gates;
    [Export] protected Vector2I _size;

    public override IEnumerable<GateConfiguration> GatesConfig => _gates;

    public override Vector2I Size => _size;

    public ChromosomeConfiguration() : this([])
    {
    }

    public ChromosomeConfiguration(Vector2I size) : this([], size)
    {
    }

    public ChromosomeConfiguration(IEnumerable<GateConfiguration> gateChromosome) : this(gateChromosome, new Vector2I(10, 10))
    {
    }

    public ChromosomeConfiguration(GateChromosome gateChromosome, Vector2I size) : this(
        BitHelpers.GetGates(gateChromosome, size), size)
    {
    }
    public ChromosomeConfiguration(IEnumerable<GateConfiguration> gateChromosome, Vector2I size)
    {
        _gates = new(gateChromosome);
        _size = size;
    }
}