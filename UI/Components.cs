using System;
using System.Collections.Generic;
using UnityEngine;
using EsportsTrainer.Core;

namespace EsportsTrainer.UI
{
    /// <summary>
    /// UI/Components.cs — 组件库（游戏原生拟态版）。
    /// 斜切按钮 / 斜切进度条 / 渐变分割线；配色一律走 Theme 样式与 backgroundColor save/restore。
    /// 自绘控件 GetRect 必须显式 Width/Height 约束。
    /// </summary>
    public static class Components
    {
        // ================= 斜切按钮 =================
        private static bool SlantButton(string text, GUIStyle style, params GUILayoutOption[] o)
        {
            // 估宽放最前：调用方传入的 Width 会覆盖它；只传 Height 的调用不再被拉伸成整行
            var opts = new List<GUILayoutOption>
            {
                GUILayout.Width(Mathf.Max(44, text.Length * 15f + 30f) * Theme.Scale)
            };
            opts.AddRange(o);
            if (style != null && style.fixedHeight > 0)
                opts.Add(GUILayout.Height(style.fixedHeight));
            var rect = GUILayoutUtility.GetRect(0, 10000, opts.ToArray());
            if (rect.width <= 1) return false;
            return GUI.Button(rect, text, style);
        }

        /// <summary>主按钮（金色渐变斜切）。</summary>
        public static bool PrimaryButton(string text, Theme.AccentColor accent, params GUILayoutOption[] o)
        {
            var style = accent == Theme.AccentColor.Yellow ? Theme.GoldBtn : Theme.StyleFor(accent);
            return SlantButton(text, style, o);
        }

        /// <summary>次级按钮（辅色渐变斜切）。</summary>
        public static bool HexButton(string text, params GUILayoutOption[] o)
            => SlantButton(text, Theme.HexBtn, o);

        /// <summary>以指定样式渲染斜切按钮（用于选中态切换等）。</summary>
        public static bool StyleButton(string text, GUIStyle style, params GUILayoutOption[] o)
            => SlantButton(text, style, o);

        /// <summary>幽灵按钮（半透明斜切）。</summary>
        public static bool GhostButton(string text, params GUILayoutOption[] o)
            => SlantButton(text, Theme.GhostBtn, o);

        /// <summary>高危操作：二次确认（3 秒内再次点击执行，超时解除）。</summary>
        private static readonly Dictionary<string, (bool armed, float time)> _arm = new Dictionary<string, (bool, float)>();
        public static bool DangerButton(string key, string text, Action action, params GUILayoutOption[] o)
        {
            _arm.TryGetValue(key, out var st);
            bool armed = st.armed && (Time.realtimeSinceStartup - st.time) < 3f;
            if (st.armed && !armed) _arm[key] = (false, 0f);

            bool hit = SlantButton(armed ? "⚠ " + L10n.T("确认") + ": " + text : text, armed ? Theme.DangerBtn : Theme.GhostBtn, o);
            if (hit)
            {
                if (armed) { _arm[key] = (false, 0f); action?.Invoke(); return true; }
                _arm[key] = (true, Time.realtimeSinceStartup);
            }
            return false;
        }

        // ================= 开关 =================
        public static bool ToggleSwitch(bool value, string label)
        {
            using (new GUILayout.HorizontalScope())
            {
                if (!string.IsNullOrEmpty(label))
                    GUILayout.Label(label, GUILayout.Width(150 * Theme.Scale));
                var rect = GUILayoutUtility.GetRect(46 * Theme.Scale, 20 * Theme.Scale,
                    GUILayout.Width(46 * Theme.Scale), GUILayout.Height(20 * Theme.Scale));
                var on = Theme.HGrad(Theme.Cur.Primary, Theme.Cur.Secondary);
                var off = Theme.Solid(new Color(1f, 1f, 1f, 0.12f));
                GUI.DrawTexture(rect, value ? on : off);
                float knobW = 14 * Theme.Scale;
                var knob = new Rect(rect.x + (value ? rect.width - knobW - 2 : 2),
                                    rect.y + 2, knobW, rect.height - 4);
                var prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, value ? 1f : 0.7f);
                GUI.DrawTexture(knob, Theme.Solid(Color.white));
                GUI.color = prev;
                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                { value = !value; Event.current.Use(); }
                return value;
            }
        }

        // ================= 文本 =================
        public static void SectionTitle(string title, Theme.AccentColor accent)
        {
            GUILayout.Space(4 * Theme.Scale);
            using (new GUILayout.HorizontalScope())
            {
                // 强调色块
                var barRect = GUILayoutUtility.GetRect(14 * Theme.Scale, 16 * Theme.Scale,
                    GUILayout.Width(14 * Theme.Scale), GUILayout.Height(16 * Theme.Scale));
                GUI.DrawTexture(barRect, Theme.HGrad(Theme.Cur.Primary, Theme.Cur.Secondary));
                GUILayout.Space(2);
                var prev = GUI.contentColor;
                GUI.contentColor = Theme.TextPrimary;
                var st = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.BoldAndItalic };
                GUILayout.Label(title, st);
                GUI.contentColor = prev;
                GUILayout.Label("", GUILayout.ExpandWidth(true));
                var lineRect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true), GUILayout.Height(2));
                GUI.DrawTexture(lineRect, Theme.HGrad(
                    new Color(Theme.Cur.Primary.r, Theme.Cur.Primary.g, Theme.Cur.Primary.b, 0.7f),
                    new Color(Theme.Cur.Primary.r, Theme.Cur.Primary.g, Theme.Cur.Primary.b, 0f)));
            }
            GUILayout.Space(2 * Theme.Scale);
        }

        public static void KeyValue(string key, string value)
        {
            using (new GUILayout.HorizontalScope())
            {
                var prev = GUI.contentColor;
                GUI.contentColor = Theme.TextSecondary;
                GUILayout.Label(key, GUILayout.Width(120 * Theme.Scale));
                GUI.contentColor = prev;
                GUILayout.Label(value);
            }
        }

        public static void Hint(string text)
        {
            var prev = GUI.contentColor;
            GUI.contentColor = new Color(Theme.TextSecondary.r, Theme.TextSecondary.g, Theme.TextSecondary.b, 0.8f);
            GUILayout.Label(text);
            GUI.contentColor = prev;
        }

        // ================= 进度条（直角渐变） =================
        public static void Bar(float frac)
        {
            var rect = GUILayoutUtility.GetRect(0, 10000, 10, 10,
                GUILayout.Height(10 * Theme.Scale), GUILayout.ExpandWidth(true));
            GUI.DrawTexture(rect, Theme.Solid(new Color(0f, 0f, 0f, 0.45f)));
            if (frac > 0.01f)
            {
                var fill = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(frac), rect.height);
                GUI.DrawTexture(fill, Theme.HGrad(Theme.Cur.Secondary, Theme.Cur.Primary));
            }
        }

        // ================= 输入行 =================
        public static void ValueRow(string label, string value, ref string input, Action apply, string btnText = "设置")
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(70 * Theme.Scale));
                var prev = GUI.contentColor;
                GUI.contentColor = Theme.TextSecondary;
                GUILayout.Label(value, GUILayout.Width(110 * Theme.Scale));
                GUI.contentColor = prev;
                input = GUILayout.TextField(input, GUILayout.Width(110 * Theme.Scale));
                if (GUILayout.Button(btnText, GUILayout.Width(46 * Theme.Scale))) apply?.Invoke();
                GUILayout.Label("", GUILayout.ExpandWidth(true));
            }
        }

        public static string TextField(string label, string value, float width)
        {
            using (new GUILayout.HorizontalScope())
            {
                if (!string.IsNullOrEmpty(label)) GUILayout.Label(label, GUILayout.Width(70 * Theme.Scale));
                return GUILayout.TextField(value, GUILayout.Width(width * Theme.Scale));
            }
        }

        // ================= 分页 =================
        public static void Pagination(ref int page, int total, int pageSize, out int start, out int end)
        {
            int totalPages = Mathf.Max(1, (total + pageSize - 1) / pageSize);
            page = Mathf.Clamp(page, 0, totalPages - 1);
            start = page * pageSize;
            end = Mathf.Min(start + pageSize, total);

            using (new GUILayout.HorizontalScope())
            {
                if (GhostButton("◀", GUILayout.Width(30 * Theme.Scale))) page = Mathf.Max(0, page - 1);
                var prev = GUI.contentColor;
                GUI.contentColor = Theme.TextSecondary;
                GUILayout.Label($"{page + 1}/{totalPages}  ({L10n.T("共")} {total})", GUILayout.ExpandWidth(true));
                GUI.contentColor = prev;
                if (GhostButton("▶", GUILayout.Width(30 * Theme.Scale))) page = Mathf.Min(totalPages - 1, page + 1);
            }
        }

        // ================= 子导航（斜切 Tab） =================
        public static int SubNav(int sel, string[] names)
        {
            using (new GUILayout.HorizontalScope())
            {
                foreach (var n in names)
                {
                    int i = System.Array.IndexOf(names, n);
                    bool active = i == sel;
                    var style = active ? Theme.NavOn : Theme.NavOff;
                    if (SlantButton(n, style, GUILayout.Width(Mathf.Max(56, n.Length * 16) * Theme.Scale)))
                        sel = i;
                }
                GUILayout.Label("", GUILayout.ExpandWidth(true));
            }
            return sel;
        }

        // ================= 选择行 =================
        public static int PopupRow(string label, int sel, string[] options, float labelWidth = 70f)
        {
            using (new GUILayout.HorizontalScope())
            {
                if (!string.IsNullOrEmpty(label)) GUILayout.Label(label, GUILayout.Width(labelWidth * Theme.Scale));
                return GUILayout.SelectionGrid(sel, options, options.Length, GUILayout.ExpandWidth(false));
            }
        }

        // ================= 滑杆行 =================
        public static void SliderRow(string label, float cur, float min, float max, Action<float> onChange, Action onMax = null, float labelW = 50f)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(labelW * Theme.Scale));
                float v = GUILayout.HorizontalSlider(cur, min, max, GUILayout.Width(150 * Theme.Scale));
                GUILayout.Label(((int)v).ToString(), GUILayout.Width(30 * Theme.Scale));
                if (onMax != null && GUILayout.Button(L10n.T("最大"), GUILayout.Width(46 * Theme.Scale))) onMax();
                else if ((int)v != (int)cur) onChange?.Invoke((int)v);
                GUILayout.Label("", GUILayout.ExpandWidth(true));
            }
        }
    }
}
