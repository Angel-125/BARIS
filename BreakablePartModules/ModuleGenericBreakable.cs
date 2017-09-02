using System;
using System.Collections.Generic;
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
    /// This is a generic class to make it a bit easier to have breakable parts. 
    /// It's functionality is a bit limited compared to dedicated breakable part modules but it handles a lot of the housekeeping chores for you.
    /// In a config file, ModuleGenericBreakable appears after the part modules it controls, but before ModuleQualityControl.
    /// It should look like: (modules to control) ModuleGenericBreakable ModuleQualityControl
    /// In addition to the MTBF checks done over time, a quality check can occur whenever RCS is activated, SAS is activated, or the throttle is set to zero or moved away from zero.
    /// Which checks to perform depend upon how ModuleGenericBreakable is set up.
    /// </summary>
    public class ModuleGenericBreakable : PartModule, ICanBreak
    {
        /// <summary>
        /// What skill to use when performing the quality check. This is not always the same skill required to repair or maintain the part.
        /// </summary>
        [KSPField()]
        public string qualityCheckSkill = "RepairSkill";

        /// <summary>
        /// Perform quality checks if RCS is toggled on/off.
        /// </summary>
        [KSPField()]
        public bool checkQualityDuringRCSToggle;

        /// <summary>
        /// Perform quality checks if SAS is toggled on/off.
        /// </summary>
        [KSPField()]
        public bool checkQualityDuringSASToggle;

        /// <summary>
        /// Perform quality checks if the throttle is throttled up from zero or down to zero.
        /// </summary>
        [KSPField()]
        public bool checkQualityDuringThrottling;

        /// <summary>
        /// List of part modules to disable during a quality check failure Separate multiple part modules using a semicolon.
        /// </summary>
        public string modulesToDisable = string.Empty;

        /// <summary>
        /// Flag to indicate that part will explode upon critical failure.
        /// </summary>
        public bool explodeOnCriticalFailure = false;

        /// <summary>
        /// During a quality check (1-100), make the part explode if the quality roll is at or below this target number. 
        /// Note: this will override explodeOnCriticalFailure
        /// </summary>
        [KSPField()]
        public int explodeTargetNumber = -1;

        /// <summary>
        /// Flag to indicate that RCS is active
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool RCSIsActive;

        /// <summary>
        /// Flag to indicate that SAS is active
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool SASIsActive;

        /// <summary>
        /// Field to indicate that the part is broken. Used to disable the module after loading a saved game.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isBroken;

        protected ModuleQualityControl qualityControl;
        protected List<PartModule> breakablePartModules = new List<PartModule>();

        protected void debugLog(string message)
        {
            if (BARISScenario.showDebug == true)
                Debug.Log("[" + this.ClassName + "] - " + message);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Get the list of part modules that we'll enable/disable when the part is broken/fixed.
            if (string.IsNullOrEmpty(modulesToDisable))
                return;

            //Go through our part modules and find the ones that we can enable/disable.
            foreach (PartModule partModule in this.part.Modules)
            {
                if (modulesToDisable.Contains(partModule.moduleName))
                {
                    breakablePartModules.Add(partModule);
                }
            }
        }

        public void Destroy()
        {
            qualityControl.onUpdateSettings -= onUpdateSettings;
            qualityControl.onPartBroken -= OnPartBroken;
            qualityControl.onPartFixed -= OnPartFixed;

            if (checkQualityDuringRCSToggle)
                BARISScenario.Instance.onSasUpdate -= onRCSUpdate;
            if (checkQualityDuringSASToggle)
                BARISScenario.Instance.onSasUpdate -= onSASUpdate;
            if (checkQualityDuringThrottling)
                BARISScenario.Instance.onThrottleUpDown -= onThrottleUpdate;
        }

        protected void onThrottleUpdate(bool isThrottledUp)
        {
            if (isBroken)
                return;
            qualityControl.PerformQualityCheck();
        }

        protected void onSASUpdate(bool sasActive)
        {
            if (isBroken)
            {
                SASIsActive = false;
                return;
            }
            SASIsActive = sasActive;
            qualityControl.PerformQualityCheck();
        }

        protected void onRCSUpdate(bool rcsActive)
        {
            if (isBroken)
            {
                RCSIsActive = false;
                return;
            }
            RCSIsActive = rcsActive;
            qualityControl.PerformQualityCheck();
        }

        #region ICanBreak
        public string GetCheckSkill()
        {
            return qualityCheckSkill;
        }

        public bool ModuleIsActivated()
        {
            if (!BARISSettings.PartsCanBreak || (!BARISBreakableParts.RCSCanFail && !BARISBreakableParts.SASCanFail))
                return false;

            return RCSIsActive || SASIsActive;
        }

        public void SubscribeToEvents(BaseQualityControl moduleQualityControl)
        {
            debugLog("SubscribeToEvents");
            qualityControl = (ModuleQualityControl)moduleQualityControl;
            qualityControl.onUpdateSettings += onUpdateSettings;
            qualityControl.onPartBroken += OnPartBroken;
            qualityControl.onPartFixed += OnPartFixed;

            //Handle persistence case for broken part.
            if (isBroken)
                OnPartBroken(qualityControl);
        }

        protected void onUpdateSettings(BaseQualityControl moduleQualityControl)
        {
            //Quality check events
            if (BARISSettings.PartsCanBreak && BARISBreakableParts.RCSCanFail)
            {
                if (checkQualityDuringRCSToggle)
                    BARISScenario.Instance.onRcsUpdate += onRCSUpdate;
                if (checkQualityDuringSASToggle)
                    BARISScenario.Instance.onSasUpdate += onSASUpdate;
                if (checkQualityDuringThrottling)
                    BARISScenario.Instance.onThrottleUpDown += onThrottleUpdate;
            }
            else
            {
                if (checkQualityDuringRCSToggle)
                    BARISScenario.Instance.onSasUpdate -= onRCSUpdate;
                if (checkQualityDuringSASToggle)
                    BARISScenario.Instance.onSasUpdate -= onSASUpdate;
                if (checkQualityDuringThrottling)
                    BARISScenario.Instance.onThrottleUpDown -= onThrottleUpdate;
            }
        }

        public void OnPartBroken(BaseQualityControl moduleQualityControl)
        {
            if (!BARISSettings.PartsCanBreak && !BARISBreakableParts.RCSCanFail)
                return;

            isBroken = true;

            //Cause the part to explode if we've fallen below the target number.
            if (explodeTargetNumber > -1)
            {
                if (qualityControl.lastQualityCheck.resultRoll <= explodeTargetNumber)
                {
                    //Send a message
                    if (this.part.vessel == FlightGlobals.ActiveVessel)
                    {
                        string message = Localizer.Format(this.part.partInfo.title + BARISScenario.CascadeFailureMsg);
                        BARISScenario.Instance.LogPlayerMessage(message);
                    }

                    this.part.explode();
                    return;
                }
            }

            //If we've suffered a critical failure and are supposed to explode, then do so.
            if (explodeOnCriticalFailure && qualityControl.lastQualityCheck.statusResult == QualityCheckStatus.criticalFail)
            {
                //Send a message
                if (this.part.vessel == FlightGlobals.ActiveVessel)
                {
                    string message = Localizer.Format(this.part.partInfo.title + BARISScenario.CascadeFailureMsg);
                    BARISScenario.Instance.LogPlayerMessage(message);
                }

                this.part.explode();
                return;
            }

            //Disable all the part modules we're supposed to.
            foreach (PartModule partModule in breakablePartModules)
            {
                partModule.enabled = false;
                partModule.isEnabled = false;
                partModule.moduleIsEnabled = false;
            }

        }

        public void OnPartFixed(BaseQualityControl moduleQualityControl)
        {
            isBroken = true;

            //Enable all the part modules we're supposed to.
            foreach (PartModule partModule in breakablePartModules)
            {
                partModule.enabled = true;
                partModule.isEnabled = true;
                partModule.moduleIsEnabled = true;
            }
        }
        #endregion

    }
}
