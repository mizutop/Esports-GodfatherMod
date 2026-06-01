using MelonLoader;
using UnityEngine;
using GameMain;
using GameMain.ClubSystem;
using GameMain.AthleteSystem;
using GameMain.BattleSystem;
using System.Collections.Generic;
using System.Linq;
using Utility.SettingSystem;

[assembly: MelonInfo(typeof(DebugMod.Main), "电竞教父修改器", "2.0.0", "Mizuof")]
[assembly: MelonGame("Asteroid Studio", "Esport Godfather")]

namespace DebugMod
{
    public partial class Main : MelonMod
    {
        // ===== 面板状态 =====
        private bool showPanel = false;
        private Rect panelRect = new Rect(20, 20, 700, 560);
        private enum TabType { 俱乐部, 队员, 战斗, 赛事, 事件, 调试 }
        private TabType currentTab = TabType.俱乐部;
        private string[] tabNames = { "俱乐部", "队员", "战斗", "赛事", "事件", "调试" };
        private Vector2 scrollPos;
        private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

        // ===== 输入字段 =====
        private string cpI = "99999", tpI = "99999", bgI = "9999999";
        private string spdI = "3", ccI = "0", medI = "";
        private string cvHp = "99999", cvShield = "99999", cvGold = "99999", cvTac = "99";
        private string rsCntI = "10", rsLvI = "5", nameI = "";

        // ===== 分页 =====
        private int athPg = 0, perPg = 0;
        private List<AthletePersonalitySetting> allPerSets = new List<AthletePersonalitySetting>();

        // ===== 游戏引用缓存 =====
        private GameMain.Game _g = null;
        private Club _c = null;
        private Club.DataComponent _cd = null;
        private Battle _b = null;

        // ===== 战斗作弊开关 =====
        private bool tIL, tISL, tNSL, tIGL, tNGL, tIT, tIR, tISR, tNSR, tIGR, tNGR, tNT;
        private bool _bpSpeedUp = false;
        private bool _forceWinActive = false;
        private bool _forceWinPlayer = true;
        private int _forceWinFrameCount = 0;

        // ===== 常量 =====
        private const int MAX_ABILITY = 120;
        private const int MAX_HAND_CARDS = 7;
        private const int REFRESH_INTERVAL = 60;
        private const int PER_PAGE_SIZE = 6;
        private const int MEDAL_PAGE_SIZE = 10;

        public override void OnInitializeMelon()
        { LoggerInstance.Msg("电竞教父修改器 v2.0 加载成功 — F1 面板 F2 快照"); }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F1)) showPanel = !showPanel;
            if (Input.GetKeyDown(KeyCode.F2)) { DumpAllGameData(); return; }

            // 战斗作弊：锁定状态下每帧自动应用
            ApplyBattleCheats();

            // BP 锁定加速
            ApplyBpSpeedUp();

            // ForceWin 每帧锁定（直到战斗结束）
            ApplyForceWinLock();

            // 缓存刷新
            if (Time.frameCount % REFRESH_INTERVAL == 0) RefreshCache();
        }

        private void ApplyBattleCheats()
        {
            try
            {
                var b = _g?.Battle;
                if (b == null) return;
                if (tIL || tISL || tNSL || tIGL || tNGL) ApplyUnitMods(b.Team0, tIL, tISL, tNSL, tIGL, tNGL);
                if (tIR || tISR || tNSR || tIGR || tNGR) ApplyUnitMods(b.Team1, tIR, tISR, tNSR, tIGR, tNGR);
                if (tIT) SetTac(b, 0, 99);
                if (tNT) SetTac(b, 1, 0);
            }
            catch (System.Exception ex) { LoggerInstance.Msg("战斗作弊异常: " + ex.Message); }
        }

        private void ApplyBpSpeedUp()
        {
            if (!_bpSpeedUp) return;
            try
            {
                var b = _g?.Battle;
                if (b == null) return;
                var m = b.BanPickManager;
                if (m == null || m.Finished) return;
                var a = m.GetCurrentAction();
                if (a != null && a.timeRemain > 1f) a.timeRemain = 1f;
            }
            catch (System.Exception ex) { LoggerInstance.Msg("BP加速异常: " + ex.Message); }
        }

        private void ApplyForceWinLock()
        {
            if (!_forceWinActive) return;
            _forceWinFrameCount++;
            try
            {
                var b = _g?.Battle;
                if (b == null) { _forceWinActive = false; return; }
                // 战斗结束时 _b 会在下一帧缓存刷新中被置 null
                // 这里通过 catch 和 null 检测退出
                ForceWinBySuicide(b, _forceWinPlayer);
                if (_forceWinFrameCount % 60 == 0)
                    LoggerInstance.Msg($"ForceWin: 第 {_forceWinFrameCount / 60} 秒 持续锁定中...");
            }
            catch (System.Exception ex) { LoggerInstance.Msg("ForceWin锁定异常: " + ex.Message); _forceWinActive = false; }
        }

        void RefreshCache()
        {
            try
            {
                var inst = GameMain.Main.Inst;
                _g = inst?.Game;
                _c = _g?.ClubPlayer;
                _cd = _c?.C_Data;
                _b = _g?.Battle;
            }
            catch (System.Exception ex) { LoggerInstance.Msg("缓存刷新异常: " + ex.Message); }
        }

        public override void OnGUI()
        {
            if (!showPanel) return;
            panelRect = GUILayout.Window(0, panelRect, DoWindow, "电竞教父 修改器 v2.0");
        }

        void DoWindow(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            for (int i = 0; i < tabNames.Length; i++)
            {
                bool active = (int)currentTab == i;
                GUI.backgroundColor = active ? new Color(0.25f, 0.5f, 0.85f) : new Color(0.35f, 0.35f, 0.35f);
                if (GUILayout.Button(tabNames[i], GUILayout.Height(26)))
                {
                    currentTab = (TabType)i;
                    scrollPos = Vector2.zero;
                    RefreshCache();
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
            switch (currentTab)
            {
                case TabType.俱乐部: ClubTab(); break;
                case TabType.队员: AthTab(); break;
                case TabType.战斗: BatTab(); break;
                case TabType.赛事: SeasTab(); break;
                case TabType.事件: EvtTab(); break;
                case TabType.调试: DebugTab(); break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        // ===== 公共 UI 辅助方法 =====
        bool Fld(string k, string l)
        {
            if (!foldouts.ContainsKey(k)) foldouts[k] = false;
            bool v = foldouts[k];
            GUI.backgroundColor = v ? new Color(0.3f, 0.5f, 0.7f) : new Color(0.25f, 0.25f, 0.25f);
            GUI.contentColor = v ? Color.white : new Color(0.8f, 0.8f, 0.8f);
            string prefix = v ? "\u25bc " : "\u25b6 ";
            foldouts[k] = GUILayout.Toggle(v, prefix + l, GUI.skin.box, GUILayout.MinHeight(22));
            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
            return foldouts[k];
        }

        void Box(Color c, string t)
        {
            GUI.backgroundColor = c;
            GUILayout.Label(t, GUI.skin.box);
            GUI.backgroundColor = Color.white;
        }

        void Btn(string t, Color c, System.Action a)
        {
            GUI.backgroundColor = c;
            if (GUILayout.Button(t, GUILayout.Height(22))) a();
            GUI.backgroundColor = Color.white;
        }

        void Btn(string t, Color c, System.Action a, GUILayoutOption o)
        {
            GUI.backgroundColor = c;
            if (GUILayout.Button(t, o)) a();
            GUI.backgroundColor = Color.white;
        }

        void RwdBtn(string t, Color c, System.Action a)
        {
            GUI.backgroundColor = c;
            if (GUILayout.Button("\u2605 " + t, GUILayout.Height(24))) a();
            GUI.backgroundColor = Color.white;
        }

        void Row(string l, string cur, ref string inp, System.Action onSet)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(l, GUILayout.Width(80));
            GUILayout.Label(cur, GUILayout.Width(60));
            inp = GUILayout.TextField(inp, GUILayout.Width(80));
            if (GUILayout.Button("设置", GUILayout.Width(50))) onSet();
            GUILayout.EndHorizontal();
        }

        // ===== 颜色常量 =====
        private static readonly Color C_TITLE_BLUE = new Color(0.2f, 0.35f, 0.5f);
        private static readonly Color C_TITLE_GREEN = new Color(0.2f, 0.4f, 0.3f);
        private static readonly Color C_TITLE_RED = new Color(0.5f, 0.2f, 0.2f);
        private static readonly Color C_TITLE_PURPLE = new Color(0.3f, 0.25f, 0.5f);
        private static readonly Color C_BTN_GREEN = new Color(0.3f, 0.6f, 0.3f);
        private static readonly Color C_BTN_RED = new Color(0.6f, 0.2f, 0.2f);
        private static readonly Color C_BTN_BLUE = new Color(0.3f, 0.5f, 0.8f);
        private static readonly Color C_BTN_PURPLE = new Color(0.6f, 0.3f, 0.6f);
        private static readonly Color C_BTN_GOLD = new Color(0.9f, 0.7f, 0.2f);
        private static readonly Color C_BTN_ORANGE = new Color(0.8f, 0.5f, 0.1f);
        private static readonly Color C_BTN_OLIVE = new Color(0.3f, 0.5f, 0.2f);
        private static readonly Color C_BTN_TEAL = new Color(0.15f, 0.45f, 0.15f);
    }
}
