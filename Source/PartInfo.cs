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
using UnityEngine;

namespace DF
{
    public class PartInfo
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
        //timeLastElectricity   - the time last EC was taken to run the part's Frozen kerbal monitoring
        //timeLastTempCheck     - the time last Temperature check was taken on the part
        //deathCounter          - the EC has run out death counter
        //tmpdeathCounter       - the Temp is too hot death counter
        //outofEC               - true if part is out of EC
        //TmpStatus             - Status of the temperature of the part
        //cabinTemp             - The part cabin temperature in Kelvin
        //lastUpdate            - Time this class entry was last updated
        //
        public const string ConfigNodeName = "PartInfo";
        public Guid vesselID;
        public string PartName;
        public int numSeats;
        public int numCrew;
        public List<string> crewMembers;
        public List<string> crewMemberTraits;
        public int numFrznCrew;
        public bool hibernating;
        public bool hasextDoor;
        public double timeLastElectricity = 0f;
        public double timeLastTempCheck = 0f;        
        public double deathCounter = 0f;
        public double tmpdeathCounter = 0f;
        public bool outofEC;
        public FrzrTmpStatus TmpStatus = FrzrTmpStatus.OK;
        public float cabinTemp = 0f;
        public double lastUpdate = 0f;

        public PartInfo(Guid vesselid, string PartName, double currentTime)
        {
            this.vesselID = vesselid;
            this.PartName = PartName;
            hibernating = false;
            hasextDoor = false;
            outofEC = false;
            lastUpdate = currentTime;
            crewMembers = new List<string>();
            crewMemberTraits = new List<string>();          
        }

        public static PartInfo Load(ConfigNode node)
        {
            string PartName = Utilities.GetNodeValue(node, "PartName", "Unknown");
            double lastUpdate = Utilities.GetNodeValue(node, "lastUpdate", 0.0);
            string tmpvesselID = Utilities.GetNodeValue(node, "vesselID", "");
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
            info.numSeats = Utilities.GetNodeValue(node, "numSeats", 0);            
            info.numCrew = Utilities.GetNodeValue(node, "numCrew", 0);
            string CrewString = Utilities.GetNodeValue(node, "crewMembers", string.Empty);
            string[] CrewStrings = CrewString.Split(',');
            if (CrewStrings.Length > 0)
            {
                for (int i = 0; i < CrewStrings.Length; i++)
                {
                    info.crewMembers.Add(CrewStrings[i]);
                }
            }
            string CrewTraitString = Utilities.GetNodeValue(node, "crewMemberTraits", string.Empty);
            string[] CrewTStrings = CrewTraitString.Split(',');
            if (CrewTStrings.Length > 0)
            {
                for (int i = 0; i < CrewTStrings.Length; i++)
                {
                    info.crewMemberTraits.Add(CrewTStrings[i]);
                }
            }
            info.numFrznCrew = Utilities.GetNodeValue(node, "numFrznCrew", 0);
            info.hibernating = Utilities.GetNodeValue(node, "hibernating", false);
            info.hasextDoor = Utilities.GetNodeValue(node, "hasextDoor", false);
            info.timeLastElectricity = Utilities.GetNodeValue(node, "timeLastElectricity", lastUpdate);            
            info.timeLastTempCheck = Utilities.GetNodeValue(node, "timeLastTempCheck", lastUpdate);
            info.deathCounter = Utilities.GetNodeValue(node, "deathCounter", 0d);
            info.tmpdeathCounter = Utilities.GetNodeValue(node, "tmpdeathCounter", 0d);
            info.outofEC = Utilities.GetNodeValue(node, "outofEC", false);
            info.TmpStatus = Utilities.GetNodeValue(node, "TmpStatus", FrzrTmpStatus.OK);
            info.cabinTemp = Utilities.GetNodeValue(node, "cabinTemp", 0f);

            return info;
        }

        public ConfigNode Save(ConfigNode config)
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
            node.AddValue("timeLastElectricity", timeLastElectricity);            
            node.AddValue("timeLastTempCheck", timeLastTempCheck);
            node.AddValue("deathCounter", deathCounter);
            node.AddValue("tmpdeathCounter", tmpdeathCounter);
            node.AddValue("outofEC", outofEC);
            node.AddValue("TmpStatus", TmpStatus.ToString());
            node.AddValue("cabinTemp", cabinTemp);
            node.AddValue("lastUpdate", lastUpdate);
            return node;            
    }

        public void ClearAmounts()
        {
            numCrew = 0;            
            numFrznCrew = 0;
            timeLastElectricity = 0f;            
            timeLastTempCheck = 0f;
        }
    }
}