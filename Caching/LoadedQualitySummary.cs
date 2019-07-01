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
    public class LoadedQualitySummary
    {
        public Vessel vessel;
        public int partCount;
        public ModuleQualityControl[] qualityModules;
        public ProtoCrewMember rankingAstronaut;
        public int highestRank;
        public int reliability;

        protected void debugLog(string message)
        {
            if (BARISScenario.showDebug == true)
                Debug.Log("[LoadedQualitySummary] - " + message);
        }

        public ModuleQualityControl[] GetStagingFailureCandidates(int stageID)
        {
            List<ModuleQualityControl> failureCandidates = new List<ModuleQualityControl>();
            ModuleQualityControl qualityControl;
            int totalQuality = 0;

            for (int index = 0; index < qualityModules.Length; index++)
            {
                qualityControl = qualityModules[index];

                //Determine which parts of the vessel are in the activated stage and the stage above it.
                //For vehicles with decouplers, the fuel tanks in the active stage are in the stage above.
                if (qualityControl.part.inverseStage == stageID || qualityControl.part.inverseStage == stageID - 1)
                {
                    //Add to the list of failure candidates
                    //It has to be activated, and its breakable modules > 0.
                    if (/*qualityControl.isActivated &&*/ qualityControl.breakableModuleCount > 0)
                    {
                        failureCandidates.Add(qualityControl);
                    }
                }

                //Now get the quality. It's computed against all the quality modules we have
                //in the vessel, not just the parts in the stage.
                totalQuality += qualityControl.currentQuality;
            }

            //Update reliability
            int maxQuality = 100 * qualityModules.Length;
            reliability = Mathf.RoundToInt(((float)totalQuality / (float)maxQuality) * 100.0f);
            debugLog("reliability: " + reliability);

            //Return the failure candidates
            if (failureCandidates.Count > 0)
                return failureCandidates.ToArray();
            else
                return null;
        }

        public ModuleQualityControl[] UpdateAndGetFailureCandidates(float mtbfDecrement)
        {
            int totalQuality = 0;
            double currentMTBF = 0;
            List<ModuleQualityControl> failureCandidates = new List<ModuleQualityControl>();
            ModuleQualityControl qualityControl;
            int skillRank = 0;
            ProtoCrewMember astronaut = null;

            debugLog("UpdateAndGetFailureCandidates called for " + vessel.vesselName);
            debugLog("qualityModuleSnapshots count: " + qualityModules.Length);
            for (int index = 0; index < qualityModules.Length; index++)
            {
                qualityControl = qualityModules[index];

                //Get current mtbf
                currentMTBF = qualityControl.currentMTBF;

                //Decrement the MTBF
                if (currentMTBF > 0 && mtbfDecrement > 0)
                {
                    if (qualityControl.isActivated)
                        currentMTBF -= mtbfDecrement;
                    else
                        currentMTBF -= (mtbfDecrement * qualityControl.mtbfHibernationFactor);
                    if (currentMTBF < 0)
                        currentMTBF = 0;

                    //Set adjusted MTBF
                    qualityControl.currentMTBF = currentMTBF;
                }

                //Add to the list of failure candidates
                //It has to be activated, its breakable modules > 0, and it is out of MTBF.
                if (qualityControl.isActivated && qualityControl.breakableModuleCount > 0 && qualityControl.currentMTBF <= 0)
                    failureCandidates.Add(qualityControl);

                //Now get the quality
                totalQuality += qualityControl.currentQuality;

                //Get highest rank
                skillRank = BARISScenario.Instance.GetHighestRank(vessel, qualityControl.qualityCheckSkill, out astronaut);
                if (skillRank > highestRank)
                {
                    highestRank = skillRank;
                    rankingAstronaut = astronaut;
                }
            }

            //Update reliability
            int maxQuality = 100 * qualityModules.Length;
            reliability = Mathf.RoundToInt(((float)totalQuality / (float)maxQuality) * 100.0f);
            debugLog("reliability: " + reliability);

            //Return the failure candidates
            if (failureCandidates.Count > 0)
                return failureCandidates.ToArray();
            else
                return null;
        }
    }
}
