using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;
#if !KSP122
using KSP.Localization;
#endif

/*
Source code copyright 2018, by Michael Billard (Angel-125)
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
    internal class TestBenchView: Dialog<TestBenchView>
    {
        const int DialogWidth = 300;
        const int DialogHeight = 200;
        const int InfoPanelHeight = 75;

        private static Texture fundsIcon = null;
        private static Texture scienceIcon = null;

        private string[] experienceModifiers = {"+1", "+5", "+10" };
        private int reliability, maxReliability = 0;
        int reliabilityAfter, maxReliabilityAfter;
        private int experienceModifier = 1;
        private float fundsCost = 0;
        private float scienceCost = 0;
        private bool isVAB;
        int buttonIndex = 0;
        private ModuleQualityControl[] qualityControlModules;
        private GUILayoutOption[] infoPanelOptions = new GUILayoutOption[] { GUILayout.Height(InfoPanelHeight) };
        private GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.Height(64), GUILayout.Width(64) };
        private Vector2 originPoint = new Vector2(0, 0);
        private Dictionary<string, int> flightLog = new Dictionary<string, int>();

        public TestBenchView() :
        base("Blah", DialogWidth, DialogHeight)
        {
            WindowTitle = Localizer.Format(BARISScenario.TestBenchTitle);
            Resizable = false;
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            //Are we in VAB or SPH?
            if (EditorLogic.fetch.ship.shipFacility == EditorFacility.VAB)
                isVAB = true;
            else
                isVAB = false;

            if (fundsIcon == null)
                fundsIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/000BARIS/Icons/FundsIcon", false);
            if (scienceIcon == null)
                scienceIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/000BARIS/Icons/ScienceIcon", false);

            if (newValue)
            {
                GameEvents.onEditorShipModified.Add(onEditorShipModified);
                onEditorShipModified(EditorLogic.fetch.ship);
            }

            else
            {
                GameEvents.onEditorShipModified.Remove(onEditorShipModified);
            }
        }

        public void Destroy()
        {
            GameEvents.onEditorShipModified.Remove(onEditorShipModified);
        }

        protected void onEditorShipModified(ShipConstruct ship)
        {
            //Get the breakable parts.
            qualityControlModules = null;
            List<ModuleQualityControl> modules = new List<ModuleQualityControl>();
            ModuleQualityControl qualityControl;
            foreach (Part part in ship.parts)
            {
                qualityControl = part.FindModuleImplementing<ModuleQualityControl>();
                if (qualityControl != null)
                    modules.Add(qualityControl);
            }

            if (modules.Count > 0)
            {
                qualityControlModules = modules.ToArray();
            }

            //Calculate reliability & cost
            calculateCostsAndReliability();
        }

        protected override void DrawWindowContents(int windowId)
        {
            float costScience = 0f;
            float costFunds = 0f;
            bool canAffordScience, canAffordFunds;

            if (qualityControlModules == null)
            {
                GUILayout.Label("<color=yellow><b>" + Localizer.Format(BARISScenario.TestBenchWarning) + "</b></color>");
                return;
            }

            calculateCostsAndReliability();

            //Get science cost
            costScience = scienceCost * experienceModifier;

            //Get funds cost
            costFunds = fundsCost * experienceModifier;

            //Reputation gives a discount: 20% * (current rep / 1000)
            //Only applies in career mode.
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                float reputation = BARISScenario.ReputationCostModifier * (Reputation.Instance.reputation / Reputation.RepRange);
                if (reputation > 0.0f)
                {
                    costScience -= (costScience * reputation);
                    costFunds -= (costFunds * reputation);
                }
            }

            //Part count & reliability
            GUILayout.Label("<color=white><b>" + Localizer.Format(BARISScenario.TestBenchPartCount) + "</b>" + qualityControlModules.Length + "</color>");

            GUILayout.BeginScrollView(originPoint, infoPanelOptions);
            GUILayout.Label("<color=white><b> " + Localizer.Format(BARISScenario.TestBenchReliabilityBefore) + "</b>" + reliability + "/" + maxReliability + "</color>");
            GUILayout.Label("<color=white><b> " + Localizer.Format(BARISScenario.TestBenchReliabilityAfter) + "</b>" + reliabilityAfter + "/" + maxReliabilityAfter + "</color>");
            GUILayout.EndScrollView();

            //Science cost to upgrade flight experience.
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
            {
                canAffordScience = ResearchAndDevelopment.CanAfford(costScience);

                GUILayout.BeginScrollView(originPoint, infoPanelOptions); 
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(scienceIcon, buttonOptions) && canAffordScience)
                {
                    ResearchAndDevelopment.Instance.AddScience(-costScience, TransactionReasons.Any);
                    addFlightExperience();
                }
                if (canAffordScience)
                    GUILayout.Label("<color=white><b>" + Localizer.Format(BARISScenario.TestBenchScienceCost) + "</b>" + string.Format("{0:n2}", costScience) + "</color>");
                else
                    GUILayout.Label("<color=red><b>" + Localizer.Format(BARISScenario.TestBenchScienceCost) + "</b>" + string.Format("{0:n2}", costScience) + "</color>");
                GUILayout.EndHorizontal();
                GUILayout.EndScrollView();
            }

            //Funds cost to upgrade flight experience.
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                canAffordFunds = Funding.CanAfford(costFunds);

                GUILayout.BeginScrollView(originPoint, infoPanelOptions); 
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(fundsIcon, buttonOptions) && canAffordFunds)
                {
                    Funding.Instance.AddFunds(-costFunds, TransactionReasons.Any);
                    addFlightExperience();
                }
                if (canAffordFunds)
                    GUILayout.Label("<color=white><b>" + Localizer.Format(BARISScenario.TestBenchFundsCost) + "</b>" + string.Format("{0:n2}", costFunds) + "</color>");
                else
                    GUILayout.Label("<color=red><b>" + Localizer.Format(BARISScenario.TestBenchFundsCost) + "</b>" + string.Format("{0:n2}", costFunds) + "</color>");
                GUILayout.EndHorizontal();
                GUILayout.EndScrollView();
            }

            //How much experience to add
            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=white>" + Localizer.Format(BARISScenario.TestBenchExpToAdd) + "</color>");
            buttonIndex = GUILayout.SelectionGrid(buttonIndex, experienceModifiers, 3);
            GUILayout.EndHorizontal();

            switch (buttonIndex)
            {
                case 0:
                    experienceModifier = 1;
                    break;

                case 1:
                    experienceModifier = 5;
                    break;

                case 2:
                    experienceModifier = 10;
                    break;
            }
        }

        protected void addFlightExperience()
        {
            string partTitle = string.Empty;

            for (int index = 0; index < qualityControlModules.Length; index++)
            {
                //Get the part title
                partTitle = qualityControlModules[index].part.partInfo.title;

                //Record the flight experience.
                BARISScenario.Instance.RecordFlightExperience(qualityControlModules[index].part, BARISSettingsLaunch.FlightsPerQualityBonus * experienceModifier);

                //Calculate the new quality rating.
                int totalQuality = qualityControlModules[index].quality + BARISScenario.Instance.GetFlightBonus(qualityControlModules[index].part);
                string message = partTitle + " " + BARISScenario.QualityLabel + totalQuality;

                //Inform the user.
                ScreenMessages.PostScreenMessage(message, BARISScenario.MessageDuration, ScreenMessageStyle.UPPER_CENTER);
            }

            //Don't forget to update the editor bays
            BARISScenario.Instance.UpdateEditorBayFlightBonuses();

            //Save the game
            GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
        }

        protected void calculateCostsAndReliability()
        {
            ModuleQualityControl qualityControl;
            int totalMaxReliability = 100 * qualityControlModules.Length;
            int totalQuality = 0;
            int totalMaxQuality = 0;
            int totalQualityAfter = 0;
            int totalMaxQualityAfter = 0;
            int baseQuality = 0;
            int integrationCap = BARISScenario.Instance.GetIntegrationCap(isVAB);
            string partTtile;
            int flightCount = 0;

            fundsCost = 0;
            flightLog.Clear();
            for (int index = 0; index < qualityControlModules.Length; index++)
            {
                //Get quality control module.
                qualityControl = qualityControlModules[index];
                partTtile = qualityControl.part.partInfo.title;

                //Update predictive flight log so we can predict what the results of the simulation will do.
                if (flightLog.ContainsKey(partTtile) == false)
                {
                    flightCount = BARISScenario.Instance.GetFlightCount(partTtile);
                    flightLog.Add(partTtile, flightCount);
                }
                flightCount = flightLog[partTtile];
                flightCount += BARISSettingsLaunch.FlightsPerQualityBonus * experienceModifier;
                flightLog[partTtile] = flightCount;

                //Quality: before testing
                baseQuality = qualityControl.quality + BARISScenario.Instance.GetFlightBonus(partTtile);
                totalQuality += baseQuality;
                totalMaxQuality += baseQuality + integrationCap;

                //Cost
                fundsCost += qualityControl.part.partInfo.cost / 4;
            }

            //Now that we have a predicted flight log, we can look at quality after integration.
            for (int index = 0; index < qualityControlModules.Length; index++)
            {
                //Get quality control module.
                qualityControl = qualityControlModules[index];
                partTtile = qualityControl.part.partInfo.title;

                //Quality: after testing
                baseQuality = qualityControl.quality + BARISScenario.Instance.GetFlightBonus(flightLog[partTtile]);
                totalQualityAfter += baseQuality;
                totalMaxQualityAfter += baseQuality + integrationCap;
            }

            //Calculate reliability: before testing
            reliability = Mathf.RoundToInt(((float)(totalQuality) / (float)totalMaxReliability) * 100.0f);
            maxReliability = Mathf.RoundToInt(((float)(totalMaxQuality) / (float)totalMaxReliability) * 100.0f);
            if (reliability < 0)
            {
                reliability = 0;
                maxReliability = 0;
            }

            //Calculate reliability: after testing
            reliabilityAfter = Mathf.RoundToInt(((float)(totalQualityAfter) / (float)totalMaxReliability) * 100.0f);
            maxReliabilityAfter = Mathf.RoundToInt(((float)(totalMaxQualityAfter) / (float)totalMaxReliability) * 100.0f);
            if (reliabilityAfter < 0)
            {
                reliabilityAfter = 0;
                maxReliabilityAfter = 0;
            }

            //Adjust funds cost.
            fundsCost *= BARISSettingsLaunch.MultiplierPerExpBonus;

            //Get science cost
            scienceCost = BARISSettingsLaunch.FlightsPerQualityBonus * qualityControlModules.Length;

        }
    }
}
