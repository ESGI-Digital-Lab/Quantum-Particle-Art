using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DefaultNamespace.Particle.Steps;
using Godot;
using UnityEngine.Assertions;

namespace UnityEngine;

public partial class GodotEntry : Node
{
	private List<MonoBehaviour> _monos;
	private Task[] _tasks;
	[Export] private Node2D _space;

	[ExportCategory("World")] [Export(PropertyHint.Link)]
	private float _worldSize = 600;

	[Export()] private float _worldAspect = 2;

	[Export(PropertyHint.Link)] private Godot.Vector2 _startArea = new(0.5f, 0.5f);
	[Export(PropertyHint.Link)] private Godot.Vector2 _startAreaWidth = new(1, 1);

	[ExportCategory("Loop")] [Export] private float _duration;
	[ExportCategory("Particles")] [Export] private int _nbParticles;
	[Export(PropertyHint.Range, "1,12")] private int[] _nbSpecies;
	[Export] private ColorPalette[] _schemes;

	[Export] private int[] _ruleType;
	[ExportCategory("Gates")] [Export] private int _nbGates = 20;

	[Export(PropertyHint.Range, "0,1,0.01")]
	private float _gateSize = .05f;

	[ExportCategory("Gates weights")] [Export(PropertyHint.Range, "0,10,0.1")]
	private float _entangleWeight = 1f;

	[Export(PropertyHint.Range, "0,10,0.1")]
	private float _measureWeight = 1f;

	[Export(PropertyHint.Range, "0,10,0.1")]
	private float _superposeWeight = 1f;

	[Export(PropertyHint.Range, "0,10,0.1")]
	private float _teleportWeight = 1f;

	[ExportCategory("Tex")] [Export] private Sprite2D _display;

	[Export] private Godot.Texture2D _backgrounds;
	[Export] private Godot.Color[] _color;
	[Export(PropertyHint.Link)] private int _textureSize;
	[ExportCategory("Saving")] [Export] private bool _saveLastFrame = true;
	[ExportCategory("View")] [Export] private float _viewportSizeInWindow = 400f;

	[Export] private Camera2D _camera;

	[Export(PropertyHint.Range, "0,10,0.1")]
	private float _zoom = 1f;

	private Vector2 WorldSize(float height) => new(height * _worldAspect, height);
	private Vector2I WorldSize(int height) => new((int)(height * _worldAspect), height);

	public override void _Ready()
	{
		_monos = new();
		List<ParticleStep> psteps = new();
		List<IInit<ParticleWorld>> prewarm = new();
		var tick = new GlobalTick();
		psteps.Add(tick);
		var Influence = new SpeciesInfluence();
		psteps.Add(Influence);
		var gates = new PointsIntersection(false);
		psteps.Add(gates);
		var viewScale = WorldSize(_viewportSizeInWindow);
		_space.Scale = viewScale;
		_camera.Zoom = Godot.Vector2.One * _zoom;
		var view = new View(_space, "res://Scenes/Views/ParticleView.tscn", "res://Scenes/Views/GateView.tscn");
		psteps.Add(view);
		var writeToTex = new WriteToTex(_display, viewScale.y,
			_saveLastFrame ? new Saver(ProjectSettings.GlobalizePath("res://Visuals/Saved")) : null);
		psteps.Add(writeToTex);
		prewarm.Add(view);
		var looper =
			new MultipleImagesLooper(_duration,
				InitConditionsArray(_entangleWeight, _measureWeight, _superposeWeight, _teleportWeight), psteps, psteps,
				prewarm);

		var world = new WorldInitializer(WorldSize(_worldSize), _nbParticles, _startArea - _startAreaWidth / 2f,
			_startAreaWidth);
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
		var ruleEnums = Enum.GetValues(typeof(RulesSaved.Defaults)).Cast<RulesSaved.Defaults>().ToArray();
		var amt = Math.Max(_nbSpecies.Length, Math.Max(_color.Length, _ruleType.Length));
		InitConditions[] initConditionsArray = new InitConditions[amt];
		//Assert.IsTrue(_backgrounds.Length == _color.Length," Backgrounds and colors arrays must be of same length");
		//The build method threw an exception.
		//System.IO.FileNotFoundException: Could not load file or assembly 'Microsoft.Build.Framework, Version=15.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'. Le fichier spécifié est introuvable. (0,0)

		for (int i = 0; i < amt; i++)
		{
			var ruleIndex = _ruleType[i % _ruleType.Length];
			Assert.IsTrue(ruleIndex >= 0 && ruleIndex < ruleEnums.Length,
				$"Rule type index {_ruleType[i % _ruleType.Length]} at position {i} is out of range");
			int nbSpecy = _nbSpecies[i % _nbSpecies.Length];
			var rules = new RulesSaved(nbSpecy, (RulesSaved.Defaults)ruleIndex);
			var color = _color[i % _color.Length];
			ATexProvider tex = new CanvasPixels(WorldSize(_textureSize), color.A == 0f ? ColorPicker.Random() : color);
			var scheme = _schemes[i % _schemes.Length];
			IColorPicker colors = scheme == null
				? ColorPicker.Random(nbSpecy)
				: ColorPicker.FromScheme(scheme.Colors);
			if (_backgrounds != null)//Real image override
			{
				var quantized = new QuantizedImage(_backgrounds, nbSpecy);
				tex = quantized;
				colors = quantized;
			}

			initConditionsArray[i] = new InitConditions(tex, rules, colors,
				new Gates(_gateSize, new RandomGates(_nbGates, gatesWeights)));
		}


		return initConditionsArray;
	}

	public override void _Process(double delta)
	{
		Time.time += (float)delta;
		for (int i = 0; i < _tasks.Length; i++)
			_tasks[i] = _monos[i].Update();
		//Task.WaitAll(_tasks);
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			_monos.ForEach(m => m.Dispose());
		}

		base._Notification(what);
	}


	private void Add(MonoBehaviour pipe)
	{
		pipe.SetNode(this);
		_monos.Add(pipe);
	}
}
