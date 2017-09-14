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
    public delegate void OnMaintenanceCheckEvent(ModuleQualityControl qualityControl, QualityCheckResult qualityResult);
    public delegate void OnMTBFQualityCheckEvent(ModuleQualityControl qualityControl, QualityCheckResult qualityResult);
    public delegate double OnRepairResourceRequestEvent(ModuleQualityControl qualityControl, double repairUnits);

    /// <summary>
    /// ModuleQualityControl is the heart of the quality check system for parts. It keeps track of the quality state of the part, the mean time between failures (MTBF), the broken/fixed state of the part,
    /// and other parameters. It is the controller that breakable part modules rely upon to tell them of the current state of the part. It interfaces with BARISScenario and relies upon the scenario object
    /// for timekeeping events and useful utilities. In a config file, ModuleQualityControl appears after all breakable part modules and the part modules that they control. A part config should
    /// only have one ModuleQualityControl.
    /// </summary>
    public class ModuleQualityControl : BaseQualityControl
    {
        #region Fields
        /// <summary>
        /// Fired when a maintenance check occurs, which is an attempt to improve the part's quality when it is out of MTBF.
        /// </summary>
        public event OnMaintenanceCheckEvent onMaintenanceCheck;

        /// <summary>
        /// Fired when a quality check is made when the part still has MTBF remaining. Generally parts don't fail when they have MTBF remaining; only a critical fail will declare the part broken.
        /// </summary>
        public event OnMTBFQualityCheckEvent onMTBFQualityCheck;

        /// <summary>
        /// Fired when the part can't find the resource required to maintain or repair the part anywhere within the vessel. It gives other mods a chance to help out.
        /// </summary>
        public event OnRepairResourceRequestEvent onRepairResourceRequest;

        /// <summary>
        /// Determines whether or not to show the GUI; some mods like to have their own displays.
        /// </summary>
        [KSPField()]
        public bool guiVisible = true;

        /// <summary>
        /// From 0 to 100, how reliable is the part? Low values mean the part is prone to breaking. This is the original quality of the part.
        /// </summary>
        [KSPField(isPersistant = true)]
        public int quality = 5;

        /// <summary>
        /// No mater what bonuses are applied, the part's maximum quality cannot exceed the cap. If > -1 then this cap will be applied. Otherwise the QualityCap from BARISSettings will be used.
        /// </summary>
        [KSPField(isPersistant = true)]
        public int qualityCap = -1;

        /// <summary>
        /// No mater what bonuses are applied, the part's maximum MTBF cannot exceed the cap. If > -1 then this cap will be applied. Otherwise the MTBFCap from BARISScenario will be used.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double mtbfCap = -1.0f;

        /// <summary>
        /// Parts benefit from spending time in a vehicle integration facility. The more time spent/more money spent, the greater the bonus.
        /// </summary>
        [KSPField(isPersistant = true)]
        public int integrationBonus = 0;

        /// <summary>
        /// Parts benefit from flight experience. The more times that a part successfully makes its quality check during staging, the more flight experience parts of this type gains.
        /// </summary>
        [KSPField(isPersistant = true)]
        public int flightExperienceBonus = 0;

        /// <summary>
        /// The more times a part flies, the more MTBF new parts gain.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double mtbfBonus = -1.0f;

        /// <summary>
        /// Friendly MTBF display units used with GetInfo.
        /// </summary>
        [KSPField()]
        public string qualityUnits = "Hours";

        /// <summary>
        /// What skill to use when performing the quality check. This is not always the same skill required to repair or maintain the part.
        /// </summary>
        [KSPField()]
        public string qualityCheckSkill = "RepairSkill";

        /// <summary>
        /// Current quality level. When currentQuality == 0, the part is Broken. When repairs are complete, currentQuality will be restored to quality.
        /// </summary>
        [KSPField(isPersistant = true)]
        public int currentQuality = -1;

        /// <summary>
        /// Measured in hours, mean time between failures. This value represent the maximum possible mean time between failures. It degrades over time when you perform repairs.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double mtbf = 1;

        /// <summary>
        /// Current mtbf, in hours. During this time, the part is unlikely to break unless you roll a critical failure. 
        /// Once this time expires, a failure result will reduce part quality, 
        /// and a critical failure will automatically break the part. A critical success will prevent the next failure or critical failure.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double currentMTBF = -1.0f;

        /// <summary>
        /// When a part is repaired, multiply mtbf by this multiplier to get how many hours can elapse before the part starts to break down again.
        /// </summary>
        [KSPField(isPersistant = true)]
        public float mtbfRepairMultiplier = 0.7f;

        /// <summary>
        /// How many times has the part been repaired? The greater the number, the lesser the maximum possible quality.
        /// </summary>
        [KSPField(isPersistant = true)]
        public int repairCount;

        /// <summary>
        /// When a part runs out of currentMTBF, then an in-game email gets sent. This flag lets us know we already sent the mail.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool maintenanceEmailSent;

        /// <summary>
        /// Skill used to perform maintenance and to perform a saving throw when a part fails its quality check. 
        /// A successful maintenance check will improve the quality of the part, improving its chances of not breaking once currentMTBF has run out. 
        /// For the saving throw, the highest ranking kerbal aboard with this skill can attempt to avert disaster.
        /// </summary>
        [KSPField()]
        public string maintenanceSkill = "RepairSkill";

        /// <summary>
        /// Minimum skill level needed to maintain or repair the part.
        /// </summary>
        [KSPField()]
        public int minimumSkillLevel = 1;

        /// <summary>
        /// Resource used to perform repairs and preventative maintenance on the part.
        /// </summary>
        [KSPField()]
        public string maintenanceResource = "Equipment";

        /// <summary>
        /// Specifies what percentage of the part's dry mass is required in order to perform maintenance. The number of units of repair resource required depends upon this value.
        /// </summary>
        [KSPField()]
        public float maintenanceMassPercent = 1f;

        /// <summary>
        /// Specifies what percentage of the part's dry mass is required in order to repair a broken part. The number of units of repair resource required depends upon this value. Defaults to 10%
        /// </summary>
        [KSPField()]
        public float repairMassPercent = 10f;

        /// <summary>
        /// Field that indicates whether or not to delcare the part broken upon start of the part module. This is set to true when an unloaded vessel fails a quality check.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isBrokenOnStart;

        /// <summary>
        /// Field that indicates the part is declared fixed upon start of the part module. This is set when a Tiger Team from Mission Control successfully fixes the part.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isFixedOnStart;

        /// <summary>
        /// The number of breakable modules in the part. Mostly used during vessel reliability checks to make sure we don't cause the part to break too many times.
        /// </summary>
        [KSPField(isPersistant = true)]
        public int breakableModuleCount = -1;

        /// <summary>
        /// For game performance reasons, it's better to derive a converter from ModuleBreakableConverter, ModleBreakableHarvester, or ModuleBreakableAsteroidDrill. 
        /// That's not always possible. As an alternative, set monitorConverters to true, and quality control will actively poll any converters/drills on the part 
        /// for their status and prevent them from being used until they're repaired. You will take a performance hit with this method, but it's better than nothing.
        /// </summary>
        [KSPField()]
        public bool monitorConverters;

        /// <summary>
        /// Flag to indicate that we've recorded our successful staging event check in the flight log.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool recordedFlightLog;

        /// <summary>
        /// Flag to indicate that one or more breakable part modules is activated.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isActivated;

        /// <summary>
        /// When a part is de-activated, it loses MTBF at a lower rate. The amount of MTBF it loses per Reliability check is multiplied by this value.
        /// </summary>
        [KSPField(isPersistant = true)]
        public float mtbfHibernationFactor = 0.1f;

        /// <summary>
        /// Result of last quality check.
        /// </summary>
        public QualityCheckResult lastQualityCheck;

        /// <summary>
        /// List of breakable part modules.
        /// </summary>
        List<ICanBreak> breakableParts = new List<ICanBreak>();

        //When a part is broken, its highlight color turns red and flashes for a few seconds.
        //These parameters help keep track of the highlighting.
        protected Color originalHighlightColor;
        protected double highlightStartTime;
        protected BaseConverter[] baseConverters = null;
        protected int totalConverters = 0;
        double deathWatchStart = 0;
        #endregion

        #region API
        public override void GetQualityStats(out int Quality, out int CurrentQuality, out double MTBF, out double CurrentMTBF)
        {
            Quality = this.quality;
            CurrentQuality = this.currentQuality;
            MTBF = this.mtbf;
            CurrentMTBF = this.currentMTBF;
        }

        /// <summary>
        /// This event repairs the part after it has been declared broken. To successfully repair the part, the vessel must have sufficient resources and the mechanic must have the correct repair skill.
        /// Additionally, the mechanic must have the minimum skill level. This event is always available to an astronaut on EVA, but it might not be available to kerbals inside the vessel depending upon
        /// the difficulty settings. Regardless, the repair button won't be available until a part has been declared broken.
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveUnfocused = true, externalToEVAOnly = false, unfocusedRange = 5.0f, guiName = "Repair Part")]
        public override void RepairPart()
        {
            double repairUnits = getRepairUnits(repairMassPercent);
            debugLog(this.part.partInfo.title + " requires " + repairUnits + " units of " + maintenanceResource + " to repair.");
            string message;

            //Make sure we have sufficient skill
            if (BARISSettings.RepairsRequireSkill && !hasSufficientSkill())
            {
                //Inform player
                if (this.part.vessel == FlightGlobals.ActiveVessel)
                {
                    message = Localizer.Format(BARISScenario.InsufficientRepairSkill) +
                        part.partInfo.title + Localizer.Format(BARISScenario.SkillRequired) +
                        minimumSkillLevel + " " + getAllowedTraits();
                    BARISScenario.Instance.LogPlayerMessage(message);
                }
                return;
            }

            //Make sure we can afford the cost
            if (BARISSettings.RepairsRequireResources && !paidRepairCost(repairUnits))
            {
                //Inform player
                if (this.part.vessel == FlightGlobals.ActiveVessel)
                {
                    message = Localizer.Format(BARISScenario.InsufficientResources1) + part.partInfo.title +
                        BARISScenario.InsufficientResources2 + string.Format("{0:f2}", repairUnits) + Localizer.Format(BARISBridge.MsgBodyUnitsOf) +
                        maintenanceResource + ".";
                    BARISScenario.Instance.LogPlayerMessage(message);
                }
                return;
            }

            //Ok, part is fixed
            DeclarePartFixed();
        }

        /// <summary>
        /// Only available when a part needs maintenance, the event enables astronauts to perform maintenance on the part. The maintenance button isn't available until the part has run out of MTBF.
        /// In order to perform maintenance, the vessel must have sufficient resources, and the astronaut must have sufficient skill (both the required skill and required rank). The event button is
        /// always available to astronauts on EVA but might not be available to kerbals inside the vessel depending upon difficulty settings.
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveUnfocused = true, externalToEVAOnly = false, unfocusedRange = 5.0f, guiName = "Perform Maintenance")]
        public override void PerformMaintenance()
        {
            double maintenanceUnits = getRepairUnits(maintenanceMassPercent);
            debugLog(this.part.partInfo.title + " requires " + maintenanceUnits + " units of " + maintenanceResource + " to maintain.");
            string message;

            //Make sure we have sufficient skill
            if (BARISSettings.RepairsRequireSkill && !hasSufficientSkill())
            {
                //Inform player
                if (this.part.vessel == FlightGlobals.ActiveVessel)
                {
                    message = Localizer.Format(BARISScenario.InsufficientRepairSkill) +
                        part.partInfo.title + Localizer.Format(BARISScenario.SkillRequired) +
                        minimumSkillLevel + " " + getAllowedTraits();
                    BARISScenario.Instance.LogPlayerMessage(message);
                }
                return;
            }

            //Make sure we can afford the cost
            if (BARISSettings.RepairsRequireResources && !paidRepairCost(maintenanceUnits))
            {
                //Inform player
                if (this.part.vessel == FlightGlobals.ActiveVessel)
                {
                    message = Localizer.Format(BARISScenario.InsufficientResources1) + part.partInfo.title +
                        BARISScenario.InsufficientResources2 + string.Format("{0:f2}", maintenanceUnits) + Localizer.Format(BARISBridge.MsgBodyUnitsOf) +
                        maintenanceResource + ".";
                    BARISScenario.Instance.LogPlayerMessage(message);
                }
                return;
            }

            //OK, we can perform a maintenance check.
            QualityCheckResult qualityResult = BARISScenario.Instance.PerformQualityCheck(this.part.vessel, currentQuality, qualityCheckSkill, false);

            switch (qualityResult.statusResult)
            {
                default:
                case QualityCheckStatus.success:
                    PerformQualityMaintSuccess();
                    break;

                case QualityCheckStatus.criticalSuccess:
                    PerformQualityMaintSuccess();
                    break;

                case QualityCheckStatus.criticalFail:
                    PerformQualityMaintCriticalFail();
                    break;

                case QualityCheckStatus.fail:
                    break;

                case QualityCheckStatus.astronautAverted:
                    if (this.part.vessel == FlightGlobals.ActiveVessel)
                    {
                        message = qualityResult.astronaut.name + Localizer.Format(BARISScenario.QualityCheckFailAvertedAstronaut1) + this.part.partInfo.title +
                            Localizer.Format(BARISScenario.QualityCheckFailAvertedAstronaut2);
                        BARISScenario.Instance.LogPlayerMessage(message);
                    }
                   break;

                case QualityCheckStatus.eventCardAverted:
                    break;
            }

            //Fire event
            if (this.onMaintenanceCheck != null)
                onMaintenanceCheck(this, qualityResult);
        }

        /// <summary>
        /// Instructs the part module to perform a quality check immediately and drive the current state. 
        /// Typically this check happens each time a converter or drill or other breakable part begins operation.
        /// </summary>
        [KSPEvent(guiName = "Perform Quality Check")]
        public override void PerformQualityCheck()
        {
            if (!BARISScenario.partsCanBreak)
            {
                debugLog(this.part.partInfo.title + ": Quality check skipped, parts can't break.");
                return;
            }

            debugLog("Performing quality check for " + this.part.partInfo.title);

            //If we're already broken then there's no need to perform a quality check.
            if (IsBroken)
            {
                BreakAPartModule();
                return;
            }

            //Make the check.
            lastQualityCheck = BARISScenario.Instance.PerformQualityCheck(this.part.vessel, currentQuality, qualityCheckSkill);

            //Drive the state based upon the result.
            UpdateQualityState(lastQualityCheck);

            //Update the flight log
            UpdateFlightLog();

            //Fire event
            if (this.onMTBFQualityCheck != null)
                onMTBFQualityCheck(this, lastQualityCheck);
        }

        /// <summary>
        /// This event declares the part broken. When that occurs, its quality is set to 0, a player message is sent and logged, the part is highlighted in red,
        /// and at least one of the breakable part modules will be selected and informed that the part modules under its control are broken. It will also fire the 
        /// onPartBroken event.
        /// </summary>
        [KSPEvent(guiName = "Declare Broken")]
        public override void DeclarePartBroken()
        {
            //Make sure that quality is updated
            currentQuality = 0;
            UpdateQualityDisplay(BARISScenario.GetConditionSummary(0, 0, 0, 0));

            //Enable the repair button
            Events["RepairPart"].active = true;
            Events["RepairPart"].guiActive = !BARISSettings.RepairsRequireEVA;

            //Send email signifying that the part is broken.
            SendPartBrokenEmail();

            //Provide an on screen message too
            if (this.part.vessel == FlightGlobals.ActiveVessel)
            {
                string message = BARISBridge.MsgBodyA + this.part.partInfo.title + BARISBridge.MsgBodyBroken1 + GetRepairCost() + BARISBridge.MsgBodyBroken2;
                BARISScenario.Instance.LogPlayerMessage(message);
            }

            //Highlight the part on and off for a few seconds.
            SetupBrokenHighlighting();

            //If we only have one breakable part module then fire the part broken event.
            if (breakableParts.Count == 1)
                FireOnPartBroken();

            //We have more than one breakable part, such as Brumby's RCS and transmitter.
            //Randomly select one to fail.
            else if (breakableModuleCount > 0)
            {
                List<ICanBreak> breakables = new List<ICanBreak>();

                //Find the modules that are activated.
                foreach (ICanBreak breakablePart in breakableParts)
                    if (breakablePart.ModuleIsActivated())
                        breakables.Add(breakablePart);
                if (breakables.Count == 0)
                    return;

                int failedModuleIndex = UnityEngine.Random.Range(0, breakables.Count - 1);
                breakables[failedModuleIndex].OnPartBroken(this);
                breakableModuleCount -= 1;
            }

            debugLog("Part " + this.part.partInfo.title + " is declared broken.");
        }

        public virtual void DeclarePartFixedOnStart()
        {
            //Disable the repair button
            Events["RepairPart"].active = false;

            //Reset highlight color
            resetHighlightColor();

            //Inform player
            if (this.part.vessel == FlightGlobals.ActiveVessel && FlightGlobals.ActiveVessel.isEVA)
            {
                string message = Localizer.Format(this.part.partInfo.title + BARISScenario.PartFixedMessage);
                BARISScenario.Instance.LogPlayerMessage(message);
            }

            //Fire part fixed event.
            FireOnPartFixed();

            debugLog("Part " + this.part.partInfo.title + " is declared fixed.");
        }

        /// <summary>
        /// This event declares the part fixed. When that occurs, the part's maximum quality will be restored with the number of times it's been repaired accounted for. Additionally,
        /// its current quality will be restored to the maximum possible. If parts wear out, then the maximum possible MTBF will be reduced somewhat, and its current MTBF restored.
        /// Finally, the part's highlight color is restored, a player message is sent and logged, and the onPartFixed event is fired.
        /// </summary>
        [KSPEvent(guiName = "Declare Fixed")]
        public virtual void DeclarePartFixed()
        {
            //Keep track of how many times the part has been repaired.
            repairCount += 1;

            //Reset breakable part count
            breakableModuleCount = breakableParts.Count;

            //Reset quality
            currentQuality = MaxQuality;

            //Reset current mtbf
            currentMTBF = MaxMTBF;
            UpdateQualityDisplay(BARISScenario.GetConditionSummary(currentMTBF, currentMTBF, currentQuality, currentQuality));

            //Disable the repair button
            Events["RepairPart"].active = false;

            //Reset highlight color
            resetHighlightColor();

            //Inform player
            if (this.part.vessel == FlightGlobals.ActiveVessel && FlightGlobals.ActiveVessel.isEVA)
            {
                string message = Localizer.Format(this.part.partInfo.title + BARISScenario.PartFixedMessage);
                BARISScenario.Instance.LogPlayerMessage(message);
            }

            //Fire part fixed event.
            FireOnPartFixed();

            //Remove any Tiger Team efforts if all the broken parts on the vessel are fixed.
            List<ModuleQualityControl> qualityModules = this.part.vessel.FindPartModulesImplementing<ModuleQualityControl>();
            if (qualityModules.Count > 0)
            {
                bool allPartsFixed = true;

                for (int index = 0; index < qualityModules.Count; index++)
                {
                    if (qualityModules[index].currentQuality <= 0)
                    {
                        allPartsFixed = false;
                        break;
                    }
                }

                if (allPartsFixed)
                    BARISScenario.Instance.CancelRepairProject(this.part.vessel);
            }

            debugLog("Part " + this.part.partInfo.title + " is declared fixed.");
        }

        /// <summary>
        /// When one of the breakable part modules experiences a state change, it should call this method to ensure that ModuleQualityControl properly keeps track of activation state.
        /// Only parts that are activated are considered during vessel reliability checks. Some breakable modules, like fuel tanks, are always active, and others like breakble RCS
        /// modules are only active when the game's RCS is active. Since parts can have more than one breakable part module, if any one of is active, then the part is considered active
        /// and can be considered during vessel reliability checks.
        /// </summary>
        public override void UpdateActivationState()
        {
            ICanBreak[] breakables = breakableParts.ToArray();

            //We're activated if at least one of the breakables is activated.
            for (int index = 0; index < breakables.Length; index++)
            {
                if (breakables[index].ModuleIsActivated())
                {
                    isActivated = true;
                    return;
                }
            }

            isActivated = false;
        }

        /// <summary>
        /// Parts can have more than one breakable part module. When a part breaks during vessel reliability checks, one or more part modules are declared broken. A part can be broken many times
        /// until it runs out of breakable part modules.
        /// </summary>
        public void BreakAPartModule()
        {
            //If we have more than one breakable part module then
            //select another one at random to fail.
            if (breakableParts.Count > 1 && breakableModuleCount > 0)
            {
                if (this.part.vessel == FlightGlobals.ActiveVessel)
                {
                    string message = this.part.partInfo.title + Localizer.Format(BARISScenario.ComponentFailure);
                    BARISScenario.Instance.LogPlayerMessage(message);
                }

                //Tell one of the ICanBreak modules that it's broken.
                List<ICanBreak> breakables = new List<ICanBreak>();

                //Find the modules that are activated.
                foreach (ICanBreak breakablePart in breakableParts)
                    if (breakablePart.ModuleIsActivated())
                        breakables.Add(breakablePart);
                if (breakables.Count == 0)
                    return;

                int failedModuleIndex = UnityEngine.Random.Range(0, breakables.Count - 1);
                breakables[failedModuleIndex].OnPartBroken(this);

                //Update the breakable part count
                breakableModuleCount -= 1;
            }
        }

        /// <summary>
        /// Generally called when one of the breakble part modules is declared broken, this method provides a way to update the user-friendly condition display.
        /// </summary>
        /// <param name="displayString"></param>
        public override void UpdateQualityDisplay(string displayString)
        {
            qualityDisplay = displayString;
            debugLog("Condition: " + qualityDisplay);

            //Dirty the GUI
            MonoUtilities.RefreshContextWindows(this.part);
        }

        /// <summary>
        /// This method return the maximum possible quality that the part can have. It is based upon its base quality, the integration bonus, the flight experience, and the quality cap.
        /// </summary>
        /// <returns>An integer value between 0 and the quality cap (which can be up to 100)</returns>
        public int GetMaxQuality()
        {
            return BARISScenario.GetMaxQuality(quality, integrationBonus, flightExperienceBonus, qualityCap, repairCount);
        }

        /// <summary>
        /// This method returns the maximum possible MTBF that the part can have. It is based upon the part's MTBF rating, R&D facility bonus, and its flight experience, and capped to the max value allowed.
        /// </summary>
        /// <returns>A double containing the maximum MTBF</returns>
        public double GetMaxMTBF()
        {
            if (mtbfBonus <= 0)
                mtbfBonus = BARISScenario.Instance.GetMTBFBonus(this.part, mtbf);

            return BARISScenario.GetMaxMTBF(mtbf, mtbfCap, repairCount, mtbfRepairMultiplier, mtbfBonus);
        }

        /// <summary>
        /// A property that returns the maximum MTBF of the part.
        /// </summary>
        public double MaxMTBF
        {
            get
            {
                return GetMaxMTBF();
            }
        }

        /// <summary>
        /// A property that contains the maximum quality of the part.
        /// </summary>
        public int MaxQuality
        {
            get
            {
                return GetMaxQuality();
            }
        }

        /// <summary>
        /// A flag that indicates whether or not the part is broken.
        /// </summary>
        public bool IsBroken
        {
            get
            {
                if (currentQuality <= 0)
                    return true;
                else
                    return false;
            }

            set
            {
                if (value)
                {
                    DeclarePartBroken();
                }
            }
        }

        /// <summary>
        /// This method drives the state machine in ModuleQualityControl. How the state machine responds depends upon the quality result and whether or not the state
        /// is driven by a staging event.
        /// </summary>
        /// <param name="qualityResult">The QualityCheckResult to process</param>
        /// <param name="isStagingEvent">A bool value indicating whether or not to process the quality result is a staging event.</param>
        public virtual void UpdateQualityState(QualityCheckResult qualityResult, bool isStagingEvent = false)
        {
            string message;

            switch (qualityResult.statusResult)
            {
                default:
                case QualityCheckStatus.success:
                    //Parts that are past their design life should eventually start
                    //to wear out. Even with a successful result, there's a small chance
                    //that the part will lose a bit of quality. That way, even parts of
                    //Excellent quality will break down over time.
                    if (currentMTBF <= 0)
                    {
                        if (UnityEngine.Random.Range(1, 100) < BARISScenario.WearAndTearTargetNumber)
                        {
                            currentQuality -= BARISScenario.WearAndTearQualityLoss;

                            //Update the quality display
                            UpdateQualityDisplay(BARISScenario.GetConditionSummary(currentMTBF, MaxMTBF, currentQuality, MaxQuality));

                            //If needed, declare part broken
                            if (currentQuality <= 0)
                            {
                                DeclarePartBroken();
                            }
                        }
                    }
                    break;

                case QualityCheckStatus.criticalSuccess:
                    break;

                case QualityCheckStatus.astronautAverted:
                    if (this.part.vessel == FlightGlobals.ActiveVessel)
                    {
                        message = qualityResult.astronaut.name + Localizer.Format(BARISScenario.QualityCheckFailAvertedAstronaut1) + this.part.partInfo.title +
                            Localizer.Format(BARISScenario.QualityCheckFailAvertedAstronaut2);
                        BARISScenario.Instance.LogPlayerMessage(message);
                    }
                    break;

                case QualityCheckStatus.eventCardAverted:
                    break;

                case QualityCheckStatus.fail:
                    //Staging events can cause a part to fail even when we have MTBF remaining.
                    if (isStagingEvent)
                    {
                        DeclarePartBroken();
                    }

                    //If currentMTBF <= 0, reduce quality a bit
                    //Otherwise, do nothing
                    else if (currentMTBF <= 0)
                    {
                        //Reduce quality
                        currentQuality -= UnityEngine.Random.Range(1, BARISScenario.QualityCheckFailLoss);

                        //Update the quality display
                        UpdateQualityDisplay(BARISScenario.GetConditionSummary(0, MaxMTBF, currentQuality, MaxQuality));

                        //If our quality has dropped too much, then the part is broken
                        if (currentQuality <= 0)
                        {
                            DeclarePartBroken();
                        }
                    }
                    break;

                case QualityCheckStatus.criticalFail:
                    //On a critical failure, the part might just explode it it is out of MTBF.
                    if (BARISBreakableParts.FailuresCanExplode && currentMTBF <= 0)
                    {
                        if (UnityEngine.Random.Range(1, 100) >= BARISBreakableParts.ExplosivePotentialCritical)
                        {
                            message = this.part.partInfo.title + Localizer.Format(BARISScenario.CatastrophicFailure);
                            BARISScenario.Instance.LogPlayerMessage(message);
                            this.part.explode();
                        }

                        //No explosion, but we're broken.
                        else
                        {
                            DeclarePartBroken();
                        }
                    }

                    //Just declare the part broken.
                    else
                    {
                        DeclarePartBroken();
                    }
                    break;
            }
        }

        /// <summary>
        /// This method updates the flight log for the part's type. It is called when a part successfully makes its quality check during a staging event.
        /// The flight experience gained won't affect the existing part; it is only applied to new parts. A part can only update the flight log once;
        /// subsequent calls to this method do nothing.
        /// </summary>
        public void UpdateFlightLog()
        {
            //Contribute to the flight log
            if (!recordedFlightLog && lastQualityCheck.statusResult == QualityCheckStatus.success)
            {
                recordedFlightLog = true;
                BARISScenario.Instance.RecordFlightExperience(this.part);
            }
        }

        /// <summary>
        /// This method decrements the part's mean time between failure (MTBF). If the MTBF drops to 0, then the part might become broken on subsequent quality checks
        /// if its quality drops to 0. If the MTBF drops to 0, then astronauts may perform maintenance on the part to temporarily restore its quality.
        /// </summary>
        /// <param name="amount">A double with the amount of MTBF to decrement.</param>
        public virtual void DecrementMTBF(double amount = 1.0f)
        {
            currentMTBF -= amount;
            if (currentMTBF <= 0)
            {
                currentMTBF = 0;
                Events["PerformMaintenance"].active = true;
                Events["PerformMaintenance"].guiActive = !BARISSettings.RepairsRequireEVA;
                SendMaintenanceEmail();
            }

            debugLog("currentMTBF decremented, new value: " + currentMTBF);
        }

        /// <summary>
        /// This method decrements the part's current quality rating. If it drops to 0, then the part is declared broken.
        /// </summary>
        /// <param name="amount">An integer with the amount of quality to decrement.</param>
        public virtual void DecrementQuality(int amount = 1)
        {
            currentQuality -= 1;
            if (currentQuality <= 0)
            {
                currentQuality = 0;
                DeclarePartBroken();
            }

            //Update the quality display
            UpdateQualityDisplay(BARISScenario.GetConditionSummary(currentMTBF, MaxMTBF, currentQuality, MaxQuality));
            debugLog("currentQuality: " + currentQuality);
        }

        /// <summary>
        /// This method declares a critical failure during a maintenance check. It's provided for diagnostic purposes. A critically failed maintenance check
        /// will drop the quality a bit. If it drops to 0, then the part is declared broken.
        /// </summary>
        public virtual void PerformQualityMaintCriticalFail()
        {
            //Reduce quality
            currentQuality -= UnityEngine.Random.Range(1, BARISScenario.QualityCheckFailLoss);

            //Inform player
            if (this.part.vessel == FlightGlobals.ActiveVessel)
            {
                string message = Localizer.Format(BARISScenario.PartMaintenanceCriticalFail);
                BARISScenario.Instance.LogPlayerMessage(message);
            }

            //If our quality has dropped too much, then the part is broken
            if (currentQuality <= 0)
            {
                currentQuality = 0;
                DeclarePartBroken();
            }

            UpdateQualityDisplay(BARISScenario.GetConditionSummary(currentMTBF, MaxMTBF, currentQuality, MaxQuality));
            debugLog("Part " + this.part.partInfo.title + " is maintenance critical fail.");
        }

        /// <summary>
        /// This method declares the maintenance check a success. It is provided fo diagnosic purposes. A successful maintenance check will temporarily improve the part's quality (which can drop again due to failed quality checks).
        /// </summary>
        public virtual void PerformQualityMaintSuccess()
        {
            //So we only compute this once...
            int maxQuality = MaxQuality;

            //Improve quality
            currentQuality += BARISScenario.MaintenanceQualityImprovement;
            if (currentQuality > maxQuality)
                currentQuality = maxQuality;

            //Inform the player
            if (this.part.vessel == FlightGlobals.ActiveVessel)
            {
                string message = Localizer.Format(BARISScenario.PartMaintenanceSuccessful);
                BARISScenario.Instance.LogPlayerMessage(message);
            }

            UpdateQualityDisplay(BARISScenario.GetConditionSummary(currentMTBF, MaxMTBF, currentQuality, MaxQuality));
            debugLog("Part " + this.part.partInfo.title + " is maintenance succeeded.");
        }

        /// <summary>
        /// This method sends the maintenance email that is sent out when a part's MTBF drops to 0. It is provided for diagnostic purposes.
        /// </summary>
        public virtual void SendMaintenanceEmail()
        {
            if (!BARISSettings.EmailMaintenanceRequests)
                return;

            maintenanceEmailSent = true;
            StringBuilder resultsMessage = new StringBuilder();
            MessageSystem.Message msg;

            resultsMessage.AppendLine(Localizer.Format(BARISScenario.FromVessel) + this.part.vessel.vesselName);
            resultsMessage.AppendLine(Localizer.Format(BARISScenario.SubjectMaintenance));
            resultsMessage.AppendLine(Localizer.Format(BARISBridge.MsgBodyA) + this.part.partInfo.title + Localizer.Format(BARISBridge.MsgMaintenance) +
                string.Format("{0:f2}", getRepairUnits(maintenanceMassPercent)) + Localizer.Format(BARISBridge.MsgBodyUnitsOf) + maintenanceResource + ".");

            resultsMessage.Append(Localizer.Format(BARISBridge.MsgTakesASkilled));
            resultsMessage.Append(getAllowedTraits());
            resultsMessage.Append(Localizer.Format(BARISBridge.MsgToEffectRepairs));

            msg = new MessageSystem.Message(this.part.vessel.vesselName + Localizer.Format(BARISScenario.MsgTitleMaintenance), resultsMessage.ToString(),
                MessageSystemButton.MessageButtonColor.ORANGE, MessageSystemButton.ButtonIcons.ALERT);
            MessageSystem.Instance.AddMessage(msg);
        }

        /// <summary>
        /// This method sends the repair request email that gets sent out when a part's quality drops to 0. It is provided for diagnostic purposes.
        /// </summary>
        public virtual void SendPartBrokenEmail()
        {
            if (!BARISSettings.EmailRepairRequests)
                return;

            StringBuilder resultsMessage = new StringBuilder();
            MessageSystem.Message msg;

            resultsMessage.AppendLine(Localizer.Format(BARISScenario.FromVessel) + this.part.vessel.vesselName);
            resultsMessage.AppendLine(Localizer.Format(BARISScenario.SubjectRepair));
            resultsMessage.AppendLine(Localizer.Format(BARISBridge.MsgBodyA) + this.part.partInfo.title + Localizer.Format(BARISBridge.MsgBodyBroken1) +
                string.Format("{0:f2}", getRepairUnits(repairMassPercent)) + Localizer.Format(BARISBridge.MsgBodyUnitsOf) + maintenanceResource + Localizer.Format(BARISBridge.MsgBodyBroken2));

            resultsMessage.Append(Localizer.Format(BARISBridge.MsgTakesASkilled));
            resultsMessage.Append(getAllowedTraits());
            resultsMessage.Append(Localizer.Format(BARISBridge.MsgToEffectRepairs));

            msg = new MessageSystem.Message(this.part.vessel.vesselName + Localizer.Format(BARISScenario.MsgTitleRepair), resultsMessage.ToString(),
                MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.FAIL);
            MessageSystem.Instance.AddMessage(msg);
        }

        /// <summary>
        /// This method hides or shows the GUI buttons depending upon the isVisible flag.
        /// </summary>
        /// <param name="isVisible">True if the GUI should be shown, false if not.</param>
        public virtual void SetupGUI(bool isVisible = true)
        {
            //Setup event buttons.
            //Repairs might require EVAs
            if (currentQuality <= 0)
                Events["RepairPart"].active = BARISSettings.RepairsRequireEVA;
            else
                Events["RepairPart"].active = false;

            if (currentMTBF <= 0)
                Events["PerformMaintenance"].active = isVisible;
            else
                Events["PerformMaintenance"].active = false;

            //Setup EVA-only repairs.
            Events["RepairPart"].guiActive = !BARISSettings.RepairsRequireEVA;
            Events["PerformMaintenance"].guiActive = !BARISSettings.RepairsRequireEVA;

            if (BARISScenario.showDebug)
            {
                Events["DeclarePartBroken"].guiActive = true;
                Events["DeclarePartFixed"].guiActive = true;
            }
        }

        /// <summary>
        /// Calculates the repair cost in units of resource required to maintain or repair the part. This method is used for display messages.
        /// </summary>
        /// <returns>A string representation of the required repair units.</returns>
        public override string GetRepairCost()
        {
            double repairUnits;

            if (currentQuality <= 0)
                repairUnits = getRepairUnits(repairMassPercent);
            else if (currentMTBF <= 0)
                repairUnits = getRepairUnits(maintenanceMassPercent);
            else
                return string.Empty;

            return string.Format("{0:f2}", repairUnits) + BARISBridge.MsgBodyUnitsOf + maintenanceResource;
        }

        /// <summary>
        /// Calculates the repair cost in units of resource required to maintain or repair the part.
        /// </summary>
        /// <returns>A double representating of the required repair units.</returns>
        public virtual double GetRepairUnits()
        {
            double repairUnits;

            if (currentQuality <= 0)
                repairUnits = getRepairUnits(repairMassPercent);
            else if (currentMTBF <= 0)
                repairUnits = getRepairUnits(maintenanceMassPercent);
            else
                return 0;

            return repairUnits;
        }

        /// <summary>
        /// Given the way that KSP works, when you decouple parts, the decoupled parts become a new vessel.
        /// We want to cover the situations where a launch abort separates parts and the situation where it doesn't.
        /// Hence, we mark a part for death, and let it handle the vessel destruction. If the marked part is on a
        /// separate vessel than the active one, great! We get good explosions. If not, the active vessel is destroyed.
        /// </summary>
        /// <param name="isMarkedForDeath">A bool indicating whether or not the part is marked for death.</param>
        public virtual void SetMarkedForDeath(bool isMarkedForDeath)
        {
            if (isMarkedForDeath)
            {
                deathWatchStart = Planetarium.GetUniversalTime();
                BARISScenario.Instance.onTimeTickEvent += checkTimeToDie;
            }
            else
            {
                BARISScenario.Instance.onTimeTickEvent -= checkTimeToDie;
            }
        }

        protected void checkTimeToDie()
        {
            double elapsedTime = Planetarium.GetUniversalTime() - deathWatchStart;

            if (elapsedTime >= BARISLaunchFailManager.VesselDestructionDelay)
            {
                //Deactivate our timer.
                BARISScenario.Instance.onTimeTickEvent -= checkTimeToDie;

                //Walk through all the parts and make them explode.
                //This works even when we're the active vessel.
                Part[] parts = this.part.vessel.parts.ToArray();
                for (int index = 0; index < parts.Length; index++)
                {
                    parts[index].explode();
                }
                this.part.explode();
            }
        }

        #endregion

        #region Overrides
        public override string GetInfo()
        {
            string baseInfo = base.GetInfo();
            string mtbfInfo = BARISScenario.MTBFLabel + string.Format("{0:f2}", mtbf) + " " + qualityUnits;

            return baseInfo + mtbfInfo;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasValue("originalHighlightColor"))
            {
                string[] rgbaValues = node.GetValue("originalHighlightColor").Split(new char[] { ',' });
                originalHighlightColor.r = float.Parse(rgbaValues[0]);
                originalHighlightColor.g = float.Parse(rgbaValues[1]);
                originalHighlightColor.b = float.Parse(rgbaValues[2]);
                originalHighlightColor.a = float.Parse(rgbaValues[3]);
            }

            if (node.HasValue("mtbfHibernationFactor"))
                mtbfHibernationFactor = float.Parse(node.GetValue("mtbfHibernationFactor"));
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            string rgbaValues = originalHighlightColor.r.ToString() + "," + originalHighlightColor.g.ToString() + "," + originalHighlightColor.b.ToString() + "," + originalHighlightColor.a.ToString();
            node.AddValue("originalHighlightColor", rgbaValues);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            debugLog("OnStart called");

            //Converter monitoring (WARNING: this method incurrs a performance hit)
            if (monitorConverters)
            {
                BARISScenario.Instance.onTimeTickEvent += monitorConvertersTimeTick;
                baseConverters = this.part.FindModulesImplementing<BaseConverter>().ToArray();
                totalConverters = baseConverters.Length;
            }

            //Initial setup: currentQuality won't be set up when the part is first created
            //so we need to do that.
            //Update the display
            setInitialQuality();
            UpdateQualityDisplay(BARISScenario.GetConditionSummary(currentMTBF, MaxMTBF, currentQuality, MaxQuality));

            //Setup the gui
            SetupGUI(guiVisible);

            //If the part is broken, then highlight it.
            if (currentQuality <= 0)
            {
                SetupBrokenHighlighting();
            }

            //Grab the list of part modules that can break, and set them up
            breakableParts = this.part.FindModulesImplementing<ICanBreak>();
            List<string> requiredSkills = new List<string>();
            string checkSkill;
            foreach (ICanBreak breakablePart in breakableParts)
            {
                breakablePart.SubscribeToEvents(this);
                checkSkill = breakablePart.GetCheckSkill();
                if (requiredSkills.Contains(checkSkill) == false)
                {
                    requiredSkills.Add(checkSkill);
                }
            }
            if (breakableModuleCount == -1)
                breakableModuleCount = breakableParts.Count;

            //Now go through and build the skill list
            StringBuilder sb = new StringBuilder();
            foreach (string skill in requiredSkills)
            {
                sb.Append(skill);
                sb.Append(";");
            }
            qualityCheckSkill = sb.ToString();
            if (!string.IsNullOrEmpty(qualityCheckSkill))
            {
                qualityCheckSkill = qualityCheckSkill.Substring(0, qualityCheckSkill.Length - 1);
                debugLog("qualityCheckSkill: " + qualityCheckSkill);
            }

            //Events
            GameEvents.OnGameSettingsApplied.Add(UpdateSettings);
            UpdateSettings();
            UpdateActivationState();

            //Declare broken if needed
            if (isBrokenOnStart)
            {
                isBrokenOnStart = false;
                DeclarePartBroken();
            }

            //Declare part fixed on start if needed
            if (isFixedOnStart)
            {
                isFixedOnStart = false;
                DeclarePartFixedOnStart();
            }
        }

        public virtual void Destroy()
        {
            GameEvents.OnGameSettingsApplied.Remove(UpdateSettings);

            if (monitorConverters)
                BARISScenario.Instance.onTimeTickEvent -= monitorConvertersTimeTick;
        }

        public virtual void UpdateSettings()
        {
            Fields["qualityDisplay"].guiActive = BARISSettings.PartsCanBreak;

            //If the part is broken and parts can no longer break,
            //then declare the part fixed.
            if (IsBroken && !BARISSettings.PartsCanBreak)
                DeclarePartFixed();

            //Setup EVA-only repairs.
            Events["RepairPart"].guiActive = !BARISSettings.RepairsRequireEVA;
            Events["PerformMaintenance"].guiActive = !BARISSettings.RepairsRequireEVA;

            //Debug buttons
            Events["DeclarePartBroken"].guiActive = BARISScenario.showDebug;
            Events["DeclarePartFixed"].guiActive = BARISScenario.showDebug;

            //Give other modules a chance
            FireOnUpdateSettings();

            //Dirty the GUI
            MonoUtilities.RefreshContextWindows(this.part);
        }
        #endregion

        #region Helpers
        protected void setInitialQuality()
        {
            bool vesselIsPreLaunch = false;

            if (this.part.vessel != null)
            {
                if (this.part.vessel.situation == Vessel.Situations.PRELAUNCH)
                    vesselIsPreLaunch = true;
            }

            if (currentQuality == -1 || (BARISScenario.isKCTInstalled && vesselIsPreLaunch))
            {
                //If this vessel was created in flight, or KCT is installed, or we don't do vehicle integration, then we just max out the integration bonus.
                if (BARISScenario.isKCTInstalled || !BARISSettingsLaunch.VesselsNeedIntegration || HighLogic.LoadedSceneIsFlight)
                {
                    //If we're in flight, use the best of VAB or SPH bonus.
                    if (HighLogic.LoadedSceneIsFlight)
                    {
                        int vabBonus = BARISScenario.Instance.GetIntegrationCap(true);
                        int sphBonus = BARISScenario.Instance.GetIntegrationCap(false);

                        if (vabBonus > sphBonus)
                            integrationBonus = vabBonus;
                        else
                            integrationBonus = sphBonus;
                    }

                    //If we're in the editor, then use the appropriate bonus.
                    else if (HighLogic.LoadedSceneIsEditor)
                    {
                        bool isVAB = false;
                        if (EditorLogic.fetch.ship.shipFacility == EditorFacility.VAB)
                            isVAB = true;

                        integrationBonus = BARISScenario.Instance.GetIntegrationCap(isVAB);
                    }
                }

                //Flight experience bonus applies knowledge gained from parts that flew before.
                flightExperienceBonus = BARISScenario.Instance.GetFlightBonus(this.part);

                //If we have no flight experience, and we're in flight, then set a default.
                if (flightExperienceBonus == 0 && HighLogic.LoadedSceneIsFlight)
                {
                    BARISScenario.Instance.RecordFlightExperience(this.part, BARISScenario.DefaultFlightBonus * BARISSettingsLaunch.FlightsPerQualityBonus);
                    flightExperienceBonus = BARISScenario.DefaultFlightBonus;
                }

                //MTBF bonus
                mtbfBonus = BARISScenario.Instance.GetMTBFBonus(this.part, mtbf);

                currentQuality = MaxQuality;
                currentMTBF = MaxMTBF;
            }

            //KLUDGE! Part upgrades increase MTBF but don't get registered in the editor.
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (this.part.vessel.situation == Vessel.Situations.PRELAUNCH && currentMTBF != mtbf)
                {
                    currentQuality = MaxQuality;
                    currentMTBF = MaxMTBF;
                }
            }
        }

        protected void monitorConvertersTimeTick()
        {
            bool convertersAreActive = false;

            //Go through all the converters. Make sure the StartResourceConverter button
            //is set up properly based upon whether or not the part is broken.
            //Also note whether or not at least one converter is activated.
            for (int index = 0; index < totalConverters; index++)
            {
                if (baseConverters[index].IsActivated)
                {
                    convertersAreActive = true;

                    //If the part is broken, then stop the converter and hide the StartResourceConverter button.
                    if (currentQuality <= 0)
                    {
                        baseConverters[index].StopResourceConverter();
                        baseConverters[index].Events["StartResourceConverter"].active = false;
                    }
                }

                //Make sure the start resource converter button is enabled if the part isn't broken.
                else if (currentQuality > 0 && !baseConverters[index].Events["StartResourceConverter"].active)
                {
                    baseConverters[index].Events["StartResourceConverter"].active = true;
                }
            }

            //If the part is broken, then we're done.
            if (currentQuality <= 0)
                return;

            //If a converter is active and we had no active converters before,
            //then perform a quality check.
            if (convertersAreActive != isActivated && convertersAreActive)
                isActivated = convertersAreActive;
        }

        public void SetupBrokenHighlighting()
        {
            //Record original highlight color
            originalHighlightColor = this.part.highlightColor;

            //Set broken highlight color
            this.part.highlightColor = Color.red;
            this.part.Highlight(true);

            //Subscribe to the time tick event.
            BARISScenario.Instance.onTimeTickEvent += onTimeTickEvent;
            highlightStartTime = Planetarium.GetUniversalTime();
        }

        protected void resetHighlightColor()
        {
            //Resture the color and turn off highlight.
            this.part.highlightColor = originalHighlightColor;
            this.part.Highlight(false);
        }

        protected void onTimeTickEvent()
        {
            //Once we exceed the time interval to highlight the part, turn off highlighting.
            //At that point, only a mouseover will highlight the part.
            double elapsedTime = Planetarium.GetUniversalTime() - highlightStartTime;
            if (elapsedTime >= BARISScenario.highlightTimeInterval)
            {
                //Unsubscribe from the time tick evnt.
                BARISScenario.Instance.onTimeTickEvent -= onTimeTickEvent;

                //Turn off highlighting. A mouse-over on the part will highlight it red instead of green
                //until the part is fixed.
                this.part.Highlight(false);
            }
        }

        protected bool hasSufficientSkill()
        {
            ProtoCrewMember astronaut;
            int highestSkill = 0;

            if (FlightGlobals.ActiveVessel.FindPartModuleImplementing<ModuleRobonaut>())
                return true;
            else if (FlightGlobals.ActiveVessel.isEVA)
                highestSkill = BARISScenario.Instance.GetHighestRank(FlightGlobals.ActiveVessel, maintenanceSkill, out astronaut);
            else
                highestSkill = BARISScenario.Instance.GetHighestRank(this.part.vessel, maintenanceSkill, out astronaut);

            debugLog("highestSkill: " + highestSkill);
            if (highestSkill >= minimumSkillLevel)
            {
                debugLog("There iss sufficient skill to effect repairs.");
                return true;
            }
            else
            {
                debugLog("Insufficient skill to effect repairs.");
                return false;
            }
        }

        protected string getAllowedTraits()
        {
            StringBuilder traitsBuilder = new StringBuilder();

            string[] experienceTraits = getTraitsWithEffect(maintenanceSkill);

            if (experienceTraits.Length > 1)
            {
                for (int index = 0; index < experienceTraits.Length - 1; index++)
                    traitsBuilder.Append(experienceTraits[index] + ", ");
                traitsBuilder.Append(Localizer.Format(BARISScenario.SkillListOr) + experienceTraits[experienceTraits.Length - 1]);
            }
            else
            {
                traitsBuilder.Append(experienceTraits[0]);
            }

            return traitsBuilder.ToString();
        }

        protected static string[] getTraitsWithEffect(string effectName)
        {
            List<string> traits;
            Experience.ExperienceSystemConfig config = new Experience.ExperienceSystemConfig();
            config.LoadTraitConfigs();
            traits = config.GetTraitsWithEffect(effectName);

            if (traits == null)
            {
                traits = new List<string>();
            }

            return traits.ToArray();
        }

        protected void debugLog(string message)
        {
            if (BARISScenario.showDebug == true)
                Debug.Log("[" + this.ClassName + "] - " + message);
        }

        protected bool paidRepairCost(double repairUnits)
        {
            double amountObtained = 0f;
            double maxAmount = 0;
            double totalPaid = 0;
            PartResourceDefinition resourceDef = null;
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;

            //Get resource definition
            if (definitions.Contains(maintenanceResource))
            {
                resourceDef = definitions[maintenanceResource];
            }
            else
            {
                debugLog("Can't find resource definition for " + maintenanceResource);
                return false;
            }

            //Does the kerbal have sufficient repair units?
            if (FlightGlobals.ActiveVessel.isEVA)
            {
                FlightGlobals.ActiveVessel.rootPart.GetConnectedResourceTotals(resourceDef.id, out amountObtained, out maxAmount, true);
                if (amountObtained >= repairUnits)
                {
                    //We have enough.
                    FlightGlobals.ActiveVessel.rootPart.RequestResource(resourceDef.id, repairUnits, ResourceFlowMode.ALL_VESSEL);
                    debugLog("Kerbal on EVA had sufficient parts to effect repairs.");
                    return true;
                }

                //We aren't paid in full, but can we do partial payments?
                else if (amountObtained < repairUnits && amountObtained > 0)
                {
                    amountObtained += totalPaid;
                    debugLog("Kerbal on EVA had some of the required resource. totalPaid to date: " + totalPaid);
                }
            }

            //See if the part's vessel has sufficient resources
            this.part.GetConnectedResourceTotals(resourceDef.id, out amountObtained, out maxAmount, true);
            if (amountObtained > 0f)
            {
                totalPaid += this.part.RequestResource(resourceDef.id, repairUnits - totalPaid, ResourceFlowMode.ALL_VESSEL);
                if (totalPaid >= repairUnits)
                {
                    //We have enough
                    debugLog("Part had sufficient resources to effect repairs.");
                    return true;
                }
            }

            //Neither the kerbal on EVA nor the part's vessel has sufficient resources to repair the part.
            //One last thing we can do: make a distributed resource request.
            if (this.onRepairResourceRequest != null)
            {
                amountObtained += onRepairResourceRequest(this, repairUnits);
                if (amountObtained / repairUnits < 0.99999f)
                {
                    debugLog("Insufficient resources to repair the " + this.part.partInfo.title);
                    return false;
                }
            }

            debugLog("Sufficient resources found to effect repairs.");
            return true;
        }

        protected double getRepairUnits(double massFraction)
        {
            PartResourceDefinition resourceDef = null;
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;

            if (definitions.Contains(maintenanceResource))
            {
                resourceDef = definitions[maintenanceResource];
            }
            else
            {
                debugLog("Can't find resource definition for " + maintenanceResource);
                return 0;
            }

            double repairUnits = ((this.part.mass) * (massFraction / 100)) / resourceDef.density;
            debugLog("Part Mass: " + this.part.mass);
            debugLog("repairUnits required: " + repairUnits);

            return repairUnits;
        }
        #endregion
    }
}
