/**
 * DeepFreezerPart.cs
 *
 *
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 *
 *  This file is part of Jamie Leighton's Fork of DeepFreeze. Original Author of DeepFreeze is 'scottpaladin' on the KSP Forums.
 *  The original DeepFreeze was licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 4.0)
 *  This File was part of the original Deepfreeze but has been heavily modified and re-written by Jamie Leighton.
 *  (C) Copyright 2015, Jamie Leighton
 *
 * Which is licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 4.0)
 * creative commons license. See <https://creativecommons.org/licenses/by-nc-sa/4.0/>
 * for full details.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace DF
{
    internal class DeepFreezeEvents
    {
        public static DeepFreezeEvents instance = new DeepFreezeEvents();
        public bool eventAdded;

        public DeepFreezeEvents()
        {
            this.Log_Debug("DeepFreezeEvents");
            eventAdded = false;
        }

        public void DeepFreezeEventAdd()
        {
            this.Log_Debug("DeepFreezeEventAdd");
            GameEvents.OnVesselRecoveryRequested.Add(this.OnVesselRecoveryRequested);
            GameEvents.onVesselRecovered.Add(this.onVesselRecovered);
            GameEvents.onVesselTerminated.Add(this.onVesselTerminated);
            GameEvents.onVesselWillDestroy.Add(this.onVesselWillDestroy);
            eventAdded = true;
            this.Log_Debug("DeepFreezeEventAdd ended");
        }

        public void OnVesselRecoveryRequested(Vessel vessel)
        {
            this.Log_Debug("OnVesselRecoveryRequested");
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
                                kerbal.type = ProtoCrewMember.KerbalType.Crew;
                                kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                                freezer.part.AddCrewmember(kerbal);
                                this.Log_Debug("Crew Added" + kerbal.name);
                            }
                        }
                    }
                }
            }
        }

        public void onVesselRecovered(ProtoVessel vessel)
        {
            this.Log_Debug("onVesselRecovered");
            List<ProtoPartSnapshot> partList = vessel.protoPartSnapshots;
            foreach (ProtoPartSnapshot a in partList)
            {
                //this.Log_Debug(a.partName);
                List<ProtoPartModuleSnapshot> modules = a.modules;
                foreach (ProtoPartModuleSnapshot module in modules)
                {
                    //this.Log_Debug(module.moduleName);
                    if (module.moduleName == "DeepFreezer")
                    {
                        ConfigNode node = module.moduleValues;
                        string FrozenCrew = node.GetValue("FrozenCrew");
                        this.Log_Debug(FrozenCrew);
                        ThawFrozenCrew(FrozenCrew);
                    }
                }
            }
        }

        public void onVesselTerminated(ProtoVessel vessel)
        {
            this.Log_Debug("onVesselTerminated");
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
            this.Log_Debug("onVesselWillDestroy");
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
            this.Log_Debug("ThawFrozenCrew");
            List<String> StoredCrew = FrozenCrew.Split(',').ToList();
            foreach (string frozenkerbal in StoredCrew)
            {
                this.Log_Debug("frozenkerbal =" + frozenkerbal);
                foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew) //There's probably a more efficient way to find Protocrewmember from the CrewRoster
                {
                    if (kerbal.name == frozenkerbal)
                    {
                        kerbal.type = ProtoCrewMember.KerbalType.Crew;
                        kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Available;
                        ScreenMessages.PostScreenMessage(kerbal.name + " was found in and thawed out.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                }
            }
        }

        public void KillFrozenCrew(string FrozenCrew)
        {
            this.Log_Debug("KillFrozenCrew");
            List<String> StoredCrew = FrozenCrew.Split(',').ToList();
            foreach (string frozenkerbal in StoredCrew)
            {
                foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew)
                {
                    if (kerbal.name == frozenkerbal)
                    {
                        this.Log_Debug(kerbal.name + " killed");
                        kerbal.type = ProtoCrewMember.KerbalType.Crew;
                        kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                        if (HighLogic.CurrentGame.Parameters.Difficulty.MissingCrewsRespawn == true)
                        {
                            kerbal.StartRespawnPeriod();
                            this.Log_Debug(kerbal.name + " respawn started.");
                        }
                    }
                }
            }
        }
    }
}