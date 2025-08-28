using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public class PointsIntersection : ParticleStep
{
    [SerializeField] private bool _gatesShouldDraw = false;
    private Dictionary<Area2D, HashSet<Particle>> _map = new();
    private (Area2D a, Particle p) _lastEntangle = (default, null);
    private (Area2D a, Particle p) _lastTeleport = (default, null);

    public PointsIntersection(bool gatesShouldDraw)
    {
        _gatesShouldDraw = gatesShouldDraw;
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
                var particle = entry[index];
                if (point.Contains(particle))
                {
                    if (!set.Contains(particle)) //Enters
                    {
                        set.Add(particle);
                        switch (point.Type)
                        {
                            case Area2D.AreaType.None:
                                break;
                            case Area2D.AreaType.Superpose:
                                particle.Superpose();
                                break;
                            case Area2D.AreaType.Entangle:
                                if (_lastEntangle.p == null)
                                    _lastEntangle = (point, particle);
                                else if (_lastEntangle.p != particle && !_lastEntangle.a.Equals(point))
                                {
                                    _lastEntangle.p.Orientation.Entangle(particle.Orientation);
                                    _lastEntangle = (default, null);
                                }

                                break;
                            case Area2D.AreaType.Teleport:
                                var toBeTeleported = _lastTeleport.p;
                                if (toBeTeleported == null)
                                    _lastTeleport = (point, particle);
                                else if (toBeTeleported != particle && !_lastTeleport.a.Equals(point))
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
                                    if(_gatesShouldDraw)
                                        _world.Drawer.AddLine(b, toBeTeleported.NormalizedPosition, ViewHelpers.TEL);
                                    _lastTeleport = (default, null);
                                }

                                break;
                            case Area2D.AreaType.Measure:
                                var before = particle.NormalizedPosition;
                                particle.Collapse();
                                if (_gatesShouldDraw)
                                    _world.Drawer.AddLine(before, particle.NormalizedPosition, ViewHelpers.MEA);
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

            if (delay > 0)
                await Task.Delay((int)(delay*1000));
        }
    }
}