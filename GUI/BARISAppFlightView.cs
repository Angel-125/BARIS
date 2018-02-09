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
    public class BARISAppFlightView : Dialog<BARISAppFlightView>
    {
        const int DialogWidth = 350;
        const int DialogHeight = 600;
        const int InfoPanelHeight = 60;

        private BreakablePart[] breakableParts;
        private Vector2 scrollPos;
        private GUILayoutOption[] scrollViewOptions = new GUILayoutOption[] { GUILayout.Width(DialogWidth) };
        private GUILayoutOption[] infoPanelOptions = new GUILayoutOption[] { GUILayout.Height(InfoPanelHeight) };
        private GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.Height(24), GUILayout.Width(24) };
        private Vector2 originPoint = new Vector2(0, 0);
        private List<Vector2> partInfoScrollPositions = new List<Vector2>();
        static Texture wrenchIcon = null;
        int reliability = 0;

        public BARISAppFlightView() :
        base("Blah", DialogWidth, DialogHeight)
        {
            WindowTitle = Localizer.Format(BARISScenario.AppFlightViewTitle);
            Resizable = false;
        }

        public void UpdateCachedData()
        {
            if (IsVisible() == false)
                return;

            int totalQuality = 0;
            List<BreakablePart> partList = BARISScenario.Instance.GetBreakableParts();
            List<ModuleQualityControl> qualityModules = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleQualityControl>();
            if (partList.Count > 0)
            {
                breakableParts = partList.ToArray();
                for (int curItem = 0; curItem < breakableParts.Length; curItem++)
                    partInfoScrollPositions.Add(new Vector2(0, 0));

                //Calculate reliability
                for (int index = 0; index < breakableParts.Length; index++)
                {
                    totalQuality += breakableParts[index].qualityControl.currentQuality;
                }

                int maxQuality = 100 * qualityModules.Count;
                reliability = Mathf.RoundToInt(((float)totalQuality / (float)maxQuality) * 100.0f);

                bool createdNewRecord = false;
                LoadedQualitySummary summary = BARISScenario.Instance.GetLoadedQualitySummary(FlightGlobals.ActiveVessel, out createdNewRecord);
                if (summary.reliability != reliability)
                    summary.reliability = reliability;
            }
        }

        protected void debugLog(string message)
        {
            if (BARISScenario.showDebug == true)
                Debug.Log("[BARISAppFlightView] - " + message);
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            if (newValue)
            {
                if (wrenchIcon == null)
                    wrenchIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/000BARIS/Icons/Wrench", false);

                //Game events
                //GameEvents.onStageSeparation.Add(onStageSeparation);
                BARISScenario.Instance.onQualityCheck += onQualityCheck;

                //Update the cache
                UpdateCachedData();
            }

            else
            {
                //GameEvents.onStageSeparation.Remove(onStageSeparation);
                BARISScenario.Instance.onQualityCheck -= onQualityCheck;
            }
        }

        protected void onQualityCheck(QualityCheckResult result)
        {
            UpdateCachedData();
        }

        protected void onStageSeparation(EventReport report)
        {
            UpdateCachedData();
        }

        protected override void DrawWindowContents(int windowId)
        {
            if (BARISScenario.showDebug)
                drawDebugButtons();

            if (breakableParts == null)
                return;
            if (breakableParts.Length > 0)
                drawBreakableParts();
        }

        protected void modifyPartQuality(int qualityModifier)
        {
            BARISScenario.Instance.ModifyPartQuality(FlightGlobals.ActiveVessel, qualityModifier);
            UpdateCachedData();
        }

        protected void zeroPartQuality()
        {
            BARISScenario.Instance.ZeroPartQuality(FlightGlobals.ActiveVessel);
            UpdateCachedData();
        }

        protected void zeroPartMTBF()
        {
            BARISScenario.Instance.ZeroPartMTBF(FlightGlobals.ActiveVessel);
            UpdateCachedData();
        }

        protected void drawDebugButtons()
        {
            if (GUILayout.Button("Next staging is success"))
            {
                BARISLaunchFailManager.Instance.NextStagingIsSuccessful = true;
                ScreenMessages.PostScreenMessage("Next staging event will succeed.", 3.0f, ScreenMessageStyle.UPPER_LEFT);
            }

            if (GUILayout.Button("Next staging is astronaut avert"))
            {
                BARISLaunchFailManager.Instance.NextStagingAstonautAverted = true;
                ScreenMessages.PostScreenMessage("Next staging event will be astronaut averted.", 3.0f, ScreenMessageStyle.UPPER_LEFT);
            }

            if (GUILayout.Button("Next staging is a failure"))
            {
                BARISLaunchFailManager.Instance.NextStagingFails = true;
                ScreenMessages.PostScreenMessage("Next staging event will fail.", 3.0f, ScreenMessageStyle.UPPER_LEFT);
            }

            if (GUILayout.Button("Next staging is critical failure"))
            {
                BARISLaunchFailManager.Instance.NextStagingIsCascadeFailure = true;
                ScreenMessages.PostScreenMessage("Next staging event will catastrophically fail.", 3.0f, ScreenMessageStyle.UPPER_LEFT);
            }

            if (GUILayout.Button("Check Yo Stagin"))
                BARISLaunchFailManager.Instance.CheckYoStaging();

            if (GUILayout.Button("All parts: +5 Quality"))
                modifyPartQuality(5);

            if (GUILayout.Button("All parts: -5 Quality"))
                modifyPartQuality(-5);

            if (GUILayout.Button("All parts: +10 Quality"))
                modifyPartQuality(10);

            if (GUILayout.Button("All parts: -10 Quality"))
                modifyPartQuality(-10);

            if (GUILayout.Button("All parts: Max Quality"))
                modifyPartQuality(1000);

            if (GUILayout.Button("All parts: Zero Quality"))
                zeroPartQuality();

            if (GUILayout.Button("All parts: Zero MTBF"))
                zeroPartMTBF();

            if (GUILayout.Button("Fix all"))
                BARISScenario.Instance.FixAllParts(FlightGlobals.ActiveVessel);

            if (GUILayout.Button("All parts: +5 Flight Experience"))
                addFlightExperience();
        }

        protected void addFlightExperience()
        {
            List<ModuleQualityControl> qualityModules = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleQualityControl>();

            foreach (ModuleQualityControl qualityModule in qualityModules)
            {
                //Add flight experience
                BARISScenario.Instance.RecordFlightExperience(qualityModule.part, 5);

                //Now recalcuate quality
                qualityModule.flightExperienceBonus = BARISScenario.Instance.GetFlightBonus(qualityModule.part);
                qualityModule.quality = qualityModule.GetMaxQuality();
                qualityModule.currentQuality = qualityModule.quality;
                qualityModule.UpdateQualityDisplay(BARISScenario.GetConditionSummary(qualityModule.currentMTBF, qualityModule.MaxMTBF, qualityModule.currentQuality, qualityModule.quality));
                debugLog(qualityModule.part.partInfo.title + " Flight Experience: " + qualityModule.flightExperienceBonus + " New Quality: " + qualityModule.quality);
            }

            //Update editor bays
            BARISScenario.Instance.UpdateEditorBayFlightBonuses();

            //Update the cache
            UpdateCachedData();

            //Save the game
            GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
        }

        protected void drawBreakableParts()
        {
            BreakablePart breakablePart;
            Vector2 panelScrollPos;

            scrollPos = GUILayout.BeginScrollView(scrollPos, scrollViewOptions);

            //Draw vessel reliability
            GUILayout.Label("<color=white><b>" + Localizer.Format(BARISScenario.ReliabilityLabel) + "</b>" + reliability + "</color>");

            for (int index = 0; index < breakableParts.Length; index++)
            {
                breakablePart = breakableParts[index];

                //Calculate the height based upon how many breakable part modules the part has.
                panelScrollPos = partInfoScrollPositions[index];
                panelScrollPos = GUILayout.BeginScrollView(panelScrollPos, infoPanelOptions);
                partInfoScrollPositions[index] = panelScrollPos;

                //Part title
                GUILayout.Label("<color=white><b>" + breakablePart.part.partInfo.title + "</b></color>");

                //Status
                drawStatus(breakablePart);

                GUILayout.EndScrollView();
            }

            GUILayout.EndScrollView();

            //Highlight breakble parts button
            if (GUILayout.Button(Localizer.Format(BARISScenario.HighlightBrokenPartsMsg)))
            {
                for (int index = 0; index < breakableParts.Length; index++)
                {
                    breakablePart = breakableParts[index];

                    if (breakablePart.qualityControl.IsBroken)
                        breakablePart.qualityControl.SetupBrokenHighlighting();
                }
            }
        }

        protected void drawStatus(BreakablePart breakablePart)
        {
            //Part might be ok or need maintenance.
            if (breakablePart.qualityControl.IsBroken == false)
            {
                //If we still have MTBF then we're ok.
                if (breakablePart.qualityControl.currentMTBF > 0)
                {
                    GUILayout.Label(Localizer.Format(BARISScenario.ConditionLabel) + breakablePart.qualityControl.qualityDisplay);
                }

                //Part needs maintenance
                else
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("<color=yellow>" + Localizer.Format(BARISScenario.ConditionLabel) + breakablePart.qualityControl.qualityDisplay + "</color>");

                    //Draw fixit button if EVA-only repairs is disabled.
                    if (!BARISSettings.RepairsRequireEVA)
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(wrenchIcon, buttonOptions))
                            breakablePart.qualityControl.PerformMaintenance();
                    }
                    GUILayout.EndHorizontal();
                }
            }

            //Part needs repairs
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("<color=red>" + breakablePart.qualityControl.qualityDisplay + "</color>");

                //Draw fixit button if EVA-only repairs is disabled.
                if (!BARISSettings.RepairsRequireEVA)
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(wrenchIcon, buttonOptions))
                        breakablePart.qualityControl.RepairPart();
                }
                GUILayout.EndHorizontal();
            }
        }
    }
}
