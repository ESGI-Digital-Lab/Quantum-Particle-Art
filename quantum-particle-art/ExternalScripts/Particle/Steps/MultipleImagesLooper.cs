using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace DefaultNamespace.Particle.Steps
{
    public class MultipleImagesLooper : PipelineLooper<WorldInitializer, ParticleWorld, ParticleSimulation>
    {
        [SerializeField] private InitConditions[] _textures;
        private event System.Action<InitConditions> _initChange;
        public event System.Action<InitConditions> InitChange
        {
            add { _initChange += value; }
            remove { _initChange -= value; }
        }
        private IInit<WorldInitializer>[] _inits;
        private IStep<ParticleWorld>[] _steps;
        private IInit<ParticleWorld>[] _prewarm;

        public MultipleImagesLooper(float duration,InitConditions[] textures, IEnumerable<IInit<WorldInitializer>> inits,
            IEnumerable<IStep<ParticleWorld>> step, IEnumerable<IInit<ParticleWorld>> prewarm)
        {
            _duration = duration;
            _textures = textures;
            _inits = inits.ToArray();
            _steps = step.ToArray();
            _prewarm = prewarm.ToArray();
        }

        public InitConditions[] Textures => _textures;
        protected override int Loops => _textures.Length;
        protected override async Task UpdateInitializer(WorldInitializer init, int loop)
        {
            init.Init = _textures[loop];
            await init.Init.Texture.Create();
            _initChange?.Invoke(init.Init);
        }

        protected override void OnFinished(ParticleSimulation pipeline)
        {
        }

        protected override IEnumerable<IInit<ParticleWorld>> GetPrewarms() => _prewarm;

        protected override IEnumerable<IInit<WorldInitializer>> GetInits() => _inits;


        protected override IEnumerable<IStep<ParticleWorld>> GetSteps() => _steps;


        protected override ParticleSimulation GetPipeline()
        {
            return new ParticleSimulation();
        }
    }
}