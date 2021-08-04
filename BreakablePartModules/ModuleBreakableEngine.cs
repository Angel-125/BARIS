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
    //SSME has an MTBF of about 20 hours of continual thrust
    /// <summary>
    /// ModuleBreakableEngine is designed to interface between ModuleEngines/ModuleEnginesFX and ModuleQualityControl. It handles the results of a quality check, such as generating a failure mode
    /// for the engine, and restoring proper engine functionality when the part is repaired. In the config file, ModuleBreakableEngine must appear after ModuleEngines/ModuleEnginesFX and before
    /// ModuleQualityControl. Most of the functionality is handled internally, but there are some exposed properties and methods that are available for diagnostic purposes.
    /// In addition to regular MTBF checks done over time, the engine performs a quality check when the throttle is set to zero or moved away from zero.
    /// </summary>
    public class ModuleBreakableEngine : PartModule, ICanBreak
    {
        static int explodeTarget = 95;
        static int stuckOnTarget = 20;

        /// <summary>
        /// What skill to use when performing the quality check. This is not always the same skill required to repair or maintain the part.
        /// </summary>
        [KSPField()]
        public string qualityCheckSkill = "RepairSkill";

        /// <summary>
        /// In a low thrust failure mode, this field stores what percentage of thrust the engine should be.
        /// </summary>
        [KSPField(isPersistant = true)]
        public float failureThrustPercent;

        /// <summary>
        /// This flag indicates that the engine is stuck in the on position and can't be shut off.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isStuckOn;

        /// <summary>
        /// This is the ID code of the failure mode.
        /// </summary>
        [KSPField(isPersistant = true)]
        public int failureModeID;

        /// <summary>
        /// This flag indicates whether or not the engine was running during the previous check.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool wasRunning;

        protected ModuleEngines engine;
        protected MultiModeEngine engineSwitcher;
        protected Dictionary<string, ModuleEngines> multiModeEngines = new Dictionary<string, ModuleEngines>();
        BARRISEngineFailureModes failureMode;
        bool[] allowShutdown;
        bool[] throttleLocked;
        ModuleQualityControl qualityControl;
        bool partsCanBreak = true;
        bool isMothballed;

        #region API

        /// <summary>
        /// This diagnostic method is used to generate a random engine failure regardless of the part's current quality rating.
        /// </summary>
        [KSPEvent(guiName = "Generate Failure Mode")]
        public void GenerateFailureMode()
        {
            if (failureMode != BARRISEngineFailureModes.None)
                return;

            //There are three possible engine failures: shutdown, stuck on, explode.
            int resultRoll = UnityEngine.Random.Range(1, 100);

            //Check for explosion
            if (resultRoll >= explodeTarget)
            {
                ExplodeEngine();
            }

            //Check for stuck on
            else if (resultRoll <= stuckOnTarget)
            {
                if (canShutdown())
                    EngineStuckOn();
                else //If we can't shut down then the engine explodes.
                    ExplodeEngine();
            }

            //Shutdown
            else
            {
                if (canShutdown())
                    ShutdownEngine();
                else //If we can't shut down then the engine explodes.
                    ExplodeEngine();
            }
        }

        /// <summary>
        /// This method is used to apply a particular failure mode. Mostly it's used internally, but it's exposed as a public method for diagnistic purposes.
        /// </summary>
        /// <param name="mode">A BARISEngineFailureModes enumerator containing the failure mode to process.</param>
        public void ApplyFailureMode(BARRISEngineFailureModes mode = BARRISEngineFailureModes.None)
        {
            //Make sure we have the active engine.
            getCurrentEngine();

            switch (mode)
            {
                case BARRISEngineFailureModes.StuckOn:
                    engine.allowShutdown = false;
                    engine.currentThrottle = 1.0f;
                    engine.finalThrust = engine.maxThrust;
                    engine.throttleLocked = true;
                    break;

                case BARRISEngineFailureModes.Shutdown:
                    if (EngineIsRunning)
                    {
                        engine.Shutdown();
                        engine.currentThrottle = 0;
                    }
                    break;

                case BARRISEngineFailureModes.ReducedThrust:
                    engine.currentThrottle = failureThrustPercent;
                    engine.finalThrust *= failureThrustPercent;
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// This diagnostic method is used to force an engine to stay on. It only applies to engines that can be shut off to begin with.
        /// </summary>
        [KSPEvent(guiName = "Engine stuck on")]
        public void EngineStuckOn()
        {
            //Make sure we're in failure mode
            if (qualityControl.currentQuality > 0)
            {
                qualityControl.currentQuality = 0;
                BARISScenario.Instance.onTimeTickEvent += failureModeTimeTick;
                qualityControl.SendPartBrokenEmail();
            }

            //Make sure we have the active engine.
            getCurrentEngine();

            failureMode = BARRISEngineFailureModes.StuckOn;
            failureModeID = (int)failureMode;

            isStuckOn = true;
            engine.allowShutdown = false;
            engine.currentThrottle = 1.0f;
            engine.finalThrust = engine.maxThrust;
            engine.throttleLocked = true;

            qualityControl.UpdateQualityDisplay(qualityControl.qualityDisplay + " Throttle Stuck");
            if (this.part.vessel == FlightGlobals.ActiveVessel)
            {
                string message = Localizer.Format(this.part.partInfo.title + BARISScenario.EngineStuckOn);
                BARISScenario.Instance.LogPlayerMessage(message);
            }
        }

        /// <summary>
        /// This diagnostics method shuts an engine off and removes its ability to be restarted again.
        /// </summary>
        [KSPEvent(guiName = "Shutdown Engine (Fail Mode)")]
        public void ShutdownEngine()
        {
            //Make sure we're in failure mode
            if (qualityControl.currentQuality > 0)
            {
                qualityControl.currentQuality = 0;
                BARISScenario.Instance.onTimeTickEvent += failureModeTimeTick;
                qualityControl.SendPartBrokenEmail();
            }

            //Make sure we have the active engine.
            getCurrentEngine();

            failureMode = BARRISEngineFailureModes.Shutdown;
            failureModeID = (int)failureMode;

            engine.Shutdown();
            engine.currentThrottle = 0;

            qualityControl.UpdateQualityDisplay(qualityControl.qualityDisplay + " Shutdown");
            if (this.part.vessel == FlightGlobals.ActiveVessel)
            {
                string message = Localizer.Format(this.part.partInfo.title + BARISScenario.EngineShutdown);
                BARISScenario.Instance.LogPlayerMessage(message);
            }
        }

        /// <summary>
        /// This diagnostics method causes the engine to explode. Typically this only happens in extreme cases (critical failure of the quality check)
        /// </summary>
        [KSPEvent(guiName = "Explode Engine")]
        public void ExplodeEngine()
        {
            //Make sure we're in failure mode
            if (qualityControl.currentQuality > 0)
            {
                qualityControl.currentQuality = 0;
                qualityControl.SendPartBrokenEmail();
            }

            //Make sure we have the active engine.
            getCurrentEngine();

            this.part.explode();

            if (this.part.vessel == FlightGlobals.ActiveVessel)
            {
                string message = Localizer.Format(this.part.partInfo.title + BARISScenario.CatastrophicFailure);
                BARISScenario.Instance.LogPlayerMessage(message);
            }
        }

        /// <summary>
        /// This property determines whether or not the engine is running.
        /// </summary>
        public bool EngineIsRunning
        {
            get
            {
                //Make sure we have the active engine.
                getCurrentEngine();

                //No engine? Then it's clearly not running...
                if (engine == null)
                    return false;

                //Check operation status
                if (!engine.isOperational || !engine.EngineIgnited)
                    return false;
                else
                    return true;
            }
        }
        #endregion

        #region Housekeeping
        protected void debugLog(string message)
        {
            if (BARISScenario.showDebug == true)
                Debug.Log("[" + this.ClassName + "] - " + message);
        }

        protected virtual void onThrottleUpDown(bool isThrottleUp)
        {
            if (EngineIsRunning)
            {
                debugLog("onThrottleUpDown thinks engine is running. Performing quality check...");
                qualityControl.PerformQualityCheck();
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Setup the engines
            setupEngines();

            //Failure mode
            failureMode = (BARRISEngineFailureModes)failureModeID;
            if (failureMode != BARRISEngineFailureModes.None)
                BARISScenario.Instance.onTimeTickEvent += failureModeTimeTick;
        }

        public void OnDestroy()
        {
            if (BARISScenario.Instance != null)
                BARISScenario.Instance.onTimeTickEvent -= failureModeTimeTick;

            if (qualityControl != null)
            {
                qualityControl.onUpdateSettings -= onUpdateSettings;
                qualityControl.onPartBroken -= OnPartBroken;
                qualityControl.onPartFixed -= OnPartFixed;
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            //If we're mothballed then shut the engine down.
            bool isRunning = EngineIsRunning;
            if (isMothballed && isRunning)
            {
                engine.Shutdown();
                engine.currentThrottle = 0;
            }

            if (!partsCanBreak)
                return;

            if (isRunning != wasRunning)
            {
                wasRunning = isRunning;
                qualityControl.UpdateActivationState();

                if (isRunning)
                {
                    debugLog("Engine start check");
                    qualityControl.PerformQualityCheck();
                }
            }
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

            if (!BARISBridge.EnginesCanFail)
                return false;

            return EngineIsRunning;
        }

        public void SubscribeToEvents(BaseQualityControl moduleQualityControl)
        {
            debugLog("SubscribeToEvents");
            qualityControl = (ModuleQualityControl)moduleQualityControl;
            qualityControl.onUpdateSettings += onUpdateSettings;
            qualityControl.onPartBroken += OnPartBroken;
            qualityControl.onPartFixed += OnPartFixed;
            qualityControl.onMothballStateChanged += onMothballStateChanged;
            BARISScenario.Instance.onThrottleUpDown += onThrottleUpDown;

            if (BARISScenario.showDebug && qualityControl.guiVisible)
            {
                Events["ExplodeEngine"].guiActive = true;
                Events["EngineStuckOn"].guiActive = true;
                Events["ShutdownEngine"].guiActive = true;
                Events["GenerateFailureMode"].guiActive = true;
            }
        }

        protected void onUpdateSettings(BaseQualityControl moduleQualityControl)
        {
            Events["ExplodeEngine"].active = BARISScenario.showDebug;
            Events["EngineStuckOn"].active = BARISScenario.showDebug;
            Events["ShutdownEngine"].active = BARISScenario.showDebug;
            Events["GenerateFailureMode"].active = BARISScenario.showDebug;

            partsCanBreak = BARISScenario.partsCanBreak;
        }

        public void onMothballStateChanged(bool isMothballed)
        {
            this.isMothballed = isMothballed;
        }

        public void OnPartFixed(BaseQualityControl moduleQualityControl)
        {
            ModuleEngines[] engines = multiModeEngines.Values.ToArray();

            //Restore engine state
            isStuckOn = false;

            for (int index = 0; index < engines.Length; index++)
            {
                engines[index].currentThrottle = FlightInputHandler.state.mainThrottle;
                engines[index].allowShutdown = allowShutdown[index];
                engines[index].throttleLocked = throttleLocked[index];
            }

            //Reset failure mode
            failureMode = BARRISEngineFailureModes.None;
            failureModeID = 0;
            BARISScenario.Instance.onTimeTickEvent -= failureModeTimeTick;
        }

        public void OnPartBroken(BaseQualityControl moduleQualityControl)
        {
            if (!BARISBridge.PartsCanBreak || !BARISBridge.EnginesCanFail)
                return;

            //Generate a failure mode
            GenerateFailureMode();

            //Subscribe to the time tick event to apply failure mode.
            BARISScenario.Instance.onTimeTickEvent += failureModeTimeTick;
        }
        #endregion

        protected void failureModeTimeTick()
        {
            //If we're broken, then apply the failure mode
            ApplyFailureMode(failureMode);
        }

        protected bool canShutdown()
        {
            for (int index = 0; index < allowShutdown.Length; index++)
            {
                if (allowShutdown[index] == false)
                {
                    debugLog(this.part.partInfo.title + " can't be shutdown");
                    return false;
                }
            }

            debugLog(this.part.partInfo.title + " can be shutdown");
            return true;
        }

        protected ModuleEngines getCurrentEngine()
        {
            //If we have multiple engines, make sure we have the current one.
            if (engineSwitcher != null)
            {
                if (engineSwitcher.runningPrimary)
                    engine = multiModeEngines[engineSwitcher.primaryEngineID];
                else
                    engine = multiModeEngines[engineSwitcher.secondaryEngineID];
            }

            return engine;
        }

        protected void setupEngines()
        {
            //See if we have multiple engines that we need to support
            engineSwitcher = this.part.FindModuleImplementing<MultiModeEngine>();
            List<ModuleEngines> engines = this.part.FindModulesImplementing<ModuleEngines>();
            ModuleEngines moduleEngine = null;

            //Setup our state flags
            allowShutdown = new bool[engines.Count];
            throttleLocked = new bool[engines.Count];

            //Find all the engines in the part and record their properties.
            for (int index = 0; index < engines.Count; index++)
            {
                moduleEngine = engines[index];
                if (!multiModeEngines.ContainsKey(moduleEngine.engineID))
                {
                    multiModeEngines.Add(moduleEngine.engineID, moduleEngine);

                    allowShutdown[index] = moduleEngine.allowShutdown;
                    throttleLocked[index] = moduleEngine.throttleLocked;
                }
            }

            //Get whichever multimode engine is the active one.
            if (engineSwitcher != null)
            {
                if (engineSwitcher.runningPrimary)
                    engine = multiModeEngines[engineSwitcher.primaryEngineID];
                else
                    engine = multiModeEngines[engineSwitcher.secondaryEngineID];
            }

            //Just get the first engine in the list.
            else if (engines.Count > 0)
            {
                engine = multiModeEngines.Values.ToArray()[0];
            }
        }
        #endregion
    }
}
