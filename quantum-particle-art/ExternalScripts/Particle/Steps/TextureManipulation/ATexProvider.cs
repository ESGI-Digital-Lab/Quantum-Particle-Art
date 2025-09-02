using Godot;
using UnityEngine;
using Texture = Godot.Texture;

public abstract class ATexProvider : ScriptableObject
{
    public abstract void Create();
    public abstract Image Texture { get; }
    public abstract string Name { get; }
}