using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
using Upgradeables;
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
    public delegate void OnTimeTickEvent();
    public delegate void OnQualityCheckEvent(QualityCheckResult result);
    public delegate void OnSASUpdate(bool isActive);
    public delegate void OnRCSUpdate(bool isActive);
    public delegate void OnThrottleUpDown(bool isThrottleUp);
    public delegate void OnStagingCriticalFailure();

    /// <summary>
    /// The BARISScenario handles all the bookeeping and timekeeping for BARIS. Its public methods handle a variety of tasks including quality checks, reporting results, keeping track of when RCS, SAS
    /// throttle, and other conditions change, and a variety of other tasks.
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class BARISScenario : ScenarioModule
    {
        /// <summary>
        /// This field is a flag to indicate if BARIS should be running in debug mode or not. If it is, BARIS will write a number of entries to the KSP logs and provide several debug buttons for testing purposes.
        /// </summary>
        public static bool showDebug = true;

        /// <summary>
        /// The instance variable of the BARISScenario.
        /// </summary>
        public static BARISScenario Instance;

        #region Events
        /// <summary>
        /// Event type for physics updates
        /// </summary>
        public event OnTimeTickEvent onTimeTickEvent;

        /// <summary>
        /// Event type when SAS is toggled
        /// </summary>
        public event OnSASUpdate onSasUpdate;

        /// <summary>
        /// Event type for when RCS is toggled
        /// </summary>
        public event OnRCSUpdate onRcsUpdate;

        /// <summary>
        /// Event type for when throttle setting is set to zero or is increased from zero.
        /// </summary>
        public event OnThrottleUpDown onThrottleUpDown;

        /// <summary>
        /// Event type for when a staging event has a critical failure.
        /// </summary>
        public event OnQualityCheckEvent onQualityCheck;
        #endregion

        #region RocketConstruction
        /// <summary>
        /// Maximum bays allowed for a level 1 facility.
        /// </summary>
        public static int Level1BayCount = 1;

        /// <summary>
        /// Maximum bays allowed for a level 2 facility.
        /// </summary>
        public static int Level2BayCount = 2;

        /// <summary>
        /// Maximum bays allowed for a level 3 facility.
        /// </summary>
        public static int Level3BayCount = 4;

        /// <summary>
        /// If launches can't fail, then there's no way for them to gain flight experience. For this case,
        /// Assign a default flight bonus.
        /// </summary>
        public static int DefaultFlightBonus = 10;

        /// <summary>
        /// Maximum per-part integration bonus that can be obtained during vehicle integration.
        /// The cap represents a level 1 building.
        /// </summary>
        public static int Level1IntegrationCap = 20;

        /// <summary>
        /// Maximum per-part integration bonus that can be obtained during vehicle integration.
        /// The cap represents a level 2 building.
        /// </summary>
        public static int Level2IntegrationCap = 30;

        /// <summary>
        /// Maximum per-part integration bonus that can be obtained during vehicle integration.
        /// The cap represents a level 3 building.
        /// </summary>
        public static int Level3IntegrationCap = 40;

        /// <summary>
        /// Maximum number of VAB High Bays allowed.
        /// </summary>
        public static int MaxHighBays = 4;

        /// <summary>
        /// Maximum number of SPH Hangar Bays allowed.
        /// </summary>
        public static int MaxHangarBays = 4;

        /// <summary>
        /// Maximum number of workers allowed per High Bay/Hangar Bay
        /// </summary>
        public static int MaxWorkersPerBay = 50;

        /// <summary>
        /// Maximum number of workers allowed per VAB/SPH. This assumes that the facility has been fully upgraded.
        /// </summary>
        public static int MaxWorkersPerFacility = 200;

        /// <summary>
        /// Minimum number of workers allowed per VAB/SPH. This assumes that the facility hasn't been upgraded.
        /// </summary>
        public static int MinWorkersPerFacility = 25;

        /// <summary>
        /// How many days to wait before deducting the payroll from existing Funds.
        /// </summary>
        public static int DaysPerPayroll = 12;

        /// <summary>
        /// The cost in funds per worker. It is payed every DaysPerPayroll
        /// </summary>
        public static int PayrollPerWorker = 100;

        /// <summary>
        /// The cost in funds per astronaut. It is payed every DaysPerPayroll.
        /// </summary>
        public static int PayrollPerAstronaut = 250;

        /// <summary>
        /// How many points of integration per worker
        /// </summary>
        public static int IntegrationPerWorker = 1;

        /// <summary>
        /// Integration point multiplier afforded by facility level. Assumes max facility level
        /// </summary>
        public static float FacilityIntegrationMultiplier = 1.25f;

        /// <summary>
        /// A collection of EditorBayItem objects, which represent the integration progress of a particular vessel awaiting launch.
        /// </summary>
        public Dictionary<string, EditorBayItem> editorBayItems = new Dictionary<string, EditorBayItem>();

        /// <summary>
        /// The EditorBayItem containing the last vessel that was launched. Used primarily when the player reverts to the editor.
        /// </summary>
        public static EditorBayItem launchedVesselBay;

        /// <summary>
        /// Multiplier for tiger team repair costs.
        /// </summary>
        public static float RepairFactor = 0.1f;
        #endregion

        #region TestBench
        /// <summary>
        /// Maximum number of flights per quality bonus.
        /// </summary>
        public static int MaxFlightsPerQualityBonus = 20;

        /// <summary>
        /// Max discount/surcharge to apply based upon reputation.
        /// </summary>
        public static float ReputationCostModifier = 0.2f;
        #endregion

        #region Miscellanious
        /// <summary>
        /// How long to highlight the part when it is broken.
        /// After this time, the normal mouseover will highlight the part.
        /// </summary>
        public static double highlightTimeInterval = 5.0;

        /// <summary>
        /// Last time the scenario was updated. Used for hourly quality checks.
        /// </summary>
        public static double lastUpdateTime;
        #endregion

        #region Quality Stats
        /// <summary>
        /// When you repair a vessel from the Tracking Station, this is the minimum number of days required.
        /// </summary>
        public static int MinimumTigerTeamRepairDays = 3;

        /// <summary>
        /// Target number required for a tiger team to successfully remotely repair a vessel.
        /// </summary>
        public static int TigerTeamRepairTarget = 75;

        /// <summary>
        /// Flag to indicate that BARIS should kill the timewarp when a Tiger Team completes a research attempt.
        /// </summary>
        public static bool KillTimewarpOnTigerTeamCompleted = true;

        /// <summary>
        /// Default integration bonus for when a part is created in the field.
        /// </summary>
        public static int FlightCreatedIntegrationBonus = 50;

        /// <summary>
        /// MTBF bonus for upgraded Research and Development. It is added to the part's MTBF using the following formula:
        /// Part MTBF * Facility Level (ranges from 0 to 1) * MTBFFacilityBonus
        /// Ex: A part has 600 hours of MTBF, and RnD is at Level 2. The MTBF to add is: 600 * 0.5 * 0.25 = 75.
        /// The part's new MTBF is: 600 + 75 = 675.
        /// </summary>
        public static float MTBFFacilityBonus = 0.25f;

        /// <summary>
        /// How many hours to add to a part's MTBF rating per point of flight experience that it gains to quality.
        /// Ex: A part has 10 flights, and parts gain 1 point of quality every 5 flights. If MTBFPerQualityBonus is 30,
        /// then the part gains (10/5) * 30 = 60 additional MTBF hours.
        /// </summary>
        public static double MTBFPerQualityBonus = 30.0f;

        /// <summary>
        /// The maximum number of MTBF hours that a part can have. Default equates to two kerbal-years.
        /// </summary>
        public static double MTBFCap = 5112.0f;

        /// <summary>
        /// Bonus provided for Facilities. Assumes facility is fully upgraded.
        /// </summary>
        public static int BaseFacilityBonus = 10;

        /// <summary>
        /// How many seconds per day
        /// </summary>
        public static double secondsPerDay;

        /// <summary>
        /// How many seconds per year
        /// </summary>
        public static double secondsPerYear;

        /// <summary>
        /// Quality Penalty for failing a quality check when out of MTBF
        /// </summary>
        public static int QualityCheckFailLoss = 2;

        /// <summary>
        /// Quality lossed when the part is repaired.
        /// </summary>
        public static int QualityLossPerRepairs = 5;

        /// <summary>
        /// Possibility of losing quality after a successful quality check when the part is out of mtbf. Roll range is 1-100.
        /// </summary>
        public static int WearAndTearTargetNumber = 10;

        /// <summary>
        /// How much quality to lose if the wear and tear check fails
        /// </summary>
        public static int WearAndTearQualityLoss = 2;

        /// <summary>
        /// How much of quality improves by during maintenance checks
        /// </summary>
        public static int MaintenanceQualityImprovement = 2;

        /// <summary>
        /// How many quality points do you get per skill point
        /// </summary>
        public static int QualityPerSkillPoint = 2;

        /// <summary>
        /// Critical fail target number
        /// </summary>
        public int CriticalFailRoll = 1;

        /// <summary>
        /// When a critical failure must be double-checked, the die roll can range from 1 and N.
        /// </summary>
        public int MaxRangeForCritalFailDoubleCheck = 50;

        /// <summary>
        /// Critical success target number
        /// </summary>
        public int CriticalSuccessRoll = 100;

        /// <summary>
        /// Timewarp threshold at which we start skipping checks (helps with game performance)
        /// </summary>
        public static int HighTimewarpIndex = 5;

        /// <summary>
        /// Maximum number of checks we can skip before we make a reliability check regardless of high timewarp.
        /// </summary>
        public static int MaxSkippedChecks = 10;

        /// <summary>
        /// Minimum penalty for skipping a reliability check due to high timewarp
        /// </summary>
        public static int MinimumCyclePenalty = -5;

        /// <summary>
        /// Penalty per skipped reliability check due to high timewarp
        /// </summary>
        public static int MissedCyclePenalty = -2;

        //Quality labels
        public static string ConditionBrokenLabel = "Broken:";
        public static string ConditionMaintenanceLabel = "Maintenance Required";
        public static string ConditionPoorLabel = "Poor";
        public static string ConditionFairLabel = "Fair";
        public static string ConditionGoodLabel = "Good";
        public static string ConditionExcellentLabel = "Excellent";
        public static string MTBFLabel = "MTBF: ";
        public static string QualityLabel = "Quality: ";
        public static string ConditionLabel = "Condition: ";

        //Quality thresholds
        public static float MaxPoorQuality = 0.25f;
        public static float MaxFairQuality = 0.5f;
        public static float MaxGoodQuality = 0.75f;
        public static int MaxExcellentQuality = 1;
        #endregion

        #region Player Messages
        public static float MessageDuration = 8.0f;
        public static string SkillListOr = " or ";
        public static string FromVessel = "From: ";
        public static string FromMissionControlSystems = "From: Mission Control - SYSTEMS";
        public static string MsgTitleMaintenance = " Needs Maintenance";
        public static string MsgTitleRepair = " Needs Repairs";
        public static string MsgTitleVesselRepairs = "Vessels need repairs";
        public static string MsgBodyVesselRepairs = "Flight, Systems. Be advised, my engineering board indicates that the following vessels have developed a problem and need repairs:";
        public static string MsgBodyTigerTeamRepairs = "FYI - You can form a Tiger Team from the Tracking Station to try and try to resolve the problem from the ground.";
        public static string MsgBodyVesselCheckStatus = "Recommend that you check on vessel status to confirm their condition and effect repairs.";
        public static string SubjectMaintenance = "Subject: Maintenance Request";
        public static string SubjectRepair = "Subject: Repair Request";
        public static string InsufficientRepairSkill = "Insufficient skill to repair the ";
        public static string InsufficientMaintenanceSkill = "Insifficient skill to maintain the ";
        public static string SkillRequired = ". It requires a level ";
        public static string InsufficientResources1 = "Insufficient resources to repair or maintain the ";
        public static string InsufficientResources2 = ". It requires ";
        public static string MaintenanceFailAvertedAstronaut = " managed to break something but fixed it before anything bad happened.";
        public static string QualityCheckFailAvertedAstronaut1 = " managed to jurry rig a fix for the ";
        public static string QualityCheckFailAvertedAstronaut2 = " before it failed.";
        public static string QualityCheckFailAvertedAstronaut3 = " averted a launch failure by switching to a backup system.";
        public static string MaintenancePerformedBy = " performs maintenance on the ";
        public static string PartFixedMessage = " is back to normal.";
        public static string PartMaintenanceSuccessful = "Maintenance successfully performed.";
        public static string PartMaintenanceFailed = "Maintenance attempt failed.";
        public static string PartMaintenanceCriticalFail = "The maintenance made things worse!";
        public static string SmallLeak = " has sprung a small leak!";
        public static string MediumLeak = " is leaking!";
        public static string LargeLeak = " has a major leak!";
        public static string ResourcesDumped = " lost all its resources due to a massive leak!";
        public static string MaxThrustReduced = " lost thrust due to controller malfunction. Thrust reduced by ";
        public static string EngineStuckOn = " cannot be shut off!";
        public static string EngineShutdown = " shutdown to prevent catastrophic failure.";
        public static string CatastrophicFailure = " has suffered a catasrophic failure!";
        public static string SASBroken = " gryo control has failed.";
        public static string RCSBroken = " RCS motor has failed.";
        public static string ComponentFailure = " Has suffered another component failure!";
        public static string TransmitterBroken = " transmitter has failed.";
        public static string LaunchFailureProbability = "Launch Failure Probability:";
        public static string AppFlightViewTitle = "Vehicle Health";
        public static string CascadeFailureMsg = " has suffered a catastrophic failure!";
        public static string FacilityBonusMsg = "Quality Check - Current VAB/SPH Bonus: +";
        public static string AstronautFacilityBonusMsg = "Quality Check - Current Astronaut Facility Bonus: +";
        public static string SASLabel = " Gyro";
        public static string RCSLabel = " RCS";
        public static string TransmitterLabel = " Radio";
        public static string SmallLeakLabel = " Sm. Leak";
        public static string MediumLeakLabel = " Md. Leak";
        public static string LargeLeakLabel = " Lg. Leak";
        public static string VesselLabel = "Vessel";
        public static string VesselHasProblem = " has a problem! One of its components has failed.";
        public static string ReliabilityLabel = "Reliability: ";
        public static string IntegratedReliabilityLabel = "Integrated Reliability: ";
        public static string LASActivatedMsg = " activated the launch abort system!";
        public static string AstronautsEvacuatedMsg = " astronauts were evacuated.";
        public static string SwitchToVesselTitle = "Switch To Vessel";
        public static string AVesselHasAProblem = "One or more vessels has a problem! You can switch to the troubled vessel below.";
        public static string FacilitiesTitle = "Facilities & Workers";
        public static string FacilityBonusLabel = "VAB/SPH Quality Bonus: ";
        public static string AstronautFacilityBonusLabel = "Astronaut Complex Quality Bonus: ";
        public static string InstructionsTitle = "Building A Rocket Isn't Simple";
        public static string InstructionsWelcome = "<color=white>Welcome to BARIS, where Building A Rocket Isn't Simple. With BARIS you have the ability to make parts wear out, experience launch failures, and take time to build rockets.</color>";
        public static string InstructionsWelcome1 = "<color=white>Be sure to consult the KSPedia for how to use BARIS. Click the icon that looks like the following to access the KSPedia:</color>";
        public static string InstructionsWelcome3 = "<color=white>To activate BARIS' features, follow the steps below:</color>";
        public static string Instructions1 = "<color=white>1. Press the Escape button to pause the game. Next, press the Settings button to display the Settings screen.</color>";
        public static string Instructions2 = "<color=white>2. In the Settings screen, press the Difficulty Options button.</color>";
        public static string Instructions3 = "<color=white>3. Find the BARIS button, and enable the Parts Can Break option.</color>";
        public static string Instructions4 = "<color=white>4. Finally, decide what options you wish to enable or disable, and press the Accept button.</color>";
        public static string InstructionsButtonLabel = "Show instructions";
        public static string EditorViewTitle = "Vehicle Integration";
        public static string LoadButtonTitle = "Load";
        public static string WorkersLabel = "Workers: ";
        public static string AvailableWorkersLabel = "Available Workers: ";
        public static string RushJobLabel = "Rush Integration";
        public static string StartVehicleIntegrationButton = "Start Integration";
        public static string HighBayLabel = "High Bay ";
        public static string HangarBayLabel = "Hangar Bay ";
        public static string CancelIntegrationLabel = "Cancel Integration";
        public static string CostLabel = "Cost: ";
        public static string CannotAffordRushMsg = "Can't afford the rush job!";
        public static string TotalHighBaysLabel = "VAB High Bays: ";
        public static string TotalHangarBaysLabel = "SPH Hangar Bays: ";
        public static string VABWorkersLabel = "VAB Workers: ";
        public static string SPHWorkersLabel = "SPH Workers: ";
        public static string Payroll1Label = "Worker Payroll: ";
        public static string Payroll2Label = " Funds every ";
        public static string Payroll3Label = " days";
        public static string PayrollAstronautsLabel = "Astronaut Payroll: ";
        public static string WorkersQuitMessage = "The Workers Union quits due to lack of pay!";
        public static string AstronautsQuitMessage = " has quit due to lack of pay!";
        public static string WorkersWorkingLabel = " working";
        public static string BuildTimeLabel = "Build Time: ";
        public static string BuildTimeLabelDays = " days";
        public static string BuildTimeLabelOneDay = " day";
        public static string BuildTimeLabelLessDay = "Less than a day";
        public static string BuildTimeLabelStatus = "Status: ";
        public static string BuildTimeLabelNeedsWorkers = "Needs workers";
        public static string BuildTimeLabelDone = "Completed";
        public static string VesselsCompletedTitle = "Vessel Integration Completed";
        public static string VesselsCompletedMsg = "The following vessels have completed their integration:";
        public static string VesselBuildCompleteMsg = " has completed vehicle integration.";
        public static string VesselBuildFailedMsg = " has failed vessel integration! All efforts are lost.";
        public static string VesselModifiedTitle = "Vessel Modified!";
        public static string VesselModifiedMsg = "Vehicle integration has FAILED due to modifications to the vessel! Reload the vessel from the Bay before launching.";
        public static string NotIntegragedMsg = "Vessel has not gone through vehicle integration! It is HIGHLY LIKELY to suffer a breakdown during launch.";
        public static string IntegrationDisabledMsg = "Vessel integration is currently disabled.";
        public static string IntegrationCompletedKACAlarm = " Vehicle Integration Complete";
        public static string WorkPausedMsg1 = "Work paused for ";
        public static string WorkPausedMsg2 = " days";
        public static string StatusBadassMsg = " is now a BadS.";
        public static string StatusMissingMsg = " has gone missing!";
        public static string StatusRetiredMsg = " has retired.";
        public static string StatusDeadMsg = " is dead!";
        public static string StatusRecruitedMsg = " has joined the team.";
        public static string BuildingDestroyedMsg = " has been destroyed!";
        public static string QualityCheckResultFail = "Next quality or reliability check fails.";
        public static string QualityCheckResultSuccess = "Next quality or reliability check succeeds.";
        public static string QualityCheckModifier = " modifier to the next quality or reliability check.";
        public static string FundsLabel = "Funds";
        public static string RepLabel = "Reputation";
        public static string ScienceLabel = "Science";
        public static string FacilityClosedMsg = "Facility is closed.";
        public static string BARISDisabledMsg = "BARIS is currently disabled. To enable BARIS, make sure the 'Parts Can Break' option is enabled. See the BARIS KSPedia for details.";
        public static string HighlightBrokenPartsMsg = "Highlight Broken Parts";
        public static string NoBaysMsg = "No vessels under construction.";
        public static string BayEmptyMsg = "Empty";
        public static string TestBenchTitle = "Part Test Bench";
        public static string TestBenchWarning = "Please load or create a collection of breakable parts to test.";
        public static string TestBenchPartCount = "Breakable Parts: ";
        public static string TestBenchScienceCost = "Science Cost: ";
        public static string TestBenchFundsCost = "Funds Cost: ";
        public static string TestBenchExpToAdd = "Add Reliability: ";
        public static string TestBenchReliabilityBefore = "Pre-Simulation Reliability: ";
        public static string TestBenchReliabilityAfter = "Post-Simulation Reliability: ";
        public static string KCTReliabilityLabel = "Reliability after construction: ";
        public static string KCTNoPartsLabel = "Please load or create a vessel.";
        public static string RepairTimeCost = "Repair Time: ";
        public static string RepairTimeDays = "days";
        public static string RepairTimeBroken = "- Broken!";
        public static string RepairTimeProgress = "Solution Research Progress: ";
        public static string TigerRepairSuccessMsg = "Mission Control's Tiger Team has transmitted repair instructions to ";
        public static string TigerRepairSuccessMsgUnmanned = "Mission Control's Tiger Team has found some workarounds for ";
        public static string TigerRepairFailMsg = "Mission Control's Tiger Team could not find a fix for ";
        public static string TigerRepairTryAgainMsg = ". They could try again...";
        public static string TigerTeamNoFunds = "Can't afford to pay the Tiger Team.";
        public static string TigerTeamNoScience = "Not enough research available to the Tiger Team.";
        public static string TigerTeamNoComm = "No CommNet connection to the striken craft.";
        public static string kMothballed = " is mothballed.";
        public static string kReactivated = " has been reactivated.";
        public static string kReactivateTime = "Reactivation in: ";
        public static string kNoReactivateEngineer = "\r\n<color=white>Must wait or have experienced Engineer aboard to reactivate immediately.</color>";
        public static string kMaxQualityReached = " has reached maximum possible quality.";
        #endregion

        #region ToolTips
        public static string RTFMTitle = "RTFM!";
        public static string RTFMMsg = "<color=white>If all else fails, read the directions. If that still doesn't work, follow them. You can find them in the KSPedia under the heading 'BARIS.'</color>";
        public static string RTFMToolTipImagePath = "WildBlueIndustries/000BARIS/Images/kspediaIcon";

        public static bool showedStaticFireTooltip = false;
        public static string StaticFireToolTipImagePath = "WildBlueIndustries/000BARIS/Images/StaticFireToolTip";
        public static string ToolTipStaticFireTitle = "Static Fire Testing";
        public static string ToolTipStaticFireMsg = "<color=white>Engineers report that <b>Flight Experience</b> can also be gained on the ground through static fire tests and will improve vessel <b>Reliability</b>.</color>";

        public static bool showedLoadBeforeFlightTip = false;
        public static string LoadBeforeFlightTitle = "Load Before Flight";
        public static string LoadBeforeFlightMsg = "<color=white>Engineers insist that you <b>Load</b> vessels from the High Bay/Hangar Bay after Vehicle Integration is complete, or the flight will fail!</color>";

        public static bool showedBuyExperienceWithScienceTip = false;
        public static string BuyExpWithScienceTitle = "Science The $#!^ Out Of This";
        public static string BuyExpWithScienceMsg = "<color=white>Scientists and engineers report that by spending <b>Science</b> and/or <b>Funds</b> you can simulate launch conditions to gain <b>Flight Experience</b> and improve vessel <b>Reliability.</b></color>";
        public static string BuyExpWithScienceToolTipImagePath = "WildBlueIndustries/000BARIS/Images/ScienceThis";

        public static bool showedRepairProjectTip = false; //Used for event cards
        public static bool showedTigerTeamToolTip = false; //Used for email repair requests.
        public static string RepairProjectTitle = "Tiger Teams";
        public static string RepairProjectMsg = "<color=white>Attempting to fix failures on the ground costs time, money, and science, and <b>isn't guaranteed to work.</b> But you can make many repair attempts. If the vessel develops another problem while repairs are in progress, the current efforts will be lost and spent currencies returned.</color>";
        public static string RepairProjectImagePath = "WildBlueIndustries/000BARIS/Icons/Wrench";

        #endregion

        #region Housekeeping
        public static List<BARISEventCard> eventCardDeck = null;
        public static AudioSource problemSound = null;
        public static AudioSource problemSoundReverse = null;
        public static AudioSource themeSong = null;
        public static AudioSource eventCardSong = null;
        public static bool isKCTInstalled = false;
        public static bool partsCanBreak;
        public static double SecondsPerDay = 0;
        public Dictionary<Vessel, UnloadedQualitySummary> unloadedQualityCache = new Dictionary<Vessel, UnloadedQualitySummary>();
        public bool isThrottleUp;
        public List<BARISEventResult> cachedQualityModifiers = new List<BARISEventResult>();
        public int workPausedDays;
        public Dictionary<string, BARISRepairProject> repairProjects = new Dictionary<string, BARISRepairProject>();
        public Dictionary<string, MothballedVessel> mothballedVessels = new Dictionary<string, MothballedVessel>();

        static string SoundsFolder = "WildBlueIndustries/000BARIS/Sounds/";
        static bool cachingInProgress;
        static bool revertEditorBays;
        Dictionary<string, EditorBayItem> revertBayItems = new Dictionary<string, EditorBayItem>();
        Dictionary<string, ProtoCrewMember> highestRankingAstronauts = new Dictionary<string, ProtoCrewMember>();
        Dictionary<string, int> highestSkills = new Dictionary<string, int>();
        Vessel lastVessel;
        Dictionary<string, int> partFlightLog = new Dictionary<string, int>();
        Dictionary<Vessel, LoadedQualitySummary> loadedQualityCache = new Dictionary<Vessel, LoadedQualitySummary>();
        List<Vessel> nonBreakableVessels = new List<Vessel>();
        int checksPerDay = 3;
        int timewarpModifier = 0;
        int skippedChecks = 0;
        BARISFocusVesselView focusVesselView;
        bool displayedInstructions = false;
        bool vesselHadAProblem;
        static bool sasWasActive;
        static bool rcsWasActive;
        static bool rcsIsActive;
        static bool sasIsActive;
        int availableVABWorkers = 0;
        int availableSPHWorkers = 0;
        double integrationStartTime = 0;
        double payrollStartTime = 0;
        double eventCardStartTime = 0;
        bool eventCardsEnabled = false;
        int eventCardFrequency = 0;
        VesselsCompletedView completedVesselsView;
        double qualityCheckInterval;
        bool ignoreAlarmDelete;

        internal void Start()
        {
            debugLog("Start called");
            focusVesselView = new BARISFocusVesselView();
            completedVesselsView = new VesselsCompletedView();

            //Init the KAC Wrapper. KAC Wrapper courtey of TriggerAu
            KACWrapper.InitKACWrapper();
            if (KACWrapper.APIReady)
            {
                debugLog("Alarm count: " + KACWrapper.KAC.Alarms.Count);
                KACWrapper.KAC.onAlarmStateChanged += KAC_onAlarmStateChanged;
            }
        }

        public override void OnAwake()
        {
            debugLog("OnAwake called - I am alive!");
            base.OnAwake();
            Instance = this;

            //BARISBridge
            if (BARISBridge.Instance == null)
            {
                BARISBridge.Instance = new BARISBridge();
                BARISBridge.Instance.playModeUpdate = PlayModeUpdate;
            }

            //Check for KCT
            AssemblyLoader.loadedAssemblies.TypeOperation(t =>
            {
                if (t.FullName == "KerbalConstructionTime.KerbalConstructionTimeData")
                {
                    isKCTInstalled = true;
                    debugLog("Found KCT");
                }
            });

            GameEvents.OnGameSettingsApplied.Add(onGameSettingsApplied);
            GameEvents.onCrewOnEva.Add(onCrewOnEva);
            GameEvents.onCrewBoardVessel.Add(onCrewBoardVessel);
            GameEvents.OnKSCFacilityUpgraded.Add(onKSCFacilityUpgraded);
            GameEvents.onVesselCreate.Add(onVesselCreate);
            GameEvents.onVesselDestroy.Add(onVesselDestroy);
            GameEvents.onAsteroidSpawned.Add(onAsteroidSpawned);
            GameEvents.onLevelWasLoadedGUIReady.Add(onLevelLoaded);
            GameEvents.onGameSceneSwitchRequested.Add(onWillSwitchGameScene);
            GameEvents.onVesselChange.Add(onVesselChange);

            showDebug = BARISSettings.DebugMode;
            checksPerDay = BARISSettings.ChecksPerDay;

            //Sound clip
            if (problemSound == null)
            {
                problemSound = gameObject.AddComponent<AudioSource>();
                problemSound.clip = GameDatabase.Instance.GetAudioClip(SoundsFolder + "HoustonWeveHadAProblem");
                problemSound.volume = GameSettings.SHIP_VOLUME;

                problemSoundReverse = gameObject.AddComponent<AudioSource>();
                problemSoundReverse.clip = GameDatabase.Instance.GetAudioClip(SoundsFolder + "HoustonWeveHadAProblemReversed");
                problemSoundReverse.volume = GameSettings.SHIP_VOLUME;

                eventCardSong = gameObject.AddComponent<AudioSource>();
                eventCardSong.clip = GameDatabase.Instance.GetAudioClip(SoundsFolder + "NewsTheme");
                eventCardSong.volume = GameSettings.SHIP_VOLUME;
            }

            //Card deck
            if (eventCardDeck == null)
            {
                eventCardDeck = BARISEventCard.LoadCards();
                if (eventCardDeck == null)
                    debugLog("eventCardDeck is still null!");
                else
                    debugLog("Card count: " + eventCardDeck.Count);
            }

            //Make sure we got the latest settings
            onGameSettingsApplied();
        }

        public void FixedUpdate()
        {
            //First time startup
            if (!displayedInstructions)
            {
                displayedInstructions = true;
                BARISAppButton.instructionsView.SetVisible(true);
            }

            //Make sure to not do anything else if BARIS is disabled.
            if (!partsCanBreak)
                return;

            //Fire the time tick event.
            onTimeTickEvent?.Invoke();

            //Integration bonus timer
            if (BARISSettingsLaunch.LaunchesCanFail && !isKCTInstalled)
                updateIntegrationTimer();

            //Payroll timer
            if ((BARISSettingsLaunch.AstronautsCostFunds || BARISSettingsLaunch.WorkersCostFunds) && HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                updatePayrollTimer();

            //Event card timer
            updateEventCardTimer();

            //Check for SAS state changes and RCS state changes.
            updateFlightControlState();

            //See if it's time to perform a reliability check
            updateReliabilityChecks();

            //Repair project timers
            updateRepairProjects();

            //Update Reactivation timers
            updateReactivationTimers();
        }

        public override void OnLoad(ConfigNode node)
        {
            debugLog("OnLoad called");
            base.OnLoad(node);

            //Load the constant values.
            loadConstants();

            //BARIS enabled state
            if (node.HasValue("partsCanBreak"))
                partsCanBreak = bool.Parse(node.GetValue("partsCanBreak"));

            //Cached quality results
            if (node.HasNode("EVENTRESULT"))
            {
                cachedQualityModifiers.Clear();
                ConfigNode[] cards = node.GetNodes("EVENTRESULT");
                BARISEventResult eventResult;
                foreach (ConfigNode cardNode in cards)
                {
                    eventResult = new BARISEventResult();
                    if (eventResult.Load(cardNode))
                        cachedQualityModifiers.Add(eventResult);
                }
            }

            //Reliability checks that we skipped.
            if (node.HasValue("skippedChecks"))
                skippedChecks = int.Parse(node.GetValue("skippedChecks"));

            //Work stoppage (days)
            if (node.HasValue("workPausedDays"))
                workPausedDays = int.Parse(node.GetValue("workPausedDays"));

            if (node.HasValue("lastUpdateTime"))
                lastUpdateTime = double.Parse(node.GetValue("lastUpdateTime"));
            else
                lastUpdateTime = Planetarium.GetUniversalTime();

            if (node.HasValue("sasWasActive"))
                sasWasActive = bool.Parse(node.GetValue("sasWasActive"));

            if (node.HasValue("rcsWasActive"))
                rcsWasActive = bool.Parse(node.GetValue("rcsWasActive"));

            ConfigNode[] partFlightLogNodes = node.GetNodes("PartFlightLogNode");
            partFlightLog.Clear();
            foreach (ConfigNode logNode in partFlightLogNodes)
            {
                partFlightLog.Add(logNode.GetValue("name"), int.Parse(logNode.GetValue("flights")));
            }

            displayedInstructions = false;
            if (node.HasValue("displayedInstructions"))
                displayedInstructions = bool.Parse(node.GetValue("displayedInstructions"));

            if (node.HasNode("RevertBayItem"))
            {
                revertBayItems.Clear();
                ConfigNode[] bayItems = node.GetNodes("RevertBayItem");
                EditorBayItem editorBayItem;
                foreach (ConfigNode bayItem in bayItems)
                {
                    editorBayItem = new EditorBayItem();
                    editorBayItem.Load(bayItem);
                    revertBayItems.Add(editorBayItem.isVAB.ToString() + editorBayItem.editorBayID.ToString(), editorBayItem);
                }
            }

            if (node.HasNode("EditorBayItem"))
            {
                editorBayItems.Clear();
                ConfigNode[] bayItems = node.GetNodes("EditorBayItem");
                EditorBayItem editorBayItem;
                foreach (ConfigNode bayItem in bayItems)
                {
                    editorBayItem = new EditorBayItem();
                    editorBayItem.Load(bayItem);
                    editorBayItems.Add(editorBayItem.isVAB.ToString() + editorBayItem.editorBayID.ToString(), editorBayItem);
                }
            }

            //If we've reverted from flight then reload the bays.
            if (revertEditorBays)
            {
                revertEditorBays = false;
                string key;
                foreach (EditorBayItem bayItem in revertBayItems.Values)
                {
                    key = bayItem.isVAB.ToString() + bayItem.editorBayID.ToString();
                    debugLog("key: " + key);
                    if (editorBayItems.ContainsKey(key))
                        editorBayItems.Remove(key);

                    editorBayItems.Add(key, bayItem);
                    debugLog("Reverted " + bayItem.vesselName);
                }
                revertBayItems.Clear();
                BARISLaunchButtonManager.editorBayItem = launchedVesselBay;
                launchedVesselBay = null;
            }

            if (node.HasValue("availableVABWorkers"))
                availableVABWorkers = int.Parse(node.GetValue("availableVABWorkers"));
            if (node.HasValue("availableSPHWorkers"))
                availableSPHWorkers = int.Parse(node.GetValue("availableSPHWorkers"));

            if (node.HasValue("integrationStartTime"))
                integrationStartTime = double.Parse(node.GetValue("integrationStartTime"));
            if (node.HasValue("payrollStartTime"))
                payrollStartTime = double.Parse(node.GetValue("payrollStartTime"));
            if (node.HasValue("eventCardStartTime"))
                eventCardStartTime = double.Parse(node.GetValue("eventCardStartTime"));

            if (node.HasValue("showedStaticFireTooltip"))
                showedStaticFireTooltip = bool.Parse(node.GetValue("showedStaticFireTooltip"));
            if (node.HasValue("showedLoadBeforeFlightTip"))
                showedLoadBeforeFlightTip = bool.Parse(node.GetValue("showedLoadBeforeFlightTip"));
            if (node.HasValue("showedBuyExperienceWithScienceTip"))
                showedBuyExperienceWithScienceTip = bool.Parse(node.GetValue("showedBuyExperienceWithScienceTip"));
            if (node.HasValue("showedRepairProjectTip"))
                showedRepairProjectTip = bool.Parse(node.GetValue("showedRepairProjectTip"));
            if (node.HasValue("showedTigerTeamToolTip"))
                showedTigerTeamToolTip = bool.Parse(node.GetValue("showedTigerTeamToolTip"));

            if (node.HasNode("BARISRepairProject"))
            {
                ConfigNode[] repairNodes = node.GetNodes("BARISRepairProject");
                BARISRepairProject repairProject;
                foreach (ConfigNode repairNode in repairNodes)
                {
                    repairProject = new BARISRepairProject();
                    repairProject.Load(repairNode);
                    repairProjects.Add(repairProject.vesselID, repairProject);
                }
            }

            if (node.HasValue("MothballedVessel"))
            {
                ConfigNode[] mothballedNodes = node.GetNodes("MothballedVessel");
                MothballedVessel mothballedVessel;
                foreach (ConfigNode mothballNode in mothballedNodes)
                {
                    mothballedVessel = new MothballedVessel();
                    mothballedVessel.Load(mothballNode);
                    mothballedVessels.Add(mothballedVessel.vesselName, mothballedVessel);
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            debugLog("OnSave called");
            base.OnSave(node);

            //BARIS enabled state
            node.AddValue("partsCanBreak", partsCanBreak);

            //Cached quality modifiers
            foreach (BARISEventResult eventResult in cachedQualityModifiers)
            {
                node.AddNode(eventResult.Save());
            }

            //Reliability checks that we skipped.
            node.AddValue("skippedChecks", skippedChecks);

            //Work stopage days
            node.AddValue("workPausedDays", workPausedDays);

            node.AddValue("lastUpdateTime", lastUpdateTime);
            node.AddValue("sasWasActive", sasWasActive);
            node.AddValue("rcsWasActive", rcsWasActive);

            ConfigNode logNode;
            foreach (string key in partFlightLog.Keys)
            {
                logNode = new ConfigNode("PartFlightLogNode");
                logNode.AddValue("name", key);
                logNode.AddValue("flights", partFlightLog[key]);
                node.AddNode(logNode);
            }

            node.AddValue("displayedInstructions", displayedInstructions);

            foreach (EditorBayItem bayItem in editorBayItems.Values)
            {
                node.AddNode(bayItem.Save());
            }

            foreach (EditorBayItem bayItem in revertBayItems.Values)
            {
                node.AddNode(bayItem.Save(true));
            }

            node.AddValue("availableVABWorkers", availableVABWorkers);
            node.AddValue("availableSPHWorkers", availableSPHWorkers);
            node.AddValue("integrationStartTime", integrationStartTime);
            node.AddValue("payrollStartTime", payrollStartTime);
            node.AddValue("eventCardStartTime", eventCardStartTime);

            if (showedLoadBeforeFlightTip)
                node.AddValue("showedLoadBeforeFlightTip", showedLoadBeforeFlightTip);
            if (showedStaticFireTooltip)
                node.AddValue("showedStaticFireTooltip", showedStaticFireTooltip);
            if (showedBuyExperienceWithScienceTip)
                node.AddValue("showedBuyExperienceWithScienceTip", showedBuyExperienceWithScienceTip);
            if (showedRepairProjectTip)
                node.AddValue("showedRepairProjectTip", showedRepairProjectTip);
            if (showedTigerTeamToolTip)
                node.AddValue("showedTigerTeamToolTip", showedTigerTeamToolTip);

            foreach (BARISRepairProject repairProject in repairProjects.Values)
                node.AddNode(repairProject.Save());

            foreach (MothballedVessel mothballedVessel in mothballedVessels.Values)
                node.AddNode(mothballedVessel.Save());
        }

        public void PlayModeUpdate(bool PartsCanBreak, bool RepairsRequireResources)
        {
            partsCanBreak = PartsCanBreak;
            BARISSettings.PartsCanBreak = partsCanBreak;
            BARISSettings.RepairsRequireResources = RepairsRequireResources;
        }

        public void OnDestroy()
        {
            GameEvents.OnGameSettingsApplied.Remove(onGameSettingsApplied);
            GameEvents.onCrewOnEva.Remove(onCrewOnEva);
            GameEvents.onCrewBoardVessel.Remove(onCrewBoardVessel);
            GameEvents.OnKSCFacilityUpgraded.Remove(onKSCFacilityUpgraded);
            GameEvents.onVesselCreate.Remove(onVesselCreate);
            GameEvents.onVesselDestroy.Remove(onVesselDestroy);
            GameEvents.onAsteroidSpawned.Remove(onAsteroidSpawned);
            GameEvents.onLevelWasLoadedGUIReady.Remove(onLevelLoaded);
            GameEvents.onGameSceneSwitchRequested.Remove(onWillSwitchGameScene);
            GameEvents.onVesselChange.Remove(onVesselChange);
        }

        protected void onWillSwitchGameScene(GameEvents.FromToAction<GameScenes, GameScenes> fromToAction)
        {
            //See if we are reverting flight back to the editor.
            if (fromToAction.from == GameScenes.FLIGHT && fromToAction.to == GameScenes.EDITOR)
            {
                debugLog("Reverting editor bays to editor.");
                revertEditorBays = true;
            }
        }

        protected void onLevelLoaded(GameScenes scene)
        {
            if (scene != GameScenes.FLIGHT && scene != GameScenes.SPACECENTER && scene != GameScenes.TRACKSTATION)
                return;

            //Build the cache if needed
            if (FlightGlobals.VesselsUnloaded.Count > 0 || FlightGlobals.VesselsLoaded.Count > 0)
            {
                if (!cachingInProgress)
                {
                    cachingInProgress = true;
                    try
                    {
                        StartCoroutine(buildSummaryCaches());
                    }
                    catch { }
                }
            }
        }

        protected void onAsteroidSpawned(Vessel asteroid)
        {
            //We ignore asteroids and other space objects
            if (asteroid.vesselType != VesselType.SpaceObject)
                return;
            nonBreakableVessels.Add(asteroid);
        }

        protected void onVesselCreate(Vessel vessel)
        {
            //If this is an asteroid or space object, ignore it.
            if (vessel.vesselType != VesselType.SpaceObject)
                return;
            nonBreakableVessels.Add(vessel);
        }

        protected void onVesselDestroy(Vessel vessel)
        {
            //Remove vessel from the caches
            if (unloadedQualityCache.ContainsKey(vessel))
                unloadedQualityCache.Remove(vessel);
            else if (loadedQualityCache.ContainsKey(vessel))
                loadedQualityCache.Remove(vessel);
            else if (nonBreakableVessels.Contains(vessel))
                nonBreakableVessels.Remove(vessel);
            if (mothballedVessels.ContainsKey(vessel.vesselName))
                mothballedVessels.Remove(vessel.vesselName);
        }

        protected void onKSCFacilityUpgraded(UpgradeableFacility facility, int level)
        {
            int facilityBonus = GetFacilityBonus();
            int astronautFacilityBonus = (int)Math.Round(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex) * BaseFacilityBonus);
            StringBuilder message = new StringBuilder();

            if (facilityBonus > 0)
            {
                message.AppendLine(Localizer.Format(FacilityBonusMsg + facilityBonus));
            }

            if (astronautFacilityBonus > 0)
            {
                message.AppendLine(Localizer.Format(AstronautFacilityBonusMsg + astronautFacilityBonus));
            }

            if (string.IsNullOrEmpty(message.ToString()) == false)
                ScreenMessages.PostScreenMessage(message.ToString(), BARISScenario.MessageDuration, ScreenMessageStyle.UPPER_CENTER);
        }

        protected void onGameSettingsApplied()
        {
            SecondsPerDay = GetSecondsPerDay();
            if (BARISSettings.PartsCanBreak != partsCanBreak)
                lastUpdateTime = Planetarium.GetUniversalTime();

            partsCanBreak = BARISSettings.PartsCanBreak;
            showDebug = BARISSettings.DebugMode;
            checksPerDay = BARISSettings.ChecksPerDay;
            eventCardFrequency = BARISSettingsLaunch.EventCardFrequency;
            eventCardsEnabled = BARISSettingsLaunch.EventCardsEnabled;

            //BARISBridge
            BARISBridge.showDebug = showDebug;
            BARISBridge.PartsCanBreak = partsCanBreak;
            BARISBridge.ConvertersCanFail = BARISBreakableParts.ConvertersCanFail;
            BARISBridge.DrillsCanFail = BARISBreakableParts.DrillsCanFail;
            BARISBridge.RepairsRequireResources = BARISSettings.RepairsRequireResources;
            BARISBridge.RepairsRequireEVA = BARISSettings.RepairsRequireEVA;
            BARISBridge.RepairsRequireSkill = BARISSettings.RepairsRequireSkill;
            BARISBridge.CrewedPartsCanFail = BARISBreakableParts.CrewedPartsCanFail;
            BARISBridge.CommandPodsCanFail = BARISBreakableParts.CommandPodsCanFail;
            BARISBridge.LogAstronautAvertMsg = BARISSettings.LogAstronautAvertMsg;
            BARISBridge.EmailMaintenanceRequests = BARISSettings.EmailMaintenanceRequests;
            BARISBridge.EmailRepairRequests = BARISSettings.EmailRepairRequests;
            BARISBridge.VesselsNeedIntegration = BARISSettingsLaunch.VesselsNeedIntegration;
            BARISBridge.FlightsPerQualityBonus = BARISSettingsLaunch.FlightsPerQualityBonus;
            BARISBridge.RCSCanFail = BARISBreakableParts.RCSCanFail;
            BARISBridge.SASCanFail = BARISBreakableParts.SASCanFail;
            BARISBridge.TanksCanFail = BARISBreakableParts.TanksCanFail;
            BARISBridge.TransmittersCanFail = BARISBreakableParts.TransmittersCanFail;
            BARISBridge.ParachutesCanFail = BARISBreakableParts.ParachutesCanFail;
            BARISBridge.FailuresCanExplode = BARISBreakableParts.FailuresCanExplode;
            BARISBridge.ExplosivePotentialLaunches = BARISBreakableParts.ExplosivePotentialLaunches;
            BARISBridge.EnginesCanFail = BARISBreakableParts.EnginesCanFail;
            BARISBridge.ExplosivePotentialCritical = BARISBreakableParts.ExplosivePotentialCritical;
            BARISBridge.QualityCap = BARISSettings.QualityCap;

            //Update loaded vessels
            if (HighLogic.LoadedSceneIsFlight)
                StartCoroutine(updateLoadedVesselSettings());
        }

        protected IEnumerator<YieldInstruction> updateLoadedVesselSettings()
        {
            int count = FlightGlobals.VesselsLoaded.Count;
            Vessel vessel;
            ModuleQualityControl[] qualityControls;
            for (int index = 0; index < count; index++)
            {
                vessel = FlightGlobals.VesselsLoaded[index];

                //Update quality modules
                qualityControls = vessel.FindPartModulesImplementing<ModuleQualityControl>().ToArray();
                for (int qualityModuleIndex = 0; qualityModuleIndex < qualityControls.Length; qualityModuleIndex++)
                {
                    qualityControls[qualityModuleIndex].UpdateSettings();
                    yield return new WaitForFixedUpdate();
                }
            }

            yield return new WaitForFixedUpdate();
        }

        protected void onVesselChange(Vessel vessel)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                rcsIsActive = FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.RCS];
                sasIsActive = FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.SAS];
                rcsWasActive = rcsIsActive;
                sasWasActive = sasIsActive;
            }
        }

        protected void onCrewBoardVessel(GameEvents.FromToAction<Part, Part> data)
        {
            //Clear our cached data for highest ranking kerbal by skill
            lastVessel = null;
            highestRankingAstronauts.Clear();
            highestSkills.Clear();

            rcsIsActive = FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.RCS];
            sasIsActive = FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.SAS];
            rcsWasActive = rcsIsActive;
            sasWasActive = sasIsActive;
        }

        protected void onCrewOnEva(GameEvents.FromToAction<Part, Part> data)
        {
            //Clear our cached data for highest ranking kerbal by skill
            lastVessel = null;
            highestRankingAstronauts.Clear();
            highestSkills.Clear();
        }

        #endregion

        #region API

        #region Mothball
        public bool IsMothballed(Vessel vessel)
        {
            return mothballedVessels.ContainsKey(vessel.vesselName);
        }

        public void MothballVessel(Vessel vessel)
        {
            if (IsMothballed(vessel))
                return;

            //Set all quality control modules to the mothballed state
            List<ModuleQualityControl> qualityModules = vessel.FindPartModulesImplementing<ModuleQualityControl>();
            int count = qualityModules.Count;
            if (count == 0)
                return;
            for (int index = 0; index < count; index++)
                qualityModules[index].SetMothballState(true);

            //Unregister vessel from the loaded cache
            if (loadedQualityCache.ContainsKey(vessel))
                loadedQualityCache.Remove(vessel);

            //Add to nonbreakables list
            if (!nonBreakableVessels.Contains(vessel))
                nonBreakableVessels.Add(vessel);

            //Register the mothballed vessel
            if (!mothballedVessels.ContainsKey(vessel.vesselName))
            {
                MothballedVessel mothballedVessel = new MothballedVessel(vessel);
                mothballedVessels.Add(mothballedVessel.vesselName, mothballedVessel);
            }

            //Player message
            ScreenMessages.PostScreenMessage(vessel.vesselName + kMothballed, MessageDuration);

            //Save game
            GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.BACKUP);
        }

        public double GetReactivationTimeRemaining(Vessel vessel)
        {
            if (!mothballedVessels.ContainsKey(vessel.vesselName))
                return -1;

            return mothballedVessels[vessel.vesselName].reactivationTimeRemaining;
        }

        public void ReactivateVessel(Vessel vessel)
        {
            //Make sure the vessel has been mothballed.
            if (!IsMothballed(vessel))
                return;

            //Clear the mothball flag
            List<ModuleQualityControl> qualityModules = vessel.FindPartModulesImplementing<ModuleQualityControl>();
            int count = qualityModules.Count;
            if (count == 0)
                return;
            for (int index = 0; index < count; index++)
                qualityModules[index].SetMothballState(false);

            //Unregister vessel from the reactivation list
            if (mothballedVessels.ContainsKey(vessel.vesselName))
                mothballedVessels.Remove(vessel.vesselName);

            //Remove vessel from the nonbreakables list
            if (nonBreakableVessels.Contains(vessel))
                nonBreakableVessels.Remove(vessel);

            //Player message
            ScreenMessages.PostScreenMessage(vessel.vesselName + kReactivated, MessageDuration);
        }

        protected void updateReactivationTimers()
        {
            MothballedVessel[] vessels = mothballedVessels.Values.ToArray();
            for (int index = 0; index < vessels.Length; index++)
                vessels[index].UpdateTimer();
        }

        #endregion

        #region Repair Project
        public BARISRepairProject GetRepairProject(Vessel unloadedVessel)
        {
            if (repairProjects.ContainsKey(unloadedVessel.id.ToString()))
                return repairProjects[unloadedVessel.id.ToString()];

            return null;
        }

        public void CancelRepairProject(Vessel unloadedVessel)
        {
            string vesselID = unloadedVessel.id.ToString();
            BARISRepairProject repairProject = GetRepairProject(unloadedVessel);

            //If the project doesn't exist in the registry then we're done.
            if (repairProject == null)
                return;

            //Refund funds and science
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
                ResearchAndDevelopment.Instance.AddScience(repairProject.repairCostScience, TransactionReasons.Any);
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                Funding.Instance.AddFunds(repairProject.repairCostFunds, TransactionReasons.Any);

            //Remove the tiger team repair from the registry
            repairProjects.Remove(vesselID);
        }

        public void RegisterRepairProject(BARISRepairProject repairProject)
        {
            if (repairProjects.ContainsKey(repairProject.vesselID))
                repairProjects.Remove(repairProject.vesselID);

            repairProjects.Add(repairProject.vesselID, repairProject);
        }
        #endregion

        #region Event Cards
        /// <summary>
        /// Plays an event card that's been drawn from the deck. This is exposed for debugging purposes.
        /// </summary>
        /// <param name="eventCard">A BARISEventCard to play.</param>
        public void PlayCard(BARISEventCard eventCard)
        {
            BARISEventCardView cardView = new BARISEventCardView();

            cardView.WindowTitle = eventCard.name;
            cardView.description = eventCard.ApplyResults();
            cardView.imagePath = eventCard.imageFilePath;

            if (!string.IsNullOrEmpty(cardView.description))
                cardView.SetVisible(true);
        }
        #endregion

        #region Integration
        /// <summary>
        /// Tells all editor bays to recalcuate their flight bonuses.
        /// </summary>
        public void UpdateEditorBayFlightBonuses()
        {
            foreach (EditorBayItem bayItem in editorBayItems.Values)
                bayItem.RecalculateTotalQuality();
        }

        /// <summary>
        /// Determines the number of integration points based upon worker count and VAB/SPH
        /// </summary>
        /// <param name="workerCount">Number of workers</param>
        /// <param name="isVAB">true if the workers are working in the VAB</param>
        /// <returns>Int with the number of integration points.</returns>
        public int GetWorkerProductivity(int workerCount, bool isVAB)
        {
            int integrationPoints = 0;
            float sphLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.SpaceplaneHangar);
            float vabLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.VehicleAssemblyBuilding);

            //Get base level of integration
            integrationPoints = workerCount * IntegrationPerWorker;

            //Add facility bonus
            if (isVAB)
                integrationPoints += Mathf.RoundToInt(integrationPoints * vabLevel * FacilityIntegrationMultiplier);
            else
                integrationPoints += Mathf.RoundToInt(integrationPoints * sphLevel * FacilityIntegrationMultiplier);

            //In career mode, reputation matters.
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                if (Reputation.CurrentRep != 0)
                    integrationPoints = Mathf.RoundToInt((float)integrationPoints * (1.0f + (Reputation.CurrentRep / Reputation.RepRange)));
            }

            return integrationPoints;
        }

        /// <summary>
        /// Loops through all the editor bays and updates their base quality values by the number of workers
        /// performing the vehicle integration.
        /// </summary>
        public void UpdateBuildTime()
        {
            //if KAC is installed then we rely upon its timers.
            if (KACWrapper.AssemblyExists && KACWrapper.APIReady)
                return;

            //If work has been paused then we're done.
            if (workPausedDays > 0)
            {
                workPausedDays -= 1;
                return;
            }

            integrationStartTime = Planetarium.GetUniversalTime();
            string facility;
            StringBuilder completedVessels = new StringBuilder();
            string vesselNames = string.Empty;
            int integrationPoints = 0;

            //Add a number of integration points equal to the number of workers performing the integration,
            //Or as many as we can.
            foreach (EditorBayItem bayItem in editorBayItems.Values)
            {
                //Ignore empty bays.
                if (string.IsNullOrEmpty(bayItem.vesselName))
                    continue;

                //Calculate integration points
                integrationPoints = GetWorkerProductivity(bayItem.workerCount, bayItem.isVAB);

                if (integrationPoints < bayItem.totalIntegrationToAdd)
                {
                    bayItem.totalIntegrationAdded += integrationPoints;
                    bayItem.totalIntegrationToAdd -= integrationPoints;
                }

                else //We've reached the max. Add to the list of completed projects
                {
                    bayItem.totalIntegrationAdded += bayItem.totalIntegrationToAdd;
                    bayItem.totalIntegrationToAdd = 0;

                    //KAC not installed, add the completed vessel to the list.
                    if (!KACWrapper.AssemblyExists)
                    {
                        facility = bayItem.isVAB == true ? "VAB: " : "SPH: ";
                        completedVessels.AppendLine(facility + bayItem.vesselName);
                    }
                }
            }

            //Inform player
            vesselNames = completedVessels.ToString();
            if (!string.IsNullOrEmpty(vesselNames))
            {
                completedVesselsView.vesselNames = vesselNames;
                completedVesselsView.SetVisible(true);
            }
        }

        void KAC_onAlarmStateChanged(KACWrapper.KACAPI.AlarmStateChangedEventArgs args)
        {
            foreach (EditorBayItem bayItem in editorBayItems.Values)
            {
                //Ignore empty bays.
                if (string.IsNullOrEmpty(bayItem.vesselName))
                    continue;
                if (string.IsNullOrEmpty(bayItem.KACAlarmID))
                    continue;

                //If the alarm isn't the one we're interested in, then continue.
                if (bayItem.KACAlarmID != args.alarm.ID)
                    continue;

                //Complete integration if the alarm was triggered.
                if (args.eventType == KACWrapper.KACAPI.KACAlarm.AlarmStateEventsEnum.Triggered)
                {
                    bayItem.totalIntegrationAdded += bayItem.totalIntegrationToAdd;
                    bayItem.totalIntegrationToAdd = 0;
                    bayItem.isCompleted = true;
                }

                //Clear vessel integration if the alarm was deleted.
                else if (args.eventType == KACWrapper.KACAPI.KACAlarm.AlarmStateEventsEnum.Deleted && !ignoreAlarmDelete && !bayItem.isCompleted)
                {
                    bayItem.Clear();
                }
            }            
        }

        /// <summary>
        /// Immediately pays workers for one standard pay period. This is exposed for diagnostic purposes.
        /// </summary>
        public void PayWorkers()
        {
            payrollStartTime = Planetarium.GetUniversalTime();

            //Deduct the worker payroll, If the player can't afford the worker payroll, then dismiss the workers.
            if (BARISSettingsLaunch.WorkersCostFunds)
            {
                int totalWorkers = GetAvailableWorkers(true) + GetAvailableWorkers(false);
                int totalWorkerCost = totalWorkers * PayrollPerWorker;

                //If we can afford them then deduct their payroll
                if (Funding.CanAfford((float)totalWorkerCost))
                {
                    Funding.Instance.AddFunds(-(float)totalWorkerCost, TransactionReasons.Any);
                }

                else //The workers union quits!
                {
                    //Clear the available workers
                    SetAvailableWorkers(0, true);
                    SetAvailableWorkers(0, false);
                    foreach (EditorBayItem bayItem in editorBayItems.Values)
                        bayItem.workerCount = 0;

                    //Inform the player that the Workers Union has quit.
                    string message = Localizer.Format(WorkersQuitMessage);
                    LogPlayerMessage(message);
                }
            }

            //Deduct the astronaut payroll
            if (BARISSettingsLaunch.AstronautsCostFunds)
            {
                KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;
                int astronautCount = roster.GetAssignedCrewCount() + roster.GetAvailableCrewCount();
                int availableAstronauts = roster.GetAvailableCrewCount();
                int astronautCost = astronautCount * PayrollPerAstronaut;

                //Deduct the payroll if the player can afford the astronauts
                if (Funding.CanAfford((float)astronautCost))
                {
                    Funding.Instance.AddFunds(-(float)astronautCost, TransactionReasons.Any);
                }

                else
                {
                    while (!Funding.CanAfford((float)astronautCost) && availableAstronauts > 0)
                    {
                        //Reduce the payroll amount by one astronaut.
                        astronautCost -= PayrollPerAstronaut;

                        //Redunce the number of available astronauts
                        availableAstronauts -= 1;

                        //Dismiss a non-veteran astronaut from the available roster (if any)
                        bool astronautHasQuit = false;
                        foreach (ProtoCrewMember astronaut in roster.Crew)
                        {
                            if (astronaut.veteran == false && astronaut.rosterStatus == ProtoCrewMember.RosterStatus.Available)
                            {
                                //Dismiss the astronaut
                                roster.SackAvailable(astronaut);
                                astronautHasQuit = true;

                                //Inform the player
                                string message = astronaut.name + Localizer.Format(AstronautsQuitMessage);
                                LogPlayerMessage(message);
                            }
                        }

                        //If we didn't find an astronaut to dismiss then there's nothing else we can do
                        if (!astronautHasQuit)
                            break;

                        //If we can afford the payroll then we're good.
                        if (Funding.CanAfford((float)astronautCost))
                        {
                            Funding.Instance.AddFunds(-(float)astronautCost, TransactionReasons.Any);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the maximum number of quality points allowed per part for the current level of the facility.
        /// These quality points are added to a part's quality rating along with the part type's flight experience
        /// to give the part's overall quality rating.
        /// </summary>
        /// <param name="isVAB">True if the request is for the VAB, false if it's for the SPH.</param>
        /// <returns>An int containing number of high bays that the facility support.</returns>
        public int GetIntegrationCap(bool isVAB)
        {
            float facilityLevel;
            if (isVAB)
                facilityLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.VehicleAssemblyBuilding);

            else
                facilityLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.SpaceplaneHangar);

            if (facilityLevel >= 1)
                return Level3IntegrationCap;
            else if (facilityLevel >= 0.5f)
                return Level2IntegrationCap;
            else
                return Level1IntegrationCap;
        }

        /// <summary>
        ///  Returns the number of workers engaged in a vehicle integration.
        /// </summary>
        /// <param name="isVAB">True if the request is for VAB workers, false if the request is for SPH workers.</param>
        /// <returns>An Int containing the number of workers currently engaged in an integration project.</returns>
        public int GetWorkersWorking(bool isVAB)
        {
            int workers = 0;
            EditorBayItem[] bays = editorBayItems.Values.ToArray();

            for (int index = 0; index < bays.Length; index++)
                if (bays[index].isVAB == isVAB)
                    workers += bays[index].workerCount;

            return workers;
        }

        public int GetMaxWorkers(bool isVAB)
        {
            if (isVAB)
                return MaxHighBays * MaxWorkersPerBay;
            else
                return MaxHangarBays * MaxWorkersPerBay;
        }

        /// <summary>
        /// Returns the maximum total number of workers allowed for the facility's current upgrade level.
        /// </summary>
        /// <param name="isVAB">True if the request is for VAB workers, false if the request is for SPH workers.</param>
        /// <returns>An Int containing the maximum number of allowed for the facility's current ugprade level.</returns>
        public int GetMaxAvailableWorkers(bool isVAB)
        {
            //Get facility level
            float facilityLevel;
            if (isVAB)
                facilityLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.VehicleAssemblyBuilding);

            else
                facilityLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.SpaceplaneHangar);

            //Set max workers
            int maxAvailableWorkers = (int)((float)GetMaxWorkers(isVAB) * facilityLevel);
            if (maxAvailableWorkers < MinWorkersPerFacility)
                maxAvailableWorkers = MinWorkersPerFacility;

            return maxAvailableWorkers;
        }

        /// <summary>
        /// Sets the current number of workers that aren't assigned to a project.
        /// The value cannot exceed the maximum number of workers allowed for the facility's upgrade level.
        /// </summary>
        /// <param name="availableWorkers">The new available number of workers.</param>
        /// <param name="isVAB">True if the request is for VAB workers, false if the request is for SPH workers.</param>
        public void SetAvailableWorkers(int availableWorkers, bool isVAB)
        {
            //Get the max available workers
            int maxAvailableWorkers = GetMaxAvailableWorkers(isVAB);

            //Set the new value
            int workers = availableWorkers;
            if (workers > maxAvailableWorkers)
                workers = maxAvailableWorkers;

            if (isVAB)
                availableVABWorkers = workers;
            else
                availableSPHWorkers = workers;
//            GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.BACKUP);
        }

        /// <summary>
        /// Returns the workers that are currently engaged in a vehicle integration project.
        /// </summary>
        /// <param name="bayItem">The EditorBayItem to raid for workers.</param>
        public void ReturnWorkers(EditorBayItem bayItem)
        {
            int availableWorkers = GetAvailableWorkers(bayItem.isVAB);
            availableWorkers += bayItem.workerCount;
            bayItem.workerCount = 0;
            SetAvailableWorkers(availableWorkers, bayItem.isVAB);
        }

        /// <summary>
        /// Returns the current number of workers that aren't assigned to a project.
        /// </summary>
        /// <param name="isVAB">True if the request is for VAB workers, false if the request is for SPH workers.</param>
        /// <returns>An integer containing the number of available workers.</returns>
        public int GetAvailableWorkers(bool isVAB)
        {
            if (isVAB)
                return availableVABWorkers;
            else
                return availableSPHWorkers;
        }

        /// <summary>
        /// Returns a list of EditorBayItem objects that have an ongoing vehicle integration effort.
        /// </summary>
        /// <param name="isVAB">True if the request is for VAB editor bays, false if the request is for SPH editor bays.</param>
        /// <returns>A list of EditorBayItem objects that have a vehicle integration effort in progress.</returns>
        public List<EditorBayItem> GetBuildsInProgress(bool isVAB)
        {
            List<EditorBayItem> buildsInProgress = new List<EditorBayItem>();

            foreach (EditorBayItem bayItem in editorBayItems.Values)
            {
                if (bayItem.isVAB == isVAB && !string.IsNullOrEmpty(bayItem.vesselName))
                    buildsInProgress.Add(bayItem);
            }

            return buildsInProgress;
        }

        /// <summary>
        /// Returns the maximum number of High Bays/Hangar Bays allowed for the current upgrade level of the facility.
        /// The number of bays actually in use depends upon whether or not the player has purchased the bay.
        /// </summary>
        /// <param name="isVAB">True if the request is for the VAB, false if it's for the SPH.</param>
        /// <returns>An int containing the maximum number of bays allowed.</returns>
        public int GetMaxBays(bool isVAB)
        {
            float facilityLevel;
            if (isVAB)
                facilityLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.VehicleAssemblyBuilding);

            else
                facilityLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.SpaceplaneHangar);

            if (facilityLevel >= 1)
                return Level3BayCount;
            else if (facilityLevel >= 0.5f)
                return Level2BayCount;
            else
                return Level1BayCount;
        }

        /// <summary>
        /// Transfers an editor bay from the active list to the flight revert list.
        /// This list is used when you revert a flight back to the VAB/SPH.
        /// </summary>
        /// <param name="bayItem">The EditorBayItem to remove.</param>
        public void TranferBayToRevertList(EditorBayItem bayItem)
        {
            string key = bayItem.isVAB.ToString() + bayItem.editorBayID.ToString();

            if (editorBayItems.ContainsKey(key))
            {
                editorBayItems.Remove(key);
                if (revertBayItems.ContainsKey(key))
                    revertBayItems.Remove(key);
                revertBayItems.Add(key, bayItem);
            }
        }

        /// <summary>
        /// Clears the editor bays from the revert list. This is typically done when
        /// A vessel was created or loaded, but not from one of the editor bays.
        /// </summary>
        public void ClearRevertList()
        {
            revertBayItems.Clear();
        }

        /// <summary>
        /// Removes an EditorBayItem from the list. Typically this is done when the player cancels a vehicle integration.
        /// </summary>
        /// <param name="bayItem">The EditorBayItem to remove.</param>
        public void RemoveEditorBay(EditorBayItem bayItem)
        {
            string key = bayItem.isVAB.ToString() + bayItem.editorBayID.ToString();

            if (editorBayItems.ContainsKey(key))
            {
                editorBayItems.Remove(key);
            }
        }

        /// <summary>
        /// Creates a new EditorBayItem with the specified ID and isVAB flag.
        /// </summary>
        /// <param name="isVAB">True if the editor bay comes from the VAB, false if it comes from the SPH.</param>
        /// <param name="bayID">ID number of the editor bay.</param>
        /// <returns>An EditorBayItem containing information about the vessel undergoing integration.</returns>
        public EditorBayItem AddEditorBay(bool isVAB, int bayID)
        {
            string key = isVAB.ToString() + bayID.ToString();

            if (editorBayItems.ContainsKey(key))
            {
                return editorBayItems[key];
            }
            else
            {
                EditorBayItem editorBayItem = new EditorBayItem();
                editorBayItem.isVAB = isVAB;
                editorBayItem.editorBayID = bayID;
                return editorBayItem;
            }
        }

        /// <summary>
        /// Returns the EditorBayItem with the specified ID and isVAB flag.
        /// </summary>
        /// <param name="isVAB">True if the editor bay comes from the VAB, false if it comes from the SPH.</param>
        /// <param name="bayID">ID number of the editor bay.</param>
        /// <returns>An EditorBayItem containing information about the vessel undergoing integration.</returns>
        public EditorBayItem GetEditorBay(bool isVAB, int bayID)
        {
            string key = isVAB.ToString() + bayID.ToString();

            if (editorBayItems.ContainsKey(key))
                return editorBayItems[key];

            return null;
        }

        /// <summary>
        /// Saves the EditorBayItem into the database.
        /// </summary>
        /// <param name="bayItem">An EditorBayItem containing information about the vessel undergoing integration.</param>
        public void SetEditorBay(EditorBayItem bayItem)
        {
            string key = bayItem.isVAB.ToString() + bayItem.editorBayID.ToString();

            if (editorBayItems.ContainsKey(key))
                editorBayItems[key] = bayItem;

            else
                editorBayItems.Add(key, bayItem);
        }
        #endregion

        #region Sounds
        /// <summary>
        /// Plays the "Houston we've had a problem" sound in reverse.
        /// </summary>
        public void PlayProblemSoundReversed()
        {
            if (problemSoundReverse == null)
                return;

            if (problemSoundReverse.isPlaying)
                return;

            problemSoundReverse.Play();
        }

        /// <summary>
        /// Plays the "Houston we've had a problem" sound.
        /// </summary>
        public void PlayProblemSound()
        {
            if (problemSound == null)
                return;

            if (problemSound.isPlaying)
                return;

            problemSound.Play();
        }

        /// <summary>
        /// Plays the BARIS theme song by Brian Langsbard
        /// </summary>
        public void PlayThemeSong()
        {
            if (themeSong == null)
            {
                themeSong = gameObject.AddComponent<AudioSource>();
                themeSong.clip = GameDatabase.Instance.GetAudioClip(SoundsFolder + "RISTheme");
                themeSong.volume = GameSettings.SHIP_VOLUME;
            }
            themeSong.Play();
        }
        #endregion

        #region Quality
        /// <summary>
        /// Returns the maximum Mean Time Between Failures allowed for the part.
        /// </summary>
        /// <param name="mtbf">Base MTBF rating.</param>
        /// <param name="mtbfCap">Local MTBF cap for the part.</param>
        /// <param name="repairCount">How many times the part has been repaired</param>
        /// <param name="mtbfRepairMultiplier">Multiplier for MTBF; applies when repairCount > 0.</param>
        /// <param name="mtbfBonus">Bonus MTBF gained through flight experience.</param>
        /// <returns>A double containing the max MTBF possible for the part.</returns>
        public static double GetMaxMTBF(double mtbf, double mtbfCap, int repairCount, float mtbfRepairMultiplier, double mtbfBonus)
        {
            double maxMTBF = mtbf + mtbfBonus;

            //Apply the MTBF cap
            if (BARISSettingsLaunch.LaunchesCanFail)
            {
                //If the part overrides the global cap, then use the part's cap.
                if (maxMTBF > mtbfCap && mtbfCap > -1)
                    maxMTBF = mtbfCap;

                //Apply the global quality cap if needed.
                else if (maxMTBF > BARISScenario.MTBFCap)
                    maxMTBF = BARISScenario.MTBFCap;
            }

            else
            {
                maxMTBF = BARISScenario.MTBFCap;
            }

            //Reduce max mtbf
            if (BARISSettings.PartsWearOut && repairCount > 0)
                maxMTBF *= (mtbfRepairMultiplier * repairCount);

            return maxMTBF;
        }

        /// <summary>
        /// Returns the maximum possibly quality rating of the part.
        /// </summary>
        /// <param name="quality">Base quality rating of the part.</param>
        /// <param name="integrationBonus">Bonus quality from part vehicle integration.</param>
        /// <param name="flightExperienceBonus">Bonus quality from flight experience.</param>
        /// <param name="qualityCap">Per-part quality cap</param>
        /// <param name="repairCount">Number of times the part has been repaired.</param>
        /// <returns>An int containing the max quality rating possible for the part.</returns>
        public static int GetMaxQuality(int quality, int integrationBonus, int flightExperienceBonus, int qualityCap, int repairCount)
        {
            //Initial value depends upon the base quality rating, the bonus for time spent in a vehicle integration facility, and the flight experience.
            int maxQuality = quality + integrationBonus + flightExperienceBonus;

            //If the part overrides the global quality cap, then use the part's quality cap.
            if (maxQuality > qualityCap && qualityCap > -1)
                maxQuality = qualityCap;

            //Apply the global quality cap if needed.
            else if ((maxQuality > BARISSettings.QualityCap) || !BARISSettingsLaunch.VesselsNeedIntegration)
                maxQuality = BARISSettings.QualityCap;

            //Adjust for the number of times the part has been repaired.
            if (BARISSettings.PartsWearOut)
                maxQuality = maxQuality - (repairCount * BARISScenario.QualityLossPerRepairs);

            return maxQuality;
        }

        /// <summary>
        /// Retrieves the bonus gained based upon the astronaut facility's level
        /// </summary>
        /// <returns>The bonus applied to quality checks for the astronaut complex</returns>
        public int GetAstronautFacilityBonus()
        {
            int astronautFacilityBonus = (int)Math.Round(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex) * BaseFacilityBonus);

            return astronautFacilityBonus;
        }

        /// <summary>
        /// Retrieves the bonus gained based upon the highest facility level amongst the VAB and SPH.
        /// </summary>
        /// <returns>The best bonus for the VAB/SPH level; used in quality checks.</returns>
        public int GetFacilityBonus()
        {
            //Quality can be improved with upgrades to the VAB or SPH.
            //Take the highest tier of either one to determine the bonus.
            float sphLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.SpaceplaneHangar);
            float vabLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.VehicleAssemblyBuilding);

            //Use SPH level
            if (sphLevel > vabLevel)
                return (int)Math.Round(sphLevel * BaseFacilityBonus);

            //Use VAB level
            else
                return (int)Math.Round(vabLevel * BaseFacilityBonus);
        }

        /// <summary>
        /// Removes all flight experience entries.
        /// </summary>
        public void ClearFlightExperienceLog()
        {
            partFlightLog.Clear();
        }

        /// <summary>
        /// Records a successful flight by the supplied part. Success in this case is defined by the part making its quality check during a staging event. Flight experience is applied to the quality check.
        /// </summary>
        /// <param name="part">The part that has gained flight experience</param>
        public void RecordFlightExperience(Part part, int experiencePoints = 1)
        {
            if (partFlightLog.ContainsKey(part.partInfo.title))
            {
                int flights = partFlightLog[part.partInfo.title];
                flights += experiencePoints;
                partFlightLog[part.partInfo.title] = flights;
            }

            else
            {
                partFlightLog.Add(part.partInfo.title, experiencePoints);
            }

            debugLog(part.partInfo.title + " has " + partFlightLog[part.partInfo.title] + " flights.");
        }

        /// <summary>
        /// Returns the experience bonus of the part. This bonus is applied during quality checks.
        /// </summary>
        /// <param name="part">The part to check for flight experience.</param>
        /// <returns>The experience bonus gained by the part.</returns>
        public int GetFlightBonus(Part part)
        {
            string partTitle = part.partInfo.title;

            return GetFlightBonus(partTitle);
        }

        /// <summary>
        /// Returns the experience bonus of the part. This bonus is applied during quality checks.
        /// </summary>
        /// <param name="partTitle">A string containing the title of the part.</param>
        /// <returns>The experience bonus gained by the part.</returns>
        public int GetFlightBonus(string partTitle)
        {
            if (!partFlightLog.ContainsKey(partTitle))
                return 0;

            return GetFlightBonus(partFlightLog[partTitle]);
        }

        /// <summary>
        /// Returns the experience bonus calculated from the number of flights. This bonus is applied during quality checks.
        /// </summary>
        /// <param name="numberOfFlights">An integeger containing the number of flights to compute the bonus for.</param>
        /// <returns>An integer representing the experience bonus.</returns>
        public int GetFlightBonus(int numberOfFlights)
        {
            int flightBonus = numberOfFlights / BARISSettingsLaunch.FlightsPerQualityBonus;

            //If launches can't fail then just give a default
            if (BARISSettingsLaunch.LaunchesCanFail == false)
            {
                flightBonus = DefaultFlightBonus;
            }

            return flightBonus;
        }

        /// <summary>
        /// Returns the number of flights that a part type has successfully flown.
        /// </summary>
        /// <param name="partTitle">A string containing the title of the part.</param>
        /// <returns>An integer with the number of flights that the part type has flown successfully.</returns>
        public int GetFlightCount(string partTitle)
        {
            if (!partFlightLog.ContainsKey(partTitle))
                return 0;

            return partFlightLog[partTitle];
        }

        /// <summary>
        /// Returns the Mean Time Between Failures bonus for the supplied part type. It is based upon flight experience and facility bonus
        /// </summary>
        /// <param name="part">A Part to check for MTBF bonus.</param>
        /// <returns>A double with the bonus hours.</returns>
        public double GetMTBFBonus(Part part, double mtbf)
        {
            return GetMTBFBonus(part.partInfo.title, mtbf);
        }

        /// <summary>
        /// Returns the Mean Time Between Failures bonus for the supplied part type. It is based upon flight experience and facility bonus
        /// </summary>
        /// <param name="partTitle">A string naming the part type to check for MTBF bonus.</param>
        /// <returns>A double with the bonus hours.</returns>
        public double GetMTBFBonus(string partTitle, double mtbf)
        {
            double mtbfBonus = MTBFFacilityBonus * ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.ResearchAndDevelopment) * mtbf;
            int flightBonus = GetFlightCount(partTitle) / BARISSettingsLaunch.FlightsPerQualityBonus;

            mtbfBonus += (double)flightBonus * MTBFPerQualityBonus;

            return mtbfBonus;
        }

        /// <summary>
        /// This is a diagnostics method that's used to modify the part's quality. The part cannot exceed the quality cap.
        /// The modifier is applied to every breakable part in the vessel.
        /// </summary>
        /// <param name="vessel">The vessel to apply the modifier to.</param>
        /// <param name="qualityModifier">The value that will increase or decrease a part's quality rating.</param>
        public void ModifyPartQuality(Vessel vessel, int qualityModifier)
        {
            Part[] vesselParts = vessel.parts.ToArray();
            ModuleQualityControl qualityControl = null;
            int maxQuality = 0;

            for (int index = 0; index < vesselParts.Length; index++)
            {
                qualityControl = vesselParts[index].FindModuleImplementing<ModuleQualityControl>();

                if (qualityControl != null)
                {
                    qualityControl.integrationBonus += qualityModifier;
                    if (qualityControl.integrationBonus < 0)
                        qualityControl.integrationBonus = 0;

                    //Recalculate max quality
                    maxQuality = qualityControl.GetMaxQuality();
                    qualityControl.currentQuality = maxQuality;
                    debugLog(vesselParts[index].partInfo.title + " New max quality: " + maxQuality);

                    //Update the quality display
                    qualityControl.qualityDisplay = BARISScenario.GetConditionSummary(qualityControl.currentMTBF, qualityControl.MaxMTBF, qualityControl.currentQuality, maxQuality);
                }
            }
        }

        /// <summary>
        /// This diagnostic method is used to set the quality rating on each breakable part in the vessel to zero.
        /// </summary>
        /// <param name="vessel">The vessel whose breakable parts will be set to zero.</param>
        public void ZeroPartQuality(Vessel vessel)
        {
            Part[] vesselParts = vessel.parts.ToArray();
            ModuleQualityControl qualityControl = null;

            for (int index = 0; index < vesselParts.Length; index++)
            {
                qualityControl = vesselParts[index].FindModuleImplementing<ModuleQualityControl>();

                if (qualityControl != null)
                {
                    //Recalculate max quality
                    qualityControl.currentQuality = 0;

                    //Update the quality display
                    qualityControl.qualityDisplay = BARISScenario.GetConditionSummary(qualityControl.currentMTBF, qualityControl.MaxMTBF, qualityControl.currentQuality, qualityControl.MaxQuality);
                }
            }
        }

        /// <summary>
        /// This diagnostic method is used to set the Mean Time Between Failure (MTBF) to zero. All breakable parts will have its MTBF set to zero.
        /// </summary>
        /// <param name="vessel">The vessel whose breakable parts will have their MTBF set to zero.</param>
        public void ZeroPartMTBF(Vessel vessel)
        {
            Part[] vesselParts = vessel.parts.ToArray();
            ModuleQualityControl qualityControl = null;

            for (int index = 0; index < vesselParts.Length; index++)
            {
                qualityControl = vesselParts[index].FindModuleImplementing<ModuleQualityControl>();

                if (qualityControl != null)
                {
                    //Recalculate max quality
                    qualityControl.currentMTBF = 0;
                }
            }
        }

        /// <summary>
        /// This diagnostic method is used to fix all breakable parts in the vessel. Thier quality ratings will be restored as will their MTBF ratings.
        /// </summary>
        /// <param name="vessel">The vessel whose breakable parts will be fixed.</param>
        public void FixAllParts(Vessel vessel)
        {
            Part[] vesselParts = vessel.parts.ToArray();
            ModuleQualityControl qualityControl = null;

            for (int index = 0; index < vesselParts.Length; index++)
            {
                qualityControl = vesselParts[index].FindModuleImplementing<ModuleQualityControl>();

                if (qualityControl != null)
                {
                    qualityControl.DeclarePartFixed();
                }
            }
        }

        /// <summary>
        /// Retrieves all the breakable parts in the vessel.
        /// </summary>
        /// <param name="vessel">The vessel to search for breakable parts.</param>
        /// <returns>A list of breakable parts and the breakable modules.</returns>
        public List<BreakablePart> GetBreakableParts(Vessel vessel = null)
        {
            List<BreakablePart> breakableParts = new List<BreakablePart>();
            BreakablePart breakablePart;
            bool hasBreakableParts;

            if (vessel == null)
                vessel = FlightGlobals.ActiveVessel;

            //Go through each part and find all the breakable part modules
            foreach (Part part in vessel.parts)
            {
                breakablePart = new BreakablePart();
                hasBreakableParts = false;

                //Part
                breakablePart.part = part;

                //Quality Control
                breakablePart.qualityControl = part.FindModuleImplementing<ModuleQualityControl>();

                //Harvesters
                breakablePart.breakableHarvesters = part.FindModulesImplementing<ModuleBreakableHarvester>();
                if (breakablePart.breakableHarvesters.Count > 0)
                    hasBreakableParts = true;

                //Asteroid drills
                breakablePart.breakableDrills = part.FindModulesImplementing<ModuleBreakableAsteroidDrill>();
                if (breakablePart.breakableDrills.Count > 0)
                    hasBreakableParts = true;

                //Converters
                breakablePart.breakableConverters = part.FindModulesImplementing<ModuleBreakableConverter>();
                if (breakablePart.breakableConverters.Count > 0)
                    hasBreakableParts = true;

                //Engine
                breakablePart.breakableEngine = part.FindModuleImplementing<ModuleBreakableEngine>();
                if (breakablePart.breakableEngine != null)
                    hasBreakableParts = true;

                //Fuel Tank
                breakablePart.breakableFuelTank = part.FindModuleImplementing<ModuleBreakableFuelTank>();
                if (breakablePart.breakableFuelTank != null)
                    hasBreakableParts = true;

                //RCS
                breakablePart.breakableRCS = part.FindModuleImplementing<ModuleBreakableRCS>();
                if (breakablePart.breakableRCS != null)
                    hasBreakableParts = true;

                //SAS
                breakablePart.breakableSAS = part.FindModuleImplementing<ModuleBreakableSAS>();
                if (breakablePart.breakableSAS != null)
                    hasBreakableParts = true;

                //Transmitter
                breakablePart.breakableTransmitter = part.FindModuleImplementing<ModuleBreakableTransmitter>();
                if (breakablePart.breakableTransmitter != null)
                    hasBreakableParts = true;

                //If we found a breakable part module then add the breakable part to the list.
                if (hasBreakableParts)
                    breakableParts.Add(breakablePart);
            }

            debugLog("Found " + breakableParts.Count + " breakable parts.");
            return breakableParts;
        }

        /// <summary>
        /// Returns a user-friendly string representing the condition of the breakable part.
        /// </summary>
        /// <param name="currentMTBF">Current MTBF of the breakable part.</param>
        /// <param name="maxMTBF">Maximum possible MTBF of the breakable part.</param>
        /// <param name="quality">Current quality of the breakable part.</param>
        /// <param name="maxQuality">Maximum possible quality of the breakable part.</param>
        /// <returns>A string representing the condition of the breakable part.</returns>
        public static string GetConditionSummary(double currentMTBF, double maxMTBF, int quality, int maxQuality)
        {
            double ratio = currentMTBF / maxMTBF;
            string summaryLabel = "";

            if (quality == 0)
                summaryLabel = ConditionBrokenLabel;

            else if (currentMTBF <= 0)
                summaryLabel = ConditionMaintenanceLabel;

            else if (ratio <= MaxPoorQuality)
                summaryLabel = ConditionPoorLabel;

            else if (ratio <= MaxFairQuality)
                summaryLabel = ConditionFairLabel;

            else if (ratio <= MaxGoodQuality)
                summaryLabel = ConditionGoodLabel;

            else
                summaryLabel = ConditionExcellentLabel;

            if (quality > 0)
                summaryLabel = summaryLabel + " | " + Localizer.Format(QualityLabel) + "(" + quality + "/" + maxQuality + ")";
            if (showDebug)
                summaryLabel = summaryLabel + " MTBF: (" + currentMTBF + "/" + maxMTBF + ")";
            return summaryLabel;
        }

        /// <summary>
        /// Performs the reliability checks for the loaded and unloaded vessels. Typically this is automatically every few hours.
        /// </summary>
        /// <param name="timewarpPenalty">If reliability checks were skipped due to high timewarp, then the checks can incur a penalty. This is the penalty to apply.</param>
        public void PerformReliabilityChecks(int timewarpPenalty = 0)
        {
            debugLog("PerformReliabilityChecks called");

            //Clean the caches
            cleanQualityCaches();
            focusVesselView.flightGlobalIndexes.Clear();
            focusVesselView.vesselNames.Clear();

            //Start coroutines
            if (HighLogic.LoadedSceneIsFlight)
                StartCoroutine(PerformQualityCheckLoaded(timewarpPenalty));

            StartCoroutine(PerformQualityCheckUnloaded(timewarpPenalty));

        }

        /// <summary>
        /// Performs a quality check based upon the quality target number, the ranking astronaut's skill, and any timewarp penalty.
        /// The quality check does not decide what to do with the result; it just makes the check and reports the results.
        /// </summary>
        /// <param name="quality">The base target number to perform the quality check against.</param>
        /// <param name="skillRank">A value between 0 and 5, the skill rank of the highest ranking kerbal.</param>
        /// <param name="timewarpPenalty">The penalty to apply due to quality checks skipped due to high timewarp.</param>
        /// <param name="doubleCheckCriticalFailures">Flag to double-check that there is a critical failure.</param>
        /// <returns>A QualityCheckResult containing the results of te quality check.</returns>
        public QualityCheckResult PerformQualityCheck(int quality, int skillRank, int timewarpPenalty, bool doubleCheckCriticalFailures = true)
        {
            QualityCheckResult result = new QualityCheckResult();
            int targetNumber = 100;
            int analysisRoll = 0;
            int facilityBonus = GetFacilityBonus();
            int difficultyBonus = BARISSettings.DifficultyModifier;
            QualityCheckStatus eventCardStatus = QualityCheckStatus.none;

            //Target number is 100 - (current quality + event card bonuses)
            //A roll of 1 is a critical failure. A roll of 100 is a critical success.
            //If the quality check fails, and the player has no failure averted cards, then
            //the highest ranking astronaut may attempt to circumvent the failure by adding his/her skill
            //and applying any accumulated astronaut skill bonus event cards.
            //It's not up to this method to decide what to do with the results.
            //It just applies any modifiers and event cards and reports the results.

            //Calculate the target number
            targetNumber = 100 - (quality + facilityBonus + difficultyBonus + timewarpPenalty);
            if (targetNumber < 0)
                targetNumber = 0;
            debugLog("targetNumber: " + targetNumber + " quality: " + quality +
                " facilityBonus: " + facilityBonus + " difficultyBonus: " + difficultyBonus + " timewarpPenalty: " + timewarpPenalty);

            //Make the roll
            analysisRoll = UnityEngine.Random.Range(1, 100);
            result.resultRoll = analysisRoll;
            debugLog("analysisRoll: " + analysisRoll);

            //Did we critically fail?
            if (analysisRoll == CriticalFailRoll)
            {
                //If we should double check the critical failure (which gives 1:10000 chance of it happening)
                //then roll again.
                if (doubleCheckCriticalFailures)
                {
                    if (UnityEngine.Random.Range(1, MaxRangeForCritalFailDoubleCheck) == CriticalFailRoll)
                    {
                        result.statusResult = QualityCheckStatus.criticalFail;
                        debugLog("Quality check: Critical Fail!");
                        if (onQualityCheck != null)
                            onQualityCheck(result);
                        return result;
                    }
                }

                //Not double checking, we have a critical failure.
                else
                {
                    result.statusResult = QualityCheckStatus.criticalFail;
                    debugLog("Quality check: Critical Fail!");
                    if (onQualityCheck != null)
                        onQualityCheck(result);
                    return result;
                }
            }

            //Did we critically succeed?
            else if (analysisRoll == CriticalSuccessRoll)
            {
                result.statusResult = QualityCheckStatus.criticalSuccess;
                debugLog("Quality check: Critical Success!");
                if (onQualityCheck != null)
                    onQualityCheck(result);
                return result;
            }

            //Add in event card modifiers
            analysisRoll += playEventCardBonus(out eventCardStatus);

            //Did an event card cause a FUBAR?
            if (eventCardStatus == QualityCheckStatus.criticalFail)
            {
                result.statusResult = QualityCheckStatus.criticalFail;
                debugLog("Quality check: Critical Fail!");
                if (onQualityCheck != null)
                    onQualityCheck(result);
                return result;
            }

            //Quality check failed, do we have an event card to avert failure?
            else if (eventCardStatus == QualityCheckStatus.success || eventCardStatus == QualityCheckStatus.criticalSuccess)
            {
                result.statusResult = QualityCheckStatus.eventCardAverted;
                debugLog("Quality check: Event card averted failure.");
            }

            //Did we succeed?
            else if (analysisRoll >= targetNumber)
            {
                result.statusResult = QualityCheckStatus.success;
                debugLog("Quality check: Success");
            }

            //Quality check failed, can the astronaut avert failure?
            else if (skillRank > 0 && (analysisRoll + (skillRank * QualityPerSkillPoint) + getAstronautBonus() >= targetNumber))
            {
                result.statusResult = QualityCheckStatus.astronautAverted;
                debugLog("Quality check: astronaut averted failure.");
            }

            //No way to avoid this failure
            else
            {
                result.statusResult = QualityCheckStatus.fail;
                debugLog("Quality check: Fail");
            }

            onQualityCheck?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Performs a quality check based upon the quality target number, the ranking astronaut's skill, and any timewarp penalty.
        /// The quality check does not decide what to do with the result; it just makes the check and reports the results.
        /// </summary>
        /// <param name="vessel">The vessel to perform the quality check against.</param>
        /// <param name="quality">The base target number aginst which to perform the quality check.</param>
        /// <param name="skillName">The name of the skill to find a ranking astronaut with that might offer a bonus. Examples include repairSKill and ScienceSkill.</param>
        /// <param name="doubleCheckCriticalFailures">Flag to double-check that there is a critical failure.</param>
        /// <returns>A QualityCheckResult containing the results of the quality check.</returns>
        public QualityCheckResult PerformQualityCheck(Vessel vessel, int quality, string skillName, bool doubleCheckCriticalFailures = true)
        {
            QualityCheckResult result = new QualityCheckResult();
            ProtoCrewMember astronaut = null;
            int skillRank = 0;

            //Get the highest ranking skill, if any
            skillRank = GetHighestRank(vessel, skillName, out astronaut);
            if (astronaut != null)
            {
                debugLog(astronaut.name + " has the highest " + skillName + " with a rank of " + skillRank);
            }

            result = PerformQualityCheck(quality, skillRank, 0, doubleCheckCriticalFailures);
            result.astronaut = astronaut;
            result.highestRank = skillRank;

            return result;
        }

        /// <summary>
        /// Returns the highest ranking astronaut in the vessel that has the required skill.
        /// </summary>
        /// <param name="vessel">The vessel to check for the highest ranking kerbal.</param>
        /// <param name="skillName">The name of the skill to look for. Examples include RepairSkill and ScienceSkill.</param>
        /// <param name="astronaut">The astronaut that has the highest ranking skill.</param>
        /// <returns>The skill rank rating of the highest ranking astronaut (if any)</returns>
        public int GetHighestRank(Vessel vessel, string skillName, out ProtoCrewMember astronaut)
        {
            astronaut = null;
            if (string.IsNullOrEmpty(skillName))
                return 0;
            try
            {
                if (vessel.GetCrewCount() == 0)
                    return 0;
            }
            catch
            {
                return 0;
            }

            //Record the last vessel
            lastVessel = vessel;

            string[] skillsToCheck = skillName.Split(new char[] { ';' });
            string checkSkill;
            int highestRank = 0;
            int crewRank = 0;
            for (int skillIndex = 0; skillIndex < skillsToCheck.Length; skillIndex++)
            {
                checkSkill = skillsToCheck[skillIndex];

                //Find the highest racking kerbal with the desired skill (if any)
                ProtoCrewMember[] vesselCrew = vessel.GetVesselCrew().ToArray();
                for (int index = 0; index < vesselCrew.Length; index++)
                {
                    if (vesselCrew[index].HasEffect(checkSkill))
                    {
                        crewRank = vesselCrew[index].experienceTrait.CrewMemberExperienceLevel();
                        if (crewRank > highestRank)
                        {
                            highestRank = crewRank;
                            astronaut = vesselCrew[index];

                            //Cache the highest ranking astronaut
                            if (highestRankingAstronauts.ContainsKey(checkSkill))
                            {
                                highestRankingAstronauts[checkSkill] = astronaut;
                            }
                            else
                            {
                                highestRankingAstronauts.Add(checkSkill, astronaut);
                                highestSkills.Add(checkSkill, astronaut.experienceTrait.CrewMemberExperienceLevel());
                            }
                        }
                    }
                }
            }

            debugLog("highestRank: " + highestRank);
            return highestRank;
        }

        /// <summary>
        /// Logs the player message. In flight, the message is also added to the flight log.
        /// </summary>
        /// <param name="message">A string containing the message to log.</param>
        public void LogPlayerMessage(string message)
        {
            ScreenMessages.PostScreenMessage(message, BARISScenario.MessageDuration, ScreenMessageStyle.UPPER_CENTER);
            if (HighLogic.LoadedSceneIsFlight)
                FlightLogger.fetch.LogEvent(message);
        }
        #endregion

        #region KAC
        /// <summary>
        /// Sets or updates the Kerbal Alarm Clock alarm for the editor bay
        /// </summary>
        /// <param name="editorBayItem">The EditorBayItem to set the integration alarm for.</param>
        /// <param name="workStoppedDays">How many days has the work been paused.</param>
        public void SetKACAlarm(EditorBayItem editorBayItem, int workStoppedDays = 0)
        {
            if (!KACWrapper.AssemblyExists)
                return;
            if (!KACWrapper.APIReady)
                return;

            //Delete the alarm if it exists
            if (!string.IsNullOrEmpty(editorBayItem.KACAlarmID))
                KACWrapper.KAC.DeleteAlarm(editorBayItem.KACAlarmID);

            //Get the start time
            double startTime = editorBayItem.integrationStartTime;
            if (startTime == 0)
            {
                startTime = Planetarium.GetUniversalTime();
                editorBayItem.integrationStartTime = startTime;
            }

            //Calculate the base build time
            double secondsPerDay = GameSettings.KERBIN_TIME == true ? 21600 : 86400;
            int workerCount = editorBayItem.workerCount;
            //If we have no workers then assume at least one worker so we don't get divide by zero errors.
            //The time to completion will get updated when more workers are added.
            if (workerCount <= 0)
                workerCount = 1;
            int buildTimeDays = Mathf.RoundToInt(editorBayItem.totalIntegrationToAdd / BARISScenario.Instance.GetWorkerProductivity(workerCount, editorBayItem.isVAB));
            double buildTimeSeconds = buildTimeDays * secondsPerDay;

            //Account for time already spent.
            double elapsedTime = Planetarium.GetUniversalTime() - startTime;
            buildTimeSeconds -= elapsedTime;

            //Account for work stoppage
            if (workStoppedDays > 0)
                buildTimeSeconds += (double)workStoppedDays * secondsPerDay;

            //Now set the alarm
            buildTimeSeconds += Planetarium.GetUniversalTime();
            editorBayItem.KACAlarmID = KACWrapper.KAC.CreateAlarm(KACWrapper.KACAPI.AlarmTypeEnum.Raw, editorBayItem.vesselName + BARISScenario.IntegrationCompletedKACAlarm, buildTimeSeconds);
        }
        #endregion

        #endregion

        #region coroutines
        public IEnumerator<YieldInstruction> PerformQualityCheckUnloaded(int timewarpPenalty = 0)
        {
            debugLog("performQualityCheckUnloaded called");
            UnloadedQualitySummary qualitySummary;
            Vessel[] unloadedVessels = FlightGlobals.VesselsUnloaded.ToArray();
            QualityCheckResult result;
            ProtoPartModuleSnapshot[] failureCandidates;
            float mtbfDecrement = (float)(qualityCheckInterval / 3600.0f);
            int failedPartIndex = 0;
            ProtoPartModuleSnapshot moduleSnapshot = null;
            bool createdNewRecord;
            int brokenModuleCount = 0;

            //Go through all the unloaded vessels and make the quality check.
            for (int index = 0; index < unloadedVessels.Length; index++)
            {
                //Get the summary info.
                qualitySummary = GetUnloadedQualitySummary(unloadedVessels[index], out createdNewRecord);
                if (qualitySummary == null)
                {
                    yield return new WaitForFixedUpdate();
                    continue;
                }
                if (createdNewRecord)
                    yield return new WaitForFixedUpdate();

                //Get failure candidates
                failureCandidates = qualitySummary.UpdateAndGetFailureCandidates(mtbfDecrement);
                yield return new WaitForFixedUpdate();

                //Make the quality check
                result = PerformQualityCheck(qualitySummary.reliability, qualitySummary.highestRank, timewarpPenalty);
                switch (result.statusResult)
                {
                    //Select one of the parts to fail.
                    case QualityCheckStatus.criticalFail:
                        failedPartIndex = UnityEngine.Random.Range(0, qualitySummary.qualityModuleSnapshots.Length - 1);
                        moduleSnapshot = qualitySummary.qualityModuleSnapshots[failedPartIndex];
                        moduleSnapshot.moduleValues.SetValue("isBrokenOnStart", true, true);
                        moduleSnapshot.moduleValues.SetValue("currentQuality", 0, true);
                        //Cancel any repair attempts in progress
                        CancelRepairProject(unloadedVessels[index]);
                        brokenModuleCount = int.Parse(moduleSnapshot.moduleValues.GetValue("breakableModuleCount"));
                        brokenModuleCount -= 1;
                        moduleSnapshot.moduleValues.SetValue("breakableModuleCount", brokenModuleCount);
                        //Only add filtered vessels.
                        if (BARISUtils.IsFilterEnabled(unloadedVessels[index]))
                        {
                            focusVesselView.flightGlobalIndexes.Add(index);
                            focusVesselView.vesselNames.Add(unloadedVessels[index].vesselName);
                        }
                        if (BARISSettings.KillTimewarpOnBreak && TimeWarp.CurrentRateIndex >= HighTimewarpIndex)
                            TimeWarp.SetRate(0, true);
                        vesselHadAProblem = true;
                        break;

                    //Select one of the failure candidates to fail.
                    case QualityCheckStatus.fail:
                        if (failureCandidates != null)
                        {
                            failedPartIndex = UnityEngine.Random.Range(0, failureCandidates.Length - 1);
                            moduleSnapshot = failureCandidates[failedPartIndex];
                            moduleSnapshot.moduleValues.SetValue("isBrokenOnStart", true, true);
                            moduleSnapshot.moduleValues.SetValue("currentQuality", 0, true);
                            //Cancel any repair attempts in progress
                            CancelRepairProject(unloadedVessels[index]);
                            brokenModuleCount = int.Parse(moduleSnapshot.moduleValues.GetValue("breakableModuleCount"));
                            brokenModuleCount -= 1;
                            moduleSnapshot.moduleValues.SetValue("breakableModuleCount", brokenModuleCount);
                            //Only add filtered vessels.
                            if (BARISUtils.IsFilterEnabled(unloadedVessels[index]))
                            {
                                focusVesselView.flightGlobalIndexes.Add(index);
                                focusVesselView.vesselNames.Add(unloadedVessels[index].vesselName);
                            }
                            if (BARISSettings.KillTimewarpOnBreak && TimeWarp.CurrentRateIndex >= HighTimewarpIndex)
                                TimeWarp.SetRate(0, true);
                            vesselHadAProblem = true;
                        }
                        else
                        {
                            debugLog("No failureCandidates have MTBF = 0");
                        }
                        break;

                    //Add a failure averted event card
                    case QualityCheckStatus.criticalSuccess:
                        break;

                    //Show event card result
                    case QualityCheckStatus.eventCardAverted:
                        break;

                    //if we have failure candidates, then there's a chance that one of them will lose
                    //a bit of quality even with a successful result.
                    case QualityCheckStatus.success:
                        if (failureCandidates != null)
                        {
                            failedPartIndex = UnityEngine.Random.Range(0, failureCandidates.Length - 1);
                            moduleSnapshot = failureCandidates[failedPartIndex];
                            if (UnityEngine.Random.Range(0, 100) <= WearAndTearTargetNumber)
                            {
                                int currentQuality = int.Parse(moduleSnapshot.moduleValues.GetValue("currentQuality"));
                                currentQuality -= WearAndTearQualityLoss;
                                if (currentQuality <= 0)
                                {
                                    currentQuality = 0;
                                    moduleSnapshot.moduleValues.SetValue("isBrokenOnStart", true, true);
                                }
                                moduleSnapshot.moduleValues.SetValue("currentQuality", currentQuality);
                            }
                        }
                        break;
                }
                //Get ready to process the next vessel
                yield return new WaitForFixedUpdate();
            }

            //Show the vessel switch dialog, but only if there's at least one vessel that has a problem and it isn't the active vessel.
            if (focusVesselView.flightGlobalIndexes.Count > 0)
            {
                if (!BARISSettings.EmailVesselRepairRequests)
                    focusVesselView.SetVisible(true);
                else
                    emailVesselRepairsRequest();
            }

            //Play sound effect
            if (vesselHadAProblem)
            {
                PlayProblemSoundReversed();
                vesselHadAProblem = false;
            }

            //Get ready to process the next vessel
            yield return new WaitForFixedUpdate();
        }

        public IEnumerator<YieldInstruction> PerformQualityCheckLoaded(int timewarpPenalty = 0)
        {
            debugLog("PerformQualityCheckLoaded called");
            LoadedQualitySummary qualitySummary;
            Vessel[] loadedVessels = FlightGlobals.VesselsLoaded.ToArray();
            QualityCheckResult result;
            float mtbfDecrement = (float)(qualityCheckInterval / 3600.0f);
            int failedPartIndex = 0;
            ModuleQualityControl[] failureCandidates;
            ModuleQualityControl qualityControl;
            bool createdNewRecord;
            List<string> brokenVesselNames = new List<string>();

            //Go through all the unloaded vessels and make the quality check.
            for (int index = 0; index < loadedVessels.Length; index++)
            {
                //Get the summary info.
                qualitySummary = GetLoadedQualitySummary(loadedVessels[index], out createdNewRecord);
                if (qualitySummary == null)
                {
                    yield return new WaitForFixedUpdate();
                    continue;
                }
                if (createdNewRecord)
                    yield return new WaitForFixedUpdate();

                //Get failure candidates
                failureCandidates = qualitySummary.UpdateAndGetFailureCandidates(mtbfDecrement);
                yield return new WaitForFixedUpdate();

                //Make the quality check
                result = PerformQualityCheck(qualitySummary.reliability, qualitySummary.highestRank, timewarpPenalty);
                switch (result.statusResult)
                {
                    //Select one of the parts to fail.
                    case QualityCheckStatus.criticalFail:
                        if (loadedVessels[index] != FlightGlobals.ActiveVessel)
                            brokenVesselNames.Add(loadedVessels[index].vesselName);
                        failedPartIndex = UnityEngine.Random.Range(0, qualitySummary.qualityModules.Length - 1);
                        qualityControl = qualitySummary.qualityModules[failedPartIndex];
                        if (!qualityControl.IsBroken)
                            qualityControl.DeclarePartBroken();
                        else
                            qualityControl.BreakAPartModule();
                        vesselHadAProblem = true;
                        break;

                    //Select one of the failure candidates to fail.
                    case QualityCheckStatus.fail:
                        if (failureCandidates != null)
                        {
                            if (loadedVessels[index] != FlightGlobals.ActiveVessel)
                                brokenVesselNames.Add(loadedVessels[index].vesselName);
                            failedPartIndex = UnityEngine.Random.Range(0, failureCandidates.Length - 1);
                            qualityControl = failureCandidates[failedPartIndex];
                            if (!qualityControl.IsBroken)
                                qualityControl.DeclarePartBroken();
                            else
                                qualityControl.BreakAPartModule();
                            vesselHadAProblem = true;
                        }
                        else
                        {
                            debugLog("No failureCandidates have MTBF = 0");
                        }
                        break;

                    //Add a failure averted event card
                    case QualityCheckStatus.criticalSuccess:
                        if (failureCandidates != null)
                        {
                            failedPartIndex = UnityEngine.Random.Range(0, failureCandidates.Length - 1);
                            qualityControl = failureCandidates[failedPartIndex];
                            if (UnityEngine.Random.Range(0, 100) <= WearAndTearTargetNumber)
                            {
                                qualityControl.currentQuality -= WearAndTearQualityLoss;
                                if (qualityControl.currentQuality <= 0)
                                {
                                    qualityControl.currentQuality = 0;
                                    qualityControl.DeclarePartBroken();
                                }
                            }
                        }
                        break;

                    //Show event card result
                    case QualityCheckStatus.eventCardAverted:
                        break;

                    //if we have failure candidates, then there's a chance that one of them will lose
                    //a bit of quality even with a successful result.
                    case QualityCheckStatus.success:
                        break;
                }

                //If there are loaded vessels that have broken that aren't the active vessel then list them out.
                if (brokenVesselNames.Count > 0)
                {
                    LogPlayerMessage(Localizer.Format(AVesselHasAProblem));
                    foreach (string vesselName in brokenVesselNames)
                        LogPlayerMessage(vesselName + Localizer.Format(VesselHasProblem));
                }

                //Get ready to process the next vessel
                yield return new WaitForFixedUpdate();
            }
        }

        IEnumerator<YieldInstruction> buildSummaryCaches()
        {
            Vessel[] vessels;
            bool createdNewRecord;

            if (FlightGlobals.VesselsUnloaded != null)
            {
                debugLog("Caching unloaded vessels...");
                vessels = FlightGlobals.VesselsUnloaded.ToArray();
                for (int index = 0; index < vessels.Length; index++)
                {
                    GetUnloadedQualitySummary(vessels[index], out createdNewRecord);
                    yield return new WaitForFixedUpdate();
                }
            }

            if (HighLogic.LoadedSceneIsFlight && FlightGlobals.VesselsLoaded != null)
            {
                debugLog("Caching loaded vessels...");
                vessels = FlightGlobals.VesselsLoaded.ToArray();
                for (int index = 0; index < vessels.Length; index++)
                {
                    GetLoadedQualitySummary(vessels[index], out createdNewRecord);
                    yield return new WaitForFixedUpdate();
                }
            }

            cachingInProgress = false;
            yield return new WaitForFixedUpdate();
        }

        IEnumerator<YieldInstruction> cacheVesselSummary(Vessel vessel)
        {
            debugLog("cacheVesselSummary called");
            bool createdNewRecord;
            if (vessel.loaded)
                GetLoadedQualitySummary(vessel, out createdNewRecord);
            else
                GetUnloadedQualitySummary(vessel, out createdNewRecord);

            yield return null;
        }
        #endregion

        #region Helpers
        public static double GetSecondsPerDay()
        {
            if (secondsPerDay > 0)
                return secondsPerDay;

            //Find homeworld
            int count = FlightGlobals.Bodies.Count;
            CelestialBody body = null;
            for (int index = 0; index < count; index++)
            {
                body = FlightGlobals.Bodies[index];
                if (body.isHomeWorld)
                    break;
                else
                    body = null;
            }
            if (body == null)
            {
                secondsPerYear = 21600 * 426.08;
                secondsPerDay = 21600;
                return secondsPerDay;
            }

            //Also get seconds per year
            secondsPerYear = body.orbit.period;

            //Return solar day length
            secondsPerDay = body.solarDayLength;
            return secondsPerDay;
        }

        protected void emailVesselRepairsRequest()
        {
            string[] vesselNames = focusVesselView.vesselNames.ToArray();

            StringBuilder resultsMessage = new StringBuilder();
            MessageSystem.Message msg;

            resultsMessage.AppendLine(Localizer.Format(BARISScenario.FromMissionControlSystems));
            resultsMessage.AppendLine(Localizer.Format(BARISScenario.SubjectRepair));
            resultsMessage.AppendLine(" ");
            resultsMessage.AppendLine(Localizer.Format(BARISScenario.MsgBodyVesselRepairs));
            resultsMessage.AppendLine(" ");

            for (int index = 0; index < vesselNames.Length; index++)
                resultsMessage.AppendLine(vesselNames[index]);
            resultsMessage.AppendLine(" ");

            resultsMessage.AppendLine(Localizer.Format(BARISScenario.MsgBodyVesselCheckStatus));

            //Tiger team tool tip
            if (!BARISScenario.showedTigerTeamToolTip)
            {
                BARISScenario.showedTigerTeamToolTip = true;
                resultsMessage.AppendLine(" ");
                resultsMessage.AppendLine(Localizer.Format(BARISScenario.MsgBodyTigerTeamRepairs));
                resultsMessage.AppendLine(" ");
            }

            msg = new MessageSystem.Message(Localizer.Format(BARISScenario.MsgTitleVesselRepairs), resultsMessage.ToString(),
                MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.FAIL);
            MessageSystem.Instance.AddMessage(msg);
        }

        protected void updateRepairProjects()
        {
            string[] keys = repairProjects.Keys.ToArray();
            double currentTime = Planetarium.GetUniversalTime();
            BARISRepairProject repairProject;
            List<string> doomed = new List<string>();
            UnloadedQualitySummary qualitySummary = null;

            for (int index = 0; index < keys.Length; index++)
            {
                //Update elapsed time
                repairProject = repairProjects[keys[index]];
                repairProject.elapsedTime = currentTime - repairProject.startTime;

                //If the project is complete then make the repair attempt
                if (repairProject.IsProgressComplete)
                {
                    //Get the unloaded summary for the vessel.
                    foreach (Vessel vessel in unloadedQualityCache.Keys)
                    {
                        if (repairProject.vesselID == vessel.id.ToString())
                        {
                            qualitySummary = unloadedQualityCache[vessel];
                            break;
                        }
                    }

                    //Attempt the repairs
                    if (qualitySummary != null)
                        qualitySummary.AttemptRepairs();

                    //Remove project
                    doomed.Add(keys[index]);
                }
            }

            //Removed all the doomed projects
            if (doomed.Count > 0)
            {
                foreach (string doomedProject in doomed)
                    repairProjects.Remove(doomedProject);
            }
        }

        protected void updateEventCardTimer()
        {
            if (!eventCardsEnabled)
                return;
            if (TimeWarp.CurrentRateIndex >= HighTimewarpIndex)
                return;
            if (eventCardStartTime == 0)
                eventCardStartTime = Planetarium.GetUniversalTime();

            double elapsedTime = Planetarium.GetUniversalTime() - eventCardStartTime;
            double checkInterval = GetSecondsPerDay();
            checkInterval *= eventCardFrequency;

            //If we've met or exceeded our check interval, then it's time to play an event card.
            if (elapsedTime >= checkInterval)
            {
                eventCardStartTime = Planetarium.GetUniversalTime();
                int index = UnityEngine.Random.Range(0, eventCardDeck.Count - 1);
                debugLog("updateEventCardTimer - Playing an event card: " + eventCardDeck[index]);
                PlayCard(eventCardDeck[index]);
            }

        }

        protected void updateIntegrationTimer()
        {
            //if KAC is installed then we rely on its integration timers.
            if (KACWrapper.AssemblyExists && KACWrapper.APIReady)
            {
                //Account for work stoppages
                if (workPausedDays > 0)
                {
                    ignoreAlarmDelete = true;
                    foreach (EditorBayItem bayItem in editorBayItems.Values)
                    {
                        if (!string.IsNullOrEmpty(bayItem.KACAlarmID))
                            SetKACAlarm(bayItem, workPausedDays);
                    }
                    ignoreAlarmDelete = false;
                    workPausedDays = 0;
                }
                return;
            }

            //We're using built-in vehicle integration timers at this point.
            if (integrationStartTime == 0)
                integrationStartTime = Planetarium.GetUniversalTime();

            double elapsedTime = Planetarium.GetUniversalTime() - integrationStartTime;
            double checkInterval = GetSecondsPerDay();

            if (elapsedTime >= checkInterval)
            {
                UpdateBuildTime();
            }
        }

        protected void updatePayrollTimer()
        {
            if (payrollStartTime == 0)
                payrollStartTime = Planetarium.GetUniversalTime();

            double elapsedTime = Planetarium.GetUniversalTime() - payrollStartTime;
            double checkInterval = GetSecondsPerDay();
            checkInterval *= DaysPerPayroll;

            if (elapsedTime >= checkInterval)
            {
                debugLog("Paying workers");
                PayWorkers();
            }
        }

        protected void logVesselHasAProblem(Vessel vessel)
        {
            string message = Localizer.Format(vessel.vesselName + BARISScenario.VesselHasProblem);
            ScreenMessages.PostScreenMessage(message, BARISScenario.MessageDuration, ScreenMessageStyle.UPPER_CENTER);

            //Log the event
            if (HighLogic.LoadedSceneIsFlight && vessel.loaded)
                FlightLogger.fetch.LogEvent(message);

            //Email player
            if (BARISSettings.EmailRepairRequests)
            {
                MessageSystem.Message msg = new MessageSystem.Message(vessel.vesselName + Localizer.Format(BARISScenario.MsgTitleRepair), message,
                    MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.FAIL);
                MessageSystem.Instance.AddMessage(msg);
            }

            //Play the sound
        }

        protected void updateReliabilityChecks()
        {
            if (!partsCanBreak)
                return;

            //If it's time to perform a reliability check, then do so for loaded and unloaded vessels.
            //If we're out of timewarp and we skipped checks, then perform a check immediately.
            if (TimeWarp.CurrentRateIndex == 0 && skippedChecks > 0)
            {
                debugLog("updateReliabilityChecks - Out of timewarp, catching up on checks");
                lastUpdateTime = Planetarium.GetUniversalTime();
                skippedChecks = 0;
                timewarpModifier += MinimumCyclePenalty;
                PerformReliabilityChecks(timewarpModifier);
                timewarpModifier = 0;
                return;
            }

            //Check elapsed time and perform reliability check as needed
            double elapsedTime = Planetarium.GetUniversalTime() - lastUpdateTime;
            qualityCheckInterval = GetSecondsPerDay();
            qualityCheckInterval /= checksPerDay;
            if (elapsedTime >= qualityCheckInterval)
            {
                lastUpdateTime = Planetarium.GetUniversalTime();

                //If we're in a high timewarp situation, then skip the check and incur a penalty
                //If we've skipped more than we're allowed to, then perform the check.
                //Typically we're allowed to skip up to 10 cycles.
                if (TimeWarp.CurrentRateIndex >= HighTimewarpIndex && skippedChecks < MaxSkippedChecks)
                {
                    skippedChecks += 1;
                    timewarpModifier += MissedCyclePenalty;

                    //If we're at high timewarp and we've reached the max skip penalty limit, then trigger a quality check
                    if (skippedChecks >= MaxSkippedChecks)
                    {
                        debugLog("updateReliabilityChecks - Met or exceeded MaxSkippedChecks, time to catch up");
                        skippedChecks = 0;
                        timewarpModifier += MinimumCyclePenalty;
                        PerformReliabilityChecks(timewarpModifier);
                        timewarpModifier = 0;
                    }
                    else
                    {
                        debugLog("updateReliabilityChecks - Skipping reliability check due to high timewarp");
                    }
                    return;
                }

                //No longer in timewarp, but if we skipped checks then we take a penalty.
                else if (skippedChecks > 0)
                {
                    debugLog("updateReliabilityChecks - Updating timewarp penalty");
                    skippedChecks = 0;
                    timewarpModifier += MinimumCyclePenalty;
                }

                //Perform quality checks
                PerformReliabilityChecks(timewarpModifier);
                timewarpModifier = 0;
            }
        }

        protected void updateFlightControlState()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (FlightGlobals.ActiveVessel == null)
                return;
            if (FlightGlobals.ActiveVessel.isEVA)
                return;

            if (HighLogic.LoadedSceneIsFlight && partsCanBreak)
            {
                rcsIsActive = FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.RCS];
                sasIsActive = FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.SAS];

                if ((rcsIsActive != rcsWasActive) && BARISBridge.RCSCanFail)
                {
                    rcsWasActive = rcsIsActive;
                    onRcsUpdate?.Invoke(rcsIsActive);
                }

                if ((sasIsActive != sasWasActive) && BARISBridge.SASCanFail)
                {
                    sasWasActive = sasIsActive;
                    onSasUpdate?.Invoke(sasIsActive);
                }

                //Check for throttle state
                if (BARISBridge.EnginesCanFail)
                {
                    if (FlightInputHandler.state.mainThrottle > 0f && !isThrottleUp)
                    {
                        isThrottleUp = true;
                        onThrottleUpDown?.Invoke(isThrottleUp);
                    }
                    else if (FlightInputHandler.state.mainThrottle <= 0f && isThrottleUp)
                    {
                        isThrottleUp = false;
                        onThrottleUpDown?.Invoke(isThrottleUp);
                    }
                }
            }
        }

        public LoadedQualitySummary GetLoadedQualitySummary(Vessel vessel, out bool createdNewRecord)
        {
            createdNewRecord = false;
            LoadedQualitySummary summary = null;

            //If we've processed this vessel before
            //and found no quality modules, then we're done.
            if (nonBreakableVessels.Contains(vessel))
                return null;

            //If the vessel is mothballed, then we're done.
            if (IsMothballed(vessel))
                return null;

            //Check the cache to see if we have it there.
            if (loadedQualityCache.ContainsKey(vessel))
            {
                debugLog("GetLoadedQualitySummary - " + vessel.vesselName + " retrieving from cache");

                //Get the summary
                summary = loadedQualityCache[vessel];

                //If the part count hasn't changed then we can return the cached summary
                //Otherwise we need to rebuild the cache.
                if (summary.partCount == vessel.parts.Count)
                {
                    debugLog("Part count unchanged, cache is valid.");
                    return summary;
                }
                else
                {
                    debugLog("Part count changed");
                    loadedQualityCache.Remove(vessel);
                }
            }

            //The cache doesn't contain the vessel so let's build a summary
            debugLog("GetQualityLoadedSummary - Cache out of date, making new entry for " + vessel.vesselName);
            createdNewRecord = true;

            int totalQuality = 0;
            List<ModuleQualityControl> qualityModules = null;
            List<ModuleQualityControl> failureCandidates = new List<ModuleQualityControl>();
            List<ModuleQualityControl> criticalFailureCandidates = new List<ModuleQualityControl>();
            ModuleQualityControl[] qualityControlModules = null;
            ModuleQualityControl qualityControl = null;
            ProtoCrewMember rankingAstronaut = null;
            ProtoCrewMember astronaut = null;
            int skillRank = 0;
            int highestRank = 0;
            
            //Find all the quality control modules, if any
            qualityModules = vessel.FindPartModulesImplementing<ModuleQualityControl>();
            if (qualityModules == null || qualityModules.Count == 0)
                return null;
            qualityControlModules = qualityModules.ToArray();

            //Go through all the quality control modules and get the stats we need.
            for (int controlIndex = 0; controlIndex < qualityControlModules.Length; controlIndex++)
            {
                //Get quality control module
                qualityControl = qualityControlModules[controlIndex];

                //Add the module's quality to the total
                totalQuality += qualityControl.currentQuality;

                //Get the highest ranking astronaut with the required quality check skill
                skillRank = GetHighestRank(vessel, qualityControl.qualityCheckSkill, out astronaut);
                if (skillRank > highestRank)
                {
                    highestRank = skillRank;
                    rankingAstronaut = astronaut;
                }
            }

            //Fill out the summary info
            if (qualityModules.Count > 0)
            {
                summary = new LoadedQualitySummary();
                summary.qualityModules = qualityControlModules;
                summary.reliability = (int)Mathf.Round(totalQuality / (100 * qualityModules.Count));
                summary.highestRank = highestRank;
                summary.rankingAstronaut = rankingAstronaut;
                summary.vessel = vessel;
                summary.partCount = vessel.parts.Count;
                loadedQualityCache.Add(vessel, summary);
            }

            else //No summary created, the vessel doesn't have any breakable parts.
            {
                createdNewRecord = false;
                nonBreakableVessels.Add(vessel);
            }

            return summary;
        }

        public UnloadedQualitySummary GetUnloadedQualitySummary(Vessel vessel, out bool createdNewRecord)
        {
            createdNewRecord = false;
            UnloadedQualitySummary summary = null;

            //If we've processed this vessel before
            //and found no quality modules, then we're done.
            if (nonBreakableVessels.Contains(vessel))
                return null;

            //If the vessel is mothballed, then we're done.
            if (IsMothballed(vessel))
            {
                debugLog("Skipping " + vessel.vesselName + ", it is mothballed.");
                return null;
            }

            //Check the cache to see if we have it there.
            if (unloadedQualityCache.ContainsKey(vessel))
            {
                debugLog("GetUnloadedQualitySummary - " + vessel.vesselName + " retrieved from cache");
                return unloadedQualityCache[vessel];
            }

            //Cached summary does not exist. Create one and gather the data we need.
            debugLog("GetUnloadedQualitySummary - Cache out of date, making new entry for " + vessel.vesselName);
            createdNewRecord = true;

            ProtoVessel protoVessel = vessel.protoVessel;
            ProtoPartSnapshot[] partSnapshots;
            ProtoPartSnapshot partSnapshot;
            ProtoPartModuleSnapshot[] moduleSnapshots;
            ProtoPartModuleSnapshot moduleSnapshot;
            List<ProtoPartModuleSnapshot> qualityModuleSnapshots = new List<ProtoPartModuleSnapshot>();
            int currentQuality = 0;
            int totalQuality = 0;
            int totalBreakableParts = 0;
            ProtoCrewMember rankingAstronaut = null;
            ProtoCrewMember astronaut = null;
            int highestRank = 0;
            int skillRank = 0;
            string qualityCheckSkill = string.Empty;

            //Go through all the snapshots and find ModuleQualityControl snapshots.
            partSnapshots = protoVessel.protoPartSnapshots.ToArray();
            for (int partIndex = 0; partIndex < partSnapshots.Length; partIndex++)
            {
                partSnapshot = partSnapshots[partIndex];

                //Go through all the snapshots and see if we find a ModuleQualityControl.
                moduleSnapshots = partSnapshot.modules.ToArray();
                for (int moduleIndex = 0; moduleIndex < moduleSnapshots.Length; moduleIndex++)
                {
                    moduleSnapshot = moduleSnapshots[moduleIndex];
                    if (moduleSnapshot.moduleName == "ModuleQualityControl")
                    {
                        //Get quality. If the currentQuality hasn't been initialized, then skip this part.
                        currentQuality = int.Parse(moduleSnapshot.moduleValues.GetValue("currentQuality"));
                        if (currentQuality == -1)
                            break;
                        totalQuality += currentQuality;

                        //Track breakable part total
                        totalBreakableParts += 1;

                        //Add the snapshot to our list
                        qualityModuleSnapshots.Add(moduleSnapshot);

                        //Get the highest ranking astronaut with the required quality check skill
                        qualityCheckSkill = moduleSnapshot.moduleValues.GetValue("qualityCheckSkill");
                        skillRank = GetHighestRank(vessel, qualityCheckSkill, out astronaut);
                        if (skillRank > highestRank)
                        {
                            highestRank = skillRank;
                            rankingAstronaut = astronaut;
                        }

                        //Stop looking, we found the ModuleQualityControl.
                        break;
                    }
                }
            }

            //Almost done, fill out the summary.
            if (qualityModuleSnapshots.Count > 0)
            {
                summary = new UnloadedQualitySummary();
                summary.qualityModuleSnapshots = qualityModuleSnapshots.ToArray();
                summary.reliability = (int)Mathf.Round(totalQuality / (100 * totalBreakableParts));
                summary.highestRank = highestRank;
                summary.rankingAstronaut = rankingAstronaut;
                summary.vessel = vessel;
                unloadedQualityCache.Add(vessel, summary);
            }

            else //No summary created, the vessel doesn't have any breakable parts. Add it to the ignore list.
            {
                createdNewRecord = false;
                nonBreakableVessels.Add(vessel);
            }

            return summary;
        }

        protected void cleanQualityCaches()
        {
            debugLog("cleanQualityCaches called");
            //Clean the loaded vessel cache
            Vessel[] vessels = loadedQualityCache.Keys.ToArray();
            List<Vessel> doomed = new List<Vessel>();
            for (int index = 0; index < vessels.Length; index++)
            {
                if (!FlightGlobals.VesselsLoaded.Contains(vessels[index]))
                    doomed.Add(vessels[index]);
            }
            if (doomed.Count > 0)
            {
                vessels = doomed.ToArray();
                for (int index = 0; index < vessels.Length; index++)
                    loadedQualityCache.Remove(vessels[index]);
                debugLog("Removed " + vessels.Length + " loaded cache items");
            }

            //Now clean unloaded cache
            doomed.Clear();
            vessels = unloadedQualityCache.Keys.ToArray();
            for (int index = 0; index < vessels.Length; index++)
            {
                if (!FlightGlobals.VesselsUnloaded.Contains(vessels[index]))
                    doomed.Add(vessels[index]);
            }
            if (doomed.Count > 0)
            {
                vessels = doomed.ToArray();
                for (int index = 0; index < vessels.Length; index++)
                    loadedQualityCache.Remove(vessels[index]);
                debugLog("Removed " + vessels.Length + " unloaded cache items");
            }

            //Finally, clean the ignore list
            vessels = nonBreakableVessels.ToArray();
            for (int index = 0; index < vessels.Length; index++)
            {
                if (!FlightGlobals.VesselsLoaded.Contains(vessels[index]))
                    doomed.Add(vessels[index]);
                else if (!FlightGlobals.VesselsUnloaded.Contains(vessels[index]))
                    doomed.Add(vessels[index]);
            }
            if (doomed.Count > 0)
            {
                vessels = doomed.ToArray();
                for (int index = 0; index < vessels.Length; index++)
                    loadedQualityCache.Remove(vessels[index]);
                debugLog("Removed " + vessels.Length + " ignored cache items");
            }
        }

        protected int playEventCardBonus(out QualityCheckStatus status)
        {
            status = QualityCheckStatus.none;

            if (cachedQualityModifiers.Count > 0)
            {
                BARISEventResult eventResult = cachedQualityModifiers[0];
                cachedQualityModifiers.Remove(eventResult);
                switch (eventResult.modifierType)
                {
                    case BARISEventModifierTypes.criticalFail:
                        status = QualityCheckStatus.criticalFail;
                        break;
                    case BARISEventModifierTypes.criticalSuccess:
                        status = QualityCheckStatus.criticalSuccess;
                        break;

                    case BARISEventModifierTypes.success:
                        status = QualityCheckStatus.success;
                        break;

                    case BARISEventModifierTypes.fail:
                        status = QualityCheckStatus.fail;
                        break;
                }
                return eventResult.qualityCheckModifier;
            }

            return 0;
        }

        protected int getAstronautBonus()
        {
            //Factor in astronaut facility tier bonus.
            int facilityLevel = (int)Math.Round(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex) * BaseFacilityBonus);

            return facilityLevel;
        }

        protected void debugLog(string message)
        {
            if (showDebug == true)
                Debug.Log("[" + this.ClassName + "] - " + message);
        }

        protected void loadConstants()
        {
            ConfigNode node = GameDatabase.Instance.GetConfigNode("BARIS_CONSTANTS");
            if (node == null)
                return;

            #region Part Quality
            if (node.HasValue("MinimumTigerTeamRepairDays"))
                MinimumTigerTeamRepairDays = int.Parse(node.GetValue("MinimumTigerTeamRepairDays"));

            if (node.HasValue("TigerTeamRepairTarget"))
                TigerTeamRepairTarget = int.Parse(node.GetValue("TigerTeamRepairTarget"));

            if (node.HasValue("KillTimewarpOnTigerTeamCompleted"))
                KillTimewarpOnTigerTeamCompleted = bool.Parse(node.GetValue("KillTimewarpOnTigerTeamCompleted"));

            if (node.HasValue("CriticalFailRoll"))
                CriticalFailRoll = int.Parse(node.GetValue("CriticalFailRoll"));

            if (node.HasValue("CriticalSuccessRoll"))
                CriticalSuccessRoll = int.Parse(node.GetValue("CriticalSuccessRoll"));

            if (node.HasValue("MaxRangeForCritalFailDoubleCheck"))
                MaxRangeForCritalFailDoubleCheck = int.Parse(node.GetValue("MaxRangeForCritalFailDoubleCheck"));

            if (node.HasValue("HighTimewarpIndex"))
                HighTimewarpIndex = int.Parse(node.GetValue("HighTimewarpIndex"));

            if (node.HasValue("MaxSkippedChecks"))
                MaxSkippedChecks = int.Parse(node.GetValue("MaxSkippedChecks"));

            if (node.HasValue("MinimumCyclePenalty"))
                MinimumCyclePenalty = int.Parse(node.GetValue("MinimumCyclePenalty"));

            if (node.HasValue("MissedCyclePenalty"))
                MissedCyclePenalty = int.Parse(node.GetValue("MissedCyclePenalty"));

            if (node.HasValue("FlightCreatedIntegrationBonus"))
                FlightCreatedIntegrationBonus = int.Parse(node.GetValue("FlightCreatedIntegrationBonus"));

            if (node.HasValue("MTBFFacilityBonus"))
                MTBFFacilityBonus = float.Parse(node.GetValue("MTBFFacilityBonus"));

            if (node.HasValue("MTBFPerQualityBonus"))
                MTBFPerQualityBonus = float.Parse(node.GetValue("MTBFPerQualityBonus"));

            if (node.HasValue("MTBFCap"))
                MTBFCap = float.Parse(node.GetValue("MTBFCap"));

            if (node.HasValue("BaseFacilityBonus"))
                BaseFacilityBonus = int.Parse(node.GetValue("BaseFacilityBonus"));

            if (node.HasValue("QualityCheckFailLoss"))
                QualityCheckFailLoss = int.Parse(node.GetValue("QualityCheckFailLoss"));

            if (node.HasValue("QualityLossPerRepairs"))
                QualityLossPerRepairs = int.Parse(node.GetValue("QualityLossPerRepairs"));

            if (node.HasValue("WearAndTearTargetNumber"))
                WearAndTearTargetNumber = int.Parse(node.GetValue("WearAndTearTargetNumber"));

            if (node.HasValue("WearAndTearQualityLoss"))
                WearAndTearQualityLoss = int.Parse(node.GetValue("WearAndTearQualityLoss"));

            if (node.HasValue("MaintenanceQualityImprovement"))
                MaintenanceQualityImprovement = int.Parse(node.GetValue("MaintenanceQualityImprovement"));

            if (node.HasValue("QualityPerSkillPoint"))
                QualityPerSkillPoint = int.Parse(node.GetValue("QualityPerSkillPoint"));
            #endregion

            #region Rocket Construction
            if (node.HasValue("Level1BayCount"))
                Level1BayCount = int.Parse(node.GetValue("Level1BayCount"));

            if (node.HasValue("Level2BayCount"))
                Level2BayCount = int.Parse(node.GetValue("Level2BayCount"));

            if (node.HasValue("Level3BayCount"))
                Level3BayCount = int.Parse(node.GetValue("Level3BayCount"));

            if (node.HasValue("DefaultFlightBonus"))
                DefaultFlightBonus = int.Parse(node.GetValue("DefaultFlightBonus"));

            if (node.HasValue("Level1IntegrationCap"))
                Level1IntegrationCap = int.Parse(node.GetValue("Level1IntegrationCap"));

            if (node.HasValue("Level2IntegrationCap"))
                Level2IntegrationCap = int.Parse(node.GetValue("Level2IntegrationCap"));

            if (node.HasValue("Level3IntegrationCap"))
                Level3IntegrationCap = int.Parse(node.GetValue("Level3IntegrationCap"));

            if (node.HasValue("MaxHighBays"))
                MaxHighBays = int.Parse(node.GetValue("MaxHighBays"));

            if (node.HasValue("MaxHangarBays"))
                MaxHangarBays = int.Parse(node.GetValue("MaxHangarBays"));

            if (node.HasValue("MaxWorkersPerBay"))
                MaxWorkersPerBay = int.Parse(node.GetValue("MaxWorkersPerBay"));

            if (node.HasValue("MinWorkersPerFacility"))
                MinWorkersPerFacility = int.Parse(node.GetValue("MinWorkersPerFacility"));

            if (node.HasValue("DaysPerPayroll"))
                DaysPerPayroll = int.Parse(node.GetValue("DaysPerPayroll"));

            if (node.HasValue("PayrollPerWorker"))
                PayrollPerWorker = int.Parse(node.GetValue("PayrollPerWorker"));

            if (node.HasValue("PayrollPerAstronaut"))
                PayrollPerAstronaut = int.Parse(node.GetValue("PayrollPerAstronaut"));

            if (node.HasValue("IntegrationPerWorker"))
                IntegrationPerWorker = int.Parse(node.GetValue("IntegrationPerWorker"));
            #endregion

            #region Launch Failures
            if (node.HasValue("MaxStagingCheckTime"))
                BARISLaunchFailManager.MaxStagingCheckTime = float.Parse(node.GetValue("MaxStagingCheckTime"));

            if (node.HasValue("MinOrbitFailTime"))
                BARISLaunchFailManager.MinOrbitFailTime = float.Parse(node.GetValue("MinOrbitFailTime"));

            if (node.HasValue("MaxOrbitFailTime"))
                BARISLaunchFailManager.MaxOrbitFailTime = float.Parse(node.GetValue("MaxOrbitFailTime"));
            #endregion
        }
        #endregion
    }
}
