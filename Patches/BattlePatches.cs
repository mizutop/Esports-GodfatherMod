using System;
using System.Reflection;
using GameMain.BattleSystem;
using HarmonyLib;

namespace EsportsTrainer.Core
{
    /// <summary>
    /// Patches/BattlePatches.cs — 集中式 Harmony 补丁（Mono 反射手动安装，单补丁失败不影响其余）。
    /// ① GetWinnerTeamIndex 强制胜负：Postfix 改返回值，比分/MVP/结算/世界分全自动跟随。
    /// </summary>
    public static class BattlePatches
    {
        // ---- 共享状态（由 BattleTab UI 驱动）----
        public static bool ForceWinActive;
        public static bool ForcePlayerWin = true;
        public static bool ForbidAIUseCard;   // 禁止敌方 AI 出牌

        public static void Install(HarmonyLib.Harmony harmony)
        {
            TryPatch(harmony, nameof(GetWinnerTeamIndexPostfix),
                AccessTools.Method(typeof(Battle), nameof(Battle.GetWinnerTeamIndex)),
                new HarmonyMethod(typeof(BattlePatches), nameof(GetWinnerTeamIndexPostfix)));
            TryPatch(harmony, "AI.TurnActionIsEnd → 禁止AI出牌",
                AccessTools.Property(typeof(BattleTeamAgentScriptAI), "TurnActionIsEnd")?.GetGetMethod(),
                new HarmonyMethod(typeof(BattlePatches), nameof(AITurnEndPostfix)));
        }

        private static void TryPatch(HarmonyLib.Harmony harmony, string name, MethodInfo original, HarmonyMethod postfix)
        {
            try
            {
                if (original == null) { MelonLoader.MelonLogger.Warning($"[Patch] {name}: 目标方法未找到，跳过"); return; }
                harmony.Patch(original, postfix: postfix);
                MelonLoader.MelonLogger.Msg($"[Patch] {name} 安装成功");
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"[Patch] {name} 安装失败: {ex.Message}");
            }
        }

        // ① 强制胜负
        private static void GetWinnerTeamIndexPostfix(Battle __instance, ref int __result)
        {
            try
            {
                if (!ForceWinActive) return;
                var t0 = __instance.Team0;
                var t1 = __instance.Team1;
                if (t0 == null || t1 == null) return;
                // 玩家胜 → 返回敌方队伍索引；玩家败 → 返回己方队伍索引
                __result = ForcePlayerWin ? t1.BattleTeamIndexSelf : t0.BattleTeamIndexSelf;
            }
            catch { }
        }

        // ② 禁止敌方 AI 出牌：AI 回合判定恒“已结束”，跳过整个出牌循环
        private static void AITurnEndPostfix(BattleTeamAgentScriptAI __instance, ref bool __result)
        {
            try
            {
                if (!ForbidAIUseCard) return;
                var team = __instance.LogicAgent?.Obj;
                if (team != null && team.BattleTeamIndexSelf != 0) __result = true;
            }
            catch { }
        }
    }
}
