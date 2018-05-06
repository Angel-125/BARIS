﻿using System;
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
    /// ModuleBreakableHarvester is designed to replace ModuleResourceHarvester and interface ModuleQualityControl. When a part is declared broken, it disables the harvester, and when the part is declared fixed,
    /// it re-enables it again. Almost all of the functionality is internal; all the part config designer needs to do is make sure that ModuleBreakableHarvester appears before ModuleQualityControl.
    /// In addition to the MTBF checks done over time, a quality check occurs whenever the drill is started or stopped. It is part of the BARISBridge plugin.
    /// </summary>
    public class ModuleBreakableHarvester : ModuleResourceHarvester, ICanBreak
    {
        /// <summary>
        /// What skill to use when performing the quality check. This is not always the same skill required to repair or maintain the part.
        /// </summary>
        [KSPField()]
        public string qualityCheckSkill = "RepairSkill";

        /// <summary>
        /// Flag to indicate that the part module is broken. If broken, then it can't be declared broken again by the ModuleQualityControl.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isBroken;

        protected BaseQualityControl qualityControl;
        protected bool isMothballed;

        protected virtual void debugLog(string message)
        {
            if (BARISBridge.showDebug == true)
                Debug.Log("[" + this.ClassName + "] - " + message);
        }

        /// <summary>
        /// This method will start the converter. When starting, it will make a quality check. Use this method in place of StartResourceConverter.
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 5.0f, guiName = "Start Drill")]
        public virtual void StartConverter()
        {
            //If the drill is broken, then don't start the converter.
            if (isBroken)
            {
                if (this.part.vessel == FlightGlobals.ActiveVessel)
                {
                    string message = this.part.partInfo.title + BARISBridge.PartBrokenCannotStart;
                    BARISBridge.LogPlayerMessage(message);
                }
                StopResourceConverter();
                return;
            }

            //Update events
            Events["StartConverter"].guiActive = false;
            Events["StartConverter"].guiActiveUnfocused = false;

            //Start the converter
            StartResourceConverter();
            qualityControl.UpdateActivationState();
            if (BARISBridge.DrillsCanFail)
                qualityControl.PerformQualityCheck();
        }

        [KSPAction()]
        public void StartConverterAction(KSPActionParam param)
        {
            StartConverter();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Setup the events
            Events["StartConverter"].guiName = "Start " + ConverterName;
            Events["StartResourceConverter"].active = false;
            if (IsActivated)
            {
                Events["StartConverter"].guiActive = false;
                Events["StartConverter"].guiActiveUnfocused = false;
            }

            //Setup actions
            Actions["StartResourceConverterAction"].active = false;
            Actions["StartConverterAction"].guiName = StartActionName;
        }

        public virtual void Destroy()
        {
            qualityControl.onPartBroken -= OnPartBroken;
            qualityControl.onPartFixed -= OnPartFixed;
            qualityControl.onUpdateSettings -= onUpdateSettings;
            qualityControl.onMothballStateChanged -= onMothballStateChanged;
        }

        public void onMothballStateChanged(bool isMothballed)
        {
            this.isMothballed = isMothballed;
        }

        protected void onUpdateSettings(BaseQualityControl moduleQualityControl)
        {
            if (!BARISBridge.DrillsCanFail)
                isBroken = false;
        }

        #region ICanBreak
        public string GetCheckSkill()
        {
            return qualityCheckSkill;
        }

        public bool ModuleIsActivated()
        {
            if (!BARISBridge.CrewedPartsCanFail && this.part.CrewCapacity > 0)
                return false;
            if (!BARISBridge.CommandPodsCanFail && this.part.FindModuleImplementing<ModuleCommand>() != null)
                return false;

            if (!BARISBridge.PartsCanBreak || !BARISBridge.DrillsCanFail)
                return false;

            return IsActivated;
        }

        public void SubscribeToEvents(BaseQualityControl moduleQualityControl)
        {
            debugLog("SubscribeToEvents");
            qualityControl = moduleQualityControl;
            qualityControl.onPartBroken += OnPartBroken;
            qualityControl.onPartFixed += OnPartFixed;
            qualityControl.onUpdateSettings += onUpdateSettings;
            qualityControl.onMothballStateChanged += onMothballStateChanged;

            //Make sure we're broken
            if (isBroken)
                OnPartBroken(qualityControl);
        }

        public virtual void OnPartFixed(BaseQualityControl moduleQualityControl)
        {
            isBroken = false;
        }

        public virtual void OnPartBroken(BaseQualityControl moduleQualityControl)
        {
            isBroken = true;
            StopResourceConverter();

            if (this.part.vessel == FlightGlobals.ActiveVessel)
            {
                string message = Localizer.Format(this.part.partInfo.title + BARISBridge.DrillBroken);
                BARISBridge.LogPlayerMessage(message);
            }
            qualityControl.UpdateQualityDisplay(qualityControl.qualityDisplay + Localizer.Format(BARISBridge.DrillLabel));
        }
        #endregion

        public override void OnUpdate()
        {
            base.OnUpdate();

            //Always hide the start resource converter button
            Events["StartResourceConverter"].active = false;

            //Only need to do the stuff below if we're in flight.
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            if (!IsActivated && Events["StartConverter"].guiActive == false)
            {
                Events["StartConverter"].guiActive = true;
                Events["StartConverter"].guiActiveUnfocused = true;
            }
        }

        protected override void PostProcess(ConverterResults result, double deltaTime)
        {
            base.PostProcess(result, deltaTime);

            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            if (IsActivated == false)
                return;
            if (qualityControl == null)
                return;
            if (isBroken || isMothballed)
            {
                StopResourceConverter();
                Events["StartConverter"].active = false;
            }
        }
    }
}
