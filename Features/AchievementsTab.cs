using System.Collections.Generic;
using System.Linq;
using EsportsTrainer.Core;
using EsportsTrainer.UI;
using GameMain.AthleteSystem;
using GameMain.ClubSystem;
using UnityEngine;

namespace EsportsTrainer.Features
{
    /// <summary>
    /// Features/AchievementsTab.cs — 成就 / 队内关系（选手里程碑已迁至选手 Tab）。
    /// </summary>
    public class AchievementsTab : ITab
    {
        public string Id => "achv";
        public string Title => L10n.T("成就");
        public Theme.AccentColor Accent => Theme.AccentColor.Green;

        private const string T = "Achv";

        private int _sub, _athPg, _achPg, _relTarget;
        private List<GameMain.AchievementSystem.AchievementSettingData> _achList;
        private static string[] SubNames => new[] { L10n.T("成就"), L10n.T("队内关系") };
        private static readonly string[] RelationNames = { "对立", "疏远", "普通", "友好", "亲密" };

        public void Render()
        {
            if (!Refs.HasClub) { Components.Hint(L10n.T("请先进入存档")); return; }
            _sub = Components.SubNav(_sub, SubNames);
            GUILayout.Space(4);
            switch (_sub)
            {
                case 0: DrawAchievements(); break;
                case 1: DrawRelations(); break;
            }
        }

        // ================= 成就 =================
        private void DrawAchievements()
        {
            Components.SectionTitle(L10n.T("成就系统"), Accent);
            try
            {
                var list = LoadAchievements();
                if (list.Count == 0) { Components.Hint(L10n.T("成就表不可用")); return; }

                int done = 0;
                foreach (var a in list)
                {
                    try { if (CurProgress(a) >= a.targetValue) done++; } catch { }
                }
                Components.KeyValue(L10n.T("总进度"), $"{done} / {list.Count}");

                if (Components.PrimaryButton(L10n.T("一键全部解锁（含 Steam 同步）"), Accent, GUILayout.Height(28)))
                    WriteOps.UnlockAllAchievements();

                Components.Pagination(ref _achPg, list.Count, 8, out int st, out int en);
                for (int i = st; i < en; i++)
                {
                    var a = list[i];
                    if (a == null) continue;
                    int cur = CurProgress(a);
                    bool finished = cur >= a.targetValue && cur >= 0;

                    // 整行底衬：完成=翠绿高亮，未完成=微亮
                    var rowRect = GUILayoutUtility.GetRect(0, 10000, 26, 26,
                        GUILayout.Height(26 * Theme.Scale), GUILayout.ExpandWidth(true));
                    if (finished)
                        GUI.DrawTexture(rowRect, Theme.HGrad(
                            new Color(0.33f, 0.85f, 0.55f, 0.22f), new Color(0.33f, 0.85f, 0.55f, 0.04f)));
                    else
                        GUI.DrawTexture(rowRect, Theme.Solid(new Color(1f, 1f, 1f, 0.04f)));

                    var h = rowRect.height;
                    var prevC = GUI.contentColor;
                    // 中文名（与游戏内一致，取自 TB_Achievements 本地化表）
                    string name = AchName(a);
                    GUI.contentColor = finished ? new Color(0.55f, 0.95f, 0.70f) : Theme.TextPrimary;
                    GUI.Label(new Rect(rowRect.x + 10, rowRect.y, 250, h), name);
                    GUI.contentColor = finished ? new Color(0.55f, 0.95f, 0.70f) : Theme.TextSecondary;
                    GUI.Label(new Rect(rowRect.x + 265, rowRect.y, 110, h), $"{cur}/{a.targetValue}");
                    GUI.contentColor = finished ? new Color(0.55f, 0.95f, 0.70f)
                        : new Color(Theme.TextSecondary.r, Theme.TextSecondary.g, Theme.TextSecondary.b, 0.55f);
                    GUI.Label(new Rect(rowRect.x + 380, rowRect.y, 90, h), finished ? "✓ 已完成" : "未完成", BoldLabel());
                    GUI.contentColor = prevC;

                    if (!finished)
                    {
                        var btnRect = new Rect(rowRect.xMax - 74, rowRect.y + (h - 22 * Theme.Scale) / 2,
                            64 * Theme.Scale, 22 * Theme.Scale);
                        var captured = a;
                        if (GUI.Button(btnRect, "解锁", Theme.GhostBtn))
                            WriteOps.UnlockAchievement(captured);
                    }
                }
                Components.Hint(L10n.T("（名称/进度实时同步游戏内；进度取 本地进度与最高记录 的较大值）"));
            }
            catch (System.Exception ex) { Components.Hint("成就异常: " + ex.Message); }
        }

        /// <summary>成就中文名（游戏官方本地化表），失败回退 api 编号。</summary>
        private static string AchName(GameMain.AchievementSystem.AchievementSettingData a)
        {
            try
            {
                var n = Localization.TB_Achievements.GetName(a.key);
                if (!string.IsNullOrEmpty(n)) return n;
            }
            catch { }
            return "[" + a.api + "]";
        }

        /// <summary>
        /// 实时进度：max(本地进度 currentValue, 全局最高记录 records)。
        /// 游戏内部分成就（Steam 同步/hasRecord 型）只写 GlobalData.records，
        /// 只读 currentValue 会导致面板进度与游戏内不一致。
        /// </summary>
        private static int CurProgress(GameMain.AchievementSystem.AchievementSettingData a)
        {
            int cur = -1;
            try { cur = GameMain.Main.AchievementLocalData.GetValue(a.api); } catch { }
            try
            {
                int rec = GameMain.Main.AchievementGlobalData.GetHighestRecord(a.key);
                if (rec > cur) cur = rec;
            }
            catch { }
            return cur;
        }

        // ================= 队内关系 =================
        private void DrawRelations()
        {
            Components.SectionTitle(L10n.T("队内关系"), Accent);
            try
            {
                var ats = Refs.AthletesCached;
                if (ats.Count < 2) { Components.Hint(L10n.T("选手不足 2 人")); return; }

                _athPg = Mathf.Clamp(_athPg, 0, ats.Count - 1);
                var src = ats[_athPg];

                // 主选选手
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(L10n.T("◀"), GUILayout.Width(32))) _athPg = (_athPg - 1 + ats.Count) % ats.Count;
                    GUILayout.Label($"主选: {WriteOps.SafeName(src)} ({L10n.Role(src.HeroRole)})", GUILayout.ExpandWidth(true));
                    if (GUILayout.Button(L10n.T("▶"), GUILayout.Width(32))) _athPg = (_athPg + 1) % ats.Count;
                }
                Components.Hint(L10n.T("为「主选 × 其他选手」设置关系："));

                // 一键亲密
                using (new GUILayout.HorizontalScope())
                {
                    if (Components.PrimaryButton(L10n.T("一键亲密 (主选×全员)"), Accent, GUILayout.Height(24)))
                        foreach (var o in ats.Where(a => a != null && !ReferenceEquals(a, src)))
                            WriteOps.SetRelation(src, o, 2);
                    if (Components.GhostButton(L10n.T("全员两两亲密"), GUILayout.Height(24)))
                    {
                        int n = 0;
                        for (int i = 0; i < ats.Count; i++)
                            for (int j = i + 1; j < ats.Count; j++)
                            {
                                if (ats[i] == null || ats[j] == null) continue;
                                WriteOps.SetRelation(ats[i], ats[j], 2);
                                n++;
                            }
                        Log.Ok(T, $"全员两两亲密: {n} 组");
                    }
                }
                GUILayout.Space(2);

                // 关系目标分页（每页 5 人）
                var others = ats.Where(a => a != null && !ReferenceEquals(a, src)).ToList();
                _relTarget = Mathf.Clamp(_relTarget, 0, Mathf.Max(0, (others.Count - 1) / 5));
                int st = _relTarget * 5, en = Mathf.Min(st + 5, others.Count);

                var rels = Refs.G.C_Data.AthleteRelations;
                for (int i = st; i < en; i++)
                {
                    var other = others[i];
                    if (other == null) continue;
                    int val = WriteOps.GetRelation(src, other);
                    string lv = (val + 2 >= 0 && val + 2 < RelationNames.Length) ? RelationNames[val + 2] : val.ToString();
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label($"{WriteOps.SafeName(other)} ({L10n.Role(other.HeroRole)})", GUILayout.Width(170));
                        var prevC = GUI.contentColor;
                        GUI.contentColor = val > 0 ? Theme.AccentOf(Theme.AccentColor.Green)
                            : val < 0 ? Theme.AccentOf(Theme.AccentColor.Red)
                            : Theme.TextSecondary;
                        GUILayout.Label(lv, GUILayout.Width(50));
                        GUI.contentColor = prevC;
                        var captured = other;
                        if (GUILayout.Button(L10n.T("对立"), GUILayout.Width(44))) WriteOps.SetRelation(src, captured, -2);
                        if (GUILayout.Button(L10n.T("普通"), GUILayout.Width(44))) WriteOps.SetRelation(src, captured, 0);
                        if (GUILayout.Button(L10n.T("友好"), GUILayout.Width(44))) WriteOps.SetRelation(src, captured, 1);
                        if (GUILayout.Button(L10n.T("亲密"), GUILayout.Width(44))) WriteOps.SetRelation(src, captured, 2);
                    }
                }
                using (new GUILayout.HorizontalScope())
                {
                    if (Components.GhostButton(L10n.T("◀ 页"), GUILayout.Width(50))) _relTarget = Mathf.Max(0, _relTarget - 1);
                    GUILayout.Label($"{L10n.T("页")} {_relTarget + 1}/{Mathf.Max(1, (others.Count + 4) / 5)}", GUILayout.ExpandWidth(true));
                    if (Components.GhostButton(L10n.T("页 ▶"), GUILayout.Width(50))) _relTarget = Mathf.Min((others.Count - 1) / 5, _relTarget + 1);
                }
                Components.Hint(L10n.T("（-2 对立 · 0 普通 · +1 友好 · +2 亲密；不存在的条目自动创建）"));
            }
            catch (System.Exception ex) { Components.Hint("关系异常: " + ex.Message); }
        }

        private List<GameMain.AchievementSystem.AchievementSettingData> LoadAchievements()
        {
            if (_achList == null)
            {
                try { _achList = GameMain.AchievementSystem.Achievement.Setting.AchievementList.ToList(); }
                catch { _achList = new List<GameMain.AchievementSystem.AchievementSettingData>(); }
            }
            return _achList;
        }

        private static GUIStyle _boldLabel;
        private static GUIStyle BoldLabel()
        {
            if (_boldLabel == null)
                _boldLabel = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.BoldAndItalic };
            return _boldLabel;
        }
    }
}
