using System;
using System.Collections.Generic;
using DefaultNamespace.Tools;
using NaughtyAttributes;
using UnityEngine;

[Serializable]
public struct InitConditions
{
	[Header("Data settings")]
	[SerializeField,Tooltip("Different type of loadable textures exist as scriptable object under Particle/Textures")] private ATexProvider _texture;
	[SerializeField] private RulesSaved _rules;
	[SerializeField] private ColorPicker _colors;
	[Header("Gates")] 
	[SerializeField, Range(.01f, .6f)] private float _gateSize;
	[SerializeField,Tooltip("Toggle between random gates with number and weights for type OR fixed gates position and type")] private bool _randomGates;
	[Header("Random")]
	[SerializeField, Range(0, 25),ShowIf(nameof(_randomGates))]
	private int _nbRandomGates;
	[SerializeField,ShowIf(nameof(_randomGates))] private DictionaryFromList<Area2D.AreaType, float> _weights;
	[Header("Fixed")]
	[SerializeField,HideIf(nameof(_randomGates))] private DictionaryFromList<Area2D.AreaType, Vector2[]> _gatesRelativePosition;
	public ATexProvider Texture => _texture;
	public Ruleset Rules => _rules.Rules;

	public Dictionary<Area2D.AreaType, Vector2[]> Position => _gatesRelativePosition.Dictionary;

	public float GateSize => _gateSize;

	public Dictionary<Area2D.AreaType, float> Weights => _weights.Dictionary;

	public int NbRandomGates => _nbRandomGates;

	public bool RandomGates => _randomGates;
	public ColorPicker Colors => _colors;
}
