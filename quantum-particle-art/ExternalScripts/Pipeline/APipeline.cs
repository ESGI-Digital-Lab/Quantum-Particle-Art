using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Assertions;

//Shortcut when we don't need explicitley the type of the steps in the pipeline logic
public abstract class APipeline<TInit, T> : APipeline<TInit, T, IStep<T>> where TInit : class
{
}

public abstract class APipeline<TInit, T, TStep> where TInit : class where TStep : IStep<T>
{
    protected TInit _info;
    protected TStep[] _steps;

    [Header("Log")] [SerializeField] protected readonly bool _logInit = true;


    [SerializeField, Range(-1, 10f)] private float _initDelay;
    [SerializeField, Range(-1, 10f)] protected float _delay;
    private TStep _last;

    public async Task Restart(TInit init, IEnumerable<TStep> steps, IEnumerable<IInit<TInit>> inits,
        IEnumerable<IInit<T>> prewarms)
    {
        _info = init;
        _steps = steps.ToArray();
        var _inits = inits.ToArray();
        await Init(init);
        foreach (var step in _inits)
        {
            await step.Init(init);
            if (_logInit)
                Debug.LogWarning("Inited successfully with global initializer " + step.GetType().Name);
        }

        var _prewarms = prewarms.ToArray();
        if (_prewarms != null && _prewarms.Length > 0)
        {
            var firstInput = GetInput(_steps[0]);
            foreach (var step in _prewarms)
            {
                await step.Init(firstInput);
                if (_logInit)
                    Debug.LogWarning("Prewarmed only once, with first input " + step.GetType().Name);
            }
        }

        Assert.IsNotNull(init, "Init values not found");
        //if (_logInit)
        //    Debug.LogWarning("Init values : " +
        //                     init.GetType().Name + "and steps : " + string.Join("\n",
        //                         _steps.Select(st => st.GetType().Name + " on " + (st as Component).gameObject.name)));
        if (_logInit)
            Debug.LogWarning("Pipeline completely initialized and prewar:ed successfully with " + _steps.Length + " steps.");
        await Sync();
        //await StepEnumerator();
    }

    private T last = default;
    public void Tick()
    {
        StepOnce();
    }

    protected virtual async Task StepOnce()
    {
        foreach (var step in _steps)
        {
            await Sync();
            var input = GetInput(step);
            await step.Step(input, _delay);
            await Sync();
            last = GetLast(step);
            await Sync();
            await Stepped(step, last);
        }

        await Sync();
    }

    public void Dispose()
    {
        if (_steps != null)
            foreach (var step in _steps)
                step.Release();
        Disposed();
    }

    protected abstract Task Init(TInit init);
    protected abstract T GetInput(TStep step);
    protected abstract Task Sync(float delay);
    protected abstract Task Stepped(TStep step, T result);
    protected abstract T GetLast(TStep step);
    protected abstract void Disposed();

    protected async Task Sync() => await Sync(_delay);
}