using System.Threading.Tasks;
using Godot;
using UnityEngine;
using Texture = Godot.Texture;

public abstract class ATexProvider : ScriptableObject
{
    public abstract Task Create();
    public abstract Image Texture { get; }
    public abstract string Name { get; }
}