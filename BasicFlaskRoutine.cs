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

namespace TreeRoutine.Routine.BasicFlaskRoutine
{
    public class BasicFlaskRoutine : BaseTreeRoutinePlugin<BasicFlaskRoutineSettings, BaseTreeCache>
    {
        public BasicFlaskRoutine() : base()
        {

        }

        private FlaskSettings[] FlaskSettings { get; set; } = new FlaskSettings[5];
        private KeyboardHelper KeyboardHelper { get; set; } = null;

        public override void Initialise()
        {
            base.Initialise();

            PluginName = "BasicFlaskRoutine";
            KeyboardHelper = new KeyboardHelper(GameController);
            PopulateFlaskSettings();

            Tree = createTree();
        }

        /// <summary>
        /// There may be a better way to do this... but I don't care enough for this example routine
        /// </summary>
        private void PopulateFlaskSettings()
        {
            FlaskSettings[0] = new FlaskSettings(Settings.FlaskSlot1Enable, Settings.FlaskSlot1Hotkey);
            FlaskSettings[1] = new FlaskSettings(Settings.FlaskSlot2Enable, Settings.FlaskSlot2Hotkey);
            FlaskSettings[2] = new FlaskSettings(Settings.FlaskSlot3Enable, Settings.FlaskSlot3Hotkey);
            FlaskSettings[3] = new FlaskSettings(Settings.FlaskSlot4Enable, Settings.FlaskSlot4Hotkey);
            FlaskSettings[4] = new FlaskSettings(Settings.FlaskSlot5Enable, Settings.FlaskSlot5Hotkey);

        }

        private Composite createTree()
        {
            return new Decorator(x => TreeHelper.canTick() && !PlayerHelper.isPlayerDead() && (!Cache.InHideout || Settings.EnableInHideout),
                    new PrioritySelector
                    (
                        createInstantHPPotionComposite(),
                        createHPPotionComposite(),
                        createInstantManaPotionComposite(),
                        createManaPotionComposite(),
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
            return new Decorator((x => Settings.SpeedFlaskEnable && (PlayerHelper.playerDoesNotHaveAnyOfBuffs(new List<string>() { "flask_bonus_movement_speed", "flask_utility_sprint", "flask_utility_haste" }))),
                new PrioritySelector(
                    new Decorator((x => Settings.QuicksilverFlaskEnable), createUseFlaskAction(FlaskActions.Speedrun)),
                    new Decorator((x => Settings.SilverFlaskEnable), createUseFlaskAction(FlaskActions.OFFENSE_AND_SPEEDRUN))
                )
            );
        }

        private Composite createDefensivePotionComposite()
        {
            return new Decorator((x => Settings.DefensiveFlaskEnable && (PlayerHelper.isHealthBelowPercentage(Settings.HPPercentDefensive) || PlayerHelper.isEnergyShieldBelowPercentage(Settings.ESPercentDefensive))),
                new PrioritySelector(
                    createUseFlaskAction(FlaskActions.Defense),
                    new Decorator((x => Settings.OffensiveAsDefensiveEnable), createUseFlaskAction(new List<FlaskActions> { FlaskActions.OFFENSE_AND_SPEEDRUN, FlaskActions.Defense }, ignoreFlasksWithAction: (() => Settings.DisableLifeSecUse ? new List<FlaskActions>() { FlaskActions.Life, FlaskActions.Mana, FlaskActions.Hybrid } : null)))
                )
            );
        }

        private Composite createOffensivePotionComposite()
        {
            return new PrioritySelector(
                new Decorator((x => Settings.OffensiveFlaskEnable && (PlayerHelper.isHealthBelowPercentage(Settings.HPPercentOffensive) || PlayerHelper.isEnergyShieldBelowPercentage(Settings.ESPercentOffensive))),
                    createUseFlaskAction(new List<FlaskActions> { FlaskActions.Offense, FlaskActions.OFFENSE_AND_SPEEDRUN }, ignoreFlasksWithAction: (() => Settings.DisableLifeSecUse ?  new List<FlaskActions>() { FlaskActions.Life, FlaskActions.Mana, FlaskActions.Hybrid} : null)))
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

                return FlaskSettings[foundFlask.Index].Hotkey;
            });
        }

        private PlayerFlask findFlaskMatchingAnyAction(FlaskActions flaskAction, Boolean? instant = null, Func<List<FlaskActions>> ignoreFlasksWithAction = null)
        {
            return findFlaskMatchingAnyAction(new List<FlaskActions> { flaskAction }, instant, ignoreFlasksWithAction);
        }


        private PlayerFlask findFlaskMatchingAnyAction (List<FlaskActions> flaskActions, Boolean? instant = null, Func<List<FlaskActions>> ignoreFlasksWithAction = null)
        {
            var allFlasks = FlaskHelper.getAllFlaskInfo();

            // We have no flasks or settings for flasks?
            if (allFlasks == null || FlaskSettings == null)
            {
                if (Settings.Debug)
                {
                    if (allFlasks == null)
                        LogMessage("No flasks to match against.", 5);
                    else if (FlaskSettings == null)
                        LogMessage("Flask settings were null. Hopefully doesn't happen frequently.", 5);
                }

                return null;
            }

            if (Settings.Debug)
            {
                foreach (var flask in allFlasks)
                {
                    LogMessage("Flask: " + flask.Name + " Instant: " + flask.Instant + " Action1: " + flask.Action1 + " Action2: " + flask.Action2, 5);
                }
            }

            List<FlaskActions> ignoreFlaskActions = ignoreFlasksWithAction == null ? null : ignoreFlasksWithAction();

            var flaskList = allFlasks.FindAll(x =>
                    // Below are cheap operations and should be done first
                    FlaskSettings[x.Index].Enabled                       // Only search for enabled flasks
                    && (instant == null || instant.GetValueOrDefault() == x.Instant ) // Only search for flasks matching the requested instant value
                    && (flaskActions.Contains(x.Action1) || flaskActions.Contains(x.Action2)) // Find any flask that matches the actions sent in
                    && (ignoreFlaskActions == null || !ignoreFlasksWithAction().Contains(x.Action1) && !ignoreFlasksWithAction().Contains(x.Action2)) // Do not choose ignored flask types
                    && FlaskHelper.canUsePotion(x)                      // Do not return flasks we can't use
                    // Below are more expensive operations and should be done last
                    && (x.Instant || (!PlayerHelper.playerHasBuffs(new List<string> { x.BuffString1 }) || !PlayerHelper.playerHasBuffs(new List<string> { x.BuffString2 }))) // If the flask is not instant, ensure we are missing at least one of the flask buffs
                    );
            if (flaskList != null && flaskList.Count == 0)
            {
                if (Settings.Debug)
                    LogError("No flasks found for action: " + flaskActions[0], 1);
                return null;
            }

            if (Settings.Debug)
                LogMessage("Flask(s) found for action: " + flaskActions[0] + " Flask Count: " + flaskList.Count, 1);

            return flaskList[0];
        }

        private Decorator createCurableDebuffDecorator(Dictionary<string, int> dictionary, Composite child, Func<int> minCharges = null)
        {
            return new Decorator((x =>
            {
                var buffs = Cache.SavedIngameState.Data.LocalPlayer.GetComponent<Life>().Buffs;
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
                if (!FlaskSettings[flasks.Index].Enabled)
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
    }
}