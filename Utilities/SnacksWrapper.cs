/*
Source code copyrighgt 2019, by Michael Billard(Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using System.Reflection;

namespace WildBlueIndustries
{
    public enum BARISStressCategories
    {
        stressCategoryLow = 1,
        stressCategoryMedium = 2,
        stressCategoryHigh = 4
    }

    /// <summary>
    /// This is a wrapper class designed to facilitate interactions between BARIS and Snacks.
    /// In a nutshell, part failures cause stress (If Stress is enabled). stating failures cause more Stress,
    /// and vehicle failures cause a LOT of Stress.
    /// </summary>
    public class SnacksWrapper
    {
        #region Wrapper Methods
        static Assembly snacksAssembly;
        static Type scenarioType;
        static MethodInfo miAddStress;
        #endregion

        #region Housekeeping
        public float minStressPerCategory = 0.1f;
        public float maxStressPerCategory = 1.25f;
        #endregion

        #region Constructors
        public SnacksWrapper()
        {
            if (snacksAssembly == null)
            {
                foreach (AssemblyLoader.LoadedAssembly loadedAssembly in AssemblyLoader.loadedAssemblies)
                {
                    if (loadedAssembly.name == "SnacksUtils")
                    {
                        snacksAssembly = loadedAssembly.assembly;
                        break;
                    }
                }

                if (snacksAssembly == null)
                    return;

                //Init methods
                scenarioType = snacksAssembly.GetTypes().First(t => t.Name.Equals("SnacksScenario"));
                miAddStress = scenarioType.GetMethods().First(t => t.Name.Equals("AddStressToCrew"));
            }
        }

        /// <summary>
        /// Adds the stress to crew if Stress is enabled. This is primarily
        /// used by 3rd party mods like BARIS.
        /// </summary>
        /// <param name="vessel">The Vessel to query for crew.</param>
        /// <param name="stressAmount">The amount of Stress to add.</param>
        public void AddStressToCrew(Vessel vessel, float stressAmount)
        {
            if (snacksAssembly != null)
            {
                miAddStress.Invoke(null, new object[] { vessel, stressAmount });
            }
        }

        /// <summary>
        /// Adds stress to the crew based on the category of stress. If Snacks isn't installed, then the method just returns.
        /// </summary>
        /// <param name="vessel">The vessel whose crew is going to receive the stress.</param>
        /// <param name="stressCategory">The category of stress to add.</param>
        public void AddStressToCrew(Vessel vessel, BARISStressCategories stressCategory)
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;
            if (snacksAssembly != null)
            {
                //Get random stress amount.
                float stressAmount = UnityEngine.Random.Range(minStressPerCategory, maxStressPerCategory) * (float)stressCategory;

                //Now add to vessel crew
                AddStressToCrew(vessel, stressAmount);
            }
        }
        #endregion
    }
}
