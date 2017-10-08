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
    public delegate bool AddWorkerDelegate(int currentWorkers);
    public delegate bool RemoveWorkerDelegate(int currentWorkers);
    public delegate void ReturnWorkersDelegate(int workerCount);
    public delegate void CloseViewDelegate();

    public class EditorBayView
    {
        const int InfoPanelWidth = 200;
        const int InfoPanelHeight = 440;

        public int bayID;
        public bool isVAB = true;

        public AddWorkerDelegate addWorker;
        public RemoveWorkerDelegate removeWorker;
        public ReturnWorkersDelegate returnWorkers;
        public CloseViewDelegate closeView;

        private Vector2 originPoint = new Vector2(0, 0);
        private GUILayoutOption[] infoPanelOptions = new GUILayoutOption[] { GUILayout.Width(InfoPanelWidth), GUILayout.Height(InfoPanelHeight) };
        private GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.Height(32), GUILayout.Width(32) };
        private float workerRequest;

        //Icons
        private bool exitIconHighlighted;
        private Texture exitIcon = null;
        private static Texture exitIconWhite = null;
        private static Texture exitIconBlack = null;
        private string exitToolTip;

        private bool newIconHighlighted;
        private Texture newIcon = null;
        private static Texture newIconWhite = null;
        private static Texture newIconBlack = null;
        private string newToolTip;

        private bool loadIconHighlighted;
        private Texture launchIcon = null;
        private static Texture loadIconWhite = null;
        private static Texture loadIconBlack = null;
        private string loadToolTip;

        private bool fundsIconHighlighted;
        private Texture fundsIcon = null;
        private static Texture fundsIconWhite = null;
        private static Texture fundsIconBlack = null;
        private string fundsToolTip;
        private string thumbnailFullPath;
        private string thumbnailBaseFileName;
        private string savesFolder;
        private EditorBayItem editorBayItem;

        protected void debugLog(string message)
        {
            if (BARISScenario.showDebug == true)
                Debug.Log("[EditorBayView] - " + message);
        }

        public EditorBayView(int editorBayID, bool isVABBay)
        {
            bayID = editorBayID;
            isVAB = isVABBay;
            editorBayItem = BARISScenario.Instance.GetEditorBay(isVAB, bayID);
            if (editorBayItem != null)
            {
                debugLog("Bay " + bayID + " exists.");
                debugLog(editorBayItem.ToString());
            }

            else
            {
                debugLog("Bay " + bayID + " does not exist, creating.");
                editorBayItem = new EditorBayItem();
                editorBayItem = BARISScenario.Instance.AddEditorBay(isVAB, bayID);
            }

            //Get the thumbnails folder.
            // See: http://stackoverflow.com/questions/52797/how-do-i-get-the-path-of-the-assembly-the-code-is-in/283917#283917
            DirectoryInfo directoryInfo = Directory.GetParent(Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).Path))).Parent.Parent;
            string codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            thumbnailFullPath = Path.GetDirectoryName(path);

            //Setup the root folder and thumbs folder.
            if (thumbnailFullPath.Contains("GameData"))
            {
                thumbnailBaseFileName = HighLogic.SaveFolder;
                if (isVAB)
                    thumbnailBaseFileName = thumbnailBaseFileName + "_VAB_";
                else
                    thumbnailBaseFileName = thumbnailBaseFileName + "_SPH_";

                int index = thumbnailFullPath.IndexOf("GameData");
                thumbnailFullPath = thumbnailFullPath.Substring(0, index);
                savesFolder = thumbnailFullPath + "saves/" + HighLogic.SaveFolder + "/";
                thumbnailFullPath += "thumbs/" + thumbnailBaseFileName;
            }

            //Icons
            if (exitIconBlack == null)
                exitIconBlack = GameDatabase.Instance.GetTexture("WildBlueIndustries/000BARIS/Icons/Exit", false);
            if (exitIconWhite == null)
                exitIconWhite = GameDatabase.Instance.GetTexture("WildBlueIndustries/000BARIS/Icons/ExitWhite", false);
            exitIcon = exitIconBlack;

            if (newIconBlack == null)
                newIconBlack = GameDatabase.Instance.GetTexture("WildBlueIndustries/000BARIS/Icons/NewIconBlack", false);
            if (newIconWhite == null)
                newIconWhite = GameDatabase.Instance.GetTexture("WildBlueIndustries/000BARIS/Icons/NewIconWhite", false);
            newIcon = newIconBlack;

            if (loadIconBlack == null)
                loadIconBlack = GameDatabase.Instance.GetTexture("WildBlueIndustries/000BARIS/Icons/loadIconBlack", false);
            if (loadIconWhite == null)
                loadIconWhite = GameDatabase.Instance.GetTexture("WildBlueIndustries/000BARIS/Icons/loadIconWhite", false);
            launchIcon = loadIconBlack;

            if (fundsIconBlack == null)
                fundsIconBlack = GameDatabase.Instance.GetTexture("WildBlueIndustries/000BARIS/Icons/FundsIconBlack", false);
            if (fundsIconWhite == null)
                fundsIconWhite = GameDatabase.Instance.GetTexture("WildBlueIndustries/000BARIS/Icons/FundsIconWhite", false);
            fundsIcon = fundsIconBlack;
        }

        public void DrawEditorBay()
        {
            GUILayout.BeginVertical();

            GUILayout.BeginScrollView(originPoint, infoPanelOptions);
            GUILayout.BeginVertical();

            if (BARISSettingsLaunch.VesselsNeedIntegration && BARISSettingsLaunch.LaunchesCanFail)
            {
                //High Bay/Hangar Bay ID code
                int highBayID = bayID + 1;
                if (isVAB)
                    GUILayout.Label("<color=white><b>" + BARISScenario.HighBayLabel + " " + highBayID + "</b></color>");
                else
                    GUILayout.Label("<color=white><b>" + BARISScenario.HangarBayLabel + " " + highBayID + "</b></color>");

                //Bay info
                if (!string.IsNullOrEmpty(editorBayItem.vesselName))
                    drawEditorBayItem(editorBayItem);
                else
                    drawEmptyBay();
            }
            else //Show some text indicating that vessel integration is disabled.
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=white><b>" + BARISScenario.IntegrationDisabledMsg + "</b></color>");
                GUILayout.FlexibleSpace();
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        protected void drawEmptyBay()
        {
            ShipConstruct ship = EditorLogic.fetch.ship;
            Color oldColor = GUI.backgroundColor;

            if (ship.parts.Count > 0)
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                //If the new integration button is pressed then record the info we need
                //to add editor bay data to the database.
                GUI.backgroundColor = XKCDColors.ColorTranslator.FromHtml("#97f5ff");
                if (GUILayout.Button(newIcon, buttonOptions))
                {
                    //Set up a new vehicle integration
                    editorBayItem.Clear();
                    editorBayItem.vesselName = ship.shipName;

                    //Generate snapshot of the vessel
                    ConfigNode shipNode = ship.SaveShip();
                    editorBayItem.vesselFilePath = savesFolder + ship.shipName + isVAB + editorBayItem.editorBayID + ".cfg";
                    debugLog("Vessel file: " + editorBayItem.vesselFilePath);
                    if (File.Exists(editorBayItem.vesselFilePath))
                        File.Delete(editorBayItem.vesselFilePath);
                    shipNode.Save(editorBayItem.vesselFilePath);
                    editorBayItem.vesselThumbnail = null;

                    float dryCost, fuelCost;
                    ship.GetShipCosts(out dryCost, out fuelCost);
                    editorBayItem.rushJobCost = dryCost;
                    calculateReliability(editorBayItem, ship);
                    editorBayItem.totalVesselParts = ship.parts.Count;

                    //Generate thumbnail
                    ShipConstruction.CaptureThumbnail(ship, "thumbs", thumbnailBaseFileName + ship.shipName);

                    //Generate the full path to the thumbnail and add it to the editorBayItem
                    editorBayItem.thumbnailPath = thumbnailFullPath + ship.shipName + ".png";

                    //Add workers
                    int availableWorkers = BARISScenario.Instance.GetAvailableWorkers(isVAB);
                    if (availableWorkers >= BARISScenario.MaxWorkersPerBay)
                    {
                        editorBayItem.workerCount = BARISScenario.MaxWorkersPerBay;
                        availableWorkers -= editorBayItem.workerCount;
                        BARISScenario.Instance.SetAvailableWorkers(availableWorkers, isVAB);
                    }
                    else
                    {
                        editorBayItem.workerCount = availableWorkers;
                        BARISScenario.Instance.SetAvailableWorkers(0, isVAB);
                    }

                    //Generate a KAC alarm (if KAC is installed)
                    setKACAlarm(editorBayItem);

                    //Save the new item
                    BARISScenario.Instance.SetEditorBay(editorBayItem);
                    GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.BACKUP);

                    //Cleanup
                    debugLog(editorBayItem.ToString());
                }
                newIcon = selectNewIcon();
                GUI.backgroundColor = oldColor;

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();

                //Tooltip
                if (string.IsNullOrEmpty(newToolTip) == false)
                    GUILayout.Label(newToolTip);
                else
                    GUILayout.Label(" ");
            }
        }

        protected void setKACAlarm(EditorBayItem editorBayItem)
        {
            if (!KACWrapper.AssemblyExists)
                return;
            if (!KACWrapper.APIReady)
                return;

            //Delete the alarm if it exists
            if (!string.IsNullOrEmpty(editorBayItem.KACAlarmID))
                KACWrapper.KAC.DeleteAlarm(editorBayItem.KACAlarmID);

            //Calculate the alarm time in seconds
            double secondsPerDay = GameSettings.KERBIN_TIME == true ? 21600 : 86400;
            double alarmTime = (editorBayItem.totalIntegrationToAdd / editorBayItem.workerCount) * secondsPerDay;
            alarmTime += Planetarium.GetUniversalTime();
            editorBayItem.KACAlarmID = KACWrapper.KAC.CreateAlarm(KACWrapper.KACAPI.AlarmTypeEnum.Raw, editorBayItem.vesselName + BARISScenario.IntegrationCompletedKACAlarm, alarmTime);
        }

        protected void drawVesselInfo(EditorBayItem editorBayItem)
        {
            //Vessel name
            GUILayout.Label("<color=lightBlue><b>" + editorBayItem.vesselName + "</b></color>");

            //Vessel thumbnail
            if (editorBayItem.vesselThumbnail != null)
            {
                GUILayout.Label(editorBayItem.vesselThumbnail, new GUILayoutOption[] { GUILayout.Width(175), GUILayout.Height(175) });
            }

            //Generate the image from the thumbnail file if it exists.
            else if (File.Exists(editorBayItem.thumbnailPath))
            {
                byte[] fileData = File.ReadAllBytes(editorBayItem.thumbnailPath);
                editorBayItem.vesselThumbnail = new Texture2D(2, 2);
                editorBayItem.vesselThumbnail.LoadImage(fileData);
                GUILayout.Label(editorBayItem.vesselThumbnail, new GUILayoutOption[] { GUILayout.Width(175), GUILayout.Height(175) });
            }

            //Thumbnail doesn't exist, use a default image.
            else
            {
            }
        }

        protected void drawReliability(EditorBayItem editorBayItem)
        {
            //Reliability
            GUILayout.Label("<color=white><b>" + Localizer.Format(BARISScenario.ReliabilityLabel) + "</b>" + editorBayItem.baseReliability + "/" + editorBayItem.maxReliability + "</color>");

            //Build Time Status
            GUILayout.Label(VehicleIntegrationStatusView.GetIntegrationStatusLabel(editorBayItem));
        }

        protected void drawRushJobButton(EditorBayItem editorBayItem)
        {
            //Rush jobs only apply to Career games.
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
                return;

            Color oldColor = GUI.backgroundColor;
            ShipConstruct ship = EditorLogic.fetch.ship;
            int totalMaxReliability = 100 * editorBayItem.breakablePartCount;
            int baseQuality = editorBayItem.totalQuality + editorBayItem.totalIntegrationAdded;
            int maxQuality = editorBayItem.totalQuality + editorBayItem.totalIntegrationAdded + editorBayItem.totalIntegrationToAdd;
            float rushJobCost = editorBayItem.rushJobCost * (1.0f - ((float)baseQuality / (float)maxQuality));

            //Rush Job button (Career only?)
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = XKCDColors.ColorTranslator.FromHtml("#c3fa70");
            if (GUILayout.Button(fundsIcon, buttonOptions))
            {
                //Can we afford it?
                if (Funding.CanAfford(rushJobCost))
                {
                    if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER && !BARISScenario.showDebug)
                        Funding.Instance.AddFunds(-rushJobCost, TransactionReasons.Any);

                    //Add the integration cap to the base quality, and clear the cap.
                    if (editorBayItem.totalIntegrationToAdd > 0)
                    {
                        editorBayItem.totalIntegrationAdded = editorBayItem.totalIntegrationToAdd;
                        editorBayItem.totalIntegrationToAdd = 0;
                    }

                    //Delete the KAC alarm if any
                    if (KACWrapper.AssemblyExists && KACWrapper.APIReady && !string.IsNullOrEmpty(editorBayItem.KACAlarmID))
                        KACWrapper.KAC.DeleteAlarm(editorBayItem.KACAlarmID);

                    //Load the vessel
                    loadVessel(editorBayItem, false);
                }
                else //Inform user
                {
                    BARISScenario.Instance.LogPlayerMessage(Localizer.Format(BARISScenario.CannotAffordRushMsg));
                }
            }
            GUI.backgroundColor = oldColor;
            fundsIcon = selectFundsIcon();

            //Rush job cost
            GUILayout.Label("<color=white><b>" + Localizer.Format(BARISScenario.CostLabel) + "</b>" + string.Format("{0:n0}", rushJobCost) + "</color>");
            GUILayout.EndHorizontal();
        }

        protected void drawWorkPaused()
        {
            GUILayout.Label("<color=white><b>" + Localizer.Format(BARISScenario.WorkPausedMsg1) +
                BARISScenario.Instance.workPausedDays + Localizer.Format(BARISScenario.WorkPausedMsg2) + "</b></color>");
        }

        protected void drawWorkersButtons(EditorBayItem editorBayItem)
        {
            Color oldColor = GUI.backgroundColor;
            ShipConstruct ship = EditorLogic.fetch.ship;

            //Add/remove workers
            GUILayout.BeginHorizontal();

            GUILayout.Label("<color=white><b>" + Localizer.Format(BARISScenario.WorkersLabel) + "</b>" + editorBayItem.workerCount + "/" + BARISScenario.MaxWorkersPerBay + "</color>");

            //Remove workers button
            if (GUILayout.RepeatButton("-"))
            {
                workerRequest += 0.1f;
                if (workerRequest >= 1.0f)
                {
                    workerRequest = 0f;
                    if (removeWorker != null)
                    {
                        if (removeWorker(editorBayItem.workerCount))
                        {
                            editorBayItem.workerCount -= 1;
                            BARISScenario.Instance.SetEditorBay(editorBayItem);
                            setKACAlarm(editorBayItem);
                        }
                    }
                }
            }

            //Add workers button
            if (GUILayout.RepeatButton("+"))
            {
                workerRequest += 0.1f;
                if (workerRequest >= 1.0f)
                {
                    workerRequest = 0f;
                    if (addWorker != null)
                    {
                        if (addWorker(editorBayItem.workerCount))
                        {
                            editorBayItem.workerCount += 1;
                            BARISScenario.Instance.SetEditorBay(editorBayItem);
                            setKACAlarm(editorBayItem);
                        }
                    }
                }
            }

            GUILayout.EndHorizontal();
        }

        protected void drawLoadAndCancelButtons(EditorBayItem editorBayItem)
        {
            Color oldColor = GUI.backgroundColor;
            ShipConstruct ship = EditorLogic.fetch.ship;

            //Load button
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = XKCDColors.LemonYellow;
            if (GUILayout.Button(launchIcon, buttonOptions))
            {
                loadVessel(editorBayItem);

                /*
                //Launch the vessel. This will fill the ship with a default crew, which isn't what we want.
                if (isVAB)
                    HighLogic.CurrentGame.editorFacility = EditorFacility.VAB;
                else
                    HighLogic.CurrentGame.editorFacility = EditorFacility.SPH;
                
                VesselCrewManifest manifest = KSP.UI.CrewAssignmentDialog.Instance.GetManifest();
                if (manifest == null)
                    manifest = HighLogic.CurrentGame.CrewRoster.DefaultCrewForVessel(EditorLogic.fetch.ship.SaveShip(), null, true);

                FlightDriver.StartWithNewLaunch(editorBayItem.vesselFilePath, EditorLogic.FlagURL, EditorLogic.fetch.launchSiteName, manifest);
                 */
            }
            launchIcon = selectLoadIcon();
            GUI.backgroundColor = oldColor;

            //Cancel Integration button
            GUI.backgroundColor = XKCDColors.ColorTranslator.FromHtml("#fe7e56");
            if (GUILayout.Button(exitIcon, buttonOptions))
            {
                //Return workers
                BARISScenario.Instance.ReturnWorkers(editorBayItem);

                //Clear the bay
                editorBayItem.Clear();
                BARISLaunchButtonManager.editorBayItem = null;

                //Delete the vessel file
                editorBayItem.DeleteSnapshot();

                BARISScenario.Instance.SetEditorBay(editorBayItem);

                //Save the game
                GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
            }
            exitIcon = selectExitIcon();
            GUI.backgroundColor = oldColor;
            GUILayout.EndHorizontal();
        }

        protected void drawTooltip(EditorBayItem editorBayItem)
        {
            //Tooltip
            GUILayout.FlexibleSpace();
            if (string.IsNullOrEmpty(exitToolTip) == false)
                GUILayout.Label(exitToolTip);
            else if (string.IsNullOrEmpty(loadToolTip) == false)
                GUILayout.Label(loadToolTip);
            else if (string.IsNullOrEmpty(newToolTip) == false)
                GUILayout.Label(newToolTip);
            else if (string.IsNullOrEmpty(fundsToolTip) == false)
                GUILayout.Label(fundsToolTip);
            else
                GUILayout.Label(" ");
        }

        protected void drawEditorBayItem(EditorBayItem editorBayItem)
        {
            drawVesselInfo(editorBayItem);
            drawReliability(editorBayItem);
            if (BARISScenario.Instance.workPausedDays <= 0)
                drawRushJobButton(editorBayItem);
            else
                drawWorkPaused();
            drawWorkersButtons(editorBayItem);
            drawLoadAndCancelButtons(editorBayItem);
            drawTooltip(editorBayItem);
        }

        protected Texture selectFundsIcon()
        {
            Texture selectedIcon;

            if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                selectedIcon = fundsIconWhite;
                fundsIconHighlighted = true;
                fundsToolTip = BARISScenario.RushJobLabel;
            }
            else if (fundsIconHighlighted)
            {
                selectedIcon = fundsIconWhite;
                fundsIconHighlighted = false;
                fundsToolTip = BARISScenario.RushJobLabel;
            }
            else
            {
                selectedIcon = fundsIconBlack;
                fundsToolTip = "";
            }

            return selectedIcon;
        }

        protected Texture selectLoadIcon()
        {
            Texture selectedIcon;

            if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                selectedIcon = loadIconWhite;
                loadIconHighlighted = true;
                loadToolTip = BARISScenario.LoadButtonTitle;
            }
            else if (loadIconHighlighted)
            {
                selectedIcon = loadIconWhite;
                loadIconHighlighted = false;
                loadToolTip = BARISScenario.LoadButtonTitle;
            }
            else
            {
                selectedIcon = loadIconBlack;
                loadToolTip = "";
            }

            return selectedIcon;
        }

        protected Texture selectNewIcon()
        {
            Texture selectedIcon;

            if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                selectedIcon = newIconWhite;
                newIconHighlighted = true;
                newToolTip = BARISScenario.StartVehicleIntegrationButton;
            }
            else if (newIconHighlighted)
            {
                selectedIcon = newIconWhite;
                newIconHighlighted = false;
                newToolTip = BARISScenario.StartVehicleIntegrationButton;
            }
            else
            {
                selectedIcon = newIconBlack;
                newToolTip = "";
            }

            return selectedIcon;
        }

        protected Texture selectExitIcon()
        {
            Texture selectedIcon;

            if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                selectedIcon = exitIconWhite;
                exitIconHighlighted = true;
                exitToolTip = BARISScenario.CancelIntegrationLabel;
            }
            else if (exitIconHighlighted)
            {
                selectedIcon = exitIconWhite;
                exitIconHighlighted = false;
                exitToolTip = BARISScenario.CancelIntegrationLabel;
            }
            else
            {
                selectedIcon = exitIconBlack;
                exitToolTip = "";
            }

            return selectedIcon;
        }

        protected void calculateReliability(EditorBayItem editorBayItem, ShipConstruct ship)
        {
            Part[] shipParts = ship.parts.ToArray();
            ModuleQualityControl qualityControl;
            int integrationCap = BARISScenario.Instance.GetIntegrationCap(isVAB);
            int flightExperienceBonus;

            //Zero out the stats
            editorBayItem.breakablePartCount = 0;
            editorBayItem.totalQuality = 0;
            editorBayItem.totalIntegrationToAdd = 0;

            //Now sum up the stats we need
            foreach (Part part in shipParts)
            {
                qualityControl = part.FindModuleImplementing<ModuleQualityControl>();
                if (qualityControl != null)
                {
                    flightExperienceBonus = BARISScenario.Instance.GetFlightBonus(part);

                    editorBayItem.breakablePartCount += 1;
                    editorBayItem.totalQuality += qualityControl.quality + flightExperienceBonus;
                    editorBayItem.totalIntegrationToAdd += integrationCap;

                    //Add part data. We'll need this if flight experience changes while a vehicle is going through integration.
                    editorBayItem.AddPartQualityData(part.partInfo.title, qualityControl.quality, flightExperienceBonus);
                }
            }
        }

        protected void loadVessel(EditorBayItem editorBayItem, bool closeDialog = true)
        {
            debugLog("loadVessel called, attempting to load " + editorBayItem.vesselName);

            //Return workers
            BARISScenario.Instance.ReturnWorkers(editorBayItem);

            //Set the launch button manager's editor bay item.
            BARISLaunchButtonManager.editorBayItem = editorBayItem;

            //Load the ship from the editor bay
            debugLog("Vessel file: " + editorBayItem.vesselFilePath);
            EditorLogic.LoadShipFromFile(editorBayItem.vesselFilePath);

            //Close the view.
            if (closeView != null && closeDialog)
                closeView();
        }
    }
}
