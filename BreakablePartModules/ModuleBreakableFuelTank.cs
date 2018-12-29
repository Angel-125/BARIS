using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
using KSP.UI.Screens;
#if !KSP122
using KSP.Localization;
#endif

/*
Source code copyrighgt 2017, by Michael Billard (Angel-125)
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
    /// <summary>
    /// ModuleBreakableFuelTank is designed to interface with ModuleQualityControl. When a part is declared broken, it leaks the resources contained in the part, and when the part is declared fixed,
    /// it fixes the leak. Almost all of the functionality is internal; all the part config designer needs to do is make sure that ModuleBreakableFuelTank appears before ModuleQualityControl.
    /// </summary>
    public class ModuleBreakableFuelTank : PartModule, ICanBreak
    {
        public static double smallLeakRate = 0.01f;
        public static double mediumLeakRate = 0.05f;
        public static double largeLeakRate = 0.2f;
        public static int smallLeakChance = 10;
        public static int mediumLeakChance = 90;

        ModuleQualityControl qualityControl;
        bool ignoreFlowStateChanges = false;

        [KSPField()]
        public string smallLeakMessage;

        [KSPField()]
        public string mediumLeakMessage;

        [KSPField()]
        public string largeLeakMessage;

        [KSPField]
        public string resourceBlacklist = "Ablator; SolidFuel;SRMFuel";

        /// <summary>
        /// What skill to use when performing the quality check. This is not always the same skill required to repair or maintain the part.
        /// </summary>
        [KSPField()]
        public string qualityCheckSkill = "RepairSkill";

        /// <summary>
        /// When the part is declared broken, this field indicates how many units of resources to leak per second. The more time the part is declared broken, the bigger the leak.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double leakedUnitsPerSec;

        #region Housekeeping
        protected void debugLog(string message)
        {
            if (BARISScenario.showDebug == true)
                Debug.Log("[" + this.ClassName + "] - " + message);
        }

        protected void onUpdateSettings(BaseQualityControl moduleQualityControl)
        {
            Events["CreateSmallLeak"].guiActive = BARISScenario.showDebug;
            Events["CreateMediumLeak"].guiActive = BARISScenario.showDebug;
            Events["CreateLargeLeak"].guiActive = BARISScenario.showDebug;
        }

        #region ICanBreak
        public string GetCheckSkill()
        {
            return qualityCheckSkill;
        }

        public bool ModuleIsActivated()
        {
            if (!BARISBreakableParts.CrewedPartsCanFail && this.part.CrewCapacity > 0)
                return false;
            if (!BARISBreakableParts.CommandPodsCanFail && this.part.FindModuleImplementing<ModuleCommand>() != null)
                return false;

            if (!BARISSettings.PartsCanBreak || !BARISBreakableParts.TanksCanFail)
                return false;

            for (int index = 0; index < this.part.Resources.Count; index++)
            {
                //Ignore any resource on our blacklist
                if (resourceBlacklist.Contains(this.part.Resources[index].resourceName))
                    continue;

                if (this.part.Resources[index].flowState)
                {
                    return true;
                }
            }

            return false;
        }

        public void SubscribeToEvents(BaseQualityControl moduleQualityControl)
        {
            debugLog("SubscribeToEvents");
            qualityControl = (ModuleQualityControl)moduleQualityControl;
            qualityControl.onUpdateSettings += onUpdateSettings;
            qualityControl.onPartBroken += OnPartBroken;
            qualityControl.onPartFixed += OnPartFixed;

            if (BARISScenario.showDebug && qualityControl.guiVisible)
            {
                Events["CreateSmallLeak"].guiActive = true;
                Events["CreateMediumLeak"].guiActive = true;
                Events["CreateLargeLeak"].guiActive = true;
            }
        }

        public void OnPartBroken(BaseQualityControl moduleQualityControl)
        {
            if (!BARISSettings.PartsCanBreak || !BARISBreakableParts.TanksCanFail)
                return;

            //If we're out of MTBF and we suffered a critical failure, then dump resources or blow up the part.
            if (qualityControl.lastQualityCheck.statusResult == QualityCheckStatus.criticalFail)
            {
                DumpResources();
            }

            //Generate a resource leak.
            else
            {
                leakedUnitsPerSec += CalculateLeakRate();
                BARISScenario.Instance.onTimeTickEvent += leakTimeTick;
            }
        }

        public void OnPartFixed(BaseQualityControl moduleQualityControl)
        {
            //Unsubscribe from the time tick event.
            BARISScenario.Instance.onTimeTickEvent -= leakTimeTick;
            leakedUnitsPerSec = 0f;
        }
        #endregion

        protected void leakTimeTick()
        {
            LeakResources();
        }
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Persistent fuel leaks
            if (leakedUnitsPerSec > 0)
                BARISScenario.Instance.onTimeTickEvent += leakTimeTick;

            GameEvents.onPartResourceFlowStateChange.Add(onFlowStateChanged);
//            GameEvents.onPartResourceNonemptyEmpty.Add(onPartResourceNonemptyEmpty);
//            GameEvents.onPartResourceNonemptyFull.Add(onPartResourceNonemptyFull);
        }

        public void Destroy()
        {
//            GameEvents.onPartResourceNonemptyFull.Remove(onPartResourceNonemptyFull);
//            GameEvents.onPartResourceNonemptyEmpty.Remove(onPartResourceNonemptyEmpty);
            GameEvents.onPartResourceFlowStateChange.Remove(onFlowStateChanged);
            qualityControl.onUpdateSettings -= onUpdateSettings;
            qualityControl.onPartBroken -= OnPartBroken;
            qualityControl.onPartFixed -= OnPartFixed;
        }
        #endregion

        #region API
        /// <summary>
        /// Toggles the flow state of all resources in the part.
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 5.0f, guiName = "Toggle Resource Locks")]
        public void ToggleResourceLocks()
        {
            ignoreFlowStateChanges = true;

            for (int index = 0; index < this.part.Resources.Count; index++)
                this.part.Resources[index].flowState = !this.part.Resources[index].flowState;

            ignoreFlowStateChanges = false;

            qualityControl.UpdateActivationState();
            qualityControl.PerformQualityCheck();
        }

        [KSPAction(guiName = "Toggle Resource Locks")]
        public void ToggleResourceLocksAction(KSPActionParam param)
        {
            ToggleResourceLocks();
        }

        /// <summary>
        /// This diagnostic method will create a small leak in the fuel tank.
        /// </summary>
        [KSPEvent(guiName = "Create Small Leak")]
        public void CreateSmallLeak()
        {
            if (this.part.vessel == FlightGlobals.ActiveVessel)
            {
                string message = Localizer.Format(this.part.partInfo.title + BARISScenario.SmallLeak);
                if (!string.IsNullOrEmpty(smallLeakMessage))
                    message = Localizer.Format(this.part.partInfo.title + smallLeakMessage);
                BARISScenario.Instance.LogPlayerMessage(message);
            }
            leakedUnitsPerSec += smallLeakRate;
            qualityControl.UpdateQualityDisplay(qualityControl.qualityDisplay + Localizer.Format(BARISScenario.SmallLeakLabel));
        }

        /// <summary>
        /// This diagnostic method will create a medium leak in the fuel tank.
        /// </summary>
        [KSPEvent(guiName = "Create Medium Leak")]
        public void CreateMediumLeak()
        {
            if (this.part.vessel == FlightGlobals.ActiveVessel)
            {
                string message = Localizer.Format(this.part.partInfo.title + BARISScenario.MediumLeak);
                if (!string.IsNullOrEmpty(mediumLeakMessage))
                    message = Localizer.Format(this.part.partInfo.title + mediumLeakMessage);
                BARISScenario.Instance.LogPlayerMessage(message);
            }
            leakedUnitsPerSec += mediumLeakRate;
            qualityControl.UpdateQualityDisplay(qualityControl.qualityDisplay + Localizer.Format(BARISScenario.MediumLeakLabel));
        }

        /// <summary>
        /// This diagnostic method will create a large leak in the fuel tank.
        /// </summary>
        [KSPEvent(guiName = "Create Large Leak")]
        public void CreateLargeLeak()
        {
            if (this.part.vessel == FlightGlobals.ActiveVessel)
            {
                string message = Localizer.Format(this.part.partInfo.title + BARISScenario.LargeLeak);
                if (!string.IsNullOrEmpty(largeLeakMessage))
                    message = Localizer.Format(this.part.partInfo.title + largeLeakMessage);
                BARISScenario.Instance.LogPlayerMessage(message);
            }
            leakedUnitsPerSec += largeLeakRate;
            qualityControl.UpdateQualityDisplay(qualityControl.qualityDisplay + Localizer.Format(BARISScenario.LargeLeakLabel));
        }

        /// <summary>
        /// Calculates a random fuel leak rate. The rate is a fixed amount depending upon whether the leak is a small, medium, or large hole.
        /// </summary>
        /// <returns>A double containing the number of units per second that will be drained away from all resources in the tank.</returns>
        public double CalculateLeakRate()
        {
            int analysisRoll = UnityEngine.Random.Range(1, 100);
            double leakRate = 0;

            if (analysisRoll <= smallLeakChance)
            {
                if (this.part.vessel == FlightGlobals.ActiveVessel)
                {
                    string message = Localizer.Format(this.part.partInfo.title + BARISScenario.SmallLeak);
                    BARISScenario.Instance.LogPlayerMessage(message);
                }
                leakRate += smallLeakRate;
                qualityControl.UpdateQualityDisplay(qualityControl.qualityDisplay + Localizer.Format(BARISScenario.SmallLeakLabel));
            }
            else if (analysisRoll <= mediumLeakChance)
            {
                if (this.part.vessel == FlightGlobals.ActiveVessel)
                {
                    string message = Localizer.Format(this.part.partInfo.title + BARISScenario.MediumLeak);
                    BARISScenario.Instance.LogPlayerMessage(message);
                }
                leakRate += mediumLeakRate;
                qualityControl.UpdateQualityDisplay(qualityControl.qualityDisplay + Localizer.Format(BARISScenario.MediumLeakLabel));
            }
            else
            {
                if (this.part.vessel == FlightGlobals.ActiveVessel)
                {
                    string message = Localizer.Format(this.part.partInfo.title + BARISScenario.LargeLeak);
                    BARISScenario.Instance.LogPlayerMessage(message);
                }
                leakRate += largeLeakRate;
                qualityControl.UpdateQualityDisplay(qualityControl.qualityDisplay + Localizer.Format(BARISScenario.LargeLeakLabel));
            }

            debugLog("Resource leak rate: " + leakRate);
            return leakRate;
        }

        /// <summary>
        /// This method will completely drain away all resources in the tank. This can occur during a critical failure of the tank's quality check. It is made a public method for diagnostic purposes.
        /// </summary>
        public void DumpResources()
        {
            int totalResources = this.part.Resources.Count;
            if (totalResources == 0)
                return;

            PartResource[] leakingResources = this.part.Resources.ToArray();
            for (int index = 0; index < totalResources; index++)
                leakingResources[index].amount = 0;

            if (this.part.vessel == FlightGlobals.ActiveVessel)
            {
                string message = Localizer.Format(this.part.partInfo.title + BARISScenario.ResourcesDumped);
                BARISScenario.Instance.LogPlayerMessage(message);
            }
            debugLog(this.part.partInfo.title + " dumped resources");
        }

        /// <summary>
        /// This method will drain a bit of the tank's resources. It is typically called once per fixed update, but it's also provided as a public method for diagnostic purposes.
        /// </summary>
        public void LeakResources()
        {
            int totalResources = this.part.Resources.Count;
            if (totalResources == 0)
                return;
            double unitsPerUpdate = TimeWarp.fixedDeltaTime * leakedUnitsPerSec;

            //Go through all the resources and remove some until depleted.
            PartResource[] leakingResources = this.part.Resources.ToArray();
            for (int index = 0; index < totalResources; index++)
            {
                if (leakingResources[index].amount > 0)
                {
                    leakingResources[index].amount -= unitsPerUpdate;
                    if (leakingResources[index].amount <= 0)
                        leakingResources[index].amount = 0;
                }
            }
        }
        #endregion

        #region Helpers
        protected void onPartResourceNonemptyFull(PartResource resource)
        {
            if (resource.part != this.part)
                return;
            qualityControl.PerformQualityCheck();
        }

        protected void onPartResourceNonemptyEmpty(PartResource resource)
        {
            if (resource.part != this.part)
                return;
            qualityControl.PerformQualityCheck();
        }

        protected void onFlowStateChanged(GameEvents.HostedFromToAction<PartResource, bool> evnt)
        {
            if (ignoreFlowStateChanges)
                return;
            if (evnt.host.part != this.part)
                return;

            qualityControl.UpdateActivationState();
            qualityControl.PerformQualityCheck();
        }
        #endregion
    }
}
