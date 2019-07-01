using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
using Upgradeables;
using KSP.UI.Screens;
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
    public class BARISFocusVesselView : Dialog<BARISFocusVesselView>
    {
        const int DialogWidth = 300;
        const int DialogHeight = 250;
        const int InfoPanelHeight = 70;

        public List<int> flightGlobalIndexes = new List<int>();
        public List<string> vesselNames = new List<string>();

        private Vector2 scrollPos;
        private GUILayoutOption[] scrollViewOptions = new GUILayoutOption[] { GUILayout.Width(DialogWidth) };
        private GUILayoutOption[] infoPanelOptions = new GUILayoutOption[] { GUILayout.Height(InfoPanelHeight) };
        private Vector2 originPoint = new Vector2(0, 0);

        public BARISFocusVesselView() :
        base("Blah", DialogWidth, DialogHeight)
        {
            WindowTitle = Localizer.Format(BARISScenario.SwitchToVesselTitle);
            Resizable = false;
        }

        protected override void DrawWindowContents(int windowId)
        {
            string buttonLabel = Localizer.Format(BARISScenario.SwitchToVesselTitle);

            GUILayout.BeginVertical();

            GUILayout.Label("<color=white>" + Localizer.Format(BARISScenario.AVesselHasAProblem) + "</color>");

            scrollPos = GUILayout.BeginScrollView(scrollPos, scrollViewOptions);

            for (int index = 0; index < flightGlobalIndexes.Count; index++)
            {
                //Calculate the height based upon how many breakable part modules the part has.
                GUILayout.BeginScrollView(originPoint, infoPanelOptions);

                //Vessel name
                GUILayout.Label("<color=white><b>" + vesselNames[index] + "</b></color>");

                //Switch to vessel button
                if (GUILayout.Button(buttonLabel))
                {
                    SetVisible(false);
                    switchToVessel(flightGlobalIndexes[index]);
                    GUILayout.EndScrollView();
                    GUILayout.EndScrollView();
                    GUILayout.EndVertical();
                    return;
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        protected void switchToVessel(int vesselID)
        {
            //Save the game
            string saveGameFile = GamePersistence.SaveGame("BARIS-SaveGame", HighLogic.SaveFolder, SaveMode.OVERWRITE);

            //Load it again so we have a game
            Game tempGame = GamePersistence.LoadGame(saveGameFile, HighLogic.SaveFolder, false, false);

            //We've got game! Now switch to the vessel
            FlightDriver.StartAndFocusVessel(tempGame, vesselID);
        }
    }
}
