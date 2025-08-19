using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class SpeciesInfluence : ParticleStep
{
    public override IEnumerator HandleParticles(ParticleWorld entry, float delay)
    {
        //Debug.Log("Species count : " + entry.Ruleset.NbSpecies);
        for (var index = 0; index < entry.Count; index++)
        {
            var p = entry[index];
            UpdateParticlesInteraction(p, entry);
            p.AdvanceSteps(entry.Ruleset[p.Species].Steps);
            if (delay > 0)
                yield return new WaitForSeconds(delay);
        }
    }

    private void UpdateParticlesInteraction(Particle p, ParticleWorld world)
    {
        Vector2 acc = Vector2.zero;
        int n = 0;
        Ruleset.Species currentSpecies = world.Ruleset[p.Species];
        for (var index = 0; index < world.Count; index++)
        {
            var other = world[index];
            if (p == other) continue;
            var dist = world.WrappedDistance(p, other, out Vector2 normalizedDirection);
            var info = currentSpecies[other.Species];
            var socialRadius = info.SocialRadius;
            if (dist > 0 && dist < socialRadius)
            {
                float collisionRadius = info.CollisionRadius;
                if (dist < collisionRadius)
                {
                    var collisionForce = (1 - dist / collisionRadius) * info.CollisionForce;
                    p.AddForce(-normalizedDirection * collisionForce);
                }

                float socialForce = info.SocialForce;
                if (info.Ramp)
                    socialForce *= 2 * (1 - dist / socialRadius);

                acc += normalizedDirection * socialForce;
                n++;
            }
        }

        int steps = currentSpecies.Steps;
        if (n > 0)
        {
            if (currentSpecies.AverageForces)
                acc /= n;
            else
                acc *= currentSpecies.NonAverageForce;
            p.SetForce(steps, acc);
        }
        else
        {
            p.SetForce(steps, Vector2.zero);
        }
    }
}