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
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class BARISTestBenchButton : MonoBehaviour
    {
        static public Texture2D appIcon = null;
        static protected ApplicationLauncherButton appLauncherButton = null;
        static TestBenchView testBenchView = new TestBenchView();

        public void Awake()
        {
            appIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/000BARIS/Icons/TestBenchIcon", false);
            GameEvents.onGUIApplicationLauncherReady.Add(SetupGUI);
        }

        public void Destroy()
        {
            testBenchView.SetVisible(false);
        }

        private void SetupGUI()
        {

            if (HighLogic.LoadedSceneIsEditor && BARISSettings.PartsCanBreak && (HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX || HighLogic.CurrentGame.Mode == Game.Modes.CAREER))
            {
                if (appLauncherButton == null)
                {
                    appLauncherButton = ApplicationLauncher.Instance.AddModApplication(ToggleGUI, ToggleGUI, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, appIcon);
                }
            }
            else if (appLauncherButton != null)
                ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
        }

        private void ToggleGUI()
        {
            testBenchView.SetVisible(!testBenchView.IsVisible());

            //Pay with Science tip
            if (!BARISScenario.showedBuyExperienceWithScienceTip)
            {
                BARISScenario.showedBuyExperienceWithScienceTip = true;
                BARISEventCardView cardView = new BARISEventCardView();

                cardView.WindowTitle = BARISScenario.BuyExpWithScienceTitle;
                cardView.description = BARISScenario.BuyExpWithScienceMsg;
                cardView.imagePath = BARISScenario.BuyExpWithScienceToolTipImagePath;

                cardView.SetVisible(true);
                GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
            }             
        }
    }
}
