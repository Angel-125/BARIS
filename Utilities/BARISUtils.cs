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
    public enum BARISStatusTypes
    {
        dead,
        missing,
        badS
    }

    public enum BARISWorkerTypes
    {
        astronaut,
        construction,
        both
    }

    public enum BARISEventTypes
    {
        currencyChange,
        astronautStatusChange,
        astronautRecruited,
        vehicleIntegrationLost,
        vehicleIntegrationPaused,
        vehicleIntegrationCompleted,
        workerPayIncrease,
        qualityCheck,
        facilityDestroyed,
        custom,
        astronautRetires
    }

    public enum BARISEventCurrencyTypes
    {
        funds,
        reputation,
        science
    }

    public enum BARISEventModifierTypes
    {
        modifier,
        multiplier,
        criticalSuccess,
        criticalFail,
        success,
        fail
    }

    public enum BARRISEngineFailureModes
    {
        None,
        ReducedThrust,
        Shutdown,
        StuckOn
    }

    /// <summary>
    /// Used internally by BARIS to determine what the status of the quality check is.
    /// </summary>
    public enum QualityCheckStatus
    {
        /// <summary>
        /// No result.
        /// </summary>
        none,

        /// <summary>
        /// Successful result.
        /// </summary>
        success,

        /// <summary>
        /// The quality check failed.
        /// </summary>
        fail,

        /// <summary>
        /// The quality check has suffered a critical failure.
        /// </summary>
        criticalFail,

        /// <summary>
        /// The quality check had a critical success.
        /// </summary>
        criticalSuccess,

        /// <summary>
        /// The quality check would have failed, but an event card was able to avert the failure.
        /// </summary>
        eventCardAverted,

        /// <summary>
        /// The quality check would have failed, but an astronaut's skill was able to avert the failure.
        /// </summary>
        astronautAverted
    }

    /// <summary>
    /// Used internally by BARIS, this struct contains the results of a quality check.
    /// </summary>
    public struct QualityCheckResult
    {
        /// <summary>
        /// The result roll itself.
        /// </summary>
        public int resultRoll;

        /// <summary>
        /// The highest ranking astronaut with the required quality check skill(s).
        /// </summary>
        public ProtoCrewMember astronaut;

        /// <summary>
        /// The value of the highest rank
        /// </summary>
        public int highestRank;

        /// <summary>
        /// The status result of the quality check. See QualityCheckStatus for details.
        /// </summary>
        public QualityCheckStatus statusResult;
    }

    /// <summary>
    /// Used internally by BARIS for vessel information displays.
    /// </summary>
    public struct BreakablePart
    {
        /// <summary>
        /// The part that can break.
        /// </summary>
        public Part part;

        /// <summary>
        /// The part's ModuleQualityControl
        /// </summary>
        public ModuleQualityControl qualityControl;

        /// <summary>
        /// Any breakable drills that the part has.
        /// </summary>
        public List<ModuleBreakableAsteroidDrill> breakableDrills;

        /// <summary>
        /// Any breakable converters that the part has.
        /// </summary>
        public List<ModuleBreakableConverter> breakableConverters;

        /// <summary>
        /// Any breakable harvesters that the part has.
        /// </summary>
        public List<ModuleBreakableHarvester> breakableHarvesters;

        /// <summary>
        /// Any breakable engines that the part has.
        /// </summary>
        public ModuleBreakableEngine breakableEngine;

        /// <summary>
        /// Any breakable fuel tanks that the part has.
        /// </summary>
        public ModuleBreakableFuelTank breakableFuelTank;

        /// <summary>
        /// Any breakable RCS modules that the part has.
        /// </summary>
        public ModuleBreakableRCS breakableRCS;

        /// <summary>
        /// Any breakable SAS/Gyro modules that the part has.
        /// </summary>
        public ModuleBreakableSAS breakableSAS;

        /// <summary>
        /// Any breakable transmitters that the part has.
        /// </summary>
        public ModuleBreakableTransmitter breakableTransmitter;
    }

    public class BARISUtils
    {
        public static bool IsFilterEnabled(Vessel vessel)
        {
            //For non-tracking station scenes we only allow a fixed set of vessels.
            if (HighLogic.LoadedScene != GameScenes.TRACKSTATION)
            {
                switch (vessel.vesselType)
                {
                    case VesselType.Base:
                    case VesselType.Lander:
                    case VesselType.Plane:
                    case VesselType.Probe:
                    case VesselType.Relay:
                    case VesselType.Rover:
                    case VesselType.Ship:
                        return true;

                    default:
                        return false;
                }
            }

            //For the tracking station, we rely upon the filter buttons.
            MapViewFiltering.VesselTypeFilter vesselTypeFilter = MapViewFiltering.vesselTypeFilter;

            if (vesselTypeFilter == MapViewFiltering.VesselTypeFilter.All)
                return true;
            if (vesselTypeFilter == MapViewFiltering.VesselTypeFilter.None)
                return false;

            switch (vessel.vesselType)
            {
                case VesselType.Base:
                    return (vesselTypeFilter & MapViewFiltering.VesselTypeFilter.Bases) > 0 ? true : false;

                case VesselType.Lander:
                    return (vesselTypeFilter & MapViewFiltering.VesselTypeFilter.Landers) > 0 ? true : false;

                case VesselType.Plane:
                    return (vesselTypeFilter & MapViewFiltering.VesselTypeFilter.Plane) > 0 ? true : false;

                case VesselType.Probe:
                    return (vesselTypeFilter & MapViewFiltering.VesselTypeFilter.Probes) > 0 ? true : false;

                case VesselType.Relay:
                    return (vesselTypeFilter & MapViewFiltering.VesselTypeFilter.Relay) > 0 ? true : false;

                case VesselType.Rover:
                    return (vesselTypeFilter & MapViewFiltering.VesselTypeFilter.Rovers) > 0 ? true : false;

                case VesselType.Ship:
                    return (vesselTypeFilter & MapViewFiltering.VesselTypeFilter.Ships) > 0 ? true : false;

                default:
                    return false;
            }
        }
    }
}
