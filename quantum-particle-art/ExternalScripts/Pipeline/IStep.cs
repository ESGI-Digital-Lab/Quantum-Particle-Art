using System.Collections;
using UnityEngine;

public interface IStep<TStep> 
{
    public IEnumerator Step(TStep entry, float delay);
    TStep Result { get; }
    void Release();
}

public interface IInit<in TInit> 
{
    IEnumerator Init(TInit init);
}

