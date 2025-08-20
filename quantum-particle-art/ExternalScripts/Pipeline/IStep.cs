using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IStep<TStep> 
{
    public IEnumerator<AsyncEnumerator> Step(TStep entry, float delay);
    TStep Result { get; }
    void Release();
}

public interface IInit<in TInit> 
{
    IEnumerator Init(TInit init);
}

