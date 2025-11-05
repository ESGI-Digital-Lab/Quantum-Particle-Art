using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace.Tools;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.ExternalScripts.Particle.Simulation;
using Random = System.Random;
using Vector2 = UnityEngine.Vector2;

public interface IGates
{
	public void Reset();
	public IEnumerable<(AGate gateModel, Vector2 pos)> Positions { get; }
}

public class RandomGates : IGates
{
	private int _nbPoints;
	private DictionaryFromList<AGate, float> _weights;

	public RandomGates(int nbPoints, DictionaryFromList<AGate, float> weights)
	{
		_nbPoints = nbPoints;
		_weights = weights;
	}

	public void Reset()
	{
		
	}

	public IEnumerable<(AGate gateModel, Vector2 pos)> Positions
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
	[Header("Fixed")] [SerializeField] private DictionaryFromList<AGate, Godot.Vector2[]> _gatesRelativePosition;

	public FixedGates(DictionaryFromList<AGate, Godot.Vector2[]> gatesRelativePosition)
	{
		_gatesRelativePosition = gatesRelativePosition;
	}

	public void Reset()
	{
		
	}

	public IEnumerable<(AGate gateModel, Vector2 pos)> Positions
	{
		get
		{
			return _gatesRelativePosition.Dictionary.SelectMany(kvp =>
				kvp.Value.Select(v => (kvp.Key,new Vector2(v.X,v.Y)))
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


	public float Size => _size;

	public IGates IGates => _gates;
}
[Serializable]
public struct InitConditions
{
	public InitConditions(InitConditions other) : this(other._ratio, other._texture, other._rules, other._colors, other._gateSize, other._specyPicker, other._spawn)
	{
		
	}
	public InitConditions(float ratio, ATexProvider texture, RulesSaved rules, IColorPicker colors, float gateSize, ISpecyPicker specyPicker, ASpawnConfiguration spawn)
	{
		_texture = texture;
		_rules = rules;
		_colors = colors;
		_spawn = spawn.Duplicate(true) as ASpawnConfiguration;
		_spawn.Reset();
		_gateSize = gateSize;
		_specyPicker = specyPicker;
		_ratio = ratio;
	}

	[Header("Data settings")]
	[SerializeField, Tooltip("Different type of loadable textures exist as scriptable object under Particle/Textures")]
	private ATexProvider _texture;

	[SerializeField] private RulesSaved _rules;
	[SerializeField] private IColorPicker _colors;
	[SerializeField] private ASpawnConfiguration _spawn;
	private float _gateSize;
	[SerializeField] private ISpecyPicker _specyPicker;
	private float _ratio;
	private string _nameAddon = "";

	public ATexProvider Texture => _texture;
	public Ruleset Rules => _rules.Rules;
	public IGates IGates => _spawn.Gates;
	public ASpawnConfiguration Spawn => _spawn;
	public EncodedConfiguration EncodedSpawn => Spawn as EncodedConfiguration;
	
	public float GateSize => _gateSize;

	public IColorPicker Colors => _colors;

	public ISpecyPicker SpecyPicker => _specyPicker;

	public float Ratio => _ratio;

	public string Name
	{
		get => _texture.Name +"_"+_nameAddon;
		set => _nameAddon = value;
	}
}
