using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DefaultNamespace.Tools;
using Godot;
using UnityEngine;
using UnityEngine.Assertions;

public class PointsIntersection : ParticleStep
{
    [SerializeField] private bool _gatesShouldDraw = false;
    private (Area2D a, Particle p) _lastControl = (default, null);
    private (Area2D a, Particle p) _lastTeleport = (default, null);
    private LineCollection _lineCollection;
    private bool _forceDifferentSpecy = false;

    public PointsIntersection(LineCollection lineCollection, bool gatesShouldDraw, bool forceDifferentSpecy)
    {
        _gatesShouldDraw = gatesShouldDraw;
        _lineCollection = lineCollection;
        this._forceDifferentSpecy = forceDifferentSpecy;
    }

    public override async Task HandleParticles(ParticleWorld entry, float delay)
    {
        foreach (var point in entry.PointsOfInterest)
        {
            for (var index = 0; index < entry.Count; index++)
            {
                var particleRoot = entry[index];
                foreach (var particleD in particleRoot.Pivots(false, true))
                {
                    var particle = particleD.p;
                    point.Handle(particle);
                }
            }

            if (delay > 0)
                await Task.Delay((int)(delay * 1000));
        }
    }
}