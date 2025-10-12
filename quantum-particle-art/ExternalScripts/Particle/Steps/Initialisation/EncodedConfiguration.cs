using System.Linq;
using Godot;
using Godot.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.ExternalScripts.Particle.Genetics;
using Mathf = Godot.Mathf;

[GlobalClass]
public partial class EncodedConfiguration : GridConfiguration
{
    [Export] private int _encoded;

    public void UpdateEncoded(int encoded)
    {
        //Debug.Log("Updating encoded to " + encoded + (_last==null?" no last":" has last") + (_grid==null?" no grid":" has grid"));
        _encoded = encoded;
    }

    public int Result()
    {
        //Debug.Log("Asking for result with last grid " + (_grid != null));
        var copies = _grid.Copies(_combined);
        //Assert.IsTrue(copies.Length == NbParticles,$"Count gate copies {copies.Length} does not match number of particles {NbParticles}");
        var acc = copies.SelectMany(c => c.Gates.Select(g=> (g as Count)?.Value)).Where(i=>i.HasValue).Select(i=>i.Value).DecodeBits(2);
        return acc;
    }

    protected override bool[] Bits()
    {
        return BitHelpers.Bits(_encoded, NbParticles);
    }
}