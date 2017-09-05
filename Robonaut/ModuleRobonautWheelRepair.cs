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
    public class ModuleRobonautWheelRepair : PartModule
    {
        ModuleWheelDamage wheelDamage = null;

        [KSPEvent(guiActive = true, guiName = "Debug: Break Wheel")]
        public void BreakWheel()
        {
            wheelDamage.DamageWheel();
        }

        [KSPEvent(guiName = "#autoLOC_6001882", guiActiveUnfocused = true, guiActive = false, externalToEVAOnly = false, unfocusedRange = 4f)]
        public void RepairWheel()
        {
            if (wheelDamage == null)
                return;
            if (wheelDamage.isDamaged == false)
                return;

            //Make sure that the active vessel has a robonaut.
            if (FlightGlobals.ActiveVessel.FindPartModuleImplementing<ModuleRobonaut>() == null)
            {
                ScreenMessages.PostScreenMessage(ModuleRobonaut.NoRobonautMsg, ModuleRobonaut.MessageDuration, ScreenMessageStyle.UPPER_CENTER);
                FlightLogger.fetch.LogEvent(ModuleRobonaut.NoRobonautMsg);
                return;
            }

            //Trigger the wheel repair event.
            wheelDamage.SetDamaged(false);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            if (wheelDamage == null)
                wheelDamage = this.part.FindModuleImplementing<ModuleWheelDamage>();
            if (wheelDamage == null)
                return;

            Events["RepairWheel"].active = wheelDamage.isDamaged && !FlightGlobals.ActiveVessel.isEVA;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Events["BreakWheel"].active = ModuleRobonaut.showDebug;

            wheelDamage = this.part.FindModuleImplementing<ModuleWheelDamage>();
        }
    }
}
