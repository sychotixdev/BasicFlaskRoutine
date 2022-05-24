﻿using TreeRoutine.DefaultBehaviors.Actions;
using TreeRoutine.DefaultBehaviors.Helpers;
using TreeRoutine.FlaskComponents;
using TreeRoutine.Routine.BasicFlaskRoutine.Flask;
using System;
using System.Collections.Generic;
using SharpDX;
using System.Linq;
using TreeRoutine.Menu;
using ImGuiNET;
using System.Diagnostics;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
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
        public List<Entity> LoadedMonsters { get; protected set; } = new List<Entity>();


        private KeyboardHelper KeyboardHelper { get; set; } = null;

        private Stopwatch PlayerMovingStopwatch { get; set; } = new Stopwatch();

        private const string basicFlaskRoutineChecker = "BasicFlaskRoutine Checker";

        private readonly List<string> travelingSkills = new List<string>()
        {
            "Blink Arrow",
            "Bodyswap",
            "Dash",
            "Flame Dash",
            "Frostblink",
            "Leap Slam",
            "Lightning Warp",
            "Mirror Arrow",
            "Phase Run",
            "Shield Charge",
            "Smoke Mine",
            "Vaal Lightning Warp",
            "Whirling Blades",
            "Withering Step"
        };



        public override bool Initialise()
        {
            base.Initialise();

            Name = "BasicFlaskRoutine";
            KeyboardHelper = new KeyboardHelper(GameController);

            Tree = CreateTree();

            // Add this as a coroutine for this plugin
            Settings.Enable.OnValueChanged += (sender, b) =>
            {
                if (b)
                {
                    if (Core.ParallelRunner.FindByName(basicFlaskRoutineChecker) == null) InitCoroutine();
                    TreeCoroutine?.Resume();
                }
                else
                    TreeCoroutine?.Pause();

            };
            InitCoroutine();

            Settings.TicksPerSecond.OnValueChanged += (sender, b) =>
            {
                UpdateCoroutineWaitRender();
            };

            return true;
        }

        private void InitCoroutine()
        {
            TreeCoroutine = new Coroutine(() => TickTree(Tree), new WaitTime(1000 / Settings.TicksPerSecond), this, "BasicFlaskRoutine Tree");
            Core.ParallelRunner.Run(TreeCoroutine);
        }

        private void UpdateCoroutineWaitRender()
        {
            TreeCoroutine.UpdateCondition(new WaitTime(1000 / Settings.TicksPerSecond));
        }

        protected override void UpdateCache()
        {
            base.UpdateCache();

            UpdatePlayerMovingStopwatch();
        }

        private void UpdatePlayerMovingStopwatch()
        {
            var player = GameController.Player.GetComponent<Actor>();
            if (player != null && player.Address != 0 && player.isMoving
                || (player.isAttacking && isTravelingSkill(player.CurrentAction.Skill)))
            {
                if (!PlayerMovingStopwatch.IsRunning)
                    PlayerMovingStopwatch.Start();
            }
            else
            {
                PlayerMovingStopwatch.Reset();
            }
        }

        private bool isTravelingSkill(ActorSkill skill)
        {
            return travelingSkills.Contains(skill.EffectsPerLevel.SkillGemWrapper.ActiveSkill.DisplayName);
        }

        private Composite CreateTree()
        {
            return new Decorator(x => TreeHelper.CanTick() && !PlayerHelper.isPlayerDead() &&
            (!Cache.InHideout || Settings.EnableInHideout) && PlayerHelper.playerDoesNotHaveAnyOfBuffs(new List<string>() { "grace_period" }),
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
                        CreateOffensivePotionComposite(),
                        CreateUseWhenFlaskFullComposite()
                    )
                );
        }

        private Composite CreateInstantHPPotionComposite()
        {
            return new Decorator((x => !Settings.BossingMode && PlayerHelper.isHealthBelowPercentage(Settings.InstantHPPotion)
                                        || (Settings.AllocatedSupremeDecadence && PlayerHelper.isEnergyShieldBelowPercentage(Settings.InstantESPotion))
                                ),
                new PrioritySelector(
                    CreateUseFlaskAction(FlaskActions.Life, true, true),
                    CreateUseFlaskAction(FlaskActions.Hybrid, true, true),
                    CreateUseFlaskAction(FlaskActions.Life),
                    CreateUseFlaskAction(FlaskActions.Hybrid)
                )
            );

        }

        private Composite CreateHPPotionComposite()
        {
            return new Decorator((x => !Settings.BossingMode && PlayerHelper.isHealthBelowPercentage(Settings.HPPotion)
                                        || (Settings.AllocatedSupremeDecadence && PlayerHelper.isEnergyShieldBelowPercentage(Settings.ESPotion))
                                ),
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
                new Decorator((x => PlayerHelper.playerDoesNotHaveAnyOfBuffs(new List<string>() { "flask_effect_mana", "flask_effect_mana_not_removed_when_full" })),
                    new PrioritySelector(
                        CreateUseFlaskAction(FlaskActions.Mana, false),
                        CreateUseFlaskAction(FlaskActions.Hybrid, false)
                    )
                )
            );
        }

        private Composite CreateSpeedPotionComposite()
        {
            return new Decorator((x => Settings.SpeedFlaskEnable
                                    && (Settings.MinMsPlayerMoving <= PlayerMovingStopwatch.ElapsedMilliseconds
                                        || Settings.UseWhileCycloning
                                            && IsCycloning()
                                            && (Settings.CycloningMonsterCount == 0
                                                || HasEnoughNearbyMonsters(Settings.CycloningMonsterCount,
                                                                           Settings.CycloningMonsterDistance,
                                                                           Settings.CycloningCountNormalMonsters,
                                                                           Settings.CycloningCountRareMonsters,
                                                                           Settings.CycloningCountMagicMonsters,
                                                                           Settings.CycloningCountUniqueMonsters,
                                                                           Settings.CycloningIgnoreFullHealthUniqueMonsters)))
                                    && (PlayerHelper.playerDoesNotHaveAnyOfBuffs(new List<string>() { "flask_bonus_movement_speed", "flask_utility_sprint", "flask_utility_phase" })
                                        || (!Settings.SilverFlaskEnable
                                            || PlayerHelper.playerDoesNotHaveAnyOfBuffs(new List<string>() { "flask_utility_haste" })))),
                new PrioritySelector(
                    new Decorator((x => Settings.QuicksilverFlaskEnable), CreateUseFlaskAction(FlaskActions.Speedrun)),
                    new Decorator((x => Settings.SilverFlaskEnable), CreateUseFlaskAction(FlaskActions.OFFENSE_AND_SPEEDRUN))
                )
            );
        }

        private Composite CreateDefensivePotionComposite()
        {
            return new Decorator((x => Settings.DefensiveFlaskEnable && !Settings.BossingMode &&
            (PlayerHelper.isHealthBelowPercentage(Settings.HPPercentDefensive) || PlayerHelper.isEnergyShieldBelowPercentage(Settings.ESPercentDefensive) ||
            Settings.DefensiveMonsterCount > 0 && HasEnoughNearbyMonsters(Settings.DefensiveMonsterCount, Settings.DefensiveMonsterDistance, Settings.DefensiveCountNormalMonsters, Settings.DefensiveCountRareMonsters, Settings.DefensiveCountMagicMonsters, Settings.DefensiveCountUniqueMonsters, Settings.DefensiveIgnoreFullHealthUniqueMonsters))),
                new PrioritySelector(
                    CreateUseFlaskAction(FlaskActions.Defense),
                    new Decorator((x => Settings.OffensiveAsDefensiveEnable), CreateUseFlaskAction(new List<FlaskActions> { FlaskActions.OFFENSE_AND_SPEEDRUN, FlaskActions.Defense }, ignoreFlasksWithAction: (() => Settings.DisableLifeSecUse ? new List<FlaskActions>() { FlaskActions.Life, FlaskActions.Mana, FlaskActions.Hybrid } : null)))
                )
            );
        }

        private Composite CreateOffensivePotionComposite()
        {
            return new PrioritySelector(
                new Decorator((x => Settings.OffensiveFlaskEnable && !Settings.BossingMode &&
                (PlayerHelper.isHealthBelowPercentage(Settings.HPPercentOffensive) || PlayerHelper.isEnergyShieldBelowPercentage(Settings.ESPercentOffensive) || Settings.OffensiveMonsterCount > 0 && HasEnoughNearbyMonsters(Settings.OffensiveMonsterCount, Settings.OffensiveMonsterDistance, Settings.OffensiveCountNormalMonsters, Settings.OffensiveCountRareMonsters, Settings.OffensiveCountMagicMonsters, Settings.OffensiveCountUniqueMonsters, Settings.OffensiveIgnoreFullHealthUniqueMonsters))),
                    CreateUseFlaskAction(new List<FlaskActions> { FlaskActions.Offense, FlaskActions.OFFENSE_AND_SPEEDRUN }, ignoreFlasksWithAction: (() => Settings.DisableLifeSecUse ? new List<FlaskActions>() { FlaskActions.Life, FlaskActions.Mana, FlaskActions.Hybrid } : null)))
            );
        }

        private Composite CreateUseWhenFlaskFullComposite()
        {
            // Fill a composite of decors based on each flask slot to use
            Composite[] composites = new Composite[Settings.FlaskSettings.Length];
            for (int i = 0; i < composites.Length; i++)
            {
                // Localize the scope of i (for those who don't understand how lamba functions handle scopes)
                int index = i;

                // Use flask when full and at least 1 use
                CanRunDecoratorDelegate checkFlaskFull = x =>
                {
                    var flasks = FlaskHelper.GetAllFlaskInfo();
                    if (flasks == null)
                    {
                        return false;
                    }

                    return flasks
                        .Where(f => f != null && f.Index == index)
                        .Any(f => f.IsFull && f.TotalUses > 0);
                };

                // Press hotkey on action
                var useFlaskAction = new UseHotkeyAction(KeyboardHelper, (x) => Settings.FlaskSettings[index].Hotkey);

                // Create decorator for use
                var useFlaskWhenFull = new Decorator(checkFlaskFull, useFlaskAction);

                // Create decorator that first checks if enabled
                var decor = new Decorator(x => Settings.FlaskSettings[index].Enabled && Settings.FlaskSettings[index].UseWhenChargesFilled, useFlaskWhenFull);

                // Set decor at that index
                composites[index] = decor;
            }

            // Return group of decor
            return new Parallel(composites);
        }

        private Composite CreateAilmentPotionComposite()
        {
            return new Decorator(x => Settings.RemAilment && !Settings.BossingMode,
                new PrioritySelector(
                    new Decorator(x => Settings.RemBleed, CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Bleeding, CreateUseFlaskAction(new List<FlaskActions> { FlaskActions.CorruptedBloodAndBleedImmune, FlaskActions.BleedImmune }, isCleansing: true))),
                    new Decorator(x => Settings.RemBurning, CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Burning, CreateUseFlaskAction(FlaskActions.IgniteImmune, isCleansing: true))),
                    CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Corruption, CreateUseFlaskAction(FlaskActions.CorruptedBloodAndBleedImmune, isCleansing: true), () => Settings.CorruptCount, () => Settings.RemCorruptingBlood),
                    new Decorator(x => Settings.RemFrozen, CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Frozen, CreateUseFlaskAction(FlaskActions.FreezeImmune, isCleansing: true))),
                    new Decorator(x => Settings.RemPoison, CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Poisoned, CreateUseFlaskAction(FlaskActions.PoisonImmune, isCleansing: true))),
                    new Decorator(x => Settings.RemShocked, CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Shocked, CreateUseFlaskAction(FlaskActions.ShockImmune, isCleansing: true))),
                    new Decorator(x => Settings.RemCurse, CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.WeakenedSlowed, CreateUseFlaskAction(FlaskActions.CurseImmune, isCleansing: true))),
                    new Decorator(x => Settings.RemMaimed, CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Maimed, CreateUseFlaskAction(FlaskActions.MaimAndHinderImmune, isCleansing: true))),
                    CreateCurableDebuffDecorator(Cache.DebuffPanelConfig.Hindered, CreateUseFlaskAction(FlaskActions.MaimAndHinderImmune, isCleansing: true), () => Settings.HinderCount, () => Settings.RemHindered)
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

        private Boolean HasEnoughNearbyMonsters(int minimumMonsterCount, int maxDistance, bool countNormal, bool countRare, bool countMagic, bool countUnique, bool ignoreUniqueIfFullHealth)
        {
            var mobCount = 0;
            var maxDistanceSquare = maxDistance * maxDistance;

            var playerPosition = GameController.Player.Pos;

            if (LoadedMonsters != null)
            {
                List<Entity> localLoadedMonsters = null;
                lock (LoadedMonstersLock)
                {
                    localLoadedMonsters = new List<Entity>(LoadedMonsters);
                }

                // Make sure we create our own list to iterate as we may be adding/removing from the list
                foreach (var monster in localLoadedMonsters)
                {
                    if (!monster.HasComponent<Monster>() || !monster.IsValid || !monster.IsAlive || !monster.IsHostile)
                        continue;

                    var monsterType = monster.GetComponent<ObjectMagicProperties>()?.Rarity ?? null;
                    if (monsterType == null) continue;
                    // Don't count this monster type if we are ignoring it
                    if (monsterType == MonsterRarity.White && !countNormal
                        || monsterType == MonsterRarity.Rare && !countRare
                        || monsterType == MonsterRarity.Magic && !countMagic
                        || monsterType == MonsterRarity.Unique && !countUnique)
                        continue;

                    if (monsterType == MonsterRarity.Unique && ignoreUniqueIfFullHealth)
                    {
                        Life monsterLife = monster.GetComponent<Life>();
                        if (monsterLife == null)
                            continue;

                        if (monsterLife.HPPercentage > 0.995)
                            continue;
                    }

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
            }
            else if (Settings.Debug)
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
                        LogMessage(Name + ": No flasks to match against.", 5);
                    else if (Settings.FlaskSettings == null)
                        LogMessage(Name + ": Flask settings were null. Hopefully doesn't happen frequently.", 5);
                }

                return null;
            }

            if (Settings.Debug)
            {
                foreach (var flask in allFlasks)
                {
                    LogMessage($"{Name}: Flask: {flask.Name} Slot: {flask.Index} Instant: {flask.InstantType} Action1: {flask.Action1} Action2: {flask.Action2}", 5);
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
                    LogError(Name + ": No flasks found for action: (instant:" + instant + ") " + flaskActions[0], 1);
                return null;
            }

            if (Settings.Debug)
                LogMessage(Name + ": Flask(s) found for action: " + flaskActions[0] + " Flask Count: " + flaskList.Count(), 1);

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
                    || (playerFlask.InstantType == FlaskInstantType.LowLife && (PlayerHelper.isHealthBelowPercentage(50)
                                                                                || Settings.AllocatedSupremeDecadence && IsReallyLowLife()));
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

        private bool IsCycloning()
        {
            try
            {
                var buffs = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<ExileCore.PoEMemory.Components.Buffs>().BuffsList;

                foreach (var buff in buffs)
                    if (buff.Name.ToLower().Equals("cyclone_channelled_stage"))
                        return float.IsInfinity(buff.Timer);
            }
            catch
            {
                if (Settings.Debug)
                    LogError("BasicFlaskRoutine: Using Speed Flasks while Cycloning is enabled, but cannot get player buffs. Try to update PoeHUD.", 5);
            }

            return false;
        }

        // When life is reserved, IngameState.Data.LocalPlayer.GetComponent<Life>().HPPercentage returns an invalid value.
        private bool IsReallyLowLife()
        {
            var playerLife = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>();
            return (playerLife.CurHP / playerLife.MaxHP) * 100 < 50;
        }

        private Decorator CreateCurableDebuffDecorator(Dictionary<string, int> dictionary, Composite child, Func<int> minCharges = null, Func<bool> isEnabled = null)
        {
            return new Decorator((x =>
            {
                if (isEnabled != null && !isEnabled())
                {
                    return false;
                }

                var buffs = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<ExileCore.PoEMemory.Components.Buffs>().BuffsList;
                if (buffs == null) return false;
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

        public override void Render()
        {
            base.Render();
            if (!Settings.Enable.Value) return;
            if (Settings.BossingModeToggle && Settings.BossingModeHotkey.PressedOnce())
            {
                Settings.BossingMode = !Settings.BossingMode;
                if (Settings.BossingMode) LogMessage("BossingMode Activated! No automatic flasking until disabled.", 5, Color.Red);
                else LogMessage("BossingMode deactivated.", 5);
            }
        }

        public override void DrawSettings()
        {
            //base.DrawSettings();

            ImGuiTreeNodeFlags collapsingHeaderFlags = ImGuiTreeNodeFlags.CollapsingHeader;

            if (ImGui.TreeNodeEx("Plugin Options", collapsingHeaderFlags))
            {
                Settings.EnableInHideout.Value = ImGuiExtension.Checkbox("Enable in Hideout", Settings.EnableInHideout);
                ImGui.Separator();
                Settings.TicksPerSecond.Value = ImGuiExtension.IntSlider("Ticks Per Second", Settings.TicksPerSecond);
                ImGuiExtension.ToolTipWithText("(?)", "Determines how many times the plugin checks flasks every second.\nLower for less resources, raise for faster response (but higher chance to chug potions).");
                ImGui.Separator();
                Settings.Debug.Value = ImGuiExtension.Checkbox("Debug Mode", Settings.Debug);
                ImGui.Separator();
                Settings.BossingModeToggle.Value = ImGuiExtension.Checkbox("Disable Defensive and Offensive Flasking", Settings.BossingModeToggle);
                ImGui.Separator();
                Settings.BossingModeHotkey.Value = ImGuiExtension.HotkeySelector("BossingModeHotkey", Settings.BossingModeHotkey.Value);
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
                            currentFlask.UseWhenChargesFilled.Value = ImGuiExtension.Checkbox("Use when Charges Reach Full (instead of relying on enchant)", currentFlask.UseWhenChargesFilled);
                            currentFlask.Hotkey.Value = ImGuiExtension.HotkeySelector("Hotkey", currentFlask.Hotkey);
                            currentFlask.ReservedUses.Value = ImGuiExtension.IntSlider("Reserved Uses", currentFlask.ReservedUses);
                            ImGuiExtension.ToolTipWithText("(?)", "The absolute number of uses reserved on a flask.\nSet to 1 to always have 1 use of the flask available for manual use.");
                            ImGui.TreePop();
                        }
                    }

                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Health and Mana"))
                {
                    Settings.AutoFlask.Value = ImGuiExtension.Checkbox("Enable", Settings.AutoFlask);

                    ImGuiExtension.SpacedTextHeader("Settings");
                    Settings.ForceBubblingAsInstantOnly.Value =
                        ImGuiExtension.Checkbox("Force Bubbling as Instant only", Settings.ForceBubblingAsInstantOnly);
                    ImGuiExtension.ToolTipWithText("(?)",
                        "When enabled, flasks with the Bubbling mod will only be used as an instant flask.");
                    Settings.ForcePanickedAsInstantOnly.Value =
                        ImGuiExtension.Checkbox("Force Panicked as Instant only", Settings.ForcePanickedAsInstantOnly);
                    ImGuiExtension.ToolTipWithText("(?)",
                        "When enabled, flasks with the Panicked mod will only be used as an instant flask. \nNote, Panicked will not be used until under 35%% with this enabled."); //
                    ImGuiExtension.SpacedTextHeader("Health Flask");
                    Settings.HPPotion.Value = ImGuiExtension.IntSlider("Min Life % Auto HP Flask", Settings.HPPotion);
                    Settings.InstantHPPotion.Value =
                        ImGuiExtension.IntSlider("Min Life % Auto Instant HP Flask", Settings.InstantHPPotion);
                    Settings.AllocatedSupremeDecadence.Value =
                        ImGuiExtension.Checkbox("Enable use Life/Hybrid Flasks to restore Energy Shield", Settings.AllocatedSupremeDecadence);
                    ImGuiExtension.ToolTipWithText("(?)", "When enabled, Life-recovery flasks will also be used to recovery Energy Shield." +
                                                          "\nWarning: Life/Hybrid Flasks without Remove Ailments affix will not be used when on full life " +
                                                          "\n        (example: ~20 points non-Reserved Life is always equal full life) because it is game mechanics.");
                    Settings.ESPotion.Value =
                        ImGuiExtension.IntSlider("Min Energy Shield % Auto HP Flask", Settings.ESPotion);
                    Settings.InstantESPotion.Value =
                        ImGuiExtension.IntSlider("Min Energy Shield % Auto Instant HP Flask", Settings.InstantESPotion);
                    Settings.DisableLifeSecUse.Value =
                        ImGuiExtension.Checkbox("Disable Life/Hybrid Flask Offensive/Defensive Usage",
                            Settings.DisableLifeSecUse);

                    ImGuiExtension.SpacedTextHeader("Mana Flask");
                    ImGui.Spacing();
                    Settings.ManaPotion.Value =
                        ImGuiExtension.IntSlider("Min Mana % Auto Mana Flask", Settings.ManaPotion);
                    Settings.InstantManaPotion.Value = ImGuiExtension.IntSlider("Min Mana % Auto Instant MP Flask",
                        Settings.InstantManaPotion);
                    Settings.MinManaFlask.Value =
                        ImGuiExtension.IntSlider("Min Mana Auto Mana Flask", Settings.MinManaFlask);
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

                    Settings.RemPoison.Value = ImGuiExtension.Checkbox("Poisoned", Settings.RemPoison);

                    Settings.RemBleed.Value = ImGuiExtension.Checkbox("Bleed", Settings.RemBleed);
                    ImGui.SameLine();
                    Settings.RemCorruptingBlood.Value = ImGuiExtension.Checkbox("Corrupting Blood", Settings.RemCorruptingBlood);
                    Settings.CorruptCount.Value = ImGuiExtension.IntSlider("Corrupting Blood Stacks", Settings.CorruptCount);

                    Settings.RemMaimed.Value = ImGuiExtension.Checkbox("Maimed", Settings.RemMaimed);
                    ImGui.SameLine();
                    Settings.RemHindered.Value = ImGuiExtension.Checkbox("Hindered", Settings.RemHindered);
                    Settings.HinderCount.Value = ImGuiExtension.IntSlider("Hindered strength", Settings.HinderCount);

                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Speed Flasks"))
                {
                    Settings.SpeedFlaskEnable.Value = ImGuiExtension.Checkbox("Enable", Settings.SpeedFlaskEnable);

                    ImGuiExtension.SpacedTextHeader("Flasks");
                    Settings.QuicksilverFlaskEnable.Value =
                        ImGuiExtension.Checkbox("Quicksilver Flask", Settings.QuicksilverFlaskEnable);
                    Settings.SilverFlaskEnable.Value =
                        ImGuiExtension.Checkbox("Silver Flask", Settings.SilverFlaskEnable);

                    ImGuiExtension.SpacedTextHeader("Settings");
                    Settings.MinMsPlayerMoving.Value =
                        ImGuiExtension.IntSlider("Milliseconds Spent Moving", Settings.MinMsPlayerMoving);
                    ImGuiExtension.ToolTipWithText("(?)",
                        "Milliseconds spent moving before flask will be used.\n1000 milliseconds = 1 second");

                    ImGuiExtension.SpacedTextHeader("Cyclone");
                    Settings.UseWhileCycloning.Value =
                        ImGuiExtension.Checkbox("Using Speed Flasks while Cycloning", Settings.UseWhileCycloning);
                    Settings.CycloningMonsterCount.Value =
                        ImGuiExtension.IntSlider("Monster Count", Settings.CycloningMonsterCount);
                    ImGuiExtension.ToolTipWithText("(?)",
                        "Set to 0 to disable.");
                    Settings.CycloningMonsterDistance.Value =
                        ImGuiExtension.IntSlider("Monster Distance", Settings.CycloningMonsterDistance);
                    Settings.CycloningCountNormalMonsters.Value =
                        ImGuiExtension.Checkbox("Normal Monsters", Settings.CycloningCountNormalMonsters);
                    Settings.CycloningCountMagicMonsters.Value =
                        ImGuiExtension.Checkbox("Magic Monsters", Settings.CycloningCountMagicMonsters);
                    Settings.CycloningCountRareMonsters.Value =
                        ImGuiExtension.Checkbox("Rare Monsters", Settings.CycloningCountRareMonsters);
                    Settings.CycloningCountUniqueMonsters.Value =
                        ImGuiExtension.Checkbox("Unique Monsters", Settings.CycloningCountUniqueMonsters);
                    Settings.CycloningIgnoreFullHealthUniqueMonsters.Value =
                        ImGuiExtension.Checkbox("Ignore Full Health Unique Monsters", Settings.CycloningIgnoreFullHealthUniqueMonsters);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Defensive Flasks"))
                {
                    Settings.DefensiveFlaskEnable.Value =
                        ImGuiExtension.Checkbox("Enable", Settings.DefensiveFlaskEnable);
                    ImGui.Spacing();
                    ImGui.Separator();
                    Settings.HPPercentDefensive.Value =
                        ImGuiExtension.IntSlider("Min Life %", Settings.HPPercentDefensive);
                    Settings.ESPercentDefensive.Value =
                        ImGuiExtension.IntSlider("Min ES %", Settings.ESPercentDefensive);
                    Settings.OffensiveAsDefensiveEnable.Value =
                        ImGuiExtension.Checkbox("Use offensive flasks for defense",
                            Settings.OffensiveAsDefensiveEnable);
                    ImGui.Separator();

                    Settings.DefensiveMonsterCount.Value =
                        ImGuiExtension.IntSlider("Monster Count", Settings.DefensiveMonsterCount);
                    Settings.DefensiveMonsterDistance.Value =
                        ImGuiExtension.IntSlider("Monster Distance", Settings.DefensiveMonsterDistance);
                    Settings.DefensiveCountNormalMonsters.Value =
                        ImGuiExtension.Checkbox("Normal Monsters", Settings.DefensiveCountNormalMonsters);
                    Settings.DefensiveCountRareMonsters.Value =
                        ImGuiExtension.Checkbox("Rare Monsters", Settings.DefensiveCountRareMonsters);
                    Settings.DefensiveCountMagicMonsters.Value =
                        ImGuiExtension.Checkbox("Magic Monsters", Settings.DefensiveCountMagicMonsters);
                    Settings.DefensiveCountUniqueMonsters.Value =
                        ImGuiExtension.Checkbox("Unique Monsters", Settings.DefensiveCountUniqueMonsters);
                    Settings.DefensiveIgnoreFullHealthUniqueMonsters.Value =
                        ImGuiExtension.Checkbox("Ignore Full Health Unique Monsters", Settings.DefensiveIgnoreFullHealthUniqueMonsters);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Offensive Flasks"))
                {
                    Settings.OffensiveFlaskEnable.Value =
                        ImGuiExtension.Checkbox("Enable", Settings.OffensiveFlaskEnable);
                    ImGui.Spacing();
                    ImGui.Separator();
                    Settings.HPPercentOffensive.Value =
                        ImGuiExtension.IntSlider("Min Life %", Settings.HPPercentOffensive);
                    Settings.ESPercentOffensive.Value =
                        ImGuiExtension.IntSlider("Min ES %", Settings.ESPercentOffensive);
                    ImGui.Separator();
                    Settings.OffensiveMonsterCount.Value =
                        ImGuiExtension.IntSlider("Monster Count", Settings.OffensiveMonsterCount);
                    Settings.OffensiveMonsterDistance.Value =
                        ImGuiExtension.IntSlider("Monster Distance", Settings.OffensiveMonsterDistance);
                    Settings.OffensiveCountNormalMonsters.Value =
                        ImGuiExtension.Checkbox("Normal Monsters", Settings.OffensiveCountNormalMonsters);
                    Settings.OffensiveCountRareMonsters.Value =
                        ImGuiExtension.Checkbox("Rare Monsters", Settings.OffensiveCountRareMonsters);
                    Settings.OffensiveCountMagicMonsters.Value =
                        ImGuiExtension.Checkbox("Magic Monsters", Settings.OffensiveCountMagicMonsters);
                    Settings.OffensiveCountUniqueMonsters.Value =
                        ImGuiExtension.Checkbox("Unique Monsters", Settings.OffensiveCountUniqueMonsters);
                    Settings.OffensiveIgnoreFullHealthUniqueMonsters.Value =
                        ImGuiExtension.Checkbox("Ignore Full Health Unique Monsters", Settings.OffensiveIgnoreFullHealthUniqueMonsters);
                    ImGui.TreePop();
                }

                ImGui.TreePop();
            }
        }

        public override void EntityAdded(Entity entityWrapper)
        {
            if (entityWrapper.HasComponent<Monster>())
            {
                lock (LoadedMonstersLock)
                {
                    LoadedMonsters.Add(entityWrapper);
                }
            }
        }

        public override void EntityRemoved(Entity entityWrapper)
        {
            lock (LoadedMonstersLock)
            {
                LoadedMonsters.Remove(entityWrapper);
            }
        }
    }
}