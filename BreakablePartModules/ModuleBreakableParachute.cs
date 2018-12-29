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
    public class ModuleBreakableParachute : ModuleParachute, ICanBreak
    {
        /// <summary>
        /// What skill to use when performing the quality check. This is not always the same skill required to repair or maintain the part.
        /// </summary>
        [KSPField()]
        public string qualityCheckSkill = "RepairSkill";

        [KSPField(isPersistant = true)]
        public bool isBroken;

        public bool isStaged = false;

        ModuleQualityControl qualityControl;
        bool partsCanBreak = true;
        bool isMothballed;

        public void Destroy()
        {
           //qualityControl.onUpdateSettings -= onUpdateSettings;
            qualityControl.onPartBroken -= OnPartBroken;
            qualityControl.onPartFixed -= OnPartFixed;
        }

        #region
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

            return true;
        }

        public void SubscribeToEvents(BaseQualityControl moduleQualityControl)
        {
            qualityControl = (ModuleQualityControl)moduleQualityControl;
            //qualityControl.onUpdateSettings += onUpdateSettings;
            qualityControl.onPartBroken += OnPartBroken;
            qualityControl.onPartFixed += OnPartFixed;
            qualityControl.onMothballStateChanged += onMothballStateChanged;

            if (BARISScenario.showDebug && qualityControl.guiVisible)
            {
            }
        }

        protected void onUpdateSettings(BaseQualityControl moduleQualityControl)
        {
            partsCanBreak = BARISScenario.partsCanBreak;
        }

        public void onMothballStateChanged(bool isMothballed)
        {
            this.isMothballed = isMothballed;
        }

        public void OnPartFixed(BaseQualityControl moduleQualityControl)
        {
            isBroken = false;
        }

        public void OnPartBroken(BaseQualityControl moduleQualityControl)
        {
            if (!BARISSettings.PartsCanBreak || !BARISBreakableParts.EnginesCanFail)
                return;

            //Record state
            isBroken = true;

            //If the chute is deployed then cut the chute
            string message = string.Empty;
            string qualityDisplay = string.Empty;
            if (deploymentState == deploymentStates.DEPLOYED || deploymentState == deploymentStates.SEMIDEPLOYED)
            {
                CutParachute();

                //Player messages
                message = this.part.partInfo.title + " chute cut!";
                qualityDisplay = "Broken";
            }
            else
            {
                //The chute cannot deploy
                message = this.part.partInfo.title + " cannot deploy chute!";
                qualityDisplay = " stuck";
;            }

            //Inform player
            qualityControl.UpdateQualityDisplay(qualityControl.qualityDisplay + qualityDisplay);
            if (this.part.vessel == FlightGlobals.ActiveVessel)
            {
                BARISScenario.Instance.LogPlayerMessage(message);
            }
        }
        #endregion

        protected override bool PassedAdditionalDeploymentChecks()
        {
            if (isBroken)
                return false;
            if (this.qualityControl == null)
                return base.PassedAdditionalDeploymentChecks();
            if (!BARISBreakableParts.ParachutesCanFail)
                return base.PassedAdditionalDeploymentChecks();

            //Make quality check
            this.qualityControl.PerformQualityCheck();
            if ((this.qualityControl.lastQualityCheck.statusResult == QualityCheckStatus.fail ||
                this.qualityControl.lastQualityCheck.statusResult == QualityCheckStatus.criticalFail) &&
                isStaged)
            {
                this.isBroken = true;
                this.qualityControl.DeclarePartBroken();
                return false;
            }
            if (isBroken)
                return false;

            return base.PassedAdditionalDeploymentChecks();
        }
    }
}
