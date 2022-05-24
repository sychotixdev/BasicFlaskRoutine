﻿using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared.Enums;

namespace TreeRoutine.DefaultBehaviors.Helpers
{
    public class PlayerHelper<TSettings, TCache>
        where TSettings : BaseTreeSettings, new()
        where TCache : BaseTreeCache, new()
    {
        public BaseTreeRoutinePlugin<TSettings, TCache> Core { get; set; }

        public Boolean isHealthBelowPercentage(int healthPercentage)
        {
            var playerLife = Core.GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>();
            return playerLife.HPPercentage * 100 < healthPercentage;
        }

        public Boolean isHealthBelowValue(int healthValue)
        {
            var playerLife = Core.GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>();
            return playerLife.CurHP < healthValue;
        }

        public Boolean isManaBelowPercentage(int manaPercentage)
        {
            var playerLife = Core.GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>();
            return playerLife.MPPercentage * 100 < manaPercentage;
        }

        public Boolean isManaBelowValue(int manaValue)
        {
            var playerLife = Core.GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>();
            return playerLife.CurMana < manaValue;
        }

        public Boolean isEnergyShieldBelowPercentage(int energyShieldPercentage)
        {
            var playerLife = Core.GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>();
            return playerLife.MaxES > 0 && playerLife.ESPercentage * 100 < energyShieldPercentage;
        }

        public Boolean isEnergyShieldBelowValue(int energyShieldValue)
        {
            var playerLife = Core.GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>();
            return playerLife.MaxES > 0 && playerLife.CurMana < energyShieldValue;
        }

        public Boolean playerHasBuffs(List<String> buffs)
        {
            if (buffs == null || buffs.Count == 0)
                return false;

            var playerBuffs = Core.GameController.Game.IngameState.Data.LocalPlayer.GetComponent<ExileCore.PoEMemory.Components.Buffs>().BuffsList;

            if (playerBuffs == null)
                return false;

            foreach (var buff in buffs)
            {
                if (!String.IsNullOrEmpty(buff) && !playerBuffs.Any(x => !String.IsNullOrWhiteSpace(x.Name) && buff.StartsWith(x.Name)))
                {
                    return false;
                }
            }
            return true;
        }

        public int? getPlayerStat(string playerStat)
        {
            int statValue = 0;
            
            if (!Core.GameController.EntityListWrapper.Player.Stats.TryGetValue((GameStat)Core.GameController.Files.Stats.records[playerStat].ID, out statValue))
                return null;

            return statValue;
        }

        public Boolean playerDoesNotHaveAnyOfBuffs(List<String> buffs)
        {
            if (buffs == null || buffs.Count == 0)
                return true;

            var playerBuffs = Core.GameController.Game.IngameState.Data.LocalPlayer.GetComponent<ExileCore.PoEMemory.Components.Buffs>().BuffsList;

            if (playerBuffs == null)
                return true;

            foreach (var buff in buffs)
            {
                if (!String.IsNullOrEmpty(buff) && playerBuffs.Any(x => !String.IsNullOrWhiteSpace(x.Name) && buff.StartsWith(x.Name)))
                {
                    return false;
                }
            }
            return true;
        }

        public Boolean isPlayerDead()
        {
            var playerLife = Core.GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>();
            return playerLife.CurHP <= 0;
        }

        public Boolean CanUseSkill(String skillName)
        {
            var actorComponent = Core.GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Actor>();
            var actorSkills = actorComponent?.ActorSkills;
            if (actorSkills != null && actorSkills.Count > 0)
            {
                foreach(var actorSkill in actorSkills)
                {
                    if (skillName.Equals(actorSkill.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return actorSkill.CanBeUsed;
                    }
                }
            }

            return false;
        }

    }
}
