using System.Collections.Generic;
using UnityEngine;

namespace EsportsTrainer.Core
{
    /// <summary>
    /// Core/Theme.cs — 「游戏原生拟态」主题单元 v3。
    /// · 3 套预设配色（紫电 / 赤焰 / 苍翠）+ 全自定义（hex 覆盖）
    /// · 运行时生成实心小长方形 / 渐变纹理；三态（normal/hover/active）用主题主色区分
    /// · 样式集中于此，渲染处禁止 new GUIStyle
    /// </summary>
    public static class Theme
    {
        // ================= 配色方案 =================
        public class Scheme
        {
            public string Name;
            public Color Primary;    // 主强调（渐变起点/标题/分割线）
            public Color Secondary;  // 次强调（渐变终点/图标）
            public Color Gold;       // 主按钮（金）
            public Color GoldText;   // 金按钮文字
            public Color BgTop;      // 窗口渐变顶
            public Color BgBottom;   // 窗口渐变底
            public Color Border;     // 描边/分割
            public Color TextMain;
            public Color TextSub;

            public Scheme(string name, string pri, string sec, string gold, string bgT, string bgB)
            {
                Name = name;
                Primary = Hex(pri);
                Secondary = Hex(sec);
                Gold = Hex(gold);
                GoldText = Hex("#3A2400");
                BgTop = Hex(bgT);
                BgBottom = Hex(bgB);
                Border = Color.Lerp(Primary, Color.black, 0.45f);
                TextMain = new Color(0.93f, 0.91f, 0.98f);
                TextSub = new Color(0.72f, 0.68f, 0.85f);
            }

            private static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.magenta;
        }

        public static readonly Scheme[] Presets =
        {
            new Scheme("紫电", "#8A5CFF", "#00C2FF", "#FFCF5C", "#2A1F52", "#12101F"),
            new Scheme("赤焰", "#FF5470", "#FF9A3D", "#FFD166", "#4A1B33", "#170F14"),
            new Scheme("苍翠", "#2DD4A7", "#38BDF8", "#FDE047", "#123B33", "#0C1418"),
        };

        private static int _schemeIndex;
        private static Color? _customPrimary, _customSecondary, _customGold;

        private static Scheme _cur;
        public static Scheme Cur => _cur;

        public static void Apply(int schemeIndex, string customPrimary, string customSecondary, string customGold)
        {
            _schemeIndex = Mathf.Clamp(schemeIndex, 0, Presets.Length - 1);
            _customPrimary = ParseHex(customPrimary);
            _customSecondary = ParseHex(customSecondary);
            _customGold = ParseHex(customGold);
            RebuildScheme();
        }

        public static void SetScheme(int index)
        {
            _schemeIndex = Mathf.Clamp(index, 0, Presets.Length - 1);
            _customPrimary = _customSecondary = _customGold = null;
            RebuildScheme();
        }

        public static void SetCustom(string which, string hex)
        {
            var c = ParseHex(hex);
            if (!c.HasValue) return;
            if (which == "primary") _customPrimary = c;
            else if (which == "secondary") _customSecondary = c;
            else if (which == "gold") _customGold = c;
            RebuildScheme();
        }

        private static void RebuildScheme()
        {
            var baseS = Presets[_schemeIndex];
            var s = new Scheme(baseS.Name, "#000000", "#000000", "#000000", ToHex(baseS.BgTop), ToHex(baseS.BgBottom))
            {
                Primary = _customPrimary ?? baseS.Primary,
                Secondary = _customSecondary ?? baseS.Secondary,
                Gold = _customGold ?? baseS.Gold,
                GoldText = baseS.GoldText,
                BgTop = baseS.BgTop,
                BgBottom = baseS.BgBottom,
            };
            s.Border = Color.Lerp(s.Primary, Color.black, 0.45f);
            s.TextMain = baseS.TextMain;
            s.TextSub = baseS.TextSub;
            s.Name = _customPrimary != null || _customSecondary != null || _customGold != null
                ? baseS.Name + "*" : baseS.Name;
            _cur = s;
            _stylesNeedBuild = true;
        }

        private static Color? ParseHex(string h)
            => !string.IsNullOrEmpty(h) && ColorUtility.TryParseHtmlString(h.StartsWith("#") ? h : "#" + h, out var c) ? c : (Color?)null;

        private static string ToHex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        // ================= 兼容旧 API =================
        public enum AccentColor { Red, Yellow, Green, Blue }
        public static Color AccentOf(AccentColor c) => c switch
        {
            AccentColor.Yellow => Cur.Gold,
            AccentColor.Blue => Cur.Secondary,
            AccentColor.Green => new Color(0.33f, 0.85f, 0.55f),
            AccentColor.Red => new Color(0.95f, 0.36f, 0.45f),
            _ => Cur.Primary
        };
        public static void SetAccent(AccentColor c) { _accent = c; _stylesNeedBuild = true; }
        private static AccentColor _accent = AccentColor.Yellow;
        public static AccentColor CurrentAccent => _accent;
        public static Color AccentNormal => AccentOf(_accent);

        public static float Scale { get; set; } = 1f;
        public static Color TextPrimary => Cur.TextMain;
        public static Color TextSecondary => Cur.TextSub;
        public static Color Surface => new Color(1f, 1f, 1f, 0.06f);
        public static Color Divider => new Color(1f, 1f, 1f, 0.14f);
        public static bool IsDark => true;

        // ================= 尺寸 =================
        private static float _scale = 1f;
        public static void SetScale(float v) { Scale = Mathf.Clamp(v, 0.7f, 1.4f); _stylesNeedBuild = true; }

        // ================= 纹理烘焙 =================
        private static readonly Dictionary<string, Texture2D> _tex = new Dictionary<string, Texture2D>();

        private static Texture2D Cached(string key, System.Func<Texture2D> make)
        {
            if (_tex.TryGetValue(key, out var t) && t) return t;
            t = make();
            _tex[key] = t;
            return t;
        }

        private static Color Soften(Color c, float mul, float add = 0f)
            => new Color(Mathf.Clamp01(c.r * mul + add), Mathf.Clamp01(c.g * mul + add), Mathf.Clamp01(c.b * mul + add), c.a);

        /// <summary>主强调色半透明染色（hover/active 状态用）。</summary>
        private static Color Tint(float a) => new Color(Cur.Primary.r, Cur.Primary.g, Cur.Primary.b, a);

        /// <summary>小长方形（纯色）。</summary>
        public static Texture2D Slant(Color c)
        {
            string key = $"R{ToHex(c)}{(int)(c.a * 100)}";
            return Cached(key, () => MakeRect(Soften(c, 1f), Soften(c, 1f)));
        }

        /// <summary>小长方形（垂直渐变：上亮下深）。</summary>
        public static Texture2D SlantGrad(Color top, Color bottom)
        {
            string key = $"RG{ToHex(top)}{ToHex(bottom)}";
            return Cached(key, () => MakeRect(Soften(top, 1.18f), bottom));
        }

        /// <summary>小长方形（水平渐变）。</summary>
        public static Texture2D SlantHGrad(Color a, Color b)
        {
            string key = $"RH{ToHex(a)}{ToHex(b)}";
            return Cached(key, () => MakeRectH(a, b));
        }

        public static Texture2D HGrad(Color a, Color b)
        {
            string key = $"H{ToHex(a)}{(int)(a.a * 100)}{ToHex(b)}";
            return Cached(key, () =>
            {
                int w = 64;
                var t = new Texture2D(w, 1, TextureFormat.RGBA32, false);
                var px = new Color[w];
                for (int x = 0; x < w; x++) px[x] = Color.Lerp(a, b, x / (float)(w - 1));
                t.SetPixels(px);
                t.Apply();
                return t;
            });
        }

        public static Texture2D VGrad(Color a, Color b)
        {
            string key = $"V{ToHex(a)}{ToHex(b)}";
            return Cached(key, () =>
            {
                int h = 64;
                var t = new Texture2D(1, h, TextureFormat.RGBA32, false);
                var px = new Color[h];
                for (int y = 0; y < h; y++) px[y] = Color.Lerp(a, b, y / (float)(h - 1));
                t.SetPixels(px);
                t.Apply();
                return t;
            });
        }

        /// <summary>实心小长方形（垂直渐变）。</summary>
        private static Texture2D MakeRect(Color top, Color bottom)
        {
            int w = 16, h = 16;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                Color c = Color.Lerp(top, bottom, y / (float)(h - 1));
                for (int x = 0; x < w; x++) px[y * w + x] = c;
            }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        /// <summary>实心小长方形（水平渐变）。</summary>
        private static Texture2D MakeRectH(Color a, Color b)
        {
            int w = 64;
            var t = new Texture2D(w, 1, TextureFormat.RGBA32, false);
            var px = new Color[w];
            for (int x = 0; x < w; x++) px[x] = Color.Lerp(a, b, x / (float)(w - 1));
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        /// <summary>1×1 纯色纹理缓存（分割线等）。</summary>
        private static readonly Dictionary<Color, Texture2D> _solid = new Dictionary<Color, Texture2D>();
        public static Texture2D Solid(Color c)
        {
            if (_solid.TryGetValue(c, out var t) && t) return t;
            t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, c);
            t.Apply();
            _solid[c] = t;
            return t;
        }

        // ================= GUISkin / 样式 =================
        private static GUISkin _skin;
        private static bool _stylesNeedBuild = true;
        public static GUIStyle NavOff, NavOn, GoldBtn, GhostBtn, HexBtn, DangerBtn;

        public static void ApplySkin()
        {
            if (_skin == null || _stylesNeedBuild)
            {
                _skin = BuildSkin();
                BuildStyles(_skin);
                _stylesNeedBuild = false;
            }
            GUI.skin = _skin;
        }

        private static GUISkin BuildSkin()
        {
            var s = Cur;
            var skin = Object.Instantiate(GUI.skin);

            // 窗口：垂直渐变背景 + 斜体标题
            skin.window = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset(12, 12, 8, 8),
                border = new RectOffset(0, 0, 0, 0),
                fontSize = Mathf.RoundToInt(15 * Scale),
                fontStyle = FontStyle.BoldAndItalic,
                alignment = TextAnchor.UpperLeft
            };
            skin.window.normal.background = VGrad(s.BgTop, s.BgBottom);
            skin.window.normal.textColor = s.TextMain;
            skin.window.onNormal = skin.window.normal;
            skin.window.onHover = skin.window.normal;
            skin.window.onActive = skin.window.normal;
            skin.window.onFocused = skin.window.normal;
            skin.window.onFocused.textColor = s.TextMain;

            // 通用按钮（Tab 内小按钮）：三态 = 暗面 / 主色 hover / 主色深 active
            skin.button = SlantStyle(Slant(new Color(1f, 1f, 1f, 0.09f)), Slant(Tint(0.30f)),
                Slant(Tint(0.18f)), s.TextMain, 12, FontStyle.Normal, 24);

            skin.label = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(13 * Scale),
                normal = { textColor = s.TextMain }
            };
            skin.textField = new GUIStyle(GUI.skin.textField)
            {
                fontSize = Mathf.RoundToInt(13 * Scale),
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 3, 3)
            };
            skin.textField.normal.background = Solid(new Color(0f, 0f, 0f, 0.35f));
            skin.textField.focused.background = Solid(new Color(0f, 0f, 0f, 0.5f));
            skin.textField.normal.textColor = s.TextMain;
            skin.textField.focused.textColor = Color.white;
            return skin;
        }

        private static void BuildStyles(GUISkin skin)
        {
            var s = Cur;
            NavOff = SlantStyle(Slant(new Color(1f, 1f, 1f, 0.07f)), Slant(Tint(0.30f)),
                Slant(Tint(0.16f)), s.TextSub, 13, FontStyle.BoldAndItalic, 30);
            NavOn = SlantStyle(SlantGrad(s.Primary, Color.Lerp(s.Primary, s.Secondary, 0.6f)),
                SlantGrad(Soften(s.Primary, 1.15f), s.Secondary), NavOn_active(), Color.white, 13, FontStyle.BoldAndItalic, 30);
            GoldBtn = SlantStyle(SlantGrad(s.Gold, Color.Lerp(s.Gold, Color.black, 0.25f)),
                SlantGrad(Soften(s.Gold, 1.1f), Color.Lerp(s.Gold, Color.black, 0.15f)),
                SlantGrad(s.Gold, Color.Lerp(s.Gold, Color.black, 0.4f)), s.GoldText, 13, FontStyle.BoldAndItalic, 28);
            HexBtn = SlantStyle(SlantGrad(Soften(s.Secondary, 1.15f), s.Secondary),
                SlantGrad(Soften(s.Secondary, 1.25f), Soften(s.Secondary, 1.1f)), SlantGrad(s.Secondary, Color.Lerp(s.Secondary, Color.black, 0.3f)),
                new Color(0.02f, 0.10f, 0.17f), 13, FontStyle.BoldAndItalic, 28);
            GhostBtn = SlantStyle(Slant(new Color(1f, 1f, 1f, 0.05f)), Slant(Tint(0.22f)),
                Slant(Tint(0.12f)), s.TextSub, 12, FontStyle.Normal, 24);
            DangerBtn = SlantStyle(SlantGrad(new Color(0.95f, 0.36f, 0.45f), new Color(0.62f, 0.12f, 0.20f)),
                SlantGrad(new Color(1f, 0.45f, 0.52f), new Color(0.72f, 0.16f, 0.24f)),
                SlantGrad(new Color(0.8f, 0.28f, 0.36f), new Color(0.45f, 0.08f, 0.14f)), Color.white, 12, FontStyle.Bold, 24);
        }

        private static Texture2D NavOn_active()
        {
            var s = Cur;
            return SlantGrad(Color.Lerp(s.Primary, Color.black, 0.2f), Color.Lerp(s.Secondary, Color.black, 0.35f));
        }

        /// <summary>长方形按钮样式（normal/hover/active 三态主题色）。</summary>
        private static GUIStyle SlantStyle(Texture2D normal, Texture2D hover, Texture2D active, Color text, int fontSize, FontStyle fs, float height)
        {
            var g = new GUIStyle
            {
                fontSize = Mathf.RoundToInt(fontSize * Scale),
                fontStyle = fs,
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(6, 6, 0, 0),   // 9-slice：实心纹理，任意拉伸不变形
                padding = new RectOffset(10, 10, 0, 0),
                fixedHeight = Mathf.RoundToInt(height * Scale)
            };
            g.normal.background = normal;
            g.hover.background = hover;
            g.active.background = active;
            g.normal.textColor = text;
            g.hover.textColor = text;
            g.active.textColor = text;
            return g;
        }

        public static GUIStyle StyleFor(AccentColor c) => c switch
        {
            AccentColor.Yellow => GoldBtn,
            AccentColor.Blue => HexBtn,
            AccentColor.Green => GreenBtn(),
            AccentColor.Red => DangerBtn,
            _ => GhostBtn
        };

        private static GUIStyle _greenBtn;
        private static GUIStyle GreenBtn()
        {
            if (_greenBtn != null && !_stylesNeedBuild) return _greenBtn;
            var g = new Color(0.33f, 0.85f, 0.55f);
            _greenBtn = SlantStyle(SlantGrad(Soften(g, 1.15f), Color.Lerp(g, Color.black, 0.25f)),
                SlantGrad(Soften(g, 1.25f), g), SlantGrad(g, Color.Lerp(g, Color.black, 0.4f)),
                new Color(0.02f, 0.15f, 0.08f), 13, FontStyle.BoldAndItalic, 28);
            return _greenBtn;
        }
    }
}
