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
using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens;
using PreFlightTests;
using RSTUtils;
using UnityEngine;

namespace DF
{
    internal class DFIntMemory : MonoBehaviour
    {
        // The DeepFreeze Internal memory class.
        // this class maintains the information in the knownVessels, knownFreezerParts and knownKACalarms dictionaries.
        // It also Executes KAC Alarms when they occur and have DeepFreeze events to execute.
        // and controls the Alternate DeepFreeze IVA cameras and Messages when in IVA.
        public static DFIntMemory Instance { get; private set; }

        public class VslFrzrCams
        {
            public Transform FrzrCamTransform { get; set; }
            public InternalModel FrzrCamModel { get; set; }
            public int FrzrCamSeatIndex { get; set; }
            public string FrzrCamPartName { get; set; }
            public DeepFreezer FrzrCamPart { get; set; }

            public VslFrzrCams(Transform frzrcamTransform, InternalModel frzrcamModel, int frzrcamSeatIndex, string frzrcamPartName, DeepFreezer frzrcamPart)
            {
                FrzrCamTransform = frzrcamTransform;
                FrzrCamModel = frzrcamModel;
                FrzrCamSeatIndex = frzrcamSeatIndex;
                FrzrCamPartName = frzrcamPartName;
                FrzrCamPart = frzrcamPart;
            }
        }

        internal List<DeepFreezer> DpFrzrActVsl = new List<DeepFreezer>();
        internal bool ActVslHasDpFrezr;
        internal Guid ActVslID;
        internal bool BGPinstalled;
        internal double invalidKACGUIDItems;
        internal int ActFrzrCamPart = 0;
        internal List<VslFrzrCams> ActFrzrCams = new List<VslFrzrCams>();  //This array of transforms stores the transforms for the cryopod cameras for the active vessel.
        internal int lastFrzrCam;                                       //Index of last frzrcam used.
        private KeyCode keyFrzrCam = (KeyCode)100;                         //Keycode for frzrcam. Loaded from settings.  Default is n
        private KeyCode keyNxtFrzrCam = (KeyCode)110;                       //Keycode for next frzrcam. Loaded from settings. Default is n
        private KeyCode keyPrvFrzrCam = (KeyCode)98;                       //Keycode for previous frzrcam. Loaded from settings. Default is b
        internal ScreenMessage IVAKerbalName, IVAkerbalPart, IVAkerbalPod;  // used for the bottom right screen messages
        //private bool refreshPortraits;                              // set to true after a vessel coupling has occurred, a timer waits 3 secnds then refreshes the portraits cams.
        //private double refreshPortraitsTimer;                          // the timer for the previous var
        private int AllVslsErrorCount;                                  //stop log spam

        protected DFIntMemory()
        {
            Utilities.Log("DFIntMemory Constructor");
            Instance = this;
        }

        private void Awake()
        {
             Utilities.Log_Debug("DFIntMemory Awake");
            KACWrapper.InitKACWrapper();      //KAC Mod
            if (KACWrapper.APIReady)
                KACWrapper.KAC.onAlarmStateChanged += KAC_onAlarmStateChanged;
            BGPinstalled = DFInstalledMods.IsBGPInstalled;  //Background Processing Mod
            GameEvents.onVesselRename.Add(onVesselRename);
            GameEvents.onVesselChange.Add(onVesselChange);
            GameEvents.onVesselLoaded.Add(onVesselLoad);
            GameEvents.onVesselCreate.Add(onVesselCreate);
            GameEvents.onPartCouple.Add(onPartCouple);
            GameEvents.onGUIEngineersReportReady.Add(AddTests);

            try
            {
                keyFrzrCam = (KeyCode)DeepFreeze.Instance.DFsettings.internalFrzrCamCode;
                keyNxtFrzrCam = (KeyCode)DeepFreeze.Instance.DFsettings.internalNxtFrzrCamCode;
                keyPrvFrzrCam = (KeyCode)DeepFreeze.Instance.DFsettings.internalPrvFrzrCamCode;
                if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel != null)
                    onVesselChange(FlightGlobals.ActiveVessel);
            }
            catch (Exception)
            {
                 Utilities.Log_Debug("Invalid Freezer Cam Code in settings. Settings value=" + DeepFreeze.Instance.DFsettings.internalFrzrCamCode);
                keyFrzrCam = (KeyCode)100;
            }
             Utilities.Log_Debug("DFIntMemory end Awake");
        }

        private void Start()
        {
             Utilities.Log_Debug("DFIntMemory startup");
            ChkUnknownFrozenKerbals();
            ChkActiveFrozenKerbals();
            DeepFreeze.Instance.DFgameSettings.DmpKnownFznKerbals();
            resetFreezerCams();
            if (Utilities.GameModeisFlight)
            {
                if (DFInstalledMods.IsTexReplacerInstalled)
                {
                    TRWrapper.InitTRWrapper();
                }
                if (DFInstalledMods.IsUSILSInstalled)
                {
                    USIWrapper.InitUSIWrapper();
                }
                if (DFInstalledMods.IsSMInstalled)
                {
                    SMWrapper.InitSMWrapper();
                }
            }
            if (DFInstalledMods.IsRTInstalled)
            {
                RTWrapper.InitTRWrapper();
            }
        }

        private void OnDestroy()
        {
             Utilities.Log_Debug("DFIntMemory OnDestroy");
            //destroy the event hook for KAC
            if (KACWrapper.APIReady)
                KACWrapper.KAC.onAlarmStateChanged -= KAC_onAlarmStateChanged;
            GameEvents.onVesselRename.Remove(onVesselRename);
            GameEvents.onVesselChange.Remove(onVesselChange);
            GameEvents.onVesselLoaded.Remove(onVesselLoad);
            GameEvents.onVesselCreate.Remove(onVesselCreate);
            GameEvents.onPartCouple.Remove(onPartCouple);
            GameEvents.onGUIEngineersReportReady.Remove(AddTests);   
        }

        private void Update()
        {
            //For some reason when we Freeze a Kerbal and switch to the Internal camera (if in IVA mode) the cameramanager gets stuck.
            //If the user hits the camera mode key while in Internal camera mode this will kick them out to flight
            if (GameSettings.CAMERA_MODE.GetKeyDown() && Utilities.IsInInternal)
            {
                CameraManager.Instance.SetCameraFlight();
            }

            //Check if the FreezerCam references have disappeared and if they have reset.
            if (ActFrzrCams.Any())
            {
                if (ActFrzrCams[lastFrzrCam].FrzrCamTransform == null || ActFrzrCams[lastFrzrCam].FrzrCamModel == null)
                {
                    resetFreezerCams();
                }
            }
            
            if (HighLogic.LoadedSceneIsFlight && ActVslHasDpFrezr)
            {
                /* Not required from KSP 1.1 as RefreskFrozenKerbals in each PartModule will remove the portraits for frozen kerbals.
                //Check if Refresh Portraits Cam is required after two vessels are docked
                if (refreshPortraits)
                {
                    if (Planetarium.GetUniversalTime() - refreshPortraitsTimer > 3)
                    {
                        foreach (Part part in FlightGlobals.ActiveVessel.parts)
                        {
                            if (part.internalModel != null)
                            {
                                foreach (InternalSeat seat in part.internalModel.seats)
                                {
                                    if (seat.kerbalRef != null)
                                    {
                                        try
                                        {
                                            KerbalPortraitGallery.Instance.UnregisterActiveCrew(seat.kerbalRef);
                                            if (!DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.ContainsKey(seat.kerbalRef.crewMemberName))
                                                KerbalPortraitGallery.Instance.RegisterActiveCrew(seat.kerbalRef);
                                        }
                                        catch (Exception)
                                        {
                                            Utilities.Log_Debug("Unregister Portrait on inactive part failed {0}", seat.kerbalRef.crewMemberName);
                                        }
                                    }
                                }
                            }
                        }
                        refreshPortraits = false;
                    }
                }*/

                //If user hits Modifier Key - D switch to freezer cams.
                if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown(keyFrzrCam) && ActFrzrCams.Count > 0 && !Utilities.StockOverlayCamIsOn)
                {
                     Utilities.Log_Debug("User hit InternalCamera modifier keys lastFrzrCam=" + lastFrzrCam);
                    if (Utilities.IsInIVA)
                    {
                         Utilities.Log_Debug("Vessel is in IVA, looking for active kerbal");
                        Kerbal activeKerbal;
                        foreach (DeepFreezer frzr in DpFrzrActVsl)
                        {
                            activeKerbal = CameraManager.Instance.IVACameraActiveKerbal;// frzr.part.FindCurrentKerbal();
                            if (activeKerbal != null)
                            {
                                int CamIndex = -1;
                                CamIndex = ActFrzrCams.FindIndex(a => a.FrzrCamPart == activeKerbal.InPart && a.FrzrCamSeatIndex == activeKerbal.protoCrewMember.seatIdx);
                                if (CamIndex != -1)
                                {
                                    lastFrzrCam = CamIndex;
                                     Utilities.Log_Debug("Vessel was in IVA so Set lastFrzrCam to " + ActFrzrCams[lastFrzrCam].FrzrCamPartName + " " + ActFrzrCams[lastFrzrCam].FrzrCamSeatIndex);
                                    break;
                                }
                            }
                        }
                    }
                    // If we have gone outside the bounds of the camera list, reset to index 0.
                    if (lastFrzrCam > ActFrzrCams.Count)
                    {
                        lastFrzrCam = 0;
                    }
                    // Try to set the freezer cam.
                    if (ActFrzrCams[lastFrzrCam].FrzrCamTransform != null && ActFrzrCams[lastFrzrCam].FrzrCamModel != null)
                    {
                        try
                        {
                            //CameraManager.Instance.SetCameraMode(CameraManager.CameraMode.Internal);
                            CameraManager.Instance.SetCameraInternal(ActFrzrCams[lastFrzrCam].FrzrCamModel, ActFrzrCams[lastFrzrCam].FrzrCamTransform);
                        }
                        catch (Exception ex)
                        {
                            Utilities.Log("Failed to set Internal Camera. " + lastFrzrCam);
                            if (ActFrzrCams[lastFrzrCam].FrzrCamModel == null)  Utilities.Log_Debug("FrzrcamModel is null");
                            if (ActFrzrCams[lastFrzrCam].FrzrCamTransform == null)  Utilities.Log_Debug("FrzrcamTransform is null");
                            Utilities.Log("Err: " + ex);
                            CameraManager.Instance.SetCameraFlight();
                        }
                    }
                    else
                    {
                         Utilities.Log_Debug("lastFrzrCam is null");
                    }
                }

                //If user hits n while we are in internal camera mode switch to the next freezer camera.
                if (Input.GetKeyDown(keyNxtFrzrCam) && Utilities.IsInInternal)
                {
                     Utilities.Log_Debug("User hit InternalCamera nextCamera key lastFrzrCam=" + lastFrzrCam);
                    if ((lastFrzrCam == ActFrzrCams.Count - 1) || (lastFrzrCam > ActFrzrCams.Count))
                    {
                        lastFrzrCam = 0;
                    }
                    else
                    {
                        lastFrzrCam++;
                    }
                     Utilities.Log_Debug("CameraCam = " + lastFrzrCam);

                    try
                    {
                        if (ActFrzrCams[lastFrzrCam].FrzrCamTransform != null && ActFrzrCams[lastFrzrCam].FrzrCamModel != null)
                        {
                            CameraManager.Instance.SetCameraInternal(ActFrzrCams[lastFrzrCam].FrzrCamModel, ActFrzrCams[lastFrzrCam].FrzrCamTransform);
                        }
                        else
                        {
                            if (ActFrzrCams[lastFrzrCam].FrzrCamTransform == null)  Utilities.Log_Debug("lastFrzrCamTransform is null");
                            if (ActFrzrCams[lastFrzrCam].FrzrCamModel == null)  Utilities.Log_Debug("lastFrzrCamModel is null");
                        }
                    }
                    catch (Exception ex)
                    {
                        Utilities.Log("Failed to set Internal Camera.");
                        if (ActFrzrCams[lastFrzrCam].FrzrCamModel == null)  Utilities.Log_Debug("FrzrcamModel is null");
                        if (ActFrzrCams[lastFrzrCam].FrzrCamTransform == null)  Utilities.Log_Debug("FrzrcamTransform is null");
                        Utilities.Log("Err: " + ex);
                        CameraManager.Instance.SetCameraFlight();
                    }
                }

                //If user hits b while we are in internal camera mode switch to the previous freezer camera.
                if (Input.GetKeyDown(keyPrvFrzrCam) && Utilities.IsInInternal)
                {
                     Utilities.Log_Debug("User hit InternalCamera prevCamera key lastFrzrCam=" + lastFrzrCam);
                    if (lastFrzrCam <= 0)
                    {
                        lastFrzrCam = ActFrzrCams.Count - 1;
                    }
                    else
                    {
                        lastFrzrCam--;
                    }
                     Utilities.Log_Debug("CameraCam = " + lastFrzrCam);

                    try
                    {
                        if (ActFrzrCams[lastFrzrCam].FrzrCamTransform != null && ActFrzrCams[lastFrzrCam].FrzrCamModel != null)
                        {
                            CameraManager.Instance.SetCameraInternal(ActFrzrCams[lastFrzrCam].FrzrCamModel, ActFrzrCams[lastFrzrCam].FrzrCamTransform);
                        }
                        else
                        {
                            if (ActFrzrCams[lastFrzrCam].FrzrCamTransform == null)  Utilities.Log_Debug("lastFrzrCamTransform is null");
                            if (ActFrzrCams[lastFrzrCam].FrzrCamModel == null)  Utilities.Log_Debug("lastFrzrCamModel is null");
                        }
                    }
                    catch (Exception ex)
                    {
                        Utilities.Log("Failed to set Internal Camera.");
                        if (ActFrzrCams[lastFrzrCam].FrzrCamModel == null)  Utilities.Log_Debug("FrzrcamModel is null");
                        if (ActFrzrCams[lastFrzrCam].FrzrCamTransform == null)  Utilities.Log_Debug("FrzrcamTransform is null");
                        Utilities.Log("Err: " + ex);
                        CameraManager.Instance.SetCameraFlight();
                    }
                }

                if (IVAKerbalName != null) ScreenMessages.RemoveMessage(IVAKerbalName);
                if (IVAkerbalPart != null) ScreenMessages.RemoveMessage(IVAkerbalPart);
                if (IVAkerbalPod != null)  ScreenMessages.RemoveMessage(IVAkerbalPod);
                if (Utilities.IsInInternal && ActFrzrCams.Count > 0)
                {
                    // Set Top Left messages for FreezerCam mode

                    // See if there is a kerbal seated/frozen in that seat get their reference
                    IVAkerbalPod = new ScreenMessage("Pod:" + ActFrzrCams[lastFrzrCam].FrzrCamSeatIndex, 1, ScreenMessageStyle.UPPER_LEFT);
                    IVAkerbalPod.color = Color.white;
                    ScreenMessages.PostScreenMessage(IVAkerbalPod);
                    IVAkerbalPart = new ScreenMessage(ActFrzrCams[lastFrzrCam].FrzrCamPartName, 1, ScreenMessageStyle.UPPER_LEFT);
                    IVAkerbalPart.color = Color.white;
                    ScreenMessages.PostScreenMessage(IVAkerbalPart);
                    
                    string kerbalname;
                    try
                    {
                        if (ActFrzrCams[lastFrzrCam].FrzrCamPart.part.internalModel.seats[ActFrzrCams[lastFrzrCam].FrzrCamSeatIndex - 1].kerbalRef != null)
                        {
                            kerbalname = ActFrzrCams[lastFrzrCam].FrzrCamPart.part.internalModel.seats[ActFrzrCams[lastFrzrCam].FrzrCamSeatIndex - 1].kerbalRef.crewMemberName;
                        }
                        else
                        {
                            Utilities.Log_Debug("Kerbalref is null");
                            kerbalname = string.Empty;
                        }
                    }
                    catch (Exception)
                    {
                        kerbalname = string.Empty;
                    }
                    IVAKerbalName = new ScreenMessage(kerbalname, 1, ScreenMessageStyle.UPPER_LEFT);
                    IVAKerbalName.color = Color.white;
                    ScreenMessages.PostScreenMessage(IVAKerbalName);
                }
            }
        }

        private void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor || Time.timeSinceLevelLoad < 5f) return; //Wait 5 seconds on level load before executing

            //Check if the active vessel has changed and if so, process.
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (FlightGlobals.ActiveVessel.id != ActVslID)
                {
                    onVesselChange(FlightGlobals.ActiveVessel);
                }
            }
            //We check/update kerbal Dictionary for comatose kerbals in EVERY Game Scene.
            try
            {
                if (DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Any())
                    CheckComaUpdate();
            }
            catch (Exception ex)
            {
                Utilities.Log("FixedUpdate failed to update DeepFreeze Internal Comatose Kerbals Memory");
                Utilities.Log("Err: " + ex);
            }

            //We check/update Vessel and Part Dictionary in EVERY Game Scene.
            try
            {
                CheckVslUpdate();
            }
            catch (Exception ex)
            {
                Utilities.Log("FixedUpdate failed to update DeepFreeze Internal Vessel Memory");
                Utilities.Log("Err: " + ex);
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
                Utilities.Log("FixedUpdate failed to update DeepFreeze Internal Alarm Memory");
                Utilities.Log("Err: " + ex);
            }
        }

        private void CheckComaUpdate()
        {
            // Check the knownfrozenkerbals for any tourists kerbals (IE: Comatose) if their time is up and reset them if it is.
            var keysToDelete = new List<string>();
            List<KeyValuePair<string, KerbalInfo>> comaKerbals = DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Where(e => e.Value.type == ProtoCrewMember.KerbalType.Tourist).ToList();
            foreach (KeyValuePair<string, KerbalInfo> comaKerbal in comaKerbals)
            {
                if (comaKerbal.Value.type == ProtoCrewMember.KerbalType.Tourist)
                {
                    if (Planetarium.GetUniversalTime() - comaKerbal.Value.lastUpdate > DeepFreeze.Instance.DFsettings.comatoseTime) // Is time up?
                    {
                        ProtoCrewMember crew = HighLogic.CurrentGame.CrewRoster.Tourist.FirstOrDefault(a => a.name == comaKerbal.Key);
                        if (crew != null)
                        {
                            DeepFreeze.Instance.setComatoseKerbal(crew, ProtoCrewMember.KerbalType.Crew);
                            keysToDelete.Add(comaKerbal.Key);
                        }
                        else
                        {
                            Utilities.Log("Unable to set comatose crew member " + comaKerbal.Key + " back to crew status.");
                        }
                    }
                }
            }
            keysToDelete.ForEach(id => DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Remove(id));
        }

        private void ChkUnknownFrozenKerbals()
        {
            // Check the roster list for any unknown dead kerbals (IE: Frozen) that were not in the save file and add them.
            List<ProtoCrewMember> unknownkerbals = HighLogic.CurrentGame.CrewRoster.Unowned.ToList();
            if (unknownkerbals != null)
            {
                Utilities.Log("There are " + unknownkerbals.Count() + " unknownKerbals in the game roster.");
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
                            kerbalInfo.partID = 0;
                            kerbalInfo.experienceTraitName = CrewMember.experienceTrait.Title;
                            try
                            {
                                Utilities.Log("Adding dead unknown kerbal " + CrewMember.name + " AKA FROZEN kerbal to DeepFreeze List");
                                DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Add(CrewMember.name, kerbalInfo);
                            }
                            catch (Exception ex)
                            {
                                Utilities.Log("Add of dead unknown kerbal " + CrewMember.name + " failed " + ex);
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
                Utilities.Log("There are " + crewkerbals.Count() + " crew Kerbals in the game roster.");
                foreach (ProtoCrewMember CrewMember in crewkerbals)
                {
                    if (DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.ContainsKey(CrewMember.name))
                    {
                        // Remove the saved frozen kerbals dictionary
                        try
                        {
                             Utilities.Log_Debug("Removing crew kerbal " + CrewMember.name + " from DeepFreeze List");
                            DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Remove(CrewMember.name);
                        }
                        catch (Exception ex)
                        {
                            Utilities.Log("Removal of crew kerbal " + CrewMember.name + " from frozen list failed " + ex);
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
                         Utilities.Log_Debug("Updating Frozen Kerbal " + key + ",VesselName=" + DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals[key].vesselName + ",VesselID=" + DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals[key].vesselID);
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.Log("DeepFreezeGUI.Update failed to set vesselname for Frozen Kerbals");
                Utilities.Log("Err: " + ex);
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
                         Utilities.Log_Debug("Updating knownvessel " + key + ",VesselName=" + DeepFreeze.Instance.DFgameSettings.knownVessels[key].vesselName);
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.Log("DeepFreezeGUI.Update failed to set vesselname for Known vessel");
                Utilities.Log("Err: " + ex);
            }
        }

        internal void onVesselLoad(Vessel vessel)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                 Utilities.Log_Debug("OnVesselLoad activevessel " + FlightGlobals.ActiveVessel.id + " parametervesselid " + vessel.id);
                resetFreezerCams();
                onVesselChange(vessel);
            }
        }

        internal void onVesselCreate(Vessel vessel)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                 Utilities.Log_Debug("OnVesselCreate activevessel " + FlightGlobals.ActiveVessel.id + " parametervesselid " + vessel.id);
                List<DeepFreezer> TmpDpFrzrActVsl = vessel.FindPartModulesImplementing<DeepFreezer>();
                foreach (DeepFreezer frzr in TmpDpFrzrActVsl)
                {
                    //Find the part in KnownFreezerParts and update the GUID
                    PartInfo partInfo;
                    if (DeepFreeze.Instance.DFgameSettings.knownFreezerParts.TryGetValue(frzr.part.flightID, out partInfo))
                    {
                        partInfo.vesselID = vessel.id;
                    }
                    //Iterate frozen kerbals in KnownFrozenKerbals and update the GUID
                    foreach (KeyValuePair<string, KerbalInfo> frznKerbals in DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals)
                    {
                        if (frznKerbals.Value.partID == frzr.part.flightID)
                        {
                            frznKerbals.Value.vesselID = vessel.id;
                            frznKerbals.Value.vesselName = vessel.vesselName;
                        }
                    }
                    //Update the Frzr Parts internal frozenkerbals list GUID
                    foreach (FrznCrewMbr storedCrew in frzr.DFIStoredCrewList) 
                    {
                        storedCrew.VesselID = vessel.id;
                    }
                }
            }
        }

        internal void onPartCouple(GameEvents.FromToAction<Part, Part> fromToAction)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                 Utilities.Log_Debug("OnPartCouple activevessel " + FlightGlobals.ActiveVessel.id + " fromPart " + fromToAction.from.flightID + "(" + fromToAction.from.vessel.id + ") toPart " + fromToAction.to.flightID + "(" + fromToAction.to.vessel.id + ")");
                List<DeepFreezer> TmpDpFrzrActVsl = fromToAction.from.vessel.FindPartModulesImplementing<DeepFreezer>();
                foreach (DeepFreezer frzr in TmpDpFrzrActVsl)
                {
                    //Find the part in KnownFreezerParts and update the GUID
                    PartInfo partInfo;
                    if (DeepFreeze.Instance.DFgameSettings.knownFreezerParts.TryGetValue(frzr.part.flightID, out partInfo))
                    {
                        partInfo.vesselID = fromToAction.to.vessel.id;
                    }
                    //Iterate frozen kerbals in KnownFrozenKerbals and update the GUID
                    foreach (KeyValuePair<string, KerbalInfo> frznKerbals in DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals)
                    {
                        if (frznKerbals.Value.partID == frzr.part.flightID)
                        {
                            frznKerbals.Value.vesselID = fromToAction.to.vessel.id;
                            frznKerbals.Value.vesselName = fromToAction.to.vessel.vesselName;
                        }
                    }
                    //Update the Frzr Parts internal frozenkerbals list GUID
                    foreach (FrznCrewMbr storedCrew in frzr.DFIStoredCrewList)
                    {
                        storedCrew.VesselID = fromToAction.to.vessel.id;
                    }
                }
                //Now resetFrozenKerbals in the parts
                foreach (DeepFreezer frzr in TmpDpFrzrActVsl)
                {
                    frzr.resetFrozenKerbals();
                }
                //refreshPortraits = true;
                //refreshPortraitsTimer = Planetarium.GetUniversalTime();
            }
        }

        internal void onVesselChange(Vessel vessel)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                 Utilities.Log_Debug("OnVesselChange activevessel " + FlightGlobals.ActiveVessel.name + "(" + FlightGlobals.ActiveVessel.id + ") parametervessel " + vessel.name + "(" + vessel.id + ")");
                //chk if current active vessel Has one or more DeepFreezer modules attached
                try
                {
                    if (!FlightGlobals.ActiveVessel.FindPartModulesImplementing<DeepFreezer>().Any())
                    {
                        ActVslHasDpFrezr = false;
                    }
                    else
                    {
                        ActVslHasDpFrezr = true;
                        DpFrzrActVsl = vessel.FindPartModulesImplementing<DeepFreezer>();
                        //Check if vessel id has changed or last freezer cam transforms is now null, reset the freezer cams.
                        if (ActVslID != vessel.id || ActFrzrCams.Any())
                        {
                            foreach (DeepFreezer frzr in DpFrzrActVsl)
                            {
                                if (frzr.partHasInternals)
                                {
                                    resetFreezerCams();
                                    break;
                                }
                            }
                        }
                    }
                    ActVslID = FlightGlobals.ActiveVessel.id;
                     Utilities.Log_Debug("OnVesselChange ActVslID " + ActVslID + " HasFreezers " + ActVslHasDpFrezr + " FreezerCams Listed " + ActFrzrCams.Count());
                }
                catch (Exception ex)
                {
                    Utilities.Log("Failed to set active vessel and Check Freezers");
                    Utilities.Log("Err: " + ex);
                    //ActVslHasDpFrezr = false;
                }
            }
            else
            {
                ActVslHasDpFrezr = false;
            }
        }

        internal void AddTests()
        {
             Utilities.Log_Debug("Adding DF Engineer Test");
            IDesignConcern DFtest = new DFEngReport();
            EngineersReport.Instance.AddTest(DFtest);
        }

        private void resetFreezerCams()
        {
            try
            {
                ActFrzrCams.Clear();
                lastFrzrCam = 0;
                 Utilities.Log_Debug("ActVslHasDpFrezer " + ActVslHasDpFrezr + " #ofFrzrs " + DpFrzrActVsl.Count());
                foreach (DeepFreezer Frzr in DpFrzrActVsl)
                {
                    if (Frzr.part.internalModel != null)
                    {
                        for (int i = 0; i < Frzr.FreezerSize; i++)
                        {
                            string frzrcamname = "FrzCam" + (i + 1);
                            Transform frzrcam = Frzr.part.internalModel.FindModelComponent<Transform>(frzrcamname);
                            if (frzrcam != null)
                            {
                                VslFrzrCams vslfrzrcam = new VslFrzrCams(frzrcam, Frzr.part.internalModel, i + 1, Frzr.part.name.Substring(8, 1) == "R" ? Frzr.part.name.Substring(0, 9) : Frzr.part.name.Substring(0, 8), Frzr);
                                ActFrzrCams.Add(vslfrzrcam);
                                 Utilities.Log_Debug("Adding ActFrzrCams " + vslfrzrcam.FrzrCamModel.internalName + " " + vslfrzrcam.FrzrCamTransform.name);
                            }
                            else
                            {
                                 Utilities.Log_Debug("Unable to find FrzCam transform " + frzrcamname);
                            }
                        }
                    }
                    else
                    {
                         Utilities.Log_Debug("Frzr " + Frzr.name + " internalmodel is null");
                    }
                }
            }
            catch (Exception)
            {
                Utilities.Log("Failed to resetFreezerCams");
                // Utilities.Log("Err: " + ex);
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
             Utilities.Log_Debug("CheckVslUpdate start");
            //* Update known vessels.
            foreach (var entry in knownVessels)
            {
                if (AllVslsErrorCount > 0 && AllVslsErrorCount < 5)
                {
                    Utilities.Log("knownvessels id = " + entry.Key + " Name = " + entry.Value.vesselName);
                }
                 Utilities.Log_Debug("knownvessels id = " + entry.Key + " Name = " + entry.Value.vesselName);
                Guid vesselId = entry.Key;
                VesselInfo vesselInfo = entry.Value;
                Vessel vessel = null;
                try
                {
                    if (AllVslsErrorCount < 5)
                        vessel = allVessels.Find(v => v.id == vesselId);
                    else
                        continue;
                }
                catch (Exception ex)
                {
                    AllVslsErrorCount++;
                    DeepFreeze.Instance.DFgameSettings.DmpKnownVessels();
                    if (allVessels == null)
                    {
                        Utilities.Log("FlightGlobals.Vessels = NULL but never should be");
                    }
                    else
                    {
                        if (allVessels.Count == 0)
                        {
                             Utilities.Log("FlightGlobals.Vessels.Count = 0");
                        }
                        else
                        {
                            foreach (Vessel vsl in allVessels)
                            {
                                 Utilities.Log("Vessel " + vsl.id + " name = " + vsl.name);
                            }
                        }
                    }
                     Utilities.Log("Exception: " + ex);
                    if (entry.Value.numFrznCrew == 0)
                    {
                         Utilities.Log("Removing entry as vessel has no frozen crew");
                        vesselsToDelete.Add(vesselId);
                        partsToDelete.AddRange(DeepFreeze.Instance.DFgameSettings.knownFreezerParts.Where(e => e.Value.vesselID == vesselId).Select(e => e.Key).ToList());
                        continue;
                    }
                }                
                if (vessel == null)
                {
                     Utilities.Log_Debug("Deleting vessel " + vesselInfo.vesselName + " - vessel does not exist anymore");
                    vesselsToDelete.Add(vesselId);
                    partsToDelete.AddRange(DeepFreeze.Instance.DFgameSettings.knownFreezerParts.Where(e => e.Value.vesselID == vesselId).Select(e => e.Key).ToList());
                    continue;
                }
                if (vessel.loaded)
                {
                    UpdateVesselInfo(vesselInfo, vessel, currentTime);
                    int crewCapacity = UpdateVesselCounts(vesselInfo, vessel, currentTime);
                    if (!vessel.FindPartModulesImplementing<DeepFreezer>().Any())
                    {
                         Utilities.Log_Debug("Deleting vessel " + vesselInfo.vesselName + " - no freezer parts anymore");
                        vesselsToDelete.Add(vesselId);
                        partsToDelete.AddRange(DeepFreeze.Instance.DFgameSettings.knownFreezerParts.Where(e => e.Value.vesselID == vesselId).Select(e => e.Key).ToList());
                    }
                    else
                    {
                        if (DeepFreeze.Instance.DFsettings.ECreqdForFreezer && vesselInfo.numFrznCrew > 0)
                        {
                            UpdatePredictedVesselEC(vesselInfo, vessel, currentTime);
                        }
                        if (vesselInfo.hasextDoor || vesselInfo.hasextPod)
                        {
                            // If vessel is Not ActiveVessel and has a Transparent Pod reset the Cryopods.
                            if (FlightGlobals.ActiveVessel != vessel)
                            {
                                List<DeepFreezer> DpFrzrLoadedVsl = new List<DeepFreezer>();
                                DpFrzrLoadedVsl = vessel.FindPartModulesImplementing<DeepFreezer>();
                                foreach (DeepFreezer frzr in DpFrzrLoadedVsl)
                                {
                                    if (frzr.ExternalDoorActive || frzr.isPodExternal)
                                    {
                                         Utilities.Log_Debug("chkvslupdate loaded freezer with door or external pod, reset the cryopods");
                                        frzr.resetCryopods(false);
                                    }
                                }
                            }
                        }
                    }
                }
                else //vessel not loaded
                {
                    //if (!DFInstalledMods.IsBGPInstalled || !Utilities.timewarpIsValid(5))
                    //{
                    UpdatePredictedVesselEC(vesselInfo, vessel, currentTime);
                    //}
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
                if (!knownVessels.ContainsKey(vessel.id) && vessel.FindPartModulesImplementing<DeepFreezer>().Any())
                {
                    Utilities.Log("New vessel: " + vessel.vesselName + " (" + vessel.id + ")");
                    VesselInfo vesselInfo = new VesselInfo(vessel.vesselName, currentTime);
                    UpdateVesselInfo(vesselInfo, vessel, currentTime);
                    int crewCapacity = UpdateVesselCounts(vesselInfo, vessel, currentTime);
                    knownVessels[vessel.id] = vesselInfo;
                }
            }
             Utilities.Log_Debug("CheckVslUpdate complete");
        }

        private void UpdatePredictedVesselEC(VesselInfo vesselInfo, Vessel vessel, double currentTime)
        {
            double ECreqdsincelastupdate = 0f;
            int frznChargeRequired = 0;
            List<KeyValuePair<uint, PartInfo>> DpFrzrVsl = DeepFreeze.Instance.DFgameSettings.knownFreezerParts.Where(p => p.Value.vesselID == vessel.id).ToList();
            foreach (KeyValuePair<uint, PartInfo> frzr in DpFrzrVsl)
            {
                //calculate the predicated time EC will run out
                double timeperiod = Planetarium.GetUniversalTime() - frzr.Value.timeLastElectricity;
                frznChargeRequired = (int)frzr.Value.frznChargeRequired;
                ECreqdsincelastupdate += frznChargeRequired / 60.0f * timeperiod * frzr.Value.numFrznCrew;
                frzr.Value.deathCounter = currentTime;
                // Utilities.Log_Debug("predicted EC part " + frzr.Value.vesselID + " " + frzr.Value.PartName + " FrznChargeRequired " + frznChargeRequired + " timeperiod " + timeperiod + " #frzncrew " + frzr.Value.numFrznCrew);
            }
            double ECafterlastupdate = vesselInfo.storedEC - ECreqdsincelastupdate;
            double predictedMinutes = ECafterlastupdate / frznChargeRequired;  // This probably should be per PART, but for simplicity we will do for the whole vessel
            vesselInfo.predictedECOut = predictedMinutes * 60;
            // Utilities.Log_Debug("UpdatePredictedVesselEC vessel " + vessel.id + " " + vessel.name + " StoredEC=" + vesselInfo.storedEC + " ECreqd=" + ECreqdsincelastupdate + " Prediction Secs=" + vesselInfo.predictedECOut);
            // Utilities.Log_Debug("ECafterlastupdate " + ECafterlastupdate + " FrznChargeRequired " + frznChargeRequired + " predictedMinutes " + predictedMinutes);
        }

        private void UpdateVesselInfo(VesselInfo vesselInfo, Vessel vessel, double currentTime)
        {
            // Utilities.Log_Debug("UpdateVesselInfo " + vesselInfo.vesselName);
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
                    Utilities.Log("New Freezer Part: " + frzr.name + "(" + frzr.part.flightID + ")" + " (" + vessel.id + ")");
                    partInfo = new PartInfo(vessel.id, frzr.name, currentTime);
                    partInfo.hasextDoor = frzr.ExternalDoorActive;
                    partInfo.hasextPod = frzr.isPodExternal;
                    partInfo.numSeats = frzr.FreezerSize;
                    partInfo.timeLastElectricity = frzr.timeSinceLastECtaken;
                    partInfo.frznChargeRequired = frzr.FrznChargeRequired;
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
                    partInfo.hasextDoor = frzr.ExternalDoorActive;
                    partInfo.hasextPod = frzr.isPodExternal;
                    partInfo.numSeats = frzr.FreezerSize;
                    partInfo.timeLastElectricity = frzr.timeSinceLastECtaken;
                    partInfo.frznChargeRequired = frzr.FrznChargeRequired;
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
                if (frzr.ExternalDoorActive)
                    vesselInfo.hasextDoor = true;
                if (frzr.isPodExternal)
                    vesselInfo.hasextPod = true;
            }
        }

        private int UpdateVesselCounts(VesselInfo vesselInfo, Vessel vessel, double currentTime)
        {
            // save current toggles to current vesselinfo
            // Utilities.Log_Debug("UpdateVesselCounts " + vessel.id);
            int crewCapacity = 0;
            vesselInfo.ClearAmounts(); // numCrew = 0; numOccupiedParts = 0; numseats = 0;
            foreach (Part part in vessel.parts)
            {
                DeepFreezer freezer = part.FindModuleImplementing<DeepFreezer>();
                if (freezer != null) // this vessel part does contain a freezer
                {
                     Utilities.Log_Debug("part:" + part.name + " Has Freezer");
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
                    // Utilities.Log_Debug("numcrew:" + part.protoModuleCrew.Count + " numfrzncrew:" + freezer.DFIStoredCrewList.Count());
                    if (part.protoModuleCrew.Count > 0 || freezer.DFIStoredCrewList.Any())
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
            // Utilities.Log_Debug("UpdateVesselCounts " + vessel.id + " complete. numCrew=" + vesselInfo.numCrew + " numfrzncrew=" + vesselInfo.numFrznCrew + " crewcapacity=" + crewCapacity + " numoccupparts=" + vesselInfo.numOccupiedParts);
            return crewCapacity;
        }

        private bool vslHasFreezer(Guid vesselID)
        {
            if (DeepFreeze.Instance.DFgameSettings.knownVessels.ContainsKey(vesselID))
            {
                return true;
            }
            return false;
        }

        #endregion UpdateVesselDictionary

        #region KACAlarms

        private void KAC_onAlarmStateChanged(KACWrapper.KACAPI.AlarmStateChangedEventArgs e)
        {
            //This is triggered whenever the KAC API triggers an Alarm event.
            //So we have to check it is an alarm we are interested in and we are only interested in the Triggered EventType.
             Utilities.Log_Debug("KAC Alarm triggered " + e.alarm.Name + "->" + e.eventType);
            /*   EventType = Created,Triggered,Closed,Deleted */
            if (e.eventType == KACWrapper.KACAPI.KACAlarm.AlarmStateEventsEnum.Triggered)
            {
                //Is it an alarm we are tracking? If so, break it down.
                if (DeepFreeze.Instance.DFgameSettings.knownKACAlarms.ContainsKey(e.alarm.ID))
                {
                     Utilities.Log_Debug("Alarm is known so set Execute to true");
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
                 Utilities.Log_Debug("knownKACAlarms id = " + entry.Key + " Name = " + entry.Value.Name);
                KACWrapper.KACAPI.KACAlarm alarm = KACWrapper.KAC.Alarms.FirstOrDefault(a => a.ID == entry.Key);
                if (alarm == null && entry.Value.AlarmExecute == false) //Alarm not known to KAC any more and not still executing so delete it.
                {
                     Utilities.Log_Debug("Alarm not known to KAC any more so deleting");
                    alarmstoDelete.Add(entry.Key);
                    continue;
                }
                // Check if Alarm has been modified and no longer has any DeepFreeze association, in which case we delete it.
                if ((entry.Value.FrzKerbals.Count == 0 && entry.Value.ThwKerbals.Count == 0) || !DeepFreeze.Instance.DFgameSettings.knownVessels.ContainsKey(entry.Value.VesselID))
                // No FREEZE or THAW events in the Notes and unknown vessel, so delete it.
                {
                     Utilities.Log_Debug("Alarm has no THAW FREEZE any more so deleting");
                    alarmstoDelete.Add(entry.Key);
                    continue;
                }
                // Check if alarm has occurred and still executing and try to execute it.
                if (entry.Value.AlarmExecute)
                {
                    if (entry.Value.ThwKerbals.Count == 0 && entry.Value.FrzKerbals.Count == 0)
                    {
                        // we are all done. Delete the alarm. Do a message.
                         Utilities.Log_Debug("Execution of alarm for vessel " + entry.Value.Name + " is complete");
                        ScreenMessages.PostScreenMessage("DeepFreeze Alarm processing completed.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        alarmstoDelete.Add(entry.Key);
                        continue;
                    }
                    if (!DeepFreeze.Instance.DFgameSettings.knownVessels.ContainsKey(entry.Value.VesselID)) // vessel doesn't exist any more, so just delete the executing alarm.
                    {
                         Utilities.Log_Debug("Executing alarm for vessel " + entry.Value.Name + " deleted as vessel no longer exists");
                        alarmstoDelete.Add(entry.Key);
                        continue;
                    }
                    if (!HighLogic.LoadedSceneIsFlight) //If we aren't in flightmode we can't, so just do message and skip this logic section
                    {
                         Utilities.Log_Debug("Executing alarm for vessel " + entry.Value.Name + " scene is not flight");
                        SwitchVslAlarmMsg(DeepFreeze.Instance.DFgameSettings.knownKACAlarms[entry.Key].Name);
                        continue;
                    }
                    if (FlightGlobals.ActiveVessel.id != DeepFreeze.Instance.DFgameSettings.knownKACAlarms[entry.Key].VesselID) // We are in flight, but active vessel is not the one we want
                    {
                        Utilities.Log_Debug("Executing alarm for vessel " + entry.Value.Name + " not the active vessel");
                        SwitchVslAlarmMsg(DeepFreeze.Instance.DFgameSettings.knownKACAlarms[entry.Key].Name);
                        continue;
                    }
                    // Ok , all that is out of the way, so no we really try to execute it.
                    //THAW first
                     Utilities.Log_Debug("Alarm is executing");
                    if (entry.Value.ThwKerbals.Count > 0)
                    {
                        foreach (string kerbnme in entry.Value.ThwKerbals)
                        {
                             Utilities.Log_Debug("Dump ThwKerbals list entry=" + kerbnme);
                        }
                        // First we find the ThwKerbal part and if they are still on-board frozen.
                        // Then check the part isn't busy already and start the thaw process.
                        bool Found = false;
                        string thwkerbalname = entry.Value.ThwKerbals.FirstOrDefault();
                         Utilities.Log_Debug("Executing alarm for vessel " + entry.Value.Name + " looking to thaw crewmember " + thwkerbalname + " finding the part");
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
                                     Utilities.Log_Debug("We found the crewmember and the part, but it is busy, so we wait");
                                    break;
                                }
                                //If we just switched to the vessel we have to wait for the vessel to load.
                                if (Time.timeSinceLevelLoad < 6f)
                                {
                                    Utilities.Log_Debug("We found the crewmember and the part, but < 4 secs since level loaded, so we wait");
                                    break;
                                }
                                //If we get here, we have found the kerbal, and the part, and it isn't busy. So we THAW!!!!!!
                                Utilities.Log_Debug("We found the crewmember and the part and it isn't busy, so we THAW!!!");
                                frzr.beginThawKerbal(thwkerbalname);
                                entry.Value.ThwKerbals.Remove(thwkerbalname);
                                foreach (string kerbnme in entry.Value.ThwKerbals)
                                {
                                    Utilities.Log_Debug("Dump ThwKerbals list entry=" + kerbnme);
                                }
                                ModifyKACAlarm(alarm, entry.Value.FrzKerbals, entry.Value.ThwKerbals);
                                break;
                            }
                        }
                        if (!Found)
                        {
                            //We didn't find them anywhere. Remove the thaw request.
                            entry.Value.ThwKerbals.Remove(thwkerbalname);
                             Utilities.Log_Debug("We didn't find the thaw kerbal " + thwkerbalname + " anywhere on the vessel so we deleted the request");
                        }
                    }
                    else
                    {
                        if (entry.Value.FrzKerbals.Count > 0)
                        {
                            foreach (string kerbnme in entry.Value.FrzKerbals)
                            {
                                 Utilities.Log_Debug("Dump FrzKerbals list entry=" + kerbnme);
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
                                 Utilities.Log_Debug("Executing alarm for vessel " + entry.Value.Name + " looking to freeze crewmember " + frzkerbalname + " but the aren't on-board so move on");
                                entry.Value.FrzKerbals.Remove(frzkerbalname);
                                continue;
                            }
                            //ProtoCrewMember crewmember = FlightGlobals.ActiveVessel.GetVesselCrew().Find(a => a.name == entry.Value.ThwKerbals[0]);
                             Utilities.Log_Debug("Executing alarm for vessel " + entry.Value.Name + " looking to freeze crewmember " + frzkerbalname + " finding the part");
                            foreach (DeepFreezer frzr in DpFrzrActVsl)
                            {
                                if (frzr.part.protoModuleCrew.Contains(crewmember))
                                {
                                    //They are in this part.
                                    Found = true;
                                    if (frzr.DFIcrewXferFROMActive || frzr.DFIcrewXferTOActive || frzr.DFIIsFreezeActive || frzr.DFIIsThawActive)
                                    {
                                        //part is busy, so we wait.
                                         Utilities.Log_Debug("We found the crewmember and the part, but it is busy, so we wait");
                                        break;
                                    }
                                    //If we just switched to the vessel we have to wait for the vessel to load.
                                    if (Time.timeSinceLevelLoad < 6f)
                                    {
                                        Utilities.Log_Debug("We found the crewmember and the part, but < 4 secs since level loaded, so we wait");
                                        break;
                                    }
                                    //If we get here, we have found the kerbal, and the part, and it isn't busy. So we FREEZE!!!!!!
                                    Utilities.Log_Debug("We found the crewmember and the part and it isn't busy, so we FREEZE!!!");
                                    frzr.beginFreezeKerbal(crewmember);
                                    entry.Value.FrzKerbals.Remove(frzkerbalname);
                                    foreach (string kerbnme in entry.Value.FrzKerbals)
                                    {
                                        Utilities.Log_Debug("Dump FrzKerbals list entry=" + kerbnme);
                                    }
                                    ModifyKACAlarm(alarm, entry.Value.FrzKerbals, entry.Value.ThwKerbals);
                                    break;
                                }
                            }
                            if (!Found)
                            {
                                //We didn't find them anywhere. Remove the freeze request.
                                entry.Value.FrzKerbals.Remove(frzkerbalname);
                                 Utilities.Log_Debug("We didn't find the freeze kerbal " + frzkerbalname + " anywhere on the vessel so we deleted the request");
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
                    if (entry.VesselID != string.Empty)
                    {
                        Guid tmpid = Guid.Empty;
                        try
                        {
                            tmpid = new Guid(entry.VesselID);
                        }
                        catch (FormatException)
                        {
                            if (invalidKACGUIDItems < 10)
                            {
                                Debug.Log("DeepFreeze invalid KAC alarm GUID caught (" + entry.VesselID + ")");
                                invalidKACGUIDItems++;
                            }
                        }
                        if (tmpid != Guid.Empty)
                        {
                            if (DeepFreeze.Instance.DFgameSettings.knownVessels.ContainsKey(tmpid)) // So we do know that vessel does have a Freezer Assocation so we add it to the KnownList
                            {
                                AlarmInfo tempAlarmInfo = new AlarmInfo(entry.Name, tmpid);
                                UpdateKACAlarmInfo(tempAlarmInfo, entry);
                                DeepFreeze.Instance.DFgameSettings.knownKACAlarms.Add(entry.ID, tempAlarmInfo);
                            }
                        }
                    }
                }
                else  //We do know about it, we update it's details
                {
                    Guid tmpid = Guid.Empty;
                    try
                    {
                        tmpid = new Guid(entry.VesselID);
                    }
                    catch (FormatException)
                    {
                        if (invalidKACGUIDItems < 10)
                        {
                            Debug.Log("DeepFreeze invalid KAC alarm GUID caught (" + entry.VesselID + ")");
                            invalidKACGUIDItems++;
                        }
                    }
                    if (tmpid != Guid.Empty)
                    {
                        AlarmInfo tempAlarmInfo = new AlarmInfo(entry.Name, tmpid);
                        UpdateKACAlarmInfo(tempAlarmInfo, entry);
                        tempAlarmInfo.AlarmExecute = DeepFreeze.Instance.DFgameSettings.knownKACAlarms[entry.ID].AlarmExecute;
                        DeepFreeze.Instance.DFgameSettings.knownKACAlarms[entry.ID] = tempAlarmInfo;
                    }
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

        internal void ModifyKACAlarm(KACWrapper.KACAPI.KACAlarm alarm, List<string> InFrzKbls, List<string> InThwKbls)
        {
            // First we strip out any existing DeepFreeze Events from the notes.
            List<string> _FrzKbls;
            List<string> _ThwKbls;
            string notes = ParseKACNotes(alarm.Notes, out _FrzKbls, out _ThwKbls);
            //Now we add our new DeepFreeze Events back in.
            string newnotes = CreateKACNotes(notes, InFrzKbls, InThwKbls);
             Utilities.Log_Debug("ModifyKACAlarm oldnotes= \r\n" + alarm.Notes);
             Utilities.Log_Debug("Stripped out notes= \r\n" + notes);
             Utilities.Log_Debug("NewNotes= \r\n" + newnotes);
            alarm.Notes = newnotes;
        }

        internal string ParseKACNotes(string Notes, out List<String> InFrzKbls, out List<string> InThwKbls)
        {
            // Parse out the KAC Alarm Notes. Input is Existing Alarm Notes. Outputs are notes with DeepFreeze events stripped out and a list of Kerbals to Freeze and list of Kerbals to Thaw
            List<string> _frzKbls = new List<string>();
            List<string> _thwKbls = new List<string>();
            string NewNotes = string.Empty;
            string[] noteStrings = Notes.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (noteStrings.Length > 0)
            {
                for (int i = 0; i < noteStrings.Length; i++)
                {
                    // Utilities.Log_Debug("ParseKACNotes Line=" + noteStrings[i]);
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
            InFrzKbls = _frzKbls;
            InThwKbls = _thwKbls;
            return NewNotes;
        }

        private string CreateKACNotes(string Notes, List<string> InFrzKbls, List<string> InThwKbls)
        {
            // Creates new KAC alarm Notes. Inputs are existing Notes (with any previous DeepFreeze events stripped out), List of Kerbals to Freeze and List of Kerbals to Thaw.
            string NewNotes = string.Empty;
            NewNotes += Notes + "\r\n";
            foreach (string kerbal in InFrzKbls)
            {
                NewNotes += "FREEZE " + kerbal + "\r\n";
            }
            foreach (string kerbal in InThwKbls)
            {
                NewNotes += "THAW " + kerbal + "\r\n";
            }
            return NewNotes;
        }

        #endregion KACAlarms
    }
}