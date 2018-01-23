using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;
using System.Windows.Forms;

namespace TreeRoutine.Routine.BasicFlaskRoutine
{
    public class BasicFlaskRoutineSettings : BaseTreeSettings
    {
        [Menu("HP/MANA % Auto Flask", 10)]
        public ToggleNode AutoFlask { get; set; } = false;

        [Menu("Min Life % Auto HP Flask", 11, 10)]
        public RangeNode<int> HPPotion { get; set; }  = new RangeNode<int>(65, 0, 100);

        [Menu("Min Life % Auto Instant HP Flask", 12, 10)]
        public RangeNode<int> InstantHPPotion { get; set; } = new RangeNode<int>(35, 0, 100);

        [Menu("Min Mana % Auto Mana Flask", 15, 10)]
        public RangeNode<int> ManaPotion { get; set; } = new RangeNode<int>(65, 0, 100);

        [Menu("Min Mana % Auto Instant MP Flask", 16, 10)]
        public RangeNode<int> InstantManaPotion { get; set; } = new RangeNode<int>(35, 0, 100);

        [Menu("Min Mana Auto Mana Flask", 17, 10)]
        public RangeNode<int> MinManaFlask { get; set; } = new RangeNode<int>(50, 0, 100);

        [Menu("Disable Life/Hybrid Flask Offensive/Defensive Usage", 19, 10)]
        public ToggleNode DisableLifeSecUse { get; set; } = false;

        [Menu("Remove Ailment Flask", 20)]
        public ToggleNode RemAilment { get; set; } = false;

        [Menu("Remove Frozen", 21, 20)]
        public ToggleNode RemFrozen { get; set; } = false;

        [Menu("Remove Burning", 22, 20)]
        public ToggleNode RemBurning { get; set; } = false;

        [Menu("Remove Shocked", 23, 20)]
        public ToggleNode RemShocked { get; set; } = false;

        [Menu("Remove Curse", 24, 20)]
        public ToggleNode RemCurse { get; set; } = false;

        [Menu("Remove Poison", 25, 20)]
        public ToggleNode RemPoison { get; set; } = false;

        [Menu("Remove Corrupting/Bleed", 26, 20)]
        public ToggleNode RemBleed { get; set; } = false;

        [Menu("Corrupting Blood Stacks", 27, 20)]
        public RangeNode<int> CorruptCount { get; set; } = new RangeNode<int>(10, 0, 20);


        [Menu("Speed Flask", 30)]
        public ToggleNode SpeedFlaskEnable { get; set; } = false;

        [Menu("QuickSilver Flask", 31, 30)]
        public ToggleNode QuicksilverFlaskEnable { get; set; } = false;

        [Menu("Silver Flask", 32, 30)]
        public ToggleNode SilverFlaskEnable { get; set; } = false;

        [Menu("Defensive Flask", 40)]
        public ToggleNode DefensiveFlaskEnable { get; set; } = false;
        [Menu("Min Life %", 41, 40)]
        public RangeNode<int> HPPercentDefensive { get; set; } = new RangeNode<int>(50, 0, 100);

        [Menu("Min ES %", 42, 40)]
        public RangeNode<int> ESPercentDefensive { get; set; } = new RangeNode<int>(50, 0, 100);

        [Menu("Treat Offensive Flasks As Defensive", 49, 40)]
        public ToggleNode OffensiveAsDefensiveEnable { get; set; } = false;

        [Menu("Offensive Flask", 50)]
        public ToggleNode OffensiveFlaskEnable { get; set; } = false;

        [Menu("Min Life %", 51, 50)]
        public RangeNode<int> HPPercentOffensive { get; set; } = new RangeNode<int>(50, 0, 100);

        [Menu("Min ES %", 52, 50)]
        public RangeNode<int> ESPercentOffensive { get; set; } = new RangeNode<int>(50, 0, 100);

        [Menu("Enable In Hideout", 60)]
        public ToggleNode EnableInHideout { get; set; } = false;

        [Menu("Flask Settings", 120)]
        public EmptyNode FlasksettingsHolder { get; set; }

        [Menu("Flask Slot 1", 130, 120)]
        public ToggleNode FlaskSlot1Enable { get; set; } = false;
        [Menu("Hotkey", 131, 130)]
        public HotkeyNode FlaskSlot1Hotkey { get; set; } = new HotkeyNode(Keys.D1);
        [Menu("Reserve Uses", 132, 130)]
        public RangeNode<int> FlaskSlot1ReserveUses { get; set; } = new RangeNode<int>(0, 0, 5);

        [Menu("Flask Slot 2", 140, 120)]
        public ToggleNode FlaskSlot2Enable { get; set; } = false;
        [Menu("Hotkey", 141, 140)]
        public HotkeyNode FlaskSlot2Hotkey { get; set; } = new HotkeyNode(Keys.D2);
        [Menu("Reserve Uses", 142, 140)]
        public RangeNode<int> FlaskSlot2ReserveUses { get; set; } = new RangeNode<int>(0, 0, 5);

        [Menu("Flask Slot 3", 150, 120)]
        public ToggleNode FlaskSlot3Enable { get; set; } = false;
        [Menu("Hotkey", 151, 150)]
        public HotkeyNode FlaskSlot3Hotkey { get; set; } = new HotkeyNode(Keys.D3);
        [Menu("Reserve Uses", 152, 150)]
        public RangeNode<int> FlaskSlot3ReserveUses { get; set; } = new RangeNode<int>(0, 0, 5);

        [Menu("Flask Slot 4", 160, 120)]
        public ToggleNode FlaskSlot4Enable { get; set; } = false;
        [Menu("Hotkey", 161, 160)]
        public HotkeyNode FlaskSlot4Hotkey { get; set; } = new HotkeyNode(Keys.D4);
        [Menu("Reserve Uses", 162, 160)]
        public RangeNode<int> FlaskSlot4ReserveUses { get; set; } = new RangeNode<int>(0, 0, 5);

        [Menu("Flask Slot 5", 170, 120)]
        public ToggleNode FlaskSlot5Enable { get; set; } = false;
        [Menu("Hotkey", 171, 170)]
        public HotkeyNode FlaskSlot5Hotkey { get; set; } = new HotkeyNode(Keys.D5);
        [Menu("Reserve Uses", 172, 170)]
        public RangeNode<int> FlaskSlot5ReserveUses { get; set; } = new RangeNode<int>(0, 0, 5);

        #region Settings Menu
        [Menu("UI Settings", 180)]
        public EmptyNode UiSesettingsHolder { get; set; }
        [Menu("Flask Slot UI", 190, 180)]
        public ToggleNode FlaskUiEnable { get; set; } = false;
        [Menu("Position: X", 191, 190)]
        public RangeNode<float> FlaskPositionX { get; set; } = new RangeNode<float>(28.0f, 0.0f, 100.0f);
        [Menu("Position: Y", 192, 190)]
        public RangeNode<float> FlaskPositionY { get; set; } = new RangeNode<float>(83.0f, 0.0f, 100.0f);
        [Menu("Text Size", 193, 190)]
        public RangeNode<int> FlaskTextSize { get; set; } = new RangeNode<int>(15, 15, 60);

        [Menu("Buff Bar UI", 200, 180)]
        public ToggleNode BuffUiEnable { get; set; } = false;
        [Menu("Position: X", 201, 200)]
        public RangeNode<float> BuffPositionX { get; set; } = new RangeNode<float>(0.0f, 0.0f, 100.0f);
        [Menu("Position: Y", 202, 200)]
        public RangeNode<float> BuffPositionY { get; set; } = new RangeNode<float>(10.0f, 0.0f, 100.0f);
        [Menu("Text Size", 203, 200)]
        public RangeNode<int> BuffTextSize { get; set; } = new RangeNode<int>(15, 15, 60);
        [Menu("Enable Flask Or Aura Debuff/Buff", 204, 200)]
        public ToggleNode EnableFlaskAuraBuff { get; set; } = false;
        #endregion
    }
}