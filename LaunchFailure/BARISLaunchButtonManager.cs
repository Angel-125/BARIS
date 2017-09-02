using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;
using KSP.UI.Screens.Flight;
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
    /// <summary>
    /// This class watches for the editor's launch button click events as well as vessel modification events. If the player loads a vessel from a High Bay or Hangar Bay,
    /// and then modifies the vessel in some way by adding or removing parts, then its vehicle integration efforts are declared failed. At this point BARIS
    /// resets the quality of each breakable part to pre-integration levels. To regain those levels, the player must re-load the vessel 
    /// from the High Bay/Hangar Bay before attempting a launch.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class BARISLaunchButtonManager : MonoBehaviour
    {
        /// <summary>
        /// An instance of the BARISLaunchButtonManager class.
        /// </summary>
        public static BARISLaunchButtonManager Instance;

        /// <summary>
        /// The EditorBayItem object that was loaded from the High Bay or Hangar Bay
        /// </summary>
        public static EditorBayItem editorBayItem = null;

        private BARISGenericMessageView messageView = new BARISGenericMessageView();
        static bool errorMessageShown = false;
        private bool clearedIntegrationBonus = false;
        private bool isVAB = true;

        protected void debugLog(string message)
        {
            if (BARISScenario.showDebug == true)
                Debug.Log("[BARISLaunchButtonManager] - " + message);
        }

        public void Awake()
        {
            if (BARISScenario.isKCTInstalled)
                return;
            debugLog("Awake called");
            Instance = this;

            //Are we in VAB or SPH?
            if (EditorLogic.fetch.ship.shipFacility == EditorFacility.VAB)
                isVAB = true;
            else
                isVAB = false;

            //Intercept the launch button events
            EditorLogic.fetch.launchBtn.onClick.RemoveListener(new UnityEngine.Events.UnityAction(EditorLogic.fetch.launchVessel));
            EditorLogic.fetch.launchBtn.onClick.AddListener(new UnityEngine.Events.UnityAction(BARISLaunchButtonManager.Instance.launchVessel));
            EditorLogic.fetch.newBtn.onClick.AddListener(new UnityEngine.Events.UnityAction(BARISLaunchButtonManager.Instance.onNewVessel));

            //Game Events
            GameEvents.onEditorShipModified.Add(onEditorShipModified);

            //Load before fire hint
            if (!BARISScenario.showedLoadBeforeFlightTip && BARISScenario.partsCanBreak)
            {
                BARISScenario.showedLoadBeforeFlightTip = true;
                BARISEventCardView cardView = new BARISEventCardView();

                cardView.WindowTitle = BARISScenario.LoadBeforeFlightTitle;
                cardView.description = BARISScenario.LoadBeforeFlightMsg;

                cardView.SetVisible(true);
                GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
            }
        }

        public void Destroy()
        {
            if (BARISScenario.isKCTInstalled)
                return;
            //Reset listeners
            EditorLogic.fetch.launchBtn.onClick.RemoveListener(new UnityEngine.Events.UnityAction(BARISLaunchButtonManager.Instance.launchVessel));
            EditorLogic.fetch.newBtn.onClick.RemoveListener(new UnityEngine.Events.UnityAction(BARISLaunchButtonManager.Instance.onNewVessel));

            //Events
            GameEvents.onEditorShipModified.Remove(onEditorShipModified);
        }

        protected void onNewVessel()
        {
            debugLog("New vessel being created, clearing the editor bay");
            editorBayItem = null;
        }

        protected void onEditorShipModified(ShipConstruct ship)
        {
            Part[] shipParts = ship.parts.ToArray();
            ModuleQualityControl qualityControl;

            debugLog("onEditorShipModified called");

            //If build time is disabled then just set the integration bonus to maximum.
            if (BARISSettingsLaunch.VesselsNeedIntegration == false)
            {
                int integrationBonus = BARISScenario.Instance.GetIntegrationCap(isVAB);

                debugLog("Vehicle integration is disabled, setting parts to max integration bonus: " + integrationBonus);
                foreach (Part part in shipParts)
                {
                    qualityControl = part.FindModuleImplementing<ModuleQualityControl>();
                    if (qualityControl != null)
                    {
                        qualityControl.integrationBonus = integrationBonus;
                    }
                }
                return;
            }

            //If you launch a vessel that was loaded from an editor bay, then go back to the editor, that vessel will be loaded again.
            //For BARIS purposes though, we need to treat it as a vessel that hasn't been integrated yet. To do that, we clear the integration bonus.
            if (editorBayItem == null)
            {
                debugLog("no editorBayItem found, clearing integration bonus.");
                if (!clearedIntegrationBonus)
                {
                    clearedIntegrationBonus = true;
                    foreach (Part part in shipParts)
                    {
                        qualityControl = part.FindModuleImplementing<ModuleQualityControl>();
                        if (qualityControl != null)
                        {
                            qualityControl.integrationBonus = 0;
                        }
                    }
                }
                return;
            }

            //onEditorShipModified gets called whenever a part is added or removed from the vessel being built.
            //It will be called when the vessel is loaded as well. We want to watch for vessel modifications when
            //the player loads a vessel from an editor bay; if the vessel changes before it is launched, then
            //any vessel integration progress is void. Fortunately the player can just re-load from the editor bay.
            //We also add the integration bonus at this time.
            int integrationPerPart = editorBayItem.totalIntegrationAdded / editorBayItem.breakablePartCount;
            bool partCountMismatch = EditorLogic.fetch.ship.parts.Count != editorBayItem.totalVesselParts;

            //Set integration bonus & Flight Experience
            //If the vessel has been tampered with then integration has failed.
            foreach (Part part in shipParts)
            {
                qualityControl = part.FindModuleImplementing<ModuleQualityControl>();
                if (qualityControl != null)
                {
                    if (partCountMismatch)
                        qualityControl.integrationBonus = 0;

                    else
                        qualityControl.integrationBonus = integrationPerPart;

                    //Set flight experience
                    qualityControl.flightExperienceBonus = BARISScenario.Instance.GetFlightBonus(qualityControl.part);

                    //Recalculate the quality.
                    qualityControl.currentQuality = qualityControl.GetMaxQuality();
                    debugLog(qualityControl.part.partInfo.title + " new quality: " + qualityControl.currentQuality);
                }
            }

            //If the vessel has been tamepred with then inform the player
            if (partCountMismatch && !errorMessageShown)
            {
                //Inform player
                errorMessageShown = true;
                messageView.WindowTitle = Localizer.Format(BARISScenario.VesselModifiedTitle);
                messageView.message = "<Color=yellow><b>" + Localizer.Format(BARISScenario.VesselModifiedMsg) + "</b></color>";
                messageView.dialogDismissedDelegate = dialogDismissed;
                messageView.SetVisible(true);
                BARISScenario.Instance.PlayProblemSound();
            }

            //Vessel is good, update the save file.
            else
            {
                ConfigNode shipNode = ship.SaveShip();
                shipNode.Save(editorBayItem.vesselFilePath);
            }
        }

        protected void dialogDismissed()
        {
            errorMessageShown = false;
        }

        public void launchVessel()
        {
            debugLog("launchVessel called");

            //If we have an editor bay item to process then transfer it to the revert list.
            //That way, if the player reverts the flight, we can rebuild the list.
            if (editorBayItem != null)
            {
                BARISScenario.Instance.TranferBayToRevertList(editorBayItem);
                BARISScenario.launchedVesselBay = editorBayItem;
            }

            //We're not launching a vessel from an editor bay, so clear the revert list.
            else if (BARISSettings.PartsCanBreak && BARISSettingsLaunch.LaunchesCanFail)
            {
                //Check for vehicle integration
                ShipConstruct ship = EditorLogic.fetch.ship;
                Part[] shipParts = ship.parts.ToArray();
                ModuleQualityControl qualityControl;

                foreach (Part part in shipParts)
                {
                    qualityControl = part.FindModuleImplementing<ModuleQualityControl>();
                    if (qualityControl != null)
                    {
                        //If the vessel hasn't been integrated then set the warning flag.
                        if (qualityControl.integrationBonus == 0)
                        {
                            BARISLaunchFailManager.vehicleNotIntegrated = true;
                            break;
                        }
                    }
                }

                BARISScenario.Instance.ClearRevertList();
            }

            else
            {
                BARISScenario.Instance.ClearRevertList();
            }

            //Cleanup
            GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.BACKUP);
            editorBayItem = null;
        }
    }
}
