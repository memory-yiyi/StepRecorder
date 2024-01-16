using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace StepRecorder.Core.Events
{
    internal partial class DIYKeyEventArgs : EventArgs
    {
        public bool Handled { get; set; } = false;
        public ReadOnlyCollection<string> Keys { get; }

        public DIYKeyEventArgs(Key key, List<string>? comboKeys)
        {
            List<string> keys;
            string lastKey = RenameNum().Replace(key.ToString(), "$2").Replace("Escape", "Esc");
            if (comboKeys != null)
            {
                keys = ["", "", "", "", ""];
                foreach(var cKey in comboKeys)
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
        public DIYKeyEventArgs(Key key) : this(key, null) { }

        [GeneratedRegex(@"(D|NumPad)(\d)")]
        private static partial Regex RenameNum();
    }
}
