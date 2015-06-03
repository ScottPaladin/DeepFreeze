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

        public FrznCrewMbr(string crewName, int seat, Guid vessel)
        {

            this.CrewName = crewName;
            this.SeatIdx = seat;
            this.VesselID = vessel;
        }

    }
    public class FrznCrewList : List<FrznCrewMbr>
    {
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

        public void DmpStCrwLst()
        {
            Utilities.Log_Debug("DeepFreezer", "DmpStCrwLst");
            if (this.Count() == 0)
                Utilities.Log_Debug("DeepFreezer", "List empty");
            foreach (FrznCrewMbr lst in this)
            {
                Utilities.Log_Debug("DeepFreezer", "Name = " + lst.CrewName + " Seat= " + lst.SeatIdx + " VesselID = " + lst.VesselID);
            }
        }

    }
}
