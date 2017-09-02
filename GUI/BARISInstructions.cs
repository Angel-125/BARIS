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
    public class BARISInstructions : Dialog<BARISInstructions>
    {
        const int DialogWidth = 650;
        const int DialogHeight = 630;
        const int InfoPanelHeight = 60;

        private GUILayoutOption[] scrollViewOptions = new GUILayoutOption[] { GUILayout.Width(DialogWidth) };
        private Vector2 scrollPos;
        private static Texture imgInstructions1 = null;
        private static Texture imgInstructions2 = null;
        private static Texture imgInstructions3 = null;
        private static Texture imgInstructions4 = null;
        private static Texture kspediaIcon = null;
        private static Texture imgBarisIsOff = null;
        private GUIStyle guiStyle = new GUIStyle();

        public BARISInstructions() :
        base("Blah", DialogWidth, DialogHeight)
        {
            WindowTitle = Localizer.Format(BARISScenario.InstructionsTitle);
            Resizable = false;
            guiStyle.fontSize = 36;
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            if (newValue)
            {
                if (imgInstructions1 == null)
                {
                    string filePath = "WildBlueIndustries/000BARIS/Images/";

                    imgInstructions1 = GameDatabase.Instance.GetTexture(filePath + "Instructions1", false);
                    imgInstructions2 = GameDatabase.Instance.GetTexture(filePath + "Instructions2", false);
                    imgInstructions3 = GameDatabase.Instance.GetTexture(filePath + "Instructions3", false);
                    imgInstructions4 = GameDatabase.Instance.GetTexture(filePath + "Instructions4", false);
                    kspediaIcon = GameDatabase.Instance.GetTexture(filePath + "kspediaIcon", false);
                    imgBarisIsOff = GameDatabase.Instance.GetTexture(filePath + "BARISIsOff", false);

                    BARISScenario.Instance.PlayProblemSound();
                    BARISScenario.Instance.PlayThemeSong();
                }
            }

            else
            {
                if (BARISScenario.themeSong.isPlaying)
                {
                    BARISScenario.themeSong.Stop();
                }

                //RTFM Hint
                BARISEventCardView cardView = new BARISEventCardView();

                cardView.WindowTitle = BARISScenario.RTFMTitle;
                cardView.description = BARISScenario.RTFMMsg;
                cardView.imagePath = BARISScenario.RTFMToolTipImagePath;

                cardView.SetVisible(true);
            }
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();

            scrollPos = GUILayout.BeginScrollView(scrollPos, scrollViewOptions);

            GUILayout.Label(Localizer.Format(BARISScenario.InstructionsWelcome));

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(imgBarisIsOff);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Label(Localizer.Format(BARISScenario.InstructionsWelcome1));
            GUILayout.Label(kspediaIcon);
            GUILayout.Label(Localizer.Format(BARISScenario.InstructionsWelcome3));

            GUILayout.Label(Localizer.Format(BARISScenario.Instructions1));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(imgInstructions1);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Label(Localizer.Format(BARISScenario.Instructions2));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(imgInstructions2);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Label(Localizer.Format(BARISScenario.Instructions3));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(imgInstructions3);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Label(Localizer.Format(BARISScenario.Instructions4));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(imgInstructions4);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }
    }
}
