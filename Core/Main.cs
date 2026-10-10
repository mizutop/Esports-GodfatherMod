using MelonLoader;
using UnityEngine;
using EsportsTrainer.UI;
using HarmonyLib;

[assembly: MelonInfo(typeof(EsportsTrainer.Core.Main), "Esports Godfather", "3.4.0", "Mizuof")]
[assembly: MelonGame("Asteroid Studio", "Esport Godfather")]

namespace EsportsTrainer.Core
{
    /// <summary>
    /// Core/Main.cs — L1 入口：横幅 / 配置 / 补丁安装 / 快捷键 / 每帧驱动 / 光标管理 / OnGUI 渲染。
    /// </summary>
    public class Main : MelonMod
    {
        private CursorLockMode _prevLock = CursorLockMode.None;
        private bool _prevVisible = true;
        private static bool _legacyInputBroken;
        private static MelonLogger.Instance _log;

        public override void OnInitializeMelon()
        {
            _log = LoggerInstance;
            Log.Init(_log);
            LoggerInstance.Msg("  ███╗   ███╗██╗███████╗██╗   ██╗ ██████╗ ███████╗");
            LoggerInstance.Msg("  ████╗ ████║██║╚══███╔╝██║   ██║██╔═══██╗██╔════╝");
            LoggerInstance.Msg("  ██╔████╔██║██║  ███╔╝ ██║   ██║██║   ██║█████╗  ");
            LoggerInstance.Msg("  ██║╚██╔╝██║██║ ███╔╝  ██║   ██║██║   ██║██╔══╝  ");
            LoggerInstance.Msg("  ██║ ╚═╝ ██║██║███████╗╚██████╔╝╚██████╔╝██║     ");
            LoggerInstance.Msg("  ╚═╝     ╚═╝╚═╝╚══════╝ ╚═════╝  ╚═════╝ ╚═╝     ");
            _log.Msg("Trainer for 电竞教父 (Esports Godfather) — Author: Mizuof");
            Log.Info("Core", "版本 v3.4.0 · F1 面板 · F2 数据快照");

            // ---- 配置 ----
            Config.Load();
            Log.Ok("Core", $"配置装载: 面板键 {Config.PanelKey} / 快照键 {Config.SnapKey} / 默认倍速 x{Config.DefaultFightSpeed}");

            // ---- 补丁 ----
            try
            {
                var harmony = new HarmonyLib.Harmony("com.mizuof.esportstrainer");
                BattlePatches.Install(harmony);
                TrainingPatches.Install(harmony);
                UnitOverridePatches.Install(harmony);
            }
            catch (System.Exception ex)
            {
                Log.Exception("Patch", "补丁安装", ex);
            }
        }

        public override void OnUpdate()
        {
            // ---- 快捷键（双输入兼容，6.9）----
            if (KeyPressed(Config.PanelKey)) TogglePanel();
            if (KeyPressed(Config.SnapKey)) Refs.DumpSnapshot(_log);

            // ---- 每帧功能驱动 ----
            Features.BattleTab.OnFrame();

            // ---- 光标管理（6.7 硬性）：面板打开期间逐帧解锁，关闭恢复 ----
            if (ModMenu.IsOpen)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            // ---- 缓存节流刷新 + 游戏就绪检测 ----
            if (Time.frameCount % 60 == 0)
            {
                bool wasReady = Refs.HasClub;
                Refs.Refresh();
                if (!wasReady && Refs.HasClub)
                    Log.Ok("Data", $"游戏实例已连接: {Refs.DateText()} · 选手 {Refs.C.Athletes.MaybeCount()} 人");
                else if (wasReady && !Refs.HasClub)
                    Log.Info("Data", "游戏实例已断开（返回主菜单）");
            }
        }

        public override void OnGUI()
        {
            if (!ModMenu.IsOpen) return;
            ModMenu.Render();
        }

        private void TogglePanel()
        {
            ModMenu.Toggle();
            if (ModMenu.IsOpen)
            {
                _prevLock = Cursor.lockState;
                _prevVisible = Cursor.visible;
                Log.Info("Core", "面板已开启");
            }
            else
            {
                Cursor.lockState = _prevLock;
                Cursor.visible = _prevVisible;
                Log.Info("Core", "面板已关闭");
            }
        }

        // ---- 双输入兼容（6.9）：legacy 优先，异常后缓存降级 ----
        private static bool KeyPressed(KeyCode key)
        {
            if (!_legacyInputBroken)
            {
                try { return Input.GetKeyDown(key); }
                catch (System.InvalidOperationException)
                {
                    _legacyInputBroken = true;
                    Log.Warn("Core", "Legacy Input 不可用，已降级 Input System");
                }
            }
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb == null) return false;
                return key switch
                {
                    KeyCode.F1 => kb.f1Key.wasPressedThisFrame,
                    KeyCode.F2 => kb.f2Key.wasPressedThisFrame,
                    KeyCode.F3 => kb.f3Key.wasPressedThisFrame,
                    KeyCode.F4 => kb.f4Key.wasPressedThisFrame,
                    _ => false
                };
            }
            catch { return false; }
        }
    }

    internal static class AthletesExt
    {
        public static int MaybeCount(this System.Collections.Generic.IEnumerable<GameMain.AthleteSystem.Athlete> src)
        {
            try { int n = 0; foreach (var _ in src) n++; return n; } catch { return 0; }
        }
    }
}
