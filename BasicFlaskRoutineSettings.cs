using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;
using System.Windows.Forms;
using TreeRoutine.Routine.BasicFlaskRoutine.Flask;

namespace TreeRoutine.Routine.BasicFlaskRoutine
{
    public class BasicFlaskRoutineSettings : BaseTreeSettings
    {
        public RangeNode<int> RunFPS { get; set; } = new RangeNode<int>(15, 1, 60);

        public ToggleNode EnableInHideout { get; set; } = false;

        public ToggleNode AutoFlask { get; set; } = false;

        public RangeNode<int> HPPotion { get; set; } = new RangeNode<int>(65, 0, 100);
        public RangeNode<int> InstantHPPotion { get; set; } = new RangeNode<int>(35, 0, 100);
        public ToggleNode DisableLifeSecUse { get; set; } = false;

        public RangeNode<int> ManaPotion { get; set; } = new RangeNode<int>(65, 0, 100);
        public RangeNode<int> InstantManaPotion { get; set; } = new RangeNode<int>(35, 0, 100);
        public RangeNode<int> MinManaFlask { get; set; } = new RangeNode<int>(50, 0, 100);


        public ToggleNode RemAilment { get; set; } = false;
        public ToggleNode RemFrozen { get; set; } = false;
        public ToggleNode RemBurning { get; set; } = false;
        public ToggleNode RemShocked { get; set; } = false;
        public ToggleNode RemCurse { get; set; } = false;
        public ToggleNode RemPoison { get; set; } = false;
        public ToggleNode RemBleed { get; set; } = false;
        public RangeNode<int> CorruptCount { get; set; } = new RangeNode<int>(10, 0, 20);


        public ToggleNode SpeedFlaskEnable { get; set; } = false;
        public ToggleNode QuicksilverFlaskEnable { get; set; } = false;
        public ToggleNode SilverFlaskEnable { get; set; } = false;

        public ToggleNode DefensiveFlaskEnable { get; set; } = false;
        public RangeNode<int> HPPercentDefensive { get; set; } = new RangeNode<int>(50, 0, 100);
        public RangeNode<int> ESPercentDefensive { get; set; } = new RangeNode<int>(50, 0, 100);
        public ToggleNode OffensiveAsDefensiveEnable { get; set; } = false;

        public ToggleNode OffensiveFlaskEnable { get; set; } = false;
        public RangeNode<int> HPPercentOffensive { get; set; } = new RangeNode<int>(50, 0, 100);
        public RangeNode<int> ESPercentOffensive { get; set; } = new RangeNode<int>(50, 0, 100);

        public FlaskSetting[] FlaskSettings { get; set; } = new FlaskSetting[5]
        {
            new FlaskSetting(new ToggleNode(false), new HotkeyNode(Keys.D1), new RangeNode<int>(0, 0, 5)),
            new FlaskSetting(new ToggleNode(false), new HotkeyNode(Keys.D2), new RangeNode<int>(0, 0, 5)),
            new FlaskSetting(new ToggleNode(false), new HotkeyNode(Keys.D3), new RangeNode<int>(0, 0, 5)),
            new FlaskSetting(new ToggleNode(false), new HotkeyNode(Keys.D4), new RangeNode<int>(0, 0, 5)),
            new FlaskSetting(new ToggleNode(false), new HotkeyNode(Keys.D5), new RangeNode<int>(0, 0, 5))
        };

        
        #region UI Settings Menu
        public EmptyNode UiSesettingsHolder { get; set; }
        public ToggleNode FlaskUiEnable { get; set; } = false;
        public RangeNode<float> FlaskPositionX { get; set; } = new RangeNode<float>(28.0f, 0.0f, 100.0f);
        public RangeNode<float> FlaskPositionY { get; set; } = new RangeNode<float>(83.0f, 0.0f, 100.0f);
        public RangeNode<int> FlaskTextSize { get; set; } = new RangeNode<int>(15, 15, 60);

        public ToggleNode BuffUiEnable { get; set; } = false;
        public RangeNode<float> BuffPositionX { get; set; } = new RangeNode<float>(0.0f, 0.0f, 100.0f);
        public RangeNode<float> BuffPositionY { get; set; } = new RangeNode<float>(10.0f, 0.0f, 100.0f);
        public RangeNode<int> BuffTextSize { get; set; } = new RangeNode<int>(15, 15, 60);
        public ToggleNode EnableFlaskAuraBuff { get; set; } = false;
        #endregion
    }
}