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
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class BARISAppButton : MonoBehaviour
    {
        static public BARISAppFlightView flightAppView;
        static public BARISAppTrackingView trackingAppView;
        static public BARISAppCenterView centerAppView;
        static public BARISInstructions instructionsView;
        static public BARISEditorView editorView;
        static public VehicleIntegrationStatusView vehicleIntegrationStatusView;
        static public KCTEditorView kctEditorView;
        static public Texture2D appIcon = null;

        static protected ApplicationLauncherButton appLauncherButton = null;

        public void Awake()
        {
            appIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/000BARIS/BARISIcon", false);
            GameEvents.onGUIApplicationLauncherReady.Add(SetupGUI);
            flightAppView = new BARISAppFlightView();
            trackingAppView = new BARISAppTrackingView();
            centerAppView = new BARISAppCenterView();
            instructionsView = new BARISInstructions();
            editorView = new BARISEditorView();
            vehicleIntegrationStatusView = new VehicleIntegrationStatusView();
            kctEditorView = new KCTEditorView();
        }

        public void OnDestroy()
        {
            switch (HighLogic.LoadedScene)
            {
                case GameScenes.SPACECENTER:
                    if (centerAppView.IsVisible())
                        centerAppView.SetVisible(false);
                    break;

                case GameScenes.FLIGHT:
                    if (flightAppView.IsVisible())
                        flightAppView.SetVisible(false);
                    break;

                case GameScenes.TRACKSTATION:
                    if (trackingAppView.IsVisible())
                        trackingAppView.SetVisible(false);
                    break;

                case GameScenes.EDITOR:
                    if (editorView.IsVisible())
                        editorView.SetVisible(false);
                    if (kctEditorView.IsVisible())
                        kctEditorView.SetVisible(false);
                    break;

                default:
                    break;
            }
        }

        private void SetupGUI()
        {
            if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedSceneIsEditor || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                if (appLauncherButton == null)
                {
                    if (HighLogic.LoadedScene == GameScenes.SPACECENTER || BARISSettings.PartsCanBreak)
                        appLauncherButton = ApplicationLauncher.Instance.AddModApplication(ToggleGUI, ToggleGUI, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, appIcon);
                }
            }
            else if (appLauncherButton != null)
                ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
        }

        private void ToggleGUI()
        {
            if (!BARISSettings.PartsCanBreak)
            {
                BARISScenario.Instance.LogPlayerMessage(Localizer.Format(BARISScenario.BARISDisabledMsg));
                return;
            }

            switch (HighLogic.LoadedScene)
            {
                case GameScenes.SPACECENTER:
                    centerAppView.SetVisible(!centerAppView.IsVisible());
                    break;

                case GameScenes.FLIGHT:
                    flightAppView.SetVisible(!flightAppView.IsVisible());
                    break;

                case GameScenes.TRACKSTATION:
                    trackingAppView.SetVisible(!trackingAppView.IsVisible());
                    break;

                case GameScenes.EDITOR:
                    if (BARISScenario.isKCTInstalled)
                    {
                        Debug.Log("FRED ToggleGUI-Editor button clicked. isKCTInstalled: " + BARISScenario.isKCTInstalled);
                        kctEditorView.SetVisible(!kctEditorView.IsVisible());
                        return;
                    }
                    editorView.SetVisible(!editorView.IsVisible());
                    break;

                default:
                    break;
            }
        }
    }
}
