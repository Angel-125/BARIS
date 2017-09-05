using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
using KSP.UI.Screens;
using ModuleWheels;
#if !KSP122
using KSP.Localization;
#endif

/*
Source code copyrighgt 2017, by Michael Billard (Angel-125)
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
    public class ModuleRobonautPackChute : PartModule
    {
        ModuleParachute parachute = null;

        [KSPEvent(guiName = "#autoLOC_6001350", guiActiveUnfocused = true, guiActive = false, externalToEVAOnly = false, unfocusedRange = 4f)]
        public void PackChute()
        {
            if (parachute == null)
                return;

            //Make sure that the active vessel has a robonaut.
            if (FlightGlobals.ActiveVessel.FindPartModuleImplementing<ModuleRobonaut>() == null)
            {
                ScreenMessages.PostScreenMessage(ModuleRobonaut.NoRobonautMsg, ModuleRobonaut.MessageDuration, ScreenMessageStyle.UPPER_CENTER);
                FlightLogger.fetch.LogEvent(ModuleRobonaut.NoRobonautMsg);
                return;
            }

            //Repack the chute.
            parachute.Repack();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            Events["PackChute"].active = parachute.deploymentState == ModuleParachute.deploymentStates.CUT && !FlightGlobals.ActiveVessel.isEVA;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            parachute = this.part.FindModuleImplementing<ModuleParachute>();
        }
    }
}
