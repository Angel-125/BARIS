using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
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
    public class UnloadedQualitySummary
    {
        public Vessel vessel;
        public ProtoPartModuleSnapshot[] qualityModuleSnapshots;
        public ProtoCrewMember rankingAstronaut;
        public int highestRank;
        public int reliability;
        public int repairCostTime = -1;
        public float repairCostFunds = -1.0f;
        public float repairCostScience = -1.0f;

        protected void debugLog(string message)
        {
            if (BARISScenario.showDebug == true)
                Debug.Log("[UnloadedQualitySummary] - " + message);
        }

        public ProtoPartModuleSnapshot[] UpdateAndGetFailureCandidates(float mtbfDecrement)
        {
            int currentQuality = 0;
            int totalQuality = 0;
            double currentMTBF = 0;
            bool isActivated = false;
            int breakableModuleCount = 0;
            List<ProtoPartModuleSnapshot> failureCandidates = new List<ProtoPartModuleSnapshot>();
            ProtoPartModuleSnapshot qualityModule;
            float mtbfHibernationFactor;

            debugLog("UpdateAndGetFailureCandidates called for " + vessel.vesselName);
            debugLog("qualityModuleSnapshots count: " + qualityModuleSnapshots.Length);
            for (int index = 0; index < qualityModuleSnapshots.Length; index++)
            {
                qualityModule = qualityModuleSnapshots[index];

                //Get current mtbf, isActivated state, and number of part modules available to break;
                if (!qualityModule.moduleValues.HasValue("currentMTBF"))
                    continue;
                currentMTBF = double.Parse(qualityModule.moduleValues.GetValue("currentMTBF"));
                if (!qualityModule.moduleValues.HasValue("isActivated"))
                    continue;
                isActivated = bool.Parse(qualityModule.moduleValues.GetValue("isActivated"));
                if (!qualityModule.moduleValues.HasValue("mtbfHibernationFactor"))
                    continue;
                mtbfHibernationFactor = float.Parse(qualityModule.moduleValues.GetValue("mtbfHibernationFactor"));
                if (!qualityModule.moduleValues.HasValue("breakableModuleCount"))
                    mtbfHibernationFactor = 0.1f;
                breakableModuleCount = int.Parse(qualityModule.moduleValues.GetValue("breakableModuleCount"));

                if (currentMTBF > 0 && mtbfDecrement > 0)
                {
                    //Decrement the MTBF
                    if (isActivated)
                        currentMTBF -= mtbfDecrement;
                    else
                        currentMTBF -= (mtbfDecrement * mtbfHibernationFactor);
                    if (currentMTBF < 0)
                        currentMTBF = 0;

                    //Set adjusted MTBF
                    qualityModule.moduleValues.SetValue("currentMTBF", currentMTBF);
                }

                //Add to the list of failure candidates
                //It has to be activated, its breakable modules > 0, and it is out of MTBF.
                if (isActivated && breakableModuleCount > 0 && currentMTBF <= 0)
                    failureCandidates.Add(qualityModule);

                //Now get the quality
                currentQuality = int.Parse(qualityModule.moduleValues.GetValue("currentQuality"));
                totalQuality += currentQuality;
            }

            //Update reliability
            int maxQuality = 100 * qualityModuleSnapshots.Length;
            reliability = Mathf.RoundToInt(((float)totalQuality / (float)maxQuality) * 100.0f);
            debugLog("reliability: " + reliability);

            //Return the failure candidates
            if (failureCandidates.Count > 0)
                return failureCandidates.ToArray();
            else
                return null;
        }

        public void DeclareVesselFixed()
        {
            ConfigNode snapshot;
            int currentQuality = 0;
            int integrationBonus = -1;
            int flightExperienceBonus = BARISScenario.DefaultFlightBonus;
            int qualityCap = -1;
            int repairCount = 0;
            int quality = 5;
            int vabBonus = BARISScenario.Instance.GetIntegrationCap(true);
            int sphBonus = BARISScenario.Instance.GetIntegrationCap(false);
            double mtbfBonus = 600.0f;
            double mtbf = 600.0f;
            double mtbfCap = BARISScenario.MTBFCap;
            float mtbfRepairMultiplier = 0.7f;
            double currentMTBF = 0;

            for (int index = 0; index < qualityModuleSnapshots.Length; index++)
            {
                //Get the snapshot
                snapshot = qualityModuleSnapshots[index].moduleValues;

                //Setup default values
                integrationBonus = -1;
                flightExperienceBonus = BARISScenario.DefaultFlightBonus;
                qualityCap = -1;
                repairCount = 0;
                quality = 5;

                //Get the stats we need
                if (snapshot.HasValue("quality"))
                    quality = int.Parse(qualityModuleSnapshots[index].moduleValues.GetValue("quality"));
                if (snapshot.HasValue("qualityCap"))
                    qualityCap = int.Parse(qualityModuleSnapshots[index].moduleValues.GetValue("qualityCap"));
                if (snapshot.HasValue("integrationBonus"))
                    integrationBonus = int.Parse(qualityModuleSnapshots[index].moduleValues.GetValue("integrationBonus"));
                if (snapshot.HasValue("flightExperienceBonus"))
                    flightExperienceBonus = int.Parse(qualityModuleSnapshots[index].moduleValues.GetValue("flightExperienceBonus"));
                if (snapshot.HasValue("repairCount"))
                    repairCount = int.Parse(qualityModuleSnapshots[index].moduleValues.GetValue("repairCount"));
                if (snapshot.HasValue("mtbfRepairMultiplier"))
                    mtbfRepairMultiplier = float.Parse(qualityModuleSnapshots[index].moduleValues.GetValue("mtbfRepairMultiplier"));
                if (snapshot.HasValue("mtbfBonus"))
                    mtbfBonus = double.Parse(qualityModuleSnapshots[index].moduleValues.GetValue("mtbfBonus"));
                if (snapshot.HasValue("mtbf"))
                    mtbf = double.Parse(qualityModuleSnapshots[index].moduleValues.GetValue("mtbf"));
                if (snapshot.HasValue("mtbfCap"))
                    mtbfCap = double.Parse(qualityModuleSnapshots[index].moduleValues.GetValue("mtbfCap"));

                //Estimate integration bonus if needed.
                if (integrationBonus == -1)
                {
                    if (vabBonus > sphBonus)
                        integrationBonus = vabBonus;
                    else
                        integrationBonus = sphBonus;
                }

                //Increment repair count
                repairCount += 1;
                qualityModuleSnapshots[index].moduleValues.SetValue("repairCount", repairCount);

                //Reset current quality
                currentQuality = BARISScenario.GetMaxQuality(quality, integrationBonus, flightExperienceBonus, qualityCap, repairCount);
                if (currentQuality > quality)
                    currentQuality = quality;
                qualityModuleSnapshots[index].moduleValues.SetValue("currentQuality", currentQuality);

                //Reset current MTBF
                currentMTBF = BARISScenario.GetMaxMTBF(mtbf, mtbfCap, repairCount, mtbfRepairMultiplier, mtbfBonus);
                if (currentMTBF > mtbf)
                    currentMTBF = mtbf;
                qualityModuleSnapshots[index].moduleValues.SetValue("currentMTBF", currentMTBF);

                //Declare the part fixed
                qualityModuleSnapshots[index].moduleValues.SetValue("isFixedOnStart", true);
                qualityModuleSnapshots[index].moduleValues.SetValue("isBrokenOnStart", false);
            }
        }

        public void AttemptRepairs()
        {
            int resultRoll = UnityEngine.Random.Range(1, 100);
            int rndFacilityBonus = (int)Math.Round(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.ResearchAndDevelopment) * BARISScenario.BaseFacilityBonus);
            int missionControlBonus = (int)Math.Round(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.MissionControl) * BARISScenario.BaseFacilityBonus);
            string message;

            //Add facility bonuses
            resultRoll += rndFacilityBonus + missionControlBonus;

            if (resultRoll >= BARISScenario.TigerTeamRepairTarget)
            {
                //Inform player of success
                if (vessel.GetCrewCount() > 0)
                    message = Localizer.Format(BARISScenario.TigerRepairSuccessMsg) + vessel.vesselName;
                else
                    message = Localizer.Format(BARISScenario.TigerRepairSuccessMsgUnmanned) + vessel.vesselName;
                BARISScenario.Instance.LogPlayerMessage(message);

                //Declare the vessel fixed
                DeclareVesselFixed();
            }

            else
            {
                //Inform player of failure
                message = Localizer.Format(BARISScenario.TigerRepairFailMsg) + vessel.vesselName + Localizer.Format(BARISScenario.TigerRepairTryAgainMsg);
                BARISScenario.Instance.LogPlayerMessage(message);
            }

            //Kill timewarp
            if (BARISScenario.KillTimewarpOnTigerTeamCompleted && TimeWarp.CurrentRateIndex >= BARISScenario.HighTimewarpIndex)
                TimeWarp.SetRate(0, true);
        }

        public void CalculateRepairCosts()
        {
            ConfigNode snapshot;
            int maxQuality = 0;
            int integrationBonus = -1;
            int flightExperienceBonus = BARISScenario.DefaultFlightBonus;
            int qualityCap = -1;
            int repairCount = 0;
            int quality = 5;
            int vabBonus = BARISScenario.Instance.GetIntegrationCap(true);
            int sphBonus = BARISScenario.Instance.GetIntegrationCap(false);

            for (int index = 0; index < qualityModuleSnapshots.Length; index++)
            {
                //Get the snapshot
                snapshot = qualityModuleSnapshots[index].moduleValues;

                //Setup default values
                integrationBonus = -1;
                flightExperienceBonus = BARISScenario.DefaultFlightBonus;
                qualityCap = -1;
                repairCount = 0;
                quality = 5;

                //Get the stats we need
                if (snapshot.HasValue("quality"))
                    quality = int.Parse(qualityModuleSnapshots[index].moduleValues.GetValue("quality"));
                if (snapshot.HasValue("qualityCap"))
                    qualityCap = int.Parse(qualityModuleSnapshots[index].moduleValues.GetValue("qualityCap"));
                if (snapshot.HasValue("integrationBonus"))
                    integrationBonus = int.Parse(qualityModuleSnapshots[index].moduleValues.GetValue("integrationBonus"));
                if (snapshot.HasValue("flightExperienceBonus"))
                    flightExperienceBonus = int.Parse(qualityModuleSnapshots[index].moduleValues.GetValue("flightExperienceBonus"));
                if (snapshot.HasValue("repairCount"))
                    repairCount = int.Parse(qualityModuleSnapshots[index].moduleValues.GetValue("repairCount"));

                //Estimate integration bonus if needed.
                if (integrationBonus == -1)
                {
                    if (vabBonus > sphBonus)
                        integrationBonus = vabBonus;
                    else
                        integrationBonus = sphBonus;
                }

                //Accumulate max quality.
                maxQuality += BARISScenario.GetMaxQuality(quality, integrationBonus, flightExperienceBonus, qualityCap, repairCount);
            }

            //Calculate the time
            repairCostTime = Mathf.RoundToInt(maxQuality / BARISScenario.MaxWorkersPerBay);
            if (repairCostTime <= BARISScenario.MinimumTigerTeamRepairDays)
                repairCostTime = BARISScenario.MinimumTigerTeamRepairDays;

            //Calculate the funding cost
            repairCostFunds = repairCostTime * BARISScenario.MaxWorkersPerBay * BARISScenario.PayrollPerWorker;

            //Calculate the science cost
            repairCostScience = (float)repairCostTime;
        }

        public bool IsBroken()
        {
            int currentQuality = 0;

            for (int index = 0; index < qualityModuleSnapshots.Length; index++)
            {
                currentQuality = int.Parse(qualityModuleSnapshots[index].moduleValues.GetValue("currentQuality"));
                if (currentQuality == 0)
                    return true;
            }

            return false;
        }

        public int UpdateSafetyRating()
        {
            int currentQuality = 0;
            int totalQuality = 0;

            for (int index = 0; index < qualityModuleSnapshots.Length; index++)
            {
                currentQuality = int.Parse(qualityModuleSnapshots[index].moduleValues.GetValue("currentQuality"));
                totalQuality += currentQuality;
            }

            reliability = (int)Mathf.Round(totalQuality / (100 * qualityModuleSnapshots.Length));
            return reliability;
        }

        public int UpdateHighestRank()
        {
            string qualityCheckSkill = string.Empty;
            int skillRank = 0;
            ProtoCrewMember astronaut = null;

            for (int index = 0; index < qualityModuleSnapshots.Length; index++)
            {
                qualityCheckSkill = qualityModuleSnapshots[index].moduleValues.GetValue("qualityCheckSkill");
                skillRank = BARISScenario.Instance.GetHighestRank(vessel, qualityCheckSkill, out astronaut);
                if (skillRank > highestRank)
                {
                    highestRank = skillRank;
                    rankingAstronaut = astronaut;
                }
            }

            return highestRank;
        }
    }
}
