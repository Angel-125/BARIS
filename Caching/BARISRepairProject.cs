using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
#if !KSP122
using KSP.Localization;
#endif

/*
Source code copyrighgt 2017-2019, by Michael Billard (Angel-125)
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
    public class BARISRepairProject
    {
        public string vesselID;
        public int repairCostTime;
        public float repairCostFunds;
        public float repairCostScience;
        public double startTime;
        public double elapsedTime;

        public float RepairProgress
        {
            get
            {
                return (float)(elapsedTime / (repairCostTime * BARISScenario.SecondsPerDay)) * 100.0f;
            }
        }

        public bool IsProgressComplete
        {
            get
            {
                if (elapsedTime >= (repairCostTime * BARISScenario.SecondsPerDay))
                    return true;
                else
                    return false;
            }
        }

        public ConfigNode Save()
        {
            ConfigNode node = new ConfigNode("BARISRepairProject");

            node.AddValue("vesselID", vesselID);
            node.AddValue("repairCostTime", repairCostTime);
            node.AddValue("repairCostFunds", repairCostFunds);
            node.AddValue("repairCostScience", repairCostScience);
            node.AddValue("startTime", startTime);
            node.AddValue("elapsedTime", elapsedTime);

            return node;
        }

        public void Load(ConfigNode node)
        {
            if (node.HasValue("vesselID"))
                vesselID = node.GetValue("vesselID");

            if (node.HasValue("repairCostTime"))
                repairCostTime = int.Parse(node.GetValue("repairCostTime"));

            if (node.HasValue("repairCostFunds"))
                repairCostFunds = float.Parse(node.GetValue("repairCostFunds"));

            if (node.HasValue("repairCostScience"))
                repairCostScience = float.Parse(node.GetValue("repairCostScience"));

            if (node.HasValue("startTime"))
                startTime = double.Parse(node.GetValue("startTime"));

            if (node.HasValue("elapsedTime"))
                elapsedTime = double.Parse(node.GetValue("elapsedTime"));
        }
    }
}
