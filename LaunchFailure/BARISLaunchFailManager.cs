using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;
using KSP.UI.Screens.Flight;
#if !KSP122
using KSP.Localization;
#endif

/*
Source code copyright 2017, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    /// <summary>
    /// The BARISLaunchFailManager handles all the housekeeping and rule application that occur during staging events. Whenever a stage is activted, it makes a quality check against the vessel's reliability.
    /// This check is known as a reliability check. Astronauts and event cards can affect the outcome, but a failed check will cause one or more breakable parts in the stage to fail. 
    /// This happens regardless of their current MTBF values. If the reliability check ends with a critical failure, then the vessel will be destroyed. Fortunately, astronauts have the opportunity to
    /// bail out of the doomed vessel if it's been designed with a launch escape system and/or escape pods. The crew will make an escape check (pilots have the best chance of succeeding) and if successful,
    /// they crew escapes the doomed vessel before it explodes. If not...
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class BARISLaunchFailManager : MonoBehaviour
    {
        /// <summary>
        /// The instance of the launch failure manager
        /// </summary>
        public static BARISLaunchFailManager Instance;

        /// <summary>
        /// When a reliability check ends in a critical failure, the launch failure manager will fire this event. It gives external mods a chance to
        /// avert the critical failure in some way.
        /// </summary>
        public event OnStagingCriticalFailure onStagingCriticalFailure;

        /// <summary>
        /// Max number of seconds to wait until performing a staging reliability check
        /// </summary>
        public static float MaxStagingCheckTime = 20.0f;

        /// <summary>
        /// Minimum number of minutes to wait to check for a launch failure.
        /// This check is made once per launch. If the vessel attains orbit before the launch check timer goes off, then
        /// somthing might break.
        /// </summary>
        public static float MinOrbitFailTime = 2.0f;

        /// <summary>
        /// Maximum number of minutes to wait to check for a launch failure.
        /// This check is made once per launch. If the vessel attains orbit before the launch check timer goes off, then
        /// somthing might break.
        /// </summary>
        public static float MaxOrbitFailTime = 6.0f;

        /// <summary>
        /// When a staging event experiences critical failure, the onStagingCriticalFailure event is fired. 
        /// That gives mods a chance to avert the critical failure. To do so, set criticalFailureAverted to true.
        /// </summary>
        public static bool criticalFailureAverted;

        /// <summary>
        /// Skill required to initiate the launch abort system during a critically failed reliability check.
        /// </summary>
        public static string escapeCheckSkill = "FullVesselControlSkill";

        /// <summary>
        /// How many seconds to wait until causing total vessel destruction. This applies to staging events where there is a catastrophic failure.
        /// </summary>
        public static double VesselDestructionDelay = 1.0f;

        /// <summary>
        /// This flag indicates whether or not the vessel that's about to launch has been integrated. If it hasn't then the player will receive a warning message.
        /// </summary>
        public static bool vehicleNotIntegrated = false;

        //Debug parameters
        public bool NextStagingFails;
        public bool NextStagingIsCascadeFailure;
        public bool NextStagingIsSuccessful;
        public bool NextStagingAstonautAverted;
        public double stagingStartTime = 0;
        public double stagingCheckSeconds = 20.0f;
        public double launchStartTime = 0;
        public double launchCheckSeconds = 0;
        public int stagingID = -1;
        public static bool stageCheckInProgress = false;

        protected List<ModuleQualityControl> doomed = new List<ModuleQualityControl>();
        protected int launchEscapeBase = 45;

        protected void debugLog(string message)
        {
            if (BARISScenario.showDebug == true)
                Debug.Log("[BARISLaunchFailManager] - " + message);
        }

        public void Awake()
        {
            debugLog("Awake called");

            Instance = this;
            GameEvents.onStageActivate.Add(onStageActivate);
            GameEvents.onStageSeparation.Add(onStageSeparation);

            if (vehicleNotIntegrated)
            {
                debugLog("Vehicle not integrated");
                vehicleNotIntegrated = false;
                ScreenMessages.PostScreenMessage(Localizer.Format(BARISScenario.NotIntegragedMsg), 20.0f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        public void Destroy()
        {
            GameEvents.onStageActivate.Remove(onStageActivate);
            GameEvents.onStageSeparation.Remove(onStageSeparation);
        }

        protected void launchTimeTick()
        {
            //Get elapsed time and perform the staging check when it's time.
            double elapsedTime = Planetarium.GetUniversalTime() - launchStartTime;

            //If our launch check time has expired and the vessel hasn't attained orbit, then perform a relaibility check.
            if (elapsedTime >= launchCheckSeconds)
            {
                BARISScenario.Instance.onTimeTickEvent -= launchTimeTick;

                if (FlightGlobals.ActiveVessel.situation != Vessel.Situations.ORBITING)
                {
                    debugLog("Vessel hasn't orbited, performing reliability check.");
                    CheckYoStaging();
                }
            }
        }

        protected void stagingTimeTick()
        {
            //Get elapsed time and perform the staging check when it's time.
            double elapsedTime = Planetarium.GetUniversalTime() - stagingStartTime;

            if (elapsedTime >= stagingCheckSeconds)
            {
                BARISScenario.Instance.onTimeTickEvent -= stagingTimeTick;
                CheckYoStaging();
            }
        }

        //Crew transfers need some time in between the transfer before spawning the IVA.
        protected IEnumerator<YieldInstruction> spawnIVA()
        {
            yield return new WaitForSeconds(0.25f);
            FlightGlobals.ActiveVessel.SpawnCrew();
            CameraManager.ICameras_ResetAll();
            GameEvents.onVesselChange.Fire(FlightGlobals.ActiveVessel);
            yield return null;
        }

        /// <summary>
        /// This method performs the staging reliability check. If successful, then all parts in the stage may contribute to their type of part's flight experience.
        /// If it fails, then one or more parts in the stage may fail regardless of their MTBF rating. If it critically fails, the vessel blows up.
        /// </summary>
        public void CheckYoStaging()
        {
            debugLog("CheckYoStaging!");
            Vessel vessel = FlightGlobals.ActiveVessel;
            bool isCascadeFailure = false;
            bool isSuccessfulStaging = false;
            bool astronautAverted = false;
            bool isFailure = false;
            ModuleQualityControl qualityControl = null;
            int totalBreakables = 0;
            string message;
            bool updateEditorBays = false;

            //Stage check flag
            stageCheckInProgress = true;

            //Debug stuff
            if (BARISScenario.showDebug)
            {
                isCascadeFailure = NextStagingIsCascadeFailure;
                NextStagingIsCascadeFailure = false;

                isSuccessfulStaging = NextStagingIsSuccessful;
                NextStagingIsSuccessful = false;

                astronautAverted = NextStagingAstonautAverted;
                NextStagingAstonautAverted = false;

                isFailure = NextStagingFails;
                NextStagingFails = false;
            }

            //Get the vessel cache
            bool createdNewRecord = false;
            LoadedQualitySummary qualitySummary = BARISScenario.Instance.GetLoadedQualitySummary(vessel, out createdNewRecord);
            if (qualitySummary == null)
                return;

            //Get failure candidates
            ModuleQualityControl[] failureCandidates = qualitySummary.GetStagingFailureCandidates(stagingID);

            //Make a vessel reliability check
            QualityCheckResult result = BARISScenario.Instance.PerformQualityCheck(qualitySummary.reliability, qualitySummary.highestRank, 0, false);

            //Debug checks
            if (BARISScenario.showDebug)
            {
                if (isSuccessfulStaging)
                    result.statusResult = QualityCheckStatus.success;
                else if (astronautAverted)
                    result.statusResult = QualityCheckStatus.astronautAverted;
                else if (isFailure)
                    result.statusResult = QualityCheckStatus.fail;
                else if (isCascadeFailure)
                    result.statusResult = QualityCheckStatus.criticalFail;

                debugLog("DEBUG: New reliability check result: " + result.statusResult);
            }

            switch (result.statusResult)
            {
                //All the failure candidates explode!
                case QualityCheckStatus.criticalFail:
                    if (failureCandidates != null)
                    {
                        //Fire the catastrophic failure event to give others a chance to avert disaster somehow.
                        if (onStagingCriticalFailure != null)
                            onStagingCriticalFailure();
                        if (criticalFailureAverted)
                        {
                            //Reset the flag
                            criticalFailureAverted = false;
                            break;
                        }

                        //Ok we're go for vessel destruction!
                        //Inform player of cascade failure
                        message = vessel.vesselName + Localizer.Format(BARISScenario.CascadeFailureMsg);
                        BARISScenario.Instance.LogPlayerMessage(message);

                        //Mark a quality module for death. This will blow up the vessel it is attached to.
                        //It cannot be a module that is marked as an escape pod. This part will initiate vessel destruction.
                        //Vessel might or might not be the active vessel depending upon how the abort sequence is set up.
                        ModuleEscapePod escapePod = null;
                        for (int index = failureCandidates.Length - 1; index >= 0; index--)
                        {
                            escapePod = failureCandidates[index].part.FindModuleImplementing<ModuleEscapePod>();
                            if (escapePod == null || escapePod.escapePodEnabled == false)
                            {
                                failureCandidates[index].SetMarkedForDeath(true);
                                break;
                            }
                            escapePod = null;
                        }

                        //Make an escape check to see if we can trigger an abort sequence.
                        launchEscapeBase = getHighestStupidity();
                        QualityCheckResult escapeResult = BARISScenario.Instance.PerformQualityCheck(vessel, launchEscapeBase, escapeCheckSkill, false);

                        //Go for abort sequence!
                        if (escapeResult.statusResult != QualityCheckStatus.fail && escapeResult.statusResult != QualityCheckStatus.criticalFail)
                        {
                            //Give crew a chance to get to the escape pods.
                            abandonShip(vessel);

                            //If there is an astronaut aboard with the escape skill then assume the astronaut hit the launch abort.
                            if (escapeResult.astronaut != null)
                            {
                                message = escapeResult.astronaut.name + Localizer.Format(BARISScenario.LASActivatedMsg);
                                BARISScenario.Instance.LogPlayerMessage(message);
                            }

                            //Hit the abort button
                            vessel.ActionGroups.SetGroup(KSPActionGroup.Abort, true);
                        }
                    }
                    else
                    {
                        debugLog("No failure candidates to blow up! Picking a random part to explode...");
                        int failedPartIndex = UnityEngine.Random.Range(0, qualitySummary.qualityModules.Length - 1);
                        qualitySummary.qualityModules[failedPartIndex].part.explode();
                    }
                    break;

                //Log successful flight experience
                case QualityCheckStatus.success:
                    totalBreakables = qualitySummary.qualityModules.Length;
                    for (int index = 0; index < totalBreakables; index++)
                        qualitySummary.qualityModules[index].UpdateFlightLog();
                    updateEditorBays = true;
                    break;

                //Any number of the failure candidates can fail.
                case QualityCheckStatus.fail:
                    if (failureCandidates != null)
                    {
                        int expGainTarget = BARISSettingsLaunch.StagingFailExpTarget;
                        debugLog("Target number to gain flight experience: " + expGainTarget);

                        int failedModules = UnityEngine.Random.Range(1, failureCandidates.Length);
                        for (int index = 0; index < failedModules; index++)
                        {
                            qualityControl = failureCandidates[index];
                            if (!qualityControl.IsBroken)
                                qualityControl.DeclarePartBroken();
                            else
                                qualityControl.BreakAPartModule();

                            //There's a chance that the part will also gain flight experience
                            if (UnityEngine.Random.Range(1, 100) >= expGainTarget)
                            {
                                debugLog(qualityControl.part.partInfo.title + " has gained flight experience during part failure.");
                                qualityControl.UpdateFlightLog();
                                updateEditorBays = true;
                            }

                            //There's a chance that the part will explode!
                            if (BARISBreakableParts.FailuresCanExplode)
                            {
                                if (UnityEngine.Random.Range(1, 100) >= BARISBreakableParts.ExplosivePotentialLaunches)
                                {
                                    message = qualityControl.part.partInfo.title + Localizer.Format(BARISScenario.CatastrophicFailure);
                                    BARISScenario.Instance.LogPlayerMessage(message);
                                    qualityControl.part.explode();
                                }
                            }
                        }
                    }
                    else
                    {
                        debugLog("No failureCandidates have MTBF = 0");
                    }
                    break;

                //Add a failure averted event card
                case QualityCheckStatus.criticalSuccess:
                    totalBreakables = qualitySummary.qualityModules.Length;
                    for (int index = 0; index < totalBreakables; index++)
                        qualitySummary.qualityModules[index].UpdateFlightLog();
                    break;

                //Show event card result
                case QualityCheckStatus.eventCardAverted:
                    break;

                //Shout out to the astronaut that saved the day
                case QualityCheckStatus.astronautAverted:
                    if (qualitySummary.rankingAstronaut != null)
                    {
                        message = qualitySummary.rankingAstronaut.name + Localizer.Format(BARISScenario.QualityCheckFailAvertedAstronaut3);
                        BARISScenario.Instance.LogPlayerMessage(message);
                    }
                    break;

                default:
                    break;
            }

            //Cleanup
            if (updateEditorBays)
                BARISScenario.Instance.UpdateEditorBayFlightBonuses();

            BARISAppButton.flightAppView.UpdateCachedData();
            stageCheckInProgress = false;
        }

        protected int getHighestStupidity()
        {
            int stupidity;
            int highestStupidity = 0;

            if (FlightGlobals.ActiveVessel.GetVesselCrew().Count == 0)
                return 0;

            ProtoCrewMember[] vesselCrew = FlightGlobals.ActiveVessel.GetVesselCrew().ToArray();
            for (int index = 0; index < vesselCrew.Length; index++)
            {
                stupidity = Mathf.RoundToInt(vesselCrew[index].stupidity);
                if (stupidity > highestStupidity)
                    highestStupidity = stupidity;
            }

            return highestStupidity;
        }

        protected void abandonShip(Vessel vessel)
        {
            //If the vessel has no crew then we're done.
            if (vessel.GetVesselCrew().Count == 0)
            {
                debugLog("No crew to evacuate");
                return;
            }
            List<Part> escapePods = new List<Part>();
            Part escapePod = null;
            int podIndex = 0;
            int evacuatedCount = 0;
            ProtoCrewMember astronaut;

            //Find all ModuleEscapePod modules that are marked as escape pods and transfer crew to them.
            List<ModuleEscapePod> pods = vessel.FindPartModulesImplementing<ModuleEscapePod>();
            foreach (ModuleEscapePod pod in pods)
            {
                if (pod.part.CrewCapacity > 0 && pod.escapePodEnabled)
                    escapePods.Add(pod.part);
            }
            debugLog("Found " + escapePods.Count + " escape pods");

            //If we have no escape pods then we're done.
            if (escapePods.Count == 0)
            {
                debugLog("No escape pods!");
                return;
            }

            //Get the initial escape pod
            escapePod = escapePods[podIndex];

            //Yank all the crew and stick them in an escape pod. If we can.
            foreach (Part part in vessel.parts)
            {
                while (part.protoModuleCrew.Count > 0)
                {
                    //If the pod is out of space then get another pod
                    if (escapePod.protoModuleCrew.Count >= escapePod.CrewCapacity)
                    {
                        podIndex += 1;
                        if (podIndex < escapePods.Count)
                            escapePod = escapePods[podIndex];
                        else
                            break;
                    }

                    //Get the astronaut
                    astronaut = part.protoModuleCrew[0];

                    //Remove the astronaut
                    part.RemoveCrewmember(astronaut);
                    astronaut.rosterStatus = ProtoCrewMember.RosterStatus.Available;
                    debugLog("Removed " + astronaut.name + " from " + part.partInfo.title);

                    //Add astronaut to the pod
                    escapePod.AddCrewmember(astronaut);
                    astronaut.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                    escapePod.RegisterCrew();
                    astronaut.seat.SpawnCrew();
                    KerbalPortraitGallery.Instance.UpdatePortrait(astronaut.KerbalRef);
                    debugLog("Added " + astronaut.name + " to " + escapePod.partInfo.title);
                    evacuatedCount += 1;
                }
            }

            //Inform player that astronauts reached escape pods.
            string message = vessel.vesselName + Localizer.Format(BARISScenario.AstronautsEvacuatedMsg);
            BARISScenario.Instance.LogPlayerMessage(message);
            StartCoroutine(spawnIVA());
        }

        protected void onStageActivate(int stageID)
        {
            if (!BARISSettings.PartsCanBreak)
            {
                debugLog("Parts can't break");
                return;
            }
            if (!BARISSettingsLaunch.LaunchesCanFail)
            {
                debugLog("Launches can't fail");
                return;
            }
            if (stageCheckInProgress)
            {
                debugLog("Staging check in progress");
                return;
            }

            //Static fire testing hint
            if (!BARISScenario.showedStaticFireTooltip && BARISScenario.partsCanBreak)
            {
                BARISScenario.showedStaticFireTooltip = true;
                BARISEventCardView cardView = new BARISEventCardView();

                cardView.WindowTitle = BARISScenario.ToolTipStaticFireTitle;
                cardView.description = BARISScenario.ToolTipStaticFireMsg;
                cardView.imagePath = BARISScenario.StaticFireToolTipImagePath;

                cardView.SetVisible(true);
                GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
            }

            debugLog("onStageActivate: stageID: " + stageID);
            stageCheckInProgress = true;

            //Record staging ID
            stagingID = stageID;

            //Get start time
            stagingStartTime = Planetarium.GetUniversalTime();

            //Determine how long to wait until we do a staging reliability check
            stagingCheckSeconds = UnityEngine.Random.Range(1, MaxStagingCheckTime);
            debugLog("Staging check in " + stagingCheckSeconds + " seconds");

            //Subscribe to the timetick event
            BARISScenario.Instance.onTimeTickEvent += stagingTimeTick;
        }

        protected void onStageSeparation(EventReport report)
        {
            debugLog("onStageSeparation called");

            if (report == null)
            {
                debugLog("onLaunch: report is null");
                return;
            }

            debugLog("eventType: " + report.eventType);
            debugLog("Stage: " + report.stage);
            if (report.origin != null)
                debugLog("Part: " + report.origin.partInfo.title);

        }
    }
}
