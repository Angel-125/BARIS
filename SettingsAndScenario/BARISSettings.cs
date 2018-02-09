using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2018, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public enum BARISDifficultyModifiers
    {
        SuperEasy,
        VeryEasy,
        Easy,
        Normal,
        Hard,
        VeryHard,
        HardCore
    }

    public class BARISSettings : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("Parts can break", toolTip = "If enabled, parts can break.", autoPersistance = true)]
        public bool partsCanBreak = false;

        [GameParameters.CustomIntParameterUI("Quality Cap", maxValue = 100, minValue = 50, stepSize = 5, toolTip = "Max quality that a part can have.", autoPersistance = true)]
        public int qualityCap = 80;

        [GameParameters.CustomIntParameterUI("Difficulty Modifier", toolTip = "Make quality checks easier or harder.", autoPersistance = true)]
        public BARISDifficultyModifiers difficulty = BARISDifficultyModifiers.Normal;

        [GameParameters.CustomParameterUI("Parts wear out", toolTip = "If enabled, parts eventually stop working.", autoPersistance = true)]
        public bool partsWearOut = true;

        [GameParameters.CustomIntParameterUI("Quality checks per day", maxValue = 6, minValue = 1, stepSize = 1, toolTip = "How often to make a quality check.", autoPersistance = true)]
        public int checksPerDay = 3;

        [GameParameters.CustomParameterUI("Maintenance and repairs require skill.", toolTip = "If enabled, you need skills to maintain or repair parts.", autoPersistance = true)]
        public bool repairsRequireSkill = true;

        [GameParameters.CustomParameterUI("Maintenance and repairs require resources", toolTip = "If enabled, you need resources to maintain or repair parts.", autoPersistance = true)]
        public bool repairsRequireResources = true;

        [GameParameters.CustomParameterUI("Maintenance and repairs require EVA", toolTip = "If enabled, you need to go outside to fix stuff.", autoPersistance = true)]
        public bool repairsRequireEVA = true;

        [GameParameters.CustomParameterUI("Email maintenance requests", toolTip = "Send an in-game email when parts need maintenance.", autoPersistance = true)]
        public bool emailMaintenanceRequests = true;

        [GameParameters.CustomParameterUI("Email repair requests", toolTip = "Send an in-game email when parts need repairs.", autoPersistance = true)]
        public bool emailRepairRequests = true;

        [GameParameters.CustomParameterUI("Kill Timewarp when parts break", toolTip = "If a part suffers a break during timewarp, kill the timewarp.", autoPersistance = true)]
        public bool killTimewarpOnBreak = true;

        [GameParameters.CustomParameterUI("Debug mode enabled", toolTip = "Lots of logging and debug options.", autoPersistance = true)]
        public bool debugMode = false;

        #region Properties
        public static bool KillTimewarpOnBreak
        {
            get
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                return settings.killTimewarpOnBreak;
            }

            set
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                settings.killTimewarpOnBreak = value;
            }
        }

        public static bool DebugMode
        {
            get
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                return settings.debugMode;
            }

            set
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                settings.debugMode = value;
            }
        }

        public static int ChecksPerDay
        {
            get
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                return settings.checksPerDay;
            }

            set
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                settings.checksPerDay = value;
            }
        }
        
        public static int QualityCap
        {
            get
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                return settings.qualityCap;
            }

            set
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                settings.qualityCap = value;
            }
        }

        public static bool EmailMaintenanceRequests
        {
            get
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                return settings.emailMaintenanceRequests;
            }

            set
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                settings.emailMaintenanceRequests = value;
            }
        }

        public static bool EmailRepairRequests
        {
            get
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                return settings.emailRepairRequests;
            }

            set
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                settings.emailRepairRequests = value;
            }
        }

        public static int DifficultyModifier
        {
            get
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                int modifier = 0;
                switch (settings.difficulty)
                {
                    case BARISDifficultyModifiers.Normal:
                        modifier = 0;
                        break;

                    case BARISDifficultyModifiers.SuperEasy:
                        modifier = 25;
                        break;

                    case BARISDifficultyModifiers.VeryEasy:
                        modifier = 15;
                        break;

                    case BARISDifficultyModifiers.Easy:
                        modifier = 10;
                        break;

                    case BARISDifficultyModifiers.Hard:
                        modifier = -10;
                        break;

                    case BARISDifficultyModifiers.VeryHard:
                        modifier = -15;
                        break;

                    case BARISDifficultyModifiers.HardCore:
                        modifier = -25;
                        break;
                }
                return modifier;
            }
        }

        public static bool PartsWearOut
        {
            get
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                return settings.partsWearOut;
            }

            set
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                settings.partsWearOut = value;
            }
        }

        public static bool RepairsRequireEVA
        {
            get
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                return settings.repairsRequireEVA;
            }

            set
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                settings.repairsRequireEVA = value;
            }
        }

        public static bool RepairsRequireSkill
        {
            get
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                return settings.repairsRequireSkill;
            }

            set
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                settings.repairsRequireSkill = value;
            }
        }

        public static bool PartsCanBreak
        {
            get
            {
                if (HighLogic.LoadedScene == GameScenes.MAINMENU)
                    return true;
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                return settings.partsCanBreak;
            }

            set
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                settings.partsCanBreak = value;
            }
        }

        public static bool RepairsRequireResources
        {
            get
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                return settings.repairsRequireResources;
            }

            set
            {
                BARISSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BARISSettings>();
                settings.repairsRequireResources = value;
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
                return "Wear & Tear";
            }
        }

        public override int SectionOrder
        {
            get
            {
                return 1;
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
            if (partsCanBreak || member.Name == "partsCanBreak")
                return true;
            else
                return false;
        }

        #endregion
    }
}
