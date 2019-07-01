using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WildBlueIndustries
{
    public delegate void PlayModeUpdateDelegate(bool partsCanBreak, bool repairsRequireResources);

    /// <summary>
    /// The ICanBreak interface is used by ModuleQualityControl to determine which part modules in the configuration support breakable part modules.
    /// All the ModuleBreakableXXX part modules in BARIS implement this interface.
    /// </summary>
    public interface ICanBreak
    {
        /// <summary>
        /// This method asks the implementer to subscribe to any events that it needs from ModuleQualityControl.
        /// </summary>
        /// <param name="moduleQualityControl">The BaseQualityControl object that is making the request.</param>
        void SubscribeToEvents(BaseQualityControl moduleQualityControl);

        /// <summary>
        /// Asks the implementer to perform whatever actions are needed when the part is declared broken.
        /// </summary>
        /// <param name="moduleQualityControl">The BaseQualityControl object that is making the request.</param>
        void OnPartBroken(BaseQualityControl moduleQualityControl);

        /// <summary>
        /// Called when the part is declared fixed, this method gives implementers a chance to restore their functionality.
        /// </summary>
        /// <param name="moduleQualityControl">The BaseQualityControl object that is making the request.</param>
        void OnPartFixed(BaseQualityControl moduleQualityControl);

        /// <summary>
        /// Asks the implementer if the module is activated. Only activated modules will be considered during quality checks.
        /// The return value varies with the type of breakable part module; fuel tanks are always active, while converters,
        /// drills, and engines are only active when they are running.
        /// </summary>
        /// <returns>True if the module is activated, false if not.</returns>
        bool ModuleIsActivated();

        /// <summary>
        /// Asks the implementer for the trait skill used for the quality check. Examples include ScienceSkill and RepairSkill.
        /// </summary>
        /// <returns>A string consisting of the skill used by the part module for quality checks.</returns>
        string GetCheckSkill();
    }

    public class BARISBridge
    {
        public static BARISBridge Instance;

        public static string PartBrokenCannotStart = " is broken and cannot be used until fixed.";
        public static string DrillBroken = " broke a drill!";
        public static string DrillLabel = " Drill";
        public static string ConverterBroken = " broke a converter!";
        public static string ConverterLabel = " Converter";
        public static string MsgBodyThis = "This ";
        public static string MsgBodyA = "A ";
        public static string MsgBodyUnitsOf = " units of ";
        public static string MsgMaintenance = " has reached a point where it needs periodic maintenance. Without maintenance the part will eventually break down. Each maintenance attempt will cost ";
        public static string MsgTakesASkilled = "It also takes a skilled ";
        public static string MsgToEffectRepairs = " to effect repairs.";
        public static string MsgBodyBroken1 = " has failed! It will cost ";
        public static string MsgBodyBroken2 = " to repair.";
        public static string MsgBodyBroken3 = " has failed!";
        public static string MsgBodyMothballed = " is mothballed.";

        public static float MessageDuration = 8.0f;
        public static bool showDebug;
        public static bool PartsCanBreak;
        public static bool ConvertersCanFail;
        public static bool DrillsCanFail;
        public static bool RepairsRequireResources;
        public static bool RepairsRequireEVA;
        public static bool CrewedPartsCanFail;
        public static bool CommandPodsCanFail;
        public static bool RepairsRequireSkill;
        public static bool LogAstronautAvertMsg;
        public static bool EmailMaintenanceRequests;
        public static bool EmailRepairRequests;
        public static bool VesselsNeedIntegration;
        public static int FlightsPerQualityBonus;
        public static bool RCSCanFail;
        public static bool SASCanFail;
        public static bool TanksCanFail;
        public static bool TransmittersCanFail;
        public static bool ParachutesCanFail;
        public static bool FailuresCanExplode;
        public static float ExplosivePotentialLaunches;
        public static float ExplosivePotentialCritical;
        public static bool EnginesCanFail;
        public static int QualityCap;

        public PlayModeUpdateDelegate playModeUpdate;

        public virtual void GetQualityStats(out int Quality, out int CurrentQuality, out double MTBF, out double CurrentMTBF)
        {
            Quality = 0;
            CurrentQuality = 0;
            MTBF = 0.0;
            CurrentMTBF = 0.0;
        }

        public void UpdatePlayMode(bool partsCanBreak, bool repairsRequireResources)
        {
            PartsCanBreak = partsCanBreak;
            RepairsRequireResources = repairsRequireResources;

            //Fire off delegate
            if (playModeUpdate != null)
                playModeUpdate(partsCanBreak, repairsRequireResources);
        }

        public static void LogPlayerMessage(string message)
        {
            ScreenMessages.PostScreenMessage(message, MessageDuration, ScreenMessageStyle.UPPER_CENTER);
            if (HighLogic.LoadedSceneIsFlight)
                FlightLogger.fetch.LogEvent(message);
        }
    }
}
