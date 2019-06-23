/**
 * DeepFreeze Continued...
 * (C) Copyright 2015, Jamie Leighton
 *
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 *
 *  This file is part of JPLRepo's DeepFreeze (continued...) - a Fork of DeepFreeze. Original Author of DeepFreeze is 'scottpaladin' on the KSP Forums.
 *  This File was not part of the original Deepfreeze but was written by Jamie Leighton.
 *  (C) Copyright 2015, Jamie Leighton
 *
 * Continues to be licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 4.0)
 * creative commons license. See <https://creativecommons.org/licenses/by-nc-sa/4.0/>
 * for full details.
 */

using RSTUtils;

namespace DF
{
    public class VesselInfo
    {
        //This class stores Info about vessels that have the DeepFreezer parts attached.
        //VesselName                - Name of the vessel
        //vesselType                - The vessel type.
        //numCrew                   - Number of crew on-board (does not include FROZEN crew)
        //numSeats                  - Number of seats in total on vessel
        //numOccupiedParts          - Number of parts that are currently occupied (does include FROZEN crew)
        //numFrznCrew               - Number of frozen crew on-board
        //hibernating               - True if vessel is unloaded
        //hasextDoor                - True if somewhere on the vessel is a DeepFreezer part with an external door
        //hasextPod                 - True if somewhere on the vessel is a DeepFreezer part with an external pod
        //lastUpdate                - Time this class entry was last updated
        //storedEC                  - How much ElectricCharge the vessel has
        //predictedECOut            - Predicted time in seconds ElectricCharge will run out (by just running the freezers)
        public const string ConfigNodeName = "VesselInfo";

        public string vesselName;
        public VesselType vesselType = VesselType.Unknown;
        public int numCrew;
        public int numSeats;
        public int numOccupiedParts;
        public int numFrznCrew;
        public bool hibernating;
        public bool hasextDoor;
        public bool hasextPod;
        public double lastUpdate;
        public double storedEC;
        public double predictedECOut;

        internal VesselInfo(string vesselName, double currentTime)
        {
            this.vesselName = vesselName;
            hibernating = false;
            hasextDoor = false;
            hasextPod = false;
            lastUpdate = currentTime;
        }

        internal static VesselInfo Load(ConfigNode node)
        {
            string vesselName = "Unknown";
            node.TryGetValue("vesselName", ref vesselName);
            double lastUpdate = 0f;
            node.TryGetValue("lastUpdate", ref lastUpdate);
            //string vesselName = Utilities.GetNodeValue(node, "vesselName", "Unknown");
            //double lastUpdate = Utilities.GetNodeValue(node, "lastUpdate", 0.0);

            VesselInfo info = new VesselInfo(vesselName, lastUpdate);
            node.TryGetValue("numSeats", ref info.numSeats);
            node.TryGetValue("numCrew", ref info.numCrew);
            node.TryGetValue("numOccupiedParts", ref info.numOccupiedParts);
            node.TryGetValue("numFrznCrew", ref info.numFrznCrew);
            node.TryGetValue("hibernating", ref info.hibernating);
            node.TryGetValue("hasextDoor", ref info.hasextDoor);
            node.TryGetValue("hasextPod", ref info.hasextPod);
            node.TryGetValue("storedEC", ref info.storedEC);
            node.TryGetValue("predictedECOut", ref info.predictedECOut);

            info.vesselType = Utilities.GetNodeValue(node, "vesselType", VesselType.Unknown);
            
            return info;
        }

        internal ConfigNode Save(ConfigNode config)
        {
            ConfigNode node = config.AddNode(ConfigNodeName);
            node.AddValue("vesselName", vesselName);
            node.AddValue("vesselType", vesselType.ToString());
            node.AddValue("numSeats", numSeats);
            node.AddValue("numCrew", numCrew);
            node.AddValue("numOccupiedParts", numOccupiedParts);
            node.AddValue("numFrznCrew", numFrznCrew);
            node.AddValue("hibernating", hibernating);
            node.AddValue("hasextDoor", hasextDoor);
            node.AddValue("hasextPod", hasextPod);
            node.AddValue("lastUpdate", lastUpdate);
            node.AddValue("storedEC", storedEC);
            node.AddValue("predictedECOut", predictedECOut);
            return node;
        }

        internal void ClearAmounts()
        {
            numCrew = 0;
            numOccupiedParts = 0;
            numFrznCrew = 0;
            numSeats = 0;
            storedEC = 0f;
            predictedECOut = 0f;
        }
    }
}