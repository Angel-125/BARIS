using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
#if !KSP122
using KSP.Localization;
#endif
/*
Source code copyright 2017, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class BARISEventResult
    {
        public BARISEventTypes eventType;
        public BARISEventCurrencyTypes currencyType;
        public BARISEventModifierTypes modifierType;
        public BARISWorkerTypes workerType;
        public double value = 0;
        public int rank = 0;
        public bool isVAB = true;
        public int minDays = 0;
        public int maxDays = 0;
        public int qualityCheckModifier = 0;
        public string facilitiesDestroyed = string.Empty;
        public BARISStatusTypes statusType = BARISStatusTypes.dead;
        public bool performImmediately = false;

        protected void debugLog(string message)
        {
            if (BARISScenario.showDebug == true)
                Debug.Log("[BARISEventResult] - " + message);
        }

        public string ApplyResult()
        {
            string message = string.Empty;

            if (!IsValid())
                return message;

            switch (eventType)
            {
                case BARISEventTypes.astronautStatusChange:
                    message = applyStatusChange();
                    break;

                case BARISEventTypes.astronautRecruited:
                    message = recruitAstronaut();
                    break;

                case BARISEventTypes.currencyChange:
                    message = updateCurrency();
                    break;

                /* Can't figure out how to destroy a space center building (well, get ahold of SpaceCenterBuilding) so this is disabled for now.
                case BARISEventTypes.facilityDestroyed:
                    destroyFacility();
                    break;
                 */

                case BARISEventTypes.qualityCheck:
                    if (qualityCheckModifier > 0)
                        message = "+" + qualityCheckModifier + Localizer.Format(BARISScenario.QualityCheckModifier);

                    else if (qualityCheckModifier < 0)
                        message = qualityCheckModifier + Localizer.Format(BARISScenario.QualityCheckModifier);

                    else
                    {
                        switch (modifierType)
                        {
                            case BARISEventModifierTypes.fail:
                            case BARISEventModifierTypes.criticalFail:
                                if (!performImmediately)
                                    message = Localizer.Format(BARISScenario.QualityCheckResultFail);
                                else
                                    message = Localizer.Format(BARISScenario.VesselLabel) + Localizer.Format(BARISScenario.VesselHasProblem);
                                break;

                            case BARISEventModifierTypes.criticalSuccess:
                            case BARISEventModifierTypes.success:
                                message = Localizer.Format(BARISScenario.QualityCheckResultSuccess);
                                break;
                        }
                    }

                    //Add the quality check event result to the cache so that it will be processed during the next quality check.
                    BARISScenario.Instance.cachedQualityModifiers.Add(this);

                    //Perform the reliability check immediately if needed
                    if (performImmediately)
                    {
                        BARISScenario.Instance.PerformReliabilityChecks();
                    }

                    break;

                case BARISEventTypes.vehicleIntegrationCompleted:
                    message = completeIntegration();
                    break;

                case BARISEventTypes.vehicleIntegrationLost:
                    applyIntegrationLost();
                    break;

                case BARISEventTypes.vehicleIntegrationPaused:
                    int daysPaused = UnityEngine.Random.Range(minDays, maxDays);
                    BARISScenario.Instance.workPausedDays += daysPaused;
                    message = Localizer.Format(BARISScenario.WorkPausedMsg1) + BARISScenario.Instance.workPausedDays + Localizer.Format(BARISScenario.WorkPausedMsg2);
                    break;

                case BARISEventTypes.workerPayIncrease:
                    message = payWorkersMore();
                    break;
            }
            return message;
        }

        /// <summary>
        /// Determines if the event result is valid for the current state of the game.
        /// </summary>
        /// <returns>True if the event result can be applied given the current state of the game, false if not.</returns>
        public bool IsValid()
        {
            bool isValid = false;
            KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;

            switch (eventType)
            {
                //Only applies if astronauts can be killed, and we have at least one non-vet astronaut.
                case BARISEventTypes.astronautStatusChange:
                    isValid = BARISSettingsLaunch.AstronautsCanBeKilled;
                    if (isValid)
                    {
                        foreach (ProtoCrewMember astronaut in roster.Crew)
                        {
                            if (!astronaut.veteran && isValid)
                                return true;
                        }
                    }
                    break;

                //Only applies if facilities can be destroyed.
                case BARISEventTypes.facilityDestroyed:
                    isValid = BARISSettingsLaunch.FacilitiesCanBeDestroyed;
                    break;

                case BARISEventTypes.vehicleIntegrationCompleted:
                    isValid = !BARISScenario.isKCTInstalled;
                    break;

                //Only applies if vehicle integrations can be lost.
                case BARISEventTypes.vehicleIntegrationLost:
                    isValid = BARISSettingsLaunch.VehicleBuildsCanBeLost;
                    break;

                //Only applies in career mode.
                case BARISEventTypes.workerPayIncrease:
                    if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                        isValid = true;
                    else
                        isValid = false;
                    break;

                //Rep & Funds only apply in Career, Science only applies in carrier/science sandbox
                case BARISEventTypes.currencyChange:
                    switch (currencyType)
                    {
                        case BARISEventCurrencyTypes.funds:
                        case BARISEventCurrencyTypes.reputation:
                            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                                isValid = true;
                            else
                                isValid = false;
                            break;

                        case BARISEventCurrencyTypes.science:
                            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
                                isValid = true;
                            else
                                isValid = false;
                            break;

                        default:
                            break;
                    }
                    break;

                default:
                    isValid = true;
                    break;
            }

            return isValid;
        }

        /// <summary>
        /// Serializes the event result to a ConfigNode
        /// </summary>
        /// <returns>A ConfigNode containing the serialized fields of the event result.</returns>
        public ConfigNode Save()
        {
            ConfigNode node = new ConfigNode("EVENTRESULT");

            node.AddValue("eventType", eventType.ToString());
            node.AddValue("currencyType", currencyType.ToString());
            node.AddValue("modifierType", modifierType.ToString());
            node.AddValue("value", value);
            node.AddValue("rank", rank);
            node.AddValue("isVAB", isVAB);
            node.AddValue("minDays", minDays);
            node.AddValue("maxDays", maxDays);
            node.AddValue("qualityCheckModifier", qualityCheckModifier);
            node.AddValue("statusType", statusType);
            node.AddValue("workerType", workerType);
            if (!string.IsNullOrEmpty(facilitiesDestroyed))
                node.AddValue("facilitiesDestroyed", facilitiesDestroyed);
            node.AddValue("performImmediately", performImmediately);
            return node;
        }

        /// <summary>
        /// De-serializes the ConfigNode into the event result fields.
        /// </summary>
        /// <param name="resultNode">A ConfigNode containing fields to load.</param>
        /// <returns>True if it can load the ConfigNode, false if not.</returns>
        public bool Load(ConfigNode resultNode)
        {
            bool validNode = false;
            string type = resultNode.GetValue("type");

            debugLog("Type: " + type);
            switch (type)
            {
                case "currencyChange":
                    validNode = loadCurrencyResult(resultNode);
                    break;

                case "astronautStatusChange":
                    validNode = astronautStatusChange(resultNode);
                    break;

                case "astronautRecruited":
                    validNode = loadAstronautRecruitedResult(resultNode);
                    break;

                case "vehicleIntegrationLost":
                    validNode = loadIntegrationLostResult(resultNode);
                    break;

                case "vehicleIntegrationPaused":
                    validNode = loadIntegrationPausedResult(resultNode);
                    break;

                case "vehicleIntegrationCompleted":
                    validNode = loadIntegrationCompletedResult(resultNode);
                    break;

                case "workerPayIncrease":
                    validNode = loadPayIncreaseResult(resultNode);
                    break;

                case "qualityCheck":
                    validNode = loadQualityCheckResult(resultNode);
                    break;

                case "facilityDestroyed":
                    validNode = loadFacilityDestroyedResult(resultNode);
                    break;

                case "custom":
                    break;

                default:
                    break;
            }

            debugLog("Loading successful: " + validNode);
            return validNode;
        }

        protected virtual bool loadFacilityDestroyedResult(ConfigNode node)
        {
            eventType = BARISEventTypes.facilityDestroyed;
            return true;
        }

        protected virtual bool loadQualityCheckResult(ConfigNode node)
        {
            eventType = BARISEventTypes.qualityCheck;
            bool boolValue = false;
            bool isValid = false;

            //Modifier is optional
            if (node.HasValue("modifier"))
            {
                if (!int.TryParse(node.GetValue("modifier"), out qualityCheckModifier))
                    return false;
                else
                    return true;
            }

            //isSuccess is optional
            else if (node.HasValue("isSuccess"))
            {
                if (!bool.TryParse(node.GetValue("isSuccess"), out boolValue))
                    return false;
                else if (boolValue)
                    modifierType = BARISEventModifierTypes.success;
                else
                    modifierType = BARISEventModifierTypes.fail;

                isValid = true;
            }

            //isCriticalFail is optional
            else if (node.HasValue("isCriticalFail"))
            {
                if (!bool.TryParse(node.GetValue("isCriticalFail"), out boolValue))
                    return false;
                else if (boolValue)
                    modifierType = BARISEventModifierTypes.criticalFail;
                else
                    modifierType = BARISEventModifierTypes.criticalSuccess;

                isValid = true;
            }

            //performImmediately is optional
            if (node.HasValue("performImmediately"))
            {
                if (!bool.TryParse(node.GetValue("performImmediately"), out boolValue))
                    return false;
                performImmediately = boolValue;
                isValid = true;
            }

            return isValid;
        }

        protected virtual bool loadPayIncreaseResult(ConfigNode node)
        {
            eventType = BARISEventTypes.workerPayIncrease;

            //worker type is optional
            if (node.HasValue("workerType"))
            {
                switch (node.GetValue("workerType"))
                {
                    case "astronaut":
                        workerType = BARISWorkerTypes.astronaut;
                        break;

                    case "construction":
                        workerType = BARISWorkerTypes.construction;
                        break;

                    default:
                        workerType = BARISWorkerTypes.both;
                        break;
                }
            }

            else
            {
                workerType = BARISWorkerTypes.both;
            }

            //Multiplier takes precedence over modifier
            if (node.HasValue("multiplier"))
            {
                modifierType = BARISEventModifierTypes.multiplier;
                if (double.TryParse(node.GetValue("multiplier"), out value) == false)
                    return false;

                value = 1.0f + (value / 100.0f);
            }
            else if (node.HasValue("modifier"))
            {
                modifierType = BARISEventModifierTypes.modifier;
                if (double.TryParse(node.GetValue("modifier"), out value) == false)
                    return false;
            }
            else
                return false;

            return true;
        }

        protected virtual bool loadIntegrationCompletedResult(ConfigNode node)
        {
            eventType = BARISEventTypes.vehicleIntegrationCompleted;
            return true;
        }

        protected virtual bool loadIntegrationPausedResult(ConfigNode node)
        {
            eventType = BARISEventTypes.vehicleIntegrationPaused;

            //Minimum days is required
            if (node.HasValue("minDays"))
            {
                if (!int.TryParse(node.GetValue("minDays"), out minDays))
                    return false;
            }

            else
            {
                return false;
            }

            //Max days is optional
            if (node.HasValue("maxDays"))
            {
                if (!int.TryParse(node.GetValue("maxDays"), out maxDays))
                    return false;
                else
                    return true;
            }

            return false;
        }

        protected virtual bool loadIntegrationLostResult(ConfigNode node)
        {
            eventType = BARISEventTypes.vehicleIntegrationLost;

            //Should have an isVAB parameter
            if (node.HasValue("isVAB"))
            {
                if (!bool.TryParse(node.GetValue("isVAB"), out isVAB))
                    return false;
                else
                    return true;
            }

            return false;
        }

        protected virtual bool loadAstronautRecruitedResult(ConfigNode node)
        {
            eventType = BARISEventTypes.astronautRecruited;

            //Rank is optional
            if (node.HasValue("rank"))
            {
                if (!int.TryParse(node.GetValue("rank"), out rank))
                    return false;
                else
                    return true;
            }

            //Is badass?
            if (node.HasValue("isBadass"))
            {
                bool isBadass = bool.Parse(node.GetValue("isBadass"));
                if (isBadass)
                    statusType = BARISStatusTypes.badS;
            }

            return true;
        }

        protected virtual bool astronautStatusChange(ConfigNode node)
        {
            eventType = BARISEventTypes.astronautStatusChange;

            //Optional: status
            if (node.HasValue("status"))
            {
                switch (node.GetValue("status"))
                {
                    case "dead":
                        statusType = BARISStatusTypes.dead;
                        break;

                    case "missing":
                        statusType = BARISStatusTypes.missing;
                        break;

                    default:
                    case "badS":
                        statusType = BARISStatusTypes.badS;
                        break;
                }
            }

            return true;
        }

        protected virtual bool loadCurrencyResult(ConfigNode node)
        {
            eventType = BARISEventTypes.currencyChange;

            //Type of currency change
            if (node.HasValue("currency"))
            {
                switch (node.GetValue("currency"))
                {
                    case "funds":
                        currencyType = BARISEventCurrencyTypes.funds;
                        break;

                    case "reputation":
                        currencyType = BARISEventCurrencyTypes.reputation;
                        break;

                    case "science":
                        currencyType = BARISEventCurrencyTypes.science;
                        break;

                    default:
                        return false;
                }
            }
            else
            {
                return false;
            }

            //Multiplier takes precedence over modifier
            if (node.HasValue("multiplier"))
            {
                modifierType = BARISEventModifierTypes.multiplier;
                if (double.TryParse(node.GetValue("multiplier"), out value) == false)
                    return false;

                value = 1.0f + (value / 100.0f);
            }
            else if (node.HasValue("modifier"))
            {
                modifierType = BARISEventModifierTypes.modifier;
                if (double.TryParse(node.GetValue("modifier"), out value) == false)
                    return false;
            }
            else
                return false;

            return true;
        }

        protected string applyStatusChange()
        {
            KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;
            string message = string.Empty;

            ProtoCrewMember[] crewRoster = roster.Crew.ToArray();
            ProtoCrewMember astronaut = null;
            int randomIndex = 0;

            //Keep looking until we find an available astronaut that isn't a vet.
            for (int index = 0; index < crewRoster.Length; index++)
            {
                randomIndex = UnityEngine.Random.Range(0, crewRoster.Length - 1);
                astronaut = crewRoster[randomIndex];

                //Set badass
                if (statusType == BARISStatusTypes.badS && !astronaut.isBadass)
                {
                    astronaut.isBadass = true;

                    //Inform player
                    message = astronaut.name + Localizer.Format(BARISScenario.StatusBadassMsg);
                    break;
                }

                else if (astronaut.rosterStatus == ProtoCrewMember.RosterStatus.Available && !astronaut.veteran)
                {
                    if (statusType == BARISStatusTypes.missing)
                    {
                        astronaut.rosterStatus = ProtoCrewMember.RosterStatus.Missing;
                        astronaut.StartRespawnPeriod();

                        //Inform player
                        message = astronaut.name + Localizer.Format(BARISScenario.StatusMissingMsg);
                        break;
                    }
                    else
                    {
                        astronaut.rosterStatus = ProtoCrewMember.RosterStatus.Dead;

                        //Inform player
                        message = astronaut.name + Localizer.Format(BARISScenario.StatusDeadMsg);
                        break;
                    }
                }
            }

            return message;
        }

        protected string recruitAstronaut()
        {
            KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;
            string message = string.Empty;
            ProtoCrewMember newRecruit = roster.GetNewKerbal();
            debugLog("New kerbal: " + newRecruit.name);

            newRecruit.type = ProtoCrewMember.KerbalType.Crew;
            newRecruit.rosterStatus = ProtoCrewMember.RosterStatus.Available;

            if (statusType == BARISStatusTypes.badS)
                newRecruit.isBadass = true;

            //Adjust rank
            debugLog("New recruit rank: " + rank);
            if (rank >= 1)
            {
                newRecruit.experience = 2;
                newRecruit.experienceLevel = 1;
                newRecruit.flightLog.AddEntry("Flight,Kerbin");
                newRecruit.flightLog.AddEntry("Suborbit,Kerbin");
                newRecruit.flightLog.AddEntry("Orbit,Kerbin");
                newRecruit.flightLog.AddEntry("Land,Kerbin");
                newRecruit.flightLog.AddEntry("Recover");
                newRecruit.ArchiveFlightLog();
            }
            if (rank >= 2)
            {
                newRecruit.experience = 8;
                newRecruit.experienceLevel = 2;
                newRecruit.flightLog.AddEntry("Flight,Kerbin");
                newRecruit.flightLog.AddEntry("Suborbit,Kerbin");
                newRecruit.flightLog.AddEntry("Orbit,Kerbin");
                newRecruit.flightLog.AddEntry("Flyby,Mun");
                newRecruit.flightLog.AddEntry("Orbit,Mun");
                newRecruit.flightLog.AddEntry("Land,Mun");
                newRecruit.flightLog.AddEntry("Flyby,Minmus");
                newRecruit.flightLog.AddEntry("Orbit,Minmus");
                newRecruit.flightLog.AddEntry("Land,Minmus");
                newRecruit.flightLog.AddEntry("Land,Kerbin");
                newRecruit.flightLog.AddEntry("Recover");
                newRecruit.ArchiveFlightLog();
            }
            if (rank >= 3)
            {
                newRecruit.experience = 16;
                newRecruit.experienceLevel = 3;
                newRecruit.flightLog.AddEntry("Flight,Kerbin");
                newRecruit.flightLog.AddEntry("Suborbit,Kerbin");
                newRecruit.flightLog.AddEntry("Orbit,Kerbin");
                newRecruit.flightLog.AddEntry("Flyby,Mun");
                newRecruit.flightLog.AddEntry("Orbit,Mun");
                newRecruit.flightLog.AddEntry("Land,Mun");
                newRecruit.flightLog.AddEntry("PlantFlag,Mun");
                newRecruit.flightLog.AddEntry("Flyby,Minmus");
                newRecruit.flightLog.AddEntry("Orbit,Minmus");
                newRecruit.flightLog.AddEntry("Land,Minmus");
                newRecruit.flightLog.AddEntry("PlantFlag,Minmus");
                newRecruit.flightLog.AddEntry("Orbit,Minmus");
                newRecruit.flightLog.AddEntry("Orbit,Sun");
                newRecruit.flightLog.AddEntry("Land,Kerbin");
                newRecruit.flightLog.AddEntry("Recover");
                newRecruit.ArchiveFlightLog();
            }

            //Game events
            newRecruit.UpdateExperience();
            roster.Update(Planetarium.GetUniversalTime());
            GameEvents.onKerbalAdded.Fire(newRecruit);
            GameEvents.onKerbalLevelUp.Fire(newRecruit);
            GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.APPEND);

            //Inform player
            message = newRecruit.name + Localizer.Format(BARISScenario.StatusRecruitedMsg);
            return message;
        }

        protected string updateCurrency()
        {
            double amount = 0;
            string message = string.Empty;
            string valueDisplay = string.Empty;
            double adjustedValue = 0;

            //Format the value
            if (modifierType == BARISEventModifierTypes.multiplier)
            {
                adjustedValue = 100 * (value - 1);
                valueDisplay = string.Format("{0:f0}% ", adjustedValue);
            }
            else
            {
                adjustedValue = value;
                valueDisplay = string.Format("{0:f0} ", value);
            }
            if (adjustedValue >= 0)
                valueDisplay = "+" + valueDisplay;

            //Multiplier takes precedence over modifier.
            debugLog("value: " + value);
            switch (currencyType)
            {
                case BARISEventCurrencyTypes.funds:
                    amount = Funding.Instance.Funds;
                    if (modifierType == BARISEventModifierTypes.multiplier)
                        amount *= value;
                    else
                        amount += value;
                    Funding.Instance.SetFunds(amount, TransactionReasons.Any);
                    message = valueDisplay + Localizer.Format(BARISScenario.FundsLabel);
                    break;

                case BARISEventCurrencyTypes.reputation:
                    amount = Reputation.Instance.reputation;
                    if (modifierType == BARISEventModifierTypes.multiplier)
                        amount *= value;
                    else
                        amount += value;
                    Reputation.Instance.SetReputation((float)amount, TransactionReasons.Any);
                    message = "<color=yellow>" + valueDisplay + Localizer.Format(BARISScenario.RepLabel) + "</color>";
                    break;

                case BARISEventCurrencyTypes.science:
                    amount = ResearchAndDevelopment.Instance.Science;
                    if (modifierType == BARISEventModifierTypes.multiplier)
                        amount *= value;
                    else
                        amount += value;
                    ResearchAndDevelopment.Instance.SetScience((float)amount, TransactionReasons.Any);
                    message = "<color=lightBlue>" + valueDisplay + Localizer.Format(BARISScenario.ScienceLabel) + "</color>";
                    break;
            }
            return message;
        }

        protected string completeIntegration()
        {
            List<EditorBayItem> bayItems = null;
            EditorBayItem bayItem = null;

            bayItems = BARISScenario.Instance.GetBuildsInProgress(isVAB);
            if (bayItems.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, bayItems.Count - 1);
                bayItem = bayItems[index];

                bayItem.totalQuality += bayItem.totalIntegrationToAdd;
                bayItem.totalIntegrationAdded += bayItem.totalIntegrationToAdd;
                bayItem.totalIntegrationToAdd = 0;

                //With KAC installed, clear the alarm.
                if (KACWrapper.AssemblyExists && KACWrapper.APIReady)
                    KACWrapper.KAC.DeleteAlarm(bayItem.KACAlarmID);

                //Inform player
                string message = bayItem.vesselName + Localizer.Format(BARISScenario.VesselBuildCompleteMsg);
                return message;
            }

            return string.Empty;
        }

        /*
        protected void destroyFacility()
        {
            List<EditorBayItem> bayItems = null;

            //Set the facility as destroyed.

            //If the facility is the VAB or SPH, then clear the bays as well.
            bayItems = BARISScenario.Instance.GetBuildsInProgress(isVAB);
            foreach (EditorBayItem doomed in bayItems)
                doomed.Clear();

            //Inform player
            string message = Localizer.Format(BARISScenario.BuildingDestroyedMsg);
            BARISScenario.Instance.LogPlayerMessage(message);
        }
         */

        protected void applyIntegrationLost()
        {
            List<EditorBayItem> bayItems = null;
            EditorBayItem bayItem = null;
            string message = string.Empty;

            bayItems = BARISScenario.Instance.GetBuildsInProgress(isVAB);
            if (bayItems.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, bayItems.Count - 1);
                bayItem = bayItems[index];
                message = bayItem.vesselName + Localizer.Format(BARISScenario.VesselBuildFailedMsg);
                bayItem.Clear();

                //Inform player
                BARISScenario.Instance.LogPlayerMessage(message);
            }
        }

        protected string payWorkersMore()
        {
            StringBuilder message = new StringBuilder();
            bool workerPayIncreased = false;
            bool astronautPayIncreased = false;

            switch (workerType)
            {
                case BARISWorkerTypes.construction:
                    if (modifierType == BARISEventModifierTypes.multiplier)
                        BARISScenario.PayrollPerWorker = Mathf.RoundToInt((float)BARISScenario.PayrollPerWorker * (float)value);
                    else
                        BARISScenario.PayrollPerWorker += (int)value;
                    workerPayIncreased = true;
                    break;

                case BARISWorkerTypes.astronaut:
                    if (modifierType == BARISEventModifierTypes.multiplier)
                        BARISScenario.PayrollPerAstronaut = Mathf.RoundToInt((float)BARISScenario.PayrollPerAstronaut * (float)value);
                    else
                        BARISScenario.PayrollPerAstronaut += (int)value;
                    astronautPayIncreased = true;
                    break;

                default:
                case BARISWorkerTypes.both:
                    if (modifierType == BARISEventModifierTypes.multiplier)
                    {
                        BARISScenario.PayrollPerWorker = Mathf.RoundToInt((float)BARISScenario.PayrollPerWorker * (float)value);
                        BARISScenario.PayrollPerAstronaut = Mathf.RoundToInt((float)BARISScenario.PayrollPerAstronaut * (float)value);
                    }
                    else
                    {
                        BARISScenario.PayrollPerWorker += (int)value;
                        BARISScenario.PayrollPerAstronaut += (int)value;
                    }
                    workerPayIncreased = true;
                    astronautPayIncreased = true;
                    break;
            }

            if (workerPayIncreased)
            {
                message.AppendLine("<color=white><b>" + Localizer.Format(BARISScenario.Payroll1Label) + "</b>" +
                        string.Format("{0:n0}", BARISScenario.PayrollPerWorker) + Localizer.Format(BARISScenario.Payroll2Label) +
                        BARISScenario.DaysPerPayroll + Localizer.Format(BARISScenario.Payroll3Label) + "</color>");
            }

            if (astronautPayIncreased)
            {
                message.AppendLine("<color=white><b>" + Localizer.Format(BARISScenario.PayrollAstronautsLabel) + "</b>" +
                        string.Format("{0:n0}", BARISScenario.PayrollPerAstronaut) + Localizer.Format(BARISScenario.Payroll2Label) +
                        BARISScenario.DaysPerPayroll + Localizer.Format(BARISScenario.Payroll3Label) + "</color>");
            }
            return message.ToString();
        }
    }
}
