using System.Collections.Generic;
using GameMain.AthleteSystem;
using GameMain.ClubSystem;

namespace EsportsTrainer.Core
{
    /// <summary>
    /// Core/L10n.cs — 界面本地化（中/英语言包）。
    /// · 枚举译名对齐游戏官方 InspectorName：进攻/防守/发育/反应/操作、上单/打野/中单/ADC/辅助
    /// · T(zh)：以中文原文为键查英文表，未收录时回退中文原文（零破坏）
    /// </summary>
    public static class L10n
    {
        // ================= 语言包开关 =================
        public static string Lang = "zh";   // "zh" | "en"

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            // ================= 通用按钮 =================
            ["授予"] = "Grant", ["解锁"] = "Unlock", ["添加"] = "Add", ["移除"] = "Remove",
            ["删除"] = "Delete", ["应用"] = "Apply", ["设置"] = "Set", ["最大"] = "Max", ["归零"] = "Zero",
            ["改名"] = "Rename", ["晋级"] = "Promote", ["降级"] = "Demote",
            ["教练晋升"] = "Coach Up", ["赞助"] = "Donate", ["重置"] = "Reset",
            ["随机心情"] = "Random Mood", ["清除心情"] = "Clear Mood", ["确认"] = "Confirm",
            ["清除所有性格"] = "Clear Personalities",
            ["清空英雄池"] = "Clear Hero Pool", ["全部潜力 S+"] = "All Potential S+",
            ["当前选手全满"] = "Max This Athlete", ["全部选手全满"] = "Max All Athletes",
            ["突破上限 (AutoBreak)"] = "Break Limit (AutoBreak)", ["超限全满 999"] = "Overlimit Max 999",
            ["教练直升 S（含奖励）"] = "Coach to S (with rewards)",
            ["解锁全部升级项（技能树去锁）"] = "Unlock All Upgrades (tree unlock)",
            ["资源全满"] = "Max Resources", ["一键全部解锁（含 Steam 同步）"] = "Unlock All (incl. Steam sync)",
            ["全部解锁并领奖"] = "Unlock All & Claim", ["支持度拉满 (确保晋级)"] = "Max Support (secure promotion)",
            ["升级到下一级场地"] = "Upgrade Venue", ["一键还清全部贷款"] = "Repay All Loans",
            ["一键全解锁"] = "Unlock All", ["搜索解锁"] = "Search & Unlock",
            ["全部升至宗师"] = "All to Grandmaster", ["添加+宗师"] = "Add + GM",
            ["补满手牌"] = "Fill Hand", ["清空手牌"] = "Clear Hand", ["批量改费"] = "Set Cost",
            ["暂停 / 恢复"] = "Pause / Resume", ["立即终结 (击杀敌方基地)"] = "End Now (kill enemy base)",
            ["强制获胜"] = "Force Win", ["强制失败"] = "Force Lose",
            ["锁定获胜中… (点击取消)"] = "Locking WIN… (click to cancel)",
            ["锁定失败中… (点击取消)"] = "Locking LOSE… (click to cancel)",
            ["招募 / 全能改造"] = "Recruit / Reroll", ["招募新选手"] = "Recruit Athlete",
            ["展开明细"] = "Expand", ["收起明细"] = "Collapse",
            ["与同位替补互换"] = "Swap with substitute",
            ["一键亲密 (主选×全员)"] = "Intimate All (with selected)", ["全员两两亲密"] = "All Pairs Intimate",
            ["一键添加全部英雄"] = "Add All Heroes", ["全部添加 + 宗师"] = "Add All + GM",
            ["全职业适应性 S"] = "All Classes Adapt. S",
            ["技能槽装满"] = "Fill Skill Slots", ["卡槽装满"] = "Fill Card Slots",
            ["教练点 +1000"] = "Coach Pts +1000", ["训练点 +1000"] = "Train Pts +1000",
            ["预算 +100000"] = "Budget +100000", ["一键满属性改造 (600/600/18/5/10)"] = "Reroll Max (600/600/18/5/10)",
            ["生成默认合同"] = "Generate Default Contract", ["重置默认合同"] = "Reset Contract",
            ["续约"] = "Renew", ["训练点 5x"] = "Train Pts 5x",
            ["恢复默认倍率"] = "Reset Multiplier", ["恢复赛事格式默认 (BO1/BO3/BO5)"] = "Reset Formats (BO1/BO3/BO5)",
            ["推进下一周 (赛程队列入队)"] = "Advance Week (queue schedule)", ["锁定规则 (不变)"] = "Lock Rules (no change)",

            // ================= Tab / 子导航 =================
            ["俱乐部"] = "Club", ["选手"] = "Athletes", ["成就"] = "Achieve",
            ["战斗"] = "Battle", ["赛事"] = "Season",
            ["资源"] = "Res", ["勋章"] = "Medals", ["里程碑"] = "Milestones",
            ["发展"] = "Develop", ["荣誉"] = "Honors",
            ["能力"] = "Ability", ["英雄池"] = "Heroes", ["特性流派"] = "Traits",
            ["附加"] = "Extra", ["合同"] = "Contract", ["招募"] = "Recruit",
            ["队内关系"] = "Relations", ["选手里程碑"] = "Athlete Miles.",

            // ================= 区块标题 =================
            ["俱乐部资源"] = "Club Resources", ["勋章系统"] = "Medals",
            ["俱乐部里程碑"] = "Club Milestones", ["晋级 / 评级"] = "Promotion / Rating",
            ["教练等级 / 技能树"] = "Coach Level / Skill Tree",
            ["观众支持度 / 晋级线"] = "Show Support / Line",
            ["贷款"] = "Loans", ["训练研究"] = "Training Research",
            ["荣誉 / 奖杯"] = "Honors / Trophies",
            ["五维能力"] = "Five Abilities", ["能力潜力 (D~S+)"] = "Potential (D~S+)",
            ["能力上限突破"] = "Break Ability Limit", ["英雄适应性"] = "Hero Adaptability",
            ["英雄池 / 熟练度"] = "Hero Pool / Mastery",
            ["流派 / 战术风格"] = "Prime Sets / Styles", ["特性 / 天赋"] = "Traits / Talents",
            ["称号 / 职位"] = "Titles / Position", ["魅力 / 粉丝"] = "Charm / Fans",
            ["性格 / 心情"] = "Personality / Mood",
            ["装备 / 战斗数据（战斗中）"] = "Equip / Battle Data (in battle)",
            ["成就系统"] = "Achievements", ["选手个人里程碑"] = "Athlete Milestones",
            ["战斗状态"] = "Battle Status", ["速度 / 暂停"] = "Speed / Pause",
            ["强制胜负 (Harmony)"] = "Force Result (Harmony)",
            ["己方增益 / 对方削弱"] = "Buff Ally / Debuff Enemy", ["手牌控制"] = "Hand Cards",
            ["赛程"] = "Schedule", ["赛事格式"] = "Match Formats", ["版本规则"] = "Version Rules",
            ["训练 / 难度"] = "Training / Difficulty",

            // ================= 底栏 / 状态栏 =================
            ["预算"] = "Budget", ["教练点"] = "Coach Pts", ["训练点"] = "Train Pts",
            ["战斗中"] = "In Battle", ["经营中"] = "Managing",
            ["等待进入存档…"] = "Waiting for save…",
            ["尺寸"] = "Size", ["主色"] = "Primary", ["辅色"] = "Accent", ["金色"] = "Gold",
            ["自定义 ▾"] = "Custom ▾", ["自定义 ▸"] = "Custom ▸",
            ["语言 · 中文"] = "Lang · EN", ["格式 #8A5CFF，留空=预设"] = "Format #8A5CFF, empty=preset",

            // ================= 键值标签 =================
            ["当前称号"] = "Title", ["职位"] = "Position", ["当前性格"] = "Personality",
            ["心情"] = "Mood", ["总进度"] = "Progress", ["进度"] = "Progress",
            ["已解锁"] = "Unlocked", ["当前级别"] = "Level", ["教练等级"] = "Coach Level",
            ["当前支持度"] = "Support", ["当前等级 / 次数"] = "Level / Count",
            ["当前上限 / 总上限"] = "Limit / Total Max",
            ["回合 / 速度"] = "Round / Speed", ["手牌 / 卡池"] = "Hand / Pool",
            ["多样性(流派上限)"] = "Prime Set Limit", ["本命特性"] = "Prime Trait",
            ["技能"] = "Skills", ["卡槽"] = "Card Slots", ["奖杯记录"] = "Trophy Records",
            ["粉丝"] = "Fans", ["粉丝数"] = "Fans", ["魅力"] = "Charm",
            ["当前日期"] = "Date", ["世界分"] = "World Pts", ["剩余赛季 / 等级"] = "Seasons Left / Lv",
            ["薪资基础"] = "Salary Base", ["训练点倍率"] = "Train Pts Mult",
            ["英雄池容量"] = "Hero Pool Cap", ["当前装备"] = "Equipment",
            ["首发主力"] = "Starter", ["替补"] = "Substitute", ["青训"] = "Youth",
            ["费用:"] = "Cost:", ["倍速:"] = "Speed:", ["HP:"] = "HP:", ["盾:"] = "Shield:", ["金:"] = "Gold:",

            // ================= 关系 / 情绪 / 位置 / 职业 =================
            ["对立"] = "Opposed", ["疏远"] = "Distant", ["普通"] = "Normal",
            ["友好"] = "Friendly", ["亲密"] = "Intimate",
            ["崩溃"] = "Collapse", ["沮丧"] = "Depressed", ["失落"] = "Lost",
            ["精神"] = "Spirit", ["激动"] = "Excited", ["亢奋"] = "High",
            ["无"] = "None", ["全部"] = "All",
            ["上单"] = "Top", ["打野"] = "Jungle", ["中单"] = "Mid", ["辅助"] = "Support",
            ["坦克"] = "Tank", ["战士"] = "Warrior", ["刺客"] = "Assassin",
            ["法师"] = "Mage", ["射手"] = "Marksman", ["角斗"] = "Duelist",

            // ================= 战斗开关 =================
            ["己方·无限生命"] = "Ally·Infinite HP", ["己方·无限护盾"] = "Ally·Infinite Shield",
            ["己方·清除护盾"] = "Ally·Clear Shield", ["己方·无限金钱"] = "Ally·Infinite Gold",
            ["己方·清零金钱"] = "Ally·Zero Gold", ["己方·战术点 99"] = "Ally·Tactic 99",
            ["对方·无限生命"] = "Enemy·Infinite HP", ["对方·无限护盾"] = "Enemy·Infinite Shield",
            ["对方·清除护盾"] = "Enemy·Clear Shield", ["对方·无限金钱"] = "Enemy·Infinite Gold",
            ["对方·清零金钱"] = "Enemy·Zero Gold", ["对方·战术点 0"] = "Enemy·Tactic 0",
            ["BP 自动加速"] = "BP Auto Speed", ["BP 自动加速 [锁定中]"] = "BP Auto Speed [locked]",

            // ================= 提示 / 其他 =================
            ["请先进入存档"] = "Enter a save first",
            ["请先进入存档（俱乐部数据不可用）"] = "Enter a save first (club data unavailable)",
            ["俱乐部无选手"] = "No athletes in club",
            ["称号表不可用"] = "Title table unavailable", ["成就表不可用"] = "Achievement table unavailable",
            ["特性表不可用"] = "Trait table unavailable", ["里程碑列表不可用"] = "Milestone list unavailable",
            ["该选手里程碑列表不可用"] = "Milestone list unavailable for this athlete",
            ["无特性组件"] = "No traits component", ["无合同数据"] = "No contract data",
            ["GameCustomData 不可用"] = "GameCustomData unavailable",
            ["当前无战斗（进入一场比赛后启用战斗功能）"] = "No battle (enter a match to enable battle features)",
            ["选手不足 2 人"] = "Fewer than 2 athletes",
            ["  读取失败"] = "  Read failed",
            ["  该选手未在战斗中上场"] = "  Athlete not fielded in this battle",
            ["  荣誉"] = "  Honor", ["  无装备组件"] = "  No equipment component",
            ["  战斗数据字段不可用"] = "  Battle data fields unavailable",
            ["(无)"] = "(none)", ["✓ 已完成"] = "✓ Done", ["未完成"] = "Not done",
            ["◀ 页"] = "◀ Prev", ["页 ▶"] = "Next ▶", ["页"] = "Page", ["共"] = "of",
            ["添加英雄:"] = "Add heroes:", ["未添加性格:"] = "Not added:",
            ["全部性格均已添加"] = "All personalities added",
            ["解锁: "] = "Unlock: ",
            ["为「主选 × 其他选手」设置关系："] = "Set relations between the selected athlete and others:",
            ["（-2 对立 · 0 普通 · +1 友好 · +2 亲密；不存在的条目自动创建）"] = "(-2 opposed · 0 neutral · +1 friendly · +2 intimate; missing entries auto-created)",
            ["（等级受游戏配置 MaxCount 上限约束）"] = "(level capped by game config MaxCount)",
            ["（技能树节点锁定 = 教练等级不足；去锁后即可在俱乐部升级界面直接点亮）"] = "(node locked = coach level too low; unlock it then activate in Club Upgrade)",
            ["（名称/进度实时同步游戏内；进度取 本地进度与最高记录 的较大值）"] = "(names/progress sync in real time; progress = max(local, record))",
            ["（添加自动归位：本命 / 养成 / 技能；技能上限 5 个）"] = "(auto-slot on add: prime/develop/skill; max 5 skills)",
            ["（推进赛程会在下周排入比赛，请在非比赛周使用）"] = "(advancing queues matches next week; use outside match weeks)",
            ["（招募后自动 5 英雄池 · 10 容量，可再用全能改造调整）"] = "(recruit grants 5-hero pool · cap 10; adjust via Reroll)",
            ["（AutoBreak 走游戏官方突破流程；超限全满直接改数据，可突破 120/维与总上限）"] = "(AutoBreak uses official flow; Overlimit writes data directly, may exceed 120/ability)",
            ["次数:"] = "Count:", ["等级:"] = "Level:", ["当前等级"] = "Level",
            ["类型:"] = "Type:", ["能力:"] = "Ability:", ["年龄:"] = "Age:", ["位置:"] = "Position:",
            ["规则变更模式"] = "Rule Change Mode", ["俱乐部场地"] = "Club Venue",
            ["配色 · "] = "Theme · ", ["试用 / 自由 / 违约"] = "Trial / Free / Buyout",
            ["渲染异常，已熔断 3 秒"] = "Render error, paused 3s",
            ["训练必成 (Harmony)"] = "Guaranteed Training (Harmony)",
            ["日常训练必定大成功"] = "Daily Training Always Great Success",
            ["能力训练必定大成功"] = "Ability Training Always Great Success",
            ["俱乐部升级全部完成"] = "Complete All Club Upgrades",
            ["全员心情最高 (亢奋)"] = "All Athletes Max Mood (High)",
            ["己方"] = "Ally", ["对方"] = "Enemy",
            ["无限生命"] = "Infinite HP", ["无限护盾"] = "Infinite Shield",
            ["清除护盾"] = "Clear Shield", ["无限金钱"] = "Infinite Gold",
            ["清零金钱"] = "Zero Gold", ["战术点 99"] = "Tactic 99", ["战术点 0"] = "Tactic 0",
            ["比赛中"] = "Match",
            ["赛点 / 全局"] = "Match Points / Global",
            ["回合"] = "Round", ["倍速"] = "Speed",
            ["魔龙倒计时"] = "Dragon CD", ["先锋倒计时"] = "Pioneer CD",
            ["自动BP (倒计时压缩)"] = "Auto BP (compress timer)",
            ["禁止AI出牌 (仅敌方)"] = "Forbid AI Cards (enemy only)",
            ["我方AI自动指挥 (自动出牌/结束回合)"] = "Ally AI Auto-Command (auto play/end)",
            ["我方选手"] = "Our Athletes", ["敌方选手"] = "Enemy Athletes",
            ["我方战术点"] = "Ally Tactic", ["敌方战术点"] = "Enemy Tactic",
            ["等级(经验)"] = "Level (Exp)", ["当前血量"] = "HP", ["最大血量"] = "Max HP",
            ["攻击力"] = "Attack", ["护甲"] = "Armor", ["暴击率"] = "Crit %", ["存在感"] = "Presence",
            ["装备"] = "Equip", ["添加装备"] = "Add Equipment", ["收起"] = "Collapse",
            ["无上场选手"] = "No fielded athletes", ["金钱"] = "Gold",
            ["选手信息"] = "Athlete Info", ["年龄"] = "Age", ["职业生命"] = "Career Life",
            ["分路"] = "Role", ["当前薪资 / 违约金"] = "Salary / Penalty", ["满意度"] = "Satisfaction",
            ["薪资档"] = "Salary Lv", ["签约费档"] = "Sign-Cost Lv", ["剩余赛季"] = "Seasons Left",
            ["剩余"] = "Left", ["利率"] = "Rate", ["金额"] = "Amount",
            ["立即还贷 (0 元还贷)"] = "Repay Now (0 cost)", ["当前无贷款"] = "No loans",
            ["每月应付本息"] = "Monthly Due",
        };
        /// <summary>取翻译：en 表命中返回英文，否则回退中文原文。</summary>
        public static string T(string zh)
            => Lang == "en" && zh != null && En.TryGetValue(zh, out var s) ? s : zh;

        public static void ToggleLang()
            => Lang = Lang == "zh" ? "en" : "zh";

        // ================= 五维能力（带语言包） =================
        public static string Ability(AthleteAbilityFlags f) => T(f switch
        {
            AthleteAbilityFlags.Aggressive => "进攻",
            AthleteAbilityFlags.Defence => "防守",
            AthleteAbilityFlags.Farming => "发育",
            AthleteAbilityFlags.Reflection => "反应",
            AthleteAbilityFlags.Exercise => "操作",
            _ => f.ToString()
        });

        // ================= 位置（对齐官方 HeroRoleFlags：上单/中单/打野/ADC/辅助/全部） =================
        public static string Role(GameMain.UnitSystem.HeroRoleFlags r)
        {
            var s = r.ToString().ToLowerInvariant();
            if (s.Contains("top")) return T("上单");
            if (s.Contains("jug")) return T("打野");   // 官方枚举名是 Jug，不是 Jungle
            if (s.Contains("mid")) return T("中单");
            if (s.Contains("adc") || s.Contains("bot")) return "ADC";
            if (s.Contains("sup")) return T("辅助");
            if (s.Contains("all")) return T("全部");
            if (s.Contains("none")) return T("无");
            return r.ToString();
        }

        // ================= 特性类型（对齐官方 AthleteTraitTypes） =================
        public static string TraitType(AthleteTraitTypes t) => T(t switch
        {
            AthleteTraitTypes.Prime => "特性",
            AthleteTraitTypes.Skill => "技能",
            AthleteTraitTypes.Develop => "养成",
            AthleteTraitTypes.HeroSkill => "英雄技能",
            _ => t.ToString()
        });

        // ================= 里程碑状态（对齐官方 MilestoneStates） =================
        public static string MilestoneState(MilestoneStates s) => T(s switch
        {
            MilestoneStates.Unrevealed => "未揭示",
            MilestoneStates.Revealed => "已揭示",
            MilestoneStates.CanTakeReward => "可以领奖",
            MilestoneStates.Finished => "完成",
            _ => s.ToString()
        });

        // ================= 心情（对齐官方 AthleteEmotionType：崩溃/沮丧/失落/普通/精神/激动/亢奋） =================
        public static string Emotion(string raw) => T(raw switch
        {
            "Collapse" => "崩溃",
            "Depressed" => "沮丧",
            "Lost" => "失落",
            "Normal" => "普通",
            "Spirit" => "精神",
            "Excited" => "激动",
            "High" => "亢奋",
            "" or null => "无",
            _ => raw
        });

        // ================= 英雄职业（对齐官方 HeroClassTypes） =================
        public static string ClassName(GameMain.UnitSystem.HeroClassTypes c) => T(c switch
        {
            GameMain.UnitSystem.HeroClassTypes.Tank => "坦克",
            GameMain.UnitSystem.HeroClassTypes.Warrior => "战士",
            GameMain.UnitSystem.HeroClassTypes.Assassin => "刺客",
            GameMain.UnitSystem.HeroClassTypes.Magician => "法师",
            GameMain.UnitSystem.HeroClassTypes.Shooter => "射手",
            GameMain.UnitSystem.HeroClassTypes.Support => "辅助",
            GameMain.UnitSystem.HeroClassTypes.LightWarrior => "角斗",
            _ => c.ToString()
        });

        // ================= 成就进度通用 =================
        public static string YesNo(bool b) => T(b ? "是" : "否");
    }
}
