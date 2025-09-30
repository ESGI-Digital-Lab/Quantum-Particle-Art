using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DefaultNamespace.Particle.Steps;
using DefaultNamespace.Particle.Steps.TextureManipulation;
using DefaultNamespace.Tools;
using Godot;
using UnityEngine.Assertions;
using UnityEngine.ExternalScripts.Particle.Simulation;

namespace UnityEngine;

public partial class GodotEntry : Node
{
	private List<MonoBehaviour> _monos;
	private Task[] _tasks;

	[ExportCategory("Display settings")] [ExportGroup("References")] [Export]
	private Sprite2D _display;

	[Export] private Camera2D _camera;
	[Export] private Node2D _space;
	[ExportGroup("View")] [Export] private float _viewportSizeInWindow = 400f;

	[Export(PropertyHint.Range, "0,10,0.1")]
	private float _zoom = 1f;

	[ExportCategory("Common parameters for all iterations")] [ExportGroup("World")] [Export]
	private float _worldSize = 600;

	[Export] private float _timeSteps = 0.02f;
	[ExportGroup("Drawing")] [Export] private bool _saveLastFrame = true;

	[ExportSubgroup("Stroke settings")] [Export(PropertyHint.Range, "0,100,1")]
	private int _maxStrokeSize = 10;

	[Export] private float _sineFrequency;

	[ExportSubgroup("Type of stroke")] [Export]
	private bool _squareStrokeOverCircle = false;

	[Export] private bool _useSpeed;
	[Export] private bool _dynamicMax;


	private enum BackgroundSource
	{
		Canvas = 0,
		Webcam = 1,
		RealImage = 2
	}

	[ExportGroup("Background")] [ExportSubgroup("Common settings, any background will try to fit this")] [Export]
	private int _targetHeightOfBackgroundTexture;

	[Export] private Vector2I _ratio = new(16, 9);


	[ExportSubgroup("Webcam only settings")] [Export]
	private CameraCSBindings _webcamFeed;

	[Export] private Vector2I _webcamRatio = new(16, 9);

	#region Particles&Gates

	[ExportGroup("Particles")] [ExportSubgroup("Spawn area")] [Export]
	private Godot.Collections.Array<ASpawnConfiguration> _spawns = new();

	[ExportGroup("Gates")] [Export] private bool _allowSameSpeciesInteraction = false;

	[Export(PropertyHint.Range, "0,1,0.01")]
	private float _gateSize = .05f;

	[Export] private GridGates _backupGates;

	#endregion

	#region Loop

	[ExportCategory(
		"Loop settings, goes to next value in each array (warping) on one step finished after defined duration")]
	[Export]
	private float _duration;

	[Export] private int _nbMinLoops = 10;

	[ExportGroup(
		"Backgrounds, any time a canvas or image is used it will progress on it's relative list, webcam settings are constant any time it's used")]
	[Export]
	private Godot.Collections.Array<BackgroundSource> _backgroundTypes;

	[ExportSubgroup("Canvas list")] [Export(PropertyHint.ColorNoAlpha)]
	private Godot.Color[] _backgroundColorForCanva;

	[Export] private ColorPalette[] _colorSchemeForCanva;

	[ExportSubgroup("Real image list")] [Export]
	private Godot.Collections.Array<Godot.Texture2D> _realImage;

	[ExportGroup("Species interactions")] [Export(PropertyHint.Range, "1,12")]
	private int[] _nbSpecies;

	[Export] private Godot.Collections.Array<RulesSaved.Defaults> _ruleType;

	#endregion

	#region Grid

	[ExportCategory("Grid")] [Export] private int _nbInstances = 50;

	#endregion

	private WriteToTex _write;

	private static Vector2 WorldSize(float height, float ratio) => new(height * ratio, height);
	private static Vector2I WorldSize(int height, float ratio) => new((int)(height * ratio), height);

	public override void _Ready()
	{
		View.DefaultTimerRoot = this;
		InitConditions[] conditions = InitConditionsArray();
		var uniqueCondition = conditions[0];
		float initialRatio = uniqueCondition.Ratio; //TODO generalize scaling for every step

		_monos = new();
		for (int i = 0; i < _nbInstances; i++)
		{
			var looper = CreateLooper(new InitConditions(uniqueCondition), i, i == 0);
			Add(looper);
		}

		_camera.Zoom = Godot.Vector2.One * _zoom; //Depending on the number of instances with view
		try
		{
			_tasks = _monos.Select(m => Task.Run(async () => await m.Awake())).ToArray();
			Task.WaitAll(_tasks);
			Debug.LogWarning("Entry mid initializing");
			_tasks = _monos.Select(m => Task.Run(async () => await m.Start())).ToArray();
			Task.WaitAll(_tasks);
			Debug.LogWarning("Entry finished initializing");
		}
		catch (Exception e)
		{
			GD.PrintErr("Exception during initialization: ", e);
		}
	}

	public override void _Process(double delta)
	{
		Time.time += (float)delta;
		try
		{
			//Debug.Log("Starting updates");
			_tasks = _monos.Select(m => Task.Run(async () => await m.Update())).ToArray();
			Task.WaitAll(_tasks);
			//Debug.Log("Finished updates");
		}
		catch (Exception e)
		{
			GD.PrintErr("Exception during update: ", e, "trace : ", e.StackTrace);
			throw e;
		}
		//Task.WaitAll(_tasks);²
	}

	private GeneticLooper CreateLooper(InitConditions conditions, int id, bool withView = true)
	{
		List<ParticleStep> psteps = new();
		List<IInit<ParticleWorld>> prewarm = new();
		LineCollection lineCollection = new();
		var tick = new GlobalTick(_timeSteps);

		psteps.Add(tick);
		var Influence = new SpeciesInfluence();
		psteps.Add(Influence);
		var gates = new PointsIntersection(lineCollection, false, !_allowSameSpeciesInteraction);
		psteps.Add(gates);
		if (withView)
		{
			var view = new View(_space, "res://Scenes/Views/ParticleView.tscn", "res://Scenes/Views/GateView.tscn");
			psteps.Add(view);
			ILiner liner = _useSpeed ? new ToggleLiner(_dynamicMax) : new ToggleLiner(_sineFrequency);
			tick.onMovement += data =>
			{
				lineCollection.AddLine(liner.CreateLine(data));
				//Debug.Log("Speed : "+ info.particle.Orientation.NormalizedSpeed);
			};
			_write = new WriteToTex(_display, WorldSize(_viewportSizeInWindow, conditions.Ratio).y, _maxStrokeSize,
				_saveLastFrame ? new Saver(ProjectSettings.GlobalizePath("res://Visuals/Saved")) : null, lineCollection,
				!_squareStrokeOverCircle);
			psteps.Add(_write);
			prewarm.Add(view);
		}

		var code = _spawns.Select(s => s.Skip ? null : s as EncodedConfiguration).FirstOrDefault(s => s != null);
		if (code == null)
		{
			Debug.LogError("No encoding spawn found, looper won't work properly");
		}

		var looper = new GeneticLooper(id, _duration, conditions, code, psteps, psteps, prewarm,
			_targetHeightOfBackgroundTexture);
		// = new MultipleImageLooper new(_duration, conditions, psteps, psteps, prewarm,_targetHeightOfBackgroundTexture);
		//looper.InitChange += OnInitChanged;
		tick.onAllDead += () => { looper.ExternalRestart(); };
		var world = new WorldInitializer(_worldSize, _spawns.ToArray());
		looper.BaseInitializer = world;
		return looper;
	}

	private void OnInitChanged(InitConditions init)
	{
		var ratio = init.Ratio;
		var viewScale = WorldSize(_viewportSizeInWindow, ratio);
		_space.Scale = viewScale;
		_write.ViewSize = viewScale.y;
	}


	private InitConditions[] InitConditionsArray()
	{
		Assert.IsTrue(_targetHeightOfBackgroundTexture > 0, "Target height of background texture must be >0");
		var amt = Math.Max(Math.Max(_nbMinLoops, _nbSpecies.Length), Math.Max(_backgroundTypes.Count, _ruleType.Count));
		InitConditions[] initConditionsArray = new InitConditions[amt];
		var spawn = _spawns.FirstOrDefault(s => s != null && !s.Skip && s.Gates != null && s is EncodedConfiguration) as EncodedConfiguration;
		Assert.IsNotNull(spawn, "No valid EncodedConfiguration template spawn with gates found in the list of spawns, cannot proceed");
		int canvasCount = -1;
		int imageCount = -1;
		for (int i = 0; i < amt; i++)
		{
			var rule = _ruleType[i % _ruleType.Count];
			Assert.IsTrue(rule != RulesSaved.Defaults.Default);
			var rules = new RulesSaved(_nbSpecies[i % _nbSpecies.Length], rule);
			int nbSpecy = rules.Rules.NbSpecies; //in case ruleset changed it
			ATexProvider tex;
			IColorPicker colors;
			ISpecyPicker specyPicker;
			float ratio = 1f;
			var bgType = _backgroundTypes[i % _backgroundTypes.Count];

			switch (bgType)
			{
				case BackgroundSource.Canvas:
					canvasCount++;
					var color = _backgroundColorForCanva[canvasCount % _backgroundColorForCanva.Length];
					ratio = _ratio.X / (1f * _ratio.Y);
					tex = new CanvasPixels(WorldSize(_targetHeightOfBackgroundTexture, ratio),
						color.A == 0f ? ColorPicker.Random() : color);
					var scheme = _colorSchemeForCanva[canvasCount % _colorSchemeForCanva.Length];
					Assert.IsTrue(scheme == null || (scheme.Colors != null && scheme.Colors.Length >= nbSpecy),
						"Color scheme has less colors than species, will fallback to a random scheme of the correct size.");
					colors = scheme?.Colors == null || scheme.Colors.Length < nbSpecy
						? ColorPicker.Random(nbSpecy)
						: ColorPicker.FromScheme(scheme.Colors);
					specyPicker = new UniformSpecyPicker(nbSpecy);
					break;
				case BackgroundSource.Webcam:
					ratio = _webcamRatio.X / (1f * _webcamRatio.Y);
					_webcamFeed.Start();
					FromImage(_webcamFeed.Texture, nbSpecy, ratio, out colors, out specyPicker, out tex);
					break;
				case BackgroundSource.RealImage:
					imageCount++;
					var texture2 = _realImage[imageCount % _realImage.Count];
					ratio = texture2.GetWidth() / (1f * texture2.GetHeight());
					FromImage(texture2.GetImage(), nbSpecy, ratio, out colors, out specyPicker, out tex);
					break;
				default: throw new ArgumentOutOfRangeException();
			}

			initConditionsArray[i] = new InitConditions(ratio, tex, rules, colors,
				_gateSize, specyPicker, spawn);
		}


		return initConditionsArray;
	}

	private void FromImage(Image image, int nbSpecy, float ratio, out IColorPicker colors, out ISpecyPicker specyPicker,
		out ATexProvider tex)
	{
		var quantized = new QuantizedImage(image, nbSpecy, WorldSize(_targetHeightOfBackgroundTexture, ratio));
		tex = quantized;
		colors = quantized;
		specyPicker = quantized;
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			_monos.ForEach(m => m.Dispose());
		}


		base._Notification(what);
	}


	private void Add(MonoBehaviour mbh)
	{
		mbh.SetNode(this);
		_monos.Add(mbh);
	}
}
