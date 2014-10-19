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
            //Debug.Log("DeepFreezeEvents");
            eventAdded = false;
        }

        public void DeepFreezeEventAdd()
        {
            Debug.Log("DeepFreezeEventAdd");
            GameEvents.OnVesselRecoveryRequested.Add(this.OnVesselRecoveryRequested);
            GameEvents.onVesselRecovered.Add(this.onVesselRecovered);
            GameEvents.onVesselTerminated.Add(this.onVesselTerminated);
            GameEvents.onVesselWillDestroy.Add(this.onVesselWillDestroy);
            eventAdded = true;
        }


        public void OnVesselRecoveryRequested(Vessel vessel)
        {
            Debug.Log("OnVesselRecoveryRequested");
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
                            {
                                freezer.part.AddCrewmember(kerbal);
                                Debug.Log("Crew Added" + kerbal.name);
                            }
                        }

                    }
                }
            }
        }

        public void onVesselRecovered(ProtoVessel vessel)
        {
            //Debug.Log("onVesselRecovered");
            List<ProtoPartSnapshot> partList = vessel.protoPartSnapshots;
            foreach (ProtoPartSnapshot a in partList)
            {
                //Debug.Log(a.partName);
                List<ProtoPartModuleSnapshot> modules = a.modules;
                foreach (ProtoPartModuleSnapshot module in modules)
                {
                    //Debug.Log(module.moduleName);
                    if (module.moduleName == "DeepFreezer")
                    {
                        ConfigNode node = module.moduleValues;
                        string FrozenCrew = node.GetValue("FrozenCrew");
                        Debug.Log(FrozenCrew);
                        ThawFrozenCrew(FrozenCrew);
                    }
                }
            }
        }


        public void onVesselTerminated(ProtoVessel vessel)
        {
            List<ProtoPartSnapshot> partList = vessel.protoPartSnapshots;
            foreach (ProtoPartSnapshot a in partList)
            {
                List<ProtoPartModuleSnapshot> modules = a.modules;
                foreach (ProtoPartModuleSnapshot module in modules)
                {

                    if (module.moduleName == "DeepFreezer")
                    {
                        ConfigNode node = module.moduleValues;
                        string FrozenCrew = node.GetValue("FrozenCrew");
                        KillFrozenCrew(FrozenCrew);
                    }
                }
            }
        }

        public void onVesselWillDestroy(Vessel vessel)
        {
            ProtoVessel pvessel;
            pvessel = vessel.protoVessel;
            List<ProtoPartSnapshot> partList = pvessel.protoPartSnapshots;
            foreach (ProtoPartSnapshot a in partList)
            {
                List<ProtoPartModuleSnapshot> modules = a.modules;
                foreach (ProtoPartModuleSnapshot module in modules)
                {

                    if (module.moduleName == "DeepFreezer")
                    {
                        ConfigNode node = module.moduleValues;
                        string FrozenCrew = node.GetValue("FrozenCrew");
                        KillFrozenCrew(FrozenCrew);
                    }
                }
            }
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
        public void KillFrozenCrew(string FrozenCrew)
        {
            List<String> StoredCrew = FrozenCrew.Split(',').ToList();
            foreach (string frozenkerbal in StoredCrew)
            {
                foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew)
                {
                    if (kerbal.name == frozenkerbal)
                    {
                        Debug.Log(kerbal.name + " killed");
                        if (HighLogic.CurrentGame.Parameters.Difficulty.MissingCrewsRespawn == true)
                        {
                            kerbal.StartRespawnPeriod();
                            Debug.Log(kerbal.name + " respawn started.");
                        }
                    }
                }
            }
        }






    }
}
