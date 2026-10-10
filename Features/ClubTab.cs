using System.Linq;
using EsportsTrainer.Core;
using EsportsTrainer.UI;
using GameMain.ClubSystem;
using UnityEngine;
using Utility.SettingSystem;

namespace EsportsTrainer.Features
{
    /// <summary>
    /// Features/ClubTab.cs — 俱乐部域（子导航：资源 / 勋章 / 里程碑 / 发展 / 荣誉）。
    /// 全部写入经 WriteOps（写入→回读→日志）。
    /// </summary>
    public class ClubTab : ITab
    {
        public string Id => "club";
        public string Title => L10n.T("俱乐部");
        public Theme.AccentColor Accent => Theme.AccentColor.Yellow;

        private const string T = "Club";

        private int _sub;
        private string _coach = "99999", _train = "99999", _budget = "9999999", _wp = "99999";
        private string _rsLv = "5", _rsCnt = "10", _medalSearch = "";
        private int _medalPg, _milePg;
        private System.Collections.Generic.List<ClubMedalSetting> _medals;
        private bool _showHonors;
        private bool _dailyVeryGood, _abilityBetter;
        private readonly System.Collections.Generic.Dictionary<string, string> _loanInputs = new System.Collections.Generic.Dictionary<string, string>();

        private static string[] SubNames => new[] { L10n.T("资源"), L10n.T("勋章"), L10n.T("里程碑"), L10n.T("发展"), L10n.T("荣誉") };

        public void Render()
        {
            if (!Refs.HasClub)
            {
                Components.Hint(L10n.T("请先进入存档（俱乐部数据不可用）"));
                return;
            }
            _sub = Components.SubNav(_sub, SubNames);
            GUILayout.Space(4);
            switch (_sub)
            {
                case 0: DrawResources(); break;
                case 1: DrawMedals(); break;
                case 2: DrawMilestones(); break;
                case 3: DrawDevelopment(); break;
                case 4: DrawHonors(); break;
            }
        }

        // ================= 资源 =================
        private void DrawResources()
        {
            var cd = Refs.CD;
            Components.SectionTitle(L10n.T("俱乐部资源"), Accent);
            Components.ValueRow(L10n.T("教练点"), cd.GetCoachPoint().ToString("N0"), ref _coach,
                () => { if (int.TryParse(_coach, out int v)) WriteOps.SetCoachPoint(cd, v); });
            Components.ValueRow(L10n.T("训练点"), cd.GetTrainPoint().ToString("N0"), ref _train,
                () => { if (int.TryParse(_train, out int v)) WriteOps.SetTrainPoint(cd, v); });
            Components.ValueRow(L10n.T("预算"), cd.GetBudget().ToString("N0"), ref _budget,
                () => { if (int.TryParse(_budget, out int v)) WriteOps.SetBudget(cd, v); });
            Components.ValueRow(L10n.T("世界分"), SafeWorldPoint(cd), ref _wp,
                () => { if (int.TryParse(_wp, out int v)) WriteOps.SetWorldPoint(cd, v); });
            Components.KeyValue(L10n.T("粉丝数"), Refs.C.Fans.ToString("N0") + "（随选手粉丝汇总）");

            using (new GUILayout.HorizontalScope())
            {
                if (Components.PrimaryButton(L10n.T("资源全满"), Accent, GUILayout.Height(26)))
                {
                    WriteOps.SetCoachPoint(cd, 99999);
                    WriteOps.SetTrainPoint(cd, 99999);
                    WriteOps.SetBudget(cd, 9999999);
                }
                if (Components.GhostButton(L10n.T("教练点 +1000"), GUILayout.Height(26)))
                    WriteOps.SetCoachPoint(cd, cd.GetCoachPoint() + 1000);
                if (Components.GhostButton(L10n.T("训练点 +1000"), GUILayout.Height(26)))
                    WriteOps.SetTrainPoint(cd, cd.GetTrainPoint() + 1000);
                if (Components.GhostButton(L10n.T("预算 +100000"), GUILayout.Height(26)))
                    WriteOps.SetBudget(cd, cd.GetBudget() + 100000);
            }
        }

        // ================= 勋章 =================
        private void DrawMedals()
        {
            var club = Refs.C;
            Components.SectionTitle(L10n.T("勋章系统"), Accent);
            int owned = 0;
            try { owned = club.C_Medal?.StoreMedals?.Keys?.Count ?? 0; } catch { }
            Components.KeyValue(L10n.T("已解锁"), owned + " 枚");

            using (new GUILayout.HorizontalScope())
            {
                _medalSearch = GUILayout.TextField(_medalSearch, GUILayout.Width(180));
                if (Components.PrimaryButton(L10n.T("搜索解锁"), Theme.AccentColor.Green, GUILayout.Width(90)))
                {
                    var f = LoadMedals().FirstOrDefault(m => { try { return m.Desc == _medalSearch; } catch { return false; } });
                    if (f != null) WriteOps.AddMedal(club, f);
                    else Log.Warn(T, $"未找到名称为「{_medalSearch}」的勋章");
                }
                if (Components.PrimaryButton(L10n.T("一键全解锁"), Accent, GUILayout.Width(100), GUILayout.Height(26)))
                    WriteOps.AddAllMedals(club);
            }

            var medals = LoadMedals();
            if (medals.Count > 0)
            {
                Components.Pagination(ref _medalPg, medals.Count, 10, out int st, out int en);
                for (int i = st; i < en; i++)
                {
                    var ms = medals[i];
                    string dn = "?";
                    try { dn = ms.Desc ?? "?"; } catch { }
                    if (Components.GhostButton(L10n.T("解锁: ") + dn))
                        WriteOps.AddMedal(club, ms);
                }
            }
        }

        // ================= 里程碑 =================
        private void DrawMilestones()
        {
            Components.SectionTitle(L10n.T("俱乐部里程碑"), Accent);
            try
            {
                var ms = Refs.C.C_Milestone?.milestones;
                if (ms == null || ms.Count == 0) { Components.Hint(L10n.T("里程碑列表不可用")); return; }

                int done = ms.Count(m => { try { return m.State == MilestoneStates.Finished; } catch { return false; } });
                Components.KeyValue(L10n.T("进度"), $"{done} / {ms.Count}");
                if (Components.PrimaryButton(L10n.T("全部解锁并领奖"), Accent, GUILayout.Height(26)))
                    WriteOps.UnlockAllMilestones(ms);

                Components.Pagination(ref _milePg, ms.Count, 8, out int st, out int en);
                for (int i = st; i < en; i++)
                    DrawMilestoneRow(ms[i]);
            }
            catch (System.Exception ex) { Components.Hint("里程碑异常: " + ex.Message); }
        }

        // ================= 发展 =================
        private void DrawDevelopment()
        {
            var cd = Refs.CD;
            var club = Refs.C;

            Components.SectionTitle(L10n.T("晋级 / 评级"), Accent);
            Components.KeyValue(L10n.T("当前级别"), club.ClubClassType.ToString());
            using (new GUILayout.HorizontalScope())
            {
                if (Components.PrimaryButton(L10n.T("晋级"), Theme.AccentColor.Green, GUILayout.Height(26)))
                    Log.Write(T, "晋级", club.ClubClassType.ToString(), "→ 上一级", null, club.PromoteClub());
                if (Components.PrimaryButton(L10n.T("降级"), Theme.AccentColor.Red, GUILayout.Height(26)))
                    Log.Write(T, "降级", club.ClubClassType.ToString(), "→ 下一级", null, club.DemoteClub());
                if (Components.GhostButton(L10n.T("教练晋升"), GUILayout.Height(26)))
                    Log.Write(T, "教练晋升", "当前", "→ 下一级", null, club.PromoteCoach());
            }

            Components.SectionTitle(L10n.T("教练等级 / 技能树"), Accent);
            try { Components.KeyValue(L10n.T("教练等级"), club.CoachClassType.ToString()); } catch { }
            using (new GUILayout.HorizontalScope())
            {
                if (Components.PrimaryButton(L10n.T("教练直升 S（含奖励）"), Theme.AccentColor.Green, GUILayout.Height(26)))
                    WriteOps.PromoteCoachMax(club);
                if (Components.PrimaryButton(L10n.T("解锁全部升级项（技能树去锁）"), Accent, GUILayout.Height(26)))
                    WriteOps.UnlockAllUpgradeItems(club);
            }
            if (Components.PrimaryButton(L10n.T("俱乐部升级全部完成"), Theme.AccentColor.Green, GUILayout.Height(26)))
                WriteOps.CompleteAllUpgradeItems(club);
            Components.Hint(L10n.T("（技能树节点锁定 = 教练等级不足；去锁后即可在俱乐部升级界面直接点亮）"));

            Components.SectionTitle(L10n.T("观众支持度 / 晋级线"), Accent);
            Components.KeyValue(L10n.T("当前支持度"), WriteOps.SupportText(cd));
            if (Components.PrimaryButton(L10n.T("支持度拉满 (确保晋级)"), Theme.AccentColor.Green, GUILayout.Height(26)))
                WriteOps.MaxShowSupport(cd);

            Components.SectionTitle(L10n.T("俱乐部场地"), Accent);
            Components.KeyValue(L10n.T("当前等级"), club.Stadium != null ? "Lv" + club.Stadium.Setting.Level : "?");
            if (Components.PrimaryButton(L10n.T("升级到下一级场地"), Theme.AccentColor.Blue, GUILayout.Height(26)))
                WriteOps.UpgradeStadium(club);

            Components.SectionTitle(L10n.T("贷款"), Accent);
            try
            {
                var loans = club.C_Data.LoansData.loans;
                if (loans == null || loans.Count == 0)
                    Components.Hint(L10n.T("当前无贷款"));
                else
                {
                    Components.KeyValue(L10n.T("每月应付本息"), club.C_Data.LoansData.GetTotalLoans(club).ToString("N0"));
                    for (int i = 0; i < loans.Count; i++)
                    {
                        var loan = loans[i];
                        if (loan == null) continue;
                        string lvName = loan.level.ToString();
                        var captured = loan;
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label($"{lvName}: {L10n.T("剩余")} {loan.remainLoans:N0} · {L10n.T("利率")} {loan.currentRate:P1}", GUILayout.Width(300));
                            _loanInputs.TryGetValue("amt_" + i, out var amt);
                            _loanInputs.TryGetValue("rate_" + i, out var rate);
                            GUILayout.Label(L10n.T("金额"), GUILayout.Width(34));
                            amt = GUILayout.TextField(amt ?? "", GUILayout.Width(80));
                            _loanInputs["amt_" + i] = amt;
                            GUILayout.Label(L10n.T("利率"), GUILayout.Width(34));
                            rate = GUILayout.TextField(rate ?? "", GUILayout.Width(56));
                            _loanInputs["rate_" + i] = rate;
                            if (GUILayout.Button(L10n.T("应用"), GUILayout.Width(46)))
                            {
                                if (int.TryParse(amt, out int av)) WriteOps.SetLoanRemain(club, captured.level, av);
                                if (float.TryParse(rate, out float rv)) WriteOps.SetLoanRate(club, captured.level, rv);
                            }
                            GUILayout.Label("", GUILayout.ExpandWidth(true));
                        }
                    }
                }
                if (Components.PrimaryButton(L10n.T("立即还贷 (0 元还贷)"), Theme.AccentColor.Green, GUILayout.Height(26)))
                    WriteOps.RepayAllLoans(club);
            }
            catch (System.Exception ex) { Components.Hint("贷款异常: " + ex.Message); }

            Components.SectionTitle(L10n.T("训练研究"), Accent);
            Components.KeyValue(L10n.T("当前等级 / 次数"), cd.trainingResearchLevel + " / " + cd.trainingResearchCount);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(L10n.T("等级:"), GUILayout.Width(40));
                _rsLv = GUILayout.TextField(_rsLv, GUILayout.Width(60));
                GUILayout.Label(L10n.T("次数:"), GUILayout.Width(40));
                _rsCnt = GUILayout.TextField(_rsCnt, GUILayout.Width(60));
                if (GUILayout.Button(L10n.T("应用"), GUILayout.Width(46)))
                {
                    int.TryParse(_rsLv, out int lv);
                    int.TryParse(_rsCnt, out int cnt);
                    WriteOps.SetResearch(cd, lv, cnt);
                }
                Components.Hint(L10n.T("（等级受游戏配置 MaxCount 上限约束）"));
            }

            Components.SectionTitle(L10n.T("训练必成 (Harmony)"), Accent);
            _dailyVeryGood = Components.ToggleSwitch(_dailyVeryGood, L10n.T("日常训练必定大成功"));
            _abilityBetter = Components.ToggleSwitch(_abilityBetter, L10n.T("能力训练必定大成功"));
            TrainingPatches.DailyAlwaysVeryGood = _dailyVeryGood;
            TrainingPatches.AbilityAlwaysSuccess = _abilityBetter;
        }

        // ================= 荣誉 =================
        private void DrawHonors()
        {
            Components.SectionTitle(L10n.T("荣誉 / 奖杯"), Accent);
            System.Collections.Generic.List<ClubHonorData> honors = null;
            try { honors = Refs.CD.ClubHonorDatas; } catch { }
            Components.KeyValue(L10n.T("奖杯记录"), (honors != null ? honors.Count : 0) + " 条");
            if (honors != null && honors.Count > 0)
            {
                if (Components.GhostButton(_showHonors ? "收起明细" : $"展开明细（最近 {System.Math.Min(10, honors.Count)} 条）"))
                    _showHonors = !_showHonors;
                if (_showHonors)
                {
                    foreach (var h in honors.Skip(System.Math.Max(0, honors.Count - 10)).Reverse())
                    {
                        try { Components.KeyValue(L10n.T("  ") + h.Date, $"{h.Type} · Rank {h.Rank} · {h.ClubId}"); }
                        catch { Components.KeyValue(L10n.T("  荣誉"), "(解析失败)"); }
                    }
                }
            }
        }

        private static string SafeWorldPoint(Club.DataComponent cd)
        {
            try { return cd.WorldPoint.ToString("N0"); } catch { return "?"; }
        }

        private System.Collections.Generic.List<ClubMedalSetting> LoadMedals()
        {
            if (_medals == null || _medals.Count == 0)
            {
                try { _medals = SettingCenter.GetSettings<ClubMedalSetting>().ToList(); }
                catch { _medals = new System.Collections.Generic.List<ClubMedalSetting>(); }
            }
            return _medals;
        }

        private static GUIStyle _boldLabel;
        private static GUIStyle BoldLabel()
        {
            if (_boldLabel == null)
                _boldLabel = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.BoldAndItalic };
            return _boldLabel;
        }

        /// <summary>里程碑行：已完成金高亮，可领奖绿高亮。</summary>
        private void DrawMilestoneRow(Milestone m)
        {
            if (m == null) return;
            string name = WriteOps.SafeName(m.Setting);
            string state = "?";
            bool finished = false, canTake = false;
            try
            {
                state = L10n.MilestoneState(m.State);
                finished = m.State == MilestoneStates.Finished;
                canTake = m.State == MilestoneStates.CanTakeReward;
            }
            catch { }

            var rowRect = GUILayoutUtility.GetRect(0, 10000, 26, 26,
                GUILayout.Height(26 * Theme.Scale), GUILayout.ExpandWidth(true));
            if (finished)
                GUI.DrawTexture(rowRect, Theme.HGrad(
                    new Color(0.95f, 0.80f, 0.36f, 0.20f), new Color(0.95f, 0.80f, 0.36f, 0.04f)));
            else if (canTake)
                GUI.DrawTexture(rowRect, Theme.HGrad(
                    new Color(0.33f, 0.85f, 0.55f, 0.22f), new Color(0.33f, 0.85f, 0.55f, 0.05f)));
            else
                GUI.DrawTexture(rowRect, Theme.Solid(new Color(1f, 1f, 1f, 0.04f)));

            var h = rowRect.height;
            var prevC = GUI.contentColor;
            GUI.contentColor = finished ? new Color(1f, 0.92f, 0.62f) : Theme.TextPrimary;
            GUI.Label(new Rect(rowRect.x + 10, rowRect.y, 240, h), name);
            GUI.contentColor = finished ? new Color(1f, 0.92f, 0.62f)
                : canTake ? new Color(0.55f, 0.95f, 0.70f)
                : new Color(Theme.TextSecondary.r, Theme.TextSecondary.g, Theme.TextSecondary.b, 0.6f);
            GUI.Label(new Rect(rowRect.x + 260, rowRect.y, 110, h), (finished ? "✓ " : "") + state, BoldLabel());
            GUI.contentColor = prevC;

            if (!finished)
            {
                var btnRect = new Rect(rowRect.xMax - 74, rowRect.y + (h - 22 * Theme.Scale) / 2,
                    64 * Theme.Scale, 22 * Theme.Scale);
                var captured = m;
                if (GUI.Button(btnRect, "解锁", Theme.GhostBtn))
                    WriteOps.UnlockMilestone(captured);
            }
        }
    }
}
