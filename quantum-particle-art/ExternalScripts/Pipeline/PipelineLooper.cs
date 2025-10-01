using System;
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
    protected float _duration;

    [SerializeField] private TInit _baseInitializer;
    private TPipe pipeline;

    public TInit BaseInitializer
    {
        get { return _baseInitializer; }
        set { _baseInitializer = value; }
    }

    protected bool _shouldRestart;

    Func<Task> timer;

    public override async Task Start()
    {
        pipeline = GetPipeline();
        i = -1;
        timer = async () =>
        {
            if (_duration > 0)
                await Task.Delay((int)(_duration * 1000));
            _shouldRestart = true;
        };
        _shouldRestart = true;
    }

    private int i = 0;
    private bool _ready = false;

    public override void Dispose()
    {
        base.Dispose();
        pipeline?.Dispose();
    }

    public override async Task Update()
    {
        await base.Update();
        if (_shouldRestart)
        {
            _shouldRestart = false;
            _ready = false;
            if (i >= 0)
                OnFinished(pipeline);
            pipeline.Dispose();
            i++;
            bool intializedCorrectly = await UpdateInitializer(_baseInitializer, i);
            if (!intializedCorrectly)
                return;
            await pipeline.Restart(_baseInitializer, GetSteps(), GetInits(), GetPrewarms());
            _ready = true;
            //Not awaited so non blocking, just launching the timer after initialization finished so we'll reenter this after duration
            Task.Run(timer);
        }

        if (_ready)
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

    protected abstract Task<bool> UpdateInitializer(TInit init, int loop);
    protected abstract void OnFinished(TPipe pipeline);
}