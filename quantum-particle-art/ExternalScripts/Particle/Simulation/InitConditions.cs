using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace.Tools;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Assertions;
using Random = System.Random;

public interface IGates
{
	public IEnumerable<(Area2D.AreaType type, Vector2 pos)> Positions { get; }
}

public class RandomGates : IGates
{
	private int _nbPoints;
	private DictionaryFromList<Area2D.AreaType, float> _weights;

	public RandomGates(int nbPoints, DictionaryFromList<Area2D.AreaType, float> weights)
	{
		_nbPoints = nbPoints;
		_weights = weights;
	}

	public IEnumerable<(Area2D.AreaType type, Vector2 pos)> Positions
	{
		get
		{
			var dictionary = _weights.Dictionary;
			if (_nbPoints <= 0)
				yield break;
			var total = dictionary.Values.Sum();
			Assert.IsTrue(dictionary.Values.Any(v => v > 0), "One weight at least should be strictly positive > 0.");
			for (int i = 0; i < _nbPoints; i++)
				yield return (WeightedRandom(dictionary, total),
					new Vector2(Random.Shared.NextSingle(), Random.Shared.NextSingle()));
		}
	}

	public T WeightedRandom<T>(Dictionary<T, float> weights, float total)
	{
		float rd = (float)Random.Shared.NextDouble() * total;
		float sum = 0f;
		foreach (var weight in weights)
		{
			sum += weight.Value;
			if (rd <= sum)
				return weight.Key;
		}

		Assert.IsTrue(false);
		return default(T);
	}
}

public class FixedGates : IGates
{
	[Header("Fixed")] [SerializeField] private DictionaryFromList<Area2D.AreaType, Vector2[]> _gatesRelativePosition;

	public FixedGates(DictionaryFromList<Area2D.AreaType, Vector2[]> gatesRelativePosition)
	{
		_gatesRelativePosition = gatesRelativePosition;
	}

	public IEnumerable<(Area2D.AreaType type, Vector2 pos)> Positions
	{
		get
		{
			return _gatesRelativePosition.Dictionary.SelectMany(kvp =>
				kvp.Value.Select(v => (kvp.Key,v))
			);
		}
	}
}

public struct Gates
{
	[Header("Gates")] [SerializeField, Range(.01f, .6f)]
	private float _size;
	private IGates _gates;

	public Gates(float size, IGates gates)
	{
		_size = size;
		_gates = gates;
	}

	public IEnumerable<(Area2D.AreaType type, Vector2 pos)> Positions => _gates.Positions;

	public float Size => _size;
}
[Serializable]
public struct InitConditions
{
	public InitConditions(ATexProvider texture, RulesSaved rules, IColorPicker colors, Gates gates)
	{
		_texture = texture;
		_rules = rules;
		_colors = colors;
		_gates = gates;
	}

	[Header("Data settings")]
	[SerializeField, Tooltip("Different type of loadable textures exist as scriptable object under Particle/Textures")]
	private ATexProvider _texture;

	[SerializeField] private RulesSaved _rules;
	[SerializeField] private IColorPicker _colors;
	[SerializeField] private Gates _gates;

	public ATexProvider Texture => _texture;
	public Ruleset Rules => _rules.Rules;

	public IEnumerable<(Area2D.AreaType type, Vector2 pos)> Position => _gates.Positions;

	public float GateSize => _gates.Size;

	public IColorPicker Colors => _colors;
}
