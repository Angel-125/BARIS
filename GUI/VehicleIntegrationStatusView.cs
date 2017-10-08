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
    public class VehicleIntegrationStatusView : Dialog<VehicleIntegrationStatusView>
    {
        const int DialogWidth = 350;
        const int DialogHeight = 600;
        const int InfoPanelHeight = 120;

        private EditorBayItem[] editorBayItems = null;
        private Vector2 scrollPos;
        private GUILayoutOption[] infoPanelOptions = new GUILayoutOption[] { GUILayout.Height(InfoPanelHeight) };
        private GUILayoutOption[] scrollViewOptions = new GUILayoutOption[] { GUILayout.Width(DialogWidth) };
        private Vector2 originPoint = new Vector2(0, 0);

        public VehicleIntegrationStatusView() :
        base("Blah", DialogWidth, DialogHeight)
        {
            WindowTitle = Localizer.Format(BARISScenario.EditorViewTitle);
            Resizable = false;
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            if (newValue)
            {
                editorBayItems = BARISScenario.Instance.editorBayItems.Values.ToArray();
            }
        }

        protected override void DrawWindowContents(int windowId)
        {
            if (editorBayItems == null || editorBayItems.Length == 0)
            {
                GUILayout.Label("<color=yellow><b>" + Localizer.Format(BARISScenario.NoBaysMsg) + "</b></color>");
                return;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos, scrollViewOptions);

            EditorBayItem bayItem = null;
            int highBayID = 0;
            for (int index = 0; index < editorBayItems.Length; index++)
            {
                bayItem = editorBayItems[index];
                highBayID = bayItem.editorBayID + 1;

                GUILayout.BeginScrollView(originPoint, infoPanelOptions);
                //Bay ID
                if (bayItem.isVAB)
                    GUILayout.Label("<color=white><b>VAB " + BARISScenario.HighBayLabel + " " + highBayID + "</b></color>");
                else
                    GUILayout.Label("<color=white><b>SPH " + BARISScenario.HangarBayLabel + " " + highBayID + "</b></color>");

                //If there's a vessel, show integration days remaining.
                if (!string.IsNullOrEmpty(bayItem.vesselName))
                {
                    //Vessel name
                    GUILayout.Label("<color=white>" + bayItem.vesselName + "</color>");

                    //Reliability & build time
                    drawReliability(bayItem);
                }

                //Report that there's no vessel in the bay.
                else
                {
                    GUILayout.Label(Localizer.Format(BARISScenario.BayEmptyMsg));
                }
                GUILayout.EndScrollView();

            }

            GUILayout.EndScrollView();
        }

        protected void drawReliability(EditorBayItem editorBayItem)
        {
            //Reliability
            GUILayout.Label("<color=white><b>" + Localizer.Format(BARISScenario.ReliabilityLabel) + "</b>" + editorBayItem.baseReliability + "/" + editorBayItem.maxReliability + "</color>");

            //Build Time Status
            GUILayout.Label(GetIntegrationStatusLabel(editorBayItem));
        }

        public static string GetIntegrationStatusLabel(EditorBayItem editorBayItem)
        {
            //Build Time
            if (editorBayItem.totalIntegrationToAdd > 0 && editorBayItem.workerCount > 0)
            {
                int buildTime = editorBayItem.totalIntegrationToAdd / BARISScenario.Instance.GetWorkerProductivity(editorBayItem.workerCount, editorBayItem.isVAB);
                if (buildTime > 1)
                    return "<color=white><b>" + Localizer.Format(BARISScenario.BuildTimeLabel) + "</b>" + buildTime + Localizer.Format(BARISScenario.BuildTimeLabelDays) + "</color>";
                else if (buildTime == 1)
                    return "<color=white><b>" + Localizer.Format(BARISScenario.BuildTimeLabel) + "</b>" + buildTime + Localizer.Format(BARISScenario.BuildTimeLabelOneDay) + "</color>";
                else
                    return "<color=white><b>" + Localizer.Format(BARISScenario.BuildTimeLabel) + "</b>" + Localizer.Format(BARISScenario.BuildTimeLabelLessDay) + "</color>";
            }

            //Vessel is completed.
            else if (editorBayItem.totalIntegrationToAdd == 0)
            {
                return "<color=white><b>" + Localizer.Format(BARISScenario.BuildTimeLabelStatus) + "</b>" + Localizer.Format(BARISScenario.BuildTimeLabelDone) + "</color>";
            }

            //No workers.
            else
            {
                return "<color=white><b>" + Localizer.Format(BARISScenario.BuildTimeLabelStatus) + "</b>" + Localizer.Format(BARISScenario.BuildTimeLabelNeedsWorkers) + "</color>";
            }
        }
    }
}
