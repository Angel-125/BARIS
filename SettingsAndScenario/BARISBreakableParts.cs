using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2017, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class BARISBreakableParts : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("Should commandable parts fail?", toolTip = "If unchecked, command pods, cockpits, and probe cores will never fail.", autoPersistance = true)]
        public bool commandPodsCanFail = true;

        [GameParameters.CustomParameterUI("Should parts with crew capacity fail?", toolTip = "If unchecked, a part with crew capacity will never fail.", autoPersistance = true)]
        public bool crewedPartsCanFail = true;

        [GameParameters.CustomParameterUI("Should converters fail?", toolTip = "Resource converters can break.", autoPersistance = true)]
        public bool convertersCanFail = true;

        [GameParameters.CustomParameterUI("Should drills fail?", toolTip = "Drills can brea.k", autoPersistance = true)]
        public bool drillsCanFail = true;

        [GameParameters.CustomParameterUI("Should engines fail?", toolTip = "Engines can breakdown.", autoPersistance = true)]
        public bool enginesCanFail = true;

        [GameParameters.CustomParameterUI("Should parts that hold resources fail?", toolTip = "Any part that stores resources can spring leaks.", autoPersistance = true)]
        public bool tanksCanFail = true;

        [GameParameters.CustomParameterUI("Should SAS and gyros fail?", toolTip = "Gyros can breakdown.", autoPersistance = true)]
        public bool sasCanFail = true;

        [GameParameters.CustomParameterUI("Should RCS fail?", toolTip = "RCS rockets can break.", autoPersistance = true)]
        public bool rcsCanFail = true;

        [GameParameters.CustomParameterUI("Should transmitters fail?", toolTip = "Transmitters can stop transmitting.", autoPersistance = true)]
        public bool transmittersCanFail = true;

        [GameParameters.CustomParameterUI("Can failed parts explode?", toolTip = "Failed parts can explode during launches or during post-launch critical failures.", autoPersistance = true)]
        public bool failuresCanExplode = false;

        [GameParameters.CustomIntParameterUI("Explosive potential (staging) %", maxValue = 100, minValue = 0, stepSize = 1, toolTip = "How likely will a staging failure cause an explosion.", autoPersistance = true)]
        public int explosivePotentialLaunches = 15;

        [GameParameters.CustomIntParameterUI("Explosive potential (critical fails) %", maxValue = 100, minValue = 0, stepSize = 1, toolTip = "How likely will a critical failure cause an explosion if the part is out of MTBF.", autoPersistance = true)]
        public int explosivePotentialCritical = 1;

        #region Properties
        public static bool CommandPodsCanFail
        {
            get
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                return settings.commandPodsCanFail;
            }

            set
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                settings.commandPodsCanFail = value;
            }
        }

        public static bool CrewedPartsCanFail
        {
            get
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                return settings.crewedPartsCanFail;
            }

            set
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                settings.crewedPartsCanFail = value;
            }
        }

        public static int ExplosivePotentialCritical
        {
            get
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                int potential = settings.explosivePotentialCritical;
                if (potential <= 0)
                    return 1000;
                return 100 - potential;
            }
        }
        public static int ExplosivePotentialLaunches
        {
            get
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                int potential = settings.explosivePotentialLaunches;
                if (potential <= 0)
                    return 1000;
                return 100 - potential;
            }
        }
        public static bool FailuresCanExplode
        {
            get
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                return settings.failuresCanExplode;
            }

            set
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                settings.failuresCanExplode = value;
            }
        }
        public static bool ConvertersCanFail
        {
            get
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                return settings.convertersCanFail;
            }

            set
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                settings.convertersCanFail = value;
            }
        }
        public static bool DrillsCanFail
        {
            get
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                return settings.drillsCanFail;
            }

            set
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                settings.drillsCanFail = value;
            }
        }
        public static bool EnginesCanFail
        {
            get
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                return settings.enginesCanFail;
            }

            set
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                settings.enginesCanFail = value;
            }
        }
        public static bool TanksCanFail
        {
            get
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                return settings.tanksCanFail;
            }

            set
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                settings.tanksCanFail = value;
            }
        }
        public static bool SASCanFail
        {
            get
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                return settings.sasCanFail;
            }

            set
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                settings.sasCanFail = value;
            }
        }
        public static bool RCSCanFail
        {
            get
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                return settings.rcsCanFail;
            }

            set
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                settings.rcsCanFail = value;
            }
        }
        public static bool TransmittersCanFail
        {
            get
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                return settings.transmittersCanFail;
            }

            set
            {
                BARISBreakableParts settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISBreakableParts>();
                settings.transmittersCanFail = value;
            }
        }
        #endregion

        #region CustomParameterNode
#if !KSP122
        public override string DisplaySection
        {
            get
            {
                return Section;
            }
        }
#endif

        public override string Section
        {
            get
            {
                return "BARIS";
            }
        }

        public override string Title
        {
            get
            {
                return "Breakable Parts";
            }
        }

        public override int SectionOrder
        {
            get
            {
                return 2;
            }
        }

        public override GameParameters.GameMode GameMode
        {
            get
            {
                return GameParameters.GameMode.ANY;
            }
        }

        public override bool HasPresets
        {
            get
            {
                return false;
            }
        }

        public override bool Enabled(System.Reflection.MemberInfo member, GameParameters parameters)
        {
            if (BARISSettings.PartsCanBreak)
            {
                if ((member.Name == "explosivePotentialCritical" || member.Name == "explosivePotentialLaunches") && !failuresCanExplode)
                    return false;

                return true;
            }
            else
                return false;
        }
        #endregion
    }
}
