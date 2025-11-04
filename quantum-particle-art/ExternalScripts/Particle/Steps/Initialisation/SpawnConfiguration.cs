using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class SpawnConfiguration : ASpawnConfiguration
{
	[ExportGroup("Positions")] [Export] private Vector2 _center;
	[Export(PropertyHint.Link)] private Vector2 _size;
	[Export] private bool _linSpaceOverRandom = false;
	[ExportGroup("Gates")]
	[Export] private int _nbGates = 100;
	[Export] private Godot.Collections.Array<AGate> _availableGates;
	private IGates _gates;
	public override IGates Gates => _gates;

	private Vector2 _posMin => _center - (_size / 2f);
	private Vector2 _posMax => _center + (_size / 2f);
	private Random _random;
	public override void Reset()
	{
		base.Reset();
		_gates = new RandomGates(_nbGates,
			new DictionaryFromList<AGate, float>(_availableGates.Select((g => (g, 1f)))));
	}

	private float RandomRange(Vector2 range)
	{
		return RandomRange(range.X, range.Y);
	}

	private float RandomRange(float min, float max)
	{
		return ((float)_random.NextDouble() * (max - min)) + min;
	}

	public override IEnumerable<UnityEngine.Vector2> Particles(System.Random random)
	{
		this._random = random;
		var lb = _posMin;
		var ub = _posMax;
		if (_linSpaceOverRandom)
		{
			foreach (var vector2 in LinearReparition(lb, ub, NbParticles)) yield return vector2;
		}
		else
		{
			for (int i = 0; i < NbParticles; i++)
			{
				Vector2 normalizedPos = new Vector2(RandomRange(lb.X, ub.X), RandomRange(lb.Y, ub.Y));
				yield return normalizedPos;
			}
		}
	}

	protected override UnityEngine.Vector2 BaseVelocity() => new UnityEngine.Vector2(UnityEngine.Random.Range(-1.0f, 1.0f), UnityEngine.Random.Range(-1.0f, 1.0f));
}
