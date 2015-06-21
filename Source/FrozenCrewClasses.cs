/**
 * FrozenCrewClasses.cs
 *
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
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DF
{

    public class FrznCrewMbr
    {
        public string CrewName { get; set; }
        public int SeatIdx { get; set; }
        public Guid VesselID { get; set; }
        public string VesselName { get; set; }

        public FrznCrewMbr(string crewName, int seat, Guid vessel, string VesselName)
        {

            this.CrewName = crewName;
            this.SeatIdx = seat;
            this.VesselID = vessel;
            this.VesselName = VesselName;
        }

    }
    public class FrznCrewList : List<FrznCrewMbr>
    {
        /*
        public string Serialize()
        {
            string tmpstring = "";
            Utilities.Log_Debug("DeepFreezer", "Serialize # of crew =" + this.Count());
            foreach (FrznCrewMbr crew in this)
            {
                tmpstring += crew.CrewName + "," + crew.SeatIdx.ToString() + "," + crew.VesselID.ToString() + "~";
                Utilities.Log_Debug("DeepFreezer", "Serialized string = " + tmpstring);
            }
            return tmpstring;
        }

        public void Deserialize(string str, Guid CrntVslID)
        {
            Utilities.Log_Debug("DeepFreezer", "DeSerialize on load");
            this.Clear();
            List<string> spltcrew = new List<string>();
            spltcrew = str.Split('~').ToList();
            foreach (string strcrew in spltcrew)
            {
                if (strcrew.Length > 0)
                {
                    string[] arr = strcrew.Split(',');
                    string crewName = arr[0];
                    string strseat = arr[1];
                    int seat = 0;
                    bool prse = int.TryParse(strseat, out seat);
                    Guid vsl = Guid.NewGuid();
                    if (arr[2] != "" && arr[2] != null)
                    {
                        vsl = new Guid(arr[2]); 
                    }

                                                                             
                    // Check this crewmember is not in the crewRoster.
                    if (HighLogic.CurrentGame.CrewRoster.Crew.FirstOrDefault(a => a.name == crewName) != null)
                    {
                        // Looks like they have been thawed already.
                        Utilities.Log("DeepFreezer", "On Load of Part Frozen crew was found active (thawed from space center). So Removing from part");
                        Utilities.Log_Debug("DeepFreezer", "DID NOT Add crew =" + crewName + " seat=" + seat);
                    }
                    else
                    {
                        if (vsl != CrntVslID)
                        {
                            //This should not happen? maybe if two vessels dock??
                            Utilities.Log("DeepFreezer", "VesselID has changed between current and stored? Report this to MOD Thread on KSP Forum");
                            //set the vesselID to the current regardless
                            vsl = CrntVslID;
                        }
                        FrznCrewMbr newcrew = new FrznCrewMbr(crewName, seat, vsl);
                        this.Add(newcrew);
                        Utilities.Log_Debug("DeepFreezer", "Added crew =" + crewName + " seat=" + seat);
                    }
                }
            }
            this.DmpStCrwLst();
            Utilities.Log_Debug("DeepFreezer", "DeSerialize completed");
        }
         * */

        public void DmpStCrwLst()
        {
            Utilities.Log_Debug("DeepFreezer", "DmpStCrwLst");
            if (this.Count() == 0)
                Utilities.Log_Debug("DeepFreezer", "List empty");
            foreach (FrznCrewMbr lst in this)
            {
                Utilities.Log_Debug("DeepFreezer", "Name = " + lst.CrewName + ",Seat= " + lst.SeatIdx + ",VesselID = " + lst.VesselID + ",VesselName = " + lst.VesselName);
            }
        }

    }
}
