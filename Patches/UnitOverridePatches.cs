using System;
using System.Collections.Generic;
using System.Reflection;
using GameMain.UnitSystem;
using HarmonyLib;
using LogicFramework;

namespace EsportsTrainer.Core
{
    /// <summary>
    /// Patches/UnitOverridePatches.cs — 战斗单位属性覆写（Harmony postfix + 覆盖字典）。
    /// 攻击力/护甲/暴击率/存在感 均为效应器计算属性（无直接 setter），
    /// 通过 postfix 拦截 GetProperty_* 返回值实现运行时修改。
    /// </summary>
    public static class UnitOverrides
    {
        public static readonly Dictionary<LogicEntity_Unit, float> Attack = new();
        public static readonly Dictionary<LogicEntity_Unit, float> Armor = new();
        public static readonly Dictionary<LogicEntity_Unit, float> Crit = new();
        public static readonly Dictionary<LogicEntity_Unit, double> Presence = new();

        public static void Set(Unit u, string kind, float v)
        {
            try
            {
                var e = u?.Entity;
                if (e == null) return;
                switch (kind)
                {
                    case "attack": Attack[e] = v; break;
                    case "armor": Armor[e] = v; break;
                    case "crit": Crit[e] = v; break;
                    case "presence": Presence[e] = v; break;
                }
            }
            catch { }
        }

        public static float? Get(Unit u, string kind)
        {
            try
            {
                var e = u?.Entity;
                if (e == null) return null;
                return kind switch
                {
                    "attack" => Attack.TryGetValue(e, out var a) ? a : (float?)null,
                    "armor" => Armor.TryGetValue(e, out var b) ? b : (float?)null,
                    "crit" => Crit.TryGetValue(e, out var c) ? c : (float?)null,
                    "presence" => Presence.TryGetValue(e, out var p) ? (float)p : (float?)null,
                    _ => null
                };
            }
            catch { return null; }
        }

        public static void ClearAll()
        {
            Attack.Clear(); Armor.Clear(); Crit.Clear(); Presence.Clear();
        }
    }

    public static class UnitOverridePatches
    {
        public static void Install(HarmonyLib.Harmony harmony)
        {
            TryPatch(harmony, "Unit.Attack 覆写", AccessTools.Method(typeof(LogicEntity_Unit), nameof(LogicEntity_Unit.GetProperty_Attack)),
                new HarmonyMethod(typeof(UnitOverridePatches), nameof(AttackPostfix)));
            TryPatch(harmony, "Unit.Armor 覆写", AccessTools.Method(typeof(LogicEntity_Unit), nameof(LogicEntity_Unit.GetProperty_Armor)),
                new HarmonyMethod(typeof(UnitOverridePatches), nameof(ArmorPostfix)));
            TryPatch(harmony, "Unit.CritChance 覆写", AccessTools.Method(typeof(LogicEntity_Unit), nameof(LogicEntity_Unit.GetProperty_CriticalChance)),
                new HarmonyMethod(typeof(UnitOverridePatches), nameof(CritPostfix)));
            TryPatch(harmony, "Unit.FightPresence 覆写", AccessTools.Method(typeof(LogicEntity_Unit), nameof(LogicEntity_Unit.GetProperty_FightPresence)),
                new HarmonyMethod(typeof(UnitOverridePatches), nameof(PresencePostfix)));
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

        // 覆盖表为空时零开销直返（攻击/护甲等属性每帧高频查询）
        private static void AttackPostfix(LogicEntity_Unit __instance, ref float __result)
        { if (UnitOverrides.Attack.Count > 0 && UnitOverrides.Attack.TryGetValue(__instance, out var v)) __result = v; }

        private static void ArmorPostfix(LogicEntity_Unit __instance, ref float __result)
        { if (UnitOverrides.Armor.Count > 0 && UnitOverrides.Armor.TryGetValue(__instance, out var v)) __result = v; }

        private static void CritPostfix(LogicEntity_Unit __instance, ref float __result)
        { if (UnitOverrides.Crit.Count > 0 && UnitOverrides.Crit.TryGetValue(__instance, out var v)) __result = v; }

        private static void PresencePostfix(LogicEntity_Unit __instance, ref double __result)
        { if (UnitOverrides.Presence.Count > 0 && UnitOverrides.Presence.TryGetValue(__instance, out var v)) __result = v; }
    }
}
