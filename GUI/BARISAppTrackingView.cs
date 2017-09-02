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
        const int DialogWidth = 350;
        const int DialogHeight = 600;
        const int InfoPanelHeight = 60;

        private Vector2 scrollPos;
        private GUILayoutOption[] scrollViewOptions = new GUILayoutOption[] { GUILayout.Width(DialogWidth) };
        private GUILayoutOption[] infoPanelOptions = new GUILayoutOption[] { GUILayout.Height(InfoPanelHeight) };
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
                {
                    continue;
                }
                
                //Update the summary
                qualitySummary.UpdateAndGetFailureCandidates(0);

                //Now draw the current reliability
                GUILayout.BeginScrollView(originPoint, infoPanelOptions);

                //Vessel name
                GUILayout.Label("<color=white><b>" + unloadedVessels[index].vesselName + "</b></color>");

                //Reliability
                GUILayout.Label("<color=white><b>" + BARISScenario.ReliabilityLabel + "</b> " + qualitySummary.reliability + "%</color>");

                GUILayout.EndScrollView();
            }

            GUILayout.EndScrollView();
        }

    }
}
