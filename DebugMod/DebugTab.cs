using UnityEngine;
using GameMain;
using GameMain.AthleteSystem;
using GameMain.ClubSystem;
using GameMain.UnitSystem;
using GameMain.MatchSystem;
using GameMain.UnitSystem.Equipment;
using GameMain.AchievementSystem;
using System.Linq;
using Utility.SettingSystem;

namespace DebugMod
{
    public partial class Main
    {
        private Vector2 debugScrollPos;
        private string debugLog = "";

        void DebugTab()
        {
            Box(C_TITLE_BLUE, "【功能逐项测试面板】");
            GUILayout.Label("点击按钮执行对应功能，结果输出到 MelonLoader 控制台");
            GUILayout.Space(5);

            debugScrollPos = GUILayout.BeginScrollView(debugScrollPos, GUILayout.ExpandHeight(true));

            DrawTestSection("俱乐部 — 资源", C_TITLE_BLUE, () =>
            {
                if (_c == null) { Log("❌ 无游戏数据"); return; }
                TestBtn("设置教练点=99999", () => { _cd.SetCoachPoint(99999); Log("✅ 教练点已设为 99999"); });
                TestBtn("设置训练点=99999", () => { _cd.SetTrainPoint(99999); Log("✅ 训练点已设为 99999"); });
                TestBtn("设置预算=9999999", () => { _cd.SetBudget(9999999); Log("✅ 预算已设为 9999999"); });
                TestBtn("教练点+1000", () => { _cd.GainCoachPoint(1000); Log("✅ 教练点+1000，当前=" + _cd.GetCoachPoint()); });
                TestBtn("训练点+1000", () => { _cd.GainTrainPoint(1000); Log("✅ 训练点+1000，当前=" + _cd.GetTrainPoint()); });
                TestBtn("预算+100000", () => { _cd.GainBudget(100000); Log("✅ 预算+100000，当前=" + _cd.GetBudget()); });
                TestBtn("读取粉丝数", () => { Log("ℹ️ 粉丝数=" + _c.Fans); });
            });

            DrawTestSection("俱乐部 — 勋章", C_TITLE_BLUE, () =>
            {
                if (_c == null) { Log("❌ 无游戏数据"); return; }
                TestBtn("列出已解锁勋章", () =>
                {
                    var ml = _c.C_Medal?.StoreMedals.Keys;
                    if (ml != null) { Log("✅ 已解锁 " + ml.Count() + " 个勋章:"); foreach (var m in ml) try { Log("  - " + m.Desc); } catch { } }
                    else Log("❌ 无勋章数据");
                });
                TestBtn("随机解锁一枚勋章", () =>
                {
                    var all = SettingCenter.GetSettings<ClubMedalSetting>().ToList();
                    if (all.Count > 0) { int ri = Random.Range(0, all.Count); _c.C_Medal.AddMedal(all[ri], true); Log("✅ 已解锁随机勋章"); }
                    else Log("❌ 无可用勋章配置");
                });
                TestBtn("列举所有可用勋章", () =>
                {
                    var all = SettingCenter.GetSettings<ClubMedalSetting>().ToList();
                    Log("ℹ️ 共 " + all.Count + " 个可用勋章:");
                    foreach (var m in all.Take(20)) try { Log("  " + m.Desc); } catch { }
                    if (all.Count > 20) Log("  ... 还有 " + (all.Count - 20) + " 个");
                });
            });

            DrawTestSection("俱乐部 — 训练研究", C_TITLE_BLUE, () =>
            {
                if (_c == null) { Log("❌ 无游戏数据"); return; }
                TestBtn("设研究等级=10", () => { _cd.trainingResearchLevel = 10; Log("✅ 研究等级=10"); });
                TestBtn("设研究次数=99", () => { _cd.trainingResearchCount = 99; Log("✅ 研究次数=99"); });
                TestBtn("读取研究等级/次数", () => { Log("ℹ️ 等级=" + _cd.trainingResearchLevel + " 次数=" + _cd.trainingResearchCount); });
            });

            DrawTestSection("俱乐部 — 晋级/降级", C_TITLE_BLUE, () =>
            {
                if (_c == null) { Log("❌ 无游戏数据"); return; }
                TestBtn("读取当前级别", () => { Log("ℹ️ 当前级别=" + _c.ClubClassType); });
                TestBtn("晋级 (PromoteClub)", () => { bool ok = _c.PromoteClub(); Log(ok ? "✅ 晋级成功, 新级别=" + _c.ClubClassType : "❌ 晋级失败(已最高级)"); });
                TestBtn("降级 (DemoteClub)", () => { bool ok = _c.DemoteClub(); Log(ok ? "✅ 降级成功, 新级别=" + _c.ClubClassType : "❌ 降级失败(已最低级)"); });
                TestBtn("教练晋升 (PromoteCoach)", () => { bool ok = _c.PromoteCoach(); Log(ok ? "✅ 教练晋升成功" : "❌ 教练晋升失败"); });
            });

            DrawTestSection("队员 — 基础信息", new Color(0.2f, 0.4f, 0.6f), () =>
            {
                if (_c == null) { Log("❌ 无游戏数据"); return; }
                var ats = _c.Athletes.ToList();
                if (ats.Count == 0) { Log("❌ 无选手"); return; }
                for (int i = 0; i < ats.Count && i < 5; i++)
                {
                    var a = ats[i];
                    int idx = i;
                    string roleName = a.HeroRole.ToString();
                    TestBtn("选手[" + idx + "] " + roleName, () =>
                    {
                        try { Log("ℹ️ 名称=" + (a.GetName() ?? "?") + " 角色=" + a.HeroRole + " 立绘=" + (a.Portrait_Head ?? "无")); } catch { Log("❌ 读取失败"); }
                    });
                }
                if (ats.Count > 5) Log("  ... 还有 " + (ats.Count - 5) + " 个选手");

                TestBtn("随机立绘 [当前页]", () =>
                {
                    var a = ats[athPg];
                    if (a != null) { Athlete.RandomSetNameAndPortrait(a, _c.IsPlayer, GameMain.Gender.None, true); Log("✅ 已随机更换立绘"); }
                });
            });

            DrawTestSection("队员 — 能力", new Color(0.2f, 0.4f, 0.6f), () =>
            {
                if (_c == null) { Log("❌ 无游戏数据"); return; }
                var ats = _c.Athletes.ToList();
                if (ats.Count == 0) { Log("❌ 无选手"); return; }
                var a = ats[athPg];
                TestBtn("读取5项能力值", () =>
                {
                    var ad = a.C_Data;
                    Log(string.Format("ℹ️ Ag={0} De={1} Fa={2} Re={3} Ex={4}",
                        ad.GetAbilityLevel(AthleteAbilityFlags.Aggressive, false),
                        ad.GetAbilityLevel(AthleteAbilityFlags.Defence, false),
                        ad.GetAbilityLevel(AthleteAbilityFlags.Farming, false),
                        ad.GetAbilityLevel(AthleteAbilityFlags.Reflection, false),
                        ad.GetAbilityLevel(AthleteAbilityFlags.Exercise, false)));
                });
                TestBtn("当前选手全满", () => { MaxAth(a.C_Data); Log("✅ 当前选手能力全满"); });
                TestBtn("全部选手全满", () => { if (_c != null) foreach (var aa in _c.Athletes) MaxAth(aa.C_Data); Log("✅ 全部选手已全满"); });
            });

            DrawTestSection("队员 — 英雄池/熟练度", new Color(0.2f, 0.4f, 0.6f), () =>
            {
                if (_c == null) { Log("❌ 无游戏数据"); return; }
                var ats = _c.Athletes.ToList();
                if (ats.Count == 0) { Log("❌ 无选手"); return; }
                var a = ats[athPg]; var ad = a.C_Data;
                TestBtn("读取英雄池", () =>
                {
                    Log("ℹ️ 英雄池: " + ad.GetHeroNum() + "/" + ad.GetHeroPoolMax());
                    foreach (var k in ad.heroDataDict.Keys)
                    {
                        var hd = ad.heroDataDict[k];
                        string hn = "?"; try { var np = k.GetType().GetProperty("Name"); if (np != null) hn = np.GetValue(k, null)?.ToString() ?? "?"; } catch { }
                        Log("  - " + hn + " 等级=" + hd.level);
                    }
                });
                TestBtn("全部升至宗师", () => { MaxMst(ad); Log("✅ 全部升至宗师"); });
                TestBtn("清空英雄池", () => { ad.ClearHeroData(); Log("✅ 英雄池已清空"); });
            });

            DrawTestSection("队员 — 性格/心情", new Color(0.2f, 0.4f, 0.6f), () =>
            {
                if (_c == null) { Log("❌ 无游戏数据"); return; }
                var ats = _c.Athletes.ToList();
                if (ats.Count == 0) { Log("❌ 无选手"); return; }
                var a = ats[athPg]; var ad = a.C_Data;
                TestBtn("读取性格列表", () =>
                {
                    if (ad.Personalities != null && ad.Personalities.Count > 0)
                    { foreach (var p in ad.Personalities) { if (p == null) continue; string pn = "?"; try { pn = p.Setting?.Name ?? "?"; } catch { } Log("  - " + pn); } }
                    else Log("ℹ️ 无性格");
                });
                TestBtn("读取心情", () =>
                {
                    string emo = "无"; try { emo = ad.CurrentEmotion?.Setting?.Type.ToString() ?? "无"; } catch { }
                    Log("ℹ️ 心情=" + emo);
                });
                TestBtn("随机变换心情", () => { a.CreateAthleteEmotion(); Log("✅ 心情已随机变换"); });
                TestBtn("清除心情", () => { a.OnRemoveAthleteEmotion(); Log("✅ 心情已清除"); });
                TestBtn("清除所有性格", () => { if (ad.Personalities != null) { Utility.PoolSystem.SimplePools.ClearCollection<AthletePersonality>(ad.Personalities); Log("✅ 性格已清除"); } });
            });

            DrawTestSection("队员 — 潜在/魅力/适应", new Color(0.2f, 0.4f, 0.6f), () =>
            {
                if (_c == null) { Log("❌ 无游戏数据"); return; }
                var ats = _c.Athletes.ToList();
                if (ats.Count == 0) { Log("❌ 无选手"); return; }
                var a = ats[athPg]; var ad = a.C_Data;
                TestBtn("读取全部潜力", () =>
                {
                    foreach (AthleteAbilityFlags f in new[] { AthleteAbilityFlags.Aggressive, AthleteAbilityFlags.Defence, AthleteAbilityFlags.Farming, AthleteAbilityFlags.Reflection, AthleteAbilityFlags.Exercise })
                    { var r = ad.GetAbilityPotentialRank(f); Log("  " + f + "=" + r); }
                });
                TestBtn("全部潜力 S+", () =>
                {
                    foreach (AthleteAbilityFlags f in new[] { AthleteAbilityFlags.Aggressive, AthleteAbilityFlags.Defence, AthleteAbilityFlags.Farming, AthleteAbilityFlags.Reflection, AthleteAbilityFlags.Exercise })
                        ad.SetAbilityPotentialRank(f, AbilityPotentialRank.S_Plus);
                    Log("✅ 全部潜力设为 S+");
                });
                TestBtn("读取魅力", () => { Log("ℹ️ 魅力=" + ad.GetCharm()); });
                TestBtn("魅力设 S", () => { ad.SetCharm(AthleteCharmRank.S); Log("✅ 魅力=S"); });
                TestBtn("读取全部适应性", () =>
                {
                    foreach (HeroClassTypes hc in new[] { HeroClassTypes.Tank, HeroClassTypes.Warrior, HeroClassTypes.Assassin, HeroClassTypes.Magician, HeroClassTypes.Shooter, HeroClassTypes.Support })
                        Log("  " + hc + "=" + ad.GetHeroAdaption(hc));
                });
                TestBtn("全部适应性 S", () =>
                {
                    foreach (HeroClassTypes hc in new[] { HeroClassTypes.Tank, HeroClassTypes.Warrior, HeroClassTypes.Assassin, HeroClassTypes.Magician, HeroClassTypes.Shooter, HeroClassTypes.Support })
                        ad.SetHeroAdaption(hc, HeroClassAdaption.S);
                    Log("✅ 全部适应性=S");
                });
            });

            DrawTestSection("队员 — 战斗数据/全能选手", new Color(0.2f, 0.4f, 0.6f), () =>
            {
                if (_c == null) { Log("❌ 无游戏数据"); return; }
                var ats = _c.Athletes.ToList();
                if (ats.Count == 0) { Log("❌ 无选手"); return; }
                var a = ats[athPg];
                TestBtn("读取对战数据", () =>
                {
                    try
                    {
                        var bd = a.GetType().GetField("battleData")?.GetValue(a);
                        if (bd == null) { Log("❌ 无 battleData"); return; }
                        foreach (var bf in new[] { "tacticPoint", "athleteFightSpeed", "athleteAccuracyFight", "athleteEvasionFight", "athleteAccuracyBattle", "athleteEvasionBattle", "athleteEcoEfficiency", "athleteEcoTransferEfficiency" })
                        { var f = bd.GetType().GetField(bf); if (f != null) Log("  " + bf + "=" + f.GetValue(bd)); }
                    }
                    catch (System.Exception ex) { Log("❌ " + ex.Message); }
                });
                TestBtn("全能选手 (600/600/18/5/10)", () =>
                {
                    try { Athlete.RecreateAthleteByAbilityValue(a, 600, 600, 18, 5, 10); Log("✅ 全能选手创建成功"); }
                    catch (System.Exception ex) { Log("❌ " + ex.Message); }
                });
            });

            DrawTestSection("队员 — 合同", new Color(0.2f, 0.4f, 0.6f), () =>
            {
                if (_c == null) { Log("❌ 无游戏数据"); return; }
                var ats = _c.Athletes.ToList();
                if (ats.Count == 0) { Log("❌ 无选手"); return; }
                var a = ats[athPg];
                TestBtn("读取合同信息", () =>
                {
                    var c = a.C_Data.Contract;
                    if (c == null) { Log("❌ 无合同数据"); return; }
                    Log("ℹ️ PriceBase=" + c.PriceBase + " RemainSeason=" + c.RemainSeasonCount + " IsFree=" + c.IsFree + " IsTrial=" + c.IsTrialContract + " IsBreak=" + c.IsBreak);
                });
                TestBtn("生成默认合同", () =>
                {
                    try { if (a.C_Data.Contract != null) AthleteContract.Despawn(a.C_Data.Contract, true); AthleteContract.SpawnDefault(a); Log("✅ 默认合同已生成"); }
                    catch (System.Exception ex) { Log("❌ " + ex.Message); }
                });
                TestBtn("续约 (SpawnByContinue)", () =>
                {
                    try
                    {
                        var c = a.C_Data.Contract;
                        if (c == null) { AthleteContract.SpawnDefault(a); Log("✅ 无合同→生成默认"); }
                        else { AthleteContract.SpawnByContinue(c); Log("✅ 续约完成"); }
                    }
                    catch (System.Exception ex) { Log("❌ " + ex.Message); }
                });
                TestBtn("设为自由合同", () =>
                {
                    try { var c = a.C_Data.Contract; if (c != null) { var p = c.GetType().GetProperty("IsFree", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); if (p != null) p.SetValue(c, true, null); Log("✅ 已设为自由合同"); } }
                    catch (System.Exception ex) { Log("❌ " + ex.Message); }
                });
            });

            DrawTestSection("队员 — 装备", new Color(0.2f, 0.4f, 0.6f), () =>
            {
                if (_b == null) { Log("⏸️ 装备需要战斗中测试 (跳过)"); return; }
                var ats = _c.Athletes.ToList();
                if (ats.Count == 0) { Log("❌ 无选手"); return; }
                var a = ats[athPg];
                var unit = FindAthleteUnit(a);
                if (unit == null) { Log("❌ 该选手未在战斗中上场"); return; }
                var eq = unit.C_Equipment;
                TestBtn("读取装备列表", () =>
                {
                    Log("ℹ️ 当前装备 " + eq.AllEquipments.Count + " 件");
                    foreach (var e in eq.AllEquipments) { try { Log("  - " + (e.Setting?.Name ?? "?")); } catch { } }
                });
                TestBtn("列出所有可用装备 (日志)", () =>
                {
                    var all = SettingCenter.GetSettings<HeroEquipmentSetting>().ToList();
                    Log("ℹ️ 共 " + all.Count + " 种装备:");
                    foreach (var s in all.Take(20)) { try { Log("  " + (s.Name ?? "?")); } catch { } }
                    if (all.Count > 20) Log("  ... 还有 " + (all.Count - 20) + " 种");
                });
            });

            DrawTestSection("战斗 — 基础信息", C_TITLE_GREEN, () =>
            {
                if (_b == null) { Log("⏸️ 无战斗中 (跳过)"); return; }
                TestBtn("读取战斗状态", () =>
                {
                    Log("ℹ️ Round=" + _b.Rounds + " Speed=" + _b.C_Data.GetFightSpeed());
                    Log("  Team0 Heroes=" + _b.Team0.Heroes.Count() + " Energy=" + _b.Team0.EnergyCurrent);
                    Log("  Team1 Heroes=" + _b.Team1.Heroes.Count() + " Energy=" + _b.Team1.EnergyCurrent);
                });
                TestBtn("读取己方单位 HP/盾/金", () =>
                {
                    foreach (var u in _b.Team0.Heroes) { if (u == null) continue; Log("  HP=" + u.HPCache + "/" + u.HPMaxCache + " 盾=" + u.Shield + " 金=" + u.Gold); }
                });
                TestBtn("读取对方单位 HP/盾/金", () =>
                {
                    foreach (var u in _b.Team1.Heroes) { if (u == null) continue; Log("  HP=" + u.HPCache + "/" + u.HPMaxCache + " 盾=" + u.Shield + " 金=" + u.Gold); }
                });
            });

            DrawTestSection("战斗 — 作弊", C_TITLE_GREEN, () =>
            {
                if (_b == null) { Log("⏸️ 无战斗中 (跳过)"); return; }
                TestBtn("己方无限生命 (单次)", () => { ApplyUnitMods(_b.Team0, true, false, false, false, false); Log("✅ 己方 HP 已设=" + cvHp); });
                TestBtn("己方无限护盾 (单次)", () => { ApplyUnitMods(_b.Team0, false, true, false, false, false); Log("✅ 己方护盾已设=" + cvShield); });
                TestBtn("己方无限金钱 (单次)", () => { ApplyUnitMods(_b.Team0, false, false, false, true, false); Log("✅ 己方金钱已设=" + cvGold); });
                TestBtn("对方清除护盾 (单次)", () => { ApplyUnitMods(_b.Team1, false, false, true, false, false); Log("✅ 对方护盾已清除"); });
                TestBtn("对方清除金钱 (单次)", () => { ApplyUnitMods(_b.Team1, false, false, false, false, true); Log("✅ 对方金钱已清除"); });
            });

            DrawTestSection("战斗 — ForceWin", C_TITLE_GREEN, () =>
            {
                if (_b == null) { Log("⏸️ 无战斗中 (跳过)"); return; }
                TestBtn("强制获胜 (基地击杀 Suicide)", () =>
                {
                    try { ForceWinBySuicide(_b, true); Log("✅ 敌方基地已击杀，游戏自然判定获胜"); }
                    catch (System.Exception ex) { Log("❌ " + ex.Message); }
                });
                TestBtn("强制失败 (基地击杀 Suicide)", () =>
                {
                    try { ForceWinBySuicide(_b, false); Log("✅ 己方基地已击杀，游戏自然判定失败"); }
                    catch (System.Exception ex) { Log("❌ " + ex.Message); }
                });
                TestBtn("开启 ForceWin 每帧锁定(获胜)", () => { _forceWinActive = true; _forceWinPlayer = true; _forceWinFrameCount = 0; Log("✅ ForceWin 锁定获胜已激活 (每帧 Suicide)"); });
                TestBtn("关闭 ForceWin 锁定", () => { _forceWinActive = false; Log("✅ ForceWin 锁定已取消"); });
                TestBtn("设置战斗速度 10x", () => { _b.C_Data.SetFightSpeed(10); Log("✅ 战斗速度=10x"); });
                TestBtn("设置战斗速度 1x", () => { _b.C_Data.SetFightSpeed(1); Log("✅ 战斗速度=1x"); });
            });

            DrawTestSection("战斗 — 手牌/BP", C_TITLE_GREEN, () =>
            {
                if (_b == null) { Log("⏸️ 无战斗中 (跳过)"); return; }
                var team = _b.Team0;
                TestBtn("读取手牌/卡池数量", () =>
                {
                    int hc = team.C_Cards?.HandCards?.GetCards()?.Count() ?? 0;
                    int ac = team.C_Cards?.AllCards?.Count ?? 0;
                    Log("ℹ️ 手牌=" + hc + " 总卡池=" + ac);
                });
                TestBtn("补满手牌", () => { FillHand(team); });
                TestBtn("清空手牌", () => { ClearHand(team); });
                TestBtn("设置所有卡费用=0", () => { SetAllCost(team, 0); Log("✅ 所有卡费用=0"); });
                TestBtn("开启 BP 加速", () => { _bpSpeedUp = true; Log("✅ BP 加速已开启"); });
                TestBtn("关闭 BP 加速", () => { _bpSpeedUp = false; Log("✅ BP 加速已关闭"); });
            });

            DrawTestSection("赛事", C_TITLE_BLUE, () =>
            {
                if (_g == null) { Log("❌ 无游戏数据"); return; }
                TestBtn("锁定规则不变", () =>
                {
                    var cd = GameMain.Main.GameCustomData;
                    if (cd != null) { cd.SetCustom(GameCustom.GameRuleChange, 2, false); Log("✅ 规则已锁定"); }
                });
                TestBtn("读取常规赛格式", () =>
                {
                    var cd = GameMain.Main.GameCustomData;
                    if (cd != null) { int v = cd.GetCustomValue(GameCustom.RegularMatch, false).AsInt(1); Log("ℹ️ 常规赛格式=" + (MatchFormatTypes)v); }
                });
                TestBtn("设为 BO1/BO3/BO5", () =>
                {
                    var cd = GameMain.Main.GameCustomData;
                    if (cd != null)
                    {
                        cd.SetCustom(GameCustom.RegularMatch, 1, false);
                        cd.SetCustom(GameCustom.PlayOffsMatch, 3, false);
                        cd.SetCustom(GameCustom.PlayOffsFinalMatch, 5, false);
                        Log("✅ 常规赛=BO1 季后赛=BO3 决赛=BO5");
                    }
                });
                TestBtn("训练点倍率=5x", () =>
                {
                    var cd = GameMain.Main.GameCustomData;
                    if (cd != null) { cd.SetCustom(GameCustom.TrainPoint, 5, false); Log("✅ 训练点倍率=5x"); }
                });
                TestBtn("训练点倍率=1x(默认)", () =>
                {
                    var cd = GameMain.Main.GameCustomData;
                    if (cd != null) { cd.SetCustom(GameCustom.TrainPoint, 1, false); Log("✅ 训练点倍率=1x"); }
                });
            });

            DrawTestSection("事件 — 俱乐部奖励", C_TITLE_PURPLE, () =>
            {
                if (_c == null) { Log("❌ 无游戏数据"); return; }
                TestBtn("获取 10000 预算", () => { _cd.GainBudget(10000); Log("✅ 预算+10000"); });
                TestBtn("获取 500 教练点", () => { _cd.GainCoachPoint(500); Log("✅ 教练点+500"); });
                TestBtn("获取 500 训练点", () => { _cd.GainTrainPoint(500); Log("✅ 训练点+500"); });
                TestBtn("全员能力+30", () =>
                {
                    foreach (var a in _c.Athletes)
                        foreach (AthleteAbilityFlags f in new[] { AthleteAbilityFlags.Aggressive, AthleteAbilityFlags.Defence, AthleteAbilityFlags.Farming, AthleteAbilityFlags.Reflection, AthleteAbilityFlags.Exercise })
                            a.C_Data.SetAbilityLevelAndExp(f, a.C_Data.GetAbilityLevel(f, false) + 30, 0, 0);
                    Log("✅ 全员能力+30");
                });
            });

            DrawTestSection("事件 — 成就", C_TITLE_PURPLE, () =>
            {
                TestBtn("读取成就列表", () =>
                {
                    try
                    {
                        var achList = Achievement.Setting?.AchievementList;
                        if (achList == null || achList.Count == 0) { Log("❌ 无成就数据"); return; }
                        var glbData = GameMain.Main.AchievementGlobalData;
                        Log("ℹ️ 共 " + achList.Count + " 个成就:");
                        foreach (var ach in achList.Take(10))
                        {
                            if (ach == null) continue;
                            int cur = glbData != null ? glbData.GetHighestRecord(ach.key) : 0;
                            Log("  " + ach.key + " [" + cur + "/" + ach.targetValue + "] " + (cur >= ach.targetValue ? "✅" : "❌"));
                        }
                    }
                    catch (System.Exception ex) { Log("❌ " + ex.Message); }
                });
                TestBtn("一键解锁全部成就", () =>
                {
                    try { var achList = Achievement.Setting?.AchievementList; if (achList != null) { foreach (var ach in achList) if (ach != null) Achievement.SetAchievementValue(ach.api, ach.targetValue); } Log("✅ 全部成就已解锁"); }
                    catch (System.Exception ex) { Log("❌ " + ex.Message); }
                });
            });

            DrawTestSection("游戏内对话框测试", new Color(0.5f, 0.3f, 0.6f), () =>
            {
                if (UIManager.View_MessageManager == null) { Log("⏸️ UIManager 未初始化 (跳过)"); return; }
                var mm = UIManager.View_MessageManager;

                TestBtn("ShowBlueTip — 转会确认", () =>
                {
                    mm.ShowBlueTip("Transfer_Sign", () => Log("✅ BlueTip 确认"), false);
                    Log("ℹ️ BlueTip 已弹出 (Transfer_Sign)");
                });
                TestBtn("ShowBlueTip — 确认删除", () =>
                {
                    mm.ShowBlueTip("ConfirmDelete", () => Log("✅ BlueTip 确认 (删除)"), false);
                    Log("ℹ️ BlueTip 已弹出 (ConfirmDelete)");
                });
                TestBtn("ShowBlueTip — 仅确认(无取消)", () =>
                {
                    mm.ShowBlueTip("LinkSteamFail", null, true);
                    Log("ℹ️ BlueTip 已弹出 (LinkSteamFail, 仅确认)");
                });
                TestBtn("ShowBlueTip — 跳过转会", () =>
                {
                    mm.ShowBlueTip("Transfer_Skip", () => Log("✅ BlueTip 确认 (跳过)"), false);
                    Log("ℹ️ BlueTip 已弹出 (Transfer_Skip)");
                });
                TestBtn("ShowBlueTip — 无选手", () =>
                {
                    mm.ShowBlueTip("NoAthlete", null, true);
                    Log("ℹ️ BlueTip 已弹出 (NoAthlete, 仅确认)");
                });
                TestBtn("ShowRedTip — 重置生涯", () =>
                {
                    mm.ShowRedTip("CareerResetTip", () => Log("✅ RedTip 确认 (重置)"), false);
                    Log("ℹ️ RedTip 已弹出 (CareerResetTip)");
                });
                TestBtn("ShowRedTip — 重置统计", () =>
                {
                    mm.ShowRedTip("StatResetTip", null, true);
                    Log("ℹ️ RedTip 已弹出 (StatResetTip, 仅确认)");
                });
                TestBtn("ShowPurpleTip — 买入蓝图", () =>
                {
                    mm.ShowPurpleTip("ConfirmBuyBluePrint", () => Log("✅ PurpleTip 确认"), false);
                    Log("ℹ️ PurpleTip 已弹出 (ConfirmBuyBluePrint)");
                });
                TestBtn("ShowPurpleTip — 签约", () =>
                {
                    mm.ShowPurpleTip("Transfer_Sign", () => Log("✅ PurpleTip 确认 (签约)"), false);
                    Log("ℹ️ PurpleTip 已弹出 (Transfer_Sign)");
                });
                TestBtn("ShowPurpleTip — Mod保存", () =>
                {
                    mm.ShowPurpleTip("ModSaveTip", () => Log("✅ PurpleTip 确认 (保存)"), false);
                    Log("ℹ️ PurpleTip 已弹出 (ModSaveTip)");
                });
                TestBtn("ShowPurpleTip — 仅确认(无DLC)", () =>
                {
                    mm.ShowPurpleTip("NoDlc", null, true);
                    Log("ℹ️ PurpleTip 已弹出 (NoDlc, 仅确认)");
                });
                TestBtn("ShowPurpleListTip — 自定义规则警告", () =>
                {
                    var list = new System.Collections.Generic.List<string> { "测试项 1: 规则变更", "测试项 2: 难度调整", "测试项 3: 版本锁定" };
                    mm.ShowPurpleListTip("CustomWarningTitle", list, () => Log("✅ PurpleListTip 确认"), false);
                    Log("ℹ️ PurpleListTip 已弹出 (CustomWarningTitle, 3条列表)");
                });
                TestBtn("ShowPurpleListTip — 缺少Mod", () =>
                {
                    var list = new System.Collections.Generic.List<string> { "Mod_A.dll", "Mod_B.dll", "Mod_C.dll" };
                    mm.ShowPurpleListTip("LackMod", list, () => Log("✅ PurpleListTip 确认 (Mod列表)"), false);
                    Log("ℹ️ PurpleListTip 已弹出 (LackMod, 3个Mod)");
                });
                TestBtn("ShowOrangeTip — 重赛/回顾", () =>
                {
                    mm.ShowOrangeTip("RePlayTheMatch",
                        () => Log("✅ OrangeTip 确认 (重赛)"),
                        () => Log("✅ OrangeTip 取消 (回顾)"), false);
                    Log("ℹ️ OrangeTip 已弹出 (RePlayTheMatch, 带取消回调)");
                });
                TestBtn("ShowOrangeTip — 仅确认(跳过特质)", () =>
                {
                    mm.ShowOrangeTip("SkipNewTraitSet", () => Log("✅ OrangeTip 确认 (跳过)"), false);
                    Log("ℹ️ OrangeTip 已弹出 (SkipNewTraitSet, 仅确认)");
                });
                TestBtn("ShowOrangeTip — 跳过Rogue比赛", () =>
                {
                    mm.ShowOrangeTip("SkipRogueMatch", () => Log("✅ OrangeTip 确认"), false);
                    Log("ℹ️ OrangeTip 已弹出 (SkipRogueMatch)");
                });
            });

            DrawTestSection("速度控制", C_TITLE_BLUE, () =>
            {
                TestBtn("全局速度 5x", () => { Time.timeScale = 5f; Log("✅ Time.timeScale=5x"); });
                TestBtn("全局速度 1x(正常)", () => { Time.timeScale = 1f; Log("✅ Time.timeScale=1x"); });
                TestBtn("全局速度 10x", () => { Time.timeScale = 10f; Log("✅ Time.timeScale=10x"); });
            });

            DrawTestSection("缓存/引用", C_TITLE_BLUE, () =>
            {
                TestBtn("手动刷新缓存", () => { RefreshCache(); Log("✅ 缓存已刷新 _g=" + (_g != null) + " _c=" + (_c != null) + " _b=" + (_b != null)); });
                TestBtn("F2 数据快照", () => { DumpAllGameData(); Log("✅ 快照已输出到控制台"); });
            });

            // 日志输出区
            GUILayout.Space(10);
            Box(new Color(0.15f, 0.15f, 0.15f), "【测试日志】");
            if (!string.IsNullOrEmpty(debugLog))
            {
                GUI.color = new Color(0.8f, 0.9f, 1f);
                GUILayout.TextArea(debugLog, GUILayout.MinHeight(60));
                GUI.color = Color.white;
            }
            if (GUILayout.Button("清除日志", GUILayout.Height(20))) debugLog = "";

            GUILayout.EndScrollView();
        }

        // ===== 辅助方法 =====
        void DrawTestSection(string title, Color color, System.Action content)
        {
            Box(color, "▶ " + title);
            GUI.backgroundColor = new Color(0.18f, 0.18f, 0.22f);
            GUILayout.BeginVertical(GUI.skin.box);
            GUI.backgroundColor = Color.white;
            content();
            GUILayout.EndVertical();
            GUILayout.Space(3);
        }

        void TestBtn(string label, System.Action action)
        {
            GUI.backgroundColor = new Color(0.25f, 0.35f, 0.5f);
            if (GUILayout.Button(label, GUILayout.Height(22)))
            {
                action();
            }
            GUI.backgroundColor = Color.white;
        }

        void Log(string msg)
        {
            LoggerInstance.Msg("[调试] " + msg);
            debugLog = System.DateTime.Now.ToString("HH:mm:ss") + " " + msg + "\n" + debugLog;
            if (debugLog.Length > 3000) debugLog = debugLog.Substring(0, 3000);
        }
    }
}
