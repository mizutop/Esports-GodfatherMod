using System;
using System.Reflection;
using GameMain.ClubSystem;
using HarmonyLib;

namespace EsportsTrainer.Core
{
    /// <summary>
    /// Patches/TrainingPatches.cs — 训练类 Harmony 补丁。
    /// ① TrainingTable.GetResult postfix：日常训练结果强制 大成功 (VeryGood)。
    /// ② Club.DataComponent.GetAdvancedTrainingCountBetterSuccess postfix：能力训练 betterSuccess 恒 true。
    /// </summary>
    public static class TrainingPatches
    {
        public static bool DailyAlwaysVeryGood;   // 日常训练必定大成功
        public static bool AbilityAlwaysSuccess;  // 能力训练必定成功(大成功)

        public static void Install(HarmonyLib.Harmony harmony)
        {
            TryPatch(harmony, "TrainingTable.GetResult → 日常训练大成功",
                AccessTools.Method(typeof(TrainingTable), nameof(TrainingTable.GetResult)),
                new HarmonyMethod(typeof(TrainingPatches), nameof(GetResultPostfix)));
            TryPatch(harmony, "GetAdvancedTrainingCountBetterSuccess → 能力训练必定大成功",
                AccessTools.Method(typeof(Club.DataComponent), nameof(Club.DataComponent.GetAdvancedTrainingCountBetterSuccess)),
                new HarmonyMethod(typeof(TrainingPatches), nameof(BetterSuccessPostfix)));
        }

        internal static void TryPatch(HarmonyLib.Harmony harmony, string name, MethodInfo original, HarmonyMethod postfix)
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

        // ① 日常训练：GetResult 掷点后统一改写为 大成功
        private static void GetResultPostfix(TrainingTable __instance)
        {
            try
            {
                if (!DailyAlwaysVeryGood || __instance?.groupDict == null) return;
                foreach (var kv in __instance.groupDict)
                {
                    if (kv.Key != null) kv.Key.resultType = TrainingResultTypes.VeryGood;
                    if (kv.Value != null) kv.Value.ResultType = TrainingResultTypes.VeryGood;
                }
            }
            catch { }
        }

        // ② 能力训练：betterSuccess 概率恒为 true
        private static void BetterSuccessPostfix(ref bool __result)
        {
            if (AbilityAlwaysSuccess) __result = true;
        }
    }
}
