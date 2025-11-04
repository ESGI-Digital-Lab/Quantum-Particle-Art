using System.Threading.Tasks;
using UnityEngine.Assertions;

namespace UnityEngine.ExternalScripts.Particle.Steps;

public class SendImage : ParticleStep
{
    private Saver _saver;
    private bool _sent = false;

    public SendImage(Saver saver)
    {
        _saver = saver;
    }

    public override async Task Init(WorldInitializer initializer)
    {
        await base.Init(initializer);
        _sent = false;
    }

    public override Task HandleParticles(ParticleWorld entry, float delay)
    {
        return Task.CompletedTask;
    }

    public override void Release()
    {
        base.Release();
        if (!_sent)
        {
            Assert.IsTrue(_saver.Saved != null && _saver.Saved.Exists, "_saver.Saved!=null && _saver.Saved.Exists");
            Debug.Log($"Sending image {_saver.Saved.Name} in {_saver.Saved.Directory}");
            _sent = true;
        }
    }
}