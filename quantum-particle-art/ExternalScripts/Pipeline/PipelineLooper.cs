using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public abstract class PipelineLooper<TInit, T, TPipe> : MonoBehaviour
    where TInit : class where TPipe : APipeline<TInit, T>
{
    [SerializeField,Range(-1,1000),Tooltip("Negative duration means it will run indefinitely, it can be changed to positive duration while it's running to make it last again N seconds")] private float _duration;
    [SerializeField] private TInit _baseInitializer;
    [SerializeField] protected abstract int Loops { get; }
    private TPipe pipeline;

    private void Awake()
    {
        pipeline = GetPipeline();
    }

    public override IEnumerator<AsyncEnumerator> Start()
    {
        CancellationTokenSource tokens = null;
        var l = Loops;
        for (int i = 0; i < l; i++)
        {
            UpdateInitializer(_baseInitializer, i);
            //We let this enumerator run async
            if (tokens != null)
                tokens.Cancel();
            tokens = new CancellationTokenSource();
            Task.Run(() =>
            {
                pipeline.Restart(_baseInitializer);
            });
            StartCoroutine();
            if (_duration > 0)
                yield return new WaitForSeconds(_duration);
            else
            {
                yield return new WaitUntil(() => _duration > 0);
                yield return new WaitForSeconds(_duration);
            }
            OnFinished(pipeline);
        }

        StopCoroutine(running);
        yield return new WaitForEndOfFrame();
        pipeline.Dispose();
    }

    protected virtual TPipe GetPipeline()
    {
        var pipeline = GetComponent<TPipe>();
        if (pipeline == null)
            pipeline = GetComponentInChildren<TPipe>(false);
        if (pipeline == null)
            Debug.LogError("Pipeline not found in " + gameObject.name);
        return pipeline;
    }

    protected abstract void UpdateInitializer(TInit init, int loop);
    protected abstract void OnFinished(TPipe pipeline);
}