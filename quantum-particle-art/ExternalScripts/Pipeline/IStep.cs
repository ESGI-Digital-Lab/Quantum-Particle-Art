using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface IStep<TStep> 
{
    public Task Step(TStep entry, float delay);
    TStep Result { get; }
    void Release();
}

public interface IInit<in TInit> 
{
    Task Init(TInit init);
}

