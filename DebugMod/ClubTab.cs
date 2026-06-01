using UnityEngine;
using GameMain;
using GameMain.ClubSystem;
using System.Linq;
using Utility.SettingSystem;

namespace DebugMod
{
    public partial class Main
    {
        void ClubTab()
        {
            Box(C_TITLE_BLUE, "【俱乐部资源】");
            if (_c == null || _cd == null) { GUILayout.Label("无游戏数据"); return; }

            Row("教练点:", _cd.GetCoachPoint().ToString(), ref cpI, () =>
            {
                if (int.TryParse(cpI, out int v)) _cd?.SetCoachPoint(v);
            });
            Row("训练点:", _cd.GetTrainPoint().ToString(), ref tpI, () =>
            {
                if (int.TryParse(tpI, out int v)) _cd?.SetTrainPoint(v);
            });
            Row("预算:", _cd.GetBudget().ToString(), ref bgI, () =>
            {
                if (int.TryParse(bgI, out int v)) _cd?.SetBudget(v);
            });

            GUILayout.Label("粉丝数: " + _c.Fans + " (通过选手粉丝增减)");
            Btn("教练点+1000", C_BTN_GREEN, () => _cd.GainCoachPoint(1000));
            Btn("训练点+1000", C_BTN_GREEN, () => _cd.GainTrainPoint(1000));
            Btn("预算+100000", C_BTN_GREEN, () => _cd.GainBudget(100000));
            Btn("全部资源最大化", C_BTN_PURPLE, () =>
            {
                _cd.SetCoachPoint(99999);
                _cd.SetTrainPoint(99999);
                _cd.SetBudget(9999999);
            });

            // ---- 勋章系统 ----
            if (Fld("medals", "勋章系统"))
            {
                var ml = _c.C_Medal?.StoreMedals.Keys;
                GUILayout.Label("已解锁: " + (ml != null ? ml.Count() + " 个" : "0 个"));
                GUILayout.BeginHorizontal();
                medI = GUILayout.TextField(medI, GUILayout.Width(100));
                Btn("搜索并添加", C_BTN_GREEN, () =>
                {
                    var all = SettingCenter.GetSettings<ClubMedalSetting>().ToList();
                    var f = all.FirstOrDefault(m2 =>
                    {
                        try { return m2.Desc == medI; }
                        catch { return false; }
                    });
                    if (f != null) _c.C_Medal.AddMedal(f, true);
                });
                GUILayout.EndHorizontal();

                if (Fld("medal_list", "所有勋章列表"))
                {
                    var all = SettingCenter.GetSettings<ClubMedalSetting>().ToList();
                    int count = Mathf.Min(all.Count, MEDAL_PAGE_SIZE);
                    for (int i = 0; i < count; i++)
                    {
                        var ms = all[i];
                        string dn = "?";
                        try { dn = ms.Desc ?? "?"; } catch { }
                        Btn("解锁: " + dn, new Color(0.3f, 0.5f, 0.2f), () => { _c.C_Medal.AddMedal(ms, true); });
                    }
                }

                Btn("随机解锁一枚勋章", C_BTN_GOLD, () =>
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
                    catch (System.Exception ex) { LoggerInstance.Msg("勋章随机解锁失败: " + ex.Message); }
                });
            }

            // ---- 训练研究 ----
            if (Fld("research", "训练研究"))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("等级:", GUILayout.Width(50));
                rsLvI = GUILayout.TextField(rsLvI, GUILayout.Width(60));
                if (GUILayout.Button("设", GUILayout.Width(40)))
                {
                    if (int.TryParse(rsLvI, out int v)) _cd.trainingResearchLevel = v;
                }
                GUILayout.Label("次数:", GUILayout.Width(50));
                rsCntI = GUILayout.TextField(rsCntI, GUILayout.Width(60));
                if (GUILayout.Button("设", GUILayout.Width(40)))
                {
                    if (int.TryParse(rsCntI, out int v)) _cd.trainingResearchCount = v;
                }
                GUILayout.EndHorizontal();
            }

            // ---- 晋级/降级 ----
            if (Fld("club_promote", "晋级 / 降级"))
            {
                try
                {
                    string curClass = _c.ClubClassType.ToString();
                    GUILayout.Label("当前级别: " + curClass);
                    Btn("晋级 (PromoteClub)", C_BTN_GREEN, () =>
                    {
                        bool ok = _c.PromoteClub();
                        LoggerInstance.Msg("晋级 " + (ok ? "成功" : "失败(已最高级)"));
                    });
                    Btn("降级 (DemoteClub)", C_BTN_RED, () =>
                    {
                        bool ok = _c.DemoteClub();
                        LoggerInstance.Msg("降级 " + (ok ? "成功" : "失败(已最低级)"));
                    });
                    Btn("教练晋升 (PromoteCoach)", C_BTN_BLUE, () =>
                    {
                        bool ok = _c.PromoteCoach();
                        LoggerInstance.Msg("教练晋升 " + (ok ? "成功" : "失败"));
                    });
                }
                catch (System.Exception ex) { GUILayout.Label("晋级系统错误: " + ex.Message); }
            }
        }
    }
}
