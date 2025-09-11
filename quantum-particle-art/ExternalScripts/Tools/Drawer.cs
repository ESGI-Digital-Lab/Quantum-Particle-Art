using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace DefaultNamespace.Tools
{
    public class Drawer
    {
        public struct Line
        {
            private Vector2 _start;
            public Vector2 Start => _start;
            private Vector2 _end;
            private float _relativeWidth = 1f;
            public Vector2 End => _end;

            public Color Color => _color;

            public float RelativeWidth
            {
                get => _relativeWidth;
                set => _relativeWidth = value;
            }

            private Color _color;

            public Line(Vector2 start, Vector2 end, Color color, float relativeWidthRatioo)
            {
                _start = start;
                _end = end;
                _color = color;
                _relativeWidth = relativeWidthRatioo;
                Assert.IsTrue(_relativeWidth>=0, "Width ratio must be positive");
            }

            public IEnumerable<Vector2> Sample(int resolution)
            {
                for (int i = 0; i <= resolution; i++)
                {
                    float t = (float)i / resolution;
                    yield return Vector2.Lerp(_start, _end, t);
                }
            }

            private static readonly List<Vector2Int> cache = new(); 
            public static IEnumerable<Vector2Int> GetPixels(Vector2Int s, Vector2Int e)
            {
                cache.Clear();
                int x0 = Mathf.RoundToInt(s.x);
                int y0 = Mathf.RoundToInt(s.y);
                int x1 = Mathf.RoundToInt(e.x);
                int y1 = Mathf.RoundToInt(e.y);

                int dx = Mathf.Abs(x1 - x0);
                int dy = Mathf.Abs(y1 - y0);
                int sx = x0 < x1 ? 1 : -1;
                int sy = y0 < y1 ? 1 : -1;
                int err = dx - dy;

                while (true)
                {
                    cache.Add(new Vector2Int(x0, y0));

                    if (x0 == x1 && y0 == y1)
                        break;

                    int e2 = 2 * err;
                    if (e2 > -dy)
                    {
                        err -= dy;
                        x0 += sx;
                    }

                    if (e2 < dx)
                    {
                        err += dx;
                        y0 += sy;
                    }
                }

                return cache;
            }
        }

        private List<Line> _lines = new List<Line>();
        private IEnumerable<Line> Lines => _lines;

        public Drawer()
        {
            _lines = new List<Line>();
        }

        public void AddLine(Vector2 start, Vector2 end, Color color, float relativeWidthRatio = 1f)
        {
            _lines.Add(new Line(start, end, color, relativeWidthRatio));
        }
        public IEnumerable<Line> GetLines()
        {
            return _lines;
        }

        public void Clear()
        {
            _lines.Clear();
        }
    }
}