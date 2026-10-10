using UnityEngine;
using EsportsTrainer.Core;
using EsportsTrainer.UI;
using GameMain;
using GameMain.MatchSystem;

namespace EsportsTrainer.Features
{
    /// <summary>
    /// Features/SeasonTab.cs — 赛事域：版本规则 / 训练难度 / 赛事格式 / 赛程推进。
    /// 锚点：Main.GameCustomData（GameCustom 自定义数值）。
    /// </summary>
    public class SeasonTab : ITab
    {
        public string Id => "season";
        public string Title => L10n.T("赛事");
        public Theme.AccentColor Accent => Theme.AccentColor.Green;

        private const string T = "Season";

        public void Render()
        {
            if (!Refs.HasGame) { Components.Hint(L10n.T("请先进入存档")); return; }

            // ---------- 版本规则 ----------
            Components.SectionTitle(L10n.T("版本规则"), Accent);
            var cd = GameMain.Main.GameCustomData;
            if (cd == null) { Components.Hint(L10n.T("GameCustomData 不可用")); return; }

            try
            {
                Components.KeyValue(L10n.T("规则变更模式"), cd.GetCustomValue(GameCustom.GameRuleChange, false).AsInt(0).ToString());
                if (Components.PrimaryButton(L10n.T("锁定规则 (不变)"), Theme.AccentColor.Blue, GUILayout.Height(26)))
                {
                    cd.SetCustom(GameCustom.GameRuleChange, 2, false);
                    Log.Ok(T, "版本规则已锁定 (不变模式)");
                }
            }
            catch (System.Exception ex) { Components.Hint("规则读取异常: " + ex.Message); }

            // ---------- 训练 / 难度 ----------
            Components.SectionTitle(L10n.T("训练 / 难度"), Accent);
            try
            {
                Components.KeyValue(L10n.T("训练点倍率"), cd.GetCustomValue(GameCustom.TrainPoint, false).AsInt(0).ToString());
                using (new GUILayout.HorizontalScope())
                {
                    if (Components.PrimaryButton(L10n.T("训练点 5x"), Theme.AccentColor.Green, GUILayout.Height(26)))
                    {
                        cd.SetCustom(GameCustom.TrainPoint, 5, false);
                        Log.Ok(T, "训练点倍率 → 5x");
                    }
                    if (Components.GhostButton(L10n.T("恢复默认倍率"), GUILayout.Height(26)))
                    {
                        cd.SetCustom(GameCustom.TrainPoint, 1, false);
                        cd.SetCustom(GameCustom.FanAdd, 1, false);
                        cd.SetCustom(GameCustom.TransferBudget, 1, false);
                        Log.Ok(T, "倍率已恢复默认 (训练点/粉丝/转会预算 = 1x)");
                    }
                }
            }
            catch (System.Exception ex) { Components.Hint("难度设置异常: " + ex.Message); }

            // ---------- 赛事格式 ----------
            Components.SectionTitle(L10n.T("赛事格式"), Accent);
            DrawFormat(cd, "常规赛", GameCustom.RegularMatch, 1);
            DrawFormat(cd, "季后赛", GameCustom.PlayOffsMatch, 3);
            DrawFormat(cd, "季后赛决赛", GameCustom.PlayOffsFinalMatch, 5);
            if (Components.GhostButton(L10n.T("恢复赛事格式默认 (BO1/BO3/BO5)")))
            {
                cd.SetCustom(GameCustom.RegularMatch, 1, false);
                cd.SetCustom(GameCustom.PlayOffsMatch, 3, false);
                cd.SetCustom(GameCustom.PlayOffsFinalMatch, 5, false);
                Log.Ok(T, "赛事格式已恢复默认 (常规赛 BO1 · 季后赛 BO3 · 决赛 BO5)");
            }

            // ---------- 赛程 ----------
            Components.SectionTitle(L10n.T("赛程"), Accent);
            Components.KeyValue(L10n.T("当前日期"), Refs.DateText());
            Components.DangerButton("next_week", L10n.T("推进下一周 (赛程队列入队)"), () =>
            {
                try
                {
                    Refs.G.C_Competition.NextWeek();
                    Log.Ok(T, "已推进下一周");
                }
                catch (System.Exception ex) { Log.Exception(T, "推进赛程", ex); }
            });
            Components.Hint(L10n.T("（推进赛程会在下周排入比赛，请在非比赛周使用）"));
        }

        private static void DrawFormat(GameCustomData cd, string label, GameCustom key, int defaultVal)
        {
            int cur;
            try { cur = cd.GetCustomValue(key, false).AsInt(defaultVal); } catch { cur = defaultVal; }
            Components.KeyValue(label, ((MatchFormatTypes)cur).ToString());
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(110);
                DrawFormatButton(label, "BO1", MatchFormatTypes.BO1, key, cur);
                DrawFormatButton(label, "BO3", MatchFormatTypes.BO3, key, cur);
                DrawFormatButton(label, "BO5", MatchFormatTypes.BO5, key, cur);
            }
        }

        private static void DrawFormatButton(string label, string text, MatchFormatTypes type, GameCustom key, int cur)
        {
            bool selected = (int)type == cur;
            var style = selected ? Theme.NavOn : Theme.GhostBtn;
            if (Components.StyleButton(text, style, GUILayout.Width(58 * Theme.Scale)))
            {
                if (!selected)
                {
                    try
                    {
                        GameMain.Main.GameCustomData.SetCustom(key, (int)type, false);
                        Log.Ok(T, $"{label} → {text}");
                    }
                    catch (System.Exception ex) { Log.Exception(T, "赛事格式", ex); }
                }
            }
        }
    }
}
