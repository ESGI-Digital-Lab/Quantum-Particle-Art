using Godot;
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Object = UnityEngine.MonoBehaviour;

namespace UnityEngine
{
	public abstract partial class MonoBehaviour
	{
		private Node _node;

		public void SetNode(Node node)
		{
			_node = node;
		}

		protected T[] GetComponentsInChildren<T>(bool _ = false)
		{
			// This is a placeholder for the actual implementation
			// In Godot, you would typically use GetNode<T>() or GetParent().GetNode<T>()
			return _node.GetChildren().OfType<T>().ToArray();
		}


		public virtual async Task Start()
		{
		}

		public virtual async Task Awake()
		{
		}

		public virtual async Task Update()
		{
		}

		public virtual void Dispose()
		{
		}
	}

	public static class Debug
	{
		public static void Log(params object[] message)
		{
			GD.Print(string.Join(' ',message));
		}
		public static void Log(object message)
		{
			GD.Print(message);
		}

		public static void LogError(params object[] message)
		{
			GD.PrintErr(string.Join(' ',message));
		}

		public static void LogWarning(object message)
		{
			GD.Print("warning :" + message);
		}
	}

	public static class Time
	{
		private static float deltaTime;
		private static float _time;

		public static float time
		{
			get => _time;
			set
			{
				deltaTime = value - time;
				_time = value;
			}
		}
	}

	public struct Transform
	{
		private Godot.Transform3D _transform;

		public Transform(Godot.Transform3D transform)
		{
			_transform = transform;
		}

		public static implicit operator Godot.Transform3D(Transform transform)
		{
			return transform._transform;
		}

		public static implicit operator Transform(Godot.Transform3D transform)
		{
			return new Transform(transform);
		}
	}

	#region Sync

	public abstract class AsyncEnumerator
	{
		protected abstract bool IsFinished();

		public async Task WaitForFinished()
		{
			while (!IsFinished())
			{
				await Task.Delay(80);
			}
		}
	}

	public class WaitForSeconds : AsyncEnumerator
	{
		public static Task Delay(float seconds) => Task.Delay((int)(seconds * 1000));
		private float _seconds;
		private DateTime _start;

		public WaitForSeconds(float seconds)
		{
			this._seconds = seconds;
			this._start = DateTime.Now;
		}

		protected override bool IsFinished()
		{
			return DateTime.Now - _start >= TimeSpan.FromSeconds(_seconds);
		}
	}

	public class WaitUntil : AsyncEnumerator
	{
		private Func<bool> _condition;

		public WaitUntil(Func<bool> condition)
		{
			this._condition = condition;
		}

		protected override bool IsFinished()
		{
			var result = _condition.Invoke();
			return result;
		}
	}

	#endregion


	public partial class Texture : Godot.Texture
	{
	}

	public partial class Texture2D : Godot.Texture2D
	{
		public Texture2D(int width, int height)
		{
			GD.PrintErr("interfacing not handled");
		}
	}


	public class Random
	{
		public static float Range(float min, float max) =>
			(float)GD.RandRange(min, max);
		public static int Range(int min, int exclusiveMax) =>
			GD.RandRange(min, exclusiveMax-1);

		public static Color ColorHSV() => Color.ColorHSV(Range(0f, 1f), Range(0f, 1f), Range(0f, 1f));
	}

	namespace Assertions
	{
		public static class Assert
		{
			public static void IsTrue(bool condition, string message = null)
			{
				IsTrue(condition, () => message ?? "Condition is false.");
			}
			public static void IsTrue(bool condition, Func<string> message)
			{
#if DEBUG
				if (!condition)
					GD.PrintErr("Assertion failed: ", message.Invoke());
#endif
			}

			public static void IsFalse(bool condition, string message = null) => Assert.IsTrue(!condition, message);
			public static void IsNotNull(object obj, string message = null) => Assert.IsTrue(obj != null, message);
		}
	}

	public static class Mathf
	{
		public static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0; 
		public static float Pow(float f, float p) => (float)Math.Pow(f, p);
		public static float Sqrt(float f) => (float)Math.Sqrt(f);
		public static float Abs(float f) => (float)Math.Abs(f);
		public static int Abs(int f) => Math.Abs(f);
		public static int FloorToInt(float f) => (int)Math.Floor(f);
		public static int RoundToInt(float f) => (int)Math.Round(f);
		public static float Atan2(float y, float x) => (float)Math.Atan2(x, y);
		public static float Cos(float f) => (float)Math.Cos(f);
		public static float Sin(float f) => (float)Math.Sin(f);
		public static float Deg2Rad => (float)(Math.PI / 180.0);
		public static float Rad2Deg => (float)(180.0 / Math.PI);
		public static float Repeat(float t, float length) => t - (float)Math.Floor(t / length) * length;

		public static float InverseLerp(float a, float b, float value)
		{
			if (a == b) return 0f; // Avoid division by zero
			return (value - a) / (b - a);
		}
	}

	public struct Vector2Int
	{
		private Godot.Vector2I _vector;
		public Vector2Int(int x, int y) => _vector = new(x, y);

		public int x
		{
			get => _vector.X;
			set => _vector.X = value;
		}

		public int y
		{
			get => _vector.Y;
			set => _vector.Y = value;
		}
	}

	public struct Vector2 : IEquatable<Vector2>
	{
		private Godot.Vector2 _vector;
		public float magnitude => _vector.Length();
		public float sqrMagnitude => _vector.LengthSquared();
		public Vector2 normalized => _vector.Normalized();

		public static float Distance(Vector2 a, Vector2 b)
		{
			return a._vector.DistanceTo(b._vector);
		}

		public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
		{
			return a._vector.Lerp(b._vector, t);
		}

		public float x
		{
			get => _vector.X;
			set => _vector.X = value;
		}

		public float y
		{
			get => _vector.Y;
			set => _vector.Y = value;
		}

		public Vector2(float x, float y)
		{
			_vector = new Godot.Vector2(x, y);
		}

		public Vector2(Godot.Vector2 vector)
		{
			_vector = vector;
		}

		public static implicit operator Vector2(Godot.Vector2 vector)
		{
			return new Vector2(vector);
		}

		public static implicit operator Godot.Vector2(Vector2 vector)
		{
			return vector._vector;
		}

		public static Vector2 operator +(Vector2 a, Vector2 b)
		{
			return a._vector + b._vector;
		}

		public static Vector2 operator -(Vector2 v)
		{
			return -v._vector;
		}

		public static Vector2 operator -(Vector2 a, Vector2 b)
		{
			return a._vector - b._vector;
		}

		public static Vector2 operator *(Vector2 a, Vector2 b)
		{
			return a._vector * b._vector;
		}

		public static Vector2 operator /(Vector2 a, Vector2 b)
		{
			return a._vector / b._vector;
		}

		public static Vector2 operator /(Vector2 a, float b)
		{
			return a._vector / b;
		}

		public static Vector2 operator *(Vector2 a, float f)
		{
			return a._vector * f;
		}

		public static Vector2 zero = new Vector2(0, 0);
		public static Vector2 one = new Vector2(1, 1);
		public override string ToString() => $"({x}, {y})";

		public bool Equals(Vector2 other)
		{
			return _vector.Equals(other._vector);
		}

		public override bool Equals(object obj)
		{
			return obj is Vector2 other && Equals(other);
		}

		public override int GetHashCode()
		{
			return _vector.GetHashCode();
		}
	}

	public struct Vector3
	{
		private Godot.Vector3 _vector;

		public float x
		{
			get => _vector.X;
			set => _vector.X = value;
		}

		public float y
		{
			get => _vector.Y;
			set => _vector.Y = value;
		}

		public float z
		{
			get => _vector.Z;
			set => _vector.Z = value;
		}

		public Vector3(float x, float y, float z)
		{
			_vector = new Godot.Vector3(x, y, z);
		}
	}

	public struct Color
	{
		private Godot.Color color;
		public static Godot.Color clear => new Color(0,0,0,0);
		public static Color black => new Color(0, 0, 0,1);
		public static Color gray => new Color(0.5f, 0.5f, 0.5f);
		public static Color white => new Color(1, 1, 1);
		public static Color red => new Color(1, 0, 0);
		public static Color green => new Color(0, 1, 0);
		public static Color blue => new Color(0, 0, 1);
		public static Color yellow => new Color(1, 1, 0);

		public float r
		{
			get => color.R;
			set => color.R = value;
		}

		public float g
		{
			get => color.G;
			set => color.G = value;
		}

		public float b
		{
			get => color.B;
			set => color.B = value;
		}
		public float a
		{
			get => color.A;
			set => color.A = value;
		}

		public static Color Lerp(Color a, Color b, float t) => new Color(a.color.Lerp(b.color, t));

		public Color(float r, float g, float b, float a = 1f)
		{
			color = new Godot.Color(r, g, b, a);
		}

		public Color(Godot.Color color)
		{
			this.color = color;
		}
		public static implicit operator Godot.Color(Color color)
		{
			return color.color;
		}
		public static implicit operator Color(Godot.Color color)
		{
			return new Color(color);
		}

		public static Color ColorHSV(float h, float s, float v, float a = 1f)
		{
			// Godot's Color constructor uses RGB, so we need to convert HSV to RGB
			return new Color(Godot.Color.FromHsv(h, s, v, a));
		}
	}

	public struct Color32
	{
		private Color color;

		public Color32(byte r, byte g, byte b, byte a = 255)
		{
			color = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
		}

		public Color32(Color color)
		{
			this.color = color;
		}

		public static implicit operator Color32(Color color)
		{
			return new Color32(color);
		}

		public static implicit operator Color(Color32 color32)
		{
			return color32.color;
		}
	}

	#region Serialization

	namespace Serialization
	{
	}

	public class ScriptableObject
	{
		
	}

	public class CreateAssetMenuAttribute : Attribute
	{
		public string fileName;
		public string menuName;
		public int order;

		public CreateAssetMenuAttribute(string fileName = "", string menuName = "", int order = 0)
		{
			this.fileName = fileName;
			this.menuName = menuName;
			this.order = order;
		}
	}

	public class SerializeFieldAttribute : Attribute
	{
	}

	public class HeaderAttribute : Attribute
	{
		public HeaderAttribute(string header)
		{
		}
	}

	public class RangeAttribute : Attribute
	{
		public RangeAttribute(float min, float max)
		{
		}

		public RangeAttribute(int min, int max)
		{
		}
	}

	public class TooltipAttribute : Attribute
	{
		public TooltipAttribute(string tooltip)
		{
		}
	}

	public class PropertyAttribute : Attribute
	{
	}

	#endregion
}

namespace DefaultNamespace.Tools
{
}
