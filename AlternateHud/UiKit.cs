using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AlternateHud
{
    internal static class UiKit
    {
        public static Theme Theme;
        private static Sprite _white;
        private static Font _font;
        private static string _fontName;

        public static Sprite White
        {
            get
            {
                if (_white) return _white;
                var t = new Texture2D(4, 4, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                var px = new Color[16];
                for (var i = 0; i < 16; i++) px[i] = Color.white;
                t.SetPixels(px);
                t.Apply();
                _white = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
                _white.hideFlags = HideFlags.HideAndDontSave;
                return _white;
            }
        }

        public static Font GetFont()
        {
            var name = Theme?.FontName ?? "Bender";
            if (_font && _fontName == name) return _font;

            Font f = null;
            if (name.ToLowerInvariant().Contains("bender"))
            {
                var tmp = Resources.Load<TMP_FontAsset>("ui/fonts/jovanny lemonad - bender normal sdf");
                if (tmp != null) f = tmp.sourceFontFile;
            }

            if (!f)
            {
                foreach (var candidate in Resources.FindObjectsOfTypeAll<Font>())
                {
                    if (candidate && candidate.name.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                    {
                        f = candidate;
                        break;
                    }
                }
            }

            if (!f) f = Font.CreateDynamicFontFromOSFont(new[] { name, "Bahnschrift", "Arial" }, 32);
            AlternateHudPlugin.Log.LogInfo("Font '" + name + "' -> " + (f ? f.name : "none"));
            _font = f;
            _fontName = name;
            return f;
        }

        public static RectTransform Node(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos,
            Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Img(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor,
            Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var rt = Node(parent, name, anchor, pivot, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color,
            TextAnchor align, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 box)
        {
            var rt = Node(parent, name, anchor, pivot, pos, box);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = GetFont();
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.text = text;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (Theme == null || Theme.Shadow)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0, 0, 0, 0.9f);
                s.effectDistance = new Vector2(1f, -1f);
            }

            return t;
        }

        public static TextAnchor AlignFor(float ax, float ay = 0.5f)
        {
            return ax < 0.25f ? TextAnchor.MiddleLeft : ax > 0.75f ? TextAnchor.MiddleRight : TextAnchor.MiddleCenter;
        }
    }
}
