using UnityEngine;

namespace DefaultNamespace.Particle.Steps
{
    public class MultipleImagesLooper : PipelineLooper<WorldInitializer, ParticleWorld, ParticleSimulation>
    {
        [SerializeField] private InitConditions[] _textures;
        public InitConditions[] Textures => _textures;
        protected override int Loops => _textures.Length;

        protected override void UpdateInitializer(WorldInitializer init, int loop)
        {
            init.Init = _textures[loop];
        }

        protected override void OnFinished(ParticleSimulation pipeline)
        {
            
        }
    }
}