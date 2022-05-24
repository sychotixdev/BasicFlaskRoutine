using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExileCore.Shared.Nodes;

namespace TreeRoutine.Routine.BasicFlaskRoutine.Flask
{
    public class FlaskSetting
    {
        public FlaskSetting()
        {
            Enabled = new ToggleNode(true);
            UseWhenChargesFilled = new ToggleNode(false);
            Hotkey = new HotkeyNode();
            ReservedUses = new RangeNode<int>(0, 0, 5);
        }

        public FlaskSetting(ToggleNode enabled, ToggleNode useWhenChargesField, HotkeyNode hotkey, RangeNode<int> reservedUses)
        {
            Enabled = enabled;
            UseWhenChargesFilled = useWhenChargesField;
            Hotkey = hotkey;
            ReservedUses = reservedUses;
        }
        public FlaskSetting(Keys hotKey, bool enabled = true, bool useWhenChargesFiled = false, int reserveUssages = 0, int maxUssage = 5)
        {
            Enabled = new ToggleNode(enabled);
            UseWhenChargesFilled = new ToggleNode(useWhenChargesFiled);
            Hotkey = new HotkeyNode(hotKey);
            ReservedUses = new RangeNode<int>(reserveUssages, 0, maxUssage);
        }

        public ToggleNode Enabled { get; set; }
        public ToggleNode UseWhenChargesFilled { get; set; }
        public HotkeyNode Hotkey { get; set; }
        public RangeNode<int> ReservedUses { get; set; }

    }
}