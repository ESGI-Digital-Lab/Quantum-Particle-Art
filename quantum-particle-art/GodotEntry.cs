using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DefaultNamespace.Particle.Steps;
using DefaultNamespace.Tools;
using Godot;
using UnityEngine.Assertions;
using UnityEngine.ExternalScripts.Particle.Genetics;
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

	[ExportCategory("Genetic Algorithm")] [ExportGroup("Training")] [ExportSubgroup("Genetic meta parameters")] [Export]
	private int _nbInstances = 50;

	[Export] private GAParams _params;
	[ExportSubgroup("Gates")] [Export] private bool _forceAllGatesLabel = true;
	[Export] private Godot.Collections.Array<AGate> _gates;
	[ExportGroup("Playback")] [Export] private Godot.Collections.Array<ChromosomeConfigurationBase> _replays;

	#endregion

	private enum Mode
	{
		Training = 0,
		Replay = 1,
		Live = 2
	}

	[ExportCategory("Common parameters for all iterations")] [Export]
	private Mode _mode;

	private bool _training => _mode == Mode.Training;

	[ExportGroup("World")] [Export] private float _worldSize = 600;

	[Export] private float _timeSteps = 0.02f;
	[Export] private int _maxSteps = 2000;
	[ExportGroup("Drawing")] [Export] private bool _saveLastFrame = true;

	[Export] private bool _lateSave;
	[Export] private bool _drawLive = false;
	[Export] private bool _drawLate = false;
	[Export] private Godot.Collections.Array<float> _saveThreholds;

	[ExportSubgroup("Stroke settings")] [Export]
	private CompressedTexture2D _brush;

	[Export(PropertyHint.Range, "0,1000,1")]
	private int _maxStrokeSize = 10;
	[Export(PropertyHint.Range, "0,1000,1")]
	private float _liveBrushSizeDivider = 10;

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
	ASpawnConfiguration _spawnTemplate;
	//private Godot.Collections.Array<ASpawnConfiguration> _spawns = new();

	[ExportGroup("Gates")] [Export] private bool _allowSameSpeciesInteraction = false;

	[Export(PropertyHint.Range, "0,1,0.001")]
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
		var spawnTemplate = _spawnTemplate;

		List<GeneticLooper> _loopers = new();
		var availableSize = new Vector2I(spawnTemplate.NbParticles - 1, spawnTemplate.NbParticles);
		List<ParticleStep> psteps;
		List<IInit<ParticleWorld>> prewarm;
		AGate.ShowLabelDefault = _forceAllGatesLabel;
		GlobalTick globalTick;
		CreateSteps(uniqueCondition.Ratio, true, out psteps, out prewarm, out globalTick);
		PipelineLooper<WorldInitializer, ParticleWorld, ParticleSimulation> viewerLooper =
			_mode switch
			{
				Mode.Training => new GeneticLooper(0, availableSize, new InitConditions(uniqueCondition), psteps,
					psteps,
					prewarm,
					_targetHeightOfBackgroundTexture),
				Mode.Replay =>
					new ReplayLooper(conditions, psteps, psteps, prewarm, _replays, _targetHeightOfBackgroundTexture),
				Mode.Live => new MultipleImagesLooper(-1f, conditions, psteps, psteps, prewarm,
					_targetHeightOfBackgroundTexture),
				_ => throw new ArgumentOutOfRangeException()
			};
		BindLooper(viewerLooper, globalTick);
		_renderMono = viewerLooper;
		Genetics globalGenetics = null;
		GatesTypesToInt.OverrideReflection(new EmptyGate(), _gates);

		if (_training)
		{
			for (int i = 0; i < _nbInstances; i++)
			{
				CreateSteps(uniqueCondition.Ratio, false, out psteps, out prewarm, out globalTick);
				var looper = new GeneticLooper(0, availableSize, new InitConditions(uniqueCondition), psteps, psteps,
					prewarm, -1);
				BindLooper(looper, globalTick);
				_loopers.Add(looper);
				_monos.Add(looper);
			}

			//Starts GA asynchronously using the provided loopers to run and evaluate simulations
			globalGenetics = new Genetics(spawnTemplate.NbParticles, availableSize, _params, _loopers,
				(GeneticLooper)viewerLooper,
				_saveThreholds);
			if (_lateSave)
			{
				var lateSave = viewerLooper.GetStep<LateWriteToTex>();
				globalGenetics.OnThresholdReached += t =>
				{
					if (t.firstReach)
					{
						if (_drawLate)
						{
							lateSave.RequestSave("Fit-", (t.value * 100).ToString("F0"));
							var saved = new ChromosomeConfiguration(t.chromosome, availableSize);
							saved.SetName(lateSave.FullName);
							ResourceSaver.Save(saved, "res://Data//Saved//" + saved.GetName() + ".tres");
						}
					}
				};
			}
		}
		else
		{
			if (_drawLate)
				viewerLooper.GetStep<LateWriteToTex>().SaveAll = _lateSave;
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

	private void CreateSteps(float ratio, bool withView, out List<ParticleStep> psteps,
		out List<IInit<ParticleWorld>> prewarm, out GlobalTick tick)
	{
		psteps = new();
		prewarm = new();
		LineCollection lineCollection = new();
		tick = new GlobalTick(_timeSteps, _maxSteps);

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
			var brushName = _brush.FileName(); //Last part without extension
			var smallBrushSize = Math.Max(1,(int)(_maxStrokeSize / _liveBrushSizeDivider));
			if(smallBrushSize > 2)
				Debug.LogWarning("Small brush size for live drawing is "+smallBrushSize+", if performance is low consider increasing the live brush size divider from "+_liveBrushSizeDivider+" to reach something closer to 1");
			var smallBrush = new Brush(_brush.GetImage(), smallBrushSize, _relativeRandomBrushOffset, brushName);
			if (_drawLive)
			{
				_write = new WriteToTex(_display, WorldSize(_viewportSizeInWindow, ratio).y,
					_saveLastFrame ? new Saver(ProjectSettings.GlobalizePath("res://Visuals/Saved")) : null,
					lineCollection,
					smallBrush);
				psteps.Add(_write);
			}

			if (_drawLate)
			{
				var detailledBrush =
					new Brush(_brush.GetImage(), _maxStrokeSize, _relativeRandomBrushOffset, brushName);
				IWidther widther = new ToggleLiner(_dynamicMax);
				var lateWrite = new LateWriteToTex(_saveLastFrame || true
					? new Saver(ProjectSettings.GlobalizePath("res://Visuals/Saved/Late"))
					: null, detailledBrush, widther, _curveRes);
				psteps.Add(lateWrite);
			}
		}
	}

	private void BindLooper(PipelineLooper<WorldInitializer, ParticleWorld, ParticleSimulation> looper, GlobalTick tick)
	{
		tick.onAllDead += looper.ExternalStop;
		looper.BaseInitializer = new WorldInitializer(_worldSize);
		looper.SetNode(this);
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
