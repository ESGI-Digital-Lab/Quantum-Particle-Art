using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using UnityEngine;

    public class MultipleImagesLooper : PipelineLooper<WorldInitializer, ParticleWorld, ParticleSimulation>
    {
        [SerializeField] private InitConditions[] _textures;

        private IInit<WorldInitializer>[] _inits;
        private IStep<ParticleWorld>[] _steps;
        private IInit<ParticleWorld>[] _prewarm;
        private int _texHeight;

        public MultipleImagesLooper(float duration, InitConditions[] textures,
            IEnumerable<IInit<WorldInitializer>> inits,
            IEnumerable<IStep<ParticleWorld>> step, IEnumerable<IInit<ParticleWorld>> prewarm, int texHeight)
        {
            _duration = duration;
            _textures = textures;
            _inits = inits.ToArray();
            _steps = step.ToArray();
            _prewarm = prewarm.ToArray();
            _texHeight = texHeight;
        }
        
        protected override async Task<bool> UpdateInitializer(WorldInitializer init, int loop)
        {
            init.Init = _textures[loop];
            if (!init.Init.Texture.Create())
                return false;
            init.Init.Texture.Texture.Resize((int)(_texHeight * init.Init.Ratio), _texHeight,
                Image.Interpolation.Trilinear);
            return true;
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
