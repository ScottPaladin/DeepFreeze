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
using UnityEngine;

namespace DF
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    internal class DeepFreezeEvents :MonoBehaviour
    {
        public static DeepFreezeEvents instance = new DeepFreezeEvents();
        public bool eventAdded;        
        private const float costToThawKerbal = 10000f;        

        public DeepFreezeEvents()
        {
            Debug.Log("DeepFreezeEvents Constructor");
            eventAdded = false;
            Debug.Log("DeepFreezeEvnts.Start called");
            if (!DeepFreezeEvents.instance.eventAdded)
            {
                DeepFreezeEvents.instance.DeepFreezeEventAdd();
                Debug.Log("!DeepFreezeEvents.instance.eventAdded");
            }
            else
                Debug.Log("DeepFreezeEvents.instance.eventAdded");
        }

        public void DeepFreezeEventAdd()
        {
            Debug.Log("DeepFreezeEvents DeepFreezeEventAdd");
            //GameEvents.OnVesselRecoveryRequested.Add(this.OnVesselRecoveryRequested);
            GameEvents.onVesselRecovered.Add(this.onVesselRecovered);
            GameEvents.onVesselTerminated.Add(this.onVesselTerminated);
            GameEvents.onVesselWillDestroy.Add(this.onVesselWillDestroy);
            eventAdded = true;
            Debug.Log("DeepFreezeEvents DeepFreezeEventAdd ended");
        }
        /*
        public void OnVesselRecoveryRequested(Vessel vessel)
        {
            Debug.Log("DeepFreezeEvents OnVesselRecoveryRequested");
            if (vessel.FindPartModulesImplementing<DeepFreezer>().Count > 0 && DeepFreeze.Instance.DFsettings.AutoRecoverFznKerbals)
            {
                foreach (DeepFreezer freezer in vessel.FindPartModulesImplementing<DeepFreezer>())
                {
                    freezer.part.CrewCapacity = freezer.StoredCrewList.Count;
                    foreach (var crewmember in freezer.StoredCrewList)
                    {
                        foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew)
                        {
                            if (kerbal.name == crewmember.CrewName)
                            {
                                kerbal.type = ProtoCrewMember.KerbalType.Crew;
                                kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                                freezer.part.AddCrewmember(kerbal);
                                Debug.Log("DeepFreezeEvents Crew Added" + kerbal.name);
                            }
                        }
                    }
                }
            }
        }
        */
        public void onVesselRecovered(ProtoVessel vessel)
        {
            Debug.Log("DeepFreezeEvents onVesselRecovered " + vessel.vesselID);
            this.Log_Debug("AutoRecover is ON");
            foreach (KeyValuePair<string, KerbalInfo> kerbal in DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals)                
            {
                if (kerbal.Value.vesselID == vessel.vesselID)
                {
                    if (DeepFreeze.Instance.DFsettings.AutoRecoverFznKerbals)
                    {
                        Debug.Log("Calling ThawFrozen Crew to thaw FrozenCrew " + kerbal.Key);
                        ThawFrozenCrew(kerbal.Key, vessel.vesselID);
                    }
                    else
                    {
                        Debug.Log("DeepFreeze AutoRecovery of frozen kerbals is set to off. Must be thawed manually.");

                        Debug.Log("DeepFreezeEvents frozenkerbal =" + kerbal.Key);
                        ProtoCrewMember realkerbal = HighLogic.CurrentGame.CrewRoster.Unowned.FirstOrDefault(b => b.name == kerbal.Key);
                        if (realkerbal != null)
                        {
                            realkerbal.type = ProtoCrewMember.KerbalType.Unowned;
                            realkerbal.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                            this.Log_Debug("Kerbal " + realkerbal.name + " " + realkerbal.type + " " + realkerbal.rosterStatus);
                            ScreenMessages.PostScreenMessage(kerbal.Key + " was stored frozen at KSC.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        }
                    }
                }                        
            }                       
        }

        public void onVesselTerminated(ProtoVessel vessel)
        {
            Debug.Log("DeepFreezeEvents onVesselTerminated " + vessel.vesselID);
            foreach (KeyValuePair<string, KerbalInfo> kerbal in DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals)
            {
                if (kerbal.Value.vesselID == vessel.vesselID)
                {                
                    KillFrozenCrew(kerbal.Key);
                }
            }
        }

        public void onVesselWillDestroy(Vessel vessel)
        {
            Debug.Log("DeepFreezeEvents onVesselWillDestroy");
            foreach (KeyValuePair<string, KerbalInfo> kerbal in DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals)
            {
                if (kerbal.Value.vesselID == vessel.id)
                {
                    KillFrozenCrew(kerbal.Key);
                }
            }
        }

        public void ThawFrozenCrew(String FrozenCrew, Guid vesselID)
        {
            Debug.Log("DeepFreezeEvents ThawFrozenCrew");                                                 
            Debug.Log("DeepFreezeEvents frozenkerbal =" + FrozenCrew);
            ProtoCrewMember kerbal = HighLogic.CurrentGame.CrewRoster.Unowned.FirstOrDefault(a => a.name == FrozenCrew);
            if (kerbal != null)
            {
                Vessel vessel = FlightGlobals.Vessels.Find(v => v.id == vesselID);
                if (vessel == null)
                {
                        if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                        {
                            if (Funding.CanAfford(costToThawKerbal))
                            {
                                Funding.Instance.AddFunds(-costToThawKerbal, TransactionReasons.Vessels);
                                this.Log_Debug("Took funds to thaw kerbal");
                            }
                            else
                            {
                                this.Log_Debug("Not enough funds to thaw kerbal");
                                ScreenMessages.PostScreenMessage("Insufficient funds to thaw " + kerbal.name + " at this time.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                                return;
                            }
                        }
                        kerbal.type = ProtoCrewMember.KerbalType.Crew;
                        kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                        this.Log_Debug("Kerbal " + kerbal.name + " " + kerbal.type + " " + kerbal.rosterStatus);
                        kerbal.ArchiveFlightLog();
                        kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Available;
                        this.Log_Debug("Kerbal " + kerbal.name + " " + kerbal.type + " " + kerbal.rosterStatus);
                        ScreenMessages.PostScreenMessage(kerbal.name + " was found and thawed out.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Remove(kerbal.name);
                }
                else
                {
                    this.Log_Debug("Cannot thaw, vessel still exists " + vessel.situation.ToString() + " at " + vessel.mainBody.bodyName);
                    ScreenMessages.PostScreenMessage("Cannot thaw " + kerbal.name + " vessel still exists " + vessel.situation.ToString() + " at " + vessel.mainBody.bodyName, 5.0f, ScreenMessageStyle.UPPER_CENTER); 
                }                                                                                                    
            }            
        }
               
        public void KillFrozenCrew(string FrozenCrew)
        {
            Debug.Log("DeepFreezeEvents KillFrozenCrew");            
            Debug.Log("DeepFreezeEvents frozenkerbal =" + FrozenCrew);
            DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Remove(FrozenCrew);
            ProtoCrewMember kerbal = HighLogic.CurrentGame.CrewRoster.Unowned.FirstOrDefault(a => a.name == FrozenCrew);
            if (kerbal != null)
            {
                Debug.Log("DeepFreezeEvents" + kerbal.name + " killed");
                kerbal.type = ProtoCrewMember.KerbalType.Crew;
                kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                if (HighLogic.CurrentGame.Parameters.Difficulty.MissingCrewsRespawn == true)
                {
                    kerbal.StartRespawnPeriod();
                    Debug.Log("DeepFreezeEvents" + kerbal.name + " respawn started.");
                }
            }
            else
                Debug.Log("DeepFreezeEvents" + kerbal.name + " couldn't find them to kill them.");                
        }
    }
}