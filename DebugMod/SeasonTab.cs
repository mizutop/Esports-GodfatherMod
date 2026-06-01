using UnityEngine;
using GameMain;
using GameMain.MatchSystem;

namespace DebugMod
{
    public partial class Main
    {
        void SeasTab()
        {
            Box(C_TITLE_BLUE, "【赛事 / 版本】");
            if (_g == null) return;

            // ---- 版本规则 ----
            if (Fld("rule", "版本规则"))
            {
                var cd = GameMain.Main.GameCustomData;
                if (cd != null)
                {
                    var grc = cd.GetCustomValue(GameCustom.GameRuleChange, false);
                    GUILayout.Label("规则变更模式: " + grc.AsInt(0));
                    Btn("锁定规则 (不变)", C_BTN_BLUE, () =>
                    {
                        if (cd != null) cd.SetCustom(GameCustom.GameRuleChange, 2, false);
                    });
                }
            }

            // ---- 训练 / 难度 ----
            if (Fld("train", "训练 / 难度设置"))
            {
                var cd = GameMain.Main.GameCustomData;
                if (cd != null)
                {
                    var tp = cd.GetCustomValue(GameCustom.TrainPoint, false);
                    GUILayout.Label("训练点倍率: " + tp.AsInt(0));
                    Btn("设为最大值 (5x)", C_BTN_GREEN, () =>
                    {
                        cd.SetCustom(GameCustom.TrainPoint, 5, false);
                    });
                    Btn("恢复默认", C_BTN_RED, () =>
                    {
                        cd.SetCustom(GameCustom.TrainPoint, 1, false);
                        cd.SetCustom(GameCustom.FanAdd, 1, false);
                        cd.SetCustom(GameCustom.TransferBudget, 1, false);
                    });
                }
            }

            // ---- 赛事格式 (新功能) ----
            if (Fld("match_format", "赛事格式"))
            {
                DrawMatchFormat();
            }

            GUILayout.Space(10);
            GUILayout.Label("版本规则修改: 需要配合 FickleRulesManager");
        }

        void DrawMatchFormat()
        {
            try
            {
                // 读取当前设置 — 通过 GameCustomData 而非 Game 直接调用
                var cd = GameMain.Main.GameCustomData;
                int regularVal = cd != null ? cd.GetCustomValue(GameCustom.RegularMatch, false).AsInt(1) : 1;
                int playoffVal = cd != null ? cd.GetCustomValue(GameCustom.PlayOffsMatch, false).AsInt(3) : 3;
                int playoffFinalVal = cd != null ? cd.GetCustomValue(GameCustom.PlayOffsFinalMatch, false).AsInt(5) : 5;

                var currentRegular = (MatchFormatTypes)regularVal;
                var currentPlayoff = (MatchFormatTypes)playoffVal;
                var currentFinal = (MatchFormatTypes)playoffFinalVal;

                GUILayout.Label(string.Format("常规赛: {0}", currentRegular));
                GUILayout.BeginHorizontal();
                DrawFormatButton("BO1", MatchFormatTypes.BO1, GameCustom.RegularMatch);
                DrawFormatButton("BO3", MatchFormatTypes.BO3, GameCustom.RegularMatch);
                DrawFormatButton("BO5", MatchFormatTypes.BO5, GameCustom.RegularMatch);
                GUILayout.EndHorizontal();

                GUILayout.Space(3);
                GUILayout.Label(string.Format("季后赛: {0}", currentPlayoff));
                GUILayout.BeginHorizontal();
                DrawFormatButton("BO1", MatchFormatTypes.BO1, GameCustom.PlayOffsMatch);
                DrawFormatButton("BO3", MatchFormatTypes.BO3, GameCustom.PlayOffsMatch);
                DrawFormatButton("BO5", MatchFormatTypes.BO5, GameCustom.PlayOffsMatch);
                GUILayout.EndHorizontal();

                GUILayout.Space(3);
                GUILayout.Label(string.Format("季后赛决赛: {0}", currentFinal));
                GUILayout.BeginHorizontal();
                DrawFormatButton("BO1", MatchFormatTypes.BO1, GameCustom.PlayOffsFinalMatch);
                DrawFormatButton("BO3", MatchFormatTypes.BO3, GameCustom.PlayOffsFinalMatch);
                DrawFormatButton("BO5", MatchFormatTypes.BO5, GameCustom.PlayOffsFinalMatch);
                GUILayout.EndHorizontal();

                Btn("恢复赛事格式默认值", C_BTN_RED, () =>
                {
                    var cd = GameMain.Main.GameCustomData;
                    if (cd != null)
                    {
                        cd.SetCustom(GameCustom.RegularMatch, 1, false);       // BO1
                        cd.SetCustom(GameCustom.PlayOffsMatch, 3, false);      // BO3
                        cd.SetCustom(GameCustom.PlayOffsFinalMatch, 5, false); // BO5
                    }
                });
            }
            catch (System.Exception ex)
            {
                GUILayout.Label("  赛事格式读取失败");
                LoggerInstance.Msg("赛事格式异常: " + ex.Message);
            }
        }

        void DrawFormatButton(string label, MatchFormatTypes type, GameCustom customKey)
        {
            GUI.backgroundColor = new Color(0.3f, 0.4f, 0.6f);
            if (GUILayout.Button(label, GUILayout.Width(50)))
            {
                var cd = GameMain.Main.GameCustomData;
                if (cd != null)
                {
                    cd.SetCustom(customKey, (int)type, false);
                    LoggerInstance.Msg(string.Format("赛事 {0} 已设为 {1}", customKey, label));
                }
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
