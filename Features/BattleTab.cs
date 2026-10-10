using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EsportsTrainer.Core;
using EsportsTrainer.UI;
using GameMain.BattleSystem;
using GameMain.UnitSystem;
using GameMain.UnitSystem.Equipment;
using Utility.SettingSystem;
using UnityEngine;

namespace EsportsTrainer.Features
{
    /// <summary>
    /// Features/BattleTab.cs — 比赛中域（v3 全量重构）。
    /// 顶部：回合/倍速/魔龙先锋倒计时/双方战术点/自动BP/禁止AI出牌/我方AI自动指挥/强制胜负。
    /// 下方：我方选手 / 敌方选手 两区，逐选手编辑数值（等级经验/血量/上限/金钱/攻击/护甲/暴击/存在感/装备增删）。
    /// 属性覆写走 Harmony postfix（UnitOverrides），战术点走 Athlete.C_Data.battleData.tacticPoint。
    /// </summary>
    public class BattleTab : ITab
    {
        public string Id => "battle";
        public string Title => L10n.T("比赛中");
        public Theme.AccentColor Accent => Theme.AccentColor.Red;

        private const int MaxHandCards = 7;
        private const string T = "Battle";

        private static GUIStyle _boldLabel;
        private static GUIStyle BoldLabel()
        {
            if (_boldLabel == null)
                _boldLabel = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.BoldAndItalic };
            return _boldLabel;
        }

        // ---- 状态 ----
        private static bool _bpSpeedUp, _forbidAI, _allyAuto;
        private static string _spdI = "3", _roundI = "1", _miniBossI = "2", _pioneerI = "2";
        private static string _tacLInput = "99", _tacRInput = "0", _costV = "0";
        private static int _unitPgL, _unitPgR, _equipPg;
        private static readonly Dictionary<Unit, Dictionary<string, string>> _unitInputs = new Dictionary<Unit, Dictionary<string, string>>();
        private static List<HeroEquipmentSetting> _equipSettings;
        private const BindingFlags AllFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public void Render()
        {
            if (!Refs.InBattle)
            {
                Components.Hint(L10n.T("当前无战斗（进入一场比赛后启用战斗功能）"));
                return;
            }
            var b = Refs.B;

            // ---------- 赛点 / 全局 ----------
            Components.SectionTitle(L10n.T("赛点 / 全局"), Accent);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(L10n.T("回合"), GUILayout.Width(34));
                _roundI = GUILayout.TextField(_roundI, GUILayout.Width(46));
                if (GUILayout.Button(L10n.T("应用"), GUILayout.Width(46)) && int.TryParse(_roundI, out int rv))
                    SafeApply(() => b.C_Data.SetRounds(rv), $"回合 → {rv}");
                GUILayout.Label(L10n.T("倍速"), GUILayout.Width(34));
                _spdI = GUILayout.TextField(_spdI, GUILayout.Width(40));
                if (GUILayout.Button(L10n.T("应用"), GUILayout.Width(46)) && int.TryParse(_spdI, out int sv))
                    SafeSpeed(b, sv);
                if (Components.GhostButton(L10n.T("x1"), GUILayout.Width(34))) SafeSpeed(b, 1);
                if (Components.GhostButton(L10n.T("x3"), GUILayout.Width(34))) SafeSpeed(b, 3);
                if (Components.GhostButton(L10n.T("暂停 / 恢复"), GUILayout.Width(80))) SafeTogglePause(b);
            }
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(L10n.T("魔龙倒计时"), GUILayout.Width(72));
                _miniBossI = GUILayout.TextField(_miniBossI, GUILayout.Width(44));
                if (GUILayout.Button(L10n.T("应用"), GUILayout.Width(46)) && int.TryParse(_miniBossI, out int mv))
                    SafeApply(() => b.C_Data.SetMiniBossRefresh(mv), $"魔龙倒计时 → {mv}");
                GUILayout.Label(L10n.T("先锋倒计时"), GUILayout.Width(72));
                _pioneerI = GUILayout.TextField(_pioneerI, GUILayout.Width(44));
                if (GUILayout.Button(L10n.T("应用"), GUILayout.Width(46)) && int.TryParse(_pioneerI, out int pv))
                    SafeApply(() => b.C_Data.SetPioneerRefreshTurn(pv), $"先锋倒计时 → {pv}");
                GUILayout.Label("", GUILayout.ExpandWidth(true));
            }
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(L10n.T("我方战术点"), GUILayout.Width(72));
                _tacLInput = GUILayout.TextField(_tacLInput, GUILayout.Width(44));
                if (GUILayout.Button(L10n.T("应用"), GUILayout.Width(46)) && int.TryParse(_tacLInput, out int tl))
                    SetTac(b, 0, tl);
                GUILayout.Label(L10n.T("敌方战术点"), GUILayout.Width(72));
                _tacRInput = GUILayout.TextField(_tacRInput, GUILayout.Width(44));
                if (GUILayout.Button(L10n.T("应用"), GUILayout.Width(46)) && int.TryParse(_tacRInput, out int tr))
                    SetTac(b, 1, tr);
                GUILayout.Label("", GUILayout.ExpandWidth(true));
            }

            _bpSpeedUp = Components.ToggleSwitch(_bpSpeedUp, L10n.T("自动BP (倒计时压缩)"));
            _forbidAI = Components.ToggleSwitch(_forbidAI, L10n.T("禁止AI出牌 (仅敌方)"));
            BattlePatches.ForbidAIUseCard = _forbidAI;
            _allyAuto = Components.ToggleSwitch(_allyAuto, L10n.T("我方AI自动指挥 (自动出牌/结束回合)"));

            // ---------- 强制胜负 ----------
            bool win = BattlePatches.ForceWinActive && BattlePatches.ForcePlayerWin;
            bool lose = BattlePatches.ForceWinActive && !BattlePatches.ForcePlayerWin;
            using (new GUILayout.HorizontalScope())
            {
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = win ? Theme.AccentOf(Theme.AccentColor.Green) : new Color(0.3f, 0.3f, 0.3f);
                if (GUILayout.Button(win ? L10n.T("锁定获胜中… (点击取消)") : L10n.T("强制获胜"), GUILayout.Height(26)))
                {
                    BattlePatches.ForceWinActive = !BattlePatches.ForceWinActive;
                    BattlePatches.ForcePlayerWin = true;
                }
                GUI.backgroundColor = lose ? Theme.AccentOf(Theme.AccentColor.Red) : new Color(0.3f, 0.3f, 0.3f);
                if (GUILayout.Button(lose ? L10n.T("锁定失败中… (点击取消)") : L10n.T("强制失败"), GUILayout.Height(26)))
                {
                    BattlePatches.ForceWinActive = !BattlePatches.ForceWinActive;
                    BattlePatches.ForcePlayerWin = false;
                }
                GUI.backgroundColor = prev;
                if (Components.GhostButton(L10n.T("立即终结 (击杀敌方基地)"), GUILayout.Height(26))) ForceWinBySuicide(b, true);
            }

            // ---------- 我方 / 敌方选手 ----------
            Components.SectionTitle(L10n.T("我方选手"), Accent);
            DrawTeamEditor(b.Team0, ref _unitPgL);
            Components.SectionTitle(L10n.T("敌方选手"), Accent);
            DrawTeamEditor(b.Team1, ref _unitPgR);
        }

        // ================= 队伍编辑区 =================
        private void DrawTeamEditor(BattleTeam team, ref int pg)
        {
            try
            {
                if (team == null) { Components.Hint(L10n.T("当前无战斗（进入一场比赛后启用战斗功能）")); return; }
                var units = CachedHeroes(team);
                if (units.Count == 0) { Components.Hint(L10n.T("无上场选手")); return; }

                Components.Pagination(ref pg, units.Count, 1, out int st, out int en);
                for (int i = st; i < en; i++)
                    DrawUnitEditor(units[i]);

                // 手牌
                int hc = team.C_Cards?.HandCards?.GetCards()?.Count() ?? 0;
                int ac = team.C_Cards?.AllCards?.Count ?? 0;
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label($"{L10n.T("手牌 / 卡池")}: {hc}/{ac}", GUILayout.Width(160 * Theme.Scale));
                    GUILayout.Label(L10n.T("费用:"), GUILayout.Width(34));
                    _costV = GUILayout.TextField(_costV, GUILayout.Width(44));
                    if (GUILayout.Button(L10n.T("批量改费"), GUILayout.Width(70)) && int.TryParse(_costV, out int cv))
                        SetAllCost(team, cv);
                    if (Components.GhostButton(L10n.T("补满手牌"), GUILayout.Width(70))) FillHand(team);
                    if (Components.GhostButton(L10n.T("清空手牌"), GUILayout.Width(70))) ClearHand(team);
                    GUILayout.Label("", GUILayout.ExpandWidth(true));
                }
            }
            catch (Exception ex) { Components.Hint("队伍读取异常: " + ex.Message); }
        }

        // ================= 单位编辑区 =================
        private void DrawUnitEditor(Unit u)
        {
            if (u == null || u.C_Data == null) return;
            string heroName = "?", athName = "?";
            try { heroName = u.Setting?.Name ?? "?"; } catch { }
            try { athName = u.Athlete?.GetName() ?? heroName; } catch { athName = heroName; }

            GUILayout.Label($"◆ {athName} · {heroName}", BoldLabel());
            var inp = Inputs(u);

            using (new GUILayout.HorizontalScope())
            {
                // 左列
                using (new GUILayout.VerticalScope(GUILayout.Width(300 * Theme.Scale)))
                {
                    EditRow(inp, "exp", L10n.T("等级(经验)"), u.Exp.ToString(), s => { if (int.TryParse(s, out int v)) u.C_Data.SetExp(v); });
                    EditRow(inp, "hp", L10n.T("当前血量"), u.HP.ToString(), s => { if (int.TryParse(s, out int v)) u.C_Data.SetHp(v); });
                    EditRow(inp, "hpmax", L10n.T("最大血量"), u.HPMax.ToString(), s =>
                    {
                        if (int.TryParse(s, out int v))
                        {
                            var f = typeof(Unit.DataComponent).GetField("m_overrideHpMax", AllFlags);
                            f?.SetValue(u.C_Data, (int?)v);
                            try { u.C_Data.SetHp(Mathf.Min(u.HP, v)); } catch { }
                        }
                    });
                    EditRow(inp, "gold", L10n.T("金钱"), u.Gold.ToString(), s => { if (int.TryParse(s, out int v)) u.C_Data.SetGold(v); });
                }
                // 右列
                using (new GUILayout.VerticalScope(GUILayout.Width(300 * Theme.Scale)))
                {
                    EditRow(inp, "atk", L10n.T("攻击力"), Fmt(UnitOverrides.Get(u, "attack") ?? u.Attack), s => { if (float.TryParse(s, out float v)) UnitOverrides.Set(u, "attack", v); });
                    EditRow(inp, "armor", L10n.T("护甲"), Fmt(UnitOverrides.Get(u, "armor") ?? u.Armor), s => { if (float.TryParse(s, out float v)) UnitOverrides.Set(u, "armor", v); });
                    EditRow(inp, "crit", L10n.T("暴击率"), Fmt((UnitOverrides.Get(u, "crit") ?? u.CritChance) * 100f) + "%", s => { if (float.TryParse(s.TrimEnd('%'), out float v)) UnitOverrides.Set(u, "crit", v / 100f); });
                    EditRow(inp, "pres", L10n.T("存在感"), Fmt(UnitOverrides.Get(u, "presence") ?? (float)u.Entity?.GetProperty_FightPresence()), s => { if (float.TryParse(s, out float v)) UnitOverrides.Set(u, "presence", v); });
                }
            }

            // 装备
            try
            {
                var eq = u.C_Equipment;
                if (eq != null)
                {
                    var list = eq.AllEquipments;
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label($"{L10n.T("装备")}({list.Count})", GUILayout.Width(120 * Theme.Scale));
                        GUILayout.Label("", GUILayout.ExpandWidth(true));
                    }
                    foreach (var e in list)
                    {
                        string en = "?";
                        try { en = e.Setting?.Name ?? "?"; } catch { }
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("  · " + en, GUILayout.Width(220 * Theme.Scale));
                            var captured = e;
                            if (Components.GhostButton(L10n.T("移除"), GUILayout.Width(50)))
                                MelonLoader.MelonCoroutines.Start(eq.Co_RemoveEquipment(captured, true));
                            GUILayout.Label("", GUILayout.ExpandWidth(true));
                        }
                    }
                    if (Components.GhostButton(_equipPg > 0 ? L10n.T("收起") : L10n.T("添加装备")))
                        _equipPg = _equipPg > 0 ? 0 : 1;
                    if (_equipPg > 0)
                    {
                        var settings = LoadEquipSettings();
                        Components.Pagination(ref _equipPg, settings.Count, 5, out int st, out int en2);
                        for (int i = st; i < en2; i++)
                        {
                            var s = settings[i];
                            if (s == null) continue;
                            string sn = "?";
                            try { sn = s.Name ?? "?"; } catch { }
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label("  + " + sn, GUILayout.Width(220 * Theme.Scale));
                                var capturedS = s;
                                if (Components.GhostButton(L10n.T("添加"), GUILayout.Width(50)))
                                    MelonLoader.MelonCoroutines.Start(eq.Co_AddEquipment(capturedS));
                                GUILayout.Label("", GUILayout.ExpandWidth(true));
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Components.Hint("装备异常: " + ex.Message); }

            GUILayout.Space(4);
            GUI.DrawTexture(GUILayoutUtility.GetRect(0, 10000, 1, 1, GUILayout.ExpandWidth(true)), Theme.Solid(Theme.Divider));
        }

        // ================= 每帧驱动（0.25s 节流，战斗结束自动还原） =================
        private static float _frameT;
        private static readonly Dictionary<BattleTeam, (List<Unit> list, float t)> _heroCache = new Dictionary<BattleTeam, (List<Unit>, float)>();

        private static List<Unit> CachedHeroes(BattleTeam team)
        {
            if (_heroCache.TryGetValue(team, out var e) && Time.unscaledTime - e.t < 0.5f) return e.list;
            var l = team?.Heroes?.ToList() ?? new List<Unit>();
            _heroCache[team] = (l, Time.unscaledTime);
            return l;
        }

        public static void OnFrame()
        {
            try
            {
                var b = Refs.B;
                if (b == null)
                {
                    // 战斗结束：自动还原除「自动BP」外的全部修改
                    bool restored = false;
                    if (BattlePatches.ForceWinActive) { BattlePatches.ForceWinActive = false; restored = true; }
                    if (BattlePatches.ForbidAIUseCard) { BattlePatches.ForbidAIUseCard = false; _forbidAI = false; restored = true; }
                    if (_allyAuto) { _allyAuto = false; restored = true; }
                    if (UnitOverrides.Attack.Count > 0) { UnitOverrides.ClearAll(); restored = true; }
                    if (_unitInputs.Count > 0) _unitInputs.Clear();
                    if (_heroCache.Count > 0) _heroCache.Clear();
                    if (restored) Log.Ok(T, "战斗结束，自动还原全部战斗修改（自动BP 除外）");
                    return;
                }

                // 节流：0.25s 执行一次，降低每帧开销
                if (Time.unscaledTime - _frameT < 0.25f) return;
                _frameT = Time.unscaledTime;

                // 自动 BP：压缩倒计时
                if (_bpSpeedUp)
                {
                    var m = b.BanPickManager;
                    if (m != null && !m.Finished)
                    {
                        var a = m.GetCurrentAction();
                        if (a != null && a.timeRemain > 1f) a.timeRemain = 1f;
                    }
                }

                // 我方 AI 自动指挥：逐张尝试出牌，无牌可用则结束回合
                if (_allyAuto) AllyAutoPlay(b);
            }
            catch { }
        }

        private static void AllyAutoPlay(Battle b)
        {
            var team = b.Team0;
            if (team?.C_Agent == null || team.C_Agent.IsAI || team.C_Cards == null) return;
            var hand = team.C_Cards.HandCards?.GetCards()?.ToList();
            if (hand != null)
            {
                foreach (var c in hand)
                {
                    if (c == null) continue;
                    try { if (team.C_Agent.TryUseCard(c)) return; } catch { }
                }
            }
            try { b.C_UI.BattleUI.SetTeamActionIsEnd(); } catch { }
        }

        // ================= 辅助 =================
        private static Dictionary<string, string> Inputs(Unit u)
        {
            if (!_unitInputs.TryGetValue(u, out var d))
            {
                d = new Dictionary<string, string>();
                _unitInputs[u] = d;
            }
            return d;
        }

        private static void EditRow(Dictionary<string, string> inp, string key, string label, string cur, Action<string> apply)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label($"{label}: {cur}", GUILayout.Width(150 * Theme.Scale));
                inp.TryGetValue(key, out var v);
                var nv = GUILayout.TextField(v ?? "", GUILayout.Width(76 * Theme.Scale));
                inp[key] = nv;
                if (GUILayout.Button(L10n.T("应用"), GUILayout.Width(46 * Theme.Scale))) apply(nv);
                GUILayout.Label("", GUILayout.ExpandWidth(true));
            }
        }

        private static string Fmt(float v) => Mathf.Approximately(v, Mathf.Round(v)) ? ((int)v).ToString() : v.ToString("F2");

        private static List<HeroEquipmentSetting> LoadEquipSettings()
        {
            if (_equipSettings == null)
            {
                try { _equipSettings = SettingCenter.GetSettings<HeroEquipmentSetting>().ToList(); }
                catch { _equipSettings = new List<HeroEquipmentSetting>(); }
            }
            return _equipSettings;
        }

        private static void SafeApply(Action act, string desc)
        {
            try { act(); Log.Ok(T, desc); }
            catch (Exception ex) { Log.Exception(T, desc, ex); }
        }

        private static void SafeSpeed(Battle b, int v)
        {
            try
            {
                int before = b.C_Data.GetFightSpeed();
                b.C_Data.SetFightSpeed(v);
                int after = b.C_Data.GetFightSpeed();
                Log.Write(T, "倍速", before + "x", v + "x", after + "x", after == v);
            }
            catch (Exception ex) { Log.Exception(T, "SetFightSpeed", ex); }
        }

        private static void SafeTogglePause(Battle b)
        {
            try
            {
                bool wasPaused = b.IsPaused;
                b.SetBattleRunState(wasPaused ? BattleRunStates.Running : BattleRunStates.Paused);
                Log.Write(T, "战斗", wasPaused ? "已暂停" : "进行中", wasPaused ? "恢复" : "暂停", b.IsPaused ? "已暂停" : "进行中", b.IsPaused != wasPaused);
            }
            catch (Exception ex) { Log.Exception(T, "暂停切换", ex); }
        }

        private static void SetTac(Battle battle, int teamIdx, int val)
        {
            var team = teamIdx == 0 ? battle.Team0 : battle.Team1;
            if (team == null) return;
            // 真实路径：Athlete.C_Data.battleData.tacticPoint（公开字段）
            int n = 0;
            foreach (var ath in team.Athletes)
            {
                if (ath == null || ath.C_Data?.battleData == null) continue;
                try { ath.C_Data.battleData.tacticPoint = val; n++; } catch { }
            }
            Log.Ok(T, $"战术点 {(teamIdx == 0 ? "我方" : "敌方")} → {val}（{n} 名选手）");
        }

        private static void ForceWinBySuicide(Battle battle, bool playerWin)
        {
            try
            {
                if (battle.Team0 == null || battle.Team1 == null) return;
                int target = playerWin ? battle.Team1.BattleTeamIndexSelf : battle.Team0.BattleTeamIndexSelf;
                var enemyBase = battle.GetBase(target);
                if (enemyBase == null || !enemyBase.BattleIsAlive || enemyBase.C_Logic == null)
                {
                    Log.Warn(T, "立即终结: 基地不可用或已阵亡");
                    return;
                }
                enemyBase.C_Logic.Suicide();
                Log.Ok(T, $"立即终结: {(playerWin ? "敌方" : "己方")}基地已击杀");
            }
            catch (Exception ex) { Log.Exception(T, "立即终结", ex); }
        }

        private static void SetAllCost(BattleTeam team, int cost)
        {
            if (team?.C_Cards?.AllCards == null) return;
            int ok = 0, total = 0;
            foreach (var c in team.C_Cards.AllCards)
            {
                if (c == null) continue;
                total++;
                try { c.Data.SetCost(cost); ok++; } catch { }
            }
            if (ok == total) Log.Ok(T, $"批量改费: {ok}/{total} 张 → {cost}");
            else Log.Warn(T, $"批量改费: 仅 {ok}/{total} 张成功 → {cost}");
        }

        private static void FillHand(BattleTeam team)
        {
            try
            {
                if (team?.C_Cards == null) return;
                // 真实结构：m_drawCards = List<BattleCard>；m_handCards = BattleCardHandCardList（自带 Add/Remove(BattleCard)）
                var draw = team.C_Cards.GetType().GetField("m_drawCards", AllFlags)
                    ?.GetValue(team.C_Cards) as List<BattleCard>;
                var hand = team.C_Cards.GetType().GetField("m_handCards", AllFlags)
                    ?.GetValue(team.C_Cards) as BattleCardHandCardList;
                if (draw == null || hand == null) { Log.Warn(T, "补牌: 卡牌列表字段不可访问"); return; }

                int cur = hand.GetCards().Count();
                int toMove = Mathf.Max(0, Mathf.Min(MaxHandCards - cur, draw.Count));
                for (int i = 0; i < toMove; i++)
                {
                    var c = draw[draw.Count - 1];
                    draw.Remove(c);
                    hand.Add(c);   // 官方重载：自动处理堆叠/分栏
                }
                try { team.RefreshHandCard(); } catch { }
                Log.Ok(T, $"补牌: {toMove} 张 → 手牌 ({hand.GetCards().Count()}/{MaxHandCards})");
            }
            catch (Exception ex) { Log.Exception(T, "补牌", ex); }
        }

        private static void ClearHand(BattleTeam team)
        {
            if (team?.C_Cards?.HandCards == null) return;
            var cards = team.C_Cards.HandCards.GetCards().ToList();
            foreach (var c in cards) team.C_Cards.HandCards.Remove(c);
            try { team.RefreshHandCard(); } catch { }
            Log.Ok(T, $"清空手牌: {cards.Count} 张");
        }
    }
}
