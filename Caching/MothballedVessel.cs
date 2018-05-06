using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
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
    public class MothballedVessel
    {
        public static double kReactivateSecondsPerTonne = 21600;
        public string vesselName;
        public double reactivationTimeRemaining = 0;
        public double lastUpdated = 0;

        public MothballedVessel()
        {
        }

        public MothballedVessel(Vessel vessel)
        {
            vesselName = vessel.vesselName;

            //Calcuate reactivation time remaining: equal to six hours per metric ton of vessel.
            reactivationTimeRemaining = vessel.GetTotalMass() * kReactivateSecondsPerTonne;

            lastUpdated = Planetarium.GetUniversalTime();
        }

        public void UpdateTimer()
        {
            if (reactivationTimeRemaining == 0)
                return;

            double elapsedTime = Planetarium.GetUniversalTime() - lastUpdated;

            reactivationTimeRemaining -= elapsedTime;
            if (reactivationTimeRemaining <= 0)
                reactivationTimeRemaining = 0;

            lastUpdated = Planetarium.GetUniversalTime();
        }

        public void Load(ConfigNode node)
        {
            if (node.HasValue("vesselName"))
                vesselName = node.GetValue("vesselName");
            if (node.HasValue("lastUpdated"))
                double.TryParse(node.GetValue("lastUpdated"), out lastUpdated);
            if (node.HasValue("reactivationTimeRemaining"))
                double.TryParse(node.GetValue("reactivationTimeRemaining"), out reactivationTimeRemaining);
        }

        public ConfigNode Save()
        {
            ConfigNode node = new ConfigNode("MothballedVessel");

            node.AddValue("vessselName", vesselName);
            node.AddValue("lastUpdated", lastUpdated);
            node.AddValue("reactivationTimeRemaining", reactivationTimeRemaining);

            return node;
        }
    }
}
