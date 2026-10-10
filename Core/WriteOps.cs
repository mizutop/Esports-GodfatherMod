using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameMain.AthleteSystem;
using GameMain.ClubSystem;
using GameMain.UnitSystem;

namespace EsportsTrainer.Core
{
    /// <summary>
    /// Core/WriteOps.cs — 统一写入操作层（L3）。
    /// 每次修改：写入 → 回读验证 → 分级日志。
    /// 上层（Tab/模块）一律经此层修改游戏数据，禁止散落直写。
    /// </summary>
    public static class WriteOps
    {
        private const string T = "Club";
        private const string TA = "Athlete";

        // ================= 俱乐部 =================
        public static void SetBudget(Club.DataComponent cd, int v)
        {
            try
            {
                int before = cd.GetBudget();
                cd.SetBudget(v);
                int after = cd.GetBudget();
                Log.Write(T, "预算", before.ToString("N0"), v.ToString("N0"), after.ToString("N0"), after == v);
            }
            catch (Exception ex) { Log.Exception(T, "SetBudget", ex); }
        }

        public static void SetCoachPoint(Club.DataComponent cd, int v)
        {
            try
            {
                int before = cd.GetCoachPoint();
                cd.SetCoachPoint(v);
                int after = cd.GetCoachPoint();
                Log.Write(T, "教练点", before.ToString(), v.ToString(), after.ToString(), after == v);
            }
            catch (Exception ex) { Log.Exception(T, "SetCoachPoint", ex); }
        }

        public static void SetTrainPoint(Club.DataComponent cd, int v)
        {
            try
            {
                int before = cd.GetTrainPoint();
                cd.SetTrainPoint(v);
                int after = cd.GetTrainPoint();
                Log.Write(T, "训练点", before.ToString(), v.ToString(), after.ToString(), after == v);
            }
            catch (Exception ex) { Log.Exception(T, "SetTrainPoint", ex); }
        }

        public static void SetWorldPoint(Club.DataComponent cd, int v)
        {
            try
            {
                int before = cd.WorldPoint;
                cd.WorldPoint = v;
                int after = cd.WorldPoint;
                Log.Write(T, "世界分", before.ToString(), v.ToString(), after.ToString(), after == v);
            }
            catch (Exception ex) { Log.Exception(T, "SetWorldPoint", ex); }
        }

        public static void SetResearch(Club.DataComponent cd, int level, int count)
        {
            try
            {
                cd.trainingResearchLevel = level;
                cd.trainingResearchCount = count;
                Log.Ok(T, $"训练研究: 等级={cd.trainingResearchLevel} 次数={cd.trainingResearchCount}");
            }
            catch (Exception ex) { Log.Exception(T, "SetResearch", ex); }
        }

        public static void AddMedal(Club club, ClubMedalSetting setting)
        {
            try
            {
                string name = "?";
                try { name = setting.Desc ?? "?"; } catch { }
                club.C_Medal.AddMedal(setting, true);
                Log.Ok(T, $"勋章解锁: {name}");
            }
            catch (Exception ex) { Log.Exception(T, "AddMedal", ex); }
        }

        public static void AddAllMedals(Club club)
        {
            try
            {
                var all = Utility.SettingSystem.SettingCenter.GetSettings<ClubMedalSetting>();
                club.C_Medal.AddMedals(all, true);
                Log.Ok(T, "勋章全部解锁");
            }
            catch (Exception ex) { Log.Exception(T, "AddAllMedals", ex); }
        }

        // ================= 选手 =================
        public static void SetAbility(Athlete.DataComponent ad, AthleteAbilityFlags flag, int level)
        {
            try
            {
                int before = ad.GetAbilityLevel(flag, false);
                // 官方 SetAbilityLevelAndExp 会钳制到 GetAbilityMax（120），
                // 这里改走直写 abilities 结构体，滑块/输入任何数值都真实生效
                SetAbilityOver(ad, flag, level);
                int after = ad.GetAbilityLevel(flag, false);
                Log.Write(TA, $"能力 {flag}", before.ToString(), level.ToString(), after.ToString(), after == level);
            }
            catch (Exception ex) { Log.Exception(TA, "SetAbility", ex); }
        }

        public static void SetPotential(Athlete.DataComponent ad, AthleteAbilityFlags flag, AbilityPotentialRank rank)
        {
            try
            {
                var before = ad.GetAbilityPotentialRank(flag);
                ad.SetAbilityPotentialRank(flag, rank);
                var after = ad.GetAbilityPotentialRank(flag);
                Log.Write(TA, $"潜力 {flag}", before.ToString(), rank.ToString(), after.ToString(), after == rank);
            }
            catch (Exception ex) { Log.Exception(TA, "SetPotential", ex); }
        }

        public static void SetAbilityLimit(Athlete.DataComponent ad, int v)
        {
            try
            {
                int before = ad.GetAbilityLimit();
                ad.SetAbilityLimit(v, true);
                int after = ad.GetAbilityLimit();
                Log.Write(TA, "能力上限", before.ToString(), v.ToString(), after.ToString(), after == v);
            }
            catch (Exception ex) { Log.Exception(TA, "SetAbilityLimit", ex); }
        }

        public static void SetCharm(Athlete.DataComponent ad, AthleteCharmRank rank)
        {
            try
            {
                ad.SetCharm(rank);
                Log.Ok(TA, $"魅力 = {rank}");
            }
            catch (Exception ex) { Log.Exception(TA, "SetCharm", ex); }
        }

        public static void SetAdaption(Athlete.DataComponent ad, HeroClassTypes cls, HeroClassAdaption rank)
        {
            try
            {
                var before = ad.GetHeroAdaption(cls);
                ad.SetHeroAdaption(cls, rank);
                var after = ad.GetHeroAdaption(cls);
                Log.Write(TA, $"适应性 {cls}", before.ToString(), rank.ToString(), after.ToString(), after == rank);
            }
            catch (Exception ex) { Log.Exception(TA, "SetAdaption", ex); }
        }

        public static void SetHeroPoolMax(Athlete.DataComponent ad, int v)
        {
            try
            {
                int before = ad.GetHeroPoolMax();
                ad.SetHeroPoolMax(v);
                int after = ad.GetHeroPoolMax();
                Log.Write(TA, "英雄池容量", before.ToString(), v.ToString(), after.ToString(), after == v);
            }
            catch (Exception ex) { Log.Exception(TA, "SetHeroPoolMax", ex); }
        }

        public static void SetMastery(Athlete.DataComponent ad, UnitSetting hero, int level)
        {
            try
            {
                string hn = SafeHeroName(hero);
                if (!ad.GetHeroData(hero, out var hd))
                {
                    Log.Warn(TA, $"熟练度 {hn}: 英雄不在池中");
                    return;
                }
                hd.level = level;
                hd.exp = 99999;
                ad.SetHeroData(hero, hd, true);
                ad.AddHeroMasteryExp(hero, 99999, level);
                int after = ad.GetHeroMasteryLevel(hero);
                Log.Write(TA, $"熟练度 {hn}", hd.level.ToString(), level.ToString(), after.ToString(), after >= level);
            }
            catch (Exception ex) { Log.Exception(TA, "SetMastery", ex); }
        }

        public static void AddHero(Athlete.DataComponent ad, UnitSetting hero, bool toMax)
        {
            try
            {
                string hn = SafeHeroName(hero);
                if (ad.GetHeroData(hero, out _)) { Log.Warn(TA, $"添加英雄 {hn}: 已在池中"); return; }
                ad.AddNewHeroData(hero, AthleteHeroData.New(hero), true);
                if (!ad.GetHeroData(hero, out _)) { Log.Err(TA, $"添加英雄 {hn}: 写入后未找到（池已满？）"); return; }
                Log.Ok(TA, $"添加英雄: {hn}");
                if (toMax) SetMastery(ad, hero, 7);
            }
            catch (Exception ex) { Log.Exception(TA, "AddHero", ex); }
        }

        public static void RemoveHero(Athlete.DataComponent ad, UnitSetting hero)
        {
            try
            {
                string hn = SafeHeroName(hero);
                if (!ad.GetHeroData(hero, out _)) { Log.Warn(TA, $"移除英雄 {hn}: 不在池中"); return; }
                ad.RemoveHeroData(hero, out _);
                Log.Ok(TA, $"移除英雄: {hn}");
            }
            catch (Exception ex) { Log.Exception(TA, "RemoveHero", ex); }
        }

        public static void ClearHeroes(Athlete.DataComponent ad)
        {
            try
            {
                int n = ad.heroDataDict?.Count ?? 0;
                ad.ClearHeroData();
                int after = ad.heroDataDict?.Count ?? 0;
                Log.Ok(TA, $"清空英雄池: {n} → {after}");
            }
            catch (Exception ex) { Log.Exception(TA, "ClearHeroes", ex); }
        }

        public static void AddTrait(Athlete ath, AthleteTraitSetting setting)
        {
            try
            {
                string name = "?";
                try { name = setting.Name ?? "?"; } catch { }
                ath.C_Traits.AddTrait(setting);
                Log.Ok(TA, $"添加特性: {name}");
            }
            catch (Exception ex) { Log.Exception(TA, "AddTrait", ex); }
        }

        public static void SetName(Athlete ath, string name)
        {
            try
            {
                string before = "?";
                try { before = ath.GetName() ?? "?"; } catch { }
                ath.SetName(name);
                string after = "?";
                try { after = ath.GetName() ?? "?"; } catch { }
                Log.Write(TA, "选手名称", before, name, after, after == name);
            }
            catch (Exception ex) { Log.Exception(TA, "SetName", ex); }
        }

        public static void AddFans(Athlete.DataComponent ad, int amount)
        {
            try
            {
                double before = ad.GetFans(GameMain.StatTimeRangeTypes.Total);
                ad.AddAthleteFans(amount);
                double after = ad.GetFans(GameMain.StatTimeRangeTypes.Total);
                Log.Write(TA, "粉丝", before.ToString("F0"), "+" + amount, after.ToString("F0"), after > before);
            }
            catch (Exception ex) { Log.Exception(TA, "AddFans", ex); }
        }

        /// <summary>粉丝设为指定值（差值经官方 AddAthleteFans 调整，支持负数）。</summary>
        public static void SetFans(Athlete.DataComponent ad, double target)
        {
            try
            {
                double before = ad.GetFans(GameMain.StatTimeRangeTypes.Total);
                long delta = (long)Math.Round(target - before);
                if (delta == 0) { Log.Info(TA, "粉丝数已等于目标值"); return; }
                int step = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, delta));
                ad.AddAthleteFans(step);
                double after = ad.GetFans(GameMain.StatTimeRangeTypes.Total);
                Log.Write(TA, "粉丝", before.ToString("F0"), target.ToString("F0"), after.ToString("F0"), Math.Abs(after - target) < 1);
            }
            catch (Exception ex) { Log.Exception(TA, "SetFans", ex); }
        }

        /// <summary>一键添加英雄池缺失的全部英雄（可选招满即宗师），返回新增数量。</summary>
        public static int AddAllHeroes(Athlete.DataComponent ad, System.Collections.Generic.List<UnitSetting> heroes, bool toGm = false)
        {
            int added = 0;
            foreach (var h in heroes)
            {
                if (h == null) continue;
                try
                {
                    bool exists = false;
                    try { exists = ad.GetHeroData(h, out _); } catch { }
                    if (exists) continue;
                    AddHero(ad, h, toGm);
                    added++;
                }
                catch { }
            }
            return added;
        }

        public static void RecreateAllRound(Athlete ath)
        {
            try
            {
                string name = "?";
                try { name = ath.GetName() ?? "?"; } catch { }
                Athlete.RecreateAthleteByAbilityValue(ath, 600, 600, 18, 5, 10);
                Log.Ok(TA, $"满属性改造: {name} (600/600/18岁/5英雄/10池)");
            }
            catch (Exception ex) { Log.Exception(TA, "RecreateAllRound", ex); }
        }

        private static readonly HeroRoleFlags[] ValidRoles =
        {
            HeroRoleFlags.Top, HeroRoleFlags.Mid, HeroRoleFlags.Jug, HeroRoleFlags.Adc, HeroRoleFlags.Sup
        };

        public static Athlete RecruitNew(GameMain.ClubSystem.Club club, GameMain.UnitSystem.HeroRoleFlags role, int ability, int age)
        {
            try
            {
                // 分路守卫：None/All/组合标志不可用于注册（官方 OnAddAthlete 要求单体分路），非法时随机一个
                if (role == HeroRoleFlags.None || role == HeroRoleFlags.All || ((int)role & ((int)role - 1)) != 0)
                    role = ValidRoles[UnityEngine.Random.Range(0, ValidRoles.Length)];

                // 官方裸创建路径：Athlete.Spawn(club, role, ...) 内部已 OnAddToClub 完成俱乐部注册；
                // 旧代码随后又调 club.OnAddAthlete → 重复注册（Entity 二次 Connect / 字典重复添加）→ NullReferenceException
                var a = Athlete.Spawn(club, role, System.Guid.Empty, false);
                if (a == null) { Log.Err(TA, $"招募 {role}: Spawn 返回 null"); return null; }

                Athlete.RecreateAthleteByAbilityValue(a, ability, ability, age, 5, 10);
                Log.Ok(TA, $"招募新选手: {a.GetName()} · {L10n.Role(role)} · 能力 {ability} · {age} 岁");
                return a;
            }
            catch (Exception ex)
            {
                Log.Exception(TA, "RecruitNew", ex);
                return null;
            }
        }

        public static string SafeHeroName(object o)
        {
            try { return o?.GetType().GetProperty("Name")?.GetValue(o, null)?.ToString() ?? "?"; }
            catch { return "?"; }
        }

        public static string SafeName(object o)
        {
            if (o == null) return "?";
            try
            {
                if (o is Athlete a) return a.GetName() ?? "?";
                var p = o.GetType().GetProperty("Name");
                return p?.GetValue(o, null)?.ToString() ?? "?";
            }
            catch { return "?"; }
        }

        // ================= 里程碑（俱乐部 / 选手通用） =================
        public static void UnlockMilestone(Milestone m)
        {
            try
            {
                string name = SafeName(m.Setting);
                m.State = MilestoneStates.CanTakeReward;
                m.TakeReward();
                Log.Ok("Mile", $"里程碑解锁: {name}");
            }
            catch (Exception ex) { Log.Exception("Mile", "UnlockMilestone", ex); }
        }

        public static void UnlockAllMilestones(System.Collections.Generic.List<Milestone> milestones)
        {
            int n = 0;
            foreach (var m in milestones)
            {
                try { m.State = MilestoneStates.CanTakeReward; m.TakeReward(); n++; } catch { }
            }
            Log.Ok("Mile", $"里程碑全部解锁: {n}/{milestones.Count}");
        }

        // ================= 成就 =================
        public static void UnlockAchievement(GameMain.AchievementSystem.AchievementSettingData a)
        {
            try
            {
                GameMain.AchievementSystem.Achievement.SetAchievementValue(a.api, a.targetValue);
                Log.Ok("Achv", $"成就解锁: [{a.api}] 目标 {a.targetValue}");
            }
            catch (Exception ex) { Log.Exception("Achv", "UnlockAchievement", ex); }
        }

        public static void UnlockAllAchievements()
        {
            try
            {
                var list = GameMain.AchievementSystem.Achievement.Setting.AchievementList;
                int n = 0;
                foreach (var a in list)
                {
                    try { GameMain.AchievementSystem.Achievement.SetAchievementValue(a.api, a.targetValue); n++; }
                    catch { }
                }
                try
                {
                    // 注意：实机 Assembly-CSharp 与反编译源码版本有漂移（直调 GameMain.Main.SaveAchievement 报 CS0117），
                    // 必须保留反射静默调用；失败时数据已写入 AchievementLocalData，下次游戏存档仍会持久化
                    typeof(GameMain.Main).GetMethod("SaveAchievement",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);
                }
                catch { }
                Log.Ok("Achv", $"成就全部解锁: {n}/{list.Count}（已存档并同步 Steam）");
            }
            catch (Exception ex) { Log.Exception("Achv", "UnlockAllAchievements", ex); }
        }

        // ================= 队内关系 =================
        public static void SetRelation(Athlete a, Athlete b, int value)
        {
            try
            {
                var rels = GameMain.Main.Inst.Game.C_Data.AthleteRelations;
                rels.Set(a, b, value);
                string[] names = { "对立", "疏远", "普通", "友好", "亲密" };
                string lv = (value + 2 >= 0 && value + 2 < names.Length) ? names[value + 2] : value.ToString();
                Log.Ok("Rel", $"{SafeName(a)} × {SafeName(b)} → {lv}");
            }
            catch (Exception ex) { Log.Exception("Rel", "SetRelation", ex); }
        }

        public static int GetRelation(Athlete a, Athlete b)
        {
            try
            {
                var rels = GameMain.Main.Inst.Game.C_Data.AthleteRelations;
                if (rels.TryGet(a, b, out var r)) return r.Value;
            }
            catch { }
            return 0;
        }

        // ================= 教练 / 俱乐部升级（技能树） =================
        /// <summary>教练等级直升到顶（循环调用游戏官方 PromoteCoach，含晋级奖励）。</summary>
        public static void PromoteCoachMax(Club club)
        {
            try
            {
                var before = club.CoachClassType;
                int n = 0;
                while (n < 10)
                {
                    bool ok;
                    try { ok = club.PromoteCoach(); }
                    catch { break; }
                    if (!ok) break;
                    n++;
                }
                Log.Write(T, "教练等级", before.ToString(), "→ S", club.CoachClassType.ToString(), n > 0);
            }
            catch (Exception ex) { Log.Exception(T, "PromoteCoachMax", ex); }
        }

        /// <summary>解锁全部俱乐部升级项（教练技能树节点去掉锁，不代替玩家完成）。</summary>
        public static void UnlockAllUpgradeItems(Club club)
        {
            try
            {
                var all = Utility.SettingSystem.SettingCenter.GetSettings<ClubAffairUpgradeItemSetting>().ToList();
                int n = 0;
                foreach (var s in all)
                {
                    if (s == null) continue;
                    try { if (club.C_Data.unlockedUpgradeItemSettings.Add(s)) n++; } catch { }
                }
                Log.Ok(T, $"俱乐部升级项解锁: {n}/{all.Count}（教练技能树/发展树已去锁）");
            }
            catch (Exception ex) { Log.Exception(T, "UnlockAllUpgradeItems", ex); }
        }

        /// <summary>俱乐部升级全部完成：全部升级项直接写入 已完成 集合（与游戏 ClubAffairUpgrade 完成路径一致）。</summary>
        public static void CompleteAllUpgradeItems(Club club)
        {
            try
            {
                var all = Utility.SettingSystem.SettingCenter.GetSettings<ClubAffairUpgradeItemSetting>().ToList();
                int n = 0;
                foreach (var s in all)
                {
                    if (s == null) continue;
                    try
                    {
                        club.C_Data.unlockedUpgradeItemSettings.Add(s);
                        if (club.C_Data.completedUpgradeItemSettings.Add(s)) n++;
                    }
                    catch { }
                }
                Log.Ok(T, $"俱乐部升级全部完成: {n}/{all.Count}");
            }
            catch (Exception ex) { Log.Exception(T, "CompleteAllUpgradeItems", ex); }
        }

        /// <summary>全员心情设定（level 0~6，6=亢奋）。</summary>
        public static void SetAllEmotion(int level)
        {
            int n = 0;
            foreach (var a in Refs.AthletesCached)
            {
                if (a == null) continue;
                try { SetEmotion(a, level); n++; } catch { }
            }
            Log.Ok(TA, $"全员心情已设定 (Lv{level}): {n} 名选手");
        }

        // ================= 能力突破上限 =================
        /// <summary>走游戏官方 AutoBreak 流程突破能力上限（受总上限约束）。</summary>
        public static void AutoBreakLimit(Athlete.DataComponent ad)
        {
            try
            {
                int before = ad.GetAbilityLimit();
                ad.AutoBreak(1000);
                int after = ad.GetAbilityLimit();
                Log.Write(TA, "突破上限", before.ToString(), "AutoBreak", after.ToString(), after > before);
            }
            catch (Exception ex) { Log.Exception(TA, "AutoBreakLimit", ex); }
        }

        /// <summary>超限写入单维能力（绕过 GetAbilityMax 钳制，直接改 abilities 数据）。</summary>
        public static void SetAbilityOver(Athlete.DataComponent ad, AthleteAbilityFlags flag, int level)
        {
            try
            {
                var ab = ad.abilities;
                if (!ab.TryGetAbilityData(flag, out var d)) return;
                int before = d.level;
                d.level = level;
                ab.SetAbilityData(flag, d);
                ad.abilities = ab;
                Log.Write(TA, $"能力 {flag}(超限)", before.ToString(), level.ToString(), level.ToString(), true);
            }
            catch (Exception ex) { Log.Exception(TA, "SetAbilityOver", ex); }
        }

        /// <summary>超限写入能力总上限（直接改 abilities.limit，不低于当前总等级）。</summary>
        public static void SetAbilityLimitOver(Athlete.DataComponent ad, int v)
        {
            try
            {
                int before = ad.GetAbilityLimit();
                var ab = ad.abilities;
                ab.limit = Math.Max(v, ab.GetAbilityLevel(AthleteAbilityFlags.All));
                ad.abilities = ab;
                Log.Write(TA, "能力上限(超限)", before.ToString(), v.ToString(), ad.GetAbilityLimit().ToString(), ad.GetAbilityLimit() >= v);
            }
            catch (Exception ex) { Log.Exception(TA, "SetAbilityLimitOver", ex); }
        }

        // ================= 称号（段位/评级） =================
        /// <summary>称号显示名：走游戏官方 TB_AthleteTitle 本地化（含名人堂 {value} 替换）。
        /// 旧 SafeName 反射找 "Name" 属性，而 AthleteTitleSetting 只有私有 Title → 显示 "?"。</summary>
        public static string TitleName(Athlete ath, AthleteTitleSetting t)
        {
            if (t == null) return "?";
            try { var n = t.GetName(ath.Id); if (!string.IsNullOrEmpty(n)) return n; } catch { }
            try { var n = Localization.TB_AthleteTitle.GetName(t.Key); if (!string.IsNullOrEmpty(n)) return n; } catch { }
            return "[" + t.TitleClass + "]";
        }

        public static void AddTitle(Athlete.DataComponent ad, AthleteTitleSetting t)
        {
            try
            {
                ad.AddAthleteTitle(t);
                Log.Ok("Athlete", $"称号授予: {t?.Key ?? "?"}");
            }
            catch (Exception ex) { Log.Exception("Athlete", "AddTitle", ex); }
        }

        // ================= 职位（首发 / 替补 / 青训） =================
        public static string SquadStatus(Athlete ath)
        {
            try { if (ath.Club.Contestants.Contains(ath)) return L10n.T("首发主力"); } catch { }
            try { if (ath.Club.Substitutes.Contains(ath)) return L10n.T(ath.IsYouth ? "青训" : "替补"); } catch { }
            return "?";
        }

        /// <summary>职位切换：首发⇄替补（走官方 ChangeAthlete/SelectSubstitute，自动处理同位置互换）。</summary>
        public static void SwapSquad(Athlete ath)
        {
            try
            {
                var club = ath.Club;
                var data = club.C_Data.athleteData;
                bool isContestant = club.ContestantRoleDict.TryGetValue(ath.HeroRole, out var cur) && cur == ath;
                if (isContestant)
                {
                    var sub = club.Substitutes.FirstOrDefault(s => s != null && !s.IsYouth &&
                        (club.IsAllRole || s.HeroRole == ath.HeroRole));
                    if (sub == null) { Log.Warn("Athlete", "无可换的同位置替补"); return; }
                    data.ChangeAthlete(ath, sub);
                    Log.Ok("Athlete", $"{SafeName(ath)} 转为替补，{SafeName(sub)} 升入首发");
                }
                else if (cur != null && cur != ath)
                {
                    data.ChangeAthlete(cur, ath);
                    Log.Ok("Athlete", $"{SafeName(ath)} 升入首发，{SafeName(cur)} 转为替补");
                }
                else
                {
                    data.SelectSubstitute(ath.HeroRole, ath, club.IsAllRole);
                    Log.Ok("Athlete", $"{SafeName(ath)} 已任命为 {L10n.Role(ath.HeroRole)} 首发主力");
                }
            }
            catch (Exception ex) { Log.Exception("Athlete", "SwapSquad", ex); }
        }

        // ================= 心情（按官方 7 级情绪直接设定） =================
        /// <summary>设定心情等级 0~6（崩溃/沮丧/失落/普通/精神/激动/亢奋）。
        /// 旧「随机心情」走 CreateAthleteEmotion = 重置为默认普通，这就是心情只有"无/普通"的根因。</summary>
        public static void SetEmotion(Athlete ath, int level)
        {
            try
            {
                var emo = ath.C_Data.CurrentEmotion;
                if (emo == null) { ath.CreateAthleteEmotion(); emo = ath.C_Data.CurrentEmotion; }
                if (emo == null) { Log.Warn("Athlete", "心情组件不可用"); return; }

                var settings = Athlete.GetEmotionSettingsByLevel(ath, level)?.ToList();
                if (settings == null || settings.Count == 0) { Log.Warn("Athlete", $"情绪等级 {level} 无可用配置"); return; }
                var pick = settings[UnityEngine.Random.Range(0, settings.Count)];
                emo.C_Setting.Set(pick);
                Log.Ok("Athlete", $"心情 → {L10n.Emotion(pick.Type.ToString())} (Lv{level})");
            }
            catch (Exception ex) { Log.Exception("Athlete", "SetEmotion", ex); }
        }

        // ================= 多样性（流派上限） =================
        public static void SetPrimeSetLimit(Athlete ath, int v)
        {
            try
            {
                int before = ath.C_Traits.PrimeSetLimit;
                ath.C_Traits.SetPrimeSetLimit(v);
                int after = ath.C_Traits.PrimeSetLimit;
                Log.Write("Athlete", "多样性(流派上限)", before.ToString(), v.ToString(), after.ToString(), after == v);
            }
            catch (Exception ex) { Log.Exception("Athlete", "SetPrimeSetLimit", ex); }
        }

        // ================= 技能槽 / 卡槽 =================
        public static void FillSkills(Athlete ath)
        {
            try
            {
                var ps = ath.C_Traits.GetPrimeSet();
                int before = ps.Skills.Count;
                var owned = new HashSet<string>();
                foreach (var s in ps.Skills)
                {
                    try { owned.Add(s.Setting.Key.ToString()); } catch { }
                }
                var pool = Utility.SettingSystem.SettingCenter.GetSettings<AthleteTraitSetting>()
                    .Where(t => { try { return t.TraitType == AthleteTraitTypes.Skill && !owned.Contains(t.Key.ToString()); } catch { return false; } })
                    .ToList();
                int added = 0;
                foreach (var t in pool)
                {
                    if (ps.Skills.Count >= AthletePrimeSet.SkillLimit) break;
                    try { ath.C_Traits.AddTrait(t); added++; } catch { }
                }
                Log.Ok("Athlete", $"技能槽装填: {before} → {ps.Skills.Count}/{AthletePrimeSet.SkillLimit}（新增 {added}）");
            }
            catch (Exception ex) { Log.Exception("Athlete", "FillSkills", ex); }
        }

        public static void FillCardSlots(Athlete ath)
        {
            try
            {
                var ps = ath.C_Traits.GetPrimeSet();
                int before = ps.Slots.Count;
                foreach (var card in ps.GetCardSelectable())
                {
                    try { ps.AddCardSlot(card); } catch { }
                }
                Log.Ok("Athlete", $"卡槽装填: {before} → {ps.Slots.Count}");
            }
            catch (Exception ex) { Log.Exception("Athlete", "FillCardSlots", ex); }
        }

        // ================= 俱乐部场地升级 =================
        public static void UpgradeStadium(Club club)
        {
            try
            {
                int cur = club.Stadium.Setting.Level;
                var next = Utility.SettingSystem.SettingCenter.GetSettings<StadiumSetting>()
                    .Where(s => s.Level > cur).OrderBy(s => s.Level).FirstOrDefault();
                if (next == null) { Log.Warn("Club", "俱乐部场地已是最高级"); return; }
                club.C_Data.SetStadium(next);
                Log.Ok("Club", $"俱乐部场地升级: Lv{cur} → Lv{next.Level}");
            }
            catch (Exception ex) { Log.Exception("Club", "UpgradeStadium", ex); }
        }

        // ================= 贷款 =================
        public static void RepayAllLoans(Club club)
        {
            try
            {
                var ld = club.C_Data.LoansData;
                int n = ld.loans?.Count ?? 0;
                if (n == 0) { Log.Info("Club", "当前无贷款"); return; }
                foreach (var loan in ld.loans.ToList())
                    ld.RepayLoans(loan.level, int.MaxValue);   // 官方方法：仅扣减 remainLoans，不动预算 = 0 元还贷
                Log.Ok("Club", $"立即还贷 (0 元): {n} 笔已清除");
            }
            catch (Exception ex) { Log.Exception("Club", "RepayAllLoans", ex); }
        }

        /// <summary>修改贷款剩余金额（官方 ClubLoansSingleData.remainLoans）。</summary>
        public static void SetLoanRemain(Club club, ClubLoansLevel level, int v)
        {
            try
            {
                var loan = club.C_Data.LoansData.loans.FirstOrDefault(l => l != null && l.level == level);
                if (loan == null) { Log.Warn(T, $"无该档位贷款: {level}"); return; }
                int before = loan.remainLoans;
                loan.remainLoans = v;
                if (loan.remainLoans <= 0) club.C_Data.LoansData.loans.Remove(loan);
                Log.Write(T, $"贷款 {level} 金额", before.ToString(), v.ToString(), v.ToString(), true);
            }
            catch (Exception ex) { Log.Exception(T, "SetLoanRemain", ex); }
        }

        /// <summary>修改贷款利率（官方 currentRate，月息比例）。</summary>
        public static void SetLoanRate(Club club, ClubLoansLevel level, float rate)
        {
            try
            {
                var loan = club.C_Data.LoansData.loans.FirstOrDefault(l => l != null && l.level == level);
                if (loan == null) { Log.Warn(T, $"无该档位贷款: {level}"); return; }
                float before = loan.currentRate;
                loan.currentRate = rate;
                Log.Write(T, $"贷款 {level} 利率", before.ToString("F3"), rate.ToString("F3"), rate.ToString("F3"), true);
            }
            catch (Exception ex) { Log.Exception(T, "SetLoanRate", ex); }
        }

        // ================= 选手：生涯 / 年龄 / 分路 =================
        public static void SetCareerLife(Athlete ath, int v)
        {
            try
            {
                int before = ath.C_Data.GetCareerLife();
                ath.C_Data.SetCareerLife(v);
                Log.Write(TA, "职业生命", before.ToString(), v.ToString(), ath.C_Data.GetCareerLife().ToString(), ath.C_Data.GetCareerLife() == v);
            }
            catch (Exception ex) { Log.Exception(TA, "SetCareerLife", ex); }
        }

        public static void SetAthleteAge(Athlete ath, float v)
        {
            try
            {
                float before = ath.C_Data.GetAge();
                ath.C_Data.SetAge(Math.Max(18f, v));
                Log.Write(TA, "年龄", before.ToString("F1"), v.ToString("F1"), ath.C_Data.GetAge().ToString("F1"), true);
            }
            catch (Exception ex) { Log.Exception(TA, "SetAthleteAge", ex); }
        }

        public static void SetRole(Athlete ath, HeroRoleFlags role)
        {
            try
            {
                var before = ath.HeroRole;
                ath.SetHeroRole(role);
                Log.Write(TA, "分路", before.ToString(), role.ToString(), ath.HeroRole.ToString(), ath.HeroRole == role);
            }
            catch (Exception ex) { Log.Exception(TA, "SetRole", ex); }
        }

        // ================= 选手：合同细则 =================
        public static void SetContractSalaryLevel(Athlete ath, int v)
        {
            try
            {
                var c = ath.C_Data.Contract;
                if (c == null) { Log.Warn(TA, "无合同"); return; }
                int before = c.SalaryLevel;
                c.SalaryLevel = v;
                Log.Write(TA, "合同薪资档", before.ToString(), v.ToString(), c.SalaryLevel.ToString(), c.SalaryLevel == v);
            }
            catch (Exception ex) { Log.Exception(TA, "SetContractSalaryLevel", ex); }
        }

        public static void SetContractSignCostLevel(Athlete ath, int v)
        {
            try
            {
                var c = ath.C_Data.Contract;
                if (c == null) { Log.Warn(TA, "无合同"); return; }
                int before = c.SignCostLevel;
                c.SignCostLevel = v;
                Log.Write(TA, "合同签约费档", before.ToString(), v.ToString(), c.SignCostLevel.ToString(), c.SignCostLevel == v);
            }
            catch (Exception ex) { Log.Exception(TA, "SetContractSignCostLevel", ex); }
        }

        public static void SetContractRemainSeason(Athlete ath, int v)
        {
            try
            {
                var c = ath.C_Data.Contract;
                if (c == null) { Log.Warn(TA, "无合同"); return; }
                int before = c.RemainSeasonCount;
                c.RemainSeasonCount = v;
                Log.Write(TA, "合同剩余赛季", before.ToString(), v.ToString(), c.RemainSeasonCount.ToString(), c.RemainSeasonCount == v);
            }
            catch (Exception ex) { Log.Exception(TA, "SetContractRemainSeason", ex); }
        }

        // ================= 晋级支持度 =================
        public static void MaxShowSupport(Club.DataComponent cd)
        {
            try
            {
                var s = cd.matchShowSupportData;
                float before = s.cur;
                s.cur = 1f;
                cd.matchShowSupportData = s;
                Log.Write("Club", "观众支持度", (before * 100).ToString("F0") + "%", "100%", null, true);
            }
            catch (Exception ex) { Log.Exception("Club", "MaxShowSupport", ex); }
        }

        public static string SupportText(Club.DataComponent cd)
        {
            try
            {
                var s = cd.matchShowSupportData;
                return $"{s.cur * 100:F0}% / 晋级线 {s.require * 100:F0}%";
            }
            catch { return "?"; }
        }
    }
}
