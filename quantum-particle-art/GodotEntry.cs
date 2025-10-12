using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DefaultNamespace.Particle.Steps;
using DefaultNamespace.Tools;
using Godot;
using UnityEngine.Assertions;
using UnityEngine.ExternalScripts.Particle.Simulation;

namespace UnityEngine;

public partial class GodotEntry : Node
{
	private List<MonoBehaviour> _monos;
	private MonoBehaviour _renderMono;
	private Task[] _tasks;

	[ExportCategory("Display settings")] [ExportGroup("References")] [Export]
	private Sprite2D _display;

	[Export] private Camera2D _camera;
	[Export] private Node2D _space;
	[ExportGroup("View")] [Export] private float _viewportSizeInWindow = 400f;

	[Export(PropertyHint.Range, "0,10,0.1")]
	private float _zoom = 1f;

	#region Genetics

	[ExportCategory("Genetic Algorithm")] [Export]
	private bool _training = true;

	[ExportGroup("Training")] [ExportSubgroup("Genetic meta parameters")] [Export]
	private int _nbInstances = 50;

	[Export] private GAParams _params;
	[ExportSubgroup("Gates")] [Export] private bool _forceAllGatesLabel = true;
	[Export] private Godot.Collections.Array<AGate> _gates;
	[ExportGroup("Playback")] [Export] private Godot.Collections.Array<ChromosomeConfiguration> _replays;

	#endregion

	[ExportCategory("Common parameters for all iterations")] [ExportGroup("World")] [Export]
	private float _worldSize = 600;

	[Export] private float _timeSteps = 0.02f;
	[Export] private int _maxSteps = 2000;
	[ExportGroup("Drawing")] [Export] private bool _saveLastFrame = true;
	[Export] private bool _drawLive = false;
	[Export] private Godot.Collections.Array<float> _saveThreholds;

	[ExportSubgroup("Stroke settings")] [Export]
	private CompressedTexture2D _brush;

	[Export(PropertyHint.Range, "0,1000,1")]
	private int _maxStrokeSize = 10;

	[Export] private float _relativeRandomBrushOffset = 0.1f;

	[Export] private int _curveRes = 1000;

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
	EncodedConfiguration _spawnTemplate;
	//private Godot.Collections.Array<ASpawnConfiguration> _spawns = new();

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
		var code = _spawnTemplate;
		//_spawns.Select(s => s.Skip ? null : s as EncodedConfiguration).FirstOrDefault(s => s != null);
		if (code == null)
		{
			Debug.LogError("No encoding spawn found, looper won't work properly");
		}

		var globalLock = new object();
		List<GeneticLooper> _loopers = new();
		var availableSize = new Vector2I(code.NbParticles - 1, code.NbParticles);
		var viewerLooper = CreateLooper(new InitConditions(uniqueCondition), 0, globalLock, availableSize, true,
			_training);
		viewerLooper.SetNode(this);
		var lateSave = viewerLooper.GetStep<LateWriteToTex>();
		_renderMono = viewerLooper;
		Genetics globalGenetics = null;
		AGate.ShowLabelDefault = _forceAllGatesLabel;

		if (_training)
		{
			for (int i = 0; i < _nbInstances; i++)
			{
				var looper = CreateGeneticLooper(new InitConditions(uniqueCondition), i + 1, globalLock, availableSize);
				looper.SetNode(this);
				_loopers.Add(looper);
				_monos.Add(looper);
			}

			//Starts GA asynchronously using the provided loopers to run and evaluate simulations
			globalGenetics = new Genetics(code.NbParticles, availableSize, _params, _loopers,
				(GeneticLooper)viewerLooper, _gates,
				_saveThreholds);
			globalGenetics.OnThresholdReached += t =>
			{
				if (t.firstReach)
				{
					lateSave.RequestSave((t.value * 100).ToString("F0"));
					var saved = new ChromosomeConfiguration(t.chromosome, availableSize);
					saved.SetName(lateSave.FullName);
					ResourceSaver.Save(saved, "res://Data//Saved//" + saved.GetName() + ".tres");
				}
			};
		}
		else
		{
			lateSave.SaveAll = true;
		}

		RunInitMethods();
		if (_training)
		{
			//After all monos has been initialized we can start the genetics that depends on their process => ready cycles
			globalGenetics?.StartAsync();
		}
	}

	private void RunInitMethods()
	{
		_camera.Zoom = Godot.Vector2.One * _zoom;
		try
		{
			_renderMono.Awake().Wait();
			_tasks = _monos.Select(m => Task.Run(async () => await m.Awake())).ToArray();
			Task.WaitAll(_tasks);
			_renderMono.Start().Wait();
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
			_renderMono.Update().Wait();
			_tasks = _monos.Select(m => Task.Run(async () => await m.Update())).ToArray();
			Task.WaitAll(_tasks);
			//Debug.Log("Finished updates");
		}
		catch (Exception e)
		{
			GD.PrintErr("Exception during update: ", e, "trace : ", e.StackTrace);
			throw e;
		}
	}

	private GeneticLooper CreateGeneticLooper(InitConditions conditions, int id, object sharedLock, Vector2I size)
	{
		return CreateLooper(conditions, id, sharedLock, size, false, true) as GeneticLooper;
	}

	private PipelineLooper<WorldInitializer, ParticleWorld, ParticleSimulation> CreateLooper(InitConditions conditions,
		int id, object sharedLock, Vector2I size,
		bool withView = true, bool isGenetic = true)
	{
		List<ParticleStep> psteps = new();
		List<IInit<ParticleWorld>> prewarm = new();
		LineCollection lineCollection = new();
		var tick = new GlobalTick(_timeSteps, _maxSteps);

		psteps.Add(tick);
		var Influence = new SpeciesInfluence();
		psteps.Add(Influence);
		var gates = new PointsIntersection(lineCollection, false, !_allowSameSpeciesInteraction);
		psteps.Add(gates);
		if (withView)
		{
			var view = new View(_space, "res://Scenes/Views/ParticleView.tscn", "res://Scenes/Views/GateView.tscn");
			psteps.Add(view);
			prewarm.Add(view);
			ILiner liner = _useSpeed ? new ToggleLiner(_dynamicMax) : new DeltaRotLiner();
			tick.onMovement += data =>
			{
				lineCollection.AddLine(liner.CreateLine(data));
				//Debug.Log("Speed : "+ info.particle.Orientation.NormalizedSpeed);
			};
			var fileName = _brush.ResourcePath.Split('/')[^1].Split('.')[0]; //Last part without extension
			var smallBrush = new Brush(_brush.GetImage(), _maxStrokeSize / 10, _relativeRandomBrushOffset, fileName);
			if (_drawLive)
			{
				_write = new WriteToTex(_display, WorldSize(_viewportSizeInWindow, conditions.Ratio).y,
					_saveLastFrame ? new Saver(ProjectSettings.GlobalizePath("res://Visuals/Saved")) : null,
					lineCollection,
					smallBrush);
				psteps.Add(_write);
			}

			var detailledBrush = new Brush(_brush.GetImage(), _maxStrokeSize, _relativeRandomBrushOffset, fileName);

			IWidther widther = new ToggleLiner(_dynamicMax);
			var lateWrite = new LateWriteToTex(_saveLastFrame || true
				? new Saver(ProjectSettings.GlobalizePath("res://Visuals/Saved/Late"))
				: null, detailledBrush, widther, _curveRes);
			psteps.Add(lateWrite);
		}

		PipelineLooper<WorldInitializer, ParticleWorld, ParticleSimulation> looper;
		if (isGenetic)
			looper = new GeneticLooper(id, size, conditions, psteps, psteps, prewarm,
				withView ? _targetHeightOfBackgroundTexture : -1);
		else
			looper = new ReplayLooper(conditions, psteps, psteps, prewarm, _replays, _targetHeightOfBackgroundTexture);

		tick.onAllDead += () =>
		{
			//Debug.Log("Finished a simulation with " + tick.NbSteps + " steps/"+_maxSteps +" on looper " + looper.ToString());
			looper.ExternalStop();
		};
		var world = new WorldInitializer(_worldSize);
		looper.BaseInitializer = world;
		return looper;
	}

	private void OnInitChanged(InitConditions init)
	{
		var ratio = init.Ratio;
		var viewScale = WorldSize(_viewportSizeInWindow, ratio);
		_space.Scale = viewScale;
		if (_write != null)
			_write.ViewSize = viewScale.y;
	}


	private InitConditions[] InitConditionsArray()
	{
		Assert.IsTrue(_targetHeightOfBackgroundTexture > 0, "Target height of background texture must be >0");
		var amt = Math.Max(Math.Max(_nbMinLoops, _nbSpecies.Length), Math.Max(_backgroundTypes.Count, _ruleType.Count));
		InitConditions[] initConditionsArray = new InitConditions[amt];
		var spawn = _spawnTemplate;
		//_spawns.FirstOrDefault(s => s != null && !s.Skip && s.Gates != null && s is EncodedConfiguration) asEncodedConfiguration;
		Assert.IsNotNull(spawn,
			"No valid EncodedConfiguration template spawn with gates found in the list of spawns, cannot proceed");
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
}
