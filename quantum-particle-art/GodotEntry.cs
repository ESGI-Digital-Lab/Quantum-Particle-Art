using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DefaultNamespace.Particle.Steps;
using Godot;

namespace UnityEngine;

public partial class GodotEntry : Node
{
	private List<MonoBehaviour> _monos;
	private Task[] _tasks;
	[Export] private Node2D _space;
	[ExportCategory("World")]
	[Export] private Godot.Vector2 _worldSize = new(600, 600);

	[Export] private Godot.Vector2 _startArea = new(0.5f, 0.5f);
	[Export] private Godot.Vector2 _startAreaWidth = new(1, 1);
	[ExportCategory("Particles")]
	[Export(PropertyHint.Range, "1,12")]
	private int _nbSpecies = 5;
	[Export] private int _nbParticles = 100;
	[Export] private RulesSaved.Defaults _ruleType = RulesSaved.Defaults.Alliances;
	[ExportCategory("Gates")]
	[Export] private int _nbGates = 20;
	[Export(PropertyHint.Range, "0,1,0.01")]
	private float _gateSize = .05f;
	[ExportCategory("Gates weights")]
	[Export(PropertyHint.Range, "0,10,0.1")] private float _entangleWeight = 1f;
	[Export(PropertyHint.Range, "0,10,0.1")] private float _measureWeight = 1f;
	[Export(PropertyHint.Range, "0,10,0.1")] private float _superposeWeight = 1f;
	[Export(PropertyHint.Range, "0,10,0.1")] private float _teleportWeight = 1f;
	

	public override void _Ready()
	{
		_monos = new();
		List<ParticleStep> psteps = new();
		List<IInit<ParticleWorld>> prewarm = new();
		var tick = new GlobalTick();
		psteps.Add(tick);
		var Influence = new SpeciesInfluence();
		psteps.Add(Influence);
		var gates = new PointsIntersection();
		psteps.Add(gates);
		var view = new View(_space, "res://Scenes/Views/ParticleView.tscn", "res://Scenes/Views/GateView.tscn");
		psteps.Add(view);
		prewarm.Add(view);
		var looper = new MultipleImagesLooper(InitConditionsArray(_entangleWeight,_measureWeight,_superposeWeight,_teleportWeight), psteps, psteps, prewarm);

		var world = new WorldInitializer(_worldSize, _nbParticles, _startArea-_startAreaWidth/2f, _startAreaWidth);
		looper.BaseInitializer = world;
		Add(looper);
		_tasks = _monos.Select(m => m.Awake()).ToArray();
		//Task.WaitAll(_tasks);
		Debug.LogWarning("Entry mid initializing");
		_tasks = _monos.Select(m => m.Start()).ToArray();
		//Task.WaitAll(_tasks);
		Debug.LogWarning("Entry finished initializing");
	}

	private InitConditions[] InitConditionsArray(float ent = 1f, float mea = 1f, float sup = 1f, float tel = 1f)
	{
		var gatesWeights = new DictionaryFromList<Area2D.AreaType, float>(
			new()
			{
				{ Area2D.AreaType.Entangle, ent },
				{ Area2D.AreaType.Measure, mea },
				{ Area2D.AreaType.Superpose, sup },
				{ Area2D.AreaType.Teleport, tel }
			});
		var rules = new RulesSaved(_nbSpecies, _ruleType);
		InitConditions[] initConditionsArray = [
			new InitConditions(new CanvasPixels(1024,1024, Color.blue), rules, ColorPicker.Random(_nbSpecies), new Gates(_gateSize, new RandomGates(_nbGates, gatesWeights)))
		];
		return initConditionsArray;
	}

	public override void _Process(double delta)
	{
		Time.time += (float)delta;
		for (int i = 0; i < _tasks.Length; i++)
			_tasks[i] = _monos[i].Update();
		//Task.WaitAll(_tasks);
	}


	private void Add(MonoBehaviour pipe)
	{
		pipe.SetNode(this);
		_monos.Add(pipe);
	}
}
