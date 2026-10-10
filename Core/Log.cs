using System;
using MelonLoader;

namespace EsportsTrainer.Core
{
    /// <summary>
    /// Core/Log.cs — 统一分级日志。
    /// 格式：[Trainer][域] 内容 ✓/✗
    /// 域标签：Core / Patch / Data / Club / Athlete / Battle / Season / Snap
    /// </summary>
    public static class Log
    {
        private static MelonLogger.Instance _l;

        public static void Init(MelonLogger.Instance instance)
        {
            _l = instance;
            Rule('═');
        }

        public static void Info(string tag, string msg) => _l?.Msg($"[{tag}] {msg}");
        public static void Ok(string tag, string msg) => _l?.Msg($"[{tag}] {msg}  ✓");
        public static void Warn(string tag, string msg) => _l?.Warning($"[{tag}] {msg}");
        public static void Err(string tag, string msg) => _l?.Error($"[{tag}] {msg}");

        public static void Rule(char c = '─', int width = 64)
        {
            _l?.Msg(new string(c, width));
        }

        public static void Exception(string tag, string where, Exception ex)
            => _l?.Error($"[{tag}] {where}: {ex.GetType().Name} — {ex.Message}\n{ex.StackTrace}");

        /// <summary>写入验证行：目标 旧值 → 新值（回读 X ✓ / 回读失败 ✗）。
        /// 默认静默（降噪）；验证失败 / 静默期外异常仍会输出。设 Log.Verbose = true 恢复全量。</summary>
        public static bool Verbose = false;

        public static void Write(string tag, string target, string before, string after, string readback, bool verified)
        {
            if (!verified) { Warn(tag, $"{target}: {before} → {after} 未生效 ✗"); return; }
            if (!Verbose) return;   // 成功写入默认不打，减少日志刷屏
            var line = $"{target}: {before} → {after}";
            if (readback != null) line += $"（回读 {readback}）";
            _l?.Msg($"[{tag}] {line}  ✓");
        }
    }
}
