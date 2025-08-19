using UnityEngine;

public abstract class ATexProvider : ScriptableObject
{
    public abstract void Create();
    public abstract Texture Texture { get; }
    public abstract Color[] GetPixels();
    public virtual string Name =>  name;

}