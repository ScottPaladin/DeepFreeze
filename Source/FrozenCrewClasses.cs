using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DF
{

    public class FrznCrewMbr
    {
        public string CrewName { get; set; }
        public int Seat { get; set; }

        public FrznCrewMbr(string crewName, int seat)
        {

            this.CrewName = crewName;
            this.Seat = seat;
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
                tmpstring += crew.CrewName + "," + crew.Seat.ToString() + "~";
                Utilities.Log_Debug("DeepFreezer", "Serialized string = " + tmpstring);
            }
            return tmpstring;
        }

        public void Deserialize(string str)
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

                    // Check this crewmember hasn't been thawed from the spacecentre.
                    if (HighLogic.CurrentGame.CrewRoster.Crew.FirstOrDefault(a => a.name == crewName) != null)
                    {
                        // Looks like they have been thawed already.
                        Utilities.Log("DeepFreezer", "On Load of Part Frozen crew was found active (thawed from space center). So Removing from part");
                        Utilities.Log_Debug("DeepFreezer", "DID NOT Add crew =" + crewName + " seat=" + seat);
                    }
                    else
                    {
                        FrznCrewMbr newcrew = new FrznCrewMbr(crewName, seat);
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
                Utilities.Log_Debug("DeepFreezer", "Name = " + lst.CrewName + " Seat= " + lst.Seat);
            }
        }

    }
}
