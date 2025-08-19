using Godot;
using System;

namespace UnityEngine
{
    public abstract partial class MonoBehaviour : GodotObject
    {
        public void _Ready()
        {
            Awake();
            Start();
        }
        public void _Process(float delta)
        {
            Time.deltaTime = delta;
            Update();
        }

        public virtual void Start(){}
        public virtual void Awake(){}
        public virtual void Update(){}
    }
    public class WaitForSeconds
    {
        private float _seconds;
        public WaitForSeconds(float seconds)
        {
            this._seconds = seconds;
        }
    }

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

    public static class Time
    {
        public static float deltaTime;
    }
    public class Random
    {
        public static float Range(float min, float max) =>
            (float)GD.RandRange(min, max);
    }

    namespace Assertions
    {
        public static class Assert
        {
            public static void IsTrue(bool condition, string message = null)
            {
                #if DEBUG
                if (!condition)
                    GD.PrintErr("Assertion failed: ", message ?? "Condition is false.");
                #endif
            }
            public static void IsFalse(bool condition, string message = null) => Assert.IsTrue(!condition, message);
        }
    }

    public static class Mathf
    {
        public static float Pow(float f, float p) => (float)Math.Pow(f, p);
        public static float Sqrt(float f) => (float)Math.Sqrt(f);
        public static int FloorToInt(float f) => (int)Math.Floor(f);
        public static float Atan2(float y, float x) => (float)Math.Atan2(x, y);
        public static float Cos(float f) => (float)Math.Cos(f);
        public static float Sin(float f) => (float)Math.Sin(f);
        public static float Deg2Rad => (float)(Math.PI / 180.0);
        public static float Rad2Deg => (float)(180.0 / Math.PI);
        public static float Repeat(float t, float length) => t - (float)Math.Floor(t / length) * length;
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
    public struct Vector2
    {
        private Godot.Vector2 _vector;
        public float magnitude => _vector.Length();
        public float sqrMagnitude => _vector.LengthSquared();
        public Vector2 normalized => _vector.Normalized();
        
        public static float Distance( Vector2 a, Vector2 b)
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
        public static Vector2 operator  -(Vector2 v)
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
    }

    public struct Color
    {
        private Godot.Color color;

        public Color(float r, float g, float b, float a = 1f)
        {
            color = new Godot.Color(r, g, b, a);
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