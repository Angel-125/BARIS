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
Source code copyright 2017, by Michael Billard (Angel-125)
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
    public struct PartQualityData
    {
        public string partTitle;
        public int quality;
        public int flightExperience;
    }

    /// <summary>
    /// This class holds information regarding a vessel undergoing vehicle integration. Vehicle integration is necessary to improve a vessel's reliability before launch.
    /// </summary>
    public class EditorBayItem
    {
        /// <summary>
        /// Set to true if the editor bay is one of the VAB bays, false if it is one of the SPH bays.
        /// </summary>
        public bool isVAB;

        /// <summary>
        /// ID number of the editor bay.
        /// </summary>
        public int editorBayID;

        /// <summary>
        /// Name of the vessel.
        /// </summary>
        public string vesselName;

        /// <summary>
        /// A texture containing the vessel thumbnail. It is generated when looking at the editor bay.
        /// </summary>
        public Texture2D vesselThumbnail;

        /// <summary>
        /// Path to the thumbnail file.
        /// </summary>
        public string thumbnailPath;

        /// <summary>
        /// The number of workers currently employed on the vehicle integration project.
        /// </summary>
        public int workerCount;

        /// <summary>
        /// Amount of Funds to charge for rushing vehicle integration.
        /// </summary>
        public float rushJobCost;

        /// <summary>
        /// Total number of breakable parts in the vessel.
        /// </summary>
        public int breakablePartCount;

        /// <summary>
        /// Total quality of all the breakable parts in the vessel. Equal to each part's base quality plus its flight experience.
        /// This value increases during each build cycle. It is used for display purposes during vehicle integration.
        /// </summary>
        public int totalQuality;

        /// <summary>
        /// Total amount of integration points to add. This value drops during each build cycle. It is used for display purposes during vehicle integration
        /// and to track when a vessel has had its parts fully integrated.
        /// </summary>
        public int totalIntegrationToAdd;

        /// <summary>
        /// Tracks how many integration points were added during vehicle integration. When a vessel is preped for launch, this value is divide by the total
        /// breakable parts to get the integration per part. The value is then given to each breakable part's integration bonus.
        /// </summary>
        public int totalIntegrationAdded;

        /// <summary>
        /// A string containing the snapshot of the craft file.
        /// </summary>
        public string vesselFilePath;

        /// <summary>
        /// Total number of parts in the vessel. This is used to determine whether or not the player has tampered with the vessel after loading it from an editor bay.
        /// </summary>
        public int totalVesselParts;

        /// <summary>
        /// Name of the construction KAC alarm that is set when vessel integration begins. Requires Kerbal Alarm Clock.
        /// </summary>
        public string KACAlarmID = string.Empty;

        /// <summary>
        /// A list of all the breakable parts in the vessel along with their quality ratings and current flight experience.
        /// </summary>
        public List<PartQualityData> partQualityData = new List<PartQualityData>();

        protected void debugLog(string message)
        {
            if (BARISScenario.showDebug == true)
                Debug.Log("[EditorBayItem] - " + message);
        }

        /// <summary>
        /// Returns the base reliability of the editor bay.
        /// </summary>
        public int baseReliability
        {
            get
            {
                int totalMaxReliability = 100 * breakablePartCount;
                int reliability = Mathf.RoundToInt(((float)(totalQuality + totalIntegrationAdded) / (float)totalMaxReliability) * 100.0f);

                if (reliability < 0)
                    reliability = 0;

                return reliability;
            }
        }

        /// <summary>
        /// Returns the max reliability of the editor bay.
        /// </summary>
        public int maxReliability
        {
            get
            {
                int totalMaxReliability = 100 * breakablePartCount;
                int reliability = Mathf.RoundToInt(((float)(totalQuality + totalIntegrationAdded + totalIntegrationToAdd) / (float)totalMaxReliability) * 100.0f);

                if (reliability < 0)
                    reliability = 0;

                return reliability;
            }
        }

        /// <summary>
        /// Recalculates the total quality of all the breakable parts in the editor bay.
        /// </summary>
        public void RecalculateTotalQuality()
        {
            //If the bay is empty then we're done.
            if (string.IsNullOrEmpty(vesselName))
                return;

            //If we don't have any part quality data then we're done.
            if (partQualityData.Count == 0)
                return;

            debugLog("Editor Bay " + editorBayID + " recalculating totalQuality.");
            debugLog("Editor Bay " + editorBayID + " totalQuality before update: " + totalQuality);

            //Setup
            totalQuality = 0;

            int flightExperience;
            PartQualityData[] dataItems = partQualityData.ToArray();
            PartQualityData qualityData;
            for (int index = 0; index < dataItems.Length; index++)
            {
                qualityData = dataItems[index];
                flightExperience = BARISScenario.Instance.GetFlightBonus(qualityData.partTitle);

                //Add the quality and flight experience
                totalQuality += qualityData.quality + flightExperience;

                //Update flight experience
                qualityData.flightExperience = flightExperience;
                partQualityData[index] = qualityData;
            }

//            debugLog("Editor Bay " + editorBayID + " totalQuality after update: " + totalQuality);
            debugLog(this.ToString());
        }

        /// <summary>
        /// Adds a new PartQualityData item to the internal list.
        /// </summary>
        /// <param name="title">User-friendly name of the part.</param>
        /// <param name="quality">Quality rating of the part.</param>
        /// <param name="flightExperienceBonus">Flight experience bonus of the part.</param>
        public void AddPartQualityData(string title, int quality, int flightExperienceBonus)
        {
            PartQualityData qualityData = new PartQualityData();
            qualityData.partTitle = title;
            qualityData.quality = quality;
            qualityData.flightExperience = flightExperienceBonus;

            partQualityData.Add(qualityData);
        }

        /// <summary>
        /// Clears all the fields in the editor bay except for bay ID and isVAB.
        /// </summary>
        public void Clear()
        {
            DeleteSnapshot();

            vesselFilePath = string.Empty;
            vesselName = string.Empty;
            workerCount = 0;
            rushJobCost = 0f;
            breakablePartCount = 0;
            totalIntegrationAdded = 0;
            totalIntegrationToAdd = 0;
            totalQuality = 0;
            totalVesselParts = 0;
            vesselThumbnail = null;

            //With KAC installed, clear the alarm.
            if (KACWrapper.AssemblyExists && KACWrapper.APIReady)
                KACWrapper.KAC.DeleteAlarm(KACAlarmID);

            //Clear part quality data
            partQualityData.Clear();
        }

        /// <summary>
        /// Generates a string containing information about the editor bay.
        /// </summary>
        /// <returns>A string containing information about the editor bay.</returns>
        public override string ToString()
        {
            StringBuilder info = new StringBuilder();

            info.AppendLine("Is VAB: " + isVAB);
            info.AppendLine("BAY ID: " + editorBayID);
            info.AppendLine("Vessel Name: " + vesselName);
            info.AppendLine("Worker Count: " + workerCount);
            info.AppendLine("Rush Job Cost: " + rushJobCost);
            info.AppendLine("Breakable Parts: " + breakablePartCount);
            info.AppendLine("Total Parts: " + totalVesselParts);
            info.AppendLine("Total Quality: " + totalQuality);
            info.AppendLine("Total Integration To Add: " + totalIntegrationToAdd);
            info.AppendLine("Total Integration Added: " + totalIntegrationAdded);
            info.AppendLine("Vessel File Path: " + vesselFilePath);

            foreach (PartQualityData qualityData in partQualityData)
            {
                info.AppendLine(qualityData.partTitle + " quality: " + qualityData.quality + " flight bonus: " + qualityData.flightExperience);
            }

            return info.ToString();
        }

        /// <summary>
        /// Deletes the vessel snapshot file.
        /// </summary>
        public void DeleteSnapshot()
        {
            if (string.IsNullOrEmpty(vesselFilePath))
                return;

            if (File.Exists(vesselFilePath))
                File.Delete(vesselFilePath);
        }

        /// <summary>
        /// Serializes the EditorBayItem into a ConfigNode.
        /// </summary>
        /// <returns>A ConfigNode containing seralized values.</returns>
        public ConfigNode Save(bool isRevertBay = false)
        {
            string nodeName = isRevertBay == false ? "EditorBayItem" : "RevertBayItem";
            ConfigNode node = new ConfigNode(nodeName);

            node.AddValue("isVAB", isVAB);
            node.AddValue("editorBayID", editorBayID);
            if (!string.IsNullOrEmpty(vesselName))
                node.AddValue("vesselName", vesselName);
            if (!string.IsNullOrEmpty(thumbnailPath))
                node.AddValue("thumbnailPath", thumbnailPath);
            node.AddValue("workerCount", workerCount);
            node.AddValue("rushJobCost", rushJobCost);
            node.AddValue("breakablePartCount", breakablePartCount);
            node.AddValue("totalQuality", totalQuality);
            node.AddValue("totalIntegrationToAdd", totalIntegrationToAdd);
            node.AddValue("totalIntegrationAdded", totalIntegrationAdded);
            if (!string.IsNullOrEmpty(vesselFilePath))
                node.AddValue("vesselFilePath", vesselFilePath);
            node.AddValue("totalVesselParts", totalVesselParts);
            if (!string.IsNullOrEmpty(KACAlarmID))
                node.AddValue("KACAlarmID", KACAlarmID);

            ConfigNode qualityDataNode;
            foreach (PartQualityData qualityData in partQualityData)
            {
                qualityDataNode = new ConfigNode("PartQualityData");
                qualityDataNode.AddValue("partTitle", qualityData.partTitle);
                qualityDataNode.AddValue("quality", qualityData.quality);
                qualityDataNode.AddValue("flightExperienceBonus", qualityData.flightExperience);
                node.AddNode(qualityDataNode);
            }

            return node;
        }

        /// <summary>
        /// De-serializes the ConfigNode's values into the EditorBayItem.
        /// </summary>
        /// <param name="node">A ConfigNode containing values to de-serialize.</param>
        public void Load(ConfigNode node)
        {
            if (node.HasValue("isVAB"))
                isVAB = bool.Parse(node.GetValue("isVAB"));

            if (node.HasValue("editorBayID"))
                editorBayID = int.Parse(node.GetValue("editorBayID"));

            if (node.HasValue("vesselName"))
                vesselName = node.GetValue("vesselName");

            if (node.HasValue("thumbnailPath"))
                thumbnailPath = node.GetValue("thumbnailPath");

            if (node.HasValue("workerCount"))
                workerCount = int.Parse(node.GetValue("workerCount"));

            if (node.HasValue("rushJobCost"))
                rushJobCost = float.Parse(node.GetValue("rushJobCost"));

            if (node.HasValue("breakablePartCount"))
                breakablePartCount = int.Parse(node.GetValue("breakablePartCount"));

            if (node.HasValue("totalQuality"))
                totalQuality = int.Parse(node.GetValue("totalQuality"));

            if (node.HasValue("totalIntegrationToAdd"))
                totalIntegrationToAdd = int.Parse(node.GetValue("totalIntegrationToAdd"));

            if (node.HasValue("totalIntegrationAdded"))
                totalIntegrationAdded = int.Parse(node.GetValue("totalIntegrationAdded"));

            if (node.HasValue("vesselFilePath"))
                vesselFilePath = node.GetValue("vesselFilePath");

            if (node.HasValue("totalVesselParts"))
                totalVesselParts = int.Parse(node.GetValue("totalVesselParts"));

            if (node.HasValue("KACAlarmID"))
                KACAlarmID = node.GetValue("KACAlarmID");

            if (node.HasNode("PartQualityData"))
            {
                partQualityData.Clear();
                ConfigNode[] qualityDataNodes = node.GetNodes("PartQualityData");
                PartQualityData qualityData;
                foreach (ConfigNode dataNode in qualityDataNodes)
                {
                    qualityData = new PartQualityData();
                    qualityData.partTitle = dataNode.GetValue("partTitle");
                    qualityData.quality = int.Parse(dataNode.GetValue("quality"));
                    qualityData.flightExperience = int.Parse(dataNode.GetValue("flightExperienceBonus"));

                    partQualityData.Add(qualityData);
                }
            }

        }
    }
}
