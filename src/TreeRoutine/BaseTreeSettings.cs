using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Attributes;
using Shared.Interfaces;
using Shared.Nodes;


namespace TreeRoutine
{
    public class BaseTreeSettings : ISettings
    {

        public BaseTreeSettings()
        {
            Enable = new ToggleNode(false);
            Debug = new ToggleNode(false);
        }

        [Menu("Enable")]
        public ToggleNode Enable { get; set; } 

        [Menu("Debug")]
        public ToggleNode Debug { get; set; }
    }
}
