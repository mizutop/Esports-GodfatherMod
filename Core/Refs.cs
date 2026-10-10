using System;
using System.Linq;
using System.Text;
using GameMain;
using GameMain.AthleteSystem;
using GameMain.BattleSystem;
using GameMain.ClubSystem;
using MelonLoader;
using UnityEngine;

namespace EsportsTrainer.Core
{
    /// <summary>
    /// Core/Refs.cs — L2/L3 数据访问层。
    /// 全局锚点：Main.Inst → Game → ClubPlayer / Battle / Calendar。
    /// 全部访问带异常兜底，60 帧节流刷新缓存。
    /// </summary>
    public static class Refs
    {
        public static Game G;
        public static Club C;
        public static Club.DataComponent CD;
        public static Battle B;

        public static bool HasGame => G != null;
        public static bool HasClub => C != null && CD != null;
        public static bool InBattle => B != null;

        public static void Refresh()
        {
            try { G = GameMain.Main.Inst != null ? GameMain.Main.Inst.Game : null; } catch { G = null; }
            try { C = G != null ? G.ClubPlayer : null; } catch { C = null; }
            try { CD = C != null ? C.C_Data : null; } catch { CD = null; }
            try { B = G != null ? G.Battle : null; } catch { B = null; }
        }

        public static string DateText()
        {
            try { return G != null && G.Calendar != null && G.Calendar.Date != null ? G.Calendar.Date.ToString() : "未知"; }
            catch { return "?"; }
        }

        // ================= 选手列表缓存（性能：IMGUI 每帧多次事件，禁止反复枚举直播集合） =================
        private static System.Collections.Generic.List<Athlete> _athCache;
        private static float _athCacheTime;

        /// <summary>0.5 秒节流的选手快照；面板渲染一律走此缓存，勿直接 C.Athletes.ToList()。</summary>
        public static System.Collections.Generic.List<Athlete> AthletesCached
        {
            get
            {
                if (_athCache == null || Time.unscaledTime - _athCacheTime > 0.5f)
                {
                    try { _athCache = C?.Athletes?.ToList() ?? new System.Collections.Generic.List<Athlete>(); }
                    catch { _athCache = new System.Collections.Generic.List<Athlete>(); }
                    _athCacheTime = Time.unscaledTime;
                }
                return _athCache;
            }
        }

        // ================= F2 数据快照 =================
        public static void DumpSnapshot(MelonLogger.Instance log)
        {
            Refresh();
            var sb = new StringBuilder();

            Rule(sb, '═');
            sb.AppendLine("  电竞教父 · 数据快照  " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Rule(sb, '═');

            if (!HasGame)
            {
                sb.AppendLine("  ◇ 状态: 游戏实例不可用（未进入存档）");
                Rule(sb, '═');
                Flush(log, sb);
                return;
            }

            // ---- 会话 ----
            sb.AppendLine($"  ◇ 会话   运行中 {G.IsRunning} · 当前时间 {DateText()}");
            string battleLine = "无";
            if (InBattle)
            {
                battleLine = $"进行中 · 回合 {Safe(() => B.Rounds.ToString())} · 倍速 x{Safe(() => B.C_Data.GetFightSpeed().ToString())}";
                if (BattlePatches.ForceWinActive)
                    battleLine += BattlePatches.ForcePlayerWin ? " · 强制胜负锁定(胜)" : " · 强制胜负锁定(败)";
            }
            sb.AppendLine("  ◇ 战斗   " + battleLine);

            // ---- 俱乐部 ----
            if (HasClub)
            {
                Rule(sb, '─');
                sb.AppendLine("  ▍俱乐部");
                sb.AppendLine($"  · 资源     预算 {Safe(() => CD.GetBudget().ToString("N0"))} · 教练点 {Safe(() => CD.GetCoachPoint().ToString("N0"))} · 训练点 {Safe(() => CD.GetTrainPoint().ToString("N0"))}");
                sb.AppendLine($"  · 声望     世界分 {Safe(() => CD.WorldPoint.ToString("N0"))} · 粉丝 {Safe(() => C.Fans.ToString("N0"))}");
                sb.AppendLine($"  · 研究     等级 {Safe(() => CD.trainingResearchLevel.ToString())} / 次数 {Safe(() => CD.trainingResearchCount.ToString())}");
                sb.AppendLine($"  · 勋章     已解锁 {Safe(() => (C.C_Medal?.StoreMedals?.Keys?.Count ?? 0).ToString())} 枚");
                sb.AppendLine($"  · 荣誉     {Safe(() => (CD.ClubHonorDatas != null ? CD.ClubHonorDatas.Count : 0).ToString())} 条奖杯记录");

                // ---- 选手 ----
                Rule(sb, '─');
                sb.AppendLine("  ▍选手");
                var ats = new System.Collections.Generic.List<Athlete>();
                try { foreach (var a in C.Athletes) ats.Add(a); } catch { }
                sb.AppendLine($"  · 在册 {ats.Count} 人");
                foreach (var a in ats.GetRange(0, System.Math.Min(8, ats.Count)))
                {
                    if (a == null) continue;
                    string name = "?";
                    try { name = a.GetName() ?? "?"; } catch { }
                    sb.AppendLine(string.Format(
                        "    {0,-12} {1,-8} 五维 {2}/{3}/{4}/{5}/{6}  上限 {7}  英雄 {8}",
                        name,
                        a.HeroRole.ToString(),
                        Lv(a, AthleteAbilityFlags.Aggressive), Lv(a, AthleteAbilityFlags.Defence),
                        Lv(a, AthleteAbilityFlags.Farming), Lv(a, AthleteAbilityFlags.Reflection),
                        Lv(a, AthleteAbilityFlags.Exercise),
                        SafeOn(a.C_Data, d => d.GetAbilityLimit().ToString()),
                        SafeOn(a.C_Data, d => d.GetHeroNum().ToString())));
                }
            }

            Rule(sb, '═');
            Flush(log, sb);
        }

        private static void Rule(StringBuilder sb, char c) => sb.AppendLine("  " + new string(c, 56));

        private static string Safe(Func<string> f) { try { return f(); } catch { return "?"; } }
        private static string SafeOn<T>(T obj, Func<T, string> f) { try { return obj != null ? f(obj) : "?"; } catch { return "?"; } }
        private static string Lv(Athlete a, AthleteAbilityFlags f)
        {
            try { return a.C_Data.GetAbilityLevel(f, false).ToString(); } catch { return "?"; }
        }

        private static void Flush(MelonLogger.Instance log, StringBuilder sb)
        {
            foreach (var line in sb.ToString().Split('\n'))
            {
                var t = line.TrimEnd('\r');
                if (t.Length > 0) log.Msg(t);
            }
        }
    }
}
