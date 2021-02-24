using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TreeRoutine.Menu;

namespace TreeRoutine.Routine.BuildYourOwnRoutine.Extension.Default.Conditions
{
    internal class CanUseSkillCondition : ExtensionCondition
    {
        private String SkillName { get; set; } = "";
        private const String skillNameString = "SkillName";


        public CanUseSkillCondition(string owner, string name) : base(owner, name)
        {

        }

        public override void Initialise(Dictionary<String, Object> Parameters)
        {
            base.Initialise(Parameters);

            SkillName = ExtensionComponent.InitialiseParameterString(skillNameString, SkillName, ref Parameters);
        }

        public override bool CreateConfigurationMenu(ExtensionParameter extensionParameter, ref Dictionary<String, Object> Parameters)
        {
            ImGui.TextDisabled("Condition Info");
            ImGuiExtension.ToolTipWithText("(?)", "This condition is used to determine if we can use a specific skill.\nSkill name can be obtained through DevTree by looking through the player's actor component, then actor skills list.");

            base.CreateConfigurationMenu(extensionParameter, ref Parameters);

            SkillName = ImGuiExtension.InputText("Skill Name", SkillName, 100, ImGuiInputTextFlags.AllowTabInput);
            Parameters[skillNameString] = SkillName.ToString();

            return true;
        }

        public override Func<bool> GetCondition(ExtensionParameter extensionParameter)
        {
            return () => extensionParameter.Plugin.PlayerHelper.CanUseSkill(SkillName);
        }

        public override string GetDisplayName(bool isAddingNew)
        {
            string displayName = "Can Use Skill";

            if (!isAddingNew)
            {
                displayName += " [";
                displayName += ("SkillName=" + SkillName.ToString());
                displayName += "]";

            }

            return displayName;
        }
    }
}
