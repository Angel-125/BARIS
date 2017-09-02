using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
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
    public class BARISEditorView : Dialog<BARISAppFlightView>
    {
        const int DialogWidth = 400;
        const int DialogHeight = 520;

        public bool isVAB = true;

        private Vector2 scrollPos;
        private List<EditorBayView> editorBays = new List<EditorBayView>();
        private int maxAvailableWorkers;
        private int maxBays;

        protected void debugLog(string message)
        {
            if (BARISScenario.showDebug == true)
                Debug.Log("[BARISEditorView] - " + message);
        }

        public BARISEditorView() :
        base("Blah", DialogWidth, DialogHeight)
        {
            WindowTitle = Localizer.Format(BARISScenario.EditorViewTitle);
            Resizable = false;
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);
            if (!newValue)
                return;

            //Are we in VAB or SPH?
            if (EditorLogic.fetch.ship.shipFacility == EditorFacility.VAB)
                isVAB = true;
            else
                isVAB = false;

            //Current max available workers
            maxAvailableWorkers = BARISScenario.Instance.GetMaxAvailableWorkers(isVAB);

            //Current max available editor bays
            //This depends upon facility level.
            maxBays = BARISScenario.Instance.GetMaxBays(isVAB);
            debugLog("maxBays: " + maxBays);

            //Create the editor bays
            EditorBayView bayView;
            editorBays.Clear();
            for (int index = 0; index < maxBays; index++)
            {
                bayView = new EditorBayView(index, isVAB);
                bayView.addWorker = canAddWorker;
                bayView.removeWorker = canRemoveWorker;
                bayView.returnWorkers = returnWorkers;
                bayView.closeView = closeView;
                editorBays.Add(bayView);
                debugLog("Added Bay: " + index);
            }
        }

        protected void closeView()
        {
            SetVisible(false);
        }

        protected void returnWorkers(int workerCount)
        {
            int availableWorkers = BARISScenario.Instance.GetAvailableWorkers(isVAB);
            availableWorkers += workerCount;
            BARISScenario.Instance.SetAvailableWorkers(availableWorkers, isVAB);
        }

        protected bool canAddWorker(int currentWorkers)
        {
            //Is the currentWorkers < total workers allowed per high bay?
            if (currentWorkers < BARISScenario.MaxWorkersPerBay)
            {
                //Do we have at least 1 available worker in the pool?
                int availableWorkers = BARISScenario.Instance.GetAvailableWorkers(isVAB);
                if (availableWorkers >= 1)
                {
                    availableWorkers -= 1;
                    BARISScenario.Instance.SetAvailableWorkers(availableWorkers, isVAB);
                    return true;
                }
            }

            return false;
        }

        protected bool canRemoveWorker(int currentWorkers)
        {
            int availableWorkers = BARISScenario.Instance.GetAvailableWorkers(isVAB);
            if (availableWorkers <= BARISScenario.MaxWorkersPerFacility && currentWorkers > 0)
            {
                availableWorkers += 1;
                BARISScenario.Instance.SetAvailableWorkers(availableWorkers, isVAB);
                return true;
            }

            return false;
        }

        protected override void DrawWindowContents(int windowId)
        {
            int availableWorkers = BARISScenario.Instance.GetAvailableWorkers(isVAB);

            //Available workers
            GUILayout.Label("<color=white><b>" + Localizer.Format(BARISScenario.AvailableWorkersLabel) + "</b>" + availableWorkers + "/" + maxAvailableWorkers + "</color>");

            GUILayout.BeginVertical();

            //High bays
            scrollPos = GUILayout.BeginScrollView(scrollPos, true, false);
            GUILayout.BeginHorizontal();

            //Draw the bays
            for (int index = 0; index < maxBays; index++)
                editorBays[index].DrawEditorBay();

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();

            //Flight experience debug option
            if (BARISScenario.showDebug)
            {
                if (GUILayout.Button("Loaded Vessel: +5 Flight Experience"))
                    addFlightExperience();
                if (GUILayout.Button("Clear Flight Experience Log"))
                {
                    BARISScenario.Instance.ClearFlightExperienceLog();
                    BARISScenario.Instance.UpdateEditorBayFlightBonuses();
                }
            }
            GUILayout.EndVertical();
        }

        protected void addFlightExperience()
        {
            if (EditorLogic.fetch.ship.parts.Count == 0)
            {
                BARISScenario.Instance.LogPlayerMessage("Load a vessel before trying to add flight experience.");
                return;
            }

            Part[] parts = EditorLogic.fetch.ship.parts.ToArray();
            ModuleQualityControl qualityControl;
            foreach (Part part in parts)
            {
                qualityControl = part.FindModuleImplementing<ModuleQualityControl>();
                if (qualityControl != null)
                {
                    BARISScenario.Instance.RecordFlightExperience(part, 5);
                }
            }

            //Update editor bays
            BARISScenario.Instance.UpdateEditorBayFlightBonuses();

            //Save the game
            GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
        }
    }
}
