using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeRoutine.Routine.BuildYourOwnRoutine.Extension.Default.Conditions
{
    internal class CanUseSkillConditionFactory : ExtensionConditionFactory
    {
        public CanUseSkillConditionFactory(string owner)
        {
            Owner = owner;
            Name = "CanUseSkillConditionFactory";
        }

        public override ExtensionCondition GetCondition()
        {
            return new CanUseSkillCondition(Owner, Name);
        }

        public override List<string> GetFilterTypes()
        {
            return new List<string>() { ExtensionComponentFilterType.Player };
        }
    }
}
