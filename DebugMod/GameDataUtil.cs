using UnityEngine;
using GameMain;
using GameMain.AthleteSystem;
using GameMain.BattleSystem;
using System.Text;
using System.Linq;

namespace DebugMod
{
    public partial class Main
    {
        void DumpAllGameData()
        {
            var sb = new StringBuilder();
            sb.AppendLine("===== \u7535\u7ade\u6559\u7236 \u6570\u636e\u5feb\u7167 (F2) =====");
            try
            {
                RefreshCache();
                if (_g == null) { sb.AppendLine("Game \u4e0d\u53ef\u7528"); OutputAndLog(sb); return; }

                sb.AppendLine("IsRunning=" + _g.IsRunning + " Loaded=" + _g.Loaded +
                    " CurState=" + (_g.CurState != null ? _g.CurState.ToString() : "null"));
                sb.AppendLine("Battle=" + (_g.Battle != null ? "\u6218\u6597\u4e2d" : "\u65e0") +
                    " Affair=" + (_g.CurAffair?.GetType().Name ?? "\u65e0"));
                sb.AppendLine("\u65f6\u95f4=" + (_g.Calendar != null ? _g.Calendar.Date.ToString() : "?"));

                if (_c != null)
                {
                    sb.AppendLine(string.Format("Club: cp={0} tp={1} budget={2} fans={3}",
                        _cd.GetCoachPoint(), _cd.GetTrainPoint(), _cd.GetBudget(), _c.Fans));
                    sb.AppendLine("\u9009\u624b: " + _c.Athletes.Count() + "\u4eba");
                    foreach (var a in _c.Athletes.Take(5))
                    {
                        if (a == null) continue;
                        sb.AppendLine(string.Format(
                            "  {0,-10} Ag={1,3} De={2,3} Fa={3,3} Re={4,3} Ex={5,3} \u4e0a\u9650={6}",
                            a.HeroRole,
                            a.C_Data.GetAbilityLevel(AthleteAbilityFlags.Aggressive, false),
                            a.C_Data.GetAbilityLevel(AthleteAbilityFlags.Defence, false),
                            a.C_Data.GetAbilityLevel(AthleteAbilityFlags.Farming, false),
                            a.C_Data.GetAbilityLevel(AthleteAbilityFlags.Reflection, false),
                            a.C_Data.GetAbilityLevel(AthleteAbilityFlags.Exercise, false),
                            a.C_Data.GetAbilityLimit()));
                    }
                }

                if (_b != null)
                {
                    sb.AppendLine(string.Format("\u6218\u6597\u4e2d: Round={0} Speed={1}x" +
                        (_forceWinActive ? " ForceWin={2}" : ""),
                        _b.Rounds, _b.C_Data.GetFightSpeed(),
                        _forceWinActive ? (_forceWinPlayer ? "\u83b7\u80dc" : "\u5931\u8d25") : ""));
                }
            }
            catch (System.Exception ex) { sb.AppendLine("\u5f02\u5e38: " + ex.Message); }

            sb.AppendLine("===== \u5feb\u7167\u7ed3\u675f =====");
            OutputAndLog(sb);
        }

        void OutputAndLog(StringBuilder sb)
        {
            foreach (var line in sb.ToString().Split('\n'))
                LoggerInstance.Msg(line.TrimEnd('\r'));
        }
    }
}
