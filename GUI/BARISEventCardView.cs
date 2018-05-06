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
    internal class BARISEventCardView : Dialog<BARISEventCardView>
    {
        const int DialogWidth = 275;
        const int DialogHeight = 365;

        public string description = string.Empty;
        public string imagePath = string.Empty;

        private Texture2D image = null;
        private GUILayoutOption[] scrollViewOptions = new GUILayoutOption[] { GUILayout.Width(DialogWidth) };

        public BARISEventCardView() :
        base("Blah", DialogWidth, DialogHeight)
        {
            Resizable = false;
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            //Theme song
            if (BARISScenario.eventCardSong != null)
            {
                if (newValue)
                    BARISScenario.eventCardSong.Play();
                else if (BARISScenario.eventCardSong.isPlaying)
                    BARISScenario.eventCardSong.Stop();
            }

            //Load the custom image if we have one. Otherwise just use the app icon.
            if (!string.IsNullOrEmpty(imagePath))
            {
                image = GameDatabase.Instance.GetTexture(imagePath, false);
                if (image == null)
                    image = BARISAppButton.appIcon;
            }

            else
            {
                image = BARISAppButton.appIcon;
            }
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();

            //Image
            if (image != null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(image);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }

            //Description
            GUILayout.Label(description);

            GUILayout.EndVertical();
        }
    }
}
