using UnityEngine;
using GameMain;
using GameMain.AchievementSystem;
using GameMain.AthleteSystem;
using GameMain.ClubSystem;
using Utility.SettingSystem;
using System.Linq;

namespace DebugMod
{
    public partial class Main
    {
        void EvtTab()
        {
            Box(C_TITLE_PURPLE, "【触发奖励】");
            if (_c == null) { GUILayout.Label("无游戏数据"); return; }

            // ---- 俱乐部奖励 ----
            if (Fld("evt_reward", "俱乐部奖励"))
            {
                RwdBtn("获取 10000 预算", C_BTN_ORANGE, () => _cd.GainBudget(10000));
                RwdBtn("获取 500 教练点", C_BTN_ORANGE, () => _cd.GainCoachPoint(500));
                RwdBtn("获取 500 训练点", C_BTN_ORANGE, () => _cd.GainTrainPoint(500));
                RwdBtn("全员能力值 +30", C_BTN_PURPLE, () =>
                {
                    foreach (var a in _c.Athletes)
                        foreach (AthleteAbilityFlags f in ALL_ABILITY_FLAGS)
                            a.C_Data.SetAbilityLevelAndExp(f,
                                a.C_Data.GetAbilityLevel(f, false) + 30, 0, 0);
                });
                RwdBtn("全员熟练度升至 10 级", C_BTN_PURPLE, () =>
                {
                    foreach (var a in _c.Athletes) MaxMst(a.C_Data);
                });
            }

            // ---- 奖杯 / 勋章 / 成就 ----
            if (Fld("evt_champ", "奖杯 / 勋章 / 成就"))
            {
                DrawMedalSection();
                DrawAchievementSection();
            }

            // ---- 速度控制 ----
            if (Fld("evt_speed", "速度控制"))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("全局游戏速度:", GUILayout.Width(100));
                spdI = GUILayout.TextField(spdI, GUILayout.Width(60));
                Btn("应用", C_BTN_BLUE, () =>
                {
                    if (float.TryParse(spdI, out float sv)) Time.timeScale = sv;
                });
                Btn("重置速度 (1x)", C_BTN_RED, () => Time.timeScale = 1f);
                GUILayout.EndHorizontal();
            }

            // ---- 数据工具 ----
            Box(C_TITLE_BLUE, "【数据工具】");
            Btn("导出完整数据快照 (F2)", C_BTN_BLUE, () => DumpAllGameData());
            GUILayout.Label("电竞教父修改器 v2.0 — By Mizuof");
        }

        void DrawMedalSection()
        {
            Box(new Color(0.5f, 0.3f, 0.1f), "勋章 (ClubMedal)");
            var am = _c.C_Medal?.StoreMedals?.Keys?.ToList();
            GUILayout.Label("已拥有: " + (am?.Count ?? 0) + " 个");
            if (am != null)
            {
                foreach (var m in am.Take(5))
                {
                    try { GUILayout.Label("  " + m.Desc); } catch { }
                }
            }
            RwdBtn("随机解锁一枚勋章", C_BTN_GOLD, () =>
            {
                try
                {
                    var all = SettingCenter.GetSettings<ClubMedalSetting>().ToList();
                    if (all.Count > 0)
                    {
                        int ri = Random.Range(0, all.Count);
                        _c.C_Medal.AddMedal(all[ri], true);
                    }
                }
                catch (System.Exception ex) { LoggerInstance.Msg("随机勋章失败: " + ex.Message); }
            });
        }

        void DrawAchievementSection()
        {
            Box(new Color(0.3f, 0.3f, 0.6f), "成就 (Achievement)");
            try
            {
                var achList = Achievement.Setting?.AchievementList;
                if (achList == null || achList.Count == 0)
                {
                    GUILayout.Label("  无成就数据");
                    return;
                }

                GUILayout.Label("成就总数: " + achList.Count + " 个");
                var glbData = GameMain.Main.AchievementGlobalData;
                int displayCount = 0;
                foreach (var ach in achList)
                {
                    if (ach == null || displayCount >= 5) continue;
                    int cur = glbData != null ? glbData.GetHighestRecord(ach.key) : 0;
                    bool unlocked = cur >= ach.targetValue;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(string.Format("  {0} [{1}/{2}]", ach.key, cur, ach.targetValue),
                        GUILayout.Width(150));
                    if (!unlocked)
                        Btn("解锁", C_BTN_GREEN, () => Achievement.SetAchievementValue(ach.api, ach.targetValue));
                    else
                    {
                        GUI.color = Color.green;
                        GUILayout.Label("已达成");
                        GUI.color = Color.white;
                    }
                    GUILayout.EndHorizontal();
                    displayCount++;
                }
                if (achList.Count > 5)
                    GUILayout.Label("  ... 还有 " + (achList.Count - 5) + " 个");
            }
            catch (System.Exception ex)
            {
                GUILayout.Label("  成就系统错误");
                LoggerInstance.Msg("成就读取异常: " + ex.Message);
            }

            Btn("一键解锁全部成就", C_BTN_ORANGE, () =>
            {
                try
                {
                    var achList = Achievement.Setting?.AchievementList;
                    if (achList != null)
                        foreach (var ach in achList)
                            if (ach != null) Achievement.SetAchievementValue(ach.api, ach.targetValue);
                    LoggerInstance.Msg("全部成就已解锁");
                }
                catch (System.Exception ex) { LoggerInstance.Msg("解锁成就失败: " + ex.Message); }
            });
        }
    }
}
