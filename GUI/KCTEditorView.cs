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
    public class KCTEditorView : Dialog<KCTEditorView>
    {
        const int DialogWidth = 300;
        const int DialogHeight = 50;

        private ModuleQualityControl[] qualityControlModules;

        public KCTEditorView() :
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
                GameEvents.onEditorShipModified.Add(onEditorShipModified);
                onEditorShipModified(EditorLogic.fetch.ship);
            }

            else
            {
                GameEvents.onEditorShipModified.Remove(onEditorShipModified);
            }
        }

        public void OnDestroy()
        {
            GameEvents.onEditorShipModified.Remove(onEditorShipModified);
        }

        protected void onEditorShipModified(ShipConstruct ship)
        {
            //Get the breakable parts.
            qualityControlModules = null;
            List<ModuleQualityControl> modules = new List<ModuleQualityControl>();
            ModuleQualityControl qualityControl;
            foreach (Part part in ship.parts)
            {
                qualityControl = part.FindModuleImplementing<ModuleQualityControl>();
                if (qualityControl != null)
                    modules.Add(qualityControl);
            }

            if (modules.Count > 0)
            {
                qualityControlModules = modules.ToArray();
            }
        }

        protected override void DrawWindowContents(int windowId)
        {
            if (qualityControlModules == null || qualityControlModules.Length == 0)
            {
                GUILayout.Label("<color=yellow><b>" + Localizer.Format(BARISScenario.KCTNoPartsLabel) + "</b></color>");
                return;
            }

            int totalMaxReliability = 100 * qualityControlModules.Length;
            int totalQuality = 0;

            //Calculate total quality
            for (int index = 0; index < qualityControlModules.Length; index++)
                totalQuality += qualityControlModules[index].GetMaxQuality();

            int reliability = Mathf.RoundToInt(((float)(totalQuality) / (float)totalMaxReliability) * 100.0f);
            if (reliability < 0)
                reliability = 0;

            GUILayout.Label("<color=white><b>" + Localizer.Format(BARISScenario.KCTReliabilityLabel) + "</b>" + reliability + "</color>");
        }
    }
}
