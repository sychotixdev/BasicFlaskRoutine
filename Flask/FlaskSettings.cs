using PoeHUD.Hud.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeRoutine.Routine.BasicFlaskRoutine.Flask
{
    public class FlaskSettings
    {
        public FlaskSettings (ToggleNode enabled, HotkeyNode hotkey)
        {
            Enabled = enabled;
            Hotkey = hotkey;
        }

        public HotkeyNode Hotkey { get; }
        public ToggleNode Enabled { get; }

    }
}
