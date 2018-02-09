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
    public class BARISEventCard
    {
        /// <summary>
        /// (Required) Name of the event card
        /// </summary>
        public string name = string.Empty;

        /// <summary>
        /// (Required) Player-friendly description of the event card.
        /// </summary>
        public string description = string.Empty;

        /// <summary>
        /// File path of the card's image (if any)
        /// </summary>
        public string imageFilePath = string.Empty;

        /// <summary>
        /// List of event results
        /// </summary>
        public List<BARISEventResult> eventResults = new List<BARISEventResult>();

        static protected void debugLog(string message)
        {
            if (BARISScenario.showDebug == true)
                Debug.Log("[BARISEventCard] - " + message);
        }

        /// <summary>
        /// Applies the event results if they're valid.
        /// </summary>
        public string ApplyResults()
        {
            StringBuilder cardText = new StringBuilder();
            string message = string.Empty;

            //Add title and description
            cardText.AppendLine("<color=white>" + description + "</color>");
            cardText.AppendLine(" ");

            foreach (BARISEventResult eventResult in eventResults)
            {
                message = eventResult.ApplyResult();
                if (!string.IsNullOrEmpty(message))
                    cardText.AppendLine(message);
            }

            debugLog(cardText.ToString());
            return cardText.ToString();
        }

        public static List<BARISEventCard> LoadCards()
        {
            debugLog("LoadCards called");
            ConfigNode[] cardNodes = GameDatabase.Instance.GetConfigNodes("EVENTCARD");
            List<BARISEventCard> eventCards = new List<BARISEventCard>();
            BARISEventCard card = null;

            //Go through all the event card nodes and load all the valid ones.
            debugLog("Card node count: " + cardNodes.Length);
            for (int index = 0; index < cardNodes.Length; index++)
            {
                card = new BARISEventCard();
                if (card.Load(cardNodes[index]))
                    eventCards.Add(card);
            }

            return eventCards;
        }

        public virtual bool Load(ConfigNode node)
        {
            //We must have a name field.
            if (node.HasValue("name"))
                name = node.GetValue("name");
            else
                return false;
            debugLog("Loading " + name);

            //We must have a description field.
            if (node.HasValue("description"))
            {
                description = node.GetValue("description");
            }
            else
            {
                debugLog("No description");
                return false;
            }

            //We can optionally have an image
            if (node.HasValue("imageFilePath"))
            {
                imageFilePath = node.GetValue("imageFilePath");
            }

            //We must have at least one EVENTRESULT
            if (!node.HasNode("EVENTRESULT"))
            {
                debugLog("No event results");
                return false;
            }
            ConfigNode[] resultNodes = node.GetNodes("EVENTRESULT");
            ConfigNode resultNode;
            BARISEventResult eventResult;

            //Now load the event results
            eventResults.Clear();
            for (int index = 0; index < resultNodes.Length; index++)
            {
                resultNode = resultNodes[index];

                //Make sure we have a type field
                if (!resultNode.HasValue("type"))
                {
                    debugLog("type field not found");
                    continue;
                }

                //Load the result if we can.
                eventResult = new BARISEventResult();
                if (eventResult.Load(resultNode) && eventResult.IsValid())
                    eventResults.Add(eventResult);
            }

            //We must have at least one event result
            if (eventResults.Count > 0)
            {
                return true;
            }
            else
            {
                debugLog("eventResults count is 0");
                return false;
            }
        }

        public ConfigNode Save()
        {
            ConfigNode node = new ConfigNode("EVENTCARD");

            node.AddValue("name", name);
            node.AddValue("description", description);
            if (!string.IsNullOrEmpty(imageFilePath))
                node.AddValue("imageFilePath", imageFilePath);

            //Save individual event result nodes.
            foreach (BARISEventResult eventResult in eventResults)
                node.AddNode(eventResult.Save());

            return node;
        }
    }
}
