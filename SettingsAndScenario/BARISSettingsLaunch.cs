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
    public class BARISSettingsLaunch : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("Enable Launch Failures", toolTip = "Launches can fail beyond user error.", autoPersistance = true)]
        public bool launchesCanFail = true;

        [GameParameters.CustomParameterUI("Vessels need integration", toolTip = "Vessels require time to improve their reliability.", autoPersistance = true)]
        public bool vesselsNeedIntegration = true;

        [GameParameters.CustomIntParameterUI("Flights per part quality bonus", maxValue = 20, minValue = 1, stepSize = 1, toolTip = "How many launches does it take to gain a quality bonus.", autoPersistance = true)]
        public int flightsPerQualityBonus = 5;

        [GameParameters.CustomIntParameterUI("Experience cost modifier", maxValue = 20, minValue = 1, stepSize = 1, toolTip = "How much it costs for a part to gain 1 point of Flight Experience.", autoPersistance = true)]
        public int multiplierPerExpBonus = 3;

        [GameParameters.CustomIntParameterUI("% chance fails gain experience", maxValue = 100, minValue = 1, stepSize = 1, toolTip = "How likely a part will gain flight experience when staging fails.", autoPersistance = true)]
        public int stagingFailExpChance = 40;

        [GameParameters.CustomParameterUI("Workers cost money", toolTip = "Workers cost money to employ.", autoPersistance = true)]
        public bool workersCostFunds = true;

        [GameParameters.CustomParameterUI("Astronauts cost money", toolTip = "Astronauts cost money to employ.", autoPersistance = true)]
        public bool astronautsCostFunds = true;

        [GameParameters.CustomParameterUI("Enable Event Cards", toolTip = "Random events can affect your space program.", autoPersistance = true)]
        public bool eventCardsEnabled = true;

        [GameParameters.CustomIntParameterUI("Days Between Events", maxValue = 288, minValue = 6, stepSize = 6, toolTip = "How much is it gonna cost", autoPersistance = true)]
        public int eventCardFrequency = 18;

        [GameParameters.CustomParameterUI("Astronauts can die in training", toolTip = "Astronauts can die during training.", autoPersistance = true)]
        public bool astronautsCanBeKilled = true;

        [GameParameters.CustomParameterUI("Facilities can be destroyed", toolTip = "Facilities can be destroyed from accidents or sabotage.", autoPersistance = true)]
        public bool facilitiesCanBeDestroyed = true;

        [GameParameters.CustomParameterUI("Vehicle builds can be lost", toolTip = "Vessels in integration can be destroyed.", autoPersistance = true)]
        public bool vehicleBuildsCanBeLost = true;

        #region Properties
        public static float MultiplierPerExpBonus
        {
            get
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                return settings.multiplierPerExpBonus;
            }
        }

        public static int StagingFailExpTarget
        {
            get
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                return 100 - settings.stagingFailExpChance;
            }
        }

        public static bool AstronautsCanBeKilled
        {
            get
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                return settings.astronautsCanBeKilled;
            }

            set
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                settings.astronautsCanBeKilled = value;
            }
        }

        public static bool FacilitiesCanBeDestroyed
        {
            get
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                return settings.facilitiesCanBeDestroyed;
            }

            set
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                settings.facilitiesCanBeDestroyed = value;
            }
        }

        public static bool VehicleBuildsCanBeLost
        {
            get
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                return settings.vehicleBuildsCanBeLost;
            }

            set
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                settings.vehicleBuildsCanBeLost = value;
            }
        }

        public static int EventCardFrequency
        {
            get
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                return settings.eventCardFrequency;
            }

            set
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                settings.eventCardFrequency = value;
            }
        }

        public static bool EventCardsEnabled
        {
            get
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                return settings.eventCardsEnabled;
            }

            set
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                settings.eventCardsEnabled = value;
            }
        }
        
        public static bool VesselsNeedIntegration
        {
            get
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                return settings.vesselsNeedIntegration;
            }

            set
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                settings.vesselsNeedIntegration = value;
            }
        }

        public static bool AstronautsCostFunds
        {
            get
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                return settings.astronautsCostFunds;
            }

            set
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                settings.astronautsCostFunds = value;
            }
        }
        
        public static bool WorkersCostFunds
        {
            get
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                return settings.workersCostFunds;
            }

            set
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                settings.workersCostFunds = value;
            }
        }

        public static int FlightsPerQualityBonus
        {
            get
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                return settings.flightsPerQualityBonus;
            }

            set
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                settings.flightsPerQualityBonus = value;
            }
        }

        public static bool LaunchesCanFail
        {
            get
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                return settings.launchesCanFail;
            }

            set
            {
                BARISSettingsLaunch settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettingsLaunch>();
                settings.launchesCanFail = value;
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
                return "Launches, Construction & Events";
            }
        }

        public override int SectionOrder
        {
            get
            {
                return 3;
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
            if (!BARISSettings.PartsCanBreak)
                return false;

            //Event cards
            if (member.Name == "eventCardsEnabled")
                return true;

            if (member.Name == "eventCardFrequency" && eventCardsEnabled)
                return true;
            else if (member.Name == "eventCardFrequency")
                return false;

            if (member.Name == "astronautsCanBeKilled" && eventCardsEnabled)
                return true;
            else if (member.Name == "astronautsCanBeKilled")
                return false;

            if (member.Name == "facilitiesCanBeDestroyed" && eventCardsEnabled)
                return true;
            else if (member.Name == "facilitiesCanBeDestroyed")
                return false;

            if (member.Name == "vehicleBuildsCanBeLost" && eventCardsEnabled)
                return true;
            else if (member.Name == "vehicleBuildsCanBeLost")
                return false;

            //Launch failures
            if (member.Name == "launchesCanFail" || launchesCanFail)
                return true;
            else if (member.Name == "launchesCanFail")
                return false;

            return true;
        }
        #endregion
    }
}
