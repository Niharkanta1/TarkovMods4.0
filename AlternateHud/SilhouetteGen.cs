using System.IO;
using UnityEngine;

namespace AlternateHud
{
    /// <summary>Writes default per-body-part silhouette PNGs into a theme folder if they are missing.</summary>
    internal static class SilhouetteGen
    {
        public static readonly string[] Parts =
            { "Head", "Chest", "Stomach", "LeftArm", "RightArm", "LeftLeg", "RightLeg" };

        private const int W = 160, H = 360;
        private static readonly Vector2[] Chest =
        {
            new Vector2(70, 309), new Vector2(63, 303), new Vector2(46, 296),
            new Vector2(40, 284), new Vector2(48, 270), new Vector2(55, 245),
            new Vector2(65, 237), new Vector2(95, 237), new Vector2(105, 245),
            new Vector2(112, 270), new Vector2(120, 284), new Vector2(114, 296),
            new Vector2(97, 303), new Vector2(90, 309)
        };
        private static readonly Vector2[] Stomach =
        {
            new Vector2(64, 243), new Vector2(96, 243), new Vector2(101, 226),
            new Vector2(105, 202), new Vector2(99, 188), new Vector2(92, 180),
            new Vector2(68, 180), new Vector2(61, 188), new Vector2(55, 202),
            new Vector2(59, 226)
        };

        public static void EnsureMasks(string dir, string prefix)
        {
            Directory.CreateDirectory(dir);
            foreach (var part in Parts)
            {
                var path = Path.Combine(dir, prefix + "_" + part + ".png");
                if (File.Exists(path)) continue;
                SaveMask(path, p => Distance(part, p));
            }

            var silhouette = Path.Combine(dir, prefix + "_silhouette.png");
            if (!File.Exists(silhouette)) SaveMask(silhouette, BodyDistance);

            var outline = Path.Combine(dir, prefix + "_outline.png");
            if (!File.Exists(outline)) SaveMask(outline, p => Mathf.Abs(BodyDistance(p)) - 1.2f);
        }

        private static void SaveMask(string path, System.Func<Vector2, float> distance)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color[W * H];
            for (var y = 0; y < H; y++)
                for (var x = 0; x < W; x++)
                {
                    var d = distance(new Vector2(x + 0.5f, y + 0.5f));
                    px[y * W + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d));
                }

            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.Destroy(tex);
        }

        private static float BodyDistance(Vector2 p)
        {
            var d = float.MaxValue;
            foreach (var part in Parts)
                d = Mathf.Min(d, Distance(part, p));
            return d;
        }

        private static float Distance(string part, Vector2 p)
        {
            switch (part)
            {
                case "Head": return Capsule(p, new Vector2(80, 330), new Vector2(80, 330), 19);
                case "Chest": return Polygon(p, Chest);
                case "Stomach": return Polygon(p, Stomach);
                case "LeftArm": return Arm(p);
                case "RightArm": return Arm(new Vector2(W - p.x, p.y));
                case "LeftLeg": return Leg(p);
                default: return Leg(new Vector2(W - p.x, p.y));
            }
        }

        private static float Arm(Vector2 p)
        {
            return Mathf.Min(
                TaperedCapsule(p, new Vector2(43, 286), new Vector2(32, 248), 9, 7),
                TaperedCapsule(p, new Vector2(32, 248), new Vector2(25, 207), 7, 5),
                TaperedCapsule(p, new Vector2(25, 207), new Vector2(24, 184), 5, 4));
        }

        private static float Leg(Vector2 p)
        {
            return Mathf.Min(
                TaperedCapsule(p, new Vector2(69, 182), new Vector2(64, 132), 12, 9),
                TaperedCapsule(p, new Vector2(64, 132), new Vector2(61, 73), 9, 6),
                TaperedCapsule(p, new Vector2(61, 73), new Vector2(59, 24), 6, 5),
                TaperedCapsule(p, new Vector2(59, 24), new Vector2(70, 19), 5, 4));
        }

        private static float Capsule(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            var pa = p - a;
            var ba = b - a;
            var len = Vector2.Dot(ba, ba);
            var h = len > 0 ? Mathf.Clamp01(Vector2.Dot(pa, ba) / len) : 0f;
            return (pa - ba * h).magnitude - r;
        }

        private static float TaperedCapsule(Vector2 p, Vector2 a, Vector2 b, float ra, float rb)
        {
            var ab = b - a;
            var len = Vector2.Dot(ab, ab);
            var t = len > 0 ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len) : 0f;
            return Vector2.Distance(p, a + ab * t) - Mathf.Lerp(ra, rb, t);
        }

        private static float Polygon(Vector2 p, Vector2[] points)
        {
            var inside = false;
            var distance = float.MaxValue;
            for (var i = 0; i < points.Length; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % points.Length];
                var ab = b - a;
                var len = Vector2.Dot(ab, ab);
                var t = len > 0 ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len) : 0f;
                distance = Mathf.Min(distance, Vector2.Distance(p, a + ab * t));

                if ((a.y > p.y) != (b.y > p.y) &&
                    p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }

            return inside ? -distance : distance;
        }
    }
}
