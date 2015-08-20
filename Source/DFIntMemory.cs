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
 *
 */
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;



namespace DF
{
    class DFIntMemory : MonoBehaviour
    {
        // The DeepFreeze Internal memory class.
        // this class maintains the information in the knownVessels, knownFreezerParts and knownKACalarms dictionaries.
        // It also Executes KAC Alarms when they occur and have DeepFreeze events to execute.
        public static DFIntMemory Instance { get; private set; }
        internal List<DeepFreezer> DpFrzrActVsl = new List<DeepFreezer>();
        internal bool ActVslHasDpFrezr = false;
        internal Guid ActVslID = new Guid();
        internal bool BGPinstalled = false;
        

        protected DFIntMemory()
        {
            Utilities.Log("DFIntMemory", "Constructor");
            Instance = this;
        }
        internal void Awake()
        {
            this.Log_Debug("DFIntMemory Awake");
            KACWrapper.InitKACWrapper();      //KAC Mod      
            if (KACWrapper.APIReady)
                KACWrapper.KAC.onAlarmStateChanged += KAC_onAlarmStateChanged;
            BGPinstalled = DFInstalledMods.IsBGPInstalled;  //Background Processing Mod
            GameEvents.onVesselRename.Add(onVesselRename);
            this.Log_Debug("DFIntMemory end Awake");
        }

        internal void Start()
        {
            this.Log_Debug("DFIntMemory startup");
            ChkUnknownFrozenKerbals();
            ChkActiveFrozenKerbals();
            DeepFreeze.Instance.DFgameSettings.DmpKnownFznKerbals();                                   
        }

        internal void OnDestroy()
        {
            this.Log_Debug("DFIntMemory OnDestroy");
            //destroy the event hook for KAC
            KACWrapper.KAC.onAlarmStateChanged -= KAC_onAlarmStateChanged;
            GameEvents.onVesselRename.Remove(onVesselRename);
            this.Log_Debug("DFIntMemory end OnDestroy");
        }

        internal void FixedUpdate()
        {
            if (Time.timeSinceLevelLoad < 2f) return; //Wait 2 seconds on level load before executing
                                  
            if (HighLogic.LoadedSceneIsFlight)
            {
                //chk if current active vessel Has one or more DeepFreezer modules attached
                try
                {
                    if (FlightGlobals.ActiveVessel.FindPartModulesImplementing<DeepFreezer>().Count() == 0)
                    {
                        ActVslHasDpFrezr = false;
                    }
                    else
                    {
                        ActVslHasDpFrezr = true;
                        DpFrzrActVsl = FlightGlobals.ActiveVessel.FindPartModulesImplementing<DeepFreezer>();
                    }
                    ActVslID = FlightGlobals.ActiveVessel.id;
                }
                catch (Exception ex)
                {
                    this.Log("Failed to set active vessel and Check Freezers");
                    this.Log("Err: " + ex);
                    ActVslHasDpFrezr = false;
                }                
            }
            else
            {
                ActVslHasDpFrezr = false;
            }

            //We check/update Vessel and Part Dictionary in EVERY Game Scene.
            try
            {
                CheckVslUpdate();
            }
            catch (Exception ex)
            {
                this.Log("FixedUpdate failed to update DeepFreeze Internal Vessel Memory");
                this.Log("Err: " + ex);
            }

            //We check/update KAC alarms in EVERY Game Scene.
            try
            {                
                if (KACWrapper.AssemblyExists && KACWrapper.InstanceExists && KACWrapper.APIReady)
                {
                    CheckKACAlarmsUpdate();
                }
            }
            catch (Exception ex)
            {
                this.Log("FixedUpdate failed to update DeepFreeze Internal Alarm Memory");
                this.Log("Err: " + ex);
            }
        }

        internal void ChkUnknownFrozenKerbals()
        {
            // Check the roster list for any unknown dead kerbals (IE: Frozen) that were not in the save file and add them.
            List<ProtoCrewMember> unknownkerbals = HighLogic.CurrentGame.CrewRoster.Unowned.ToList();
            if (unknownkerbals != null)
            {
                this.Log("There are " + unknownkerbals.Count() + " unknownKerbals in the game roster.");
                foreach (ProtoCrewMember CrewMember in unknownkerbals)
                {
                    if (CrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Dead)
                    {
                        if (!DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.ContainsKey(CrewMember.name))
                        {
                            // Update the saved frozen kerbals dictionary
                            KerbalInfo kerbalInfo = new KerbalInfo(Planetarium.GetUniversalTime());
                            kerbalInfo.vesselID = Guid.Empty;
                            kerbalInfo.vesselName = "";
                            kerbalInfo.type = CrewMember.type;
                            kerbalInfo.status = CrewMember.rosterStatus;
                            //kerbalInfo.seatName = "Unknown";
                            kerbalInfo.seatIdx = 0;
                            kerbalInfo.partID = (uint)0;
                            kerbalInfo.experienceTraitName = CrewMember.experienceTrait.Title;
                            try
                            {
                                this.Log("Adding dead unknown kerbal " + CrewMember.name + " AKA FROZEN kerbal to DeepFreeze List");
                                DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Add(CrewMember.name, kerbalInfo);
                            }
                            catch (Exception ex)
                            {
                                this.Log("Add of dead unknown kerbal " + CrewMember.name + " failed " + ex);
                            }
                        }
                    }
                }
            }
        }

        internal void ChkActiveFrozenKerbals()
        {
            // Check the roster list for any crew kerbals that we think are frozen but aren't any more and delete them.
            List<ProtoCrewMember> crewkerbals = HighLogic.CurrentGame.CrewRoster.Crew.ToList();
            if (crewkerbals != null)
            {
                this.Log("There are " + crewkerbals.Count() + " crew Kerbals in the game roster.");
                foreach (ProtoCrewMember CrewMember in crewkerbals)
                {
                    if (DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.ContainsKey(CrewMember.name))
                    {
                        // Remove the saved frozen kerbals dictionary
                        try
                        {
                            this.Log_Debug("Removing crew kerbal " + CrewMember.name + " from DeepFreeze List");
                            DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Remove(CrewMember.name);
                        }
                        catch (Exception ex)
                        {
                            this.Log("Removal of crew kerbal " + CrewMember.name + " from frozen list failed " + ex);
                        }
                    }
                }
            }
        }

        internal void onVesselRename(GameEvents.HostedFromToAction<Vessel, string> fromToAction)
        {
            //Update Vessel name for all frozen kerbals when a rename occurs
            try
            {
                List<string> frznKerbalkeys = new List<string>(DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Keys);
                foreach (string key in frznKerbalkeys)
                {
                    if (DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals[key].vesselID == fromToAction.host.id)
                    {
                        DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals[key].vesselName = fromToAction.to;
                        this.Log_Debug("Updating Frozen Kerbal " + key + ",VesselName=" + DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals[key].vesselName + ",VesselID=" + DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals[key].vesselID);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Log("DeepFreezeGUI.Update failed to set vesselname for Frozen Kerbals");
                this.Log("Err: " + ex);
            }

            //Update Vessel names for all known vessels
            try
            {
                List<Guid> knownvslkeys = new List<Guid>(DeepFreeze.Instance.DFgameSettings.knownVessels.Keys);
                foreach (Guid key in knownvslkeys)
                {
                    if (key == fromToAction.host.id)
                    {
                        DeepFreeze.Instance.DFgameSettings.knownVessels[key].vesselName = fromToAction.to;
                        this.Log_Debug("Updating knownvessel " + key + ",VesselName=" + DeepFreeze.Instance.DFgameSettings.knownVessels[key].vesselName);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Log("DeepFreezeGUI.Update failed to set vesselname for Known vessel");
                this.Log("Err: " + ex);
            }
        }
        
        #region UpdateVesselDictionary

        private void CheckVslUpdate()
        {
            // Called every fixed update from fixedupdate - Check for vessels that have been deleted and remove from Dictionary
            // also updates current active vessel details/settings
            // adds new vessel if current active vessel is not known and updates it's details/settings
            double currentTime = Planetarium.GetUniversalTime();
            List<Vessel> allVessels = FlightGlobals.Vessels;
            var vesselsToDelete = new List<Guid>();
            var partsToDelete = new List<uint>();
            var knownVessels = DeepFreeze.Instance.DFgameSettings.knownVessels;
            this.Log_Debug("CheckVslUpdate start");
            //* Update known vessels.
            foreach (var entry in knownVessels)
            {
                this.Log_Debug("knownvessels id = " + entry.Key + " Name = " + entry.Value.vesselName);
                Guid vesselId = entry.Key;
                VesselInfo vesselInfo = entry.Value;
                Vessel vessel = allVessels.Find(v => v.id == vesselId);
                if (vessel == null)
                {
                    this.Log_Debug("Deleting vessel " + vesselInfo.vesselName + " - vessel does not exist anymore");
                    vesselsToDelete.Add(vesselId);
                    partsToDelete.AddRange(DeepFreeze.Instance.DFgameSettings.knownFreezerParts.Where(e => e.Value.vesselID == vesselId).Select(e => e.Key).ToList());
                    continue;
                }
                if (vessel.loaded)
                {
                    UpdateVesselInfo(vesselInfo, vessel, currentTime);
                    int crewCapacity = UpdateVesselCounts(vesselInfo, vessel, currentTime);
                    if (DeepFreeze.Instance.DFsettings.ECreqdForFreezer && vesselInfo.numFrznCrew > 0)
                    {
                        UpdatePredictedVesselEC(vesselInfo, vessel, currentTime);
                    }
                    if (vessel.FindPartModulesImplementing<DeepFreezer>().Count() == 0)
                    {
                        this.Log_Debug("Deleting vessel " + vesselInfo.vesselName + " - no freezer parts anymore");
                        vesselsToDelete.Add(vesselId);
                        partsToDelete.AddRange(DeepFreeze.Instance.DFgameSettings.knownFreezerParts.Where(e => e.Value.vesselID == vesselId).Select(e => e.Key).ToList());
                        continue;
                    }
                    else
                    {
                        if (DeepFreeze.Instance.DFsettings.ECreqdForFreezer && vesselInfo.numFrznCrew > 0)
                        {
                            UpdatePredictedVesselEC(vesselInfo, vessel, currentTime);
                        }
                        if (vesselInfo.hasextDoor)
                        {
                            // If vessel is Not ActiveVessel and has a Transparent Pod reset the Cryopods.
                            if (FlightGlobals.ActiveVessel != vessel)
                            {
                                List<DeepFreezer> DpFrzrLoadedVsl = new List<DeepFreezer>();
                                DpFrzrLoadedVsl = vessel.FindPartModulesImplementing<DeepFreezer>();
                                foreach (DeepFreezer frzr in DpFrzrLoadedVsl)
                                {
                                    if (frzr.hasExternalDoor)
                                    {
                                        this.Log_Debug("chkvslupdate loaded freezer with door, reset the cryopods");
                                        frzr.resetCryopods(false);
                                    }
                                }
                            }
                        }
                    }
                }
                else //vessel not loaded
                {
                    vesselInfo.hibernating = true;
                }
            }

            // Delete vessels we don't care about any more.
            vesselsToDelete.ForEach(id => DeepFreeze.Instance.DFgameSettings.knownVessels.Remove(id));
            // Delete parts that were part of those vessels.
            partsToDelete.ForEach(id => DeepFreeze.Instance.DFgameSettings.knownFreezerParts.Remove(id));

            // Scan through all in-game vessels and add any new ones we don't know about that have a freezer module.
            foreach (Vessel vessel in allVessels.Where(v => v.loaded))
            {
                if (!knownVessels.ContainsKey(vessel.id) && vessel.FindPartModulesImplementing<DeepFreezer>().Count() > 0)
                {
                    this.Log("New vessel: " + vessel.vesselName + " (" + vessel.id + ")");
                    VesselInfo vesselInfo = new VesselInfo(vessel.vesselName, currentTime);
                    UpdateVesselInfo(vesselInfo, vessel, currentTime);
                    int crewCapacity = UpdateVesselCounts(vesselInfo, vessel, currentTime);
                    knownVessels[vessel.id] = vesselInfo;
                }
            }            
            this.Log_Debug("CheckVslUpdate complete");
        }

        private void UpdatePredictedVesselEC(VesselInfo vesselInfo, Vessel vessel, double currentTime)
        {
            double ECreqdsincelastupdate = 0f;
            int FrznChargeRequired = 0;
            List<KeyValuePair<uint, PartInfo>> DpFrzrUnLoadedVsls = DeepFreeze.Instance.DFgameSettings.knownFreezerParts.Where(p => p.Value.vesselID == vessel.id).ToList();
            foreach (KeyValuePair<uint, PartInfo> frzr in DpFrzrUnLoadedVsls)
            {
                
                    //calculate the predicated time EC will run out                        
                    double timeperiod = Planetarium.GetUniversalTime() - (double)frzr.Value.timeLastElectricity;
                    ProtoPartSnapshot partsnapshot = vessel.protoVessel.protoPartSnapshots.Find(p => p.flightID == frzr.Key);
                    ProtoPartModuleSnapshot modulesnapshot = partsnapshot.modules.Find(m => m.moduleName == "DeepFreezer");
                    string strFrznChargeRequired = modulesnapshot.moduleValues.GetValue("FrznChargeRequired");
                    
                    bool success = Int32.TryParse(strFrznChargeRequired, out FrznChargeRequired);
                    ECreqdsincelastupdate += ((FrznChargeRequired / 60.0f) * timeperiod * frzr.Value.numFrznCrew);                                   
            }
            double ECafterlastupdate = vesselInfo.storedEC - ECreqdsincelastupdate;
            double predictedMinutes = ECafterlastupdate / FrznChargeRequired;  // This probably should be per PART, but for simplicity we will do for the whole vessel            
            vesselInfo.predictedECOut = predictedMinutes * 60;
            this.Log_Debug("UpdatePredictedVesselEC vessel " + vessel.id + " " + vessel.name + " StoredEC=" + vesselInfo.storedEC + " ECreqd=" + ECreqdsincelastupdate + " Prediction Secs=" + vesselInfo.predictedECOut);
        }

        private void UpdateVesselInfo(VesselInfo vesselInfo, Vessel vessel, double currentTime)
        {
            //this.Log_Debug("UpdateVesselInfo " + vesselInfo.vesselName);
            vesselInfo.vesselType = vessel.vesselType;
            vesselInfo.lastUpdate = Planetarium.GetUniversalTime();
            vesselInfo.hibernating = false;
            vesselInfo.hasextDoor = false;
            List<DeepFreezer> DpFrzrLoadedVsl = new List<DeepFreezer>();
            DpFrzrLoadedVsl = vessel.FindPartModulesImplementing<DeepFreezer>();
            foreach (DeepFreezer frzr in DpFrzrLoadedVsl)
            {
                // do we have a known part? If not add it
                PartInfo partInfo;
                if (!DeepFreeze.Instance.DFgameSettings.knownFreezerParts.TryGetValue(frzr.part.flightID, out partInfo))
                {
                    this.Log("New Freezer Part: " + frzr.name + "(" + frzr.part.flightID + ")" + " (" + vessel.id + ")");
                    partInfo = new PartInfo(vessel.id, frzr.name, currentTime);
                    partInfo.hasextDoor = frzr.hasExternalDoor;
                    partInfo.numSeats = frzr.FreezerSize;
                    partInfo.timeLastElectricity = frzr.timeSinceLastECtaken;
                    partInfo.timeLastTempCheck = frzr.timeSinceLastTmpChk;
                    partInfo.deathCounter = frzr.deathCounter;
                    partInfo.tmpdeathCounter = frzr.tmpdeathCounter;
                    partInfo.outofEC = frzr.DFFreezerOutofEC;
                    partInfo.TmpStatus = frzr.DFFrzrTmp;
                    partInfo.cabinTemp = frzr.CabinTemp;
                    foreach (ProtoCrewMember crew in frzr.part.protoModuleCrew)
                    {
                        partInfo.crewMembers.Add(crew.name);
                        partInfo.crewMemberTraits.Add(crew.experienceTrait.Title);
                    }
                    
                    DeepFreeze.Instance.DFgameSettings.knownFreezerParts[frzr.part.flightID] = partInfo;
                }
                else   // Update existing entry
                {
                    partInfo.hasextDoor = frzr.hasExternalDoor;
                    partInfo.numSeats = frzr.FreezerSize;
                    partInfo.timeLastElectricity = frzr.timeSinceLastECtaken;
                    partInfo.timeLastTempCheck = frzr.timeSinceLastTmpChk;
                    partInfo.deathCounter = frzr.deathCounter;
                    partInfo.tmpdeathCounter = frzr.tmpdeathCounter;
                    partInfo.outofEC = frzr.DFFreezerOutofEC;
                    partInfo.TmpStatus = frzr.DFFrzrTmp;
                    partInfo.cabinTemp = frzr.CabinTemp;
                    partInfo.crewMembers.Clear();
                    partInfo.crewMemberTraits.Clear();
                    foreach (ProtoCrewMember crew in frzr.part.protoModuleCrew)
                    {
                        partInfo.crewMembers.Add(crew.name);
                        partInfo.crewMemberTraits.Add(crew.experienceTrait.Title);
                    }
                }
                //now update the knownfreezerpart and any related vesselinfo field
                if (frzr.hasExternalDoor)
                {
                    vesselInfo.hasextDoor = true;
                    break;
                }
            }
        }

        private int UpdateVesselCounts(VesselInfo vesselInfo, Vessel vessel, double currentTime)
        {
            // save current toggles to current vesselinfo
            //this.Log_Debug("UpdateVesselCounts " + vessel.id);
            int crewCapacity = 0;
            vesselInfo.ClearAmounts(); // numCrew = 0; numOccupiedParts = 0; numseats = 0;
            foreach (Part part in vessel.parts)
            {
                DeepFreezer freezer = part.FindModuleImplementing<DeepFreezer>();
                if (freezer != null) // this vessel part does contain a freezer
                {
                    this.Log_Debug("part:" + part.name + " Has Freezer");
                    //first Update the PartInfo counts
                    PartInfo partInfo;
                    if (DeepFreeze.Instance.DFgameSettings.knownFreezerParts.TryGetValue(freezer.part.flightID, out partInfo))
                    {
                        partInfo.numCrew = freezer.part.protoModuleCrew.Count;
                        partInfo.numFrznCrew = freezer.DFIStoredCrewList.Count();
                    }
                    //Now update the VesselInfo counts
                    crewCapacity += freezer.FreezerSize;
                    vesselInfo.numSeats += freezer.FreezerSize;
                    vesselInfo.numCrew += part.protoModuleCrew.Count;
                    vesselInfo.numFrznCrew += freezer.DFIStoredCrewList.Count();
                    //this.Log_Debug("numcrew:" + part.protoModuleCrew.Count + " numfrzncrew:" + freezer.DFIStoredCrewList.Count());
                    if (part.protoModuleCrew.Count > 0 || freezer.DFIStoredCrewList.Count() > 0)
                    {
                        ++vesselInfo.numOccupiedParts;
                    }                    
                }
                else //this vessel part does not contain a freezer
                {                    
                    crewCapacity += part.CrewCapacity;
                    vesselInfo.numSeats += part.CrewCapacity;
                    if (part.protoModuleCrew.Count > 0)
                    {
                        vesselInfo.numCrew += part.protoModuleCrew.Count;
                        ++vesselInfo.numOccupiedParts;
                    }
                    foreach (PartResource res in part.Resources)
                    {
                        if (res.resourceName == "ElectricCharge")
                        {
                            vesselInfo.storedEC += res.amount;
                        }
                    }
                }
            }
            //this.Log_Debug("UpdateVesselCounts " + vessel.id + " complete. numCrew=" + vesselInfo.numCrew + " numfrzncrew=" + vesselInfo.numFrznCrew + " crewcapacity=" + crewCapacity + " numoccupparts=" + vesselInfo.numOccupiedParts);
            return crewCapacity;
        }

        private bool vslHasFreezer(Guid vesselID)
        {
            if (DeepFreeze.Instance.DFgameSettings.knownVessels.ContainsKey(vesselID))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion UpdateVesselDictionary
        
        #region KACAlarms
        
        private void KAC_onAlarmStateChanged(KACWrapper.KACAPI.AlarmStateChangedEventArgs e)
        {
            //This is triggered whenever the KAC API triggers an Alarm event.
            //So we have to check it is an alarm we are interested in and we are only interested in the Triggered EventType.
            this.Log_Debug("KAC Alarm triggered " + e.alarm.Name + "->" + e.eventType);
            /*   EventType = Created,Triggered,Closed,Deleted */
            if (e.eventType == KACWrapper.KACAPI.KACAlarm.AlarmStateEventsEnum.Triggered)
            {
                //Is it an alarm we are tracking? If so, break it down.
                if (DeepFreeze.Instance.DFgameSettings.knownKACAlarms.ContainsKey(e.alarm.ID))
                {
                    this.Log_Debug("Alarm is known so set Execute to true");
                    DeepFreeze.Instance.DFgameSettings.knownKACAlarms[e.alarm.ID].AlarmExecute = true;
                    // Do some messages if the LoadedScene is not flight or the active vessel is not the one we just got the alarm for.
                    //The user has to manually switch to that vessel. Maybe we can automate this in a later version.
                    if (!HighLogic.LoadedSceneIsFlight)
                    {
                        SwitchVslAlarmMsg(DeepFreeze.Instance.DFgameSettings.knownKACAlarms[e.alarm.ID].Name);
                    }
                    else
                    {
                        if (FlightGlobals.ActiveVessel.id != DeepFreeze.Instance.DFgameSettings.knownKACAlarms[e.alarm.ID].VesselID)
                        {
                            SwitchVslAlarmMsg(DeepFreeze.Instance.DFgameSettings.knownKACAlarms[e.alarm.ID].Name);
                        }
                    }
                }
            }
        }

        private void SwitchVslAlarmMsg(string vesselname)
        {
            ScreenMessages.PostScreenMessage("A DeepFreeze Alarm event has occurred. Please Switch to " + vesselname + " to execute.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
        }

        private void CheckKACAlarmsUpdate()
        {
            var alarmstoDelete = new List<string>();
            //iterate all the alarms looking for vessel.ID matches
            foreach (var entry in DeepFreeze.Instance.DFgameSettings.knownKACAlarms)
            {
                this.Log_Debug("knownKACAlarms id = " + entry.Key + " Name = " + entry.Value.Name);
                KACWrapper.KACAPI.KACAlarm alarm = KACWrapper.KAC.Alarms.FirstOrDefault(a => a.ID == entry.Key);
                if (alarm == null && entry.Value.AlarmExecute == false) //Alarm not known to KAC any more and not still executing so delete it.
                {
                    this.Log_Debug("Alarm not known to KAC any more so deleting");
                    alarmstoDelete.Add(entry.Key);
                    continue;
                }
                // Check if Alarm has been modified and no longer has any DeepFreeze association, in which case we delete it.                
                if ((entry.Value.FrzKerbals.Count == 0 && entry.Value.ThwKerbals.Count == 0) || !DeepFreeze.Instance.DFgameSettings.knownVessels.ContainsKey(entry.Value.VesselID)) 
                    // No FREEZE or THAW events in the Notes and unknown vessel, so delete it.
                {
                    this.Log_Debug("Alarm has no THAW FREEZE any more so deleting");
                    alarmstoDelete.Add(entry.Key);
                    continue;
                }
                // Check if alarm has occurred and still executing and try to execute it.
                if (entry.Value.AlarmExecute == true)
                {                    
                    if (entry.Value.ThwKerbals.Count == 0 && entry.Value.FrzKerbals.Count == 0)
                    {
                        // we are all done. Delete the alarm. Do a message.
                        this.Log_Debug("Execution of alarm for vessel " + entry.Value.Name + " is complete");
                        ScreenMessages.PostScreenMessage("DeepFreeze Alarm processing completed.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        alarmstoDelete.Add(entry.Key);
                        continue;
                    }
                    if (!DeepFreeze.Instance.DFgameSettings.knownVessels.ContainsKey(entry.Value.VesselID)) // vessel doesn't exist any more, so just delete the executing alarm.
                    {
                        this.Log_Debug("Executing alarm for vessel " + entry.Value.Name + " deleted as vessel no longer exists");
                        alarmstoDelete.Add(entry.Key);
                        continue;
                    }
                    if (!HighLogic.LoadedSceneIsFlight) //If we aren't in flightmode we can't, so just do message and skip this logic section
                    {
                        this.Log_Debug("Executing alarm for vessel " + entry.Value.Name + " scene is not flight");
                        SwitchVslAlarmMsg(DeepFreeze.Instance.DFgameSettings.knownKACAlarms[entry.Key].Name);
                        continue;
                    }
                    else
                    {
                        if (FlightGlobals.ActiveVessel.id != DeepFreeze.Instance.DFgameSettings.knownKACAlarms[entry.Key].VesselID) // We are in flight, but active vessel is not the one we want
                        {
                            this.Log_Debug("Executing alarm for vessel " + entry.Value.Name + " not the active vessel");
                            SwitchVslAlarmMsg(DeepFreeze.Instance.DFgameSettings.knownKACAlarms[entry.Key].Name);
                            continue;
                        }
                    }
                    // Ok , all that is out of the way, so no we really try to execute it.
                    //THAW first
                    this.Log_Debug("Alarm is executing");
                    if (entry.Value.ThwKerbals.Count > 0)
                    {
                        foreach (string kerbnme in entry.Value.ThwKerbals)
                        {
                            this.Log_Debug("Dump ThwKerbals list entry=" + kerbnme);
                        }
                        // First we find the ThwKerbal part and if they are still on-board frozen.
                        // Then check the part isn't busy already and start the thaw process.
                        bool Found = false;
                        string thwkerbalname = entry.Value.ThwKerbals.FirstOrDefault();
                        this.Log_Debug("Executing alarm for vessel " + entry.Value.Name + " looking to thaw crewmember " + thwkerbalname + " finding the part");
                        foreach (DeepFreezer frzr in DpFrzrActVsl)
                        {
                            // Check if they are in the frozen list for this part or not?
                            if (frzr.DFIStoredCrewList.FirstOrDefault(a => a.CrewName == thwkerbalname) != null)
                            {
                                //They are in this part.
                                Found = true;
                                if (frzr.DFIcrewXferFROMActive || frzr.DFIcrewXferTOActive || frzr.DFIIsFreezeActive || frzr.DFIIsThawActive)
                                {
                                    //part is busy, so we wait.
                                    this.Log_Debug("We found the crewmember and the part, but it is busy, so we wait");
                                    break;
                                }
                                else
                                {
                                    //If we just switched to the vessel we have to wait for the vessel to load.
                                    if (Time.timeSinceLevelLoad < 6f)
                                    {
                                        this.Log_Debug("We found the crewmember and the part, but < 4 secs since level loaded, so we wait");
                                        break;
                                    }
                                    //If we get here, we have found the kerbal, and the part, and it isn't busy. So we THAW!!!!!!
                                    this.Log_Debug("We found the crewmember and the part and it isn't busy, so we THAW!!!");
                                    frzr.beginThawKerbal(thwkerbalname);
                                    entry.Value.ThwKerbals.Remove(thwkerbalname);
                                    foreach (string kerbnme in entry.Value.ThwKerbals)
                                    {
                                        this.Log_Debug("Dump ThwKerbals list entry=" + kerbnme);
                                    }
                                    ModifyKACAlarm(alarm, entry.Value.FrzKerbals, entry.Value.ThwKerbals);
                                    break;
                                }
                            }
                        }
                        if (!Found)
                        {
                            //We didn't find them anywhere. Remove the thaw request.
                            entry.Value.ThwKerbals.Remove(thwkerbalname);
                            this.Log_Debug("We didn't find the thaw kerbal " + thwkerbalname + " anywhere on the vessel so we deleted the request");
                        }
                    }
                    else
                    {
                        if (entry.Value.FrzKerbals.Count > 0)
                        {
                            foreach (string kerbnme in entry.Value.FrzKerbals)
                            {
                                this.Log_Debug("Dump FrzKerbals list entry=" + kerbnme);
                            }
                            // First we find the FrzKerbal part and if they are still on-board
                            // Then check the part isn't busy already and start the freeze process.
                            bool Found = false;
                            List<ProtoCrewMember> vslcrew = FlightGlobals.ActiveVessel.GetVesselCrew();
                            string frzkerbalname = entry.Value.FrzKerbals.FirstOrDefault();
                            ProtoCrewMember crewmember = vslcrew.FirstOrDefault(c => c.name == frzkerbalname);
                            if (crewmember == null)
                            {
                                //They aren't in the vessel any more. So delete this thaw and move on.
                                this.Log_Debug("Executing alarm for vessel " + entry.Value.Name + " looking to freeze crewmember " + frzkerbalname + " but the aren't on-board so move on");
                                entry.Value.FrzKerbals.Remove(frzkerbalname);
                                continue;
                            }
                            //ProtoCrewMember crewmember = FlightGlobals.ActiveVessel.GetVesselCrew().Find(a => a.name == entry.Value.ThwKerbals[0]);
                            this.Log_Debug("Executing alarm for vessel " + entry.Value.Name + " looking to freeze crewmember " + frzkerbalname + " finding the part");
                            foreach (DeepFreezer frzr in DpFrzrActVsl)
                            {
                                if (frzr.part.protoModuleCrew.Contains(crewmember))
                                {
                                    //They are in this part.
                                    Found = true;
                                    if (frzr.DFIcrewXferFROMActive || frzr.DFIcrewXferTOActive || frzr.DFIIsFreezeActive || frzr.DFIIsThawActive)
                                    {
                                        //part is busy, so we wait.
                                        this.Log_Debug("We found the crewmember and the part, but it is busy, so we wait");
                                        break;
                                    }
                                    else
                                    {
                                        //If we just switched to the vessel we have to wait for the vessel to load.
                                        if (Time.timeSinceLevelLoad < 6f)
                                        {
                                            this.Log_Debug("We found the crewmember and the part, but < 4 secs since level loaded, so we wait");
                                            break;
                                        }
                                        //If we get here, we have found the kerbal, and the part, and it isn't busy. So we FREEZE!!!!!!
                                        this.Log_Debug("We found the crewmember and the part and it isn't busy, so we FREEZE!!!");
                                        frzr.beginFreezeKerbal(crewmember);
                                        entry.Value.FrzKerbals.Remove(frzkerbalname);
                                        foreach (string kerbnme in entry.Value.FrzKerbals)
                                        {
                                            this.Log_Debug("Dump FrzKerbals list entry=" + kerbnme);
                                        }
                                        ModifyKACAlarm(alarm, entry.Value.FrzKerbals, entry.Value.ThwKerbals);
                                        break;
                                    }
                                }
                            }
                            if (!Found)
                            {
                                //We didn't find them anywhere. Remove the freeze request.
                                entry.Value.FrzKerbals.Remove(frzkerbalname);
                                this.Log_Debug("We didn't find the freeze kerbal " + frzkerbalname + " anywhere on the vessel so we deleted the request");
                            }
                        }
                    }
                }
            }

            alarmstoDelete.ForEach(id => DeepFreeze.Instance.DFgameSettings.knownKACAlarms.Remove(id));

            //loop through all the KAC alarms. If the alarm is NOT already in the KnownList and the Vessel has a Freezer association that we know of, we add it to the KnownList
            foreach (var entry in KACWrapper.KAC.Alarms)
            {
                if (!DeepFreeze.Instance.DFgameSettings.knownKACAlarms.ContainsKey(entry.ID)) // So we don't already know about it
                {
                    Guid tmpid = new Guid(entry.VesselID);
                    if (DeepFreeze.Instance.DFgameSettings.knownVessels.ContainsKey(tmpid)) // So we do know that vessel does have a Freezer Assocation so we add it to the KnownList
                    {
                        AlarmInfo tempAlarmInfo = new AlarmInfo(entry.Name, tmpid);
                        UpdateKACAlarmInfo(tempAlarmInfo, entry);
                        DeepFreeze.Instance.DFgameSettings.knownKACAlarms.Add(entry.ID, tempAlarmInfo);
                    }
                }
                else  //We do know about it, we update it's details
                {
                    Guid tmpid = new Guid(entry.VesselID);
                    AlarmInfo tempAlarmInfo = new AlarmInfo(entry.Name, tmpid);
                    UpdateKACAlarmInfo(tempAlarmInfo, entry);
                    tempAlarmInfo.AlarmExecute = DeepFreeze.Instance.DFgameSettings.knownKACAlarms[entry.ID].AlarmExecute;
                    DeepFreeze.Instance.DFgameSettings.knownKACAlarms[entry.ID] = tempAlarmInfo;
                }
            }
        }

        private void UpdateKACAlarmInfo(AlarmInfo alarmInfo, KACWrapper.KACAPI.KACAlarm alarm)
        {
            alarmInfo.AlarmType = alarm.AlarmType;
            alarmInfo.AlarmTime = alarm.AlarmTime;
            alarmInfo.AlarmMargin = alarm.AlarmMargin;
            alarmInfo.Notes = alarm.Notes;
            string tmpnotes = ParseKACNotes(alarm.Notes, out alarmInfo.FrzKerbals, out alarmInfo.ThwKerbals);
        }

        internal void ModifyKACAlarm(KACWrapper.KACAPI.KACAlarm alarm, List<string> FrzKbls, List<string> ThwKbls)
        {
            // First we strip out any existing DeepFreeze Events from the notes.
            List<string> _FrzKbls;
            List<string> _ThwKbls;
            string notes = ParseKACNotes(alarm.Notes, out _FrzKbls, out _ThwKbls);
            //Now we add our new DeepFreeze Events back in.
            string newnotes = CreateKACNotes(notes, FrzKbls, ThwKbls);
            this.Log_Debug("ModifyKACAlarm oldnotes= \r\n" + alarm.Notes);
            this.Log_Debug("Stripped out notes= \r\n" + notes);
            this.Log_Debug("NewNotes= \r\n" + newnotes);
            alarm.Notes = newnotes;
        }

        internal string ParseKACNotes(string Notes, out List<String> FrzKbls, out List<string> ThwKbls)
        {
            // Parse out the KAC Alarm Notes. Input is Existing Alarm Notes. Outputs are notes with DeepFreeze events stripped out and a list of Kerbals to Freeze and list of Kerbals to Thaw
            List<string> _frzKbls = new List<string>();
            List<string> _thwKbls = new List<string>();
            string NewNotes = string.Empty;
            string[] noteStrings = Notes.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (noteStrings.Length > 0)
            {
                for (int i = 0; i < noteStrings.Length; i++)
                {
                    //this.Log_Debug("ParseKACNotes Line=" + noteStrings[i]);
                    string[] notelinewords = noteStrings[i].Split(' ');
                    if (notelinewords.Length > 2)
                    {
                        if (notelinewords[0] == "THAW")
                        {
                            string thwkrblname = notelinewords[1] + " " + notelinewords[2];
                            _thwKbls.Add(thwkrblname);
                        }
                        else
                        {
                            if (notelinewords[0] == "FREEZE")
                            {
                                string frzkrblname = notelinewords[1] + " " + notelinewords[2];
                                _frzKbls.Add(frzkrblname);
                            }
                            else
                            {
                                NewNotes += noteStrings[i] + "\r\n";
                            }
                        }
                    }
                    else
                    {
                        NewNotes += noteStrings[i] + "\r\n";
                    }
                }
            }
            FrzKbls = _frzKbls;
            ThwKbls = _thwKbls;
            return NewNotes;
        }

        private string CreateKACNotes(string Notes, List<string> FrzKbls, List<string> ThwKbls)
        {
            // Creates new KAC alarm Notes. Inputs are existing Notes (with any previous DeepFreeze events stripped out), List of Kerbals to Freeze and List of Kerbals to Thaw.
            string NewNotes = string.Empty;
            NewNotes += Notes + "\r\n";
            foreach (string kerbal in FrzKbls)
            {
                NewNotes += "FREEZE " + kerbal + "\r\n";
            }
            foreach (string kerbal in ThwKbls)
            {
                NewNotes += "THAW " + kerbal + "\r\n";
            }
            return NewNotes;
        }

        #endregion KACAlarms

    }
}
