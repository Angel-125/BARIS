using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;
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
    public class BARISAppTrackingView : Dialog<BARISAppTrackingView>
    {
        private static Texture wrenchIcon = null;

        const int DialogWidth = 350;
        const int DialogHeight = 500;
        const int InfoPanelHeightBroken = 120;
        const int InfoPanelHeight = 60;

        private Vector2 scrollPos;
        private GUILayoutOption[] scrollViewOptions = new GUILayoutOption[] { GUILayout.Width(DialogWidth) };
        private GUILayoutOption[] infoPanelOptions = new GUILayoutOption[] { GUILayout.Height(InfoPanelHeight) };
        private GUILayoutOption[] infoPanelOptionsBroken = new GUILayoutOption[] { GUILayout.Height(InfoPanelHeightBroken) };
        private GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.Height(24), GUILayout.Width(24) };
        private Vector2 originPoint = new Vector2(0, 0);

        public BARISAppTrackingView() :
        base("Blah", DialogWidth, DialogHeight)
        {
            WindowTitle = Localizer.Format(BARISScenario.AppFlightViewTitle);
            Resizable = false;
        }

        protected void debugLog(string message)
        {
            if (BARISScenario.showDebug == true)
                Debug.Log("[BARISAppTrackingView] - " + message);
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            if (wrenchIcon == null)
                wrenchIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/000BARIS/Icons/Wrench", false);

            if (newValue)
            {
                //Game events
                BARISScenario.Instance.onQualityCheck += onQualityCheck;
            }

            else
            {
                BARISScenario.Instance.onQualityCheck -= onQualityCheck;
            }
        }

        protected void onQualityCheck(QualityCheckResult result)
        {
        }

        protected void registerRepairProject(UnloadedQualitySummary qualitySummary)
        {
            //Show tooltip: The repair attempt might not suceed!
            if (!BARISScenario.showedRepairProjectTip && BARISScenario.partsCanBreak)
            {
                BARISScenario.showedRepairProjectTip = true;
                BARISEventCardView cardView = new BARISEventCardView();

                cardView.WindowTitle = BARISScenario.RepairProjectTitle;
                cardView.description = BARISScenario.RepairProjectMsg;
                cardView.imagePath = BARISScenario.RepairProjectImagePath;

                cardView.SetVisible(true);
                GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
            }

            //Create repair project
            BARISRepairProject repairProject = new BARISRepairProject();
            repairProject.vesselID = qualitySummary.vessel.id.ToString();
            repairProject.repairCostFunds = qualitySummary.repairCostFunds;
            repairProject.repairCostScience = qualitySummary.repairCostScience;
            repairProject.repairCostTime = qualitySummary.repairCostTime;
            repairProject.startTime = Planetarium.GetUniversalTime();

            //Register the project
            BARISScenario.Instance.RegisterRepairProject(repairProject);
        }

        protected override void DrawWindowContents(int windowId)
        {
            drawVesselSummary();
        }

        protected void drawVesselSummary()
        {
            Vessel[] unloadedVessels = FlightGlobals.VesselsUnloaded.ToArray();
            UnloadedQualitySummary qualitySummary;
            bool createdNewRecord;

            scrollPos = GUILayout.BeginScrollView(scrollPos, scrollViewOptions);

            for (int index = 0; index < unloadedVessels.Length; index++)
            {
                //Get the summary info.
                qualitySummary = BARISScenario.Instance.GetUnloadedQualitySummary(unloadedVessels[index], out createdNewRecord);
                if (qualitySummary == null)
                    continue;
                //Skip vessels marked as debris.
                if (!BARISUtils.IsFilterEnabled(qualitySummary.vessel))
                    continue;
                
                //Update the summary
                qualitySummary.UpdateAndGetFailureCandidates(0);

                if (qualitySummary.IsBroken())
                {
                    GUILayout.BeginScrollView(originPoint, infoPanelOptionsBroken);

                    //Vessel name & reliability
                    GUILayout.Label("<color=white><b>" + unloadedVessels[index].vesselName + "</b></color>");
                    GUILayout.Label("<color=white><b>" + BARISScenario.ConditionLabel + "</b> " + BARISScenario.RepairTimeBroken + "</color>");
                    drawTigerTeamRepairs(qualitySummary);

                    GUILayout.EndScrollView();
                }

                else
                {
                    GUILayout.BeginScrollView(originPoint, infoPanelOptions);

                    //Vessel name & reliability
                    GUILayout.Label("<color=white><b>" + unloadedVessels[index].vesselName + "</b></color>");
                    GUILayout.Label("<color=white><b>" + BARISScenario.ReliabilityLabel + "</b> " + qualitySummary.reliability + "%</color>");

                    GUILayout.EndScrollView();
                }
            }

            GUILayout.EndScrollView();
        }

        protected void drawTigerTeamRepairs(UnloadedQualitySummary qualitySummary)
        {
            bool canAffordScience = true;
            bool canAffordFunds = true;
            bool commNetEnabled = true;

            //Do we have a repair entry? If so, draw the progress.
            BARISRepairProject repairProject = BARISScenario.Instance.GetRepairProject(qualitySummary.vessel);
            if (repairProject != null)
            {
                GUILayout.Label("<color=white><b>" + Localizer.Format(BARISScenario.RepairTimeProgress) + "</b>" + string.Format("{0:f3}", repairProject.RepairProgress) + "%</color>");
                return;
            }

            //Calculate repair costs
            qualitySummary.CalculateRepairCosts();

            //If we don't then draw the repair costs and buttons.
            //Science cost to upgrade flight experience.
            GUILayout.BeginHorizontal();
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
            {
                canAffordScience = ResearchAndDevelopment.CanAfford(qualitySummary.repairCostScience);

                if (canAffordScience)
                    GUILayout.Label("<color=white><b>" + Localizer.Format(BARISScenario.TestBenchScienceCost) + "</b>" + string.Format("{0:n2}", qualitySummary.repairCostScience) + "</color>");
                else
                    GUILayout.Label("<color=red><b>" + Localizer.Format(BARISScenario.TestBenchScienceCost) + "</b>" + string.Format("{0:n2}", qualitySummary.repairCostScience) + "</color>");
            }

            //Funds cost to upgrade flight experience.
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                canAffordFunds = Funding.CanAfford(qualitySummary.repairCostFunds);

                if (canAffordFunds)
                    GUILayout.Label("<color=white><b>" + Localizer.Format(BARISScenario.TestBenchFundsCost) + "</b>" + string.Format("{0:n2}", qualitySummary.repairCostFunds) + "</color>");
                else
                    GUILayout.Label("<color=red><b>" + Localizer.Format(BARISScenario.TestBenchFundsCost) + "</b>" + string.Format("{0:n2}", qualitySummary.repairCostFunds) + "</color>");
            }

            //CommNet status
            if (CommNet.CommNetScenario.CommNetEnabled)
                commNetEnabled = qualitySummary.vessel.connection.IsConnectedHome;

            //Time to attempt repair
            GUILayout.Label("<color=white><b>" + Localizer.Format(BARISScenario.RepairTimeCost) + "</b>" + string.Format("{0:n2}", qualitySummary.repairCostTime) + 
                " " + Localizer.Format(BARISScenario.RepairTimeDays) + "</color>");

            //Repair button
            if (GUILayout.Button(wrenchIcon, buttonOptions))
            {
                //Can we afford the funds?
                if (!canAffordFunds)
                {
                    BARISScenario.Instance.LogPlayerMessage(Localizer.Format(BARISScenario.TigerTeamNoFunds));
                    GUILayout.EndHorizontal();
                    return;
                }

                //Can we afford the science?
                else if (!canAffordScience)
                {
                    GUILayout.EndHorizontal();
                    BARISScenario.Instance.LogPlayerMessage(Localizer.Format(BARISScenario.TigerTeamNoScience));
                    return;
                }

                //Do we have a CommNet connection?
                else if (!commNetEnabled)
                {
                    GUILayout.EndHorizontal();
                    BARISScenario.Instance.LogPlayerMessage(Localizer.Format(BARISScenario.TigerTeamNoComm));
                    return;
                }

                //Deduct the costs
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                    Funding.Instance.AddFunds(-qualitySummary.repairCostFunds, TransactionReasons.Any);
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
                    ResearchAndDevelopment.Instance.AddScience(-qualitySummary.repairCostScience, TransactionReasons.Any);

                //Register the new project
                registerRepairProject(qualitySummary);
            }

            GUILayout.EndHorizontal();

        }

    }
}
