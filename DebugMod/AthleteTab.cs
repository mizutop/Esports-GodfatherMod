using UnityEngine;
using GameMain;
using GameMain.AthleteSystem;
using GameMain.UnitSystem;
using GameMain.UnitSystem.Equipment;
using System.Linq;
using System.Collections.Generic;
using Utility.PoolSystem;
using Utility.SettingSystem;

namespace DebugMod
{
    public partial class Main
    {
        void AthTab()
        {
            Box(new Color(0.2f, 0.4f, 0.6f), "【选手能力】");
            if (_c == null) { GUILayout.Label("无游戏数据"); return; }

            var ats = _c.Athletes.ToList();
            if (ats.Count == 0) { GUILayout.Label("无选手"); return; }

            athPg = Mathf.Clamp(athPg, 0, ats.Count - 1);
            var ath = ats[athPg];
            var ad = ath.C_Data;

            // ---- 选手翻页 ----
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("\u25c0", GUILayout.Width(30))) athPg = (athPg - 1 + ats.Count) % ats.Count;
            GUILayout.Label(string.Format("[{0}/{1}] {2}", athPg + 1, ats.Count, ath.HeroRole), GUILayout.ExpandWidth(true));
            if (GUILayout.Button("\u25b6", GUILayout.Width(30))) athPg = (athPg + 1) % ats.Count;
            GUILayout.EndHorizontal();

            // ---- 名称 / 立绘 ----
            string athName = "?";
            try { athName = ath.GetName() ?? "?"; } catch { }
            string athPor = "?";
            try { athPor = ath.Portrait_Head ?? "无"; } catch { }
            Box(new Color(0.15f, 0.3f, 0.5f),
                "名称: " + athName + " 立绘: " + (athPor.Length > 35 ? athPor.Substring(0, 35) + "..." : athPor));

            GUILayout.BeginHorizontal();
            GUILayout.Label("修改名称:", GUILayout.Width(70));
            nameI = GUILayout.TextField(nameI, GUILayout.Width(100));
            if (GUILayout.Button("应用", GUILayout.Width(50)))
            {
                if (!string.IsNullOrEmpty(nameI)) ath.SetName(nameI);
            }
            Btn("随机立绘", C_BTN_OLIVE, () =>
                Athlete.RandomSetNameAndPortrait(ath, _c.IsPlayer, GameMain.Gender.None, true));
            GUILayout.EndHorizontal();

            // ---- 5 项能力滑块 ----
            AbilitySlider("进攻", AthleteAbilityFlags.Aggressive, ad, MAX_ABILITY);
            AbilitySlider("防守", AthleteAbilityFlags.Defence, ad, MAX_ABILITY);
            AbilitySlider("发育", AthleteAbilityFlags.Farming, ad, MAX_ABILITY);
            AbilitySlider("反应", AthleteAbilityFlags.Reflection, ad, MAX_ABILITY);
            AbilitySlider("操作", AthleteAbilityFlags.Exercise, ad, MAX_ABILITY);

            Btn("当前选手全满", new Color(0.2f, 0.3f, 0.6f), () => MaxAth(ad));
            Btn("全部选手全满", new Color(0.2f, 0.3f, 0.6f), () =>
            {
                if (_c != null)
                    foreach (var a in _c.Athletes) MaxAth(a.C_Data);
            });

            // ---- 英雄池 / 熟练度 ----
            if (Fld("ath_hero", "英雄池 / 熟练度"))
            {
                DrawHeroPool(ad);
            }

            // ---- 性格 / 心情 ----
            if (Fld("ath_personality", "性格 / 心情"))
            {
                DrawPersonality(ad, ath);
            }

            // ---- 潜在 / 适应 / 战斗数据 ----
            if (Fld("ath_adv", "潜在 / 适应 / 战斗数据"))
            {
                DrawAdvancedStats(ad, ath);
            }

            // ---- 合同 / 续约 (新) ----
            if (Fld("ath_contract", "合同 / 续约"))
            {
                DrawContract(ath);
            }

            // ---- 装备管理 (新) ----
            if (Fld("ath_equip", "装备管理"))
            {
                DrawEquipment(ath);
            }
        }

        void AbilitySlider(string label, AthleteAbilityFlags flag, Athlete.DataComponent ad, int max)
        {
            int cur = ad.GetAbilityLevel(flag, false);
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(50));
            float v = GUILayout.HorizontalSlider(cur, 0, max, GUILayout.Width(120));
            GUILayout.Label(((int)v).ToString(), GUILayout.Width(25));
            if (GUILayout.Button("最大", GUILayout.Width(45)))
            {
                ad.SetAbilityLevelAndExp(flag, MAX_ABILITY, 0, 0);
            }
            else if ((int)v != cur)
            {
                ad.SetAbilityLevelAndExp(flag, (int)v, 0, 0);
            }
            GUILayout.EndHorizontal();
        }

        void MaxAth(Athlete.DataComponent ad)
        {
            foreach (AthleteAbilityFlags f in ALL_ABILITY_FLAGS)
                ad.SetAbilityLevelAndExp(f, MAX_ABILITY, 0, 0);
            ad.SetAbilityLimit(999, true);
        }

        void MaxMst(Athlete.DataComponent ad)
        {
            if (ad.heroDataDict == null) return;
            foreach (var k in ad.heroDataDict.Keys.ToArray())
            {
                var hd = ad.heroDataDict[k];
                hd.level = 7;
                hd.exp = 99999;
                ad.heroDataDict[k] = hd;
                ad.AddHeroMasteryExp(k, 99999, 7);
            }
        }

        // ===== 英雄池渲染 =====
        void DrawHeroPool(Athlete.DataComponent ad)
        {
            GUILayout.Label(string.Format("英雄池: {0}/{1}", ad.GetHeroNum(), ad.GetHeroPoolMax()));
            var heroKeys = ad.heroDataDict.Keys.ToArray();
            foreach (var hk in heroKeys)
            {
                var hd = ad.heroDataDict[hk];
                string hn = HeroNameCache(hk);
                GUILayout.BeginHorizontal();
                GUILayout.Label((hn.Length > 10 ? hn.Substring(0, 10) : hn) + ": " + hd.level, GUILayout.Width(95));

                for (int li = 0; li < MASTERY_LEVELS.Length; li++)
                {
                    bool active2 = hd.level >= li + 1;
                    GUI.backgroundColor = active2 ? new Color(0.9f, 0.7f, 0.1f) : new Color(0.25f, 0.25f, 0.25f);
                    if (GUILayout.Button(MASTERY_LEVELS[li], GUILayout.Width(30)))
                    {
                        hd.level = li + 1; hd.exp = 99999; ad.heroDataDict[hk] = hd;
                        ad.AddHeroMasteryExp(hk, 99999, li + 1);
                    }
                }
                GUI.backgroundColor = Color.white;
                Btn("X", C_BTN_RED, () => ad.RemoveHeroData(hk, out _), GUILayout.Width(20));
                GUILayout.EndHorizontal();
            }
            if (ad.heroDataDict.Count == 0) GUILayout.Label("  无英雄数据");
            Btn("全部升至宗师", new Color(0.3f, 0.5f, 0.8f), () => MaxMst(ad));
            Btn("清空英雄池", C_BTN_RED, () => ad.ClearHeroData());
        }

        // ===== 性格/心情渲染 =====
        void DrawPersonality(Athlete.DataComponent ad, Athlete ath)
        {
            GUILayout.Label("当前性格:");
            if (ad.Personalities != null && ad.Personalities.Count > 0)
            {
                foreach (var p in ad.Personalities)
                {
                    if (p == null) continue;
                    string pn = "?";
                    try { pn = p.Setting?.Name ?? "?"; } catch { }
                    Box(new Color(0f, 0.5f, 0.5f), "  " + pn);
                }
            }
            else
            {
                GUI.color = Color.gray;
                GUILayout.Label("  (无性格)");
                GUI.color = Color.white;
            }

            string emo = "无";
            try { emo = ad.CurrentEmotion?.Setting?.Type.ToString() ?? "无"; } catch { }
            GUI.color = emo == "Normal" || emo == "\u6b63\u5e38" ? Color.green : (emo == "无" ? Color.gray : Color.yellow);
            GUILayout.Label("心情: " + emo);
            GUI.color = Color.white;

            Box(new Color(0.2f, 0.4f, 0.7f), "指定性格");
            if (allPerSets.Count == 0)
            {
                try { allPerSets = SettingCenter.GetSettings<AthletePersonalitySetting>().ToList(); }
                catch { }
            }
            if (allPerSets.Count > 0)
            {
                int tp = Mathf.Max(1, (allPerSets.Count + PER_PAGE_SIZE - 1) / PER_PAGE_SIZE);
                perPg = Mathf.Clamp(perPg, 0, tp - 1);
                int st = perPg * PER_PAGE_SIZE, en = Mathf.Min(st + PER_PAGE_SIZE, allPerSets.Count);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("\u25c0", GUILayout.Width(30))) perPg = Mathf.Max(0, perPg - 1);
                GUILayout.Label(string.Format("{0}/{1}", perPg + 1, tp), GUILayout.ExpandWidth(true));
                if (GUILayout.Button("\u25b6", GUILayout.Width(30))) perPg = Mathf.Min(tp - 1, perPg + 1);
                GUILayout.EndHorizontal();
                for (int pi = st; pi < en; pi++)
                {
                    var p2 = allPerSets[pi];
                    if (p2 == null) continue;
                    string pn = "?";
                    try { pn = p2.Name ?? "?"; } catch { }
                    var capP = p2;
                    Btn("添加: " + pn, C_BTN_TEAL, () =>
                    {
                        try
                        {
                            var np = AthletePersonality.Spawn(capP, ath);
                            if (ad.Personalities != null) ad.Personalities.Add(np);
                        }
                        catch (System.Exception ex)
                        {
                            LoggerInstance.Msg("添加性格失败: " + ex.Message);
                        }
                    });
                }
            }
            Btn("清除所有性格", C_BTN_RED, () =>
            {
                if (ad.Personalities != null)
                    SimplePools.ClearCollection<AthletePersonality>(ad.Personalities);
            });
            Btn("随机变换心情", new Color(0.3f, 0.5f, 0.3f), () => ath.CreateAthleteEmotion());
            Btn("清除心情", new Color(0.5f, 0.3f, 0.3f), () => { ath.OnRemoveAthleteEmotion(); });
        }

        // ===== 高级数据渲染 =====
        void DrawAdvancedStats(Athlete.DataComponent ad, Athlete ath)
        {
            Box(new Color(0.3f, 0.2f, 0.5f), "能力潜力");
            foreach (var fl in ALL_ABILITY_FLAGS)
            {
                var curRank = ad.GetAbilityPotentialRank(fl);
                string rn = curRank.ToString().Replace("_Plus", "+").Replace("_Minus", "-");
                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("{0,8}: {1,5}", fl.ToString(), rn), GUILayout.Width(150));
                if (GUILayout.Button("S+", GUILayout.Width(30))) ad.SetAbilityPotentialRank(fl, AbilityPotentialRank.S_Plus);
                if (GUILayout.Button("S", GUILayout.Width(28))) ad.SetAbilityPotentialRank(fl, AbilityPotentialRank.S);
                if (GUILayout.Button("A", GUILayout.Width(28))) ad.SetAbilityPotentialRank(fl, AbilityPotentialRank.A);
                if (GUILayout.Button("B", GUILayout.Width(28))) ad.SetAbilityPotentialRank(fl, AbilityPotentialRank.B);
                if (GUILayout.Button("C", GUILayout.Width(28))) ad.SetAbilityPotentialRank(fl, AbilityPotentialRank.C);
                if (GUILayout.Button("D", GUILayout.Width(28))) ad.SetAbilityPotentialRank(fl, AbilityPotentialRank.D);
                GUILayout.EndHorizontal();
            }
            Btn("全部潜力 S+", C_BTN_PURPLE, () =>
            {
                foreach (var fl in ALL_ABILITY_FLAGS)
                    ad.SetAbilityPotentialRank(fl, AbilityPotentialRank.S_Plus);
            });

            Box(new Color(0.5f, 0.2f, 0.3f), "魅力 (Charm)");
            GUILayout.BeginHorizontal();
            GUILayout.Label("当前: " + ad.GetCharm(), GUILayout.Width(100));
            foreach (AthleteCharmRank cr in ALL_CHARM_RANKS)
                if (GUILayout.Button(cr.ToString(), GUILayout.Width(35))) ad.SetCharm(cr);
            GUILayout.EndHorizontal();

            Box(new Color(0.2f, 0.4f, 0.4f), "英雄适应性");
            foreach (var hc in ALL_HERO_CLASSES)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("{0,10}: {1,4}", hc, ad.GetHeroAdaption(hc)), GUILayout.Width(150));
                foreach (HeroClassAdaption ha in ALL_ADAPTION_RANKS)
                    if (GUILayout.Button(ha.ToString(), GUILayout.Width(30))) ad.SetHeroAdaption(hc, ha);
                GUILayout.EndHorizontal();
            }
            Btn("全部适应性 S", new Color(0.4f, 0.5f, 0.3f), () =>
            {
                foreach (var hc in ALL_HERO_CLASSES) ad.SetHeroAdaption(hc, HeroClassAdaption.S);
            });

            Box(new Color(0.3f, 0.5f, 0.4f), "英雄池容量 / 粉丝");
            GUILayout.BeginHorizontal();
            int poolMax = ad.GetHeroPoolMax();
            GUILayout.Label("上限: " + poolMax, GUILayout.Width(80));
            if (GUILayout.Button("-", GUILayout.Width(30))) ad.SetHeroPoolMax(Mathf.Max(1, poolMax - 1));
            if (GUILayout.Button("+", GUILayout.Width(30))) ad.SetHeroPoolMax(poolMax + 1);
            GUILayout.Label("粉丝: " + ad.GetFans(GameMain.StatTimeRangeTypes.Total).ToString("F0"), GUILayout.Width(120));
            if (GUILayout.Button("+1000", GUILayout.Width(50))) ad.AddAthleteFans(1000);
            if (GUILayout.Button("+10000", GUILayout.Width(55))) ad.AddAthleteFans(10000);
            GUILayout.EndHorizontal();

            Box(new Color(0.4f, 0.3f, 0.3f), "对战数据 (BattleData)");
            DrawBattleData(ath);

            // 全能选手一键创建
            Box(new Color(0.6f, 0.4f, 0.2f), "全能选手");
            Btn("一键全能 (600/600/18/5/10)", C_BTN_GOLD, () =>
            {
                try
                {
                    Athlete.RecreateAthleteByAbilityValue(ath, 600, 600, 18, 5, 10);
                    LoggerInstance.Msg("全能选手创建成功: " + (ath.GetName() ?? "?"));
                }
                catch (System.Exception ex) { LoggerInstance.Msg("全能选手失败: " + ex.Message); }
            });
        }

        void DrawBattleData(Athlete ath)
        {
            try
            {
                var bd = ath.GetType().GetField("battleData")?.GetValue(ath);
                if (bd == null) { GUILayout.Label("  无战斗数据"); return; }
                var bt = bd.GetType();
                foreach (var bf in BATTLE_DATA_FIELDS)
                {
                    var fld = bt.GetField(bf);
                    if (fld == null) continue;
                    object val = fld.GetValue(bd);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(string.Format("{0,-30}: {1,8}", bf, val), GUILayout.Width(230));
                    if (GUILayout.Button("最大", GUILayout.Width(45)))
                    {
                        if (fld.FieldType == typeof(int)) fld.SetValue(bd, 999);
                        else fld.SetValue(bd, 999.0);
                    }
                    if (GUILayout.Button("归零", GUILayout.Width(45)))
                    {
                        if (fld.FieldType == typeof(int)) fld.SetValue(bd, 0);
                        else fld.SetValue(bd, 0.0);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            catch { GUILayout.Label("  读取失败"); }
        }

        // ===== 合同系统 (新功能) =====
        void DrawContract(Athlete ath)
        {
            try
            {
                var contract = ath.C_Data.Contract;
                if (contract == null)
                {
                    GUILayout.Label("  无合同数据");
                    Btn("生成默认合同", C_BTN_GREEN, () =>
                    {
                        try { AthleteContract.SpawnDefault(ath); }
                        catch (System.Exception ex) { LoggerInstance.Msg("生成合同失败: " + ex.Message); }
                    });
                    return;
                }

                // 显示合同信息
                GUILayout.Label(string.Format("  薪资基础: {0}", contract.PriceBase));
                GUILayout.Label(string.Format("  剩余赛季: {0}", contract.RemainSeasonCount));
                GUILayout.Label(string.Format("  赛季等级: {0}", contract.SeasonCountLevel));
                GUILayout.Label(string.Format("  是否试用: {0}", contract.IsTrialContract));
                GUILayout.Label(string.Format("  是否自由: {0}", contract.IsFree));
                GUILayout.Label(string.Format("  是否违约: {0}", contract.IsBreak));
                GUILayout.Label(string.Format("  是否续约: {0}", contract.IsContinue));
                GUILayout.Label(string.Format("  满意度达标: {0}", contract.IsPassSatisfaction));

                GUILayout.Space(5);

                Btn("续约 (重置合同)", C_BTN_BLUE, () =>
                {
                    try
                    {
                        if (ath.C_Data.Contract == null || ath.C_Data.Contract.IsTrialContract || ath.C_Data.Contract.IsFree)
                        {
                            AthleteContract.Despawn(ath.C_Data.Contract, true);
                            AthleteContract.SpawnDefault(ath);
                            ath.C_Data.Contract.IsContinue = true;
                        }
                        else
                        {
                            var oldContract = ath.C_Data.Contract;
                            if (oldContract.IsBreak)
                                AthleteContract.SpawnByBreak(oldContract, true);
                            else
                                AthleteContract.SpawnByContinue(oldContract);
                        }
                        LoggerInstance.Msg("续约完成");
                    }
                    catch (System.Exception ex) { LoggerInstance.Msg("续约失败: " + ex.Message); }
                });

                Btn("重置为默认合同", C_BTN_ORANGE, () =>
                {
                    try
                    {
                        AthleteContract.Despawn(ath.C_Data.Contract, true);
                        AthleteContract.SpawnDefault(ath);
                        LoggerInstance.Msg("合同已重置");
                    }
                    catch (System.Exception ex) { LoggerInstance.Msg("重置合同失败: " + ex.Message); }
                });

                Btn("设为自由合同", C_BTN_RED, () =>
                {
                    try
                    {
                        var c = ath.C_Data.Contract;
                                var p = c.GetType().GetProperty("IsFree", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); if (p != null) p.SetValue(c, true, null);
                        LoggerInstance.Msg("已设为自由合同");
                    }
                    catch (System.Exception ex) { LoggerInstance.Msg("设置失败: " + ex.Message); }
                });
            }
            catch (System.Exception ex)
            {
                GUILayout.Label("  合同系统错误");
                LoggerInstance.Msg("合同渲染异常: " + ex.Message);
            }
        }

        // ===== 装备管理 (新功能) =====
        void DrawEquipment(Athlete ath)
        {
            try
            {
                // 比赛中的 Unit 才有 EquipmentComponent
                if (_b == null)
                {
                    GUILayout.Label("  装备管理需要在战斗中 (访问 Unit 数据)");
                    return;
                }

                // 查找该选手对应的战斗 Unit
                var unit = FindAthleteUnit(ath);
                if (unit == null)
                {
                    GUILayout.Label("  该选手未在战斗中上场");
                    return;
                }

                var equipComp = unit.C_Equipment;
                if (equipComp == null)
                {
                    GUILayout.Label("  无装备组件");
                    return;
                }

                // 当前装备列表
                var allEquip = equipComp.AllEquipments;
                GUILayout.Label(string.Format("当前装备: {0} 件", allEquip.Count));
                foreach (var eq in allEquip)
                {
                    string eqName = "?";
                    try { eqName = eq.Setting?.Name ?? eq.GetType().Name ?? "?"; } catch { }
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("  " + eqName, GUILayout.Width(150));
                    var capturedEq = eq;
                    Btn("移除", C_BTN_RED, () =>
                    {
                        try { MelonLoader.MelonCoroutines.Start(equipComp.Co_RemoveEquipment(capturedEq, true)); }
                        catch (System.Exception ex) { LoggerInstance.Msg("移除装备失败: " + ex.Message); }
                    });
                    GUILayout.EndHorizontal();
                }

                if (allEquip.Count == 0) GUILayout.Label("  (空)");

                GUILayout.Space(5);
                Btn("列出所有可用装备 (日志)", C_BTN_BLUE, () =>
                {
                    try
                    {
                        var allSettings = SettingCenter.GetSettings<HeroEquipmentSetting>().ToList();
                        LoggerInstance.Msg("可用装备共 " + allSettings.Count + " 种:");
                        foreach (var s in allSettings.Take(30))
                        {
                            string sn = "?";
                            try { sn = s.Name ?? "?"; } catch { }
                            LoggerInstance.Msg("  " + sn);
                        }
                        if (allSettings.Count > 30)
                            LoggerInstance.Msg("  ... 还有 " + (allSettings.Count - 30) + " 种");
                    }
                    catch (System.Exception ex) { LoggerInstance.Msg("列出装备失败: " + ex.Message); }
                });
            }
            catch (System.Exception ex)
            {
                GUILayout.Label("  装备系统错误");
                LoggerInstance.Msg("装备渲染异常: " + ex.Message);
            }
        }

        Unit FindAthleteUnit(Athlete athlete)
        {
            try
            {
                if (_b == null) return null;
                foreach (var team in new[] { _b.Team0, _b.Team1 })
                {
                    if (team == null) continue;
                    foreach (var u in team.Heroes)
                    {
                        if (u == null) continue;
                        if (u.Athlete == athlete) return u;
                    }
                }
            }
            catch (System.Exception ex) { LoggerInstance.Msg("FindAthleteUnit 异常: " + ex.Message); }
            return null;
        }

        // ===== 静态缓存 (减少每帧 GC 分配) =====
        private static readonly string[] MASTERY_LEVELS = { "入门", "熟练", "老手", "精通", "专家", "绝活", "宗师" };
        private static readonly AthleteAbilityFlags[] ALL_ABILITY_FLAGS = {
            AthleteAbilityFlags.Aggressive, AthleteAbilityFlags.Defence,
            AthleteAbilityFlags.Farming, AthleteAbilityFlags.Reflection, AthleteAbilityFlags.Exercise };
        private static readonly AthleteCharmRank[] ALL_CHARM_RANKS = {
            AthleteCharmRank.D, AthleteCharmRank.C,
            AthleteCharmRank.B, AthleteCharmRank.A, AthleteCharmRank.S };
        private static readonly HeroClassTypes[] ALL_HERO_CLASSES = {
            HeroClassTypes.Tank, HeroClassTypes.Warrior, HeroClassTypes.Assassin,
            HeroClassTypes.Magician, HeroClassTypes.Shooter, HeroClassTypes.Support };
        private static readonly HeroClassAdaption[] ALL_ADAPTION_RANKS = {
            HeroClassAdaption.D, HeroClassAdaption.C, HeroClassAdaption.B,
            HeroClassAdaption.A, HeroClassAdaption.S };
        private static readonly string[] BATTLE_DATA_FIELDS = {
            "tacticPoint", "athleteFightSpeed", "athleteAccuracyFight", "athleteEvasionFight",
            "athleteAccuracyBattle", "athleteEvasionBattle", "athleteEcoEfficiency", "athleteEcoTransferEfficiency" };

        // 英雄名称缓存（反射结果，减少重复 GetProperty）
        private static Dictionary<object, string> _heroNameCache = new Dictionary<object, string>();
        string HeroNameCache(object heroKey)
        {
            if (heroKey == null) return "?";
            string cached;
            if (_heroNameCache.TryGetValue(heroKey, out cached)) return cached;
            try
            {
                var np = heroKey.GetType().GetProperty("Name");
                if (np != null) { cached = np.GetValue(heroKey, null)?.ToString() ?? "?"; }
                else cached = "?";
            }
            catch { cached = "?"; }
            _heroNameCache[heroKey] = cached;
            return cached;
        }
    }
}
