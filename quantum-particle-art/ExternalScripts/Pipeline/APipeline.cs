using System.Collections;
using System.Linq;
using Godot;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Assertions;

//Shortcut when we don't need explicitley the type of the steps in the pipeline logic
public abstract class APipeline<TInit, T> : APipeline<TInit, T, IStep<T>> where TInit : class
{
}
public abstract class APipeline<TInit, T, TStep> : MonoBehaviour where TInit : class where TStep : IStep<T>
{
    protected TInit _info;
    protected TStep[] _steps;
    private bool _manualStep = false;
    private bool _manualCycle = false;
    [SerializeField]
    private bool _autoRun = false;

    [Header("Log")]
    [SerializeField] private bool _finalUpdateOnly = true;
    [SerializeField] protected bool _logInit = true;
    [SerializeField] protected bool _logSteps = true;


    [SerializeField, Range(-1, 10f)] private float _initDelay;
    [SerializeField, Range(-1, 10f)] protected float _delay;
    private TStep _last;

    public IEnumerator Restart(TInit init,Node node)
    {
        Dispose();
        _info = init;
        _steps = GetComponentsInChildren<TStep>(false);
        var _inits = GetComponentsInChildren<IInit<TInit>>(false);
        yield return Init(init);
        foreach (var step in _inits)
        {
            if (_logInit)
                Debug.LogWarning("Initing with global initializer" + step.GetType().Name);
            yield return step.Init(init);
        }
        var _prewarms = GetComponentsInChildren<IInit<T>>(false);
        var firstInput = GetInput(_steps[0]);
        foreach (var step in _prewarms)
        {
            if (_logInit)
                Debug.LogWarning("Prewarming only once, with first input " + step.GetType().Name);
            yield return step.Init(firstInput);
        }

        Assert.IsNotNull(init, "Init values not found");
        //if (_logInit)
        //    Debug.LogWarning("Init values : " +
        //                     init.GetType().Name + "and steps : " + string.Join("\n",
        //                         _steps.Select(st => st.GetType().Name + " on " + (st as Component).gameObject.name)));
        yield return Sync();
        yield return StepEnumerator();
    }
    private IEnumerator StepEnumerator()
    {
        T last = default;
        while (_steps != null && _steps.Length > 0)
        {
            foreach (var step in _steps)
            {
                yield return Sync();
                if (!_autoRun)
                {
                    yield return new WaitUntil(() => _manualStep || _manualCycle);
                    _manualStep = false; // Reset manual step flag after stepping
                }

                var input = GetInput(step);
                yield return step.Step(input, _delay);
                yield return Sync(_delay);
                last = GetLast(step);
                yield return Sync();
                yield return Stepped(step, last);
            }

            _manualCycle = false; // Reset manual cycle flag


            yield return Sync();
        }
    }
    public override void Dispose()
    {
        if (_steps != null)
            foreach (var step in _steps)
                step.Release();
        Disposed();
    }
    protected abstract IEnumerator Init(TInit init);
    protected abstract T GetInput(TStep step);
    protected abstract IEnumerator Sync(float delay);
    protected abstract IEnumerator Stepped(TStep step, T result);
    protected abstract T GetLast(TStep step);
    protected abstract void Disposed();

    protected IEnumerator Sync() => Sync(_delay);
    
    [Button]
    public void StepOnce()
    {
        _manualStep = true;
    }

    [Button]
    public void Cycle()
    {
        _manualCycle = true;
    }
    
    private void OnDisable()
    {
        Dispose();
    }

    private void OnDestroy()
    {
        Dispose();
    }

    private void OnApplicationQuit()
    {
        Dispose();
    }

}