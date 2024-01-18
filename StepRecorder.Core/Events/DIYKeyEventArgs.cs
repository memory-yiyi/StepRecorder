using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace StepRecorder.Core.Events
{
    internal partial class DIYKeyEventArgs : DIYInputEventArgs
    {
        public override ReadOnlyCollection<string> Keys { get; }

        public DIYKeyEventArgs(Key key, List<string>? comboKeys = null)
        {
            List<string> keys;
            string lastKey = key.ToString();
            if (key < Key.A || key > Key.Z)
            {
                lastKey = RenameNum().Replace(lastKey, "$2");
                switch (lastKey)
                {
                    case "Escape": lastKey = "Esc"; break;
                    case "Return": lastKey = "Enter"; break;
                    case "Delete": lastKey = "Del"; break;
                    case "Oem3": lastKey = "`"; break;
                    case "OemQuestion": lastKey = "/"; break;
                    case "OemPeriod": lastKey = "."; break;
                    case "OemComma": lastKey = ","; break;
                    case "OemPlus": lastKey = "="; break;
                    case "OemMinus": lastKey = "-"; break;
                    case "PageUp": lastKey = "PgUp"; break;
                    case "Next": lastKey = "PgDown"; break;
                    case "Oem5": lastKey = "\\"; break;
                    case "OemOpenBrackets": lastKey = "["; break;
                    case "Oem6": lastKey = "]"; break;
                    case "Oem1": lastKey = ";"; break;
                    case "OemQuotes": lastKey = "'"; break;
                }
            }
            if (comboKeys != null)
            {
                keys = ["", "", "", "", ""];
                foreach (var cKey in comboKeys)
                {
                    switch (cKey[1..])
                    {
                        case "Win":
                            keys[0] = cKey;
                            break;
                        case "Ctrl":
                            keys[1] = cKey;
                            break;
                        case "Alt":
                            keys[2] = cKey;
                            break;
                        case "Shift":
                            keys[3] = cKey;
                            break;
                    }
                }
                keys[4] = lastKey;
                keys.RemoveAll(k => k.Equals(""));
            }
            else
                keys = [lastKey];
            Keys = new ReadOnlyCollection<string>(keys);
        }

        [GeneratedRegex(@"(D|NumPad)(\d)")]
        private static partial Regex RenameNum();
    }
}
