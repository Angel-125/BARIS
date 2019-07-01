using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
using KSP.UI.Screens;
using ModuleWheels;
#if !KSP122
using KSP.Localization;
#endif

/*
Source code copyrighgt 2017-2019, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class ModuleRobonautExperimentManager : PartModule
    {
        ModuleScienceExperiment experiment = null;

        [KSPEvent(guiName = "Run Experiment", guiActiveUnfocused = true, guiActive = false, externalToEVAOnly = false, unfocusedRange = 4f)]
        public void RunExperiment()
        {
            if (experiment == null)
                return;

            //Make sure that the active vessel has a robonaut.
            if (FlightGlobals.ActiveVessel.FindPartModuleImplementing<ModuleRobonaut>() == null)
            {
                ScreenMessages.PostScreenMessage(ModuleRobonaut.NoRobonautMsg, ModuleRobonaut.MessageDuration, ScreenMessageStyle.UPPER_CENTER);
                FlightLogger.fetch.LogEvent(ModuleRobonaut.NoRobonautMsg);
                return;
            }

            bool experienceEnabled = HighLogic.CurrentGame.Parameters.CustomParams<GameParameters.AdvancedParams>().EnableKerbalExperience;
            HighLogic.CurrentGame.Parameters.CustomParams<GameParameters.AdvancedParams>().EnableKerbalExperience = false;
            experiment.DeployExperimentExternal();
            HighLogic.CurrentGame.Parameters.CustomParams<GameParameters.AdvancedParams>().EnableKerbalExperience = experienceEnabled;
        }

        [KSPEvent(guiName = "Collect Result Data", guiActiveUnfocused = true, guiActive = false, externalToEVAOnly = false, unfocusedRange = 4f)]
        public void CollectData()
        {
            if (experiment == null)
                return;

            //Make sure that the active vessel has a robonaut.
            if (FlightGlobals.ActiveVessel.FindPartModuleImplementing<ModuleRobonaut>() == null)
            {
                ScreenMessages.PostScreenMessage(ModuleRobonaut.NoRobonautMsg, ModuleRobonaut.MessageDuration, ScreenMessageStyle.UPPER_CENTER);
                FlightLogger.fetch.LogEvent(ModuleRobonaut.NoRobonautMsg);
                return;
            }

            //Make sure we have a container
            ModuleScienceContainer scienceContainer = this.part.FindModuleImplementing<ModuleScienceContainer>();
            if (scienceContainer == null)
            {
                Debug.Log("[ModuleRobonautCleanResetExperiment] - ModuleScienceContainer not found.");
                return;
            }

            bool experienceEnabled = HighLogic.CurrentGame.Parameters.CustomParams<GameParameters.AdvancedParams>().EnableKerbalExperience;
            HighLogic.CurrentGame.Parameters.CustomParams<GameParameters.AdvancedParams>().EnableKerbalExperience = false;
            experiment.CollectDataExternalEvent();
            HighLogic.CurrentGame.Parameters.CustomParams<GameParameters.AdvancedParams>().EnableKerbalExperience = experienceEnabled;
        }

        [KSPEvent(guiName = "#autoLOC_900305", guiActiveUnfocused = true, guiActive = false, externalToEVAOnly = false, unfocusedRange = 4f)]
        public void ResetExperiment()
        {
            if (experiment == null)
                return;

            //Make sure that the active vessel has a robonaut.
            if (FlightGlobals.ActiveVessel.FindPartModuleImplementing<ModuleRobonaut>() == null)
            {
                ScreenMessages.PostScreenMessage(ModuleRobonaut.NoRobonautMsg, ModuleRobonaut.MessageDuration, ScreenMessageStyle.UPPER_CENTER);
                FlightLogger.fetch.LogEvent(ModuleRobonaut.NoRobonautMsg);
                return;
            }

            bool experienceEnabled = HighLogic.CurrentGame.Parameters.CustomParams<GameParameters.AdvancedParams>().EnableKerbalExperience;
            HighLogic.CurrentGame.Parameters.CustomParams<GameParameters.AdvancedParams>().EnableKerbalExperience = false;
            experiment.ResetExperiment();
            HighLogic.CurrentGame.Parameters.CustomParams<GameParameters.AdvancedParams>().EnableKerbalExperience = experienceEnabled;
        }

        public override void OnStart(StartState state)
        {
            Debug.Log("[ModuleRobonautExperimentManager] - OnStart");
            base.OnStart(state);
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            experiment = this.part.FindModuleImplementing<ModuleScienceExperiment>();
            if (experiment == null)
                return;

            Events["ResetExperiment"].guiName = experiment.resetActionName;
            Events["CollectData"].guiName = experiment.collectActionName;
            Events["RunExperiment"].guiName = experiment.experimentActionName;
        }

        public override void OnUpdate()
        {
            Debug.Log("[ModuleRobonautExperimentManager] - OnUpdate");
            base.OnUpdate();
            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            Debug.Log("OnUpdate: " + this.part.partInfo.title);
            if (experiment == null)
                experiment = this.part.FindModuleImplementing<ModuleScienceExperiment>();
            if (experiment == null)
                return;

            Debug.Log("resettable: " + experiment.resettable);
            Debug.Log("dataIsCollectable: " + experiment.dataIsCollectable);
            Debug.Log("Deployed: " + experiment.Deployed);

            //Reset experiment
            Events["ResetExperiment"].active = experiment.Deployed && experiment.resettable && !FlightGlobals.ActiveVessel.isEVA;

            //Collect data
            Events["CollectData"].active = experiment.Deployed && experiment.dataIsCollectable && !FlightGlobals.ActiveVessel.isEVA;

            //Run experiment
            Events["RunExperiment"].active = !experiment.Deployed && !FlightGlobals.ActiveVessel.isEVA;
        }
    }
}
