using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DefaultNamespace.Particle.Steps;
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

	[ExportCategory("Common parameters for all iterations")] [Export]
	private bool _saveLastFrame = true;

	[ExportGroup("World")] [Export(PropertyHint.Link)]
	private float _worldSize = 600;

	private enum BackgroundSource
	{
		Canvas = 0,
		Webcam = 1,
		RealImage = 2
	}

	[ExportGroup("Background")] [ExportSubgroup("Canvas common settings")] [Export]
	private int _heightSize;

	[Export] private Vector2I _ratio = new(16, 9);

	[ExportSubgroup("Webcam (only) common settings")] [Export]
	private CameraCSBindings _webcamFeed;

	[Export] private Vector2I _webcamRatio = new(16, 9);

	#region Particles&Gates

	[ExportGroup("Particles")] [Export] private int _nbParticles;

	[ExportSubgroup("Spawn area")] [Export(PropertyHint.Link)]
	private Godot.Vector2 _startArea = new(0.5f, 0.5f);

	[Export(PropertyHint.Link)] private Godot.Vector2 _startAreaWidth = new(1, 1);

	[ExportGroup("Gates")] [Export] private int _nbGates = 20;

	[Export(PropertyHint.Range, "0,1,0.01")]
	private float _gateSize = .05f;

	[ExportSubgroup("Gates weights")] [Export(PropertyHint.Range, "0,10,0.1")]
	private float _entangleWeight = 1f;

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

	private float _worldAspectOnCanvas = 2;
	private Vector2 WorldSize(float height) => new(height * _worldAspectOnCanvas, height);
	private Vector2I WorldSize(int height) => new((int)(height * _worldAspectOnCanvas), height);

	private T SwitchOnBgType<T>(T ifCanvas, T ifWebcam, T ifImage, int loopIndex = 0)
	{
		T variable;
		switch (_backgroundTypes[loopIndex])
		{
			case BackgroundSource.Canvas:
				variable = ifCanvas;
				break;
			case BackgroundSource.Webcam:
				variable = ifWebcam;
				break;
			case BackgroundSource.RealImage:
				variable = ifImage;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(_backgroundTypes), _backgroundTypes, null);
		}

		return variable;
	}

	public override void _Ready()
	{
		InitConditions[] conditions =
			InitConditionsArray(_entangleWeight, _measureWeight, _superposeWeight, _teleportWeight);
		_worldAspectOnCanvas = SwitchOnBgType(_ratio.X / (1f * _ratio.Y),
			_webcamRatio.X / (1f * _webcamRatio.Y), _realImage[0].GetWidth() / (float)_realImage[0].GetHeight());

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
		MultipleImagesLooper looper = new(_duration, conditions, psteps, psteps, prewarm);

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
		var amt = Math.Max(_nbSpecies.Length, Math.Max(_backgroundTypes.Count, _ruleType.Count));
		InitConditions[] initConditionsArray = new InitConditions[amt];
		int canvasCount = -1;
		int imageCount = -1;
		for (int i = 0; i < amt; i++)
		{
			var rule = _ruleType[i % _ruleType.Count];
			Assert.IsTrue(rule != RulesSaved.Defaults.Default);
			int nbSpecy = _nbSpecies[i % _nbSpecies.Length];
			var rules = new RulesSaved(nbSpecy, rule);
			ATexProvider tex;
			IColorPicker colors;
			ISpecyPicker specyPicker;
			var bgType = _backgroundTypes[i % _backgroundTypes.Count];
			switch (bgType)
			{
				case BackgroundSource.Canvas:
					canvasCount++;
					var color = _backgroundColorForCanva[canvasCount % _backgroundColorForCanva.Length];
					tex = new CanvasPixels(WorldSize(_heightSize), color.A == 0f ? ColorPicker.Random() : color);
					var scheme = _colorSchemeForCanva[canvasCount % _colorSchemeForCanva.Length];
					Assert.IsTrue(scheme == null || (scheme.Colors != null && scheme.Colors.Length >= nbSpecy),
						"Color scheme has less colors than species, will fallback to a random scheme of the correct size.");
					colors = scheme?.Colors == null || scheme.Colors.Length < nbSpecy
						? ColorPicker.Random(nbSpecy)
						: ColorPicker.FromScheme(scheme.Colors);
					specyPicker = new UniformSpecyPicker(nbSpecy);
					break;
				case BackgroundSource.Webcam:
					_webcamFeed.Start();
					var quantizedWebcam = new QuantizedImage(_webcamFeed.Texture, nbSpecy);
					tex = quantizedWebcam;
					colors = quantizedWebcam;
					specyPicker = quantizedWebcam;
					break;
				case BackgroundSource.RealImage:
					imageCount++;
					var img = _realImage[imageCount % _realImage.Count];
					var quantizdBg = new QuantizedImage(img, nbSpecy);
					tex = quantizdBg;
					colors = quantizdBg;
					specyPicker = quantizdBg;
					break;
				default: throw new ArgumentOutOfRangeException();
			}

			initConditionsArray[i] = new InitConditions(tex, rules, colors,
				new Gates(_gateSize, new RandomGates(_nbGates, gatesWeights)), specyPicker);
		}


		return initConditionsArray;
	}

	public override void _Process(double delta)
	{
		Time.time += (float)delta;
		for (int i = 0; i < _tasks.Length; i++)
			_tasks[i] = _monos[i].Update();
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
