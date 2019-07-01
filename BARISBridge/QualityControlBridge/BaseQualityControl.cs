using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
using KSP.UI.Screens;
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
    public delegate void OnPartBrokenEvent(BaseQualityControl qualityControl);
    public delegate void OnPartFixedEvent(BaseQualityControl qualityControl);
    public delegate void OnUpdateSettingsEvent(BaseQualityControl qualityControl);
    public delegate void OnMothballStateChangedEvent(bool isMothballed);

    /// <summary>
    /// This is a stub class designed to create a bridge between BARIS and mods that use BARIS. It also serves as the base class for ModuleQualityControl. It is part of the BARISBridge plugin.
    /// </summary>
    public class BaseQualityControl : PartModule
    {
        //Events
        /// <summary>
        /// Fired when the part is declared broken. Individual breakable part modules also receive this state change through the ICanBreak interface.
        /// </summary>
        public event OnPartBrokenEvent onPartBroken;

        /// <summary>
        /// FIred when the part is declared fixed. Individual breakable part modules also receive this state change through the ICanBreak interface.
        /// </summary>
        public event OnPartFixedEvent onPartFixed;

        /// <summary>
        /// Fired when any of the difficulty settings have been changed.
        /// </summary>
        public event OnUpdateSettingsEvent onUpdateSettings;

        /// <summary>
        /// Fired when the mothball state changes.
        /// </summary>
        public event OnMothballStateChangedEvent onMothballStateChanged;

        /// <summary>
        /// Human readable quality display. Broken (0), Poor (1-24), Fair (25-49), Good (50-74), Excellent (75-100)
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Condition")]
        public string qualityDisplay = string.Empty;

        /// <summary>
        /// Field to indicate whether or not the part is mothballed.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isMothballed;

        public virtual void SetMothballState(bool mothballState)
        {
            //Set the flag
            isMothballed = mothballState;

            //Update breakable parts
            if (onMothballStateChanged != null)
                onMothballStateChanged(isMothballed);
        }

        public virtual void GetQualityStats(out int Quality, out int CurrentQuality, out double MTBF, out double CurrentMTBF)
        {
            Quality = 0;
            CurrentQuality = 0;
            MTBF = 0.0;
            CurrentMTBF = 0.0;
        }

        public virtual void PerformMaintenance()
        {
        }

        public virtual string GetRepairCost()
        {
            return string.Empty;
        }

        public virtual void RepairPart()
        {
        }

        public virtual void UpdateActivationState()
        {
        }

        public virtual void PerformQualityCheck()
        {
        }

        public virtual void UpdateQualityDisplay(string displayString)
        {
        }

        public virtual void DeclarePartBroken()
        {
            FireOnPartBroken();
        }

        public virtual void FireOnUpdateSettings()
        {
            if (onUpdateSettings != null)
                onUpdateSettings(this);
        }

        public virtual void FireOnPartFixed()
        {
            onPartFixed?.Invoke(this);
        }

        public virtual void FireOnPartBroken()
        {
            onPartBroken?.Invoke(this);
        }
    }
}
