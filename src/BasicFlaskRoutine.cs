    using TreeRoutine.DefaultBehaviors.Actions;
using TreeRoutine.DefaultBehaviors.Helpers;
using TreeRoutine.FlaskComponents;
using TreeRoutine.Routine.BasicFlaskRoutine.Flask;
using PoeHUD.Poe.Components;
using System;
using System.Collections.Generic;
using SharpDX;
using PoeHUD.Models.Enums;
using System.Linq;
using TreeRoutine.Menu;
using ImGuiNET;
using PoeHUD.Framework;
using PoeHUD.Framework.Helpers;
using System.Diagnostics;
using PoeHUD.Models;
    using TreeRoutine.TreeSharp;

namespace TreeRoutine.Routine.BasicFlaskRoutine
{
    public class BasicFlaskRoutine : BaseTreeRoutinePlugin<BasicFlaskRoutineSettings, BaseTreeCache>
    {
        public BasicFlaskRoutine() : base()
        {

        }

        public Composite Tree { get; set; }
        private Coroutine TreeCoroutine { get; set; }
        public Object LoadedMonstersLock { get; set; } = new Object();
        public List<EntityWrapper> LoadedMonsters { get; protected set; } = new List<EntityWrapper>();


        private KeyboardHelper KeyboardHelper { get; set; } = null;

        private Stopwatch PlayerMovingStopwatch { get; set; } = new Stopwatch();

        public override void Initialise()
        {
            base.Initialise();

            PluginName = "BasicFlaskRoutine";
            KeyboardHelper = new KeyboardHelper(GameController);

            Tree = CreateTree();

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

        private Composite CreateTree()
        {
            return new Decorator(x => TreeHelper.CanTick() && !PlayerHelper.isPlayerDead() && (!Cache.InHideout || Settings.EnableInHideout) && PlayerHelper.playerDoesNotHaveAnyOfBuffs(new List<string>() { "grace_period" }),
                    new PrioritySelector
                    (
                        new Decorator(x => Settings.AutoFlask,
                        new PrioritySelector(
                            CreateInstantHPPotionComposite(),
                            CreateHPPotionComposite(),
                            CreateInstantManaPotionComposite(),
                            CreateManaPotionComposite()
                            )
                        ),
                        CreateAilmentPotionComposite(),
                        CreateDefensivePotionComposite(),
                        CreateSpeedPotionComposite(),
                        CreateOffensivePotionComposite()
                    )
                );
        }

        private Composite CreateInstantHPPotionComposite()
        {
            return new Decorator((x => PlayerHelper.isHealthBelowPercentage(Settings.InstantHPPotion)),
                new PrioritySelector(
                    CreateUseFlaskAction(FlaskActions.Life, true, true),
                    CreateUseFlaskAction(FlaskActions.Hybrid, true, true)
                )
            );

        }

        private Composite CreateHPPotionComposite()
        {
            return new Decorator((x => PlayerHelper.isHealthBelowPercentage(Settings.HPPotion)),
                new Decorator((x => PlayerHelper.playerDoesNotHaveAnyOfBuffs(new List<string>() { "flask_effect_life" })),
                 new PrioritySelector(
                    CreateUseFlaskAction(FlaskActions.Life, false),
                    CreateUseFlaskAction(FlaskActions.Hybrid, false)
                    )
                )
            );
        }

        private Composite CreateInstantManaPotionComposite()
        {
            return new Decorator((x => PlayerHelper.isManaBelowPercentage(Settings.InstantManaPotion)),
                new PrioritySelector(
                    CreateUseFlaskAction(FlaskActions.Mana, true, true),
                    CreateUseFlaskAction(FlaskActions.Hybrid, true, true)
                )
            );
        }

        private Composite CreateManaPotionComposite()
        {
            return new Decorator((x => PlayerHelper.isManaBelowPercentage(Settings.ManaPotion) || PlayerHelper.isManaBelowValue(Settings.MinManaFlask)),
                new Decorator((x => PlayerHelper.playerDoesNotHaveAnyOfBuffs(new List<string>() { "flask_effect_mana" })),
                    new PrioritySelector(
                        CreateUseFlaskAction(FlaskActions.Mana, false),
                        CreateUseFlaskAction(FlaskActions.Hybrid, false)
                    )
                )
            );
        }

        private Composite CreateSpeedPotionComposite()
        {
            return new Decorator((x => Settings.SpeedFlaskEnable && Settings.MinMsPlayerMoving <= PlayerMovingStopwatch.ElapsedMilliseconds && (PlayerHelper.playerDoesNotHaveAnyOfBuffs(new List<string>() { "flask_bonus_movement_speed", "flask_utility_sprint" }) && (!Settings.SilverFlaskEnable || PlayerHelper.playerDoesNotHaveAnyOfBuffs(new List<string>() { "flask_utility_haste" })))),
                new PrioritySelector(
                    new Decorator((x => Settings.QuicksilverFlaskEnable), CreateUseFlaskAction(FlaskActions.Speedrun)),
                    new Decorator((x => Settings.SilverFlaskEnable), CreateUseFlaskAction(FlaskActions.OFFENSE_AND_SPEEDRUN))
                )
            );
        }

        private Composite CreateDefensivePotionComposite()
        {
            return new Decorator((x => Settings.DefensiveFlaskEnable && (PlayerHelper.isHealthBelowPercentage(Settings.HPPercentDefensive) || PlayerHelper.isEnergyShieldBelowPercentage(Settings.ESPercentDefensive) || Settings.DefensiveMonsterCount > 0 && HasEnoughNearbyMonsters(Settings.DefensiveMonsterCount, Settings.DefensiveMonsterDistance, Settings.DefensiveCountNormalMonsters, Settings.DefensiveCountRareMonsters, Settings.DefensiveCountMagicMonsters, Settings.DefensiveCountUniqueMonsters))),
                new PrioritySelector(
                    CreateUseFlaskAction(FlaskActions.Defense),
                    new Decorator((x => Settings.OffensiveAsDefensiveEnable), CreateUseFlaskAction(new List<FlaskActions> { FlaskActions.OFFENSE_AND_SPEEDRUN, FlaskActions.Defense }, ignoreFlasksWithAction: (() => Settings.DisableLifeSecUse ? new List<FlaskActions>() { FlaskActions.Life, FlaskActions.Mana, FlaskActions.Hybrid } : null)))
                )
            );
        }

        private Composite CreateOffensivePotionComposite()
        {
            return new PrioritySelector(
                new Decorator((x => Settings.OffensiveFlaskEnable && (PlayerHelper.isHealthBelowPercentage(Settings.HPPercentOffensive) || PlayerHelper.isEnergyShieldBelowPercentage(Settings.ESPercentOffensive) || Settings.OffensiveMonsterCount > 0 && HasEnoughNearbyMonsters(Settings.OffensiveMonsterCount, Settings.OffensiveMonsterDistance, Settings.OffensiveCountNormalMonsters, Settings.OffensiveCountRareMonsters, Settings.OffensiveCountMagicMonsters, Settings.OffensiveCountUniqueMonsters))),
                    CreateUseFlaskAction(new List<FlaskActions> { FlaskActions.Offense, FlaskActions.OFFENSE_AND_SPEEDRUN }, ignoreFlasksWithAction: (() => Settings.DisableLifeSecUse ?  new List<FlaskActions>() { FlaskActions.Life, FlaskActions.Mana, FlaskActions.Hybrid} : null)))
            );
        }

        private Composite CreateAilmentPotionComposite()
        {
            return new Decorator(x => Settings.RemAilment,
                new PrioritySelector(
                    new Decorator(x => Settings.RemBleed, CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Bleeding, CreateUseFlaskAction(FlaskActions.BleedImmune, isCleansing:true))),
                    new Decorator(x => Settings.RemBurning, CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Burning, CreateUseFlaskAction(FlaskActions.IgniteImmune, isCleansing: true))),
                    CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Corruption, CreateUseFlaskAction(FlaskActions.BleedImmune, isCleansing: true), (() => Settings.CorruptCount)),
                    new Decorator(x => Settings.RemFrozen, CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Frozen, CreateUseFlaskAction(FlaskActions.FreezeImmune, isCleansing: true))),
                    new Decorator(x => Settings.RemPoison, CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Poisoned, CreateUseFlaskAction(FlaskActions.PoisonImmune, isCleansing: true))),
                    new Decorator(x => Settings.RemShocked, CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Shocked, CreateUseFlaskAction(FlaskActions.ShockImmune, isCleansing: true))),
                    new Decorator(x => Settings.RemCurse, CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.WeakenedSlowed, CreateUseFlaskAction(FlaskActions.CurseImmune, isCleansing: true)))
                    )
                );
        }

        private Composite CreateUseFlaskAction(FlaskActions flaskAction, Boolean? instant = null, Boolean ignoreBuffs = false, Func<List<FlaskActions>> ignoreFlasksWithAction = null, Boolean isCleansing = false)
        {
            return CreateUseFlaskAction(new List<FlaskActions> { flaskAction }, instant, ignoreBuffs, ignoreFlasksWithAction, isCleansing);
        }

        private Composite CreateUseFlaskAction(List<FlaskActions> flaskActions, Boolean? instant = null, Boolean ignoreBuffs = false, Func<List<FlaskActions>> ignoreFlasksWithAction = null, Boolean isCleansing = false)
        {
            return new UseHotkeyAction(KeyboardHelper, x =>
            {
                var foundFlask = FindFlaskMatchingAnyAction(flaskActions, instant, ignoreBuffs, ignoreFlasksWithAction, isCleansing);

                if (foundFlask == null)
                {
                    return null;
                }

                return Settings.FlaskSettings[foundFlask.Index].Hotkey;
            });
        }

        private Boolean HasEnoughNearbyMonsters(int minimumMonsterCount, int maxDistance, bool countNormal, bool countRare, bool countMagic, bool countUnique)
        {
            var mobCount = 0;
            var maxDistanceSquare = maxDistance * maxDistance;

            var playerPosition = GameController.Player.Pos;

            if (LoadedMonsters != null)
            {
                List<PoeHUD.Models.EntityWrapper> localLoadedMonsters = null;
                lock (LoadedMonstersLock)
                {
                    localLoadedMonsters = new List<PoeHUD.Models.EntityWrapper>(LoadedMonsters);
                }

                // Make sure we create our own list to iterate as we may be adding/removing from the list
                foreach (var monster in localLoadedMonsters)
                {
                    if (!monster.HasComponent<Monster>() || !monster.IsValid || !monster.IsAlive || !monster.IsHostile || monster.Invincible || monster.CannotBeDamaged)
                        continue;

                    var monsterType = monster.GetComponent<ObjectMagicProperties>().Rarity;

                    // Don't count this monster type if we are ignoring it
                    if (monsterType == MonsterRarity.White && !countNormal
                        || monsterType == MonsterRarity.Rare && !countRare
                        || monsterType == MonsterRarity.Magic && !countMagic
                        || monsterType == MonsterRarity.Unique && !countUnique)
                        continue;

                    var monsterPosition = monster.Pos;

                    var xDiff = playerPosition.X - monsterPosition.X;
                    var yDiff = playerPosition.Y - monsterPosition.Y;
                    var monsterDistanceSquare = (xDiff * xDiff + yDiff * yDiff);

                    if (monsterDistanceSquare <= maxDistanceSquare)
                    {
                        mobCount++;
                    }

                    if (mobCount >= minimumMonsterCount)
                    {
                        if (Settings.Debug)
                        {
                            Log("NearbyMonstersCondition returning true because " + mobCount + " mobs valid monsters were found nearby.", 2);
                        }
                        return true;
                    }
                }
            } else if (Settings.Debug)
            {
                Log("NearbyMonstersCondition returning false because mob list was invalid.", 2);
            }

            if (Settings.Debug)
            {
                Log("NearbyMonstersCondition returning false because " + mobCount + " mobs valid monsters were found nearby.", 2);
            }
            return false;
        }

        private PlayerFlask FindFlaskMatchingAnyAction(FlaskActions flaskAction, Boolean? instant = null, Boolean ignoreBuffs = false, Func<List<FlaskActions>> ignoreFlasksWithAction = null, Boolean isCleansing = false)
        {
            return FindFlaskMatchingAnyAction(new List<FlaskActions> { flaskAction }, instant, ignoreBuffs, ignoreFlasksWithAction, isCleansing);
        }

        private PlayerFlask FindFlaskMatchingAnyAction(List<FlaskActions> flaskActions, Boolean? instant = null, Boolean ignoreBuffs = false, Func<List<FlaskActions>> ignoreFlasksWithAction = null, Boolean isCleansing = false)
        {
            var allFlasks = FlaskHelper.GetAllFlaskInfo();

            // We have no flasks or settings for flasks?
            if (allFlasks == null || Settings.FlaskSettings == null)
            {
                if (Settings.Debug)
                {
                    if (allFlasks == null)
                        LogMessage(PluginName + ": No flasks to match against.", 5);
                    else if (Settings.FlaskSettings == null)
                        LogMessage(PluginName + ": Flask settings were null. Hopefully doesn't happen frequently.", 5);
                }

                return null;
            }

            if (Settings.Debug)
            {
                foreach (var flask in allFlasks)
                {
                    LogMessage($"{PluginName}: Flask: {flask.Name} Slot: {flask.Index} Instant: {flask.InstantType} Action1: {flask.Action1} Action2: {flask.Action2}", 5);
                }
            }

            List<FlaskActions> ignoreFlaskActions = ignoreFlasksWithAction == null ? null : ignoreFlasksWithAction();

            var flaskList = allFlasks
                    .Where(x =>
                    Settings.FlaskSettings[x.Index].Enabled
                    && FlaskHasAvailableAction(flaskActions, ignoreFlaskActions, x)
                    && FlaskHelper.CanUsePotion(x, Settings.FlaskSettings[x.Index].ReservedUses, isCleansing)
                    && FlaskMatchesInstant(x, instant)
                    && (ignoreBuffs || MissingFlaskBuff(x))
                    ).OrderByDescending(x => flaskActions.Contains(x.Action1)).ThenByDescending(x => x.TotalUses - Settings.FlaskSettings[x.Index].ReservedUses).ToList();


            if (flaskList == null || !flaskList.Any())
            {
                if (Settings.Debug)
                    LogError(PluginName + ": No flasks found for action: " + flaskActions[0], 1);
                return null;
            }

            if (Settings.Debug)
                LogMessage(PluginName + ": Flask(s) found for action: " + flaskActions[0] + " Flask Count: " + flaskList.Count(), 1);

            return flaskList.FirstOrDefault();
        }

        private bool FlaskHasAvailableAction(List<FlaskActions> flaskActions, List<FlaskActions> ignoreFlaskActions, PlayerFlask flask)
        {
            return flaskActions.Any(x => x == flask.Action1 || x == flask.Action2)
                    && (ignoreFlaskActions == null || !ignoreFlaskActions.Any(x => x == flask.Action1 || x == flask.Action2));
        }

        private bool FlaskMatchesInstant(PlayerFlask playerFlask, Boolean? instant)
        {
            return instant == null
                    || instant == false && CanUseFlaskAsRegen(playerFlask)
                    || instant == true && CanUseFlaskAsInstant(playerFlask);
        }

        private bool CanUseFlaskAsInstant(PlayerFlask playerFlask)
        {
            // If the flask is instant, no special logic needed
            return playerFlask.InstantType == FlaskInstantType.Partial
                    || playerFlask.InstantType == FlaskInstantType.Full
                    || playerFlask.InstantType == FlaskInstantType.LowLife && PlayerHelper.isHealthBelowPercentage(35);
        }

        private bool CanUseFlaskAsRegen(PlayerFlask playerFlask)
        {
            return playerFlask.InstantType == FlaskInstantType.None
                    || playerFlask.InstantType == FlaskInstantType.Partial && !Settings.ForceBubblingAsInstantOnly
                    || playerFlask.InstantType == FlaskInstantType.LowLife && !Settings.ForcePanickedAsInstantOnly;
        }

        private bool MissingFlaskBuff(PlayerFlask playerFlask)
        {
            return !PlayerHelper.playerHasBuffs(new List<string> { playerFlask.BuffString1 }) || !PlayerHelper.playerHasBuffs(new List<string> { playerFlask.BuffString2 });
        }

        private Decorator CreateCurableDebuffDecorator(Dictionary<string, int> dictionary, Composite child, Func<int> minCharges = null)
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

            var allFlasks = FlaskHelper.GetAllFlaskInfo();

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
                    ImGui.Separator();

                    Settings.DefensiveMonsterCount.Value = ImGuiExtension.IntSlider("Monster Count", Settings.DefensiveMonsterCount);
                    Settings.DefensiveMonsterDistance.Value = ImGuiExtension.IntSlider("Monster Distance", Settings.DefensiveMonsterDistance);
                    Settings.DefensiveCountNormalMonsters = ImGuiExtension.Checkbox("Normal Monsters", Settings.DefensiveCountNormalMonsters);
                    Settings.DefensiveCountRareMonsters = ImGuiExtension.Checkbox("Rare Monsters", Settings.DefensiveCountRareMonsters);
                    Settings.DefensiveCountMagicMonsters = ImGuiExtension.Checkbox("Magic Monsters", Settings.DefensiveCountMagicMonsters);
                    Settings.DefensiveCountUniqueMonsters = ImGuiExtension.Checkbox("Unique Monsters", Settings.DefensiveCountUniqueMonsters);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Offensive Flasks"))
                {
                    Settings.OffensiveFlaskEnable.Value = ImGuiExtension.Checkbox("Enable", Settings.OffensiveFlaskEnable);
                    ImGui.Spacing();
                    ImGui.Separator();
                    Settings.HPPercentOffensive.Value = ImGuiExtension.IntSlider("Min Life %", Settings.HPPercentOffensive);
                    Settings.ESPercentOffensive.Value = ImGuiExtension.IntSlider("Min ES %", Settings.ESPercentOffensive);
                    ImGui.Separator();
                    Settings.OffensiveMonsterCount.Value = ImGuiExtension.IntSlider("Monster Count", Settings.OffensiveMonsterCount);
                    Settings.OffensiveMonsterDistance.Value = ImGuiExtension.IntSlider("Monster Distance", Settings.OffensiveMonsterDistance);
                    Settings.OffensiveCountNormalMonsters = ImGuiExtension.Checkbox("Normal Monsters", Settings.OffensiveCountNormalMonsters);
                    Settings.OffensiveCountRareMonsters = ImGuiExtension.Checkbox("Rare Monsters", Settings.OffensiveCountRareMonsters);
                    Settings.OffensiveCountMagicMonsters = ImGuiExtension.Checkbox("Magic Monsters", Settings.OffensiveCountMagicMonsters);
                    Settings.OffensiveCountUniqueMonsters = ImGuiExtension.Checkbox("Unique Monsters", Settings.OffensiveCountUniqueMonsters);

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

        public override void EntityAdded(EntityWrapper entityWrapper)
        {
            if (entityWrapper.HasComponent<Monster>())
            {
                lock (LoadedMonstersLock)
                {
                    LoadedMonsters.Add(entityWrapper);
                }
            }
        }

        public override void EntityRemoved(EntityWrapper entityWrapper)
        {
            lock (LoadedMonstersLock)
            {
                LoadedMonsters.Remove(entityWrapper);
            }
        }
    }
}