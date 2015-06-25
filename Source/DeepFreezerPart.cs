/**
 * DeepFreezerPart.cs
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
using UnityEngine;

namespace DF
{

    public class DeepFreezer : PartModule, IDeepFreezer
    {
        private float lastUpdate = 0.0f;                  // time since we last updated the part menu
        private float lastRemove = 0.0f;                  // time since we last removed a part menu event
        private float updatetnterval = .5f;               // time between part menu updates
        private float updateECTempInterval = 2f;         // time between EC and Temp checks updates
        private double deathCounter = 0f;                 // time delay counter until the chance of a frozen kerbal dying due to lack of EC
        private float  deathRoll = 240f;                  // time delay until the chance of a frozen kerbal dying due to lack of EC
        private double tmpdeathCounter = 0f;              // time delay counter until the chance of a frozen kerbal dying due to part being too hot
        private float tmpdeathRoll = 120f;                // time delay until the chance of a frozen kerbal dying due to part being too hot
        private bool ECWarningIssued = false;             // set to true if EC warning has been issued
        private bool TmpWarningIssued = false;            // set to true if a Temp warning has been issued
        private bool _crewXferTOActive = false;           // true if a Stock crewXfer to this part is active
        public bool DFIcrewXferTOActive                   // Interface var for API = true if a Stock crewXfer to this part is active
        {
            get
            {
                return this._crewXferTOActive;
            }
        }            
        private bool _crewXferFROMActive = false;         // true if a Stock crewXfer from this part is active
        public bool DFIcrewXferFROMActive                 //  Interface var for API = true if a Stock crewXfer from this part is active
        {
            get
            {
                return this._crewXferFROMActive;
            }
        }   
        private ProtoCrewMember xfercrew;                 // set to the crew kerbal during a crewXfer
        private Part xferfromPart;                        // set to the from part during a crewXfer
        private Part xfertoPart;                          // set to the to part during a crewXfer
        private InternalSeat xferfromSeat;                // set to the from seat during a crewXfer
        //private InternalSeat xfertoSeat;                  // set to the to seat during a crewXfer
        private bool xferisfromEVA = false;               // set to true if CrewXferTOActive and it is FROM an EVA kerbal entering the part.
        public bool crewXferSMActive = false;             // set to true if CrewXfer is active and SM is installed and managing the xfer.
        public bool crewXferSMStock = false;              // set to true if a Stock CrewXfer is active and SM is installed and managing the xfer.
        private bool refreshPortraits = false;            // set to true if we are running a timer to refresh the Portrait cameras
        private double refreshPortraitsTimer = 0f;        // the timer for refreshing the portrait cameras
        private const double refreshPortraitsWaitTime = 40f; // how long to wait in frames before refreshing the portrait cameras
        private bool _FreezerOutofEC = false;               // true if the freezer has run out of EC
        public bool DFIFreezerOutofEC                       //  Interface var for API = true if the freezer has run out of EC
        {
            get
            {
                return this._FreezerOutofEC;
            }                         
        }   
        private FrzrTmpStatus _FrzrTmp = FrzrTmpStatus.OK;  // ok, warning and red alert flags for temperature monitoring of the freezer
        public FrzrTmpStatus DFIFrzrTmp                     //  Interface var for API = ok, warning and red alert flags for temperature monitoring of the freezer
        {
            get
            {
                return this._FrzrTmp;
            }            
        }   
        private double heatamtMonitoringFrznKerbals = 5f;  //amount of heat generated when monitoring a frozen kerbal
        private double heatamtThawFreezeKerbal = 50f;       //amount of heat generated when freezing or thawing a kerbal
        
        private static string[] CryoPodWindowNames = { "cryopodWindow2", "cryopodWindow2 1", "cryopodWindow2 2", "cryopodWindow2 3", "cryopodWindow2 4", "cryopodWindow2 5" 
                                                 , "cryopodWindow2 6", "cryopodWindow2 7", "cryopodWindow2 8", "cryopodWindow2 9"};

       
        public ScreenMessage ThawMsg, FreezeMsg , OnGoingECMsg, TempChkMsg, IVAKerbalName, IVAkerbalPod;  // used for the bottom right screen messages
        private System.Random rnd = new System.Random(); // Random seed for Killing Kerbals when we run out of EC to keep the Freezer running.
        private bool RTlastKerbalFreezeWarn = false;     //  set to true if you are using RemoteTech and you attempt to freeze your last kerbal in active vessel
        [KSPField(isPersistant = true, guiActive = false, guiName = "Freezer Size")] //Total Size of Freezer, get's read from part.cfg.
        public int FreezerSize;
        public int DFIFreezerSize        
        {
            get
            {
                return this.FreezerSize;
            }            
        }           
        [KSPField(isPersistant = true, guiActive = true, guiName = "Total Frozen Kerbals")] //WISOTT Total number of frozen kerbals, just a count of the list object.
        public int TotalFrozen;
        public int DFITotalFrozen
        {
            get
            {
                return this.TotalFrozen;
            }            
        }   
        [KSPField(isPersistant = true, guiActive = true, guiName = "Freezer Space")] //Total space available for storage. Set by Part.cfg file.
        public int FreezerSpace;
        public int DFIFreezerSpace
        {
            get
            {
                return this.FreezerSpace;
            }
            
        }   
        [KSPField(isPersistant = true)] //Is set to true if the part is full (taking into account frozen kerbals in the part).
        public bool PartFull;
        public bool DFIPartFull
        {
            get
            {
                return this.PartFull;
            }            
        }   
        [KSPField(isPersistant = true, guiName = "Cabin Temperature", guiUnits = "K", guiFormat = "F1", guiActive = true)]
        public float CabinTemp = 0f;
        [KSPEvent(active = true, guiActive = true, name = "showMenu", guiName = "Toggle Menu")]
        public void showMenu()
        {
            DeepFreezeGUI obj = DeepFreeze.Instance.GetComponent("DeepFreezeGUI") as DeepFreezeGUI;
            if (obj != null)
                obj.GuiVisible = !obj.GuiVisible;
            else
                Debug.Log("DeepFreezer ToggleMenu error");
        }

        [KSPField(isPersistant = true)]
        public float timeSinceLastECtaken; //This is the game time since EC was taken, for the ongoing EC usage while kerbal's are frozen

        [KSPField(isPersistant = true)]
        public float timeSinceLastTmpChk; //This is the game time since Temperature was checked, for the ongoing storage of frozen kerbal's

        [KSPField(isPersistant = true)]
        public Int32 FrznChargeRequired; //Set by part.cfg. Total EC value required to maintain a frozen kerbal per minute.

        [KSPField(isPersistant = true)]
        public Int32 GlykerolRequired; //Set by part.cfg. Total Glykerol value required to freeze a kerbal.

        [KSPField()]                     // set to active while freezing a kerbal
        public bool IsFreezeActive;
        public bool DFIIsFreezeActive
        {
            get
            {
                return this.IsFreezeActive;
            }            
        }   

        [KSPField()]                     // set to active while thawing a kerbal
        public bool IsThawActive;
        public bool DFIIsThawActive
        {
            get
            {
                return this.IsThawActive;
            }            
        }   

        [KSPField()]
        public double StoredCharge;      // Stores up EC as we are freezing or thawing over time until we reach what we need.

        [KSPField(isPersistant = true)]
        public Int32 ChargeRequired; //Set by part.cfg. Total EC value required for a complete freeze or thaw.

        [KSPField(isPersistant = true)]
        public Int32 ChargeRate; //Set by part.cfg. EC draw per tick.

        private ProtoCrewMember ActiveFrzKerbal;  // These vars store info about the kerbal while we are freezing or thawing
        private string ToFrzeKerbal = "";
        private int ToFrzeKerbalSeat = 0;
        private string ToFrzeKerbalXformNme = "Unknown";
        private string ToThawKerbal = "";

        private FrznCrewList _StoredCrewList = new FrznCrewList(); // This is the frozen StoredCrewList for the part
        public FrznCrewList DFIStoredCrewList                      //  Interface var for API = This is the frozen StoredCrewList for the part
        {
            get
            {
                return this._StoredCrewList;
            }
        }   
        public Guid CrntVslID;
        private uint CrntPartID;
        private string CrntVslName;
        private DFGameSettings DFgameSettings;
        private DFSettings DFsettings;
        private bool setGameSettings = false;
        private bool partHasInternals = false;
        private bool onvslchgInternal = false;
        private bool onvslchgExternal = false;
        private double ResAvail = 0f;

        //Audio Sounds
        protected AudioSource hatch_lock;
        protected AudioSource ice_freeze;
        protected AudioSource machine_hum;
        protected AudioSource ding_ding;

        public override void OnUpdate()
        {
            if (Time.timeSinceLevelLoad < 2.0f || !HighLogic.LoadedSceneIsFlight) // Check not loading level or not in flight
            {
                return;
            }
            if (refreshPortraits)  // Check if we are delaying a portrait camera refresh and if so process the delay
            {
                refreshPortraitsTimer += 1;
                if (refreshPortraitsTimer > refreshPortraitsWaitTime)  // Delay time up, refresh the cameras
                {
                    respawnVesselCrew();
                    refreshPortraitsTimer = 0;
                    refreshPortraits = false;
                }
            }

            ScreenMessages.RemoveMessage(IVAKerbalName);
            ScreenMessages.RemoveMessage(IVAkerbalPod);
            if (Utilities.VesselIsInIVA(this.part.vessel)) 
            {
                this.Log_Debug("Vessel is in IVA mode");                
                Kerbal actkerbal = Utilities.FindCurrentKerbal(this.part);
                if (actkerbal != null)
                {                    
                    ProtoCrewMember crew = this.part.protoModuleCrew.FirstOrDefault(a => a.name == actkerbal.name);
                    int SeatIndx = -1;
                    if (crew != null)
                    {
                        SeatIndx = crew.seatIdx;
                    }
                    this.Log_Debug("ActiveKerbalFound, seatidx=" + SeatIndx);
                    if (SeatIndx != -1)
                    {
                        SeatIndx++;
                        IVAkerbalPod = ScreenMessages.PostScreenMessage("Pod:" + SeatIndx);
                    }
                    IVAKerbalName = ScreenMessages.PostScreenMessage(actkerbal.name);                                                             
                }                                                           
            }

            if ((Time.time - lastUpdate) > updatetnterval && (Time.time - lastRemove) > updatetnterval) // We only update every updattnterval time interval.
            {
                
                lastUpdate = Time.time;
                if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && FlightGlobals.ActiveVessel != null)
                {
                    CrntVslID = this.vessel.id;
                    CrntVslName = this.vessel.vesselName;

                    // This should only happen once we need to load the StoredCrewList of frozen kerbals for this part from the DeepFreeze master list
                    if (!setGameSettings)
                    {
                        onceoffSetup();
                    }

                    if (DFsettings.ECReqdToFreezeThaw != ChargeRequired)
                    {
                        Utilities.Log_Debug("DeepFreezer", "Master config ECReqdToFreezeThaw=" + DFsettings.ECReqdToFreezeThaw + " overriding Part ChargeRequired=" + ChargeRequired + " parm");
                        ChargeRequired = DFsettings.ECReqdToFreezeThaw;
                    }
                    if (DFsettings.GlykerolReqdToFreeze != GlykerolRequired)
                    {
                        Utilities.Log_Debug("DeepFreezer", "Master config GlykerolReqdToFreeze=" + DFsettings.GlykerolReqdToFreeze + " overriding Part GlykerolRequired=" + GlykerolRequired + " parm");
                        GlykerolRequired = DFsettings.GlykerolReqdToFreeze;
                    }

                    Utilities.Log_Debug("DeepFreezer", "Set Temp");
                    if (DFsettings.TempinKelvin)
                    {
                        this.Fields["CabinTemp"].guiUnits = "K";
                        CabinTemp = (float)this.part.temperature;
                    }
                    else
                    {
                        this.Fields["CabinTemp"].guiUnits = "C";
                        CabinTemp = Utilities.KelvintoCelsius((float)this.part.temperature);
                    }
                    Utilities.Log_Debug("DeepFreezer", "Set Temp done");

                    // Set a flag if this part has internals or not. If it doesn't we don't try to save/restore specific seats for the frozen kerbals
                    if (this.part.internalModel == null)
                    {
                        partHasInternals = false;
                    }
                    {
                        partHasInternals = true;
                    }

                    // Check Crew Xfers in action
                                       
                    if (_crewXferFROMActive)
                    {
                        Debug.Log("Crew XferFROM Active, checking if complete");
                        if (DFInstalledMods.SMInstalled && crewXferSMActive) //Xfer from this part SM Xfer
                        {
                            if (IsSMXferRunning()) 
                            {
                                Utilities.Log_Debug("DeepFreezer", "CrewXfer SMxfer and it's still running, so wait");
                            }
                            else
                            {
                                _crewXferFROMActive = false;
                                crewXferSMActive = false;
                                crewXferSMStock = false;                                
                                Utilities.Log_Debug("DeepFreezer", "CrewXferFROM SMXfer Completed");
                            }
                        }
                        else // Xfer from this part Stock Xfer
                        {
                            ProtoCrewMember crew = this.part.protoModuleCrew.FirstOrDefault(a => a.name == xfercrew.name);
                            if (crew == null)
                            {
                                _crewXferFROMActive = false;
                                crewXferSMActive = false;
                                crewXferSMStock = false;                                
                                refreshPortraits = true;
                                refreshPortraitsTimer = 0f;
                                Utilities.Log_Debug("DeepFreezer", "CrewXferFROM Stock Completed");
                            }
                        }                                                   
                    }

                    if (_crewXferTOActive)
                    {
                        Debug.Log("Crew XferTO Active, checking if complete");
                        if (DFInstalledMods.SMInstalled && crewXferSMActive)        //Xfer to this part SM Xfer                    
                        {
                            if (IsSMXferRunning()) 
                            {
                                Utilities.Log_Debug("DeepFreezer", "CrewXfer SMxfer and it's still running, so wait");
                            }
                            else
                            {
                                xferisfromEVA = false;
                                _crewXferTOActive = false;
                                crewXferSMActive = false;
                                crewXferSMStock = false;
                                Utilities.Log_Debug("DeepFreezer", "CrewXferTO SMXfer Completed");
                            }
                        }
                        else // Xfer to this part Stock Xfer
                        {
                            Utilities.Log_Debug("DeepFreezer", "CrewXfer active & SMXfer is not active, so checking");
                            ProtoCrewMember crew = this.part.protoModuleCrew.FirstOrDefault(a => a.name == xfercrew.name);
                            if (crew != null)
                            {
                                if (PartFull) // If the part is actually full we have to send them back to where they came from.
                                {
                                    if (xferisfromEVA)  // if it was from EVA send them back outside.
                                    {
                                        Utilities.Log_Debug("DeepFreezer", "CrewXfer xferisfromEVA = true kick them out to EVA");

                                        FlightEVA.fetch.spawnEVA(xfercrew, xfertoPart, xfertoPart.airlock);
                                        CameraManager.Instance.SetCameraFlight();
                                    }
                                    else // it wasn't from EVA so send them back to the part they came from.
                                    {
                                        Utilities.Log_Debug("DeepFreezer", "CrewXfer xferisfromEVA = false kick them out to from part");
                                        this.part.RemoveCrewmember(xfercrew);
                                        xferfromPart.AddCrewmember(xfercrew);
                                        refreshPortraits = true;
                                        refreshPortraitsTimer = 0f;                                        
                                    }
                                    onvslchgInternal = true;
                                    GameEvents.onVesselChange.Fire(vessel);
                                    ScreenMessages.PostScreenMessage("Freezer is Full, cannot enter at this time", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                                    Utilities.Log_Debug("DeepFreezer", "crewXferSMActive but PART is FULL ended");
                                }

                                xferisfromEVA = false;
                                _crewXferTOActive = false;
                                crewXferSMActive = false;
                                crewXferSMStock = false;                                                                
                                Utilities.Log_Debug("DeepFreezer", "CrewXferTO Stock Completed");
                            }
                        }
                    }                    
                    UpdateEvents(); // Update the Freeze/Thaw Events that are attached to this Part.
                }
                UpdateCounts();
            }
            
        }

        private void onceoffSetup()
        {
            Utilities.Log_Debug("DeepFreezer", "OnUpdate SetGameSettings");
            DFgameSettings = DeepFreeze.Instance.DFgameSettings;
            DFsettings = DeepFreeze.Instance.DFsettings;
            _StoredCrewList.Clear();
            CrntVslID = this.vessel.id;
            CrntVslName = this.vessel.vesselName;
            CrntPartID = this.part.flightID;
            onvslchgExternal = true;
            if (DFsettings.RegTempReqd)
            {
                heatamtMonitoringFrznKerbals = DFsettings.heatamtMonitoringFrznKerbals;
                heatamtThawFreezeKerbal = DFsettings.heatamtThawFreezeKerbal;
            }
            Utilities.Log_Debug("DeepFreezer", "This CrntVslID = " + CrntVslID);
            Utilities.Log_Debug("DeepFreezer", "This CrntPartID = " + CrntPartID);
            Utilities.Log_Debug("DeepFreezer", "This CrntVslName = " + CrntVslName);

            //resetCryoWindows();

            // Iterate through the dictionary of all known frozen kerbals
            foreach (KeyValuePair<string, KerbalInfo> kerbal in DFgameSettings.KnownFrozenKerbals)
            {
                // if the known kerbal is in this part in this vessel
                if (kerbal.Value.vesselID == CrntVslID && kerbal.Value.partID == CrntPartID)
                {
                    //add them to our storedcrewlist for this part.
                    Utilities.Log_Debug("DeepFreezer", "Adding frozen kerbal to this part storedcrewlist " + kerbal.Key);
                    //if (kerbal.Value.seatIdx == null) kerbal.Value.seatIdx = 0;
                    FrznCrewMbr fzncrew = new FrznCrewMbr(kerbal.Key, kerbal.Value.seatIdx, CrntVslID, CrntVslName);
                    _StoredCrewList.Add(fzncrew);
                    //setCryoWindowOn(CryoPodWindowNames[kerbal.Value.seatIdx]);                    
                }
                else
                {
                    Utilities.Log_Debug("DeepFreezer", kerbal.Key + "," + kerbal.Value.vesselID + "," + kerbal.Value.partID + " not this vessel/part");
                }
            }
            setGameSettings = true;
        }

        private void FixedUpdate()
        {            
            if (Time.timeSinceLevelLoad < 2.0f) // Check not loading level
            {
                return;
            }

            if (IsFreezeActive == true) // Process active freezing process
            {
                Utilities.Log_Debug("DeepFreezer", "FreezeActive ToFrzeKerbal = " + ToFrzeKerbal + " Seat =" + ToFrzeKerbalSeat);
                if (!requireResource(vessel, "ElectricCharge", ChargeRate, false, out ResAvail) == true)
                {
                    ScreenMessages.PostScreenMessage("Insufficient electric charge to freeze kerbal", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    FreezeKerbalAbort(ActiveFrzKerbal);
                    return;
                }
                else
                {
                    requireResource(vessel, "ElectricCharge", ChargeRate, true, out ResAvail);
                    StoredCharge = StoredCharge + ChargeRate;
                    ScreenMessages.RemoveMessage(FreezeMsg);
                    FreezeMsg = ScreenMessages.PostScreenMessage(" Freezing - Charge: " + StoredCharge.ToString("######0"));
                    if (DFsettings.RegTempReqd)
                    {
                        this.part.AddThermalFlux(heatamtThawFreezeKerbal);
                    }
                    Utilities.Log_Debug("DeepFreezer", "Drawing Charge StoredCharge =" + StoredCharge.ToString("0000.00") + " ChargeRequired =" + ChargeRequired);
                    if (StoredCharge > ChargeRequired)
                    {
                        if (requireResource(vessel, "Glykerol", GlykerolRequired, true, out ResAvail))
                        {
                            ScreenMessages.RemoveMessage(FreezeMsg);
                            FreezeKerbalConfirm(ActiveFrzKerbal);
                        }
                        else
                        {
                            FreezeKerbalAbort(ActiveFrzKerbal);
                        }
                    }
                }
            }

            if (IsThawActive == true) // Process active thawing process
            {
                Utilities.Log_Debug("DeepFreezer", "ThawActive Kerbal = " + ToThawKerbal);
                if (!requireResource(vessel, "ElectricCharge", ChargeRate, false, out ResAvail))
                {
                    ScreenMessages.PostScreenMessage("Insufficient electric charge to thaw kerbal", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    ThawKerbalAbort(ToThawKerbal);
                }
                else
                {
                    requireResource(vessel, "ElectricCharge", ChargeRate, true, out ResAvail);
                    StoredCharge = StoredCharge + ChargeRate;
                    ScreenMessages.RemoveMessage(ThawMsg);
                    ThawMsg = ScreenMessages.PostScreenMessage(" Thawing - Charge: " + StoredCharge.ToString("######0"));
                    if (DFsettings.RegTempReqd)
                    {
                        this.part.AddThermalFlux(heatamtThawFreezeKerbal);
                    }
                    if (StoredCharge > ChargeRequired)
                    {
                        ScreenMessages.RemoveMessage(ThawMsg);
                        ThawKerbalConfirm(ToThawKerbal);
                    }
                }
            }
            
            // The following section is the on-going EC check and temperature checks for the freezer, only in flight and activevessel
            if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && FlightGlobals.ActiveVessel != null && setGameSettings)
            {
                if (DFsettings.ECreqdForFreezer && timewarpIsValid)
                {
                    ChkOngoingEC(); // Check the on-going EC usage
                }

                if (DFsettings.RegTempReqd && timewarpIsValid)
                {
                    ChkOngoingTemp(); // Check the on-going Temperature
                }
                if (onvslchgExternal)
                {
                    timeSinceLastECtaken = (float)Planetarium.GetUniversalTime(); 
                    timeSinceLastTmpChk = (float)Planetarium.GetUniversalTime();
                    onvslchgExternal = false;
                }                
            }           
        }

        private void ChkOngoingEC()
        {
            // The follow section of code consumes EC when we have ECreqdForFreezer set to true in the part config.
            // This consumes electric charge when we have frozen kerbals on board.
            // But due to bugs in KSP ith EC and SolarPanels at high timewarp if timewarp is > 4x we turn it off.
            // If we run out of EC we roll the dice. There is a 1 in 3 chance a Kerbal will DIE!!!!

            Utilities.Log_Debug("ChkOngoingEC start");
            double currenttime = Planetarium.GetUniversalTime();            
            double timeperiod = currenttime - (double)timeSinceLastECtaken;
            if (timeperiod > updateECTempInterval && TotalFrozen > 0) //We have frozen Kerbals, consume EC
            {                                              
                double ECreqd = ((FrznChargeRequired / 60.0f) * timeperiod * TotalFrozen);
                Utilities.Log_Debug("DeepFreezer", "Running the freezer parms currenttime =" + currenttime + " timeperiod =" + timeperiod + " ecreqd =" + ECreqd);                
                if (requireResource(vessel, "ElectricCharge", ECreqd, false, out ResAvail))
                {
                    ScreenMessages.RemoveMessage(OnGoingECMsg);
                    //Have resource
                    requireResource(vessel, "ElectricCharge", ECreqd, true, out ResAvail);
                    Utilities.Log_Debug("DeepFreezer", "Consumed Freezer EC " + ECreqd + " units");
                    timeSinceLastECtaken = (float)currenttime;
                    deathCounter = currenttime;
                    _FreezerOutofEC = false;
                    ECWarningIssued = false;
                }
                else
                {
                    if (onvslchgExternal) // this is true if vessel just loaded or we just switched to this vessel
                                          // we need to check if we aren't going to exhaust all EC in one call.. and???
                    {
                        ECreqd = ResAvail * 95 / 100;
                        if (requireResource(vessel, "ElectricCharge", ECreqd, false, out ResAvail))
                        {
                            requireResource(vessel, "ElectricCharge", ECreqd, true, out ResAvail);                            
                        }                        
                    }
                    Debug.Log("DeepFreezer Ran out of EC to run the freezer");
                    if (!ECWarningIssued)
                    {
                        ScreenMessages.PostScreenMessage("Insufficient electric charge to monitor frozen kerbals. They are going to die!!", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                        ECWarningIssued = true;
                    }                    
                    ScreenMessages.RemoveMessage(OnGoingECMsg);
                    OnGoingECMsg = ScreenMessages.PostScreenMessage(" Freezer Out of EC : Systems critical in " + (deathRoll - (currenttime - deathCounter)).ToString("######0") + " secs");
                    _FreezerOutofEC = true;
                    Utilities.Log_Debug("DeepFreezer", "deathCounter = " + deathCounter);
                    if (currenttime - deathCounter > deathRoll)
                    {
                        Utilities.Log_Debug("DeepFreezer", "deathRoll reached, Kerbals all die...");
                        deathCounter = currenttime;
                        //all kerbals dies                          
                        var kerbalsToDelete = new List<FrznCrewMbr>();
                        foreach (FrznCrewMbr deathKerbal in _StoredCrewList)
                        {
                            DeepFreeze.Instance.KillFrozenCrew(deathKerbal.CrewName);
                            ScreenMessages.PostScreenMessage(deathKerbal.CrewName + " died due to lack of Electrical Charge to run cryogenics", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                            Debug.Log("DeepFreezer - kerbal " + deathKerbal.CrewName + " died due to lack of Electrical charge to run cryogenics");
                            kerbalsToDelete.Add(deathKerbal);
                        }
                        kerbalsToDelete.ForEach(id => _StoredCrewList.Remove(id));
                    }
                }
            }
            Utilities.Log_Debug("ChkOngoingEC end");
        }

        private void ChkOngoingTemp()
        {
            // The follow section of code checks Temperatures when we have RegTempReqd set to true in the master config file.
            // This check is done when we have frozen kerbals on board.
            // But due to bugs in KSP with EC and SolarPanels at high timewarp if timewarp is > 4x we turn it off.
            // If the temperature is too high we roll the dice. There is a 1 in 3 chance a Kerbal will DIE!!!!

            Utilities.Log_Debug("ChkOngoingTemp start time=" + Time.time.ToString() + ",timeSinceLastTmpChk=" + timeSinceLastTmpChk.ToString() + ",Planetaruim.UniversalTime=" + Planetarium.GetUniversalTime().ToString());
            double currenttime = Planetarium.GetUniversalTime();
            double timeperiod = currenttime - (double)timeSinceLastTmpChk;
            if (timeperiod > updateECTempInterval && TotalFrozen > 0) //We have frozen Kerbals, consume EC
            {
                //Add Heat for equipment monitoring frozen kerbals
                double heatamt = _StoredCrewList.Count * heatamtMonitoringFrznKerbals;
                if (heatamt > 0) this.part.AddThermalFlux(heatamt);
                Utilities.Log_Debug("Added " + heatamt + " kW of heat for monitoring frozen kerbals");
                if (this.part.temperature < DFsettings.RegTempMonitor)
                {
                    Utilities.Log_Debug("DeepFreezer", "Temperature check is good parttemp=" + this.part.temperature + ",MaxTemp=" + DFsettings.RegTempMonitor);
                    ScreenMessages.RemoveMessage(TempChkMsg);
                    _FrzrTmp = FrzrTmpStatus.OK;
                    tmpdeathCounter = currenttime;
                    // do warning if within 40 and 20 kelvin
                    double tempdiff = DFsettings.RegTempMonitor - this.part.temperature;
                    if (tempdiff <= 40)
                    {
                        _FrzrTmp = FrzrTmpStatus.WARN;
                        ScreenMessages.PostScreenMessage("Check Temperatures, Freezer getting hot", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    else
                    {
                        if (tempdiff < 20)
                        {
                            _FrzrTmp = FrzrTmpStatus.RED;
                            ScreenMessages.PostScreenMessage("Warning!! Check Temperatures NOW, Freezer getting very hot", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        }
                    }
                    timeSinceLastTmpChk = (float)currenttime;
                    TmpWarningIssued = false;
                }
                else
                {
                    // OVER TEMP I'm Melting!!!! CODE GOES HERE
                    Debug.Log("DeepFreezer Part Temp TOO HOT, Kerbals are going to melt");
                    if (!TmpWarningIssued)
                    {
                        ScreenMessages.PostScreenMessage("Temperature too hot for kerbals to remain frozen. They are going to die!!", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                        TmpWarningIssued = true;
                    }                    
                    _FrzrTmp = FrzrTmpStatus.RED;
                    Utilities.Log_Debug("DeepFreezer", "tmpdeathCounter = " + tmpdeathCounter);
                    ScreenMessages.RemoveMessage(TempChkMsg);
                    TempChkMsg = ScreenMessages.PostScreenMessage(" Freezer Over Temp : Systems critical in " + (tmpdeathRoll - (currenttime - tmpdeathCounter)).ToString("######0") + " secs");
                    if (currenttime - tmpdeathCounter > tmpdeathRoll)                    
                    {
                        Utilities.Log_Debug("DeepFreezer", "tmpdeathRoll reached, roll the dice...");
                        tmpdeathCounter = currenttime;
                        TmpWarningIssued = false;
                        //a kerbal dies                        
                        int dice = rnd.Next(1, _StoredCrewList.Count); // Randomly select a Kerbal to kill.
                        Utilities.Log_Debug("DeepFreezer", "A Kerbal dies dice=" + dice);
                        FrznCrewMbr deathKerbal = _StoredCrewList[dice-1];
                        DeepFreeze.Instance.KillFrozenCrew(deathKerbal.CrewName);
                        ScreenMessages.PostScreenMessage(deathKerbal.CrewName + " died due to overheating, cannot keep frozen", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                        Debug.Log("DeepFreezer - kerbal " + deathKerbal.CrewName + " died due to overheating, cannot keep frozen");
                        _StoredCrewList.Remove(deathKerbal);
                    }
                }
            }
            Utilities.Log_Debug("ChkOngoingTemp end");
        }

        public override void OnLoad(ConfigNode node)
        {
            Debug.Log("DeepFreezer onLoad");
            Debug.Log("FreezerSize=" + FreezerSize + ",ChargeRequired=" + ChargeRequired + ",GlykerolRequired=" + GlykerolRequired + ",ChargeRate=" + ChargeRate + ",FrznChargeRequired=" + FrznChargeRequired);                  
            Debug.Log("DeepFreezer end onLoad");
        }

        public override void OnStart(PartModule.StartState state)
        {
            Debug.Log("DeepFreezer OnStart");
            base.OnStart(state);           
                        
            if ((state != StartState.None || state != StartState.Editor))
            {
                GameEvents.onCrewTransferred.Add(this.OnCrewTransferred);
                GameEvents.onVesselChange.Add(this.OnVesselChange);
                GameEvents.onCrewBoardVessel.Add(this.OnCrewBoardVessel);    
                /*
                if (DFInstalledMods.SMInstalled) // Check if Ship Manifest (SM) is installed? Disable STOCK Xfer window.
                {
                    FlightEVA.fetch.DisableInterface();  
                }  */                                                  
            }
            
            hatch_lock = gameObject.AddComponent<AudioSource>();
            hatch_lock.clip = GameDatabase.Instance.GetAudioClip("REPOSoftTech/DeepFreeze/Sounds/hatch_lock");
            hatch_lock.volume = .5F;
            hatch_lock.panLevel = 0;
            hatch_lock.rolloffMode = AudioRolloffMode.Linear;
            hatch_lock.Stop();
            ice_freeze = gameObject.AddComponent<AudioSource>();
            ice_freeze.clip = GameDatabase.Instance.GetAudioClip("REPOSoftTech/DeepFreeze/Sounds/ice_freeze");
            ice_freeze.volume = 1;
            ice_freeze.panLevel = 0;
            ice_freeze.rolloffMode = AudioRolloffMode.Linear;
            ice_freeze.Stop();
            machine_hum = gameObject.AddComponent<AudioSource>();
            machine_hum.clip = GameDatabase.Instance.GetAudioClip("REPOSoftTech/DeepFreeze/Sounds/machine_hum");
            machine_hum.volume = .1F;
            machine_hum.panLevel = 0;
            machine_hum.rolloffMode = AudioRolloffMode.Linear;
            machine_hum.Stop();
            ding_ding = gameObject.AddComponent<AudioSource>();
            ding_ding.clip = GameDatabase.Instance.GetAudioClip("REPOSoftTech/DeepFreeze/Sounds/ding_ding");
            ding_ding.volume = .25F;
            ding_ding.panLevel = 0;
            ding_ding.rolloffMode = AudioRolloffMode.Linear;
            ding_ding.Stop();
            Debug.Log("DeepFreezer  END OnStart");
        }
        

        public override void OnSave(ConfigNode node)
        {
            Debug.Log("OnSave: " + node);
        }

        private void OnDestroy()
        {
            Debug.Log("DeepFreezer OnDestroy");
            GameEvents.onCrewTransferred.Remove(this.OnCrewTransferred);
            GameEvents.onVesselChange.Remove(this.OnVesselChange);
            GameEvents.onCrewBoardVessel.Remove(this.OnCrewBoardVessel);
            /*
            if (DFInstalledMods.SMInstalled) // Check if Ship Manifest (SM) is installed? Disable STOCK Xfer window.
            {
                FlightEVA.fetch.EnableInterface();
            }   */
            Debug.Log("DeepFreezer END OnDestroy");
        }

        #region Events

        private void UpdateEvents()
        {
            // If we aren't Thawing or Freezing a kerbal right now, and no crewXfer i active we check all the events.
            if (!IsThawActive && !IsFreezeActive && !_crewXferFROMActive && !_crewXferTOActive)
            {
                Debug.Log("UpdateEvents");
                var eventsToDelete = new List<BaseEvent>();
                foreach (BaseEvent itemX in Events) // Iterate through all Events
                {
                    Debug.Log("Checking Events item " + itemX.name);
                    string[] subStrings = itemX.name.Split(' ');
                    if (subStrings.Length == 3)
                    {
                        if (subStrings[0] == "Freeze") // If it's a Freeze Event
                        {
                            string crewname = "";
                            crewname = subStrings[1] + " " + subStrings[2];
                            if (part.protoModuleCrew.FirstOrDefault(a => a.name == crewname) == null) // Search the part for the crewmember.
                            // We didn't find the crewmember so remove the Freeze Event.
                            {
                                eventsToDelete.Add(itemX);
                            }
                        }
                    }
                }
                eventsToDelete.ForEach(id => Events.Remove(id));

                if (_StoredCrewList.Count < FreezerSize) // If the Freezer isn't full
                {
                    foreach (var CrewMember in part.protoModuleCrew) // We Add Freeze Events for all active crew in the part
                    {
                        addFreezeEvent(CrewMember);
                    }
                }
                if ((part.protoModuleCrew.Count < part.CrewCapacity) || part.CrewCapacity <= 0)  // If part is not full or zero (should always be true, think this is redundant line)
                {
                    foreach (var frozenkerbal in _StoredCrewList) // We add a Thaw Event for every frozenkerbal.
                    {
                        addThawEvent(frozenkerbal.CrewName);
                    }
                }
            }
        }

        private void addFreezeEvent(ProtoCrewMember CrewMember)
        {
            try
            {
                BaseEvent item = Events.Find(v => v.name == "Freeze " + CrewMember.name);  // Search to see if there isn't already a Freeze Event for this CrewMember
                if (item == null && CrewMember.type == ProtoCrewMember.KerbalType.Crew) // Did we find one? and CrewMember is type=Crew? if so, add new Event.
                //***** Could change this to Tourists as well but needs more changes.
                {
                    Events.Add(new BaseEvent(Events, "Freeze " + CrewMember.name, () =>
                    {
                        beginFreezeKerbal(CrewMember);
                    }, new KSPEvent { guiName = "Freeze " + CrewMember.name, guiActive = true }));
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Exception adding Freeze Event for " + CrewMember);
                Debug.Log("Err: " + ex);
            }
        }

        private void removeFreezeEvent(string CrewMember)
        {
            try
            {
                BaseEvent item = Events.Find(v => v.name == "Freeze " + CrewMember); // Find the Freeze event for the CrewMember
                if (item != null)
                {
                    Events.Remove(item); // Remove it
                    lastRemove = Time.time; // we check this time when we do updateevents because if it is done too quickly the GUI goes crazy
                    // There is probably a quicker way to do this, but it finds the GUI for the Part ActionWindow and sets it to dirty which forces Unity to re-draw it.
                    foreach (UIPartActionWindow window in FindObjectsOfType(typeof(UIPartActionWindow)))
                    {
                        if (window.part == part)
                        {
                            window.displayDirty = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Exception removing Freeze Event for " + CrewMember);
                Debug.Log("Err: " + ex);
            }
        }

        private void addThawEvent(string frozenkerbal)
        {
            try
            {
                BaseEvent item = Events.Find(v => v.name == "Thaw " + frozenkerbal); // Check a Thaw even doesn't already exist for this kerbal
                if (item == null) // No Item exists so add a new Thaw Event.
                {
                    Events.Add(new BaseEvent(Events, "Thaw " + frozenkerbal, () =>
                    {
                        FrznCrewMbr tmpKerbal = _StoredCrewList.Find(a => a.CrewName == frozenkerbal);

                        if (tmpKerbal != null)
                        {
                            beginThawKerbal(frozenkerbal);
                        }
                    }, new KSPEvent { guiName = "Thaw " + frozenkerbal, guiActive = true }));
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Exception adding Thaw Event for " + frozenkerbal);
                Debug.Log("Err: " + ex);
            }
        }

        private void removeThawEvent(string frozenkerbal)
        {
            try
            {
                BaseEvent item = Events.Find(v => v.name == "Thaw " + frozenkerbal);
                if (item != null)
                {
                    Events.Remove(item);  // Remove it
                    lastRemove = Time.time; // we check this time when we do updateevents because if it is done too quickly the GUI goes crazy
                    // There is probably a quicker way to do this, but it finds the GUI for the Part ActionWindow and sets it to dirty which forces Unity to re-draw it.
                    foreach (UIPartActionWindow window in FindObjectsOfType(typeof(UIPartActionWindow)))
                    {
                        if (window.part == part)
                        {
                            window.displayDirty = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Exception removing Thaw Event for " + frozenkerbal);
                Debug.Log("Err: " + ex);
            }
        }

        #endregion Events

        #region FrzKerbals

        public void beginFreezeKerbal(ProtoCrewMember CrewMember)
        {
            try
            {
                if (this.FreezerSpace > 0 && this.part.protoModuleCrew.Contains(CrewMember)) // Freezer has space? and Part contains the CrewMember?
                {
                    if (!requireResource(vessel, "Glykerol", GlykerolRequired, false, out ResAvail)) // check we have Glykerol on board. 5 units per freeze event. This should be a part config item not hard coded.
                    {
                        ScreenMessages.PostScreenMessage("Insufficient Glykerol to freeze kerbal", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        return;
                    }
                    else // We have enough Glykerol
                    {
                        if (DFsettings.RegTempReqd) // Temperature check is required
                        {
                            if ((float)this.part.temperature > DFsettings.RegTempFreeze)
                            {
                                ScreenMessages.PostScreenMessage("Cannot Freeze while Temperature > " + DFsettings.RegTempFreeze.ToString("######0") + this.Fields["CabinTemp"].guiUnits, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                                return;
                            }
                        }
                        if (DFInstalledMods.SMInstalled) // Check if Ship Manifest (SM) is installed?
                        {
                            if (IsSMXferRunning())  // SM is installed and is a Xfer running? If so we can't run a Freeze while a SMXfer is running.
                            {
                                ScreenMessages.PostScreenMessage("Cannot Freeze while Crew Xfer in progress", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                                return;
                            }
                        }
                        if (_crewXferFROMActive || _crewXferTOActive)  // We can't run a freeze process if a crewXfer is active, this is catching Stock Xfers.
                        {
                            ScreenMessages.PostScreenMessage("Cannot Freeze while Crew Xfer in progress", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                            return;
                        }
                        if (IsThawActive || IsFreezeActive)
                        {
                            ScreenMessages.PostScreenMessage("Cannot run Freeze process on more than one Kerbal at a time", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                            return;
                        }
                        if (DFInstalledMods.IsRTInstalled)
                        {
                            if (this.part.vessel.GetCrewCount() == 1 && RTlastKerbalFreezeWarn == false)
                            {
                                ScreenMessages.PostScreenMessage("RemoteTech Detected. Press Freeze Again if you want to Freeze your Last Active Kerbal", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                                ScreenMessages.PostScreenMessage("An Active connection or Active Kerbal is Required On-Board to Initiate Thaw Process", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                                RTlastKerbalFreezeWarn = true;
                                return;
                            }
                            RTlastKerbalFreezeWarn = false;
                        }
                        FreezeKerbal(CrewMember); // Begin the Freezing Process
                    }
                }
                else
                {
                    if (FreezerSpace == 0)
                        ScreenMessages.PostScreenMessage("Cannot freeze kerbal. Freezer is full", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Exception attempting to start Freeze for " + CrewMember);
                Debug.Log("Err: " + ex);
                ScreenMessages.PostScreenMessage("Cannot freeze kerbal at this time", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        private void FreezeKerbal(ProtoCrewMember CrewMember)
        {
            this.Log_Debug("Freeze kerbal called");
            ActiveFrzKerbal = CrewMember; // set the Active Freeze Kerbal
            ToFrzeKerbal = CrewMember.name;  // set the Active Freeze Kerbal name
            this.Log_Debug("FreezeKerbal " + CrewMember.name + ",Seatindx=" + CrewMember.seatIdx + ",Seatname=" + CrewMember.seat.seatTransformName);            
            try
            {
                ToFrzeKerbalSeat = CrewMember.seatIdx;                
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to find internal seat index for " + CrewMember.name);
                Debug.Log("Err: " + ex);
                ToFrzeKerbalSeat = 0; // Set their seat
            }
            try
            {
                ToFrzeKerbalXformNme = CrewMember.seat.seatTransformName;                
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to find internal seat for " + CrewMember.name);
                Debug.Log("Err: " + ex);
                ToFrzeKerbalXformNme = "Unknown"; // Set their set Xform Name
            }
            this.Log_Debug("FreezeKerbal ACtiveFrzKerbal=" + ActiveFrzKerbal + ",ToFrzeKerbalSeat=" + ToFrzeKerbalSeat + ",ToFrzeKerbalXformNme=" + ToFrzeKerbalXformNme);
            // If we are in IVA mode we must go to flight mode before freezing (until we sort out the internal space and it's finished...
            if (Utilities.VesselIsInIVA(this.part.vessel))
            {
                CameraManager.Instance.SetCameraFlight();
                ScreenMessages.PostScreenMessage("Privacy Please! Camera must be in Flight mode to Freeze", 5.0f, ScreenMessageStyle.UPPER_CENTER);                      
            }
            this.part.RemoveCrewmember(CrewMember);  // remove the CrewMember from the part, because they are frozen, and this is the only way to trick the game.
            this.part.internalModel.seats[ToFrzeKerbalSeat].taken = true; // Set their seat to Taken, because they are really still there. :)
            this.part.internalModel.seats[ToFrzeKerbalSeat].kerbalRef = CrewMember.KerbalRef;
            //setCryoWindowOn(CryoPodWindowNames[ToFrzeKerbalSeat]);            

            IsFreezeActive = true; // Set the Freezer actively freezing mode on
            ScreenMessages.PostScreenMessage("Starting Freeze process", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            this.Log_Debug("ActiveFrzKerbal=" + ActiveFrzKerbal.name + ",ToFrzeKerbal=" + ToFrzeKerbal + ",SeatIdx=" + ToFrzeKerbalSeat + ",seat transform name=" + ToFrzeKerbalXformNme);
            UpdateCounts();  // Update the Crew counts
            // Start sound effects
            hatch_lock.Play();
            machine_hum.Play();
            machine_hum.loop = true;
            this.Log_Debug("FreezeKerbal ended");
        }

        private void FreezeKerbalAbort(ProtoCrewMember CrewMember)
        {
            this.Log_Debug("FreezeKerbalAbort " + CrewMember.name + " seat " + ToFrzeKerbalSeat);
            ScreenMessages.PostScreenMessage("Freezing Aborted", 5.0f, ScreenMessageStyle.UPPER_CENTER);

            this.part.internalModel.seats[ToFrzeKerbalSeat].taken = false; // Set their seat to NotTaken before we assign them back to their seat, not sure we really need this.
            this.part.AddCrewmemberAt(CrewMember, ToFrzeKerbalSeat); // Add the CrewMember back into the part in their assigned seat.
            //setCryoWindowOff(CryoPodWindowNames[ToFrzeKerbalSeat]);                     
            //respawnVesselCrew();
            refreshPortraits = true;
            refreshPortraitsTimer = 0f;
            IsFreezeActive = false; // Turn the Freezer actively freezing mode off
            ToFrzeKerbal = ""; // Set the Active Freeze Kerbal to null
            machine_hum.Stop(); // Stop the sound effects
            UpdateCounts();  // Update the Crew counts
            onvslchgInternal = true;
            GameEvents.onVesselChange.Fire(vessel);
            ScreenMessages.RemoveMessage(FreezeMsg);
            this.Log_Debug("FreezeKerbalAbort ended");
        }

        private void FreezeKerbalConfirm(ProtoCrewMember CrewMember)
        {
            this.Log_Debug("FreezeKerbalConfirm kerbal " + CrewMember.name + " seatIdx " + ToFrzeKerbalSeat);
            machine_hum.Stop(); // stop the sound effects
            StoredCharge = 0;  // Discharge all EC stored

            // Add frozen kerbal details to the frozen kerbal list in this part.
            FrznCrewMbr tmpcrew = new FrznCrewMbr(CrewMember.name, ToFrzeKerbalSeat, this.vessel.id, this.vessel.name);
            _StoredCrewList.Add(tmpcrew);

            // Set our newly frozen Popsicle, er Kerbal, to Unowned type (usually a Crew) and Dead status.
            CrewMember.type = ProtoCrewMember.KerbalType.Unowned;
            CrewMember.rosterStatus = ProtoCrewMember.RosterStatus.Dead;

            // Update the saved frozen kerbals dictionary
            KerbalInfo kerbalInfo = new KerbalInfo(Planetarium.GetUniversalTime());
            kerbalInfo.vesselID = CrntVslID;
            kerbalInfo.vesselName = CrntVslName;
            kerbalInfo.type = CrewMember.type;
            kerbalInfo.status = CrewMember.rosterStatus;
            kerbalInfo.seatName = ToFrzeKerbalXformNme;
            kerbalInfo.seatIdx = ToFrzeKerbalSeat;
            kerbalInfo.partID = CrntPartID;
            try
            {
                kerbalInfo.experienceTraitName = CrewMember.experienceTrait.Title;
            }
            catch (Exception ex)
            {
                kerbalInfo.experienceTraitName = "Unknown";
                this.Log("Unable to set ExperienceTraitTitle adding frozen crewmember " + CrewMember.name);
                this.Log("Err: " + ex);
            }
            this.Log_Debug("Adding New Frozen Crew to dictionary");
            try
            {
                DFgameSettings.KnownFrozenKerbals.Add(CrewMember.name, kerbalInfo);
            }
            catch (Exception ex)
            {
                this.Log("Unable to add to knownfrozenkerbals frozen crewmember " + CrewMember.name);
                this.Log("Err: " + ex);
                ScreenMessages.PostScreenMessage("DeepFreezer mechanical failure", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                FreezeKerbalAbort(CrewMember);
            }

            DFgameSettings.DmpKnownFznKerbals();
            UpdateCounts();  // Update the Crew counts
            IsFreezeActive = false; // Turn the Freezer actively freezing mode off
            ToFrzeKerbal = ""; // Set the Active Freeze Kerbal to null
            ActiveFrzKerbal = null; // Set the Active Freeze Kerbal to null
            removeFreezeEvent(CrewMember.name);  // Remove the Freeze Event for this kerbal.
            ScreenMessages.PostScreenMessage(CrewMember.name + " frozen", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            ice_freeze.Play();
            //refreshPortraits = true;
            //refreshPortraitsTimer = 0f;
            onvslchgInternal = true;
            GameEvents.onVesselChange.Fire(vessel);
            this.Log_Debug("FreezeCompleted");
        }

        #endregion FrzKerbals

        #region ThwKerbals

        public void beginThawKerbal(string frozenkerbal)
        {
            try
            {
                this.Log_Debug("beginThawKerbal " + frozenkerbal);
                if (DFInstalledMods.IsRTInstalled)
                {
                    if (!DFInstalledMods.RTVesselConnected)
                    {
                        this.Log_Debug("Cannot thaw " + frozenkerbal + " No RemoteTech Control of vessel");
                        ScreenMessages.PostScreenMessage("Cannot initiate Thaw, No Active Vessel Control with RemoteTech installed", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        return;
                    }
                }
                if (this.part.protoModuleCrew.Count >= this.part.CrewCapacity)
                {
                    ScreenMessages.PostScreenMessage("Cannot Thaw " + frozenkerbal + " Part is full", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    this.Log_Debug("Cannot thaw " + frozenkerbal + " Part is full");
                }
                else
                {
                    if (DFInstalledMods.SMInstalled) // Check if Ship Manifest (SM) is installed?
                    {
                        if (IsSMXferRunning()) // SM is installed and is a Xfer running? If so we can't run a Freeze while a SMXfer is running.
                        {
                            ScreenMessages.PostScreenMessage("Cannot Thaw while Crew Xfer in progress", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                            return;
                        }
                    }
                    if (_crewXferFROMActive || _crewXferTOActive)  // We can't run a thaw process if a crewXfer is active, this is catching Stock Xfers.
                    {
                        ScreenMessages.PostScreenMessage("Cannot Thaw while Crew Xfer in progress", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        return;
                    }
                    if (IsThawActive || IsFreezeActive)
                    {
                        ScreenMessages.PostScreenMessage("Cannot run Thaw process on more than one Kerbal at a time", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        return;
                    }
                    // If we are in IVA mode we must go to flight mode before freezing (until we sort out the internal space and it's finished...
                    if (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA || CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Internal)
                    {
                        CameraManager.Instance.SetCameraFlight();
                        ScreenMessages.PostScreenMessage("Privacy Please! Camera must be in Flight mode to Thaw", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    
                    ToThawKerbal = frozenkerbal;  // Set the Active Thaw Kerbal to frozenkerbal name
                    IsThawActive = true;  // Turn the Freezer actively thawing mode on
                    hatch_lock.Play();  // Play the sound effects.
                    machine_hum.Play();
                    machine_hum.loop = true;                    
                    this.Log_Debug("beginThawKerbal has started thawing process");
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Exception attempting to start Thaw for " + frozenkerbal);
                Debug.Log("Err: " + ex);
                ScreenMessages.PostScreenMessage("Cannot thaw kerbal at this time", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        private void ThawKerbalAbort(String ThawKerbal)
        {
            this.Log_Debug("ThawkerbalAbort called");
            ScreenMessages.PostScreenMessage("Thawing Aborted", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            IsThawActive = false; // Turn the Freezer actively thawing mode off
            ToThawKerbal = ""; // Set the Active Thaw Kerbal to null
            StoredCharge = 0; // Discharge all EC stored
            machine_hum.Stop(); //stop the sound effects
            ScreenMessages.RemoveMessage(ThawMsg);
            this.Log_Debug("ThawkerbalAbort End");
        }

        private void ThawKerbalConfirm(String frozenkerbal)
        {
            this.Log_Debug("ThawKerbalConfirm start for " + frozenkerbal);
            foreach (ProtoCrewMember crewlist in this.part.protoModuleCrew)
            {
                this.Log_Debug(crewlist.name + ",Seat=" + crewlist.seatIdx);
            }
            ProtoCrewMember kerbal = HighLogic.CurrentGame.CrewRoster.Unowned.FirstOrDefault(a => a.name == frozenkerbal);
            if (kerbal != null)
            {
                machine_hum.Stop(); //stop sound effects
                StoredCharge = 0;   // Discharge all EC stored
                // Set our newly thawed Popsicle, er Kerbal, to Crew type again (from Unowned) and Assigned status (from Dead status).
                this.Log_Debug("set type to crew and assigned");
                kerbal.type = ProtoCrewMember.KerbalType.Crew;
                kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                this.Log_Debug("find the stored crew member");
                FrznCrewMbr tmpcrew = _StoredCrewList.Find(a => a.CrewName == frozenkerbal);  // Find the thawed kerbal in the frozen kerbal list.
                if (tmpcrew != null)
                {
                    //check if seat is empty, if it is we have to seat them in next available seat
                    //this.part.CrewCapacity++;
                    this.Log_Debug("frozenkerbal " + tmpcrew.CrewName + ",seatindx=" + tmpcrew.SeatIdx);
                    if (partHasInternals)
                    {
                        this.Log_Debug("Part has internals");
                        this.Log_Debug("Checking their seat taken=" + this.part.internalModel.seats[tmpcrew.SeatIdx].taken);
                        if (this.part.internalModel.seats[tmpcrew.SeatIdx].crew == null)
                        {
                            this.Log_Debug("re-add them at seatidx=" + tmpcrew.SeatIdx);
                            this.part.internalModel.seats[tmpcrew.SeatIdx].taken = false; // Set their seat to NotTaken before we assign them back to their seat, not sure we really need this.
                            try
                            {
                                this.part.AddCrewmemberAt(kerbal, tmpcrew.SeatIdx); //Add the CrewMember back into the part in their assigned seat.                                
                                //setCryoWindowOff(CryoPodWindowNames[tmpcrew.SeatIdx]); 
                            }                            
                            catch (Exception ex)
                            {
                                Debug.Log("Exception attempting to add to seat for " + frozenkerbal);
                                Debug.Log("Err: " + ex);
                                ScreenMessages.PostScreenMessage("Code Error: Cannot thaw kerbal at this time, Check Log", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                                ThawKerbalAbort(frozenkerbal);
                                return;
                            }   
                        }
                        else
                        {
                            this.Log_Debug("Seat taken, just add");
                            try
                            {
                                this.part.AddCrewmember(kerbal);  // Add them to the part anyway.
                            }                            
                            catch (Exception ex)
                            {
                                Debug.Log("Exception attempting to add to seat for " + frozenkerbal);
                                Debug.Log("Err: " + ex);
                                ScreenMessages.PostScreenMessage("Code Error: Cannot thaw kerbal at this time, Check Log", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                                ThawKerbalAbort(frozenkerbal);
                                return;
                            }  
                        }
                    }
                    else
                    {
                        this.Log_Debug("Part has no internals, just add");
                        try
                        {
                            this.part.AddCrewmember(kerbal);  // Add them to the part anyway.
                        }                        
                        catch (Exception ex)
                        {
                            Debug.Log("Exception attempting to add to seat for " + frozenkerbal);
                            Debug.Log("Err: " + ex);
                            ScreenMessages.PostScreenMessage("Code Error: Cannot thaw kerbal at this time, Check Log", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                            ThawKerbalAbort(frozenkerbal);
                            return;
                        }  
                    }                    
                }
                else // This should NEVER occur.
                {
                    Debug.Log("Could not find frozen kerbal to Thaw, Very Very Bad. Report this to Mod thread");
                    ScreenMessages.PostScreenMessage("Code Error: Cannot thaw kerbal at this time, Check Log", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    ThawKerbalAbort(frozenkerbal);
                    return;
                }
                // Remove thawed kerbal from the frozen kerbals dictionary
                _StoredCrewList.Remove(tmpcrew); // Remove them from the frozen kerbal list.
                double timeFrozen = Planetarium.GetUniversalTime() - DFgameSettings.KnownFrozenKerbals[frozenkerbal].lastUpdate;
                DFgameSettings.KnownFrozenKerbals.Remove(frozenkerbal);
                DFgameSettings.DmpKnownFznKerbals();

                ScreenMessages.PostScreenMessage(kerbal.name + " thawed out", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                Debug.Log("Thawed out: " + kerbal.name + " They were frozen for " + timeFrozen.ToString());
                foreach (ProtoCrewMember crewlist in this.part.protoModuleCrew)
                {
                    this.Log_Debug(crewlist.name + ",Seat=" + crewlist.seatIdx);
                }
                ToThawKerbal = ""; // Set the Active Thaw Kerbal to null
                IsThawActive = false; // Turn the Freezer actively thawing mode off
                UpdateCounts(); // Update the Crew counts
                removeThawEvent(frozenkerbal); // Remove the Thaw Event for this kerbal.
                ding_ding.Play();
                refreshPortraits = true;
                refreshPortraitsTimer = 0f;
                onvslchgInternal = true;
                GameEvents.onVesselChange.Fire(vessel);
            }
            this.Log_Debug("ThawKerbalConfirm End");
        }

        #endregion ThwKerbals

        #region CrewXfers

        public bool IsSMXferRunning()  // Checks if Ship Manifest is running a CrewXfer or Not.
        {
            ShipManifest.ICrewTransfer SMObject = null;
            try
            {
                SMObject = ShipManifest.SMInterface.GetCrewTransfer();
                if (SMObject.CrewXferActive == true && (SMObject.FromPart == this.part || SMObject.ToPart == this.part))
                {
                    Utilities.Log_Debug("DeepFreeze", "SMXfer running and it is from or to this part");
                    return true;
                }
                else
                {
                    if (SMObject.CrewXferActive == true)
                    {
                        Utilities.Log_Debug("DeepFreeze", "SMXfer running but is it not from or to this part");
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                Utilities.Log("DeepFreezer", " Error attempting to check Ship Manifest if there is a crew transfer active");
                Utilities.Log("DeepFreezer ", ex.Message);
                return false;
            }
        }

        public bool IsSMXferStockRunning()  // Checks if Ship Manifest is running a Stock CrewXfer or Not.
        {
            ShipManifest.ICrewTransfer SMObject = null;
            try
            {
                SMObject = ShipManifest.SMInterface.GetCrewTransfer();
                if (SMObject.IsStockXfer == true)
                {
                    Utilities.Log_Debug("DeepFreeze", "SMXfer running and it is StockXfer");
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Utilities.Log("DeepFreezer", " Error attempting to check Ship Manifest if there is a crew transfer active");
                Utilities.Log("DeepFreezer ", ex.Message);
                return false;
            }
        }

        public ShipManifest.ICrewTransfer GetSMXfer()  // Checks if Ship Manifest is running a CrewXfer or Not.
        {
            ShipManifest.ICrewTransfer SMObject = null;
            try
            {
                SMObject = ShipManifest.SMInterface.GetCrewTransfer();
                return SMObject;
            }
            catch (Exception ex)
            {
                Utilities.Log("DeepFreezer", " Error attempting to get Ship Manifest Xfer details");
                Utilities.Log("DeepFreezer ", ex.Message);
                return null;
            }
        }

        // this is called when a crew transfer has completed. For catching stock Xfers. Because Ship Manifest Xfers will avoid these scenarios.
        private void OnCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> fromToAction)
        {
            Utilities.Log_Debug("DeepFreezer", "OnCrewTransferred Fired From: " + fromToAction.from.name + " To: " + fromToAction.to.name + " Host: " + fromToAction.host.name);
            //Ship Manifest Transfers checked
            crewXferSMActive = false;
            crewXferSMStock = false;            
            if (DFInstalledMods.SMInstalled)
            {
                Utilities.Log_Debug("DeepFreezer", "Check SMxfer running what kind and store it");
                ShipManifest.ICrewTransfer SMObject = null;
                SMObject = GetSMXfer();
                if (SMObject.CrewXferActive)
                {
                    Utilities.Log_Debug("SMXfer is running");
                    crewXferSMActive = SMObject.CrewXferActive;
                    crewXferSMStock = SMObject.IsStockXfer;                    
                    if (SMObject.FromPart == this.part)
                    {
                        removeFreezeEvent(fromToAction.host.name);
                        _crewXferFROMActive = true;  // Set a flag to know a Xfer has started and we check when it is finished in
                        xferfromPart = SMObject.FromPart;
                        xfertoPart = SMObject.ToPart;
                        xfercrew = fromToAction.host;
                    }
                    if (SMObject.ToPart == this.part)
                    {
                        _crewXferTOActive = true; // Set a flag to know a Xfer has started and we check when it is finished in
                        xferfromPart = SMObject.FromPart;
                        xfertoPart = SMObject.ToPart;
                        xfercrew = fromToAction.host;
                    }
                    return;
                }
                else
                {
                    Utilities.Log_Debug("No SMXfer running");
                }                                         
            }

            //Stock Transfer only past here, or no SMXfer is active so it must be stock

            if (fromToAction.from == this.part)  // if the Xfer is FROM this part
            {
                Utilities.Log_Debug("DeepFreezer", "crewXferFROMActive");
                removeFreezeEvent(fromToAction.host.name);  // Remove the Freeze Event for the crewMember leaving the part
                if (fromToAction.to.Modules.Cast<PartModule>().Any(x => x is KerbalEVA)) // Kerbal is going EVA
                {
                    return;
                }
                _crewXferFROMActive = true;  // Set a flag to know a Xfer has started and we check when it is finished in
                xferfromPart = fromToAction.from;
                xfertoPart = fromToAction.to;                
                xfercrew = fromToAction.host;
                return;
            }

            if (fromToAction.to == this.part)  // if the Xfer is TO this part
            {
                Utilities.Log_Debug("DeepFreezer", "crewXferTOActive");
                _crewXferTOActive = true; // Set a flag to know a Xfer has started and we check when it is finished in

                if (fromToAction.from.Modules.Cast<PartModule>().Any(x => x is KerbalEVA)) // Kerbal is entering from EVA
                {
                    Utilities.Log_Debug("DeepFreezer", "CrewXfer xferisfromEVA = true");
                    xferisfromEVA = true;
                    xferfromPart = null;
                    xferfromSeat = null;
                }
                else
                {
                    Utilities.Log_Debug("DeepFreezer", "CrewXfer xferisfromEVA = false");
                    xferisfromEVA = false;
                    xferfromPart = fromToAction.from;
                    xferfromSeat = fromToAction.host.seat;
                }
                xfertoPart = fromToAction.to;                
                xfercrew = fromToAction.host;
                
                if (PartFull)  // If there is no free seats for this Kerbal we kick them back out.
                {
                    Utilities.Log_Debug("DeepFreezer", "CrewXfer PartFull transfer them back, part is full - attempt to cancel stock xfer");
                    ScreenMessages.PostScreenMessage("Cannot enter this freezer, part is full", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    // Remove the transfer message that stock displayed. 
                    var message = new ScreenMessage(string.Empty, 15f, ScreenMessageStyle.LOWER_CENTER);
                    var messages = FindObjectOfType<ScreenMessages>();
                    if (messages != null)
                    {
                        var messagesToRemove = messages.activeMessages.Where(x => x.startTime == message.startTime && x.style == ScreenMessageStyle.LOWER_CENTER).ToList();
                        foreach (var m in messagesToRemove)
                            ScreenMessages.RemoveMessage(m);                        
                    }
                }
                Utilities.Log_Debug("DeepFreezer", "crewXferTOActive end");
            }
        }

        #endregion CrewXfers

        private bool timewarpIsValid
        {
            get
            {
                return TimeWarp.CurrentRateIndex < 5;
            }
        }

        private void respawnVesselCrew()
        {
            this.vessel.SpawnCrew();
            onvslchgInternal = true;
            //GameEvents.onVesselChange.Fire(vessel);
        }

        // this is called when a vessel change event fires.
        private void OnVesselChange(Vessel vessel)
        {
            Debug.Log("OnVesselChange " + vessel.id);
            if (onvslchgInternal)
            {
                onvslchgInternal = false;
                return;
            }
            onvslchgExternal = true;
            //resetCryoWindows();
        }

        private void OnCrewBoardVessel(GameEvents.FromToAction<Part, Part> fromToAction)
        {
            Debug.Log("OnCrewBoardVessel ");
            onvslchgExternal = true;
            //resetCryoWindows();
        }


        private void UpdateCounts()
        {
            // Update the part counts.
            if (!IsThawActive && !IsFreezeActive)
            {
                FreezerSpace = (FreezerSize - _StoredCrewList.Count);
                TotalFrozen = _StoredCrewList.Count;
                PartFull = (TotalFrozen + this.part.protoModuleCrew.Count >= this.part.CrewCapacity);
                Utilities.Log_Debug("DeepFreezer", "UpdateCounts FreezerSpace=" + FreezerSpace + ",TotalFrozen=" + TotalFrozen + ",Partfull=" + PartFull);
                // Reset the seat status for frozen crew to taken - true, because it seems to reset by something?? So better safe than sorry.
                if (partHasInternals)
                {
                    // set cryotube window off for all crew onboard the part
                    Utilities.Log_Debug("DeepFreezer", "Checking all actual crewseats status");
                    foreach (ProtoCrewMember onbrdcrew in this.part.protoModuleCrew)
                    {
                        setCryoWindowOff(CryoPodWindowNames[onbrdcrew.seatIdx]);
                        Utilities.Log_Debug("Setting CryoWindowOff for onboard crew " + onbrdcrew.name + ",Seatindx=" + onbrdcrew.seatIdx);
                    }
                    // set cryotube window off for all empty seats in the part
                    int i = 0;
                    foreach (InternalSeat chkpartseats in this.part.internalModel.seats)
                    {
                        string kerblrefstring;
                        if (chkpartseats.kerbalRef == null)
                        {
                            kerblrefstring = ("kerbalref not found in seatindex=" + i + ",turning the window off");
                            setCryoWindowOff(CryoPodWindowNames[i]);
                        }
                        else kerblrefstring = chkpartseats.kerbalRef.crewMemberName;
                        Utilities.Log_Debug("DeepFreezer", "seatXformName=" + chkpartseats.seatTransformName + ",SeatIndex=" + i + ",KerbalRef=" + kerblrefstring);
                        i++;
                    }
                    // set cryotube window ON for all frozen kerbals in the part
                    Utilities.Log_Debug("DeepFreezer", "StoredCrewList");
                    foreach (FrznCrewMbr lst in _StoredCrewList)
                    {
                        this.part.internalModel.seats[lst.SeatIdx].taken = true;
                        setCryoWindowOn(CryoPodWindowNames[lst.SeatIdx]);
                        ProtoCrewMember kerbal = HighLogic.CurrentGame.CrewRoster.Unowned.FirstOrDefault(a => a.name == lst.CrewName);
                        if (kerbal == null) Utilities.Log_Debug("DeepFreezer", "Frozen Kerbal " + lst.CrewName + " is not found in the currentgame.crewroster.unowned, this should never happen");
                        else this.part.internalModel.seats[lst.SeatIdx].kerbalRef = kerbal.KerbalRef;
                        string kerblrefstring;
                        if (this.part.internalModel.seats[lst.SeatIdx].kerbalRef == null) kerblrefstring = "kerbalref not found";
                        else kerblrefstring = this.part.internalModel.seats[lst.SeatIdx].kerbalRef.crewMemberName;
                        Utilities.Log_Debug("DeepFreezer", "Frozen Crew SeatIdx= " + lst.SeatIdx + ",KerbalRef=" + kerblrefstring);
                    }
                    
                }
                Utilities.Log_Debug("DeepFreezer", "UpdateCounts end");
            }
        }

        // Simple bool for resource checking and usage.  Returns true and optionally uses resource if resAmount of res is available. - Credit TMarkos https://github.com/TMarkos/ as this is lifted verbatim from his Beacon's pack. Mad modify as needed.
        private bool requireResource(Vessel craft, string res, double resAmount, bool consumeResource, out double resavail)
        {
            if (!craft.loaded)
            {
                resavail = 0;
                return false; // Unloaded resource checking is unreliable.
            }
            Dictionary<PartResource, double> toDraw = new Dictionary<PartResource, double>();
            double resRemaining = resAmount;
            foreach (Part cPart in craft.Parts)
            {
                foreach (PartResource cRes in cPart.Resources)
                {
                    if (cRes.resourceName != res) continue;
                    if (cRes.amount == 0) continue;
                    if (cRes.amount >= resRemaining)
                    {
                        toDraw.Add(cRes, resRemaining);
                        resRemaining = 0;
                    }
                    else
                    {
                        toDraw.Add(cRes, cRes.amount);
                        resRemaining -= cRes.amount;
                    }
                }
                if (resRemaining <= 0) break;
            }
            if (resRemaining > 0)
            {
                resavail = resAmount - resRemaining;
                return false;
            }
            if (consumeResource)
            {
                foreach (KeyValuePair<PartResource, double> drawSource in toDraw)
                {
                    drawSource.Key.amount -= drawSource.Value;
                }
            }
            resavail = resAmount;
            return true;
        }

        #region CryoWindows

        private void resetCryoWindows()
        {
            Debug.Log("resetCryoWindows start for partid=" + this.part.flightID);
            foreach (string element in CryoPodWindowNames)
            {
                Debug.Log("Hiding the renderers podname = " + element);                
                setCryoWindowOff(element);
            }
            foreach (FrznCrewMbr frznkerbal in _StoredCrewList)
            {
                Debug.Log("Showing the renderers podname = " + CryoPodWindowNames[frznkerbal.SeatIdx]);
                setCryoWindowOn(CryoPodWindowNames[frznkerbal.SeatIdx]);
            }
            Debug.Log("resetCryoWindows end");
        }

        private void setCryoWindowOff(string windowname)
        {
            Renderer renderer;
            try
            {
                Debug.Log("setCryoWindowOff for " + windowname + " on partid=" + this.part.flightID);
                renderer = this.part.internalModel.FindModelComponent<Renderer>(windowname);
                //renderer.enabled = false;
                GameObject objfnd = renderer.gameObject;
                objfnd.layer = 21;
                Debug.Log("setcryoff set renderergo to layer 21 ok");
                Component parobj = objfnd.GetComponentUpwards("Component");
                parobj.gameObject.layer = 21;
                Debug.Log("setcryoff set rendererparentgo to layer 21 ok");
            }
            catch (Exception ex)
            {
                Debug.Log("Exception Unable to find Renderer in internal model for this part called " + windowname);
                Debug.Log("Err: " + ex);
            }
        }

        private void setCryoWindowOn(string windowname)
        {
            Renderer renderer;
            try
            {
                Debug.Log("setCryoWindowOn for " + windowname + " on partid=" + this.part.flightID);
                renderer = this.part.internalModel.FindModelComponent<Renderer>(windowname);
                //renderer.enabled = true;
                GameObject objfnd = renderer.gameObject;
                objfnd.layer = 16;
                Debug.Log("setcryon set renderergo to layer 16 ok");
                Component parobj = objfnd.GetComponentUpwards("Component");
                parobj.gameObject.layer = 16;
                Debug.Log("setcryon set rendererparentgo to layer 16 ok");
            }
            catch (Exception ex)
            {
                Debug.Log("Exception Unable to find Renderer in internal model for this part called " + windowname);
                Debug.Log("Err: " + ex);
            }
        }

        #endregion CryoWindows
    }
}