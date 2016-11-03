/**
 * * DeepFreeze Continued...
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

using System;
using System.Collections.Generic;
using RSTUtils;
using UnityEngine;

namespace DF
{
    internal class PartInfo
    {
        //This class stores Info about DeepFreezer Parts that have the DeepFreezer partmodule attached.
        //VesselID              - VesselID of the Vessel this part is a part of.
        //PartName              - The name of the part
        //numSeats              - How many seats in the part
        //numCrew               - How many crew in the part (NOT Frozen)
        //crewMembers           - List of crewMembers in the part (NOT Frozen)
        //crewMemberTraits      - Traits of the crewMembers in the part (for display in GUI)
        //numFrznCrew           - # of frozen crew in the part
        //hibernating           - true if the part/vessel is unloaded
        //hasextDoor            - true if the part has an External door and therefore needs TransparentPod treatment, etc.
        //hasextPod             - True if the part has an external pod and therefore need TransparentPod treatment, etc.
        //timeLastElectricity   - the time last EC was taken to run the part's Frozen kerbal monitoring
        //frznChargeRequired    - the amount of EC required per frozen kerbal to run the part's Frozen kerbal monitoring
        //timeLastTempCheck     - the time last Temperature check was taken on the part
        //deathCounter          - the EC has run out death counter
        //tmpdeathCounter       - the Temp is too hot death counter
        //outofEC               - true if part is out of EC
        //TmpStatus             - Status of the temperature of the part
        //cabinTemp             - The part cabin temperature in Kelvin
        //lastUpdate            - Time this class entry was last updated
        //
        internal const string ConfigNodeName = "PartInfo";

        internal Guid vesselID;
        internal string PartName;
        internal int numSeats;
        internal int numCrew;
        internal List<string> crewMembers;
        internal List<string> crewMemberTraits;
        internal int numFrznCrew;
        internal bool hibernating;
        internal bool hasextDoor;
        internal bool hasextPod;
        internal double timeLastElectricity;
        internal double frznChargeRequired;
        internal double timeLastTempCheck;
        internal double deathCounter;
        internal double tmpdeathCounter;
        internal bool outofEC;
        internal FrzrTmpStatus TmpStatus = FrzrTmpStatus.OK;
        internal bool ECWarning;
        internal bool TempWarning;
        internal float cabinTemp;
        internal double lastUpdate;

        internal PartInfo(Guid vesselid, string PartName, double currentTime)
        {
            vesselID = vesselid;
            this.PartName = PartName;
            hibernating = false;
            hasextDoor = false;
            hasextPod = false;
            outofEC = false;
            ECWarning = false;
            TempWarning = false;
            lastUpdate = currentTime;
            crewMembers = new List<string>();
            crewMemberTraits = new List<string>();
        }

        internal static PartInfo Load(ConfigNode node)
        {
            string PartName = "Unknown";
            node.TryGetValue("PartName", ref PartName);
            double lastUpdate = 0.0;
            node.TryGetValue("lastUpdate", ref lastUpdate);
            string tmpvesselID = "";
            node.TryGetValue("vesselID", ref tmpvesselID);
            Guid vesselID = Guid.Empty;
            try
            {
                vesselID = new Guid(tmpvesselID);
            }
            catch (Exception ex)
            {
                vesselID = Guid.Empty;
                Debug.Log("DFInterface - Load of GUID VesselID for known part failed Err: " + ex);
            }
            PartInfo info = new PartInfo(vesselID, PartName, lastUpdate);
            node.TryGetValue("numSeats", ref info.numSeats);
            node.TryGetValue("numCrew", ref info.numCrew);

            string CrewString = "";
            node.TryGetValue("crewMembers", ref CrewString);
            
            string[] CrewStrings = CrewString.Split(',');
            if (CrewStrings.Length > 0)
            {
                for (int i = 0; i < CrewStrings.Length; i++)
                {
                    info.crewMembers.Add(CrewStrings[i]);
                }
            }
            string CrewTraitString = "";
            node.TryGetValue("crewMemberTraits", ref CrewTraitString);
            string[] CrewTStrings = CrewTraitString.Split(',');
            if (CrewTStrings.Length > 0)
            {
                for (int i = 0; i < CrewTStrings.Length; i++)
                {
                    info.crewMemberTraits.Add(CrewTStrings[i]);
                }
            }
            node.TryGetValue("numFrznCrew", ref info.numFrznCrew);
            node.TryGetValue("hibernating", ref info.hibernating);
            node.TryGetValue("hasextDoor", ref info.hasextDoor);
            node.TryGetValue("hasextPod", ref info.hasextPod);
            node.TryGetValue("timeLastElectricity", ref info.timeLastElectricity);
            node.TryGetValue("frznChargeRequired", ref info.frznChargeRequired);
            node.TryGetValue("timeLastTempCheck", ref info.timeLastTempCheck);
            node.TryGetValue("deathCounter", ref info.deathCounter);
            node.TryGetValue("tmpdeathCounter", ref info.tmpdeathCounter);
            node.TryGetValue("outofEC", ref info.outofEC);
            info.TmpStatus = Utilities.GetNodeValue(node, "TmpStatus", FrzrTmpStatus.OK);
            node.TryGetValue("cabinTemp", ref info.cabinTemp);
            node.TryGetValue("ECWarning", ref info.ECWarning);
            node.TryGetValue("TempWarning", ref info.TempWarning);
            
            return info;
        }

        internal ConfigNode Save(ConfigNode config)
        {
            ConfigNode node = config.AddNode(ConfigNodeName);
            node.AddValue("vesselID", vesselID);
            node.AddValue("PartName", PartName);
            node.AddValue("numSeats", numSeats);
            node.AddValue("numCrew", numCrew);
            string crewlst = string.Join(",", crewMembers.ToArray());
            node.AddValue("crewMembers", crewlst);
            string crewtrts = string.Join(",", crewMemberTraits.ToArray());
            node.AddValue("crewMemberTraits", crewtrts);
            node.AddValue("numFrznCrew", numFrznCrew);
            node.AddValue("hibernating", hibernating);
            node.AddValue("hasextDoor", hasextDoor);
            node.AddValue("hasextPod", hasextPod);
            node.AddValue("timeLastElectricity", timeLastElectricity);
            node.AddValue("frznChargeRequired", frznChargeRequired);
            node.AddValue("timeLastTempCheck", timeLastTempCheck);
            node.AddValue("deathCounter", deathCounter);
            node.AddValue("tmpdeathCounter", tmpdeathCounter);
            node.AddValue("outofEC", outofEC);
            node.AddValue("TmpStatus", TmpStatus.ToString());
            node.AddValue("cabinTemp", cabinTemp);
            node.AddValue("ECWarning", ECWarning);
            node.AddValue("TempWarning", TempWarning);
            node.AddValue("lastUpdate", lastUpdate);
            return node;
        }

        internal void ClearAmounts()
        {
            numCrew = 0;
            numFrznCrew = 0;
            timeLastElectricity = 0f;
            timeLastTempCheck = 0f;
        }
    }
}