using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public abstract class PipelineLooper<TInit, T, TPipe> : MonoBehaviour
    where TInit : class where TPipe : APipeline<TInit, T>
{
    [SerializeField, Range(-1, 1000),
     Tooltip(
         "Negative duration means it will run indefinitely, it can be changed to positive duration while it's running to make it last again N seconds")]
    private float _duration;

    [SerializeField] private TInit _baseInitializer;
    [SerializeField] protected abstract int Loops { get; }
    private TPipe pipeline;

    public TInit BaseInitializer
    {
        get { return _baseInitializer; }
        set { _baseInitializer = value; }
    }

    public override async Task Start()
    {
        pipeline = GetPipeline();
        _lastStart = -1;
        i = -1;
    }

    private float _lastStart;
    private int i = 0;
    public override void Dispose()
    {
        base.Dispose();
        pipeline?.Dispose();
    }

    public override async Task Update()
    {
        await base.Update();
        if (_lastStart < 0 || (_duration > 0 && i < Loops && Time.time - _lastStart < _lastStart + _duration))
        {
            if (i >= 0) //Skipped on first
                OnFinished(pipeline);
            pipeline.Dispose();
            i++;
            _lastStart = Time.time;
            UpdateInitializer(_baseInitializer, i);
            //Not awaited on purpose, this update juste starts the loop whenever the duration is passed
            await pipeline.Restart(_baseInitializer, GetSteps(), GetInits(), GetPrewarms());
        }

        pipeline.Tick();
    }

    protected abstract IEnumerable<IInit<T>> GetPrewarms();

    protected abstract IEnumerable<IInit<TInit>> GetInits();

    protected abstract IEnumerable<IStep<T>> GetSteps();

    protected abstract TPipe GetPipeline();
    //{
    //    var found = GetComponentsInChildren<TPipe>();
    //    if (found == null || found.Length == 0)
    //        found = GetComponentsInChildren<TPipe>(false);
    //    if (found == null)
    //        Debug.LogError("Pipeline not found in ");
    //    return found[0];
    //}

    protected abstract void UpdateInitializer(TInit init, int loop);
    protected abstract void OnFinished(TPipe pipeline);
}