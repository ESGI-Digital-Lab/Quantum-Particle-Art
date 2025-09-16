using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DefaultNamespace.Tools;
using UnityEngine;
using UnityEngine.Assertions;

public class PointsIntersection : ParticleStep
{
    [SerializeField] private bool _gatesShouldDraw = false;
    private Dictionary<Area2D, HashSet<Particle>> _map = new();
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
            if (!_map.ContainsKey(point))
                _map.Add(point, new HashSet<Particle>());
            var set = _map[point];
            for (var index = 0; index < entry.Count; index++)
            {
                var particleRoot = entry[index];
                foreach (var particleD in particleRoot.Pivots(false, true))
                {
                    var particle = particleD.p;
                    if (point.Contains(particle))
                    {
                        //Superpose gates cannot be retriggered if already occupied, it need for the initially activating particle to exit first to avoind instantaneous multiple superpositions
                        if (!set.Contains(particle) &&
                            (point.Type != Area2D.AreaType.Superpose || set.Count == 0)) //Enters
                        {
                            set.Add(particle);
                            switch (point.Type)
                            {
                                case Area2D.AreaType.None:
                                    break;
                                case Area2D.AreaType.Superpose:
                                    particle.Superpose();

                                    break;
                                case Area2D.AreaType.Control:
                                    var toBecontrolled = _lastControl.p;
                                    if (toBecontrolled == null)
                                        _lastControl = (point, particle);
                                    else if (toBecontrolled != particle && !_lastControl.a.Equals(point) &&
                                             SpecyCondition(toBecontrolled, particle))
                                    {
                                        toBecontrolled.Orientation.Control(particle.Orientation);
                                        _lastControl = (default, null);
                                    }

                                    break;
                                case Area2D.AreaType.Teleport:
                                    var toBeTeleported = _lastTeleport.p;
                                    if (toBeTeleported == null)
                                        _lastTeleport = (point, particle);
                                    else if (toBeTeleported != particle && !_lastTeleport.a.Equals(point) &&
                                             SpecyCondition(toBeTeleported, particle))
                                    {
                                        if (particle.Orientation.Teleportation !=
                                            null) //Teleportation is about to be overriden
                                        {
                                            //_world.Drawer.AddLine(
                                            //    particle.Position,
                                            //    particle.Orientation.Teleportation.Position,
                                            //    ViewGelpers.TELE);
                                        }

                                        var b = toBeTeleported.NormalizedPosition;
                                        toBeTeleported.Orientation.Teleport(particle.Orientation);
                                        if (_gatesShouldDraw)
                                            _lineCollection.AddLine(b, toBeTeleported.NormalizedPosition,
                                                ViewHelpers.TEL);
                                        _lastTeleport = (default, null);
                                    }

                                    break;
                                case Area2D.AreaType.Measure:
                                    var before = particle.NormalizedPosition;
                                    particle.Collapse();
                                    if (_gatesShouldDraw)
                                        _lineCollection.AddLine(before, particle.NormalizedPosition, ViewHelpers.MEA);
                                    break;
                                default:
                                    Assert.IsTrue(false, $"Unhandled point type: {point.Type}");
                                    break;
                            }
                        }
                        else
                        {
                            //Stays
                        }
                    }
                    else
                    {
                        if (set.Contains(particle)) //Exits
                            set.Remove(particle);
                    }
                }
            }

            if (delay > 0)
                await Task.Delay((int)(delay * 1000));
        }
    }

    private bool SpecyCondition(Particle toBeTeleported, Particle particle)
    {
        return !_forceDifferentSpecy || toBeTeleported.Species != particle.Species;
    }
}