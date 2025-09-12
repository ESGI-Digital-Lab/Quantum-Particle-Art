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

	[ExportCategory("Common parameters for all iterations")] 
	[ExportGroup("World")] [Export] private float _worldSize = 600;
	[ExportGroup("Drawing")] [Export]
	private bool _saveLastFrame = true;

	[ExportSubgroup("Stroke settings")]
	[Export(PropertyHint.Range, "0,100,1")]
	private int _maxStrokeSize = 10;
	[Export] private float _sineFrequency;
	[ExportSubgroup("Type of stroke")]
	[Export] private bool _squareStrokeOverCircle = false;
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

	[ExportGroup("Particles")] [Export] private int _nbParticles;

	[ExportSubgroup("Spawn area")] [Export(PropertyHint.Link)]
	private Godot.Vector2 _startArea = new(0.5f, 0.5f);

	[Export(PropertyHint.Link)] private Godot.Vector2 _startAreaWidth = new(1, 1);

	[ExportGroup("Gates")] [Export] private int _nbGates = 20;

	[Export]
	private bool _allowSameSpeciesInteraction = false;
	[Export(PropertyHint.Range, "0,1,0.01")]
	private float _gateSize = .05f;

	[ExportSubgroup("Gates weights")] [Export(PropertyHint.Range, "0,10,0.1")]
	private float _controlWeight = 1f;

	[Export(PropertyHint.Range, "0,10,0.1")]
	private float _measureWeight = 1f;

	[Export(PropertyHint.Range, "0,10,0.1")]
	private float _superposeWeight = 1f;

	[Export(PropertyHint.Range, "0,10,0.1")]
	private float _teleportWeight = 1f;

	#endregion

	#region Loop

	[ExportCategory(
		"Loop settings, goes to next value in each array (warping) on one step finished after defined duration")]
	[Export]
	private float _duration;

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
		InitConditions[] conditions =
			InitConditionsArray(_controlWeight, _measureWeight, _superposeWeight, _teleportWeight);
		float initialRatio = conditions[0].Ratio; //TODO generalize scaling for every step

		_monos = new();
		List<ParticleStep> psteps = new();
		List<IInit<ParticleWorld>> prewarm = new();
		LineCollection lineCollection = new();
		ILiner liner = _useSpeed ? new ToggleLiner(_dynamicMax) : new ToggleLiner(_sineFrequency);
		var tick = new GlobalTick();
		tick.onMovement += data =>
		{
			lineCollection.AddLine(liner.CreateLine(data));
			//Debug.Log("Speed : "+ info.particle.Orientation.NormalizedSpeed);
		};
		psteps.Add(tick);
		var Influence = new SpeciesInfluence();
		psteps.Add(Influence);
		var gates = new PointsIntersection(lineCollection, false,!_allowSameSpeciesInteraction);
		psteps.Add(gates);
		_camera.Zoom = Godot.Vector2.One * _zoom;
		var view = new View(_space, "res://Scenes/Views/ParticleView.tscn", "res://Scenes/Views/GateView.tscn");
		psteps.Add(view);
		_write = new WriteToTex(_display, WorldSize(_viewportSizeInWindow, conditions[0].Ratio).y, _maxStrokeSize,
			_saveLastFrame ? new Saver(ProjectSettings.GlobalizePath("res://Visuals/Saved")) : null, lineCollection,
			!_squareStrokeOverCircle);
		psteps.Add(_write);
		prewarm.Add(view);
		MultipleImagesLooper looper = new(_duration, conditions, psteps, psteps, prewarm,
			_targetHeightOfBackgroundTexture);
		looper.InitChange += OnInitChanged;
		var world = new WorldInitializer(_worldSize, _nbParticles, _startArea - _startAreaWidth / 2f,
			_startAreaWidth);
		looper.BaseInitializer = world;
		Add(looper);
		try
		{
			_tasks = _monos.Select(m => m.Awake()).ToArray();
			//Task.WaitAll(_tasks);
			Debug.LogWarning("Entry mid initializing");
			_tasks = _monos.Select(m => m.Start()).ToArray();
			//Task.WaitAll(_tasks);
			Debug.LogWarning("Entry finished initializing");
		}
		catch (Exception e)
		{
			GD.PrintErr("Exception during initialization: ", e);
		}
	}

	private void OnInitChanged(InitConditions init)
	{
		var ratio = init.Ratio;
		var viewScale = WorldSize(_viewportSizeInWindow, ratio);
		_space.Scale = viewScale;
		_write.ViewSize = viewScale.y;
	}


	private InitConditions[] InitConditionsArray(float ent = 1f, float mea = 1f, float sup = 1f, float tel = 1f)
	{
		var gatesWeights = new DictionaryFromList<Area2D.AreaType, float>(
			new()
			{
				{ Area2D.AreaType.Control, ent },
				{ Area2D.AreaType.Measure, mea },
				{ Area2D.AreaType.Superpose, sup },
				{ Area2D.AreaType.Teleport, tel }
			});
		var amt = Math.Max(_nbSpecies.Length, Math.Max(_backgroundTypes.Count, _ruleType.Count));
		InitConditions[] initConditionsArray = new InitConditions[amt];
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
				new Gates(_gateSize, new RandomGates(_nbGates, gatesWeights)), specyPicker);
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

	public override void _Process(double delta)
	{
		Time.time += (float)delta;
		try
		{
			for (int i = 0; i < _tasks.Length; i++)
				_tasks[i] = _monos[i].Update();
		}
		catch (Exception e)
		{
			GD.PrintErr("Exception during update: ", e);
		}
		//Task.WaitAll(_tasks);²
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
