using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WildBlueIndustries
{
    public class ModuleLESEngine: ModuleEnginesFX
    {
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Actions["ActivateAction"].actionGroup = KSPActionGroup.Abort;
        }
    }
}
