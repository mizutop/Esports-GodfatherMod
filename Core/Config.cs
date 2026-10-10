using MelonLoader;
using UnityEngine;

namespace EsportsTrainer.Core
{
    /// <summary>
    /// ConfigKeys + MelonPreferences 集中配置（SOP 5.5 / 7.5）。
    /// 配色：预设索引 + 三路 hex 自定义覆盖（空 = 用预设值）。
    /// </summary>
    public static class Config
    {
        // ---- 集中键名 ----
        public const string PrefPanelKey = "PanelKey";
        public const string PrefSnapKey = "SnapshotKey";
        public const string PrefDefaultSpeed = "DefaultFightSpeed";
        public const string PrefScheme = "ColorScheme";
        public const string PrefCustomPrimary = "CustomPrimary";
        public const string PrefCustomSecondary = "CustomSecondary";
        public const string PrefCustomGold = "CustomGold";
        public const string PrefLang = "UiLang";

        // ---- 运行时键值 ----
        public static KeyCode PanelKey = KeyCode.F1;
        public static KeyCode SnapKey = KeyCode.F2;
        public static int DefaultFightSpeed = 3;
        public static int SchemeIndex;
        public static string CustomPrimary = "";
        public static string CustomSecondary = "";
        public static string CustomGold = "";
        public static string Lang = "zh";

        private static MelonPreferences_Category _cat;
        private static MelonPreferences_Entry<int> _ePanel;
        private static MelonPreferences_Entry<int> _eSnap;
        private static MelonPreferences_Entry<int> _eSpeed;
        private static MelonPreferences_Entry<int> _eScheme;
        private static MelonPreferences_Entry<string> _eCP, _eCS, _eCG;
        private static MelonPreferences_Entry<string> _eLang;

        public static void Load()
        {
            try
            {
                _cat = MelonPreferences.CreateCategory("EsportsTrainer", "Esports Godfather Trainer");
                _ePanel = _cat.CreateEntry(PrefPanelKey, (int)KeyCode.F1, "面板开关键(KeyCode)");
                _eSnap = _cat.CreateEntry(PrefSnapKey, (int)KeyCode.F2, "数据快照键(KeyCode)");
                _eSpeed = _cat.CreateEntry(PrefDefaultSpeed, 3, "战斗默认倍速");
                _eScheme = _cat.CreateEntry(PrefScheme, 0, "配色方案索引 0紫电/1赤焰/2苍翠");
                _eCP = _cat.CreateEntry(PrefCustomPrimary, "", "自定义主色(hex，留空用预设)");
                _eCS = _cat.CreateEntry(PrefCustomSecondary, "", "自定义辅色(hex，留空用预设)");
                _eCG = _cat.CreateEntry(PrefCustomGold, "", "自定义金色(hex，留空用预设)");
                _eLang = _cat.CreateEntry(PrefLang, "zh", "界面语言 zh/en");

                PanelKey = ToKey(_ePanel.Value, KeyCode.F1);
                SnapKey = ToKey(_eSnap.Value, KeyCode.F2);
                DefaultFightSpeed = _eSpeed.Value <= 0 ? 1 : _eSpeed.Value;
                SchemeIndex = _eScheme.Value;
                CustomPrimary = _eCP.Value ?? "";
                CustomSecondary = _eCS.Value ?? "";
                CustomGold = _eCG.Value ?? "";
                Lang = _eLang.Value == "en" ? "en" : "zh";
                L10n.Lang = Lang;

                Theme.Apply(SchemeIndex, CustomPrimary, CustomSecondary, CustomGold);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Msg($"[Config] 配置装载失败，使用默认值: {ex.Message}");
                Theme.Apply(0, "", "", "");
            }
        }

        public static void SetScheme(int i)
        {
            SchemeIndex = i;
            CustomPrimary = CustomSecondary = CustomGold = "";
            try
            {
                _eScheme.Value = i;
                _eCP.Value = "";
                _eCS.Value = "";
                _eCG.Value = "";
            }
            catch { }
            Theme.SetScheme(i);
        }

        public static void SetCustomColor(string which, string hex)
        {
            if (which == "primary") { CustomPrimary = hex; try { _eCP.Value = hex; } catch { } }
            else if (which == "secondary") { CustomSecondary = hex; try { _eCS.Value = hex; } catch { } }
            else if (which == "gold") { CustomGold = hex; try { _eCG.Value = hex; } catch { } }
            Theme.SetCustom(which, hex);
        }

        public static void SetLang(string lang)
        {
            Lang = lang == "en" ? "en" : "zh";
            L10n.Lang = Lang;
            try { _eLang.Value = Lang; } catch { }
        }

        public static void ToggleLang() => SetLang(Lang == "zh" ? "en" : "zh");

        private static KeyCode ToKey(int v, KeyCode fallback)
            => System.Enum.IsDefined(typeof(KeyCode), v) ? (KeyCode)v : fallback;
    }
}
