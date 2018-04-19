using TreeRoutine.DefaultBehaviors.Actions;
using TreeRoutine.DefaultBehaviors.Helpers;
using TreeRoutine.FlaskComponents;
using TreeRoutine.Routine.BasicFlaskRoutine.Flask;
using PoeHUD.Poe.Components;
using System;
using System.Collections.Generic;
using TreeSharp;
using SharpDX;
using PoeHUD.Models.Enums;
using System.Linq;
using TreeRoutine.Menu;
using ImGuiNET;
using PoeHUD.Framework;
using PoeHUD.Framework.Helpers;
using System.Diagnostics;

namespace TreeRoutine.Routine.BasicFlaskRoutine
{
    public class BasicFlaskRoutine : BaseTreeRoutinePlugin<BasicFlaskRoutineSettings, BaseTreeCache>
    {
        public BasicFlaskRoutine() : base()
        {

        }

        public Composite Tree { get; set; }
        private Coroutine TreeCoroutine { get; set; }


        private KeyboardHelper KeyboardHelper { get; set; } = null;

        private Stopwatch PlayerMovingStopwatch { get; set; } = new Stopwatch();

        public override void Initialise()
        {
            base.Initialise();

            PluginName = "BasicFlaskRoutine";
            KeyboardHelper = new KeyboardHelper(GameController);

            Tree = createTree();

            // Add this as a coroutine for this plugin
            TreeCoroutine = (new Coroutine(() => TickTree(Tree)
            , new WaitTime(1000 / Settings.TicksPerSecond), nameof(BasicFlaskRoutine), "BasicFlaskRoutine Tree"))
                .AutoRestart(GameController.CoroutineRunner).Run();

            Settings.TicksPerSecond.OnValueChanged += UpdateCoroutineWaitRender;
        }

        private void UpdateCoroutineWaitRender()
        {
            TreeCoroutine.UpdateCondtion(new WaitTime(1000 / Settings.TicksPerSecond));
        }

        protected override void UpdateCache()
        {
            base.UpdateCache();

            UpdatePlayerMovingStopwatch();
        }

        private void UpdatePlayerMovingStopwatch()
        {
            var player = GameController.Player.GetComponent<Actor>();
            if (player != null && player.Address != 0 && player.isMoving)
            {
                if (!PlayerMovingStopwatch.IsRunning)
                    PlayerMovingStopwatch.Start();
            }
            else
            {
                PlayerMovingStopwatch.Reset();
            }
        }

        private Composite createTree()
        {
            return new Decorator(x => TreeHelper.canTick() && !PlayerHelper.isPlayerDead() && (!Cache.InHideout || Settings.EnableInHideout),
                    new PrioritySelector
                    (
                        new Decorator(x => Settings.AutoFlask,
                        new PrioritySelector(
                            createInstantHPPotionComposite(),
                            createHPPotionComposite(),
                            createInstantManaPotionComposite(),
                            createManaPotionComposite()
                            )
                        ),
                        createAilmentPotionComposite(),
                        createDefensivePotionComposite(),
                        createSpeedPotionComposite(),
                        createOffensivePotionComposite()
                    )
                );
        }

        private Composite createInstantHPPotionComposite()
        {
            return new Decorator((x => PlayerHelper.isHealthBelowPercentage(Settings.InstantHPPotion)),
                new PrioritySelector(
                    createUseFlaskAction(FlaskActions.Life, true),
                    createUseFlaskAction(FlaskActions.Hybrid, true)
                )
            );

        }

        private Composite createHPPotionComposite()
        {
            return new Decorator((x => PlayerHelper.isHealthBelowPercentage(Settings.HPPotion)),
                new Decorator((x => PlayerHelper.playerDoesNotHaveAnyOfBuffs(new List<string>() { "flask_effect_life" })),
                 new PrioritySelector(
                    createUseFlaskAction(FlaskActions.Life, false),
                    createUseFlaskAction(FlaskActions.Hybrid, false)
                    )
                )
            );
        }

        private Composite createInstantManaPotionComposite()
        {
            return new Decorator((x => PlayerHelper.isManaBelowPercentage(Settings.InstantManaPotion)),
                new PrioritySelector(
                    createUseFlaskAction(FlaskActions.Mana, true),
                    createUseFlaskAction(FlaskActions.Hybrid, true)
                )
            );
        }

        private Composite createManaPotionComposite()
        {
            return new Decorator((x => PlayerHelper.isManaBelowPercentage(Settings.ManaPotion) || PlayerHelper.isManaBelowValue(Settings.MinManaFlask)),
                new Decorator((x => PlayerHelper.playerDoesNotHaveAnyOfBuffs(new List<string>() { "flask_effect_mana" })),
                    new PrioritySelector(
                        createUseFlaskAction(FlaskActions.Mana, false),
                        createUseFlaskAction(FlaskActions.Hybrid, false)
                    )
                )
            );
        }

        private Composite createSpeedPotionComposite()
        {
            return new Decorator((x => Settings.SpeedFlaskEnable && Settings.MinMsPlayerMoving <= PlayerMovingStopwatch.ElapsedMilliseconds && (PlayerHelper.playerDoesNotHaveAnyOfBuffs(new List<string>() { "flask_bonus_movement_speed", "flask_utility_sprint", "flask_utility_haste" }))),
                new PrioritySelector(
                    new Decorator((x => Settings.QuicksilverFlaskEnable), createUseFlaskAction(FlaskActions.Speedrun)),
                    new Decorator((x => Settings.SilverFlaskEnable), createUseFlaskAction(FlaskActions.OFFENSE_AND_SPEEDRUN))
                )
            );
        }

        private Composite createDefensivePotionComposite()
        {
            return new Decorator((x => Settings.DefensiveFlaskEnable &&
            (PlayerHelper.isHealthBelowPercentage(Settings.HPPercentDefensive) ||
            PlayerHelper.isEnergyShieldBelowPercentage(Settings.ESPercentDefensive))),
                new PrioritySelector(
                    createUseFlaskAction(FlaskActions.Defense),
                    new Decorator((x => Settings.OffensiveAsDefensiveEnable),
                    createUseFlaskAction(new List<FlaskActions> { FlaskActions.OFFENSE_AND_SPEEDRUN, FlaskActions.Defense },
                    ignoreFlasksWithAction: (() => Settings.DisableLifeSecUse ? new List<FlaskActions>() { FlaskActions.Life, FlaskActions.Mana, FlaskActions.Hybrid } : null)))
                )
            );
        }

        private Composite createOffensivePotionComposite()
        {
            return new PrioritySelector(
                new Decorator((x => Settings.OffensiveFlaskEnable &&
                (PlayerHelper.isHealthBelowPercentage(Settings.HPPercentOffensive) ||
                PlayerHelper.isEnergyShieldBelowPercentage(Settings.ESPercentOffensive))),
                    createUseFlaskAction(new List<FlaskActions> { FlaskActions.Offense, FlaskActions.OFFENSE_AND_SPEEDRUN }, ignoreFlasksWithAction: (() => Settings.DisableLifeSecUse ? new List<FlaskActions>() { FlaskActions.Life, FlaskActions.Mana, FlaskActions.Hybrid } : null)))
            );
        }

        private Composite createAilmentPotionComposite()
        {
            return new Decorator(x => Settings.RemAilment,
                new PrioritySelector(
                    new Decorator(x => Settings.RemBleed, createCurableDebuffDecorator(Cache.DebuffPanelConfig.Bleeding, createUseFlaskAction(FlaskActions.BleedImmune))),
                    new Decorator(x => Settings.RemBurning, createCurableDebuffDecorator(Cache.DebuffPanelConfig.Burning, createUseFlaskAction(FlaskActions.IgniteImmune))),
                    createCurableDebuffDecorator(Cache.DebuffPanelConfig.Corruption, createUseFlaskAction(FlaskActions.BleedImmune), (() => Settings.CorruptCount)),
                    new Decorator(x => Settings.RemFrozen, createCurableDebuffDecorator(Cache.DebuffPanelConfig.Frozen, createUseFlaskAction(FlaskActions.FreezeImmune))),
                    new Decorator(x => Settings.RemPoison, createCurableDebuffDecorator(Cache.DebuffPanelConfig.Poisoned, createUseFlaskAction(FlaskActions.PoisonImmune))),
                    new Decorator(x => Settings.RemShocked, createCurableDebuffDecorator(Cache.DebuffPanelConfig.Shocked, createUseFlaskAction(FlaskActions.ShockImmune))),
                    new Decorator(x => Settings.RemCurse, createCurableDebuffDecorator(Cache.DebuffPanelConfig.WeakenedSlowed, createUseFlaskAction(FlaskActions.CurseImmune)))
                    )
                );
        }

        private Composite createUseFlaskAction(FlaskActions flaskAction, Boolean? instant = null, Func<List<FlaskActions>> ignoreFlasksWithAction = null)
        {
            return createUseFlaskAction(new List<FlaskActions> { flaskAction }, instant, ignoreFlasksWithAction);
        }

        private Composite createUseFlaskAction(List<FlaskActions> flaskActions, Boolean? instant = null, Func<List<FlaskActions>> ignoreFlasksWithAction = null)
        {
            return new UseHotkeyAction(KeyboardHelper, x =>
            {
                var foundFlask = findFlaskMatchingAnyAction(flaskActions, instant, ignoreFlasksWithAction);

                if (foundFlask == null)
                {
                    return null;
                }

                return Settings.FlaskSettings[foundFlask.Index].Hotkey;
            });
        }

        private PlayerFlask findFlaskMatchingAnyAction(FlaskActions flaskAction, Boolean? instant = null, Func<List<FlaskActions>> ignoreFlasksWithAction = null)
        {
            return findFlaskMatchingAnyAction(new List<FlaskActions> { flaskAction }, instant, ignoreFlasksWithAction);
        }

        //It seems horribly inefficient that we're going through all this trouble to create a list and then only using FirstOrDefault() from it.
        private PlayerFlask findFlaskMatchingAnyAction(List<FlaskActions> flaskActions, Boolean? instant = null, Func<List<FlaskActions>> ignoreFlasksWithAction = null)
        {
            List<PlayerFlask> allFlasks = FlaskHelper.getAllFlaskInfo();
            if (!AreThereFlasksToUse(allFlasks))
                return null;
            LogFlasks(allFlasks);

            var flaskList = GetListOfFlasksToUse(ignoreFlasksWithAction, allFlasks, flaskActions, instant);
            if (flaskList == null || !flaskList.Any())
            {
                LogNoFlasks(flaskActions);
                return null;
            }
            LogFlaskToUse(flaskActions, flaskList);

            return flaskList.FirstOrDefault();
        }

        private List<PlayerFlask> GetListOfFlasksToUse(Func<List<FlaskActions>> ignoreFlasksWithAction, List<PlayerFlask> allFlasks, List<FlaskActions> flaskActions, bool? instant)
        {
            List<FlaskActions> ignoreFlaskActions = ignoreFlasksWithAction == null ? null : ignoreFlasksWithAction();

            List<PlayerFlask> enabledFlasks = allFlasks.Where(x => Settings.FlaskSettings[x.Index].Enabled).ToList();
            List<PlayerFlask> flasksWithValidActions = GetFlasksWithValidActions(enabledFlasks, flaskActions, ignoreFlaskActions);
            List<PlayerFlask> notInstantFlasks = GetFlasksThatArentInstant(flasksWithValidActions, instant);
            List<PlayerFlask> notInstantInactiveFlasks = GetInactiveBuffFlasks(notInstantFlasks);
            List<PlayerFlask> instantFlasks = GetInstantFlasks(flasksWithValidActions, instant);
            List<PlayerFlask> instantInactiveFlasks = GetInactiveBuffFlasks(instantFlasks);

            var flaskList = new List<PlayerFlask>();
            flaskList.AddRange(notInstantInactiveFlasks);
            flaskList.AddRange(instantInactiveFlasks);
            flaskList.OrderByDescending(x => x.TotalUses - Settings.FlaskSettings[x.Index].ReservedUses);
            return flaskList;
        }
        
        private bool AreThereFlasksToUse(List<PlayerFlask> allFlasks)
        {
            if (allFlasks == null || Settings.FlaskSettings == null)
            {
                if (Settings.Debug)
                {
                    if (allFlasks == null)
                        LogMessage(PluginName + ": No flasks to match against.", 5);
                    else if (Settings.FlaskSettings == null)
                        LogMessage(PluginName + ": Flask settings were null. Hopefully doesn't happen frequently.", 5);
                }
                return false;
            }
            return true;
        }

        private List<PlayerFlask> GetFlasksWithValidActions(List<PlayerFlask> enabledFlasks, List<FlaskActions> validActions, List<FlaskActions> ignoreActions)
        {
            return enabledFlasks.Where(x =>
                       (validActions.Contains(x.Action1) || validActions.Contains(x.Action2)) // Find any flask that matches the actions sent in
                       && (ignoreActions == null || !ignoreActions.Contains(x.Action1) && !ignoreActions.Contains(x.Action2)) // Do not choose ignored flask types
                       && FlaskHelper.canUsePotion(x, Settings.FlaskSettings[x.Index].ReservedUses) // Do not return flasks we can't use
                       ).ToList();
        }

        private List<PlayerFlask> GetFlasksThatArentInstant(List<PlayerFlask> flasksWithValidActions, bool? instant)
        {
            return flasksWithValidActions.Where(x =>
                     (instant == null || (instant == false
                         && (x.InstantType == FlaskInstantType.None || x.InstantType == FlaskInstantType.Partial && !Settings.ForceBubblingAsInstantOnly
                         || x.InstantType == FlaskInstantType.LowLife && !Settings.ForcePanickedAsInstantOnly)
                     ))).ToList();
        }

        private List<PlayerFlask> GetInactiveBuffFlasks(List<PlayerFlask> possiblyActiveFlasks)
        {
            return possiblyActiveFlasks.Where(x =>
                !PlayerHelper.playerHasBuffs(new List<string> { x.BuffString1 }) || 
                !PlayerHelper.playerHasBuffs(new List<string> { x.BuffString2 })
            ).ToList();
        }

        private List<PlayerFlask> GetInstantFlasks(List<PlayerFlask> flasksWithValidActions, bool? instant)
        {
            return flasksWithValidActions.Where(x =>
                instant == true && 
                    (x.InstantType == FlaskInstantType.Partial || x.InstantType == FlaskInstantType.Full || 
                    x.InstantType == FlaskInstantType.LowLife && PlayerHelper.isHealthBelowPercentage(35))
                ).ToList();
        }

        private void LogFlasks(List<PlayerFlask> allFlasks)
        {
            if (Settings.Debug)
            {
                foreach (var flask in allFlasks)
                {
                    LogMessage(PluginName + ": Flask: " + flask.Name + " Instant: " + flask.InstantType.ToString() + " Action1: " + flask.Action1 + " Action2: " + flask.Action2, 5);
                }
            }
        }

        private void LogNoFlasks(List<FlaskActions> flaskActions)
        {
            if (Settings.Debug)
                LogError(PluginName + ": No flasks found for action: " + flaskActions[0], 1);
        }

        private void LogFlaskToUse(List<FlaskActions> flaskActions, List<PlayerFlask> flaskList)
        {
            if (Settings.Debug)
                LogMessage(PluginName + ": Flask(s) found for action: " + flaskActions[0] + " Flask Count: " + flaskList.Count(), 1);

        }

        private Decorator createCurableDebuffDecorator(Dictionary<string, int> dictionary, Composite child, Func<int> minCharges = null)
        {
            return new Decorator((x =>
            {
                var buffs = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>().Buffs;
                foreach (var buff in buffs)
                {
                    if (float.IsInfinity(buff.Timer))
                        continue;

                    int filterId = 0;
                    if (dictionary.TryGetValue(buff.Name, out filterId))
                    {
                        // I'm not sure what the values are here, but this is the effective logic from the old plugin
                        return (filterId == 0 || filterId != 1) && (minCharges == null || buff.Charges >= minCharges());
                    }
                }
                return false;
            }), child);
        }

        public void BuffUi()
        {
            if (!Settings.BuffUiEnable.Value || Cache.InTown) return;
            var x = GameController.Window.GetWindowRectangle().Width * Settings.BuffPositionX.Value * .01f;
            var y = GameController.Window.GetWindowRectangle().Height * Settings.BuffPositionY.Value * .01f;
            var position = new Vector2(x, y);
            float maxWidth = 0;
            float maxheight = 0;
            foreach (var buff in GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>().Buffs)
            {
                var isInfinity = float.IsInfinity(buff.Timer);
                var isFlaskBuff = buff.Name.ToLower().Contains("flask");
                if (!Settings.EnableFlaskAuraBuff.Value && (isInfinity || isFlaskBuff))
                    continue;

                Color textColor;
                if (isFlaskBuff)
                    textColor = Color.SpringGreen;
                else if (isInfinity)
                    textColor = Color.Purple;
                else
                    textColor = Color.WhiteSmoke;

                var size = Graphics.DrawText(buff.Name + ":" + buff.Timer, Settings.BuffTextSize.Value, position, textColor);
                position.Y += size.Height;
                maxheight += size.Height;
                maxWidth = Math.Max(maxWidth, size.Width);
            }
            var background = new RectangleF(x, y, maxWidth, maxheight);
            Graphics.DrawFrame(background, 5, Color.Black);
            Graphics.DrawImage("lightBackground.png", background);
        }
        public void FlaskUi()
        {
            if (!Settings.FlaskUiEnable.Value) return;

            var allFlasks = FlaskHelper.getAllFlaskInfo();

            if (allFlasks == null || allFlasks.Count == 0)
                return;

            var x = GameController.Window.GetWindowRectangle().Width * Settings.FlaskPositionX.Value * .01f;
            var y = GameController.Window.GetWindowRectangle().Height * Settings.FlaskPositionY.Value * .01f;
            var position = new Vector2(x, y);
            float maxWidth = 0;
            float maxheight = 0;
            var textColor = Color.WhiteSmoke;

            int lastIndex = 0;
            foreach (var flasks in allFlasks.OrderBy(flask => flask.Index))
            {
                if (!Settings.FlaskSettings[flasks.Index].Enabled)
                    textColor = Color.Red;
                else switch (flasks.Mods.ItemRarity)
                    {
                        case ItemRarity.Magic:
                            textColor = Color.CornflowerBlue;
                            break;
                        case ItemRarity.Unique:
                            textColor = Color.Chocolate;
                            break;
                        case ItemRarity.Normal:
                            break;
                        case ItemRarity.Rare:
                            break;
                        default:
                            textColor = Color.WhiteSmoke;
                            break;
                    }

                // Flasks are returned in a list that may not contain every flask
                // We need to make sure we are drawing to the right place
                while (lastIndex++ < flasks.Index)
                {
                    var skippedSize = Graphics.DrawText("", Settings.FlaskTextSize.Value, position, textColor);
                    position.Y += skippedSize.Height;
                    maxheight += skippedSize.Height;
                    maxWidth = Math.Max(maxWidth, skippedSize.Width);
                }

                var size = Graphics.DrawText(flasks.Name, Settings.FlaskTextSize.Value, position, textColor);
                position.Y += size.Height;
                maxheight += size.Height;
                maxWidth = Math.Max(maxWidth, size.Width);
            }
            var background = new RectangleF(x, y, maxWidth, maxheight);
            Graphics.DrawFrame(background, 5, Color.Black);
            Graphics.DrawImage("lightBackground.png", background);
        }
        public override void Render()
        {
            base.Render();
            if (!Settings.Enable.Value) return;
            FlaskUi();
            BuffUi();
        }

        public override void InitializeSettingsMenu()
        {

        }

        public override void DrawSettingsMenu()
        {
            TreeNodeFlags collapsingHeaderFlags = TreeNodeFlags.CollapsingHeader;

            if (ImGui.TreeNodeEx("Plugin Options", collapsingHeaderFlags))
            {
                Settings.EnableInHideout.Value = ImGuiExtension.Checkbox("Enable in Hideout", Settings.EnableInHideout);
                ImGui.Separator();
                Settings.TicksPerSecond.Value = ImGuiExtension.IntSlider("Ticks Per Second", Settings.TicksPerSecond); ImGuiExtension.ToolTipWithText("(?)", "Determines how many times the plugin checks flasks every second.\nLower for less resources, raise for faster response (but higher chance to chug potions).");
                ImGui.Separator();
                Settings.Debug.Value = ImGuiExtension.Checkbox("Debug Mode", Settings.Debug);
                ImGui.TreePop();
            }


            if (ImGui.TreeNodeEx("Flask Options", collapsingHeaderFlags))
            {
                if (ImGui.TreeNode("Individual Flask Settings"))
                {
                    for (int i = 0; i < 5; i++)
                    {
                        FlaskSetting currentFlask = Settings.FlaskSettings[i];
                        if (ImGui.TreeNode("Flask " + (i + 1) + " Settings"))
                        {
                            currentFlask.Enabled.Value = ImGuiExtension.Checkbox("Enable", currentFlask.Enabled);
                            currentFlask.Hotkey.Value = ImGuiExtension.HotkeySelector("Hotkey", currentFlask.Hotkey);
                            currentFlask.ReservedUses.Value = ImGuiExtension.IntSlider("Reserved Uses", currentFlask.ReservedUses); ImGuiExtension.ToolTipWithText("(?)", "The absolute number of uses reserved on a flask.\nSet to 1 to always have 1 use of the flask available for manual use.");
                            ImGui.TreePop();
                        }
                    }

                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Health and Mana"))
                {
                    Settings.AutoFlask.Value = ImGuiExtension.Checkbox("Enable", Settings.AutoFlask);

                    ImGuiExtension.SpacedTextHeader("Settings");
                    Settings.ForceBubblingAsInstantOnly = ImGuiExtension.Checkbox("Force Bubbling as Instant only", Settings.ForceBubblingAsInstantOnly);
                    ImGuiExtension.ToolTipWithText("(?)", "When enabled, flasks with the Bubbling mod will only be used as an instant flask.");
                    Settings.ForcePanickedAsInstantOnly = ImGuiExtension.Checkbox("Force Panicked as Instant only", Settings.ForcePanickedAsInstantOnly);
                    ImGuiExtension.ToolTipWithText("(?)", "When enabled, flasks with the Panicked mod will only be used as an instant flask. \nNote, Panicked will not be used until under 35%% with this enabled."); //
                    ImGuiExtension.SpacedTextHeader("Health Flask");
                    Settings.HPPotion.Value = ImGuiExtension.IntSlider("Min Life % Auto HP Flask", Settings.HPPotion);
                    Settings.InstantHPPotion.Value = ImGuiExtension.IntSlider("Min Life % Auto Instant HP Flask", Settings.InstantHPPotion);
                    Settings.DisableLifeSecUse.Value = ImGuiExtension.Checkbox("Disable Life/Hybrid Flask Offensive/Defensive Usage", Settings.DisableLifeSecUse);

                    ImGuiExtension.SpacedTextHeader("Mana Flask");
                    ImGui.Spacing(); Settings.ManaPotion.Value = ImGuiExtension.IntSlider("Min Mana % Auto Mana Flask", Settings.ManaPotion);
                    Settings.InstantManaPotion.Value = ImGuiExtension.IntSlider("Min Mana % Auto Instant MP Flask", Settings.InstantManaPotion);
                    Settings.MinManaFlask.Value = ImGuiExtension.IntSlider("Min Mana Auto Mana Flask", Settings.MinManaFlask);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Remove Ailments"))
                {
                    Settings.RemAilment.Value = ImGuiExtension.Checkbox("Enable", Settings.RemAilment);

                    ImGuiExtension.SpacedTextHeader("Ailments");
                    Settings.RemFrozen.Value = ImGuiExtension.Checkbox("Frozen", Settings.RemFrozen);
                    ImGui.SameLine();
                    Settings.RemBurning.Value = ImGuiExtension.Checkbox("Burning", Settings.RemBurning);
                    Settings.RemShocked.Value = ImGuiExtension.Checkbox("Shocked", Settings.RemShocked);
                    ImGui.SameLine();
                    Settings.RemCurse.Value = ImGuiExtension.Checkbox("Cursed", Settings.RemCurse);
                    Settings.RemPoison.Value = ImGuiExtension.Checkbox("Poison", Settings.RemPoison);
                    ImGui.SameLine();
                    Settings.RemBleed.Value = ImGuiExtension.Checkbox("Bleed", Settings.RemBleed);
                    Settings.CorruptCount.Value = ImGuiExtension.IntSlider("Corrupting Blood Stacks", Settings.CorruptCount);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Speed Flasks"))
                {
                    Settings.SpeedFlaskEnable.Value = ImGuiExtension.Checkbox("Enable", Settings.SpeedFlaskEnable);

                    ImGuiExtension.SpacedTextHeader("Flasks");
                    Settings.QuicksilverFlaskEnable.Value = ImGuiExtension.Checkbox("Quicksilver Flask", Settings.QuicksilverFlaskEnable);
                    Settings.SilverFlaskEnable.Value = ImGuiExtension.Checkbox("Silver Flask", Settings.SilverFlaskEnable);

                    ImGuiExtension.SpacedTextHeader("Settings");
                    Settings.MinMsPlayerMoving.Value = ImGuiExtension.IntSlider("Milliseconds Spent Moving", Settings.MinMsPlayerMoving); ImGuiExtension.ToolTipWithText("(?)", "Milliseconds spent moving before flask will be used.\n1000 milliseconds = 1 second");
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Defensive Flasks"))
                {
                    Settings.DefensiveFlaskEnable.Value = ImGuiExtension.Checkbox("Enable", Settings.DefensiveFlaskEnable);
                    ImGui.Spacing();
                    ImGui.Separator();
                    Settings.HPPercentDefensive.Value = ImGuiExtension.IntSlider("Min Life %", Settings.HPPercentDefensive);
                    Settings.ESPercentDefensive.Value = ImGuiExtension.IntSlider("Min ES %", Settings.ESPercentDefensive);
                    Settings.OffensiveAsDefensiveEnable.Value = ImGuiExtension.Checkbox("Use offensive flasks for defense", Settings.OffensiveAsDefensiveEnable);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Offensive Flasks"))
                {
                    Settings.OffensiveFlaskEnable.Value = ImGuiExtension.Checkbox("Enable", Settings.OffensiveFlaskEnable);
                    ImGui.Spacing();
                    ImGui.Separator();
                    Settings.HPPercentOffensive.Value = ImGuiExtension.IntSlider("Min Life %", Settings.HPPercentOffensive);
                    Settings.ESPercentOffensive.Value = ImGuiExtension.IntSlider("Min ES %", Settings.ESPercentOffensive);
                    ImGui.TreePop();
                }
                ImGui.TreePop();
            }

            if (ImGui.TreeNodeEx("UI Settings", collapsingHeaderFlags))
            {
                if (ImGui.TreeNodeEx("Flask UI", TreeNodeFlags.Framed))
                {
                    Settings.FlaskUiEnable.Value = ImGuiExtension.Checkbox("Enable", Settings.FlaskUiEnable);
                    Settings.FlaskPositionX.Value = ImGuiExtension.FloatSlider("X Position", Settings.FlaskPositionX);
                    Settings.FlaskPositionY.Value = ImGuiExtension.FloatSlider("Y Position", Settings.FlaskPositionY);
                    Settings.FlaskTextSize.Value = ImGuiExtension.IntSlider("Text Size", Settings.FlaskTextSize);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNodeEx("Buff UI", TreeNodeFlags.Framed))
                {
                    Settings.BuffUiEnable.Value = ImGuiExtension.Checkbox("Enable", Settings.BuffUiEnable);
                    Settings.BuffPositionX.Value = ImGuiExtension.FloatSlider("X Position", Settings.BuffPositionX);
                    Settings.BuffPositionY.Value = ImGuiExtension.FloatSlider("Y Position", Settings.BuffPositionY);
                    Settings.BuffTextSize.Value = ImGuiExtension.IntSlider("Text Size", Settings.BuffTextSize);
                    Settings.EnableFlaskAuraBuff.Value = ImGuiExtension.Checkbox("Enable Flask Or Aura Debuff/Buff", Settings.EnableFlaskAuraBuff);
                    ImGui.TreePop();
                }

                ImGui.TreePop();
            }
        }
    }
}