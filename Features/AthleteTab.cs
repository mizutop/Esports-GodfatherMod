using System;
using System.Collections.Generic;
using System.Linq;
using EsportsTrainer.Core;
using EsportsTrainer.UI;
using GameMain;
using GameMain.AthleteSystem;
using GameMain.ClubSystem;
using GameMain.UnitSystem;
using UnityEngine;
using Utility.SettingSystem;

namespace EsportsTrainer.Features
{
    /// <summary>
    /// Features/AthleteTab.cs — 选手域（子导航：能力 / 英雄池 / 特性流派 / 附加 / 合同 / 招募）。
    /// 对应游戏选手界面五个页签（能力/英雄池/对战数据/潜力）+ 扩展功能。
    /// 全部写入经 WriteOps（写入→回读→日志）。
    /// </summary>
    public class AthleteTab : ITab
    {
        public string Id => "athlete";
        public string Title => L10n.T("选手");
        public Theme.AccentColor Accent => Theme.AccentColor.Blue;

        private const int MaxAbility = 120;
        private const int PerPageSize = 6;
        private const string T = "Athlete";

        private int _sub, _athPg, _perPg, _heroPg, _traitPg, _traitTypeIdx, _titlePg, _milePg;
        private string _nameInput = "";
        private string _recruitAbility = "600", _recruitAge = "18";
        private string _poolMaxInput = "1", _fansInput = "1000";
        private string _ageInput = "18", _careerLifeInput = "60";
        private readonly Dictionary<object, Dictionary<string, string>> _editInputs = new Dictionary<object, Dictionary<string, string>>();
        private List<AthletePersonalitySetting> _perSets;
        private List<UnitSetting> _heroes;
        private List<AthleteTraitSetting> _traits;
        private List<AthleteTitleSetting> _titles;
        private readonly Dictionary<object, string> _nameCache = new Dictionary<object, string>();
        private bool _showBattleData;

        private static string[] SubNames => new[] { L10n.T("能力"), L10n.T("英雄池"), L10n.T("特性流派"), L10n.T("里程碑"), L10n.T("附加"), L10n.T("合同"), L10n.T("招募") };

        private static readonly string[] MasteryLevels = { "入门", "熟练", "老手", "精通", "专家", "绝活", "宗师" };
        private static readonly AthleteAbilityFlags[] AbilityFlags =
        {
            AthleteAbilityFlags.Aggressive, AthleteAbilityFlags.Defence, AthleteAbilityFlags.Farming,
            AthleteAbilityFlags.Reflection, AthleteAbilityFlags.Exercise
        };
        private static string[] AbilityNames => new[]
        {
            L10n.Ability(AthleteAbilityFlags.Aggressive), L10n.Ability(AthleteAbilityFlags.Defence),
            L10n.Ability(AthleteAbilityFlags.Farming), L10n.Ability(AthleteAbilityFlags.Reflection),
            L10n.Ability(AthleteAbilityFlags.Exercise)
        };
        private static readonly string[] CharmNames = { "D", "C", "B", "A", "S" };
        private static readonly AthleteCharmRank[] CharmRanks =
        {
            AthleteCharmRank.D, AthleteCharmRank.C, AthleteCharmRank.B, AthleteCharmRank.A, AthleteCharmRank.S
        };
        private static readonly HeroClassTypes[] HeroClasses =
        {
            HeroClassTypes.Tank, HeroClassTypes.Warrior, HeroClassTypes.Assassin,
            HeroClassTypes.Magician, HeroClassTypes.Shooter, HeroClassTypes.Support
        };
        private static readonly HeroClassAdaption[] AdaptionRanks =
        {
            HeroClassAdaption.D, HeroClassAdaption.C, HeroClassAdaption.B, HeroClassAdaption.A, HeroClassAdaption.S
        };
        private static readonly string[] BattleDataFields =
        {
            "tacticPoint", "athleteFightSpeed", "athleteAccuracyFight", "athleteEvasionFight",
            "athleteAccuracyBattle", "athleteEvasionBattle", "athleteEcoEfficiency",
            "athleteEcoTransferEfficiencyAttack", "athleteEcoTransferEfficiencyDefence", "athleteEcoTransferEfficiency"
        };

        public void Render()
        {
            if (!Refs.HasClub) { Components.Hint(L10n.T("请先进入存档")); return; }
            var ats = Refs.AthletesCached;
            if (ats.Count == 0) { Components.Hint(L10n.T("俱乐部无选手")); return; }

            _athPg = Mathf.Clamp(_athPg, 0, ats.Count - 1);
            var ath = ats[_athPg];
            var ad = ath.C_Data;

            // ---------- 选手选择（常驻） ----------
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button(L10n.T("◀"), GUILayout.Width(30))) _athPg = (_athPg - 1 + ats.Count) % ats.Count;
                GUILayout.Label($"[{_athPg + 1}/{ats.Count}]  {SafeName(ath)}  ({L10n.Role(ath.HeroRole)})", GUILayout.Width(200 * Theme.Scale));
                if (GUILayout.Button(L10n.T("▶"), GUILayout.Width(30))) _athPg = (_athPg + 1) % ats.Count;
                GUILayout.Label(L10n.T("改名"), GUILayout.Width(34 * Theme.Scale));
                _nameInput = GUILayout.TextField(_nameInput, GUILayout.Width(100 * Theme.Scale));
                if (GUILayout.Button(L10n.T("应用"), GUILayout.Width(46 * Theme.Scale)) && !string.IsNullOrEmpty(_nameInput))
                    WriteOps.SetName(ath, _nameInput);
                GUILayout.Label("", GUILayout.ExpandWidth(true));
            }

            GUILayout.Space(2);
            _sub = Components.SubNav(_sub, SubNames);
            GUILayout.Space(4);

            switch (_sub)
            {
                case 0: DrawAbilities(ad); break;
                case 1: DrawHeroPool(ad); break;
                case 2: DrawTraits(ath); break;
                case 3: DrawMilestones(ath); break;
                case 4: DrawExtra(ad, ath); break;
                case 5: DrawContract(ath); break;
                case 6: DrawRecruit(ath); break;
            }
        }

        // ================= 能力 =================
        private void DrawAbilities(Athlete.DataComponent ad)
        {
            Components.SectionTitle(L10n.T("五维能力"), Accent);
            for (int i = 0; i < AbilityFlags.Length; i++)
            {
                var flag = AbilityFlags[i];
                int cur = ad.GetAbilityLevel(flag, false);
                var captured = flag;
                Components.SliderRow(AbilityNames[i], cur, 0, MaxAbility,
                    v => WriteOps.SetAbility(ad, captured, (int)v),
                    () => WriteOps.SetAbility(ad, captured, MaxAbility));
            }
            using (new GUILayout.HorizontalScope())
            {
                if (Components.PrimaryButton(L10n.T("当前选手全满"), Accent, GUILayout.Height(26))) MaxAthlete(ad);
                if (Components.GhostButton(L10n.T("全部选手全满"), GUILayout.Height(26)))
                    foreach (var a in Refs.AthletesCached) MaxAthlete(a.C_Data);
            }

            Components.SectionTitle(L10n.T("能力上限突破"), Accent);
            try { Components.KeyValue(L10n.T("当前上限 / 总上限"), $"{ad.GetAbilityLimit()} / {ad.GetAbilityTotalMax()}"); } catch { }
            using (new GUILayout.HorizontalScope())
            {
                if (Components.PrimaryButton(L10n.T("突破上限 (AutoBreak)"), Accent, GUILayout.Height(26)))
                    WriteOps.AutoBreakLimit(ad);
                if (Components.GhostButton(L10n.T("超限全满 999"), GUILayout.Height(26))) MaxAthleteOver(ad);
            }
            Components.Hint(L10n.T("（AutoBreak 走游戏官方突破流程；超限全满直接改数据，可突破 120/维与总上限）"));

            Components.SectionTitle(L10n.T("能力潜力 (D~S+)"), Accent);
            foreach (var flag in AbilityFlags)
            {
                using (new GUILayout.HorizontalScope())
                {
                    string rank = "?";
                    try { rank = ad.GetAbilityPotentialRank(flag).ToString().Replace("_Plus", "+").Replace("_Minus", "-"); } catch { }
                    GUILayout.Label(L10n.Ability(flag) + ": " + rank, GUILayout.Width(150));
                    var captured = flag;
                    if (GUILayout.Button(L10n.T("S+"), GUILayout.Width(34))) WriteOps.SetPotential(ad, captured, AbilityPotentialRank.S_Plus);
                    if (GUILayout.Button(L10n.T("S"), GUILayout.Width(30))) WriteOps.SetPotential(ad, captured, AbilityPotentialRank.S);
                    if (GUILayout.Button(L10n.T("A"), GUILayout.Width(30))) WriteOps.SetPotential(ad, captured, AbilityPotentialRank.A);
                    if (GUILayout.Button(L10n.T("B"), GUILayout.Width(30))) WriteOps.SetPotential(ad, captured, AbilityPotentialRank.B);
                    if (GUILayout.Button(L10n.T("C"), GUILayout.Width(30))) WriteOps.SetPotential(ad, captured, AbilityPotentialRank.C);
                    if (GUILayout.Button(L10n.T("D"), GUILayout.Width(30))) WriteOps.SetPotential(ad, captured, AbilityPotentialRank.D);
                }
            }
            if (Components.GhostButton(L10n.T("全部潜力 S+")))
                foreach (var flag in AbilityFlags) WriteOps.SetPotential(ad, flag, AbilityPotentialRank.S_Plus);

            Components.SectionTitle(L10n.T("英雄适应性"), Accent);
            foreach (var hc in HeroClasses)
            {
                using (new GUILayout.HorizontalScope())
                {
                    // 官方中文职业名 + 当前适应性状态（高亮显示）
                    var curRank = HeroClassAdaption.None;
                    try { curRank = ad.GetHeroAdaption(hc); } catch { }
                    GUILayout.Label($"{L10n.ClassName(hc)}: {curRank}", GUILayout.Width(120));
                    var captured = hc;
                    foreach (var ha in AdaptionRanks)
                    {
                        var prevB = GUI.backgroundColor;
                        if (curRank == ha) GUI.backgroundColor = Theme.AccentOf(Theme.AccentColor.Green);
                        if (GUILayout.Button(ha.ToString(), GUILayout.Width(30)))
                            WriteOps.SetAdaption(ad, captured, ha);
                        GUI.backgroundColor = prevB;
                    }
                    GUILayout.Label("", GUILayout.ExpandWidth(true));
                }
            }
            if (Components.PrimaryButton(L10n.T("全职业适应性 S"), Accent, GUILayout.Height(26)))
                foreach (var hc in HeroClasses)
                    WriteOps.SetAdaption(ad, hc, HeroClassAdaption.S);
        }

        // ================= 英雄池 =================
        private void DrawHeroPool(Athlete.DataComponent ad)
        {
            Components.SectionTitle(L10n.T("英雄池 / 熟练度"), Accent);
            try
            {
                Components.KeyValue(L10n.T("英雄池"), $"{ad.GetHeroNum()} / {ad.GetHeroPoolMax()}");

                // 英雄池容量：自定义输入 + 减少/增加/应用
                int poolMax = ad.GetHeroPoolMax();
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(L10n.T("英雄池容量") + $": {poolMax}", GUILayout.Width(130));
                    int step = int.TryParse(_poolMaxInput, out int ps) && ps > 0 ? ps : 1;
                    if (GUILayout.Button(L10n.T("−"), GUILayout.Width(28))) WriteOps.SetHeroPoolMax(ad, Mathf.Max(1, poolMax - step));
                    if (GUILayout.Button(L10n.T("＋"), GUILayout.Width(28))) WriteOps.SetHeroPoolMax(ad, poolMax + step);
                    _poolMaxInput = GUILayout.TextField(_poolMaxInput, GUILayout.Width(70));
                    if (GUILayout.Button(L10n.T("应用"), GUILayout.Width(46)) && int.TryParse(_poolMaxInput, out int pv) && pv > 0)
                        WriteOps.SetHeroPoolMax(ad, pv);
                    GUILayout.Label("", GUILayout.ExpandWidth(true));
                }

                var keys = ad.heroDataDict.Keys.ToArray();
                foreach (var hk in keys)
                {
                    var hd = ad.heroDataDict[hk];
                    string hn = WriteOps.SafeName(hk);
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label((hn.Length > 9 ? hn.Substring(0, 9) : hn) + ": " + hd.level, GUILayout.Width(100));
                        for (int li = 0; li < MasteryLevels.Length; li++)
                        {
                            int lv = li + 1;
                            var prev = GUI.backgroundColor;
                            GUI.backgroundColor = hd.level >= lv ? Theme.AccentOf(Theme.AccentColor.Yellow) : new Color(0.3f, 0.3f, 0.3f);
                            if (GUILayout.Button(MasteryLevels[li], GUILayout.Width(38)))
                                WriteOps.SetMastery(ad, hk, lv);
                            GUI.backgroundColor = prev;
                        }
                        var captured = hk;
                        if (GUILayout.Button(L10n.T("✕"), GUILayout.Width(24))) WriteOps.RemoveHero(ad, captured);
                    }
                }
                using (new GUILayout.HorizontalScope())
                {
                    if (Components.PrimaryButton(L10n.T("全部升至宗师"), Theme.AccentColor.Yellow, GUILayout.Height(26)))
                        foreach (var k in ad.heroDataDict.Keys.ToArray())
                            WriteOps.SetMastery(ad, k, 7);
                    Components.DangerButton("clear_heroes", L10n.T("清空英雄池"), () => WriteOps.ClearHeroes(ad));
                }

                Components.Hint(L10n.T("添加英雄:"));
                var heroes = LoadHeroes();
                using (new GUILayout.HorizontalScope())
                {
                    if (Components.PrimaryButton(L10n.T("一键添加全部英雄"), Accent, GUILayout.Height(24)))
                    { int n = WriteOps.AddAllHeroes(ad, heroes, false); Log.Ok(T, $"一键添加英雄: 新增 {n} 个"); }
                    if (Components.GhostButton(L10n.T("全部添加 + 宗师"), GUILayout.Height(24)))
                    { int n = WriteOps.AddAllHeroes(ad, heroes, true); Log.Ok(T, $"一键添加英雄(宗师): 新增 {n} 个"); }
                }
                Components.Pagination(ref _heroPg, heroes.Count, 5, out int st, out int en);
                for (int i = st; i < en; i++)
                {
                    var h = heroes[i];
                    if (h == null) continue;
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label(WriteOps.SafeName(h), GUILayout.Width(140));
                        var captured = h;
                        if (Components.GhostButton(L10n.T("添加"))) WriteOps.AddHero(ad, captured, false);
                        if (Components.GhostButton(L10n.T("添加+宗师"))) WriteOps.AddHero(ad, captured, true);
                    }
                }
            }
            catch (System.Exception ex) { Components.Hint("英雄池异常: " + ex.Message); }
        }

        // ================= 特性流派 =================
        private void DrawTraits(Athlete ath)
        {
            Components.SectionTitle(L10n.T("流派 / 战术风格"), Accent);
            try
            {
                if (ath.C_Traits == null) { Components.Hint(L10n.T("无特性组件")); return; }

                var ps = ath.C_Traits.GetPrimeSet();
                int limit = ath.C_Traits.PrimeSetLimit;
                Components.KeyValue(L10n.T("多样性(流派上限)"), limit.ToString());
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(L10n.T("-"), GUILayout.Width(28))) WriteOps.SetPrimeSetLimit(ath, Mathf.Max(1, limit - 1));
                    if (GUILayout.Button(L10n.T("+"), GUILayout.Width(28))) WriteOps.SetPrimeSetLimit(ath, limit + 1);
                    if (Components.GhostButton(L10n.T("技能槽装满")))
                        WriteOps.FillSkills(ath);
                    if (Components.GhostButton(L10n.T("卡槽装满")))
                        WriteOps.FillCardSlots(ath);
                }
                if (ps != null)
                {
                    string prime = ps.Prime != null && ps.Prime.Setting != null ? SafeTraitName(ps.Prime.Setting) : "(空)";
                    Components.KeyValue(L10n.T("本命特性"), prime);
                    Components.KeyValue(L10n.T("技能"), ps.Skills == null || ps.Skills.Count == 0
                        ? "(空)"
                        : string.Join(", ", ps.Skills.Select(s => s != null && s.Setting != null ? SafeTraitName(s.Setting) : "?").ToArray()));
                    Components.KeyValue(L10n.T("卡槽"), ps.Slots == null ? "0" : ps.Slots.Count + " 个");
                }
            }
            catch (System.Exception ex) { Components.Hint("流派异常: " + ex.Message); }

            Components.SectionTitle(L10n.T("特性 / 天赋"), Accent);
            try
            {
                var traits = LoadTraits();
                if (traits.Count == 0) { Components.Hint(L10n.T("特性表不可用")); return; }

                var traitTypeVals = System.Enum.GetValues(typeof(AthleteTraitTypes)).Cast<AthleteTraitTypes>().ToList();
                var types = new List<string> { "全部" };
                types.AddRange(traitTypeVals.Select(L10n.TraitType));
                int sel = Components.PopupRow(L10n.T("类型:"), _traitTypeIdx, types.ToArray());
                if (sel != _traitTypeIdx) { _traitTypeIdx = sel; _traitPg = 0; }

                IEnumerable<AthleteTraitSetting> filtered = traits;
                if (sel > 0)
                {
                    var want = traitTypeVals[sel - 1];
                    filtered = traits.Where(t => { try { return t.TraitType == want; } catch { return false; } });
                }
                var list = filtered.ToList();
                Components.Pagination(ref _traitPg, list.Count, 5, out int st, out int en);
                for (int i = st; i < en; i++)
                {
                    var ts = list[i];
                    if (ts == null) continue;
                    using (new GUILayout.HorizontalScope())
                    {
                        string tp = "?";
                        try { tp = L10n.TraitType(ts.TraitType); } catch { }
                        GUILayout.Label("[" + tp + "] " + SafeTraitName(ts), GUILayout.Width(240));
                        var captured = ts;
                        if (Components.GhostButton(L10n.T("添加"), GUILayout.Width(60)))
                            WriteOps.AddTrait(ath, captured);
                    }
                }
                Components.Hint(L10n.T("（添加自动归位：本命 / 养成 / 技能；技能上限 5 个）"));
            }
            catch (System.Exception ex) { Components.Hint("特性异常: " + ex.Message); }
        }

        // ================= 附加 =================
        private void DrawExtra(Athlete.DataComponent ad, Athlete ath)
        {
            Components.SectionTitle(L10n.T("魅力 / 粉丝"), Accent);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(L10n.T("魅力"), GUILayout.Width(60));
                foreach (var cr in CharmRanks)
                {
                    int idx = System.Array.IndexOf(CharmRanks, cr);
                    if (GUILayout.Button(CharmNames[idx], GUILayout.Width(30)))
                        WriteOps.SetCharm(ad, cr);
                }
                GUILayout.Label("", GUILayout.ExpandWidth(true));
            }

            // 粉丝数：自定义输入 + 减少/增加/应用（增减步长 = 输入值，默认 1000）
            double fans = 0;
            try { fans = ad.GetFans(GameMain.StatTimeRangeTypes.Total); } catch { }
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(L10n.T("粉丝") + $": {fans:F0}", GUILayout.Width(150));
                long stepF = long.TryParse(_fansInput, out long fs) && fs > 0 ? fs : 1000;
                if (GUILayout.Button(L10n.T("−"), GUILayout.Width(28))) WriteOps.SetFans(ad, System.Math.Max(0, fans - stepF));
                if (GUILayout.Button(L10n.T("＋"), GUILayout.Width(28))) WriteOps.SetFans(ad, fans + stepF);
                _fansInput = GUILayout.TextField(_fansInput, GUILayout.Width(80));
                if (GUILayout.Button(L10n.T("应用"), GUILayout.Width(46)) && long.TryParse(_fansInput, out long fv) && fv >= 0)
                    WriteOps.SetFans(ad, fv);
                GUILayout.Label("", GUILayout.ExpandWidth(true));
            }

            Components.SectionTitle(L10n.T("称号 / 职位"), Accent);
            DrawTitles(ad, ath);

            Components.SectionTitle(L10n.T("性格 / 心情"), Accent);
            DrawPersonality(ad, ath);

            if (Refs.InBattle)
            {
                Components.SectionTitle(L10n.T("装备 / 战斗数据（战斗中）"), Accent);
                DrawEquipment(ath);
            }
        }

        // ================= 称号 / 职位 =================
        private void DrawTitles(Athlete.DataComponent ad, Athlete ath)
        {
            try
            {
                // 官方本地化名（旧 SafeName 反射 "Name" 属性不存在 → "?"）
                string cur = "?";
                try { cur = WriteOps.TitleName(ath, ad.GetAthleteTitleSetting()); } catch { }
                Components.KeyValue(L10n.T("当前称号"), cur);

                // 职位：首发主力 / 替补 / 青训 + 一键互换（走官方 ChangeAthlete/SelectSubstitute）
                Components.KeyValue(L10n.T("职位"), WriteOps.SquadStatus(ath));
                if (Components.GhostButton(L10n.T("与同位替补互换")))
                    WriteOps.SwapSquad(ath);

                var titles = LoadTitles();
                if (titles.Count == 0) { Components.Hint(L10n.T("称号表不可用")); return; }
                Components.Pagination(ref _titlePg, titles.Count, 5, out int st, out int en);
                for (int i = st; i < en; i++)
                {
                    var t = titles[i];
                    if (t == null) continue;
                    using (new GUILayout.HorizontalScope())
                    {
                        string cls = "?";
                        try { cls = t.TitleClass.ToString(); } catch { }
                        GUILayout.Label($"[{cls}] {WriteOps.TitleName(ath, t)}", GUILayout.Width(240));
                        var captured = t;
                        if (Components.GhostButton(L10n.T("授予"), GUILayout.Width(60)))
                            WriteOps.AddTitle(ad, captured);
                    }
                }
            }
            catch (System.Exception ex) { Components.Hint("称号异常: " + ex.Message); }
        }

        // ================= 性格 / 心情 =================
        private void DrawPersonality(Athlete.DataComponent ad, Athlete ath)
        {
            try
            {
                // ---- 已添加性格（支持删除） ----
                var addedSettings = new HashSet<AthletePersonalitySetting>();
                if (ad.Personalities != null && ad.Personalities.Count > 0)
                {
                    var seen = new HashSet<AthletePersonality>();
                    foreach (var p in ad.Personalities)
                    {
                        if (p == null || !seen.Add(p)) continue;
                        string pn = "?";
                        try { pn = p.Setting?.Name ?? "?"; addedSettings.Add(p.Setting); } catch { }
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("✔ " + pn, GUILayout.Width(200));
                            var capturedP = p;
                            if (Components.GhostButton(L10n.T("删除"), GUILayout.Width(50)))
                            {
                                try
                                {
                                    AthletePersonality.Despawn(capturedP);
                                    ad.Personalities?.Remove(capturedP);
                                    Log.Ok(T, "删除性格: " + pn);
                                }
                                catch (System.Exception ex) { Log.Exception(T, "删除性格", ex); }
                            }
                        }
                    }
                }
                else Components.KeyValue(L10n.T("当前性格"), L10n.T("(无)"));

                string emo = "无";
                int emoLv = -1;
                try
                {
                    emo = L10n.Emotion(ad.CurrentEmotion?.Setting?.Type.ToString());
                    emoLv = ad.CurrentEmotion.Level;
                }
                catch { }
                Components.KeyValue(L10n.T("心情"), emoLv >= 0 ? $"{emo} (Lv{emoLv}/6)" : emo);

                // 官方 7 级情绪直设（0~6），替代原来只会重置为"普通"的随机心情
                var emotionNames = new[] { "崩溃", "沮丧", "失落", "普通", "精神", "激动", "亢奋" };
                using (new GUILayout.HorizontalScope())
                {
                    for (int lv = 0; lv < emotionNames.Length; lv++)
                    {
                        int capturedLv = lv;
                        bool active = emoLv == lv;
                        var prevB = GUI.backgroundColor;
                        if (active) GUI.backgroundColor = Theme.AccentOf(Theme.AccentColor.Green);
                        if (GUILayout.Button(L10n.T(emotionNames[lv]), GUILayout.Width(46)))
                            WriteOps.SetEmotion(ath, capturedLv);
                        GUI.backgroundColor = prevB;
                    }
                }
                if (Components.PrimaryButton(L10n.T("全员心情最高 (亢奋)"), Theme.AccentColor.Green, GUILayout.Height(24)))
                    WriteOps.SetAllEmotion(6);

                // ---- 未添加性格（分页添加，自动排除已添加） ----
                Components.Hint(L10n.T("未添加性格:"));
                var perSets = LoadPersonalitySets()
                    .Where(s => s != null && !addedSettings.Contains(s)).ToList();
                if (perSets.Count > 0)
                {
                    Components.Pagination(ref _perPg, perSets.Count, PerPageSize, out int st, out int en);
                    for (int i = st; i < en; i++)
                    {
                        var ps = perSets[i];
                        if (ps == null) continue;
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label(ps.Name ?? "?", GUILayout.Width(200));
                            var captured = ps;
                            if (Components.GhostButton(L10n.T("添加"), GUILayout.Width(50)))
                            {
                                try { AthletePersonality.Spawn(captured, ath); Log.Ok(T, "添加性格: " + (captured.Name ?? "?")); }
                                catch (System.Exception ex) { Log.Exception(T, "添加性格", ex); }
                            }
                        }
                    }
                }
                else Components.Hint(L10n.T("全部性格均已添加"));

                using (new GUILayout.HorizontalScope())
                {
                    Components.DangerButton("clear_personality", L10n.T("清除所有性格"),
                        () =>
                        {
                            if (ad.Personalities == null) return;
                            int n = ad.Personalities.Count;
                            foreach (var p in ad.Personalities.ToList())
                            {
                                try { AthletePersonality.Despawn(p); } catch { }
                            }
                            ad.Personalities.Clear();
                            Log.Ok(T, $"清除性格: {n} 个");
                        });
                    if (Components.GhostButton(L10n.T("随机心情")))
                    {
                        try { WriteOps.SetEmotion(ath, UnityEngine.Random.Range(0, 7)); }
                        catch (System.Exception ex) { Log.Exception(T, "随机心情", ex); }
                    }
                    if (Components.GhostButton(L10n.T("清除心情")))
                    {
                        try { ath.OnRemoveAthleteEmotion(); Log.Ok(T, "心情已清除"); }
                        catch (System.Exception ex) { Log.Exception(T, "清除心情", ex); }
                    }
                }
            }
            catch (System.Exception ex) { Components.Hint("性格/心情异常: " + ex.Message); }
        }

        // ================= 战斗数据（战斗中）——中文标签 + 精确输入编辑 =================
        /// <summary>字段名 → 官方中文语义（对齐 AthleteSystem.BattleData）。</summary>
        private static readonly Dictionary<string, string> BattleDataLabels = new Dictionary<string, string>
        {
            ["tacticPoint"] = "战术点",
            ["athleteFightSpeed"] = "比赛速度",
            ["athleteAccuracyFight"] = "战斗命中",
            ["athleteEvasionFight"] = "战斗闪避",
            ["athleteAccuracyBattle"] = "战场命中",
            ["athleteEvasionBattle"] = "战场闪避",
            ["athleteEcoEfficiency"] = "打钱效率",
            ["athleteEcoTransferEfficiencyAttack"] = "进攻转化",
            ["athleteEcoTransferEfficiencyDefence"] = "防御转化",
            ["athleteEcoTransferEfficiency"] = "经济转化",
        };

        private void DrawBattleData(Athlete ath)
        {
            const System.Reflection.BindingFlags bfFlags =
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            try
            {
                // 真实路径：Athlete.C_Data.battleData；BattleData 是 struct：改副本后必须写回
                var bd = ath.C_Data?.battleData;
                bool any = false;
                foreach (var bf in BattleDataFields)
                {
                    var fld = typeof(GameMain.AthleteSystem.BattleData).GetField(bf, bfFlags);
                    if (fld == null) continue;
                    any = true;
                    object val = fld.GetValue(bd);
                    string label = BattleDataLabels.TryGetValue(bf, out var zh) ? zh : bf;
                    var inputs = Inputs(ath);
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label($"{label}: {val}", GUILayout.Width(170));
                        inputs.TryGetValue("bd_" + bf, out var v);
                        var nv = GUILayout.TextField(v ?? "", GUILayout.Width(80));
                        inputs["bd_" + bf] = nv;
                        var capturedBf = bf;
                        if (GUILayout.Button(L10n.T("应用"), GUILayout.Width(46)))
                        {
                            if (fld.FieldType == typeof(int)) { if (int.TryParse(nv, out int iv)) WriteBattleDataField(ath, capturedBf, iv); }
                            else if (double.TryParse(nv, out double dv)) WriteBattleDataField(ath, capturedBf, dv);
                        }
                        if (GUILayout.Button(L10n.T("最大"), GUILayout.Width(45))) WriteBattleDataField(ath, capturedBf, 999);
                        if (GUILayout.Button(L10n.T("归零"), GUILayout.Width(45))) WriteBattleDataField(ath, capturedBf, 0);
                        GUILayout.Label("", GUILayout.ExpandWidth(true));
                    }
                }
                if (!any) Components.Hint(L10n.T("  战斗数据字段不可用"));
            }
            catch { Components.Hint(L10n.T("  读取失败")); }
        }

        private Dictionary<string, string> Inputs(object owner)
        {
            if (!_editInputs.TryGetValue(owner, out var d))
            {
                d = new Dictionary<string, string>();
                _editInputs[owner] = d;
            }
            return d;
        }

        /// <summary>写 BattleData 单字段：改副本 → 写回 struct（旧反射写装箱副本不生效）。</summary>
        private static void WriteBattleDataField(Athlete ath, string field, double value)
        {
            const System.Reflection.BindingFlags bfFlags =
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            try
            {
                var bd = ath.C_Data.battleData;
                var fld = typeof(GameMain.AthleteSystem.BattleData).GetField(field, bfFlags);
                if (fld == null) return;
                object v = fld.FieldType == typeof(int) ? (object)(int)value : (object)value;
                fld.SetValue(bd, v);
                ath.C_Data.battleData = bd;   // 写回真实数据
                Log.Ok("Battle", $"{SafeName(ath)} {field} → {value}");
            }
            catch (System.Exception ex) { Log.Exception("Battle", "WriteBattleDataField", ex); }
        }

        // ================= 装备（战斗中） =================
        private void DrawEquipment(Athlete ath)
        {
            try
            {
                var unit = FindAthleteUnit(ath);
                if (unit == null) { Components.Hint(L10n.T("  该选手未在战斗中上场")); DrawBattleData(ath); return; }

                var equipComp = unit.C_Equipment;
                if (equipComp == null) { Components.Hint(L10n.T("  无装备组件")); return; }

                var allEquip = equipComp.AllEquipments;
                Components.KeyValue(L10n.T("当前装备"), allEquip.Count + " 件");
                foreach (var eq in allEquip)
                {
                    string eqName = "?";
                    try { eqName = eq.Setting?.Name ?? eq.GetType().Name ?? "?"; } catch { }
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("  " + eqName, GUILayout.Width(180));
                        var capturedEq = eq;
                        if (Components.GhostButton(L10n.T("移除")))
                        {
                            try
                            {
                                MelonLoader.MelonCoroutines.Start(equipComp.Co_RemoveEquipment(capturedEq, true));
                                Log.Ok("Battle", "移除装备: " + eqName);
                            }
                            catch (System.Exception ex) { Log.Exception("Battle", "移除装备", ex); }
                        }
                    }
                }

                if (Components.GhostButton(_showBattleData ? "战斗数据 ▾" : "战斗数据 ▸"))
                    _showBattleData = !_showBattleData;
                if (_showBattleData) DrawBattleData(ath);
            }
            catch (System.Exception ex) { Components.Hint("装备系统异常: " + ex.Message); }
        }

        private Unit FindAthleteUnit(Athlete athlete)
        {
            try
            {
                var b = Refs.B;
                if (b == null) return null;
                foreach (var team in new[] { b.Team0, b.Team1 })
                {
                    if (team == null) continue;
                    foreach (var u in team.Heroes)
                        if (u != null && u.Athlete == athlete) return u;
                }
            }
            catch { }
            return null;
        }

        // ================= 合同 =================
        private void DrawContract(Athlete ath)
        {
            // ---------- 选手信息：年龄 / 职业生命 / 分路 ----------
            Components.SectionTitle(L10n.T("选手信息"), Accent);
            try
            {
                var inputs = Inputs(ath);

                float age = ath.C_Data.GetAge();
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label($"{L10n.T("年龄")}: {age:F1}", GUILayout.Width(150));
                    _ageInput = GUILayout.TextField(_ageInput, GUILayout.Width(60));
                    if (GUILayout.Button(L10n.T("应用"), GUILayout.Width(46)) && float.TryParse(_ageInput, out float av))
                        WriteOps.SetAthleteAge(ath, av);
                    GUILayout.Label("", GUILayout.ExpandWidth(true));
                }

                int life = ath.C_Data.GetCareerLife();
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label($"{L10n.T("职业生命")}: {life}", GUILayout.Width(150));
                    _careerLifeInput = GUILayout.TextField(_careerLifeInput, GUILayout.Width(60));
                    if (GUILayout.Button(L10n.T("应用"), GUILayout.Width(46)) && int.TryParse(_careerLifeInput, out int lv))
                        WriteOps.SetCareerLife(ath, lv);
                    GUILayout.Label("", GUILayout.ExpandWidth(true));
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label($"{L10n.T("分路")}: {L10n.Role(ath.HeroRole)}", GUILayout.Width(150));
                    foreach (var role in new[] { HeroRoleFlags.Top, HeroRoleFlags.Mid, HeroRoleFlags.Jug, HeroRoleFlags.Adc, HeroRoleFlags.Sup })
                    {
                        var captured = role;
                        var prevB = GUI.backgroundColor;
                        if (ath.HeroRole == role) GUI.backgroundColor = Theme.AccentOf(Theme.AccentColor.Green);
                        if (GUILayout.Button(L10n.Role(role), GUILayout.Width(46)))
                            WriteOps.SetRole(ath, captured);
                        GUI.backgroundColor = prevB;
                    }
                    GUILayout.Label("", GUILayout.ExpandWidth(true));
                }
                Components.Hint(L10n.T("（改分路会同步改阵容注册位置；若与首发冲突请先在职位区转为替补）"));
            }
            catch (System.Exception ex) { Components.Hint("选手信息异常: " + ex.Message); }

            // ---------- 合同 ----------
            Components.SectionTitle(L10n.T("合同"), Accent);
            try
            {
                var contract = ath.C_Data.Contract;
                if (contract == null)
                {
                    Components.Hint(L10n.T("无合同数据"));
                    if (Components.GhostButton(L10n.T("生成默认合同")))
                    {
                        try { AthleteContract.SpawnDefault(ath); Log.Ok(T, "默认合同已生成"); }
                        catch (System.Exception ex) { Log.Exception(T, "生成合同", ex); }
                    }
                    return;
                }

                // 合同细则：当前值 + 输入编辑
                var inputs = Inputs(ath);
                Components.KeyValue(L10n.T("当前薪资 / 违约金"), $"{contract.GetSalary():N0} / {contract.GetPenalty():N0}");
                Components.KeyValue(L10n.T("满意度"), contract.GetCurrentSatisfaction().ToString());
                ContractEditRow(inputs, "salaryLv", $"{L10n.T("薪资档")} (cur {contract.SalaryLevel})", contract.SalaryLevel.ToString(),
                    s => { if (int.TryParse(s, out int v)) WriteOps.SetContractSalaryLevel(ath, v); });
                ContractEditRow(inputs, "signCostLv", $"{L10n.T("签约费档")} (cur {contract.SignCostLevel})", contract.SignCostLevel.ToString(),
                    s => { if (int.TryParse(s, out int v)) WriteOps.SetContractSignCostLevel(ath, v); });
                ContractEditRow(inputs, "remain", $"{L10n.T("剩余赛季")} (cur {contract.RemainSeasonCount})", contract.RemainSeasonCount.ToString(),
                    s => { if (int.TryParse(s, out int v)) WriteOps.SetContractRemainSeason(ath, v); });

                Components.KeyValue(L10n.T("试用 / 自由 / 违约"), $"{L10n.YesNo(contract.IsTrialContract)} / {L10n.YesNo(contract.IsFree)} / {L10n.YesNo(contract.IsBreak)}");
                using (new GUILayout.HorizontalScope())
                {
                    if (Components.PrimaryButton(L10n.T("续约"), Theme.AccentColor.Green, GUILayout.Height(26)))
                    {
                        try
                        {
                            var c = ath.C_Data.Contract;
                            if (c == null || c.IsTrialContract || c.IsFree)
                            {
                                AthleteContract.Despawn(ath.C_Data.Contract, true);
                                AthleteContract.SpawnDefault(ath);
                                ath.C_Data.Contract.IsContinue = true;
                            }
                            else if (c.IsBreak) AthleteContract.SpawnByBreak(c, true);
                            else AthleteContract.SpawnByContinue(c);
                            Log.Ok(T, "续约完成");
                        }
                        catch (System.Exception ex) { Log.Exception(T, "续约", ex); }
                    }
                    if (Components.GhostButton(L10n.T("重置默认合同")))
                    {
                        try
                        {
                            AthleteContract.Despawn(ath.C_Data.Contract, true);
                            AthleteContract.SpawnDefault(ath);
                            Log.Ok(T, "合同已重置为默认");
                        }
                        catch (System.Exception ex) { Log.Exception(T, "重置合同", ex); }
                    }
                }
            }
            catch (System.Exception ex) { Components.Hint("合同异常: " + ex.Message); }
        }

        private static void ContractEditRow(Dictionary<string, string> inp, string key, string label, string cur, Action<string> apply)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(220));
                inp.TryGetValue(key, out var v);
                var nv = GUILayout.TextField(v ?? "", GUILayout.Width(70));
                inp[key] = nv;
                if (GUILayout.Button(L10n.T("应用"), GUILayout.Width(46))) apply(nv);
                GUILayout.Label("", GUILayout.ExpandWidth(true));
            }
        }

        // ================= 招募 =================
        private void DrawRecruit(Athlete ath)
        {
            Components.SectionTitle(L10n.T("招募 / 全能改造"), Accent);
            // 只列 5 个合法单体分路（None/All 不可用于注册）
            var roles = new[] { HeroRoleFlags.Top, HeroRoleFlags.Mid, HeroRoleFlags.Jug, HeroRoleFlags.Adc, HeroRoleFlags.Sup };
            string[] roleNames = roles.Select(r => L10n.Role(r)).ToArray();
            _roleIdx = Mathf.Clamp(_roleIdx, 0, roles.Length - 1);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(L10n.T("位置:"), GUILayout.Width(40));
                _roleIdx = GUILayout.SelectionGrid(_roleIdx, roleNames, roleNames.Length, GUILayout.ExpandWidth(false));
            }
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(L10n.T("能力:"), GUILayout.Width(40));
                _recruitAbility = GUILayout.TextField(_recruitAbility, GUILayout.Width(60));
                GUILayout.Label(L10n.T("年龄:"), GUILayout.Width(40));
                _recruitAge = GUILayout.TextField(_recruitAge, GUILayout.Width(60));
                if (Components.PrimaryButton(L10n.T("招募新选手"), Theme.AccentColor.Green, GUILayout.Width(110), GUILayout.Height(26)))
                {
                    int.TryParse(_recruitAbility, out int ability);
                    int.TryParse(_recruitAge, out int age);
                    if (ability <= 0) ability = 600;
                    if (age <= 0) age = 18;
                    WriteOps.RecruitNew(Refs.C, roles[_roleIdx], ability, age);
                }
            }
            if (Components.PrimaryButton(L10n.T("一键满属性改造 (600/600/18/5/10)"), Theme.AccentColor.Green, GUILayout.Height(26)))
                WriteOps.RecreateAllRound(ath);
            Components.Hint(L10n.T("（招募后自动 5 英雄池 · 10 容量，可再用全能改造调整）"));
        }

        private int _roleIdx;

        // ================= 辅助 =================
        private static void MaxAthlete(Athlete.DataComponent ad)
        {
            foreach (var f in AbilityFlags)
                WriteOps.SetAbility(ad, f, MaxAbility);
            WriteOps.SetAbilityLimitOver(ad, 999);
        }

        /// <summary>超限全满：绕过钳制直接写 abilities 数据（999/维，总上限 999）。</summary>
        private static void MaxAthleteOver(Athlete.DataComponent ad)
        {
            WriteOps.SetAbilityLimitOver(ad, 999);
            foreach (var f in AbilityFlags)
                WriteOps.SetAbilityOver(ad, f, 999);
        }

        private static string SafeName(object o)
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

        private string SafeTraitName(object s)
        {
            if (s == null) return "?";
            if (_nameCache.TryGetValue(s, out var n)) return n;
            try { n = s.GetType().GetProperty("Name")?.GetValue(s, null)?.ToString() ?? "?"; }
            catch { n = "?"; }
            _nameCache[s] = n;
            return n;
        }

        private static string SafeFans(Athlete.DataComponent ad)
        {
            try { return ad.GetFans(StatTimeRangeTypes.Total).ToString("F0"); } catch { return "?"; }
        }

        private List<AthletePersonalitySetting> LoadPersonalitySets()
        {
            if (_perSets == null)
            {
                try { _perSets = SettingCenter.GetSettings<AthletePersonalitySetting>().ToList(); }
                catch { _perSets = new List<AthletePersonalitySetting>(); }
            }
            return _perSets;
        }

        private List<UnitSetting> LoadHeroes()
        {
            if (_heroes == null)
            {
                try { _heroes = SettingCenter.GetSettings<UnitSetting>().ToList(); }
                catch { _heroes = new List<UnitSetting>(); }
            }
            return _heroes;
        }

        private List<AthleteTraitSetting> LoadTraits()
        {
            if (_traits == null)
            {
                try { _traits = SettingCenter.GetSettings<AthleteTraitSetting>().ToList(); }
                catch { _traits = new List<AthleteTraitSetting>(); }
            }
            return _traits;
        }

        private List<AthleteTitleSetting> LoadTitles()
        {
            if (_titles == null)
            {
                try { _titles = SettingCenter.GetSettings<AthleteTitleSetting>().ToList(); }
                catch { _titles = new List<AthleteTitleSetting>(); }
            }
            return _titles;
        }

        private static GUIStyle _boldLabel;
        private static GUIStyle BoldLabel()
        {
            if (_boldLabel == null)
                _boldLabel = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.BoldAndItalic };
            return _boldLabel;
        }

        // ================= 选手里程碑（自成就 Tab 迁入，归属选手域） =================
        private void DrawMilestones(Athlete ath)
        {
            Components.SectionTitle(L10n.T("选手个人里程碑"), Accent);
            try
            {
                var ms = ath.C_Milestone?.milestones;
                if (ms == null || ms.Count == 0) { Components.Hint(L10n.T("该选手里程碑列表不可用")); return; }

                int done = ms.Count(m => { try { return m.State == MilestoneStates.Finished; } catch { return false; } });
                Components.KeyValue(L10n.T("进度"), $"{done} / {ms.Count}");
                if (Components.PrimaryButton(L10n.T("全部解锁并领奖"), Accent, GUILayout.Height(26)))
                    WriteOps.UnlockAllMilestones(ms);

                Components.Pagination(ref _milePg, ms.Count, 8, out int st, out int en);
                for (int i = st; i < en; i++)
                    MilestoneRow(ms[i]);
            }
            catch (System.Exception ex) { Components.Hint("选手里程碑异常: " + ex.Message); }
        }

        /// <summary>里程碑行：已完成金/绿高亮，未完成暗色 + 解锁钮。</summary>
        private void MilestoneRow(Milestone m)
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
            GUI.Label(new Rect(rowRect.x + 260, rowRect.y, 110, h),
                (finished ? "✓ " : "") + (finished ? L10n.T("✓ 已完成") : state), BoldLabel());
            GUI.contentColor = prevC;

            if (!finished)
            {
                var btnRect = new Rect(rowRect.xMax - 74, rowRect.y + (h - 22 * Theme.Scale) / 2,
                    64 * Theme.Scale, 22 * Theme.Scale);
                var captured = m;
                if (GUI.Button(btnRect, L10n.T("解锁"), Theme.GhostBtn))
                    WriteOps.UnlockMilestone(captured);
            }
        }
    }
}
