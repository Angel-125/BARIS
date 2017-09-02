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
    public class BARISAppCenterView : Dialog<BARISAppCenterView>
    {
        const float RepeatButtonSpeed = 0.2f;

        const int DialogWidth = 350;
        const int DialogHeight = 365;
        const int InfoPanelHeight = 65;

        private GUILayoutOption[] scrollViewOptions = new GUILayoutOption[] { GUILayout.Width(DialogWidth) };
        private GUILayoutOption[] infoPanelOptions = new GUILayoutOption[] { GUILayout.Height(InfoPanelHeight) };
        private GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.Height(24), GUILayout.Width(24) };
        private Vector2 originPoint = new Vector2(0, 0);
        private float workerRequest;
        private BARISEventButtonsView eventButtonsView = new BARISEventButtonsView();
        private TestBenchView testBenchView = new TestBenchView();

        public BARISAppCenterView() :
        base("Blah", DialogWidth, DialogHeight)
        {
            WindowTitle = Localizer.Format(BARISScenario.FacilitiesTitle);
            Resizable = false;

            if (BARISScenario.isKCTInstalled)
            {
                int kctHeight = 125;

                windowPos = new Rect((Screen.width - DialogWidth) / 2, (Screen.height - kctHeight) / 2, DialogWidth, kctHeight);
            }
        }

        protected void debugLog(string message)
        {
            if (BARISScenario.showDebug == true)
                Debug.Log("[BARISAppCenterView] - " + message);
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);
            if (newValue)
            {
                BARISScenario.Instance.PlayProblemSound();
            }
        }

        protected void drawVABWorkers()
        {
            int availableWorkers = BARISScenario.Instance.GetAvailableWorkers(true);
            int maxAvailableWorkers = BARISScenario.Instance.GetMaxAvailableWorkers(true);

            //Available workers also needs to account for workers currently working.
            int workersWorking = BARISScenario.Instance.GetWorkersWorking(true);
            int totalWorkers = availableWorkers + workersWorking;

            GUILayout.BeginScrollView(originPoint, infoPanelOptions);
            GUILayout.Label("<color=white><b>" + BARISScenario.VABWorkersLabel + "</b>" + totalWorkers + "/" + maxAvailableWorkers +
                " (" + workersWorking + Localizer.Format(BARISScenario.WorkersWorkingLabel) + ")</color>");
            GUILayout.BeginHorizontal();

            //Remove workers button
            if (GUILayout.RepeatButton("-"))
            {
                workerRequest += RepeatButtonSpeed;
                if (workerRequest >= 1.0f)
                {
                    workerRequest = 0f;
                    if (availableWorkers - 1 >= 0)
                    {
                        availableWorkers -= 1;
                        BARISScenario.Instance.SetAvailableWorkers(availableWorkers, true);
                    }
                }
            }

            //Add workers button
            if (GUILayout.RepeatButton("+"))
            {
                workerRequest += RepeatButtonSpeed;
                if (workerRequest >= 1.0f)
                {
                    workerRequest = 0f;
                    if (availableWorkers + workersWorking + 1 <= maxAvailableWorkers)
                    {
                        availableWorkers += 1;
                        BARISScenario.Instance.SetAvailableWorkers(availableWorkers, true);
                    }
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        protected void drawSPHWorkers()
        {
            int availableWorkers = BARISScenario.Instance.GetAvailableWorkers(false);
            int maxAvailableWorkers = BARISScenario.Instance.GetMaxAvailableWorkers(false);

            //Available workers also needs to account for workers currently working.
            int workersWorking = BARISScenario.Instance.GetWorkersWorking(false);
            int totalWorkers = availableWorkers + workersWorking;

            GUILayout.BeginScrollView(originPoint, infoPanelOptions);
            GUILayout.Label("<color=white><b>" + BARISScenario.SPHWorkersLabel + "</b>" + totalWorkers + "/" + maxAvailableWorkers +
                " (" + workersWorking + Localizer.Format(BARISScenario.WorkersWorkingLabel) + ")</color>");
            GUILayout.BeginHorizontal();

            //Remove workers button
            if (GUILayout.RepeatButton("-"))
            {
                workerRequest += RepeatButtonSpeed;
                if (workerRequest >= 1.0f)
                {
                    workerRequest = 0f;
                    if (availableWorkers - 1 >= 0)
                    {
                        availableWorkers -= 1;
                        BARISScenario.Instance.SetAvailableWorkers(availableWorkers, false);
                    }
                }
            }

            //Add workers button
            if (GUILayout.RepeatButton("+"))
            {
                workerRequest += RepeatButtonSpeed;
                if (workerRequest >= 1.0f)
                {
                    workerRequest = 0f;
                    if (availableWorkers + workersWorking + 1 <= maxAvailableWorkers)
                    {
                        availableWorkers += 1;
                        BARISScenario.Instance.SetAvailableWorkers(availableWorkers, false);
                    }
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        protected int getActiveDutyAstronauts()
        {
            int astronautCount = 0;
            KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;

            astronautCount += roster.GetAssignedCrewCount() + roster.GetAvailableCrewCount();

            return astronautCount;
        }

        protected void drawPayroll()
        {
            int workerCost = BARISScenario.Instance.GetAvailableWorkers(true) + BARISScenario.Instance.GetAvailableWorkers(false);
            workerCost *= BARISScenario.PayrollPerWorker;
            int astronautCost = getActiveDutyAstronauts();
            astronautCost *= BARISScenario.PayrollPerAstronaut;

            GUILayout.BeginScrollView(originPoint, infoPanelOptions);

            if (BARISSettingsLaunch.WorkersCostFunds && !BARISScenario.isKCTInstalled)
            {
                GUILayout.Label("<color=white><b>" + Localizer.Format(BARISScenario.Payroll1Label) + "</b>" +
                    string.Format("{0:n0}", workerCost) + Localizer.Format(BARISScenario.Payroll2Label) + BARISScenario.DaysPerPayroll + Localizer.Format(BARISScenario.Payroll3Label) + "</color>");
            }

            if (BARISSettingsLaunch.AstronautsCostFunds)
            {
                GUILayout.Label("<color=white><b>" + Localizer.Format(BARISScenario.PayrollAstronautsLabel) + "</b>" +
                    string.Format("{0:n0}", astronautCost) + Localizer.Format(BARISScenario.Payroll2Label) + BARISScenario.DaysPerPayroll + Localizer.Format(BARISScenario.Payroll3Label) + "</color>");
            }

            GUILayout.EndScrollView();
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();

            //VAB/SPH benefits
            GUILayout.BeginScrollView(originPoint, infoPanelOptions);
            int vabSPHBonus = BARISScenario.Instance.GetFacilityBonus();
            GUILayout.Label("<color=white><b>" + BARISScenario.FacilityBonusLabel + "</b>+" + vabSPHBonus + "</color>");

            //Astronaut facility benefits
            int astronautFacilityBonus = BARISScenario.Instance.GetAstronautFacilityBonus();
            GUILayout.Label("<color=white><b>" + BARISScenario.AstronautFacilityBonusLabel + "</b>+" + vabSPHBonus + "</color>");
            GUILayout.EndScrollView();

            //Use BARIS construction system if KCT is not installed.
            if (!BARISScenario.isKCTInstalled)
            {
                if (BARISSettingsLaunch.VesselsNeedIntegration && BARISSettingsLaunch.LaunchesCanFail)
                {
                    //Number of High Bays & Hangar Bays
                    GUILayout.BeginScrollView(originPoint, infoPanelOptions);
                    GUILayout.Label("<color=white><b>" + BARISScenario.TotalHighBaysLabel + "</b>" + BARISScenario.Instance.GetMaxBays(true) + "</color>");
                    GUILayout.Label("<color=white><b>" + BARISScenario.TotalHangarBaysLabel + "</b>" + BARISScenario.Instance.GetMaxBays(false) + "</color>");
                    GUILayout.EndScrollView();

                    //Hire/Fire workers
                    if (BARISScenario.Instance.workPausedDays > 0)
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.BeginScrollView(originPoint, infoPanelOptions);
                        GUILayout.Label("<color=white><b>" + Localizer.Format(BARISScenario.WorkPausedMsg1) +
                            BARISScenario.Instance.workPausedDays + Localizer.Format(BARISScenario.WorkPausedMsg2) + "</b></color>");
                        GUILayout.EndScrollView();
                        GUILayout.FlexibleSpace();
                    }
                    else
                    {
                        drawVABWorkers();
                        drawSPHWorkers();
                    }
                }

                else //No integration needed/launches can't fail
                {
                    GUILayout.BeginScrollView(originPoint, infoPanelOptions);
                    GUILayout.EndScrollView();
                    GUILayout.BeginScrollView(originPoint, infoPanelOptions);
                    GUILayout.EndScrollView();
                }
            }

            //Payroll: workers aren't available when KCT is installed, but you can still pay astronauts.
            drawPayroll();

            //Unloaded quality check
            if (BARISScenario.showDebug)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Reliability Check"))
                    BARISScenario.Instance.PerformReliabilityChecks();
                if (GUILayout.Button("Payday"))
                    BARISScenario.Instance.PayWorkers();
                if (GUILayout.Button("Build Time"))
                    BARISScenario.Instance.UpdateBuildTime();
                if (GUILayout.Button("Events"))
                {
                    eventButtonsView.SetVisible(!eventButtonsView.IsVisible());
                    SetVisible(false);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            //Vehicle Integration Status
            if (GUILayout.Button(Localizer.Format(BARISScenario.EditorViewTitle)))
            {
                BARISAppButton.vehicleIntegrationStatusView.SetVisible(true);
                SetVisible(false);
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

    }
}
