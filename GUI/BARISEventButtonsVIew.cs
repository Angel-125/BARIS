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
    internal class BARISEventButtonsView : Dialog<BARISEventButtonsView>
    {
        const int DialogWidth = 350;
        const int DialogHeight = 600;

        private Vector2 scrollPos;
        private GUILayoutOption[] scrollViewOptions = new GUILayoutOption[] { GUILayout.Width(DialogWidth) };
        private BARISEventCard[] eventCards = null;

        public BARISEventButtonsView() :
        base("Blah", DialogWidth, DialogHeight)
        {
            WindowTitle = "Play An Event Card";
            Resizable = false;
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            eventCards = BARISScenario.eventCardDeck.ToArray();
            Debug.Log("[BARISEventButtonsView] - eventCards count: " + eventCards.Length);
            Debug.Log("[BARISEventButtonsView] - eventCardDeck count: " + BARISScenario.eventCardDeck.Count);

        }

        protected override void DrawWindowContents(int windowId)
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos, scrollViewOptions);

            for (int index = 0; index < eventCards.Length; index++)
            {
                if (GUILayout.Button(eventCards[index].name))
                {
                    BARISScenario.Instance.PlayCard(eventCards[index]);
                }
            }

            GUILayout.EndScrollView();
        }
    }
}
