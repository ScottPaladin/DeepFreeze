using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using DeepFreezer;


namespace DeepFreezer
{
    class DeepFreezeEvents
    {
        public static DeepFreezeEvents instance = new DeepFreezeEvents();
        public bool eventAdded;

        public DeepFreezeEvents()
        {
            eventAdded = false;
        }

        public void DeepFreezeEventAdd()
        {
            GameEvents.OnVesselRecoveryRequested.Add(this.OnVesselRecoveryRequested);
            GameEvents.onVesselRecoveryProcessing.Add(this.onVesselRecoveryProcessing);
            GameEvents.onVesselRecovered.Add(this.onVesselRecovered);
            eventAdded = true;

        }


        public void OnVesselRecoveryRequested(Vessel vessel)
        {

            if (vessel.FindPartModulesImplementing<DeepFreezer>().Count > 0)
            {
                foreach (DeepFreezer freezer in vessel.FindPartModulesImplementing<DeepFreezer>())
                {
                    freezer.part.CrewCapacity = freezer.StoredCrew.Count;
                    foreach (var crewmember in freezer.StoredCrew)
                    {
                        foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew)
                        {
                            if (kerbal.name == crewmember)
                                freezer.part.AddCrewmember(kerbal);
                            Debug.Log("Crew Added" + kerbal.name);
                        }

                    }
                }
            }
        }
        public void onVesselRecoveryProcessing(ProtoVessel vessel, MissionRecoveryDialog d, float f)
        {
            Debug.Log("OnVesselRecoveryProcessing");
        }
        //public void onVesselRecovered(ProtoVessel vessel) Old Version of onVesselRecovered Known to work
        //{
        //    List<ProtoPartSnapshot> partList = vessel.protoPartSnapshots;
        //    foreach (ProtoPartSnapshot a in partList)
        //    {
        //        List<ProtoPartModuleSnapshot> modules = a.modules;
        //        foreach (ProtoPartModuleSnapshot module in modules)
        //        {

        //            if (module.moduleName == "DeepFreezer")
        //            {
        //                ConfigNode node = module.moduleValues;
        //                string FrozenCrew = node.GetValue("FrozenCrew");
        //                ThawFrozenCrew(FrozenCrew);
        //            }
        //        }
        //    }
        //}

        public void onVesselRecovered(ProtoVessel vessel) //RefactoredOnVessel Recovered to use the new FindProtoModuleVariable, should work for more than just this one job now.
        {
                        List<string> ProtoModuleVariables = FindProtoModuleVariable(vessel, "DeepFreezer", "FrozenCrew");
                        foreach (string FrozenCrew in ProtoModuleVariables)
                        {
                            ThawFrozenCrew(FrozenCrew);
                        }
          
        }
        public List<String> FindProtoModuleVariable(ProtoVessel vessel, String m, String s)
        {
            List<ProtoPartSnapshot> partList = vessel.protoPartSnapshots;
            List<String> result = null;
            foreach (ProtoPartSnapshot a in partList)
            {
                List<ProtoPartModuleSnapshot> modules = a.modules;
                foreach (ProtoPartModuleSnapshot module in modules)
                {

                    if (module.moduleName == m)
                    {
                        ConfigNode node = module.moduleValues;
                        string tempresult = node.GetValue(s);
                        result.Add(tempresult);
                    }
                }
            }
            return result;
        }
        public void ThawFrozenCrew(String FrozenCrew)
        {
            List<String> StoredCrew = FrozenCrew.Split(',').ToList();
            foreach (string frozenkerbal in StoredCrew)
            {
                foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew) //There's probably a more efficient way to find Protocrewmember from the CrewRoster
                {
                    if (kerbal.name == frozenkerbal)
                    {
                        kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Available;
                        ScreenMessages.PostScreenMessage(kerbal.name + " was found in and thawed out.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                }
            }
        }

        //    //foreach (ProtoPartModuleSnapshot module in a)
        //    //{

        //    //}
        //    ProtoPartModuleSnapshot freezer = a.modules.Find(n => n.moduleName == "DeepFreezer");
        //    if (freezer != null)
        //    {
        //        if (freezer.moduleValues.GetValue("FrozenCrew") == null)
        //        {
        //            Debug.Log("Freezer foud but FrozenCrew value is null");
        //        }
        //    }





    }
}
