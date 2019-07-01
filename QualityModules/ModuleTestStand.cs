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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
using KSP.UI.Screens;
using KSP.Localization;
using UnityEngine;

namespace WildBlueIndustries
{
    public struct TestStandPart
    {
        public double highlightStartTime;
        public Part part;
    }

    /// <summary>
    /// This class represents a test stand from which various parts can be tested. The player simple manipulates the part's PAW/action groups and the test stand monitors the activity. Each time the part is manipulated,
    /// the test stand rolls against the part's quality. If there is a failure then there's a chance that the quality will be improved. There's also a chance that the vessel will explode. At first the chance for the vessel
    /// to explode is small, but it gradually increases.
    /// </summary>
    public class ModuleTestStand: PartModule
    {
        #region Constants
        #endregion

        #region Fields
        [KSPField(isPersistant = true, guiActive = true, guiName = "Test Stand")]
        [UI_Toggle(enabledText = "On", disabledText = "Off")]
        public bool isRunning;

        [KSPField(isPersistant = true)]
        public float vesselExplodeProbability;

        /// <summary>
        /// If a vessel fails to explode, increment the chance to explode by this amount.
        /// </summary>
        [KSPField]
        public float explodeProbabilityIncrement = 0.5f;

        /// <summary>
        /// Minimum die roll needed to improve part quality.
        /// </summary>
        [KSPField]
        public int minDieRoll = 1;

        /// <summary>
        /// Maximum die roll needed to improve part quality.
        /// </summary>
        [KSPField]
        public int maxDieRoll = 100;

        /// <summary>
        /// Target number needed to improve part quality.
        /// </summary>
        [KSPField]
        public int improveQualityTargetNumber = 99;

        /// <summary>
        /// How much to improve quality by during a successful improvement check.
        /// </summary>
        public int qualityImprovementAmount = 1;
        #endregion

        #region Housekeeping
        public bool isDirty;
        ModuleQualityControl[] qualityModules;
        Dictionary<ModuleQualityControl, TestStandPart> testStandParts = new Dictionary<ModuleQualityControl, TestStandPart>();
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            GameEvents.onStageActivate.Add(onStageActivate);
            GameEvents.onStageSeparation.Add(onStageSeparation);

            //Get the quality modules
            getQualityModules();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (isRunning)
            {
                //Check for cooldown timer expiration
                ModuleQualityControl[] keys = testStandParts.Keys.ToArray();
                TestStandPart testStandPart;
                double elapsedTime;
                ModuleQualityControl qualityControl;

                for (int index = 0; index < keys.Length; index++)
                {
                    qualityControl = keys[index];
                    testStandPart = testStandParts[qualityControl];
                    elapsedTime = Planetarium.GetUniversalTime() - testStandPart.highlightStartTime;

                    if (elapsedTime >= BARISScenario.highlightTimeInterval / 2.0f)
                    {
                        testStandPart.part.highlightColor = qualityControl.originalHighlightColor;
                        testStandPart.part.Highlight(false);
                        testStandParts.Remove(qualityControl);
                    }
                }
            }

            if (isDirty == isRunning)
                return;
            isDirty = isRunning;

            getQualityModules();

            //There can be only one active test stand monitoring events...
            ModuleTestStand[] testStands = this.part.vessel.FindPartModulesImplementing<ModuleTestStand>().ToArray();
            for (int index = 0; index < testStands.Length; index++)
            {
                if (testStands[index].part != this.part)
                {
                    testStands[index].isRunning = !isRunning;
                    testStands[index].isDirty = !isRunning;
                    testStands[index].Fields["isRunning"].guiActive = !isRunning;
                }
            }

            //Clear test stand parts
            if (!isRunning)
                testStandParts.Clear();
        }
        #endregion

        #region PAW Events
        #endregion

        #region Helpers
        protected void getQualityModules()
        {
            qualityModules = this.part.vessel.FindPartModulesImplementing<ModuleQualityControl>().ToArray();
            for (int index = 0; index < qualityModules.Length; index++)
            {
                if (qualityModules[index].part != this.part)
                {
                    qualityModules[index].onMTBFQualityCheck += onMTBFQualityCheck;
                    qualityModules[index].testStandMode = isRunning;
                }
            }
        }

        private void onStageActivate(int stageID)
        {
            getQualityModules();
        }

        private void onStageSeparation(EventReport report)
        {
            getQualityModules();
        }

        protected void onMTBFQualityCheck(ModuleQualityControl qualityControl, QualityCheckResult result)
        {
            if (!isRunning)
                return;

            if ((result.statusResult == QualityCheckStatus.criticalFail || result.statusResult == QualityCheckStatus.fail) && !testStandParts.ContainsKey(qualityControl))
            {
                TestStandPart testStandPart = new TestStandPart();
                testStandPart.part = qualityControl.part;
                testStandPart.highlightStartTime = Planetarium.GetUniversalTime();
                testStandParts.Add(qualityControl, testStandPart);

                onPartBroken(qualityControl);
            }
        }

        public void onPartBroken(BaseQualityControl moduleQualityControl)
        {
            if (!isRunning || moduleQualityControl.part == this.part)
                return;

            ModuleQualityControl qualityModule = null;
            if (moduleQualityControl is ModuleQualityControl)
                qualityModule = (ModuleQualityControl)moduleQualityControl;
            if (qualityModule == null)
                return;

            //Record original highlight color
            qualityModule.originalHighlightColor = qualityModule.part.highlightColor;

            //Set broken highlight color
            qualityModule.part.highlightColor = Color.red;
            qualityModule.part.Highlight(true);

            //Roll RNG to see if the part gains quality.
            int rollResult = UnityEngine.Random.Range(minDieRoll, maxDieRoll);
            string partTitle = string.Empty;
            int totalQuality = 0;
            string message = "";

            //If we've met our target number then improve part quality.
            if (rollResult >= improveQualityTargetNumber)
            {
                //Get the part title
                partTitle = qualityModule.part.partInfo.title;

                //Don't exceed max quality.
                totalQuality = qualityModule.quality + BARISScenario.Instance.GetFlightBonus(qualityModule.part);
                if (totalQuality + qualityImprovementAmount <= BARISBridge.QualityCap)
                {
                    //Record the flight experience.
                    BARISScenario.Instance.RecordFlightExperience(qualityModule.part, BARISBridge.FlightsPerQualityBonus * qualityImprovementAmount);

                    //Calculate the new quality rating.
                    totalQuality = qualityModule.quality + BARISScenario.Instance.GetFlightBonus(qualityModule.part);
                    message = partTitle + " " + BARISScenario.QualityLabel + totalQuality;

                    //Inform the user.
                    ScreenMessages.PostScreenMessage(message, BARISScenario.MessageDuration, ScreenMessageStyle.UPPER_CENTER);
                }

                else
                {
                    //Max quality reached.
                    message = partTitle + Localizer.Format(BARISScenario.kMaxQualityReached);

                    //Inform the user.
                    ScreenMessages.PostScreenMessage(message, BARISScenario.MessageDuration, ScreenMessageStyle.UPPER_CENTER);
                }
            }

            //Roll to see if the vessel explodes. If not then increase the chances slightly.
            float explodeResult = UnityEngine.Random.Range(0f, 100.0f);
            if (explodeResult >= (100.0f - vesselExplodeProbability))
            {
                for (int index = 0; index < qualityModules.Length; index++)
                    qualityModules[index].SetMarkedForDeath(true);
            }
            else
            {
                vesselExplodeProbability += explodeProbabilityIncrement;

                //Update quality
                qualityModule.quality = totalQuality;
                qualityModule.UpdateQualityDisplay(BARISScenario.GetConditionSummary(qualityModule.currentMTBF, qualityModule.MaxMTBF, qualityModule.currentQuality, qualityModule.MaxQuality));
            }
        }

        private void OnDestroy()
        {
            GameEvents.onStageActivate.Remove(onStageActivate);
            GameEvents.onStageSeparation.Remove(onStageSeparation);

            if (qualityModules != null)
            {
                for (int index = 0; index < qualityModules.Length; index++)
                {
                    qualityModules[index].onMTBFQualityCheck -= onMTBFQualityCheck;
                    qualityModules[index].onPartBroken -= onPartBroken;
                }
            }
        }
        #endregion
    }
}
