using UnityEngine;
using GameMain;
using GameMain.BattleSystem;
using System.Linq;
using System.Collections;

namespace DebugMod
{
    public partial class Main
    {
        void BatTab()
        {
            if (_b == null) { GUILayout.Label("当前没有战斗中"); return; }

            Box(C_TITLE_BLUE, string.Format("回合 {0} | 速度 {1}x", _b.Rounds, _b.C_Data.GetFightSpeed()));
            DrawTeamInfo(_b.Team0, "己方队伍", true);
            DrawTeamInfo(_b.Team1, "对方队伍", false);

            // ---- 己方作弊 ----
            if (Fld("bat_self", "己方作弊"))
            {
                tIL = GUILayout.Toggle(tIL, "无限生命");
                tISL = GUILayout.Toggle(tISL, "无限护盾");
                tNSL = GUILayout.Toggle(tNSL, "没有护盾");
                tIGL = GUILayout.Toggle(tIGL, "无限金钱");
                tNGL = GUILayout.Toggle(tNGL, "没有金钱");
                tIT = GUILayout.Toggle(tIT, "无限战术点");
                Btn("应用", C_BTN_GREEN, () =>
                {
                    ApplyUnitMods(_b.Team0, tIL, tISL, tNSL, tIGL, tNGL);
                    if (tIT) SetTac(_b, 0, 99);
                });
            }

            // ---- 对方削弱 ----
            if (Fld("bat_enemy", "对方削弱"))
            {
                tIR = GUILayout.Toggle(tIR, "无限生命");
                tISR = GUILayout.Toggle(tISR, "无限护盾");
                tNSR = GUILayout.Toggle(tNSR, "没有护盾");
                tIGR = GUILayout.Toggle(tIGR, "无限金钱");
                tNGR = GUILayout.Toggle(tNGR, "没有金钱");
                tNT = GUILayout.Toggle(tNT, "无战术点");
                Btn("应用", C_BTN_RED, () =>
                {
                    ApplyUnitMods(_b.Team1, tIR, tISR, tNSR, tIGR, tNGR);
                    if (tNT) SetTac(_b, 1, 0);
                });
            }

            // ---- 强制结果 / 速度 (重构版) ----
            if (Fld("bat_result", "强制结果 / 速度"))
            {
                GUILayout.BeginHorizontal();
                GUI.backgroundColor = _forceWinActive ? (_forceWinPlayer ? C_BTN_GREEN : C_BTN_RED) : Color.gray;
                if (GUILayout.Button(_forceWinActive
                    ? (_forceWinPlayer ? "\u26a1 锁定获胜中..." : "\u26a1 锁定失败中...")
                    : "\u26a1 强制获胜", GUILayout.Height(24)))
                {
                    if (!_forceWinActive)
                    {
                        _forceWinActive = true;
                        _forceWinPlayer = true;
                        _forceWinFrameCount = 0;
                        LoggerInstance.Msg("ForceWin: 开始锁定获胜");
                    }
                    else
                    {
                        _forceWinActive = false;
                        LoggerInstance.Msg("ForceWin: 已取消");
                    }
                }
                GUI.backgroundColor = _forceWinActive && !_forceWinPlayer ? C_BTN_RED : Color.gray;
                if (GUILayout.Button(_forceWinActive && !_forceWinPlayer
                    ? "\u26a1 锁定失败中..."
                    : "\u26a1 强制失败", GUILayout.Height(24)))
                {
                    if (!_forceWinActive)
                    {
                        _forceWinActive = true;
                        _forceWinPlayer = false;
                        _forceWinFrameCount = 0;
                        LoggerInstance.Msg("ForceWin: 开始锁定失败");
                    }
                    else
                    {
                        _forceWinActive = false;
                        LoggerInstance.Msg("ForceWin: 已取消");
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                if (_forceWinActive)
                    GUILayout.Label(string.Format("  ForceWin 已激活: {0} ({1}帧)",
                        _forceWinPlayer ? "获胜" : "失败", _forceWinFrameCount));

                GUILayout.BeginHorizontal();
                GUILayout.Label("速度:", GUILayout.Width(50));
                spdI = GUILayout.TextField(spdI, GUILayout.Width(60));
                if (GUILayout.Button("应用", GUILayout.Width(50)))
                {
                    if (int.TryParse(spdI, out int v)) _b.C_Data.SetFightSpeed(v);
                }
                GUILayout.EndHorizontal();
            }

            // ---- 手牌控制 ----
            if (Fld("bat_hand", "手牌控制"))
            {
                var team = _b.Team0;
                if (team != null)
                {
                    int hc = team.C_Cards?.HandCards?.GetCards()?.Count() ?? 0;
                    int ac = team.C_Cards?.AllCards?.Count ?? 0;
                    GUILayout.Label(string.Format("手牌: {0}  总卡池: {1}", hc, ac));
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("费用:", GUILayout.Width(40));
                    ccI = GUILayout.TextField(ccI, GUILayout.Width(50));
                    if (GUILayout.Button("批量修改费用", GUILayout.Width(100)))
                    {
                        if (int.TryParse(ccI, out int cv)) SetAllCost(team, cv);
                    }
                    GUILayout.EndHorizontal();
                    Btn("补满手牌", new Color(0.2f, 0.5f, 0.3f), () => FillHand(team));
                    Btn("清空手牌", C_BTN_RED, () => ClearHand(team));
                }

                _bpSpeedUp = GUILayout.Toggle(_bpSpeedUp,
                    _bpSpeedUp ? "[锁定] BP 自动加速中" : "[未锁定] BP 加速", GUILayout.Height(24));
            }
        }

        void DrawTeamInfo(BattleTeam team, string label, bool isPlayer)
        {
            if (team == null) return;
            Box(isPlayer ? C_TITLE_GREEN : C_TITLE_RED,
                label + string.Format(" (英雄 {0}  能量 {1})", team.Heroes.Count(), team.EnergyCurrent));
            int idx = 0;
            foreach (var u in team.Heroes)
            {
                if (u == null || idx >= 5) continue;
                GUILayout.Label(string.Format("  [{0}] HP:{1}/{2} 盾:{3} 金:{4}",
                    idx++, u.HPCache, u.HPMaxCache, u.Shield, u.Gold));
            }
        }

        void ApplyUnitMods(BattleTeam team, bool il, bool ish, bool ns, bool ig, bool ng)
        {
            if (team == null) return;
            if (!int.TryParse(cvHp, out int hpV)) hpV = 99999;
            if (!int.TryParse(cvShield, out int shV)) shV = 99999;
            if (!int.TryParse(cvGold, out int gdV)) gdV = 99999;
            foreach (var u in team.Heroes)
            {
                if (u == null) continue;
                var dc = u.C_Data;
                var t = dc.GetType();
                if (il) { var hf = t.GetField("HpCache") ?? t.GetField("hpCache"); if (hf != null) hf.SetValue(dc, hpV); }
                if (ish)
                {
                    var sf = t.GetField("Shield") ?? t.GetField("shield");
                    if (sf != null) sf.SetValue(dc, shV);
                    else { var sp = t.GetProperty("Shield"); sp?.SetValue(dc, shV, null); }
                }
                if (ns)
                {
                    var sf = t.GetField("Shield") ?? t.GetField("shield");
                    if (sf != null) sf.SetValue(dc, 0);
                    else { var sp = t.GetProperty("Shield"); sp?.SetValue(dc, 0, null); }
                }
                if (ig)
                {
                    var gf = t.GetField("Gold") ?? t.GetField("gold");
                    if (gf != null) gf.SetValue(dc, gdV);
                    else { var gp = t.GetProperty("Gold"); gp?.SetValue(dc, gdV, null); }
                }
                if (ng)
                {
                    var gf = t.GetField("Gold") ?? t.GetField("gold");
                    if (gf != null) gf.SetValue(dc, 0);
                    else { var gp = t.GetProperty("Gold"); gp?.SetValue(dc, 0, null); }
                }
            }
        }

        void SetTac(Battle battle, int teamIdx, int val)
        {
            if (!int.TryParse(cvTac, out int tv)) tv = val;
            val = tv;
            var team = teamIdx == 0 ? battle.Team0 : battle.Team1;
            if (team == null) return;
            foreach (var a in team.Athletes)
            {
                if (a == null) continue;
                var bd = a.GetType().GetField("battleData")?.GetValue(a);
                if (bd != null)
                {
                    var tf = bd.GetType().GetField("tacticPoint");
                    if (tf != null) tf.SetValue(bd, val);
                }
            }
        }

        void ForceWinBySuicide(Battle battle, bool playerWin)
        {
            // 用 BattleTeamIndexSelf 获取正确的基地索引
            int selfTeamIdx = battle.Team0.BattleTeamIndexSelf;
            int enemyTeamIdx = battle.Team1.BattleTeamIndexSelf;
            int targetTeamIdx = playerWin ? enemyTeamIdx : selfTeamIdx;
            var enemyBase = battle.GetBase(targetTeamIdx);
            if (enemyBase == null) { LoggerInstance.Msg("ForceWin: 无法获取基地"); return; }
            if (!enemyBase.BattleIsAlive)
            {
                return; // 基地已死
            }
            enemyBase.C_Logic.Suicide();
            LoggerInstance.Msg(string.Format("ForceWin: 基地击杀 ({0}) HP={1}→0",
                playerWin ? "敌方" : "己方",
                enemyBase.HPCache));
        }

        void SetAllCost(BattleTeam team, int cost)
        {
            if (team?.C_Cards?.AllCards == null) return;
            int ok = 0, fail = 0;
            foreach (var c in team.C_Cards.AllCards)
                if (c != null)
                {
                    try { c.Data.SetCost(cost); ok++; }
                    catch { fail++; }
                }
            LoggerInstance.Msg(string.Format("批量改费: {0}张成功, {1}张失败", ok, fail));
        }

        void FillHand(BattleTeam team)
        {
            try
            {
                if (team?.C_Cards == null) return;
                var drawFld = team.C_Cards.GetType().GetField("m_drawCards",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var handFld = team.C_Cards.GetType().GetField("m_handCards",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (drawFld == null || handFld == null) { LoggerInstance.Msg("无法访问卡牌列表"); return; }
                var draw = drawFld.GetValue(team.C_Cards) as System.Collections.IList;
                var hand = handFld.GetValue(team.C_Cards) as System.Collections.IList;
                if (draw == null || hand == null) return;
                int curHand = hand.Count;
                int toMove = Mathf.Max(0, Mathf.Min(MAX_HAND_CARDS - curHand, draw.Count));
                if (toMove <= 0) { LoggerInstance.Msg("手牌已满或无可用牌"); return; }
                var toRemove = new System.Collections.Generic.List<object>();
                for (int i = 0; i < toMove; i++) { toRemove.Add(draw[0]); draw.RemoveAt(0); }
                foreach (var c in toRemove) hand.Add(c);
                LoggerInstance.Msg("已移动 " + toMove + " 张牌到手牌");
            }
            catch (System.Exception ex) { LoggerInstance.Msg("补牌失败: " + ex.Message); }
        }

        void ClearHand(BattleTeam team)
        {
            if (team?.C_Cards?.HandCards == null) return;
            var cards = team.C_Cards.HandCards.GetCards().ToList();
            foreach (var c in cards) team.C_Cards.HandCards.Remove(c);
            LoggerInstance.Msg("清空手牌 " + cards.Count + " 张");
        }
    }
}
