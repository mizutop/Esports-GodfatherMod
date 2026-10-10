using UnityEngine;
using EsportsTrainer.Core;
using EsportsTrainer.Features;

namespace EsportsTrainer.UI
{
    /// <summary>
    /// UI/ModMenu.cs — 主窗口（游戏原生拟态版）。
    /// 渐变窗口底 + 斜体标题 + 顶栏渐变饰带 + 斜切 Tab 导航 + 配色系统（3 预设 / hex 自定义）。
    /// 渲染熔断 + 拖动 + 署名三件套。
    /// </summary>
    public static class ModMenu
    {
        private static bool _show;
        private static Vector2 _scroll;
        private static int _tabIndex;
        private static int _frameErrors;
        private static float _skipUntil;
        private static bool _showColorBar;
        private static string _hexP = "", _hexS = "", _hexG = "";

        private static readonly ITab[] Tabs =
        {
            new ClubTab(),
            new AthleteTab(),
            new AchievementsTab(),
            new BattleTab(),
            new SeasonTab(),
        };

        private const float WinW = 940f;
        private const float WinH = 600f;

        public static void Toggle() { _show = !_show; }
        public static bool IsOpen => _show;

        public static void Render()
        {
            if (!_show) return;
            if (_frameErrors > 8 && Time.unscaledTime < _skipUntil) return; // 熔断

            Theme.ApplySkin();
            float w = WinW * Theme.Scale, h = WinH * Theme.Scale;
            var win = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUI.Window(0x5D1F, win, Win, "  电竞教父 · Trainer");
        }

        private static void Win(int id)
        {
            try
            {
                DrawHeaderBand();
                DrawNav();
                DrawContent();
                DrawFooter();
                if (_showColorBar) DrawColorBar();
            }
            catch (System.Exception)
            {
                _frameErrors++;
                _skipUntil = Time.unscaledTime + 3f;
                GUILayout.Label(L10n.T("渲染异常，已熔断 3 秒"));
            }
            GUI.DragWindow(new Rect(0, 0, WinW * Theme.Scale, 30 * Theme.Scale));
        }

        // ---------- 顶栏：渐变饰带 + 单行状态 ----------
        private static void DrawHeaderBand()
        {
            using (new GUILayout.HorizontalScope())
            {
                // 饰带（左侧固定宽度渐变，避免拉伸全行）
                var bandRect = GUILayoutUtility.GetRect(320 * Theme.Scale, 6 * Theme.Scale,
                    GUILayout.Width(320 * Theme.Scale), GUILayout.Height(6 * Theme.Scale));
                GUI.DrawTexture(bandRect, Theme.HGrad(Theme.Cur.Primary, new Color(0, 0, 0, 0)));
                GUI.DrawTexture(new Rect(bandRect.x, bandRect.yMax - 1, bandRect.width, 1),
                    Theme.HGrad(Theme.Cur.Primary, Theme.Cur.Secondary));
            }

            using (new GUILayout.HorizontalScope())
            {
                var prevC = GUI.contentColor;
                GUI.contentColor = Theme.TextSecondary;
                GUILayout.Label("⏱ " + Refs.DateText(), GUILayout.Width(170 * Theme.Scale));
                GUILayout.Label(Refs.HasClub
                    ? $"{L10n.T("预算")} {Refs.CD.GetBudget():N0} · {L10n.T("教练点")} {Refs.CD.GetCoachPoint():N0} · {L10n.T("训练点")} {Refs.CD.GetTrainPoint():N0}"
                    : L10n.T("等待进入存档…"), GUILayout.ExpandWidth(true));
                GUI.contentColor = Refs.InBattle ? Theme.AccentOf(Theme.AccentColor.Red) : Theme.AccentOf(Theme.AccentColor.Green);
                GUILayout.Label(Refs.InBattle ? "● " + L10n.T("战斗中") : "○ " + L10n.T("经营中"), GUILayout.Width(76 * Theme.Scale));
                GUI.contentColor = prevC;
            }
            GUILayout.Space(2);
        }

        // ---------- 斜切 Tab 导航 ----------
        private static void DrawNav()
        {
            using (new GUILayout.HorizontalScope())
            {
                for (int i = 0; i < Tabs.Length; i++)
                {
                    var tab = Tabs[i];
                    bool active = _tabIndex == i;
                    var style = active ? Theme.NavOn : Theme.NavOff;
                    var rect = GUILayoutUtility.GetRect(0, 10000, style.fixedHeight, style.fixedHeight,
                        GUILayout.Width(92 * Theme.Scale), GUILayout.Height(style.fixedHeight));
                    if (GUI.Button(rect, tab.Title, style) && !active)
                        _tabIndex = i;
                }
                GUILayout.Label("", GUILayout.ExpandWidth(true));
            }
            GUILayout.Space(2);
        }

        // ---------- 内容区 ----------
        private static void DrawContent()
        {
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            GUILayout.Space(4);
            Tabs[_tabIndex].Render();
            GUILayout.Space(8);
            GUILayout.EndScrollView();
        }

        // ---------- 底栏：版本 / 水印 / 尺寸 / 配色 / 赞助 ----------
        private static void DrawFooter()
        {
            using (new GUILayout.HorizontalScope())
            {
                var prev = GUI.contentColor;
                GUI.contentColor = Theme.TextSecondary;
                GUILayout.Label("v3.4 · Mizuof · www.mizu7.top");
                GUI.contentColor = prev;

                GUILayout.Label(L10n.T("尺寸"), GUILayout.Width(30 * Theme.Scale));
                float ns = GUILayout.HorizontalSlider(Theme.Scale, 0.7f, 1.4f, GUILayout.Width(70 * Theme.Scale));
                if (Mathf.Abs(ns - Theme.Scale) > 0.001f) Theme.SetScale(ns);

                GUILayout.Label("", GUILayout.ExpandWidth(true));

                // 语言包切换（中/英），持久化到 MelonPreferences
                if (Components.GhostButton(Config.Lang == "zh" ? "语言 · 中文" : "Lang · EN", GUILayout.Width(96 * Theme.Scale)))
                    Config.ToggleLang();

                if (Components.GhostButton(L10n.T("配色 · ") + Theme.Cur.Name, GUILayout.Width(110 * Theme.Scale)))
                {
                    int next = (Config.SchemeIndex + 1) % Theme.Presets.Length;
                    Config.SetScheme(next);
                    _hexP = _hexS = _hexG = "";
                }
                if (Components.GhostButton(_showColorBar ? "自定义 ▾" : "自定义 ▸", GUILayout.Width(78 * Theme.Scale)))
                {
                    _showColorBar = !_showColorBar;
                    _hexP = Config.CustomPrimary;
                    _hexS = Config.CustomSecondary;
                    _hexG = Config.CustomGold;
                }
                if (Components.PrimaryButton(L10n.T("♥ ") + L10n.T("赞助"), Theme.AccentColor.Yellow,
                        GUILayout.Width(80 * Theme.Scale), GUILayout.Height(24 * Theme.Scale)))
                {
                    Application.OpenURL("https://www.mizu7.top/archives/thankyou");
                }
            }
        }

        // ---------- 自定义配色条 ----------
        private static void DrawColorBar()
        {
            using (new GUILayout.HorizontalScope())
            {
                var prev = GUI.contentColor;
                GUI.contentColor = Theme.TextSecondary;
                GUILayout.Label(L10n.T("主色"), GUILayout.Width(34 * Theme.Scale));
                _hexP = GUILayout.TextField(_hexP, GUILayout.Width(90 * Theme.Scale));
                GUILayout.Label(L10n.T("辅色"), GUILayout.Width(34 * Theme.Scale));
                _hexS = GUILayout.TextField(_hexS, GUILayout.Width(90 * Theme.Scale));
                GUILayout.Label(L10n.T("金色"), GUILayout.Width(34 * Theme.Scale));
                _hexG = GUILayout.TextField(_hexG, GUILayout.Width(90 * Theme.Scale));
                GUI.contentColor = prev;

                if (Components.HexButton(L10n.T("应用"), GUILayout.Width(60 * Theme.Scale), GUILayout.Height(24 * Theme.Scale)))
                {
                    Config.SetCustomColor("primary", _hexP);
                    Config.SetCustomColor("secondary", _hexS);
                    Config.SetCustomColor("gold", _hexG);
                    Log.Ok("Core", "配色已更新: " + Theme.Cur.Name);
                }
                if (Components.GhostButton(L10n.T("重置"), GUILayout.Width(50 * Theme.Scale), GUILayout.Height(24 * Theme.Scale)))
                {
                    Config.SetScheme(Config.SchemeIndex);
                    _hexP = _hexS = _hexG = "";
                }
                Components.Hint(L10n.T("格式 #8A5CFF，留空=预设"));
            }
            var lineRect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true), GUILayout.Height(1));
            GUI.DrawTexture(lineRect, Theme.Solid(Theme.Divider));
        }
    }
}
