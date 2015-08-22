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
using System.Collections;
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
        internal static float updateECTempInterval = 2f;  // time between EC and Temp checks updates
        internal double deathCounter = 0f;                // time delay counter until the chance of a frozen kerbal dying due to lack of EC
        internal double tmpdeathCounter = 0f;             // time delay counter until the chance of a frozen kerbal dying due to part being too hot
        internal static float tmpdeathRoll = 120f;        // time delay until the chance of a frozen kerbal dying due to part being too hot
        internal static float deathRoll = 240f;           // time delay until the chance of a frozen kerbal dying due to lack of EC

        // EC and Temp Functions Vars
        private System.Random rnd = new System.Random();  // Random seed for Killing Kerbals when we run out of EC to keep the Freezer running.

        internal static bool ECWarningIssued = false;     // set to true if EC warning has been issued
        internal static bool TmpWarningIssued = false;    // set to true if a Temp warning has been issued
        private double heatamtMonitoringFrznKerbals = 5f;  //amount of heat generated when monitoring a frozen kerbal, can by overridden by DeepFreeze master settings
        private double heatamtThawFreezeKerbal = 50f;      //amount of heat generated when freezing or thawing a kerbal, can be overriddent by DeepFreeze master settings

        // Crew Transfer Vars
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

        private ProtoCrewMember xfercrew;                       // set to the crew kerbal during a crewXfer
        private Part xferfromPart;                              // set to the from part during a crewXfer
        private Part xfertoPart;                                // set to the to part during a crewXfer
        private InternalSeat xferfromSeat;                      // set to the from seat during a crewXfer
        private InternalSeat xfertoSeat;                        // set to the to seat during a crewXfer
        private bool xferisfromEVA = false;                     // set to true if CrewXferTOActive and it is FROM an EVA kerbal entering the part.
        private bool crewXferSMActive = false;                   // set to true if CrewXfer is active and SM is installed and managing the xfer.
        private bool crewXferSMStock = false;                    // set to true if a Stock CrewXfer is active and SM is installed and managing the xfer.
        private bool xferbackwhenFull = false;                  // set to true when a CrewXfer triggers and the part is already full.
        private int IvaUpdateFrameDelay = 5;                    // Frame delay for Iva portrait updates
        private bool IvaUpdateActive = false;                   // True when an Iva update framedelay is active
        private int IvaPortraitDelay = 0;                       // Counter for IVA portrait delay
        private double timecrewXferTOfired = 0;                 // The Time.time that last crewXferTOFired so we can timeout if it takes too long.
        private double timecrewXferFROMfired = 0;               // The Time.time that last crewXferFROMFired so we can timeout if it takes too long.
        private double crewXferSMTimeDelay = 0;                 // crewXfer time delay used by Ship Manifest.

        internal static ScreenMessage OnGoingECMsg, TempChkMsg;  // used for the bottom right screen messages, these ones are static because the background processor uses them.
        internal ScreenMessage ThawMsg, FreezeMsg, IVAKerbalName, IVAkerbalPart, IVAkerbalPod;  // used for the bottom right screen messages

        private bool RTlastKerbalFreezeWarn = false;     //  set to true if you are using RemoteTech and you attempt to freeze your last kerbal in active vessel

        [KSPField(isPersistant = false, guiActive = false, guiName = "Animated")] //Set to true if Internal contains Animated Cryopods, read from part.cfg.
        public bool isPartAnimated;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Freezer Capacity")] //Total Size of Freezer, get's read from part.cfg.
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

        [KSPField(isPersistant = true, guiActive = true, guiName = "Part is Full?")] //Is set to true if the part is full (taking into account frozen kerbals in the part).
        public bool PartFull;

        public bool DFIPartFull
        {
            get
            {
                return this.PartFull;
            }
        }

        [KSPField(isPersistant = false, guiName = "R/T Connection", guiActive = false)]
        public bool isRTConnected;

        [KSPField(isPersistant = true, guiName = "Freezer Temp", guiActive = true)]
        public FrzrTmpStatus _FrzrTmp = FrzrTmpStatus.OK;  // ok, warning and red alert flags for temperature monitoring of the freezer

        public FrzrTmpStatus DFIFrzrTmp                     //  Interface var for API = ok, warning and red alert flags for temperature monitoring of the freezer
        {
            get
            {
                return this._FrzrTmp;
            }
        }

        internal FrzrTmpStatus DFFrzrTmp
        {
            get
            {
                return this._FrzrTmp;
            }
            set
            {
                this._FrzrTmp = value;
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

        [KSPField(isPersistant = true, guiName = "Freezer Out of EC", guiActive = true)]
        public bool _FreezerOutofEC = false;             // true if the freezer has run out of EC

        public bool DFIFreezerOutofEC                     //  Interface var for API = true if the freezer has run out of EC
        {
            get
            {
                return this._FreezerOutofEC;
            }
        }

        internal bool DFFreezerOutofEC
        {
            get
            {
                return this._FreezerOutofEC;
            }
            set
            {
                this._FreezerOutofEC = value;
            }
        }

        [KSPField(isPersistant = false, guiName = "EC p/Kerbal to run", guiUnits = " p/min", guiActive = true)]
        public Int32 FrznChargeRequired; //Set by part.cfg. Total EC value required to maintain a frozen kerbal per minute.

        [KSPField(isPersistant = false, guiActive = true, guiName = "Current EC Usage", guiUnits = " p/sec", guiFormat = "F3")]
        public float FrznChargeUsage;

        [KSPField(isPersistant = false, guiName = "Glykerol Reqd. to Freeze", guiActive = true)]
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

        [KSPField(isPersistant = false, guiName = "EC p/Kerbal to Frze/Thaw", guiActive = true)]
        public Int32 ChargeRequired; //Set by part.cfg. Total EC value required for a complete freeze or thaw.

        [KSPField(isPersistant = false)]
        public Int32 ChargeRate; //Set by part.cfg. EC draw per tick.

        [KSPField]
        public string animationName = string.Empty;  //Set by part.cfg. name of external animation name for doors if equipped.

        private Animation externalDoorAnim;

        //we persist the external door state in strings because KSP can't handle ENUMs
        [KSPField(isPersistant = true)]
        public string externaldoorstate = "CLOSED";

        internal DoorState _externaldoorstate = DoorState.CLOSED;

        [KSPField(isPersistant = true)]
        public string prevexterndoorstate = "CLOSED";

        internal DoorState _prevexterndoorstate = DoorState.CLOSED;

        [KSPField(isPersistant = true)]
        public bool hasExternalDoor = false;

        [KSPField]
        public string transparentTransforms = string.Empty; //Set by part.cfg. contains list of transforms that should be transparent | separated.

        [KSPEvent(active = false, guiActive = true, guiActiveUnfocused = true, guiActiveEditor = true, unfocusedRange = 5f, name = "eventOpenDoors", guiName = "Open Doors")]
        public void eventOpenDoors()
        {
            Events["eventOpenDoors"].active = false;
            this.Log_Debug("eventOpenDoors triggered - open the bay doors Hal");
            try
            {
                Animation anim;
                Animation[] animators = this.part.internalModel.FindModelAnimators("DOORHandle");
                if (animators.Length > 0)
                {
                    anim = animators[0];
                    anim["DOORHandle"].speed = float.MaxValue;
                    anim["DOORHandle"].normalizedTime = 0;
                    anim.Play("DOORHandle");
                }
                ext_door.Play();
            }
            catch (Exception ex)
            {
                Debug.Log("Exception trying to run the Doorhandle animation");
                Debug.Log("Err: " + ex);
            }
            StartCoroutine(openDoors(1f));
        }

        [KSPAction("OpenDoors")]
        public void ActivateAction(KSPActionParam param)
        {
            eventOpenDoors();
        }

        [KSPEvent(active = false, guiActive = true, guiActiveUnfocused = true, guiActiveEditor = true, unfocusedRange = 5f, name = "eventCloseDoors", guiName = "Close Doors")]
        public void eventCloseDoors()
        {
            Events["eventCloseDoors"].active = false;
            this.Log_Debug("eventOpenDoors triggered - close the bay doors Hal");
            try
            {
                Animation anim;
                Animation[] animators = this.part.internalModel.FindModelAnimators("DOORHandle");
                if (animators.Length > 0)
                {
                    anim = animators[0];
                    anim["DOORHandle"].speed = float.MinValue;
                    anim["DOORHandle"].normalizedTime = 1;
                    anim.Play("DOORHandle");
                }
                ext_door.Play();
            }
            catch (Exception ex)
            {
                Debug.Log("Exception trying to run the Doorhandle animation");
                Debug.Log("Err: " + ex);
            }
            StartCoroutine(closeDoors(-1f));
        }

        [KSPAction("CloseDoors")]
        public void DeActivateAction(KSPActionParam param)
        {
            eventCloseDoors();
        }

        // These vars store info about the kerbal while we are freezing or thawing
        private ProtoCrewMember ActiveFrzKerbal;

        private string ToFrzeKerbal = "";
        private int ToFrzeKerbalSeat = 0;
        private string ToFrzeKerbalXformNme = "Unknown";
        private string ToThawKerbal = "";
        private int ToThawKerbalSeat = 0;
        private bool OpenPodAnimPlaying = false;
        private bool WaitforPodAnim = false;
        private Animation _animation;
        private Shader KSPTranslucentShader;
        private Shader KSPSpecularShader;

        private FrznCrewList _StoredCrewList = new FrznCrewList(); // This is the frozen StoredCrewList for the part

        public FrznCrewList DFIStoredCrewList                      //  Interface var for API = This is the frozen StoredCrewList for the part
        {
            get
            {
                return this._StoredCrewList;
            }
        }

        //Various Vars about the part and the vessel it is attached to
        private Guid CrntVslID;

        private uint CrntPartID;
        private string CrntVslName;
        private bool vesselisinIVA;
        private bool vesselisinInternal;
        private DFGameSettings DFgameSettings;
        private DFSettings DFsettings;
        private bool setGameSettings = false;
        private bool partHasInternals = false;
        private bool onvslchgInternal = false;  //set to true if a VesselChange game event is triggered by this module
        private bool onvslchgExternal = false;  //set to true if a VesselChange game event is triggered outside of this module
        private bool onvslchgNotActive = false; //sets a timer count started when external VesselChange game event is triggered before resetting cryopod and extdoor animations.
        private float onvslchgNotActiveDelay = 0f; // timer as per previous var
        private double ResAvail = 0f;

        [KSPField(isPersistant = true)]  //we keep the game time the last cryopod reset occured here and only run if the last one was longer than cryopodResetTimeDelay ago.
        private double cryopodResetTime = 0f;

        [KSPField(isPersistant = true)]  //we persist the cryopod animation states in a string because KSP can't handle bool arrays
        public string cryopodstateclosedstring = string.Empty;

        private bool[] cryopodstateclosed;   //This bool array is set to true for each cryopod on the part when the cryopod is in closed state.
        private bool[] seatTakenbyFrznKerbal; //This bool array is set to true for each seat that is currently being taken by a frozen kerbal.

        //Audio Sounds
        protected AudioSource hatch_lock;

        protected AudioSource ice_freeze;
        protected AudioSource machine_hum;
        protected AudioSource ding_ding;
        protected AudioSource ext_door;

        public override void OnUpdate()
        {
            if (Time.timeSinceLevelLoad < 2.0f || !HighLogic.LoadedSceneIsFlight) // Check not loading level or not in flight
            {
                return;
            }

            //For some reason when we go on EVA or switch vessels the InternalModel is destroyed.
            //Which causes a problem when we re-board the part as the re-boarding kerbal ends up in a frozen kerbals seat.
            //So we check for the internmodel existing while the vessel this part is attached to is loaded and if it isn't we re-instansiate it.
            if (this.vessel.loaded && this.part.internalModel == null)
            {
                this.Log("Part " + this.part.name + "(" + this.part.flightID + ") is loaded and internalModel has disappeared, so re-instansiate it");
                this.part.SpawnCrew();
                resetFrozenKerbals();
                resetCryopods(true);
                Utilities.CheckPortraitCams(vessel);
            }

            //For some reason when we Freeze a Kerbal and switch to the Internal camera (if in IVA mode) the cameramanager gets stuck.
            //If the user hits the camera mode key while in Internal camera mode this will kick them out to flight
            if (GameSettings.CAMERA_MODE.GetKeyDown() && CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Internal)
            {
                CameraManager.Instance.SetCameraFlight();
            }

            if ((Time.time - lastUpdate) > updatetnterval && (Time.time - lastRemove) > updatetnterval) // We only update every updattnterval time interval.
            {
                lastUpdate = Time.time;
                if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && FlightGlobals.ActiveVessel != null)
                {
                    CrntVslID = this.vessel.id;
                    CrntVslName = this.vessel.vesselName;

                    // This should only happen once, we need to load the StoredCrewList of frozen kerbals for this part from the DeepFreeze master list
                    //This should be done in onload, but it doesn't seem to be working, probably should be checking and doing when vessel loads/unloads/switches/etc.
                    if (!setGameSettings)
                    {
                        onceoffSetup();
                    }
                    // Master settings values override the part values for EC required and Glykerol required
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
                    //Set the Part temperature in the partmenu
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

                    // If RemoteTech installed set the connection status
                    if (DFInstalledMods.IsRTInstalled)
                    {
                        if (DFInstalledMods.RTVesselConnected(this.part.vessel.id))
                        {
                            isRTConnected = true;
                        }
                        else
                        {
                            isRTConnected = false;
                        }
                    }

                    // If we have an external door (CRY-0300) check if the door state has changed and then set the helmet state
                    if (hasExternalDoor)
                    {
                        // if the current and previous door states are different we need to do checks, otherwise we do nothing.
                        if (_externaldoorstate != _prevexterndoorstate)
                        {
                            // if the previous state was closing and now it's closed we can take our helmets off.
                            if (_prevexterndoorstate == DoorState.CLOSING && _externaldoorstate == DoorState.CLOSED)
                            {
                                Utilities.setHelmets(this.part, false);
                            }
                            else // all other door states we keep our helmets on.
                            {
                                Utilities.setHelmets(this.part, true);
                            }
                            _prevexterndoorstate = _externaldoorstate;
                        }
                    }

                    //If a kerbal entered the part from EVA into a frozen kerbals seat then we moved them
                    // and now we wait IvaUpdateFrameDelay frames to refresh the portraits
                    if (IvaUpdateActive)
                    {
                        this.Log_Debug("IvaUpdateActive delay counter=" + IvaPortraitDelay);
                        if (IvaPortraitDelay >= IvaUpdateFrameDelay)
                        {
                            IvaUpdateActive = false;
                            IvaPortraitDelay = 0;
                            this.vessel.SpawnCrew();
                            resetFrozenKerbals();
                            Utilities.CheckPortraitCams(vessel);
                        }
                        else
                        {
                            IvaPortraitDelay += 1;
                        }
                    }

                    //Refresh IVA mode Messages and Bools
                    ScreenMessages.RemoveMessage(IVAKerbalName);
                    ScreenMessages.RemoveMessage(IVAkerbalPart);
                    ScreenMessages.RemoveMessage(IVAkerbalPod);
                    if (Utilities.VesselIsInIVA(this.part.vessel))
                    {
                        this.Log_Debug("Vessel is in IVA mode");
                        vesselisinIVA = true;
                        vesselisinInternal = false;
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
                            IVAkerbalPart = ScreenMessages.PostScreenMessage(this.part.name.Substring(0, 8));
                            IVAKerbalName = ScreenMessages.PostScreenMessage(actkerbal.name);
                        }
                    }
                    else
                    {
                        ScreenMessages.RemoveMessage(IVAKerbalName);
                        ScreenMessages.RemoveMessage(IVAkerbalPart);
                        ScreenMessages.RemoveMessage(IVAkerbalPod);
                        vesselisinIVA = false;
                        this.Log_Debug("Vessel is NOT in IVA mode");
                        if (Utilities.IsInInternal())
                        {
                            vesselisinInternal = true;
                            this.Log_Debug("Vessel is in Internal mode");
                        }
                        else
                        {
                            vesselisinInternal = false;
                            this.Log_Debug("Vessel is NOT in Internal mode");
                        }
                    }

                    // If we have Crew Xfers in progress then check and process to completion.
                    if (_crewXferFROMActive || _crewXferTOActive)
                    {
                        completeCrewTransferProcessing();
                    }

                    UpdateEvents(); // Update the Freeze/Thaw Events that are attached to this Part.
                }
            }
            //UpdateCounts(); // Update the Kerbal counters and stored crew lists for the part - MOVED to Fixed
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
            lastUpdate = Time.time;
            lastRemove = Time.time;
            if (DFsettings.RegTempReqd)
            {
                heatamtMonitoringFrznKerbals = DFsettings.heatamtMonitoringFrznKerbals;
                heatamtThawFreezeKerbal = DFsettings.heatamtThawFreezeKerbal;
            }
            PartInfo partInfo;
            if (DFgameSettings.knownFreezerParts.TryGetValue(this.part.flightID, out partInfo))
            {
                timeSinceLastECtaken = (float)partInfo.timeLastElectricity;
                timeSinceLastTmpChk = (float)partInfo.timeLastTempCheck;
            }
            Utilities.Log_Debug("DeepFreezer", "This CrntVslID = " + CrntVslID);
            Utilities.Log_Debug("DeepFreezer", "This CrntPartID = " + CrntPartID);
            Utilities.Log_Debug("DeepFreezer", "This CrntVslName = " + CrntVslName);

            // Set a flag if this part has internals or not. If it doesn't we don't try to save/restore specific seats for the frozen kerbals
            if (this.part.internalModel == null)
            {
                partHasInternals = false;
            }
            else
            {
                partHasInternals = true;
            }
            resetFrozenKerbals();
            this.Log_Debug("Onceoffsetup resetcryopod doors");
            resetCryopods(true);
            //If we have an external door (CRY-0300) enabled set the current door state and the helmet states
            if (hasExternalDoor)
            {
                //_externaldoorstate = setdoorState();
                setHelmetstoDoorState();
            }
            setGameSettings = true; //set the flag so this method doesn't execute a second time
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
                if (WaitforPodAnim)
                {
                    if (!_animation.IsPlaying("Close"))
                    {
                        Utilities.Log_Debug("Waiting for Pod Close animation to complete");
                        WaitforPodAnim = false;
                        FreezeKerbalConfirm(ActiveFrzKerbal);
                    }
                }
                else
                {
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
                        if (StoredCharge >= ChargeRequired)
                        {
                            if (requireResource(vessel, "Glykerol", GlykerolRequired, true, out ResAvail))
                            {
                                ScreenMessages.RemoveMessage(FreezeMsg);
                                if (_animation != null)
                                {
                                    if (_animation.IsPlaying("Close"))
                                    {
                                        WaitforPodAnim = true;
                                    }
                                    else
                                    {
                                        FreezeKerbalConfirm(ActiveFrzKerbal);
                                    }
                                }
                                else
                                {
                                    FreezeKerbalConfirm(ActiveFrzKerbal);
                                }
                            }
                            else
                            {
                                FreezeKerbalAbort(ActiveFrzKerbal);
                            }
                        }
                    }
                }
            }

            if (IsThawActive == true) // Process active thawing process
            {
                Utilities.Log_Debug("DeepFreezer", "ThawActive Kerbal = " + ToThawKerbal);
                if (WaitforPodAnim)
                {
                    if (!_animation.IsPlaying("Open"))
                    {
                        Utilities.Log_Debug("Waiting for Pod Open animation to complete");
                        WaitforPodAnim = false;
                        ThawKerbalConfirm(ToThawKerbal);
                    }
                }
                else
                {
                    if (vesselisinInternal)
                    {
                        setIVAFrzrCam(ToThawKerbalSeat);
                    }

                    if (!OpenPodAnimPlaying && isPartAnimated)
                    {
                        OpenPodAnimPlaying = true;
                        //now that we have added the kerbal back into the part we run the animation
                        openCryopod(ToThawKerbalSeat);
                        cryopodstateclosed[ToThawKerbalSeat] = false;
                    }

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

                            if (_animation != null)
                            {
                                if (_animation.IsPlaying("Open"))
                                {
                                    WaitforPodAnim = true;
                                }
                                else
                                {
                                    ThawKerbalConfirm(ToThawKerbal);
                                }
                            }
                            else
                            {
                                ThawKerbalConfirm(ToThawKerbal);
                            }
                        }
                    }
                }
            }

            // The following section is the on-going EC check and temperature checks for the freezer, only in flight and activevessel
            if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && FlightGlobals.ActiveVessel != null && setGameSettings)
            {
                if (DFsettings.ECreqdForFreezer)
                {
                    this.Fields["FrznChargeRequired"].guiActive = true;
                    this.Fields["FrznChargeUsage"].guiActive = true;
                    this.Fields["_FreezerOutofEC"].guiActive = true;
                    if (Utilities.timewarpIsValid(5))  // EC usage and generation still seems to work up to warpfactor of 4.
                    {
                        ChkOngoingEC(); // Check the on-going EC usage
                    }
                    else
                    {
                        timeSinceLastECtaken = (float)Planetarium.GetUniversalTime();
                    }
                }
                else
                {
                    this.Fields["FrznChargeRequired"].guiActive = false;
                    this.Fields["FrznChargeUsage"].guiActive = false;
                    this.Fields["_FreezerOutofEC"].guiActive = false;
                }
                if (DFsettings.RegTempReqd)
                {
                    this.Fields["_FrzrTmp"].guiActive = true;
                    if (Utilities.timewarpIsValid(2)) // Temperature is buggy in timewarp so it is disabled whenever timewarp is on.
                    {
                        ChkOngoingTemp(); // Check the on-going Temperature
                    }
                    else
                    {
                        timeSinceLastTmpChk = (float)Planetarium.GetUniversalTime();
                    }
                }
                else
                {
                    this.Fields["_FrzrTmp"].guiActive = false;
                }

                if (onvslchgExternal)
                {
                    timeSinceLastECtaken = (float)Planetarium.GetUniversalTime();
                    timeSinceLastTmpChk = (float)Planetarium.GetUniversalTime();
                    onvslchgNotActive = true;
                    onvslchgNotActiveDelay = 0f;
                    onvslchgExternal = false;
                }

                if (onvslchgNotActive) // this only runs if a VesselChange game event has triggered externally (not triggered by this module)
                {
                    if (onvslchgNotActiveDelay > 3f)
                    {
                        this.Log_Debug("Onupdate reset cryopod doors after vessel change");
                        onvslchgNotActive = false;
                        resetCryopods(true);
                        if (hasExternalDoor)
                        {
                            setHelmetstoDoorState();
                            setDoorHandletoDoorState();
                        }
                    }
                    else
                    {
                        if (_crewXferTOActive)
                        {
                            //Crew Xfer is the cause of the vessel change, let the crewXfer code reset the pods. so reset this counter and turn off this event.
                            onvslchgNotActive = false;
                        }
                        onvslchgNotActiveDelay += Time.fixedDeltaTime;
                    }
                }
                UpdateCounts(); // Update the Kerbal counters and stored crew lists for the part
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
            this.Log_Debug("currenttime = " + currenttime + " timeperiod = " + timeperiod + " updateECTempInterval= " + updateECTempInterval);
            if (timeperiod > updateECTempInterval) //only update every udpateECTempInterval to avoid request resource bug when amounts are too small
            {
                if (TotalFrozen > 0) //We have frozen Kerbals, consume EC
                {
                    double ECreqd = (((FrznChargeRequired / 60.0f) * timeperiod) * TotalFrozen);
                    Utilities.Log_Debug("DeepFreezer", "Running the freezer parms currenttime =" + currenttime + " timeperiod =" + timeperiod + " ecreqd =" + ECreqd);
                    if (requireResource(vessel, "ElectricCharge", ECreqd, false, out ResAvail))
                    {
                        ScreenMessages.RemoveMessage(OnGoingECMsg);
                        //Have resource
                        requireResource(vessel, "ElectricCharge", ECreqd, true, out ResAvail);
                        FrznChargeUsage = (float)ResAvail;
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
                                FrznChargeUsage = (float)ResAvail;
                            }
                        }
                        Debug.Log("DeepFreezer Ran out of EC to run the freezer");
                        if (!ECWarningIssued)
                        {
                            if (TimeWarp.CurrentRateIndex > 1) Utilities.stopWarp();
                            ScreenMessages.PostScreenMessage("Insufficient electric charge to monitor frozen kerbals. They are going to die!!", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                            ECWarningIssued = true;
                        }
                        ScreenMessages.RemoveMessage(OnGoingECMsg);
                        OnGoingECMsg = ScreenMessages.PostScreenMessage(" Freezer Out of EC : Systems critical in " + (deathRoll - (currenttime - deathCounter)).ToString("######0") + " secs");
                        _FreezerOutofEC = true;
                        FrznChargeUsage = 0f;
                        Utilities.Log_Debug("DeepFreezer", "deathCounter = " + deathCounter);
                        if (currenttime - deathCounter > deathRoll)
                        {
                            Utilities.Log_Debug("DeepFreezer", "deathRoll reached, Kerbals all die...");
                            deathCounter = currenttime;
                            //all kerbals die
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
                else  // no frozen kerbals, so just update last time EC checked
                {
                    this.Log_Debug("No frozen kerbals for EC consumption in part " + this.part.name);
                    timeSinceLastECtaken = (float)currenttime;
                    FrznChargeUsage = 0f;
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
            double currenttime = Planetarium.GetUniversalTime();
            double timeperiod = currenttime - (double)timeSinceLastTmpChk;
            Utilities.Log_Debug("ChkOngoingTemp start time=" + Time.time.ToString() + ",timeSinceLastTmpChk=" + timeSinceLastTmpChk.ToString() + ",Planetarium.UniversalTime=" + Planetarium.GetUniversalTime().ToString() + " timeperiod=" + timeperiod.ToString());
            if (timeperiod > updateECTempInterval) //only update every udpateECTempInterval to avoid request resource bug when amounts are too small
            {
                if (TotalFrozen > 0) //We have frozen Kerbals, generate and check heat
                {
                    //Add Heat for equipment monitoring frozen kerbals
                    double heatamt = (((heatamtMonitoringFrznKerbals / 60.0f) * timeperiod) * TotalFrozen);
                    if (heatamt > 0) this.part.AddThermalFlux(heatamt);
                    Utilities.Log_Debug("Added " + heatamt + " kW of heat for monitoring " + TotalFrozen + " frozen kerbals");
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
                        // OVER TEMP I'm Melting!!!!
                        Debug.Log("DeepFreezer Part Temp TOO HOT, Kerbals are going to melt parttemp=" + this.part.temperature);
                        if (!TmpWarningIssued)
                        {
                            if (TimeWarp.CurrentRateIndex > 1) Utilities.stopWarp();
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
                            FrznCrewMbr deathKerbal = _StoredCrewList[dice - 1];
                            DeepFreeze.Instance.KillFrozenCrew(deathKerbal.CrewName);
                            ScreenMessages.PostScreenMessage(deathKerbal.CrewName + " died due to overheating, cannot keep frozen", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                            Debug.Log("DeepFreezer - kerbal " + deathKerbal.CrewName + " died due to overheating, cannot keep frozen");
                            _StoredCrewList.Remove(deathKerbal);
                        }
                    }
                }
                else  // no frozen kerbals, so just update last time tmp checked
                {
                    timeSinceLastTmpChk = (float)currenttime;
                }
            }
            Utilities.Log_Debug("ChkOngoingTemp end");
        }

        public override void OnLoad(ConfigNode node)
        {
            Debug.Log("DeepFreezer onLoad");
            base.OnLoad(node);
            cryopodstateclosed = new bool[FreezerSize];
            seatTakenbyFrznKerbal = new bool[FreezerSize];
            loadcryopodstatepersistent();
            loadexternaldoorstatepersistent();
            Debug.Log("OnLoad: " + node);
            Debug.Log("DeepFreezer end onLoad");
        }

        public override void OnStart(PartModule.StartState state)
        {
            Debug.Log("DeepFreezer OnStart");
            base.OnStart(state);
            //Set the GameEvents we are interested in
            if ((state != StartState.None || state != StartState.Editor))
            {
                GameEvents.onCrewTransferred.Add(this.OnCrewTransferred);
                GameEvents.onVesselChange.Add(this.OnVesselChange);
                GameEvents.onCrewBoardVessel.Add(this.OnCrewBoardVessel);
                GameEvents.onCrewOnEva.Add(this.onCrewOnEva);
            }

            //Set Shaders for changing the Crypod Windows
            HashSet<Shader> shaders = new HashSet<Shader>();
            Resources.FindObjectsOfTypeAll<Shader>().ToList().ForEach(sh => shaders.Add(sh));
            List<Shader> listshaders = new List<Shader>(shaders);
            KSPTranslucentShader = listshaders.Find(a => a.name == "KSP/Alpha/Translucent Specular");
            KSPSpecularShader = listshaders.Find(b => b.name == "KSP/Specular");

            // Setup the sounds
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
            machine_hum.volume = .2F;
            machine_hum.panLevel = 0;
            machine_hum.rolloffMode = AudioRolloffMode.Linear;
            machine_hum.Stop();
            ding_ding = gameObject.AddComponent<AudioSource>();
            ding_ding.clip = GameDatabase.Instance.GetAudioClip("REPOSoftTech/DeepFreeze/Sounds/ding_ding");
            ding_ding.volume = .25F;
            ding_ding.panLevel = 0;
            ding_ding.rolloffMode = AudioRolloffMode.Linear;
            ding_ding.Stop();
            ext_door = gameObject.AddComponent<AudioSource>();
            ext_door.clip = GameDatabase.Instance.GetAudioClip("REPOSoftTech/DeepFreeze/Sounds/extdoorswitch");
            ext_door.volume = 1;
            ext_door.panLevel = 0;
            ext_door.rolloffMode = AudioRolloffMode.Linear;
            ext_door.Stop();

            //If we have an external door (CRY-0300) check if RPM is installed, if not disable the door, otherwise set it's current state (open/closed).
            if (animationName != string.Empty)
            {
                externalDoorAnim = this.part.FindModelAnimators(animationName).FirstOrDefault();
                if (externalDoorAnim == null)
                {
                    this.Log_Debug("Part has external animation defined but cannot find the animation on the part");
                    hasExternalDoor = false;
                    Events["eventOpenDoors"].active = false;
                    Events["eventCloseDoors"].active = false;
                    if (transparentTransforms != string.Empty)
                        Utilities.setTransparentTransforms(this.part, transparentTransforms);
                }
                else
                {
                    this.Log_Debug("Part has external animation, check if RPM is installed and process");
                    if (DFInstalledMods.IsRPMInstalled)
                    {
                        this.Log_Debug("RPM installed, set doorstate");

                        hasExternalDoor = true;
                        //_externaldoorstate = setdoorState();
                        if (_externaldoorstate == DoorState.OPEN)
                        {
                            Events["eventCloseDoors"].active = true;
                            Events["eventOpenDoors"].active = false;
                            StartCoroutine(openDoors(float.MaxValue));
                        }
                        else
                        {
                            Events["eventOpenDoors"].active = true;
                            Events["eventCloseDoors"].active = false;
                            StartCoroutine(closeDoors(float.MinValue));
                        }
                    }
                    else  //RPM is not installed, disable the doors.
                    {
                        this.Log_Debug("RPM NOT installed, set transparent transforms");
                        hasExternalDoor = false;
                        Events["eventOpenDoors"].active = false;
                        Events["eventCloseDoors"].active = false;
                        this.Log_Debug("door actions/events off");
                        if (transparentTransforms != string.Empty)
                            Utilities.setTransparentTransforms(this.part, transparentTransforms);
                    }
                }
            }

            if (DFInstalledMods.IsRTInstalled)
            {
                this.Fields["isRTConnected"].guiActive = true;
            }
            else
            {
                this.Fields["isRTConnected"].guiActive = false;
            }

            Debug.Log("DeepFreezer  END OnStart");
        }

        public override void OnSave(ConfigNode node)
        {
            Debug.Log("DeepFreezer onSave");
            savecryopodstatepersistent();
            saveexternaldoorstatepersistent();
            base.OnSave(node);
            Debug.Log("OnSave: " + node);
            Debug.Log("DeepFreezer end onSave");
        }

        private void OnDestroy()
        {
            //Remove GameEvent callbacks.
            Debug.Log("DeepFreezer OnDestroy");
            GameEvents.onCrewTransferred.Remove(this.OnCrewTransferred);
            GameEvents.onVesselChange.Remove(this.OnVesselChange);
            GameEvents.onCrewBoardVessel.Remove(this.OnCrewBoardVessel);
            GameEvents.onCrewOnEva.Remove(this.onCrewOnEva);
            Debug.Log("DeepFreezer END OnDestroy");
        }

        #region Events

        //This Region controls the part right-click menu additions for thaw/freeze kerbals
        private void UpdateEvents()
        {
            // If we aren't Thawing or Freezing a kerbal right now, and no crewXfer i active we check all the events.
            if (!IsThawActive && !IsFreezeActive && !_crewXferFROMActive && !_crewXferTOActive)
            {
                //Debug.Log("UpdateEvents");
                var eventsToDelete = new List<BaseEvent>();
                foreach (BaseEvent itemX in Events) // Iterate through all Events
                {
                    //Debug.Log("Checking Events item " + itemX.name);
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
                // Events will only appear if RemoteTech is NOT installed OR it is installed and vessel is connected.
                //if (!DFInstalledMods.IsRTInstalled || (DFInstalledMods.IsRTInstalled && DFInstalledMods.RTVesselConnected(this.part.vessel.id)))
                if (!DFInstalledMods.IsRTInstalled || (DFInstalledMods.IsRTInstalled && isRTConnected))
                {
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

        //This region contains the methods for freezing a kerbal
        public void beginFreezeKerbal(ProtoCrewMember CrewMember)
        {
            //This method is the first called to Freeze a Kerbal it will check all the pre-conditions are right for freezing and then call FreezeKerbal if they are
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
            //this method sets all the vars for the kerbal about to be frozen and starts the freezing process (sound).
            //if we are in IVA camera mode it will switch to the view in front of the kerbal and run the cryopod closing animation.
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
            // If we are in IVA mode we switch to the internal camera in front of their cryopod and run the close cryopod animation.
            if (vesselisinIVA || vesselisinInternal)
            {
                setIVAFrzrCam(ToFrzeKerbalSeat);
            }
            if (!isPartAnimated) setCryoWindowOn(ToFrzeKerbalSeat);
            string camname = "FrzCam" + (ToFrzeKerbalSeat + 1).ToString();
            if (isPartAnimated)
            {
                closeCryopod(ToFrzeKerbalSeat);
                cryopodstateclosed[ToFrzeKerbalSeat] = true;
            }
            //Utilities.setFrznKerbalLayer(CrewMember, false, true);
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
            Utilities.setFrznKerbalLayer(CrewMember, true, false);
            if (isPartAnimated)
            {
                if (vesselisinIVA || vesselisinInternal)
                {
                    setIVAFrzrCam(ToFrzeKerbalSeat);
                }
                openCryopod(ToFrzeKerbalSeat);
                cryopodstateclosed[ToFrzeKerbalSeat] = false;
            }
            else
                setCryoWindowOff(ToFrzeKerbalSeat);
            IsFreezeActive = false; // Turn the Freezer actively freezing mode off
            ToFrzeKerbal = ""; // Set the Active Freeze Kerbal to null
            machine_hum.Stop(); // Stop the sound effects
            UpdateCounts();  // Update the Crew counts
            onvslchgInternal = true;
            seatTakenbyFrznKerbal[ToFrzeKerbalSeat] = false;
            GameEvents.onVesselChange.Fire(vessel);
            ScreenMessages.RemoveMessage(FreezeMsg);
            this.Log_Debug("FreezeKerbalAbort ended");
        }

        private void FreezeKerbalConfirm(ProtoCrewMember CrewMember)
        {
            //this method runs with the freeze process is complete (EC consumed)
            //it will store the frozen crew member's details in the _StorecrewList and KnownFrozenKerbals dictionary
            //it will remove the kerbal from the part and set their status to dead and unknown
            this.Log_Debug("FreezeKerbalConfirm kerbal " + CrewMember.name + " seatIdx " + ToFrzeKerbalSeat);
            machine_hum.Stop(); // stop the sound effects
            StoredCharge = 0;  // Discharge all EC stored
            Utilities.setFrznKerbalLayer(CrewMember, false, false);

            // Add frozen kerbal details to the frozen kerbal list in this part.
            FrznCrewMbr tmpcrew = new FrznCrewMbr(CrewMember.name, ToFrzeKerbalSeat, this.vessel.id, this.vessel.name);
            _StoredCrewList.Add(tmpcrew);

            // Update the saved frozen kerbals dictionary
            KerbalInfo kerbalInfo = new KerbalInfo(Planetarium.GetUniversalTime());
            kerbalInfo.vesselID = CrntVslID;
            kerbalInfo.vesselName = CrntVslName;
            kerbalInfo.type = ProtoCrewMember.KerbalType.Unowned;
            kerbalInfo.status = ProtoCrewMember.RosterStatus.Dead;
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
                if (DFsettings.debugging) DFgameSettings.DmpKnownFznKerbals();
            }
            catch (Exception ex)
            {
                this.Log("Unable to add to knownfrozenkerbals frozen crewmember " + CrewMember.name);
                this.Log("Err: " + ex);
                ScreenMessages.PostScreenMessage("DeepFreezer mechanical failure", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                _StoredCrewList.Remove(tmpcrew);
                FreezeKerbalAbort(CrewMember);
            }

            if (hasExternalDoor)
                Utilities.setHelmetshaders(CrewMember.KerbalRef, true);
            // remove the CrewMember from the part crewlist and unregister their traits, because they are frozen, and this is the only way to trick the game.
            CrewMember.UnregisterExperienceTraits(this.part);
            this.part.protoModuleCrew.Remove(CrewMember);
            this.part.internalModel.seats[ToFrzeKerbalSeat].taken = true; // Set their seat to Taken, because they are really still there. :)
            this.part.internalModel.seats[ToFrzeKerbalSeat].kerbalRef = CrewMember.KerbalRef;
            setseatstaticoverlay(this.part.internalModel.seats[kerbalInfo.seatIdx]);
            seatTakenbyFrznKerbal[ToFrzeKerbalSeat] = true;
            // Set our newly frozen Popsicle, er Kerbal, to Unowned type (usually a Crew) and Dead status.
            CrewMember.type = ProtoCrewMember.KerbalType.Unowned;
            CrewMember.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
            CrewMember.KerbalRef.InPart = null;
            if (vesselisinIVA || vesselisinInternal)
            {
                setIVAFrzrCam(ToFrzeKerbalSeat);
            }
            //Remove them from the GUIManager Portrait cams.
            KerbalGUIManager.RemoveActiveCrew(CrewMember.KerbalRef);
            KerbalGUIManager.PrintActiveCrew();
            UpdateCounts();                       // Update the Crew counts
            IsFreezeActive = false;               // Turn the Freezer actively freezing mode off
            ToFrzeKerbal = "";                    // Set the Active Freeze Kerbal to null
            ActiveFrzKerbal = null;               // Set the Active Freeze Kerbal to null
            removeFreezeEvent(CrewMember.name);   // Remove the Freeze Event for this kerbal.
            if (DFInstalledMods.IsUSILSInstalled) // IF USI LS Installed, remove tracking.
            {
                this.Log_Debug("USI/LS installed untrack kerbal=" + CrewMember.name);
                USIUntrackKerbal(CrewMember.name);
            }
            ScreenMessages.PostScreenMessage(CrewMember.name + " frozen", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            ice_freeze.Play();
            onvslchgInternal = true;
            GameEvents.onVesselChange.Fire(vessel);
            GameEvents.onVesselWasModified.Fire(vessel);
            this.Log_Debug("FreezeCompleted");
        }

        private void USIUntrackKerbal(string crewmember)
        //This will remove tracking of a frozen kerbal from USI Life Support MOD, so that they don't consume resources when they are thawed.
        {
            bool tracked = global::LifeSupport.LifeSupportManager.Instance.IsKerbalTracked(crewmember);
            this.Log_Debug("USI/LS Iskerbaltracked before call=" + tracked);
            global::LifeSupport.LifeSupportManager.Instance.UntrackKerbal(crewmember);
            tracked = global::LifeSupport.LifeSupportManager.Instance.IsKerbalTracked(crewmember);
            this.Log_Debug("USI/LS Iskerbaltracked after call=" + tracked);
        }

        #endregion FrzKerbals

        #region ThwKerbals

        //This region contains the methods for thawing a kerbal
        public void beginThawKerbal(string frozenkerbal)
        {
            try
            {
                this.Log_Debug("beginThawKerbal " + frozenkerbal);
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

                    ProtoCrewMember kerbal = HighLogic.CurrentGame.CrewRoster.Unowned.FirstOrDefault(a => a.name == frozenkerbal);
                    if (kerbal != null)
                    {
                        // Set our newly thawed Popsicle, er Kerbal, to Crew type again (from Unowned) and Assigned status (from Dead status).
                        this.Log_Debug("set type to crew and assigned");
                        kerbal.type = ProtoCrewMember.KerbalType.Crew;
                        kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                        this.Log_Debug("find the stored crew member");
                        FrznCrewMbr tmpcrew = _StoredCrewList.Find(a => a.CrewName == frozenkerbal);  // Find the thawed kerbal in the frozen kerbal list.
                        if (tmpcrew != null)
                        {
                            //check if seat is empty, if it is we have to seat them in next available seat
                            this.Log_Debug("frozenkerbal " + tmpcrew.CrewName + ",seatindx=" + tmpcrew.SeatIdx);
                            ToThawKerbalSeat = tmpcrew.SeatIdx;
                            if (partHasInternals)
                            {
                                this.Log_Debug("Part has internals");
                                this.Log_Debug("Checking their seat taken=" + this.part.internalModel.seats[tmpcrew.SeatIdx].taken);
                                ProtoCrewMember crew = this.part.internalModel.seats[tmpcrew.SeatIdx].crew;
                                if (crew != null)
                                {
                                    if (crew.name == frozenkerbal)
                                    {
                                        try
                                        {
                                            //seat is taken and it is by themselves. Expected condition.
                                            //Check the KerbalRef isn't null. If it is we need to respawn them. (this shouldn't occur).
                                            if (kerbal.KerbalRef == null)
                                            {
                                                this.Log_Debug("Kerbal kerbalref is still null, respawn");
                                                kerbal.seat = this.part.internalModel.seats[tmpcrew.SeatIdx];
                                                kerbal.seatIdx = tmpcrew.SeatIdx;
                                                kerbal.Spawn();
                                                this.Log_Debug("Kerbal kerbalref = " + kerbal.KerbalRef.GetInstanceID());
                                                Utilities.CheckPortraitCams(vessel);
                                            }
                                            Utilities.setFrznKerbalLayer(kerbal, true, false);  //Set the Kerbal renderer layers on so they are visible again.
                                            //Utilities.setFrznKerbalLayer(kerbal, false, true);
                                            this.Log_Debug("Expected condition met, kerbal already in their seat.");

                                            if (!isPartAnimated) setCryoWindowOff(tmpcrew.SeatIdx);
                                            if (vesselisinIVA || vesselisinInternal)
                                                setIVAFrzrCam(tmpcrew.SeatIdx);

                                            if (hasExternalDoor)
                                            {
                                                //now set the helmet state depending on the external door state.
                                                if (_externaldoorstate == DoorState.CLOSED)
                                                {
                                                    Utilities.setHelmetshaders(kerbal.KerbalRef, false);
                                                }
                                                else
                                                {
                                                    Utilities.setHelmetshaders(kerbal.KerbalRef, true);
                                                }
                                            }
                                            this.Log_Debug("Reference part after add=" + this.vessel.GetReferenceTransformPart().name + ",flightid=" + this.vessel.GetReferenceTransformPart().flightID);
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
                                    else  //Seat is taken, but not by our frozen KErbal, we can't continue.
                                    {
                                        this.Log_Debug("Seat taken by someone else, Abort");
                                        Debug.Log("Could not start kerbal Thaw process as seat is taken by another kerbal. Very Very Bad. Report this to Mod thread");
                                        ScreenMessages.PostScreenMessage("Code Error: Cannot thaw kerbal at this time, Check Log", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                                        ThawKerbalAbort(frozenkerbal);
                                        return;
                                    }
                                }
                                else
                                // The Seat's Crew is set to NULL. This Should NEVER Happen. Except on UPGRADE from V0.17 and below.
                                {
                                    this.Log_Debug("Seat Crew KerbalRef is NULL re-add them at seatidx=" + tmpcrew.SeatIdx);
                                    //this.part.internalModel.seats[tmpcrew.SeatIdx].taken = false; // Set their seat to NotTaken before we assign them back to their seat, not sure we really need this.
                                    try
                                    {
                                        this.part.internalModel.SitKerbalAt(kerbal, this.part.internalModel.seats[tmpcrew.SeatIdx]);
                                        if (hasExternalDoor)
                                        {
                                            //set the seat to allow helmet, this will cause the helmet to appear
                                            kerbal.seat.allowCrewHelmet = true;
                                        }
                                        kerbal.seat.SpawnCrew();
                                        setseatstaticoverlay(this.part.internalModel.seats[tmpcrew.SeatIdx]);
                                        // Think this will get rid of the static that appears on the portrait camera

                                        if (!isPartAnimated) setCryoWindowOff(tmpcrew.SeatIdx);
                                        if (vesselisinIVA || vesselisinInternal)
                                            setIVAFrzrCam(tmpcrew.SeatIdx);
                                        if (hasExternalDoor)
                                        {
                                            //now set the helmet state depending on the external door state.
                                            if (_externaldoorstate == DoorState.CLOSED)
                                            {
                                                Utilities.setHelmetshaders(kerbal.KerbalRef, true);
                                            }
                                            else
                                            {
                                                Utilities.setHelmetshaders(kerbal.KerbalRef, true);
                                            }
                                        }
                                        this.Log_Debug("Reference part after add=" + this.vessel.GetReferenceTransformPart().name + ",flightid=" + this.vessel.GetReferenceTransformPart().flightID);
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
                            else //All DeepFreeze supplied parts have an internal. this is in case someone adds their own part with DeepFreezer Module attached.
                            {
                                this.Log_Debug("Part has no internals, just add");
                                try
                                {
                                    this.part.AddCrewmember(kerbal);  // Add them to the part anyway.
                                    seatTakenbyFrznKerbal[ToThawKerbalSeat] = false;
                                    kerbal.seat.SpawnCrew();
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
            OpenPodAnimPlaying = false;
            ProtoCrewMember kerbal = HighLogic.CurrentGame.CrewRoster.Crew.FirstOrDefault(a => a.name == ThawKerbal);
            this.part.RemoveCrewmember(kerbal);  // remove the CrewMember from the part, because they are frozen, and this is the only way to trick the game.
            kerbal.type = ProtoCrewMember.KerbalType.Unowned;
            kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
            seatTakenbyFrznKerbal[ToThawKerbalSeat] = true;
            if (vesselisinIVA || vesselisinInternal)
            {
                setIVAFrzrCam(ToThawKerbalSeat);
            }
            if (isPartAnimated)
            {
                closeCryopod(ToThawKerbalSeat);
                cryopodstateclosed[ToThawKerbalSeat] = true;
            }
            Utilities.setFrznKerbalLayer(kerbal, false, false);
            ScreenMessages.RemoveMessage(ThawMsg);
            this.Log_Debug("ThawkerbalAbort End");
        }

        private void ThawKerbalConfirm(String frozenkerbal)
        {
            this.Log_Debug("ThawKerbalConfirm start for " + frozenkerbal);
            machine_hum.Stop(); //stop sound effects
            StoredCharge = 0;   // Discharge all EC stored

            FrznCrewMbr tmpcrew = _StoredCrewList.Find(a => a.CrewName == frozenkerbal);  // Find the thawed kerbal in the frozen kerbal list.
            ProtoCrewMember kerbal = HighLogic.CurrentGame.CrewRoster.Crew.FirstOrDefault(a => a.name == frozenkerbal);
            //ProtoCrewMember kerbal = this.part.internalModel.seats[tmpcrew.SeatIdx].crew;

            // Re-Register crew to Part crew list, add experience traits, set seat references.
            kerbal.KerbalRef.InPart = this.part;
            this.part.protoModuleCrew.Add(kerbal);
            kerbal.RegisterExperienceTraits(this.part);
            kerbal.seat = this.part.internalModel.seats[tmpcrew.SeatIdx];
            kerbal.seatIdx = tmpcrew.SeatIdx;
            this.part.internalModel.seats[tmpcrew.SeatIdx].crew = kerbal;
            this.part.internalModel.seats[tmpcrew.SeatIdx].taken = true;
            setseatstaticoverlay(this.part.internalModel.seats[tmpcrew.SeatIdx]);
            //Utilities.setFrznKerbalLayer(crew, true, true);

            // Remove thawed kerbal from the frozen kerbals dictionary
            _StoredCrewList.Remove(tmpcrew); // Remove them from the frozen kerbal list.
            double timeFrozen = Planetarium.GetUniversalTime() - DFgameSettings.KnownFrozenKerbals[frozenkerbal].lastUpdate;
            DFgameSettings.KnownFrozenKerbals.Remove(frozenkerbal);
            DFgameSettings.DmpKnownFznKerbals();

            seatTakenbyFrznKerbal[ToThawKerbalSeat] = false;
            ToThawKerbal = ""; // Set the Active Thaw Kerbal to null
            IsThawActive = false; // Turn the Freezer actively thawing mode off

            KerbalGUIManager.AddActiveCrew(kerbal.KerbalRef);
            this.Log_Debug("Just thawed crew and added to GUIManager");
            KerbalGUIManager.PrintActiveCrew();
            ScreenMessages.PostScreenMessage(frozenkerbal + " thawed out", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            Debug.Log("Thawed out: " + frozenkerbal + " They were frozen for " + timeFrozen.ToString());
            UpdateCounts(); // Update the Crew counts
            removeThawEvent(frozenkerbal); // Remove the Thaw Event for this kerbal.
            ding_ding.Play();
            OpenPodAnimPlaying = false;
            onvslchgInternal = true;
            GameEvents.onVesselChange.Fire(vessel);
            GameEvents.onVesselWasModified.Fire(vessel);
            this.Log_Debug("ThawKerbalConfirm End");
        }

        #endregion ThwKerbals

        #region CrewXfers

        //This region contains the methods for handling Crew Transfers correctly
        internal bool IsSMXferRunning()  // Checks if Ship Manifest is running a CrewXfer or Not.
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

        internal bool IsSMXferStockRunning()  // Checks if Ship Manifest is running a Stock CrewXfer or Not.
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

        private ShipManifest.ICrewTransfer GetSMXfer()  // Checks if Ship Manifest is running a CrewXfer or Not.
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
                //If IsStockXfer = true than a StockXfer is running under SM control
                //When it finishes, SM will revert the Xfer and then run a normal SM CrewXfer.
                //So While the Stock Xfer is running we ignore this OnCrewTransferred event.
                //If SM does not have OverrideStockCrewXfer = true than it will ignore the event, so we bypass this
                // IF and the next IF and do a stock Xfer processing further down.
                this.Log_Debug("SMStockXfer?=" + SMObject.IsStockXfer);
                this.Log_Debug("SMXfer?=" + SMObject.CrewXferActive);
                this.Log_Debug("SMseat2seat?=" + SMObject.IsSeat2SeatXfer);
                this.Log_Debug("OverrideStock?=" + SMObject.OverrideStockCrewXfer);
                crewXferSMTimeDelay = SMObject.CrewXferDelaySec;
                if (SMObject.IsStockXfer || (!SMObject.CrewXferActive && SMObject.OverrideStockCrewXfer))
                {
                    //we need to just check one thing, that if this a xfer to the part that isn't FULL of frozen kerbals.
                    //If it is we must take over and revert.
                    this.Log_Debug("Partfull=" + PartFull);
                    this.Log_Debug("to part is this part=" + fromToAction.to + " - " + this.part);
                    if (FreezerSpace == 0 && fromToAction.to == this.part)  // If there is no available seats for this Kerbal we kick them back out.
                    {
                        _crewXferTOActive = true; // Set a flag to know a Xfer has started and we check when it is finished in
                        savecryopodstatepersistent();
                        saveexternaldoorstatepersistent();
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
                        xferisfromEVA = false;
                        xferfromPart = fromToAction.from;
                        xferfromSeat = fromToAction.host.seat;
                        xfertoPart = fromToAction.to;
                        xfercrew = fromToAction.host;
                        setseatstaticoverlay(xfercrew.seat);
                        xferbackwhenFull = true;
                        return;
                    }

                    Utilities.Log_Debug("Stock Xfer is running with Ship Manifest Override, so we ignore the Stock Xfer");
                    return;
                }

                //This is a normal Ship Manifest Crew Xfer or an over-riden Stock Crew Xfer from SM
                if (SMObject.CrewXferActive)
                {
                    Utilities.Log_Debug("SMXfer is running");
                    FlightEVA.fetch.DisableInterface();
                    savecryopodstatepersistent();
                    saveexternaldoorstatepersistent();
                    crewXferSMActive = SMObject.CrewXferActive;
                    //crewXferSMStock = SMObject.IsStockXfer;
                    if (SMObject.FromPart == this.part)
                    {
                        removeFreezeEvent(fromToAction.host.name);
                        _crewXferFROMActive = true;  // Set a flag to know a Xfer has started and we check when it is finished in
                        xferfromPart = SMObject.FromPart;
                        xfertoPart = SMObject.ToPart;
                        xfercrew = fromToAction.host;
                        timecrewXferFROMfired = Time.time;
                    }
                    if (SMObject.ToPart == this.part)
                    {
                        _crewXferTOActive = true; // Set a flag to know a Xfer has started and we check when it is finished in
                        xferfromPart = SMObject.FromPart;
                        xfertoPart = SMObject.ToPart;
                        xfercrew = fromToAction.host;
                        xfertoSeat = SMObject.ToSeat;
                        setseatstaticoverlay(SMObject.ToSeat);
                        timecrewXferTOfired = Time.time;
                    }
                    return;
                }
                else
                {
                    Utilities.Log_Debug("No SMXfer running");
                }
            }

            //Stock Transfers only past here, or no Stock Xfer override is active within SM. So it must be stock
            savecryopodstatepersistent();
            saveexternaldoorstatepersistent();
            if (fromToAction.from == this.part)  // if the Xfer is FROM this part
            {
                Utilities.Log_Debug("DeepFreezer", "crewXferFROMActive");
                FlightEVA.fetch.DisableInterface();
                removeFreezeEvent(fromToAction.host.name);  // Remove the Freeze Event for the crewMember leaving the part
                if (fromToAction.to.Modules.Cast<PartModule>().Any(x => x is KerbalEVA)) // Kerbal is going EVA
                {
                    return;
                }
                _crewXferFROMActive = true;  // Set a flag to know a Xfer has started and we check when it is finished in
                xferfromPart = fromToAction.from;
                xfertoPart = fromToAction.to;
                xfercrew = fromToAction.host;
                timecrewXferFROMfired = Time.time;
                return;
            }

            if (fromToAction.to == this.part)  // if the Xfer is TO this part
            {
                Utilities.Log_Debug("DeepFreezer", "crewXferTOActive");
                FlightEVA.fetch.DisableInterface();
                _crewXferTOActive = true; // Set a flag to know a Xfer has started and we check when it is finished in

                if (fromToAction.from.Modules.Cast<PartModule>().Any(x => x is KerbalEVA)) // Kerbal is entering from EVA
                {
                    Utilities.Log_Debug("DeepFreezer", "CrewXfer xferisfromEVA = true");
                    xferisfromEVA = true;
                    xferfromPart = null;
                    xferfromSeat = null;
                    xfertoPart = fromToAction.to;
                    xfercrew = fromToAction.host;
                    Utilities.Log_Debug("CrewXFER host seatidx=" + xfercrew.seatIdx);
                    foreach (FrznCrewMbr lst in _StoredCrewList)
                    {
                        Utilities.Log_Debug("CrewXFER Frozen Crew SeatIdx= " + lst.SeatIdx + ",Seattaken=" + this.part.internalModel.seats[lst.SeatIdx].taken);
                    }
                }
                else
                {
                    Utilities.Log_Debug("DeepFreezer", "CrewXfer xferisfromEVA = false");
                    xferisfromEVA = false;
                    xferfromPart = fromToAction.from;
                    xferfromSeat = fromToAction.host.seat;
                    xfertoPart = fromToAction.to;
                    xfercrew = fromToAction.host;
                    setseatstaticoverlay(xfercrew.seat);
                }
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
                    xferbackwhenFull = true;
                }
                timecrewXferTOfired = Time.time;
                Utilities.Log_Debug("DeepFreezer", "crewXferTOActive end");
            }
        }

        private void completeCrewTransferProcessing()
        {
            //First we deal with a Full Freezer Scenario
            if (_crewXferTOActive && xferbackwhenFull)
            {
                Debug.Log("Crew XferTO Active, but freezer is full, revert Xfer");
                xferBackifPartisFull();
                FlightEVA.fetch.EnableInterface();
                return;
            }

            //Now we check for time outs.
            double TimeDelay = DFsettings.defaultTimeoutforCrewXfer + crewXferSMTimeDelay;
            if (_crewXferFROMActive && (Time.time - timecrewXferFROMfired) > TimeDelay)
            {
                //Cancel
                Debug.Log("CrewXfer Timed OUT, Cancelling Tracking of CrewXfer");
                resetFrozenKerbals();
                resetCryopods(true);
                _crewXferFROMActive = false;
                crewXferSMActive = false;
                crewXferSMStock = false;
                FlightEVA.fetch.EnableInterface();
                return;
            }

            if (_crewXferTOActive && (Time.time - timecrewXferTOfired) > TimeDelay)
            {
                //Cancel
            }

            // Check Crew Xfers in action and deal with them
            if (_crewXferFROMActive)
            {
                Debug.Log("Crew XferFROM Active, checking if complete");
                if (DFInstalledMods.SMInstalled && crewXferSMActive) //Xfer from this part SM Xfer
                {
                    if (IsSMXferRunning())
                    {
                        Utilities.Log_Debug("DeepFreezer", "CrewXfer SMxfer and it's still running, so wait");
                        return;
                    }
                    else // It's finished
                    {
                        resetFrozenKerbals();
                        resetCryopods(true);
                        _crewXferFROMActive = false;
                        crewXferSMActive = false;
                        crewXferSMStock = false;
                        FlightEVA.fetch.EnableInterface();
                        Utilities.Log_Debug("DeepFreezer", "CrewXferFROM SMXfer Completed");
                        return;
                    }
                }
                else // Xfer from this part Stock Xfer
                {
                    ProtoCrewMember crew = this.part.protoModuleCrew.FirstOrDefault(a => a.name == xfercrew.name);
                    if (crew == null)  // they have left the part, so xfer is finished
                    {
                        resetFrozenKerbals();
                        resetCryopods(true);
                        _crewXferFROMActive = false;
                        crewXferSMActive = false;
                        crewXferSMStock = false;
                        FlightEVA.fetch.EnableInterface();
                        Utilities.Log_Debug("DeepFreezer", "CrewXferFROM Stock Completed");
                        return;
                    }
                    return;
                }
            } // End Crew Xfer FROM

            if (_crewXferTOActive)
            {
                Debug.Log("Crew XferTO Active, checking if complete");
                if (DFInstalledMods.SMInstalled && crewXferSMActive)        //Xfer to this part SM Xfer
                {
                    if (IsSMXferRunning())
                    {
                        Utilities.Log_Debug("DeepFreezer", "CrewXfer SMxfer and it's still running, so wait");
                        //setseatstaticoverlay(xfertoSeat);
                        return;
                    }
                    else // It's finished
                    {
                        setseatstaticoverlay(xfertoSeat);
                        resetFrozenKerbals();
                        if (xferisfromEVA)
                        {
                            resetCryopods(true);
                        }
                        else
                        {
                            resetCryopods(true);
                        }
                        xferisfromEVA = false;
                        _crewXferTOActive = false;
                        crewXferSMActive = false;
                        crewXferSMStock = false;
                        FlightEVA.fetch.EnableInterface();
                        Utilities.Log_Debug("DeepFreezer", "CrewXferTO SMXfer Completed");
                        return;
                    }
                }
                else // Xfer to this part Stock Xfer
                {
                    Utilities.Log_Debug("DeepFreezer", "CrewXfer active & SMXfer is not active, so checking");
                    //Check if the crewmember is now in the part
                    ProtoCrewMember crew = this.part.protoModuleCrew.FirstOrDefault(a => a.name == xfercrew.name);
                    if (crew != null) // they are in the part, so xfer is finished
                    {
                        Utilities.Log_Debug("Already on-board, check seat allocation");
                        if (seatTakenbyFrznKerbal[crew.seatIdx] == true)
                        {
                            //Seat is taken by frozen kerbal, find them an empty seat
                            Utilities.Log_Debug("Seat is taken by frozen kerbal, find them an empty seat");
                            bool foundseat = false;
                            for (int i = 0; i < seatTakenbyFrznKerbal.Length; i++)
                            {
                                if (seatTakenbyFrznKerbal[i] == false && this.part.internalModel.seats[i].taken == false)
                                {
                                    Utilities.Log_Debug("they can sit at seat=" + i);
                                    foundseat = true;
                                    this.part.RemoveCrewmember(xfercrew);
                                    this.part.AddCrewmemberAt(xfercrew, i);
                                    IvaUpdateActive = true;
                                    IvaPortraitDelay = 0;
                                    break;
                                }
                            }
                            if (!foundseat) //If we didn't find a seat Transfer them back.
                            {
                                xferBackifPartisFull();
                                FlightEVA.fetch.EnableInterface();
                                return;
                            }
                        }
                        // We found them a seat so complete the Xfer

                        setseatstaticoverlay(xfercrew.seat);
                        resetFrozenKerbals();
                        if (xferisfromEVA)
                        {
                            if (hasExternalDoor)
                            {
                                setHelmetstoDoorState();
                                setDoorHandletoDoorState();
                            }
                            resetCryopods(true);
                        }
                        else
                        {
                            resetCryopods(true);
                        }
                        xferisfromEVA = false;
                        _crewXferTOActive = false;
                        crewXferSMActive = false;
                        crewXferSMStock = false;
                        FlightEVA.fetch.EnableInterface();
                        Utilities.Log_Debug("DeepFreezer", "CrewXferTO Stock Completed");
                        return;
                    }
                    else
                    {
                        this.Log_Debug("CrewXfer still not completed");
                        return;
                    }
                }
            } // End Crew Xfer TO
        }

        // Transfer a Kerbal back to where they came from if the part is actually full.
        // This is because Stock KSP doesn't see our frozen kerbals.
        private void xferBackifPartisFull()
        {
            this.Log_Debug("Transfer Back if Part is FULL start");
            if (xferisfromEVA)  // if it was from EVA send them back outside.
            {
                Utilities.Log_Debug("DeepFreezer", "CrewXfer xferisfromEVA = true kick them out to EVA");
                if (hasExternalDoor)
                {
                    setHelmetstoDoorState();
                    setDoorHandletoDoorState();
                }
                resetFrozenKerbals();
                resetCryopods(true);
                xferbackwhenFull = false;
                FlightEVA.fetch.EnableInterface();
                xferisfromEVA = false;
                _crewXferTOActive = false;
                crewXferSMActive = false;
                crewXferSMStock = false;
                FlightEVA.fetch.spawnEVA(xfercrew, xfertoPart, xfertoPart.airlock);
                CameraManager.Instance.SetCameraFlight();
            }
            else // it wasn't from EVA so send them back to the part they came from.
            {
                Utilities.Log_Debug("DeepFreezer", "CrewXfer xferisfromEVA = false kick them out to from part");
                this.part.RemoveCrewmember(xfercrew);
                xferfromPart.AddCrewmember(xfercrew);
                setseatstaticoverlay(xfercrew.seat);
                resetFrozenKerbals();
                resetCryopods(true);
                IvaUpdateActive = true;
                IvaPortraitDelay = 0;
            }
            onvslchgInternal = true;
            xferbackwhenFull = false;
            FlightEVA.fetch.EnableInterface();
            GameEvents.onVesselChange.Fire(vessel);
            ScreenMessages.PostScreenMessage("Freezer is Full, cannot enter at this time", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            xferisfromEVA = false;
            _crewXferTOActive = false;
            crewXferSMActive = false;
            crewXferSMStock = false;
            FlightEVA.fetch.EnableInterface();
            Utilities.Log_Debug("Transfer back if Part is FULL ended");
        }

        // this is called when a vessel change event fires.
        private void OnVesselChange(Vessel vessel)
        {
            Debug.Log("OnVesselChange activevessel " + FlightGlobals.ActiveVessel.id + " parametervesselid " + vessel.id + " this vesselid " + this.vessel.id + " this partid " + part.flightID);
            if (onvslchgInternal)
            {
                onvslchgInternal = false;
                onvslchgExternal = false;
                return;
            }
            onvslchgExternal = true;
            //If the vessel we have changed to is the same as the vessel this partmodule is attached to we LOAD persistent vars, otherwise we SAVE persistent vars.
            if (vessel.id == this.vessel.id)
            {
                loadcryopodstatepersistent();
                loadexternaldoorstatepersistent();
                resetFrozenKerbals();
            }
            else
            {
                savecryopodstatepersistent();
                saveexternaldoorstatepersistent();
            }
        }

        private void resetFrozenKerbals()
        {
            // Iterate through the dictionary of all known frozen kerbals
            foreach (KeyValuePair<string, KerbalInfo> kerbal in DFgameSettings.KnownFrozenKerbals)
            {
                // if the known kerbal is in this part in this vessel
                if (kerbal.Value.vesselID == CrntVslID && kerbal.Value.partID == CrntPartID)
                {
                    FrznCrewMbr fzncrew = new FrznCrewMbr(kerbal.Key, kerbal.Value.seatIdx, CrntVslID, CrntVslName);
                    FrznCrewMbr tmpcrew = _StoredCrewList.Find(a => a.CrewName == kerbal.Key);
                    if (tmpcrew == null)
                    {
                        //add them to our storedcrewlist for this part.
                        Utilities.Log_Debug("DeepFreezer", "Adding frozen kerbal to this part storedcrewlist " + kerbal.Key);

                        _StoredCrewList.Add(fzncrew);
                    }
                    //check if they are in part spawned, if not do so.
                    ProtoCrewMember crewmember = HighLogic.CurrentGame.CrewRoster.Unowned.FirstOrDefault(a => a.name == kerbal.Key);
                    crewmember.seatIdx = kerbal.Value.seatIdx;
                    crewmember.seat = this.part.internalModel.seats[crewmember.seatIdx];
                    if (crewmember.KerbalRef == null)
                    {
                        this.Log_Debug("Kerbal kerbalref is null, respawn");
                        crewmember.Spawn();
                        crewmember.KerbalRef.transform.parent = this.part.internalModel.seats[crewmember.seatIdx].seatTransform;
                        crewmember.KerbalRef.transform.localPosition = Vector3.zero;
                        crewmember.KerbalRef.transform.localRotation = Quaternion.identity;
                        crewmember.KerbalRef.InPart = null;
                        if (hasExternalDoor)
                        {
                            //set the seat to allow helmet, this will cause the helmet to appear
                            crewmember.KerbalRef.showHelmet = true;
                        }
                        else
                        {
                            crewmember.KerbalRef.showHelmet = false;
                            crewmember.KerbalRef.ShowHelmet(false);
                        }
                        this.Log_Debug("Kerbal kerbalref = " + crewmember.KerbalRef.GetInstanceID());
                    }
                    else
                    {
                        this.Log_Debug("Kerbal kerbalref reset");
                        //crewmember.Spawn();
                        crewmember.KerbalRef.transform.parent = this.part.internalModel.seats[crewmember.seatIdx].seatTransform;
                        crewmember.KerbalRef.transform.localPosition = Vector3.zero;
                        crewmember.KerbalRef.transform.localRotation = Quaternion.identity;
                        crewmember.KerbalRef.InPart = null;
                        if (hasExternalDoor)
                        {
                            //set the seat to allow helmet, this will cause the helmet to appear
                            crewmember.KerbalRef.showHelmet = true;
                        }
                        else
                        {
                            crewmember.KerbalRef.showHelmet = false;
                            crewmember.KerbalRef.ShowHelmet(false);
                        }
                        this.Log_Debug("Kerbal kerbalref = " + crewmember.KerbalRef.GetInstanceID());
                    }
                    seatTakenbyFrznKerbal[crewmember.seatIdx] = true;
                    //setup seat and part settings for frozen kerbal.
                    Utilities.setFrznKerbalLayer(crewmember, false, false);
                    crewmember.UnregisterExperienceTraits(this.part);
                    this.part.protoModuleCrew.Remove(crewmember);
                    this.part.internalModel.seats[crewmember.seatIdx].taken = true;
                    this.part.internalModel.seats[crewmember.seatIdx].kerbalRef = crewmember.KerbalRef;
                    this.part.internalModel.seats[crewmember.seatIdx].crew = crewmember;
                    setseatstaticoverlay(this.part.internalModel.seats[crewmember.seatIdx]);
                    if (KerbalGUIManager.ActiveCrew.Contains(crewmember.KerbalRef))
                    {
                        KerbalGUIManager.RemoveActiveCrew(crewmember.KerbalRef);
                    }
                }
                else
                {
                    Utilities.Log_Debug("DeepFreezer", kerbal.Key + "," + kerbal.Value.vesselID + "," + kerbal.Value.partID + " not this vessel/part");
                }
            }
        }

        private void OnCrewBoardVessel(GameEvents.FromToAction<Part, Part> fromToAction)
        {
            Debug.Log("OnCrewBoardVessel " + vessel.id + " " + part.flightID);
            onvslchgExternal = true;
        }

        private void onCrewOnEva(GameEvents.FromToAction<Part, Part> fromToAction)
        {
            Debug.Log("OnCrewOnEva " + vessel.id + " " + part.flightID);
            onvslchgExternal = true;
        }

        #endregion CrewXfers

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
                    if (!isPartAnimated) //Only applicable for non-animated internals
                    {
                        // set cryotube window off for all crew onboard the part
                        Utilities.Log_Debug("DeepFreezer", "Checking all actual crewseats status");
                        foreach (ProtoCrewMember onbrdcrew in this.part.protoModuleCrew)
                        {
                            setCryoWindowOff(onbrdcrew.seatIdx);
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
                                setCryoWindowOff(i);
                            }
                            else kerblrefstring = chkpartseats.kerbalRef.crewMemberName;
                            Utilities.Log_Debug("DeepFreezer", "seatXformName=" + chkpartseats.seatTransformName + ",SeatIndex=" + i + ",KerbalRef=" + kerblrefstring);
                            i++;
                        }
                    }

                    // reset seats to TAKEN for all frozen kerbals in the part, check KerbalRef is still in place or re-instantiate it and check frozen kerbals
                    // are not appearing in the Portrait Cameras, if they are remove them.
                    Utilities.Log_Debug("DeepFreezer", "StoredCrewList");
                    foreach (FrznCrewMbr lst in _StoredCrewList)
                    {
                        this.part.internalModel.seats[lst.SeatIdx].taken = true;
                        seatTakenbyFrznKerbal[lst.SeatIdx] = true;
                        if (!isPartAnimated) setCryoWindowOn(lst.SeatIdx);
                        ProtoCrewMember kerbal = HighLogic.CurrentGame.CrewRoster.Unowned.FirstOrDefault(a => a.name == lst.CrewName);
                        if (kerbal == null)
                        {
                            Utilities.Log("DeepFreezer", "Frozen Kerbal " + lst.CrewName + " is not found in the currentgame.crewroster.unowned, this should never happen");
                        }
                        else
                        {
                            if (kerbal.KerbalRef == null)  // Check if the KerbalRef is null, as this causes issues with CrewXfers, if it is, respawn it.
                            {
                                this.Log_Debug("Kerbalref = null");
                                kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                                kerbal.type = ProtoCrewMember.KerbalType.Unowned;
                                this.part.internalModel.seats[lst.SeatIdx].crew = kerbal;
                                this.part.internalModel.seats[lst.SeatIdx].SpawnCrew();  // This spawns the Kerbal and sets the seat.kerbalref
                                setseatstaticoverlay(this.part.internalModel.seats[lst.SeatIdx]);
                                Utilities.CheckPortraitCams(vessel);
                            }
                            Utilities.setFrznKerbalLayer(kerbal, false, false);  // Double check kerbal is invisible.
                        }
                        string kerblrefstring;
                        if (this.part.internalModel.seats[lst.SeatIdx].kerbalRef == null) kerblrefstring = "kerbalref not found";
                        else kerblrefstring = this.part.internalModel.seats[lst.SeatIdx].kerbalRef.crewMemberName;
                        Utilities.Log_Debug("DeepFreezer", "Frozen Crew SeatIdx= " + lst.SeatIdx + ",Seattaken=" + this.part.internalModel.seats[lst.SeatIdx].taken + ",KerbalRef=" + kerblrefstring);
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

        #region Cryopods

        //This region contains the methods for animating the cryopod doors and turning windows on/off (if not animated)
        private void loadcryopodstatepersistent()
        {
            try
            {
                var cryopodstatestring = cryopodstateclosedstring.Split(',');
                for (int i = 0; i < cryopodstatestring.Length; i++)
                {
                    this.Log_Debug("parse cryopodstring " + i + " " + cryopodstatestring[i]);
                    cryopodstateclosed[i] = bool.Parse(cryopodstatestring[i]);
                }
                Debug.Log("Load cryopodstatepersistent value " + cryopodstateclosedstring);
            }
            catch (Exception ex)
            {
                Debug.Log("Exception OnLoad of cryopod state string");
                Debug.Log("Err: " + ex);
            }
        }

        private void savecryopodstatepersistent()
        {
            //if (HighLogic.LoadedSceneIsFlight && Time.timeSinceLevelLoad > 3f)
            //if (HighLogic.LoadedSceneIsFlight)
            //{
            try
            {
                cryopodstateclosedstring = string.Empty;
                cryopodstateclosedstring = string.Join(", ", cryopodstateclosed.Select(b => b.ToString()).ToArray());
                Debug.Log("Save cryopodstatepersistent value " + cryopodstateclosedstring);
            }
            catch (Exception ex)
            {
                Debug.Log("Exception OnSave of cryopod state string");
                Debug.Log("Err: " + ex);
            }
            //}
        }

        public void resetCryopods(bool resetall)
        {
            try
            {
                if (resetall)
                {
                    double currenttime = Planetarium.GetUniversalTime();
                    if (currenttime - cryopodResetTime < DFsettings.cryopodResettimeDelay)
                    {
                        this.Log_Debug("Last cryopod resetall occurred at: " + cryopodResetTime + " currenttime: " + currenttime + " is less than " + DFsettings.cryopodResettimeDelay + " secs ago, Ignoring request.");
                        return;
                    }
                    cryopodResetTime = currenttime;
                    for (int i = 0; i < FreezerSize; i++)
                    {
                        cryopodstateclosed[i] = true;
                    }
                }
                bool[] closedpods = new bool[FreezerSize];
                for (int i = 0; i < FreezerSize; i++)
                {
                    this.Log_Debug("cryopodstate closed=" + cryopodstateclosed[i].ToString() + " check pod " + i);
                }
                foreach (FrznCrewMbr frzncrew in _StoredCrewList)
                {
                    closedpods[frzncrew.SeatIdx] = true;
                }
                for (int i = 0; i < closedpods.Length; i++)
                {
                    this.Log_Debug("resetCryopod " + i + " contains frozen kerbal? " + closedpods[i]);
                    if (closedpods[i])
                    {
                        if (isPartAnimated)
                        {
                            if (!cryopodstateclosed[i])
                            {
                                this.Log_Debug("pod is open so close it");
                                closeCryopod(i);
                                cryopodstateclosed[i] = true;
                            }
                            else
                            {
                                this.Log_Debug("pod is already closed");
                            }
                        }
                        else
                        {
                            setCryoWindowOn(i);
                        }
                    }
                    else
                    {
                        if (isPartAnimated)
                        {
                            if (cryopodstateclosed[i])
                            {
                                this.Log_Debug("pod is closed so open it");
                                openCryopod(i);
                                cryopodstateclosed[i] = false;
                            }
                            else
                            {
                                this.Log_Debug("pod is already open");
                            }
                        }
                        else
                        {
                            setCryoWindowOff(i);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to reset cryopods in internal model for " + this.part.vessel.id.ToString() + " " + this.part.flightID);
                Debug.Log("Err: " + ex);
            }
        }

        private void setCryoWindowOff(int SeatIndx)  //only called for non-animated internal parts
        {
            Renderer renderer;
            string windowname = "Cryopod-" + (SeatIndx + 1).ToString() + "-Window";
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
                Debug.Log("Unable to find Renderer in internal model for this part called " + windowname);
                Debug.Log("Err: " + ex);
            }
        }

        private void setCryoWindowOn(int SeatIndx) //only called for non-animated internal parts
        {
            Renderer renderer;
            string windowname = "Cryopod-" + (SeatIndx + 1).ToString() + "-Window";
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
                Debug.Log("Unable to find Renderer in internal model for this part called " + windowname);
                Debug.Log("Err: " + ex);
            }
            try
            {
                Renderer windowrenderer = this.part.internalModel.FindModelComponent<Renderer>(windowname);
                windowrenderer.material.shader = KSPSpecularShader;
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to set specular in internal model for this part called " + windowname);
                Debug.Log("Err: " + ex);
            }
        }

        private void openCryopod(int seatIndx) //only called for animated internal parts
        {
            string podname = "Animated-Cryopod-" + (seatIndx + 1).ToString();
            string windowname = "Animated-Cryopod-" + (seatIndx + 1).ToString() + "-Window";
            this.Log_Debug("playing animation opencryopod " + podname);
            try
            {
                _animation = this.part.internalModel.FindModelComponent<Animation>(podname);
                if (_animation != null)
                {
                    if (cryopodstateclosed[seatIndx])
                    {
                        _animation.Play("Open");
                        cryopodstateclosed[seatIndx] = false;
                    }
                    else
                    {
                        this.Log_Debug("pod already open so do nothing");
                    }
                }
                else
                    this.Log_Debug("animation not found");
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to find animation in internal model for this part called " + podname);
                Debug.Log("Err: " + ex);
            }
            try
            {
                Renderer windowrenderer = this.part.internalModel.FindModelComponent<Renderer>(windowname);
                windowrenderer.material.shader = KSPTranslucentShader;
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to set translucentshader in internal model for this part called " + windowname);
                Debug.Log("Err: " + ex);
            }
        }

        private void closeCryopod(int seatIndx) //only called for animated internal parts
        {
            string podname = "Animated-Cryopod-" + (seatIndx + 1).ToString();
            string windowname = "Animated-Cryopod-" + (seatIndx + 1).ToString() + "-Window";
            this.Log_Debug("playing animation closecryopod " + podname);
            try
            {
                _animation = this.part.internalModel.FindModelComponent<Animation>(podname);
                if (_animation != null)
                {
                    if (!cryopodstateclosed[seatIndx])
                    {
                        _animation.Play("Close");
                        cryopodstateclosed[seatIndx] = true;
                    }
                    else
                    {
                        this.Log_Debug("pod already closed so do nothing");
                    }
                }
                else
                    this.Log_Debug("animation not found");
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to find animation in internal model for this part called " + podname);
                Debug.Log("Err: " + ex);
            }
            try
            {
                Renderer windowrenderer = this.part.internalModel.FindModelComponent<Renderer>(windowname);
                windowrenderer.material.shader = KSPSpecularShader;
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to set specular in internal model for this part called " + windowname);
                Debug.Log("Err: " + ex);
            }
        }

        //This method sets the internal camera to the Freezer view prior to thawing or freezing a kerbal so we can see the nice animations.
        private void setIVAFrzrCam(int seatIndx)
        {
            string camname = "FrzCam" + (seatIndx + 1).ToString();
            this.Log_Debug("Setting FrzrCam " + camname);
            Camera cam = this.part.internalModel.FindModelComponent<Camera>(camname);
            if (cam != null)
            {
                Transform camxform = cam.transform;
                if (camxform != null)
                {
                    CameraManager.Instance.SetCameraInternal(this.part.internalModel, camxform);
                }
            }
        }

        private void setseatstaticoverlay(InternalSeat seat)
        {
            try
            {
                if (seat.kerbalRef != null)
                {
                    seat.kerbalRef.staticOverlayDuration = 0f;
                    seat.kerbalRef.state = Kerbal.States.ALIVE;
                }
            }
            catch (Exception ex)
            {
                Utilities.Log("DeepFreezer", " Error attempting to change staticoverlayduration");
                Utilities.Log("DeepFreezer ", ex.Message);
            }
        }

        #endregion Cryopods

        #region ExternalDoor

        private IEnumerator openDoors(float speed)
        {
            _prevexterndoorstate = _externaldoorstate;
            _externaldoorstate = DoorState.OPENING;
            if (animationName != null)
            {
                externalDoorAnim[animationName].normalizedTime = 0;
                externalDoorAnim[animationName].speed = speed;
                externalDoorAnim.Play("Open");
                IEnumerator wait = Utilities.WaitForAnimation(externalDoorAnim, "Open");
                while (wait.MoveNext()) yield return null;
            }
            Events["eventCloseDoors"].active = true;
            _prevexterndoorstate = _externaldoorstate;
            _externaldoorstate = DoorState.OPEN;
        }

        private IEnumerator closeDoors(float speed)
        {
            _prevexterndoorstate = _externaldoorstate;
            _externaldoorstate = DoorState.CLOSING;
            if (animationName != null)
            {
                externalDoorAnim[animationName].normalizedTime = 1;
                externalDoorAnim[animationName].speed = speed;
                externalDoorAnim.Play("Open");
                IEnumerator wait = Utilities.WaitForAnimation(externalDoorAnim, "Open");
                while (wait.MoveNext()) yield return null;
            }
            Events["eventOpenDoors"].active = true;
            _prevexterndoorstate = _externaldoorstate;
            _externaldoorstate = DoorState.CLOSED;
        }

        private void setHelmetstoDoorState()
        {
            if (_externaldoorstate == DoorState.CLOSED)
            {
                Utilities.setHelmets(this.part, false);
            }
            else
            {
                Utilities.setHelmets(this.part, true);
            }
        }

        private void setDoorHandletoDoorState()
        {
            if (_externaldoorstate == DoorState.OPEN || _externaldoorstate == DoorState.OPENING)
            {
                try
                {
                    Animation anim;
                    Animation[] animators = this.part.internalModel.FindModelAnimators("DOORHandle");
                    if (animators.Length > 0)
                    {
                        anim = animators[0];
                        anim["DOORHandle"].speed = float.MaxValue;
                        anim["DOORHandle"].normalizedTime = 0;
                        anim.Play("DOORHandle");
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log("Exception trying to run the Doorhandle animation");
                    Debug.Log("Err: " + ex);
                }
            }
        }

        private void loadexternaldoorstatepersistent()
        {
            try
            {
                if (externaldoorstate == "OPEN")
                {
                    _externaldoorstate = DoorState.OPEN;
                }
                else
                {
                    _externaldoorstate = DoorState.CLOSED;
                }
                if (prevexterndoorstate == "OPEN")
                {
                    _prevexterndoorstate = DoorState.OPEN;
                }
                else
                {
                    _prevexterndoorstate = DoorState.CLOSED;
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Exception OnLoad of external door state string");
                Debug.Log("Err: " + ex);
            }
        }

        private void saveexternaldoorstatepersistent()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (_externaldoorstate == DoorState.OPEN || _externaldoorstate == DoorState.OPENING)
                {
                    externaldoorstate = "OPEN";
                }
                else
                {
                    externaldoorstate = "CLOSED";
                }
                if (_prevexterndoorstate == DoorState.OPEN || _externaldoorstate == DoorState.OPENING)
                {
                    prevexterndoorstate = "OPEN";
                }
                else
                {
                    prevexterndoorstate = "CLOSED";
                }
            }
            else
            {
                externaldoorstate = "CLOSED";
                prevexterndoorstate = "CLOSED";
            }
        }

        //private DoorState setdoorState()
        //{
        //return _externaldoorstate;
        /*
        Animation anim = this.part.FindModelAnimators(animationName).FirstOrDefault();
        if (anim != null)
        {
            if (anim[animationName].normalizedTime == 0f) //closed
            {
                this.Log_Debug("setdoorState closed");
                Events["eventOpenDoors"].active = true;
                Events["eventCloseDoors"].active = false;
                return DoorState.CLOSED;
            }
            if (anim[animationName].normalizedTime == 1f) //open
            {
                this.Log_Debug("setdoorState open");
                return DoorState.OPEN;
            }
            return DoorState.UNKNOWN;
        }
        else
        {
            this.Log_Debug("setdoorState Animation not found");
            return DoorState.UNKNOWN;
        }*/
        //}

        #endregion ExternalDoor

        #region BackgroundProcessing

        private const String MAIN_POWER_NAME = "ElectricCharge";

        /*
        public static void BackgroundLoad(Vessel v, uint partFlightId, ref System.Object data)
        {
            // We need the FrzChargeRequired field from the part's config node.
            bool debug = DeepFreeze.Instance.DFsettings.debugging;

            try
            {
                ProtoPartSnapshot partsnapshot = v.protoVessel.protoPartSnapshots.Find(p => p.flightID == partFlightId);
                ProtoPartModuleSnapshot modulesnapshot = partsnapshot.modules.Find(m => m.moduleName == "DeepFreezer");
                string strFrznChargeRequired = modulesnapshot.moduleValues.GetValue("FrznChargeRequired");
                int FrznChargeRequired = 0;
                bool success = Int32.TryParse(strFrznChargeRequired, out FrznChargeRequired);
                data = FrznChargeRequired;
                if (debug) Debug.Log("BackgroundLoad vessel " + v + " partID " + partFlightId + " FrznChargeRequired = " + FrznChargeRequired + " data = " + data.ToString());
            }
            catch (Exception ex)
            {
                Debug.Log("Exception BackgroundLoad of DeepFreezer partmodule settings");
                Debug.Log("Err: " + ex);
            }
        }
        */

        //This method is called by the BackgroundProcessing DLL, if the user has installed it. Otherwise it will never be called.
        //It will consume ElectricCharge for Freezer that contain frozen kerbals for vessels that are unloaded, if the user has turned on the ECreqdForFreezer option in the settings menu.
        public static void FixedBackgroundUpdate(Vessel v, uint partFlightID, Func<Vessel, float, string, float> resourceRequest, ref System.Object data)
        {
            bool debug = DeepFreeze.Instance.DFsettings.debugging;
            if (debug) Debug.Log("FixedBackgroundUpdate vesselID " + v.id + " partID " + partFlightID);
            // If the user does not have ECreqdForFreezer option ON, then we do nothing and return
            if (!DeepFreeze.Instance.DFsettings.ECreqdForFreezer)
            {
                if (debug) Debug.Log("FixedBackgroundUpdate ECreqdForFreezer is OFF, nothing to do");
                return;
            }
            // If the vessel this module is attached to is NOT stored in the DeepFreeze dictionary of known deepfreeze vessels we can't do anything, But this should NEVER happen.
            VesselInfo vslinfo;
            if (!DeepFreeze.Instance.DFgameSettings.knownVessels.TryGetValue(v.id, out vslinfo))
            {
                if (debug) Debug.Log("FixedBackgroundUpdate unknown vessel, cannot process");
                return;
            }
            //Except if there are no frozen crew on board we don't need to consume any EC
            if (vslinfo.numFrznCrew == 0)
            {
                if (debug) Debug.Log("FixedBackgroundUpdate No Frozen Crew on-board, nothing to do");
                return;
            }
            PartInfo partInfo;
            if (!DeepFreeze.Instance.DFgameSettings.knownFreezerParts.TryGetValue(partFlightID, out partInfo))
            {
                if (debug) Debug.Log("FixedBackgroundUpdate Can't get the Freezer Part Information, so cannot process");
                return;
            }
            // OK now we have something to do for real.
            // Calculate the time since last consumption of EC, then calculate the EC required and request it from BackgroundProcessing DLL.
            // If the vessel runs out of EC the DeepFreezeGUI class will handle notifying the user, not here.
            double currenttime = Planetarium.GetUniversalTime();
            if (Utilities.timewarpIsValid(5))
            {
                double timeperiod = currenttime - (double)partInfo.timeLastElectricity;
                if (timeperiod >= 1f && partInfo.numFrznCrew > 0) //We have frozen Kerbals, consume EC
                {
                    //int FrznChargeRequired = Convert.ToInt32(data);
                    //Utilities.Log_Debug(" data frznchargerequired " + FrznChargeRequired + " partinfo frznchargereqed " + partInfo.frznChargeRequired);

                    double Ecreqd = ((partInfo.frznChargeRequired / 60.0f) * timeperiod * vslinfo.numFrznCrew);
                    if (debug) Debug.Log("FixedBackgroundUpdate timeperiod = " + timeperiod + " frozenkerbals onboard part = " + vslinfo.numFrznCrew + " ECreqd = " + Ecreqd);
                    float Ecrecvd = 0f;
                    Ecrecvd = resourceRequest(v, (float)Ecreqd, MAIN_POWER_NAME);

                    if (debug) Debug.Log("Consumed Freezer EC " + Ecreqd + " units");

                    if (Ecrecvd >= (float)Ecreqd * 0.99)
                    {
                        ScreenMessages.RemoveMessage(OnGoingECMsg);
                        partInfo.timeLastElectricity = (float)currenttime;
                        partInfo.deathCounter = currenttime;
                        partInfo.outofEC = false;
                        ECWarningIssued = false;
                    }
                    else
                    {
                        if (debug) Debug.Log("FixedBackgroundUpdate DeepFreezer Ran out of EC to run the freezer");
                        if (!ECWarningIssued)
                        {
                            ScreenMessages.PostScreenMessage("Insufficient electric charge to monitor frozen kerbals. They are going to die!!", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                            ECWarningIssued = true;
                        }
                        ScreenMessages.RemoveMessage(OnGoingECMsg);
                        OnGoingECMsg = ScreenMessages.PostScreenMessage(" Freezer Out of EC : Systems critical in " + (deathRoll - (currenttime - partInfo.deathCounter)).ToString("######0") + " secs");
                        partInfo.outofEC = true;
                        if (debug) Debug.Log("FixedBackgroundUpdate deathCounter = " + partInfo.deathCounter);
                        if (currenttime - partInfo.deathCounter > deathRoll)
                        {
                            if (debug) Debug.Log("FixedBackgroundUpdate deathRoll reached, Kerbals all die...");
                            partInfo.deathCounter = currenttime;
                            //all kerbals dies
                            var kerbalsToDelete = new List<string>();
                            foreach (KeyValuePair<string, KerbalInfo> kerbal in DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals)
                            {
                                if (kerbal.Value.partID == partFlightID && kerbal.Value.vesselID == v.id)
                                {
                                    kerbalsToDelete.Add(kerbal.Key);
                                }
                            }
                            foreach (string deathKerbal in kerbalsToDelete)
                            {
                                DeepFreeze.Instance.KillFrozenCrew(deathKerbal);
                                ScreenMessages.PostScreenMessage(deathKerbal + " died due to lack of Electrical Charge to run cryogenics", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                                if (debug) Debug.Log("FixedBackgroundUpdate DeepFreezer - kerbal " + deathKerbal + " died due to lack of Electrical charge to run cryogenics");
                            }
                            kerbalsToDelete.ForEach(id => DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Remove(id));
                        }
                    }
                }
            }
            else  //Timewarp is too high
            {
                if (debug) Debug.Log("FixedBackgroundUpdate Timewarp is too high to backgroundprocess");
                partInfo.timeLastElectricity = (float)currenttime;
                partInfo.deathCounter = currenttime;
                partInfo.outofEC = false;
                ECWarningIssued = false;
            }
        }

        #endregion BackgroundProcessing
    }

    #region ExtDoorMgr

    public class DFExtDoorMgr : InternalModule
    {
        private DeepFreezer Freezer;

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && FlightGlobals.ActiveVessel != null)
            {
                if (Freezer == null)
                {
                    Freezer = this.part.FindModuleImplementing<DeepFreezer>();
                    Utilities.Log_Debug("DFExtDoorMgr OnUpdate Set part " + this.part.name);
                }
            }
        }

        public void ButtonExtDoor(bool state)
        {
            if (Freezer == null)
            {
                Freezer = this.part.FindModuleImplementing<DeepFreezer>();
                Utilities.Log_Debug("DFExtDoorMgr buttonExtDoorState set part " + this.part.name);
            }
            if (Freezer == null) return; // If freezer is still null just return
            if (!Freezer.hasExternalDoor) return;  // if freezer doesn't have an external door just return.

            if (Freezer._externaldoorstate == DoorState.OPEN)
            {
                //Door is open so we trigger a closedoor.
                Freezer.eventCloseDoors();
                Utilities.Log_Debug("DFExtDoorMgr ButtonExtDoor fired triggerred eventCloseDoors");
            }
            else
            {
                if (Freezer._externaldoorstate == DoorState.CLOSED)
                {
                    //Door is closed so we trigger a opendoor.
                    Freezer.eventOpenDoors();
                    Utilities.Log_Debug("DFExtDoorMgr ButtonExtDoor fired triggerred eventOpenDoors");
                }
                else
                {
                    // door already opening or closing...
                    Utilities.Log_Debug("DFExtDoorMgr ButtonExtDoor fired but door state is opening, closing or unknown");
                }
            }
        }

        public bool ButtonExtDoorState()
        {
            //this.Log_Debug("DFExtDoorMgr ButtonExtDoorState fired");
            if (Freezer == null)
            {
                Freezer = this.part.FindModuleImplementing<DeepFreezer>();
                Utilities.Log_Debug("DFExtDoorMgr buttonExtDoorState set part " + this.part.name);
            }
            if (Freezer == null) return false; // if freezer still null return false
            if (!Freezer.hasExternalDoor) return false; // if freezer doesn't have an external door just return.
            if (Freezer._externaldoorstate == DoorState.CLOSED || Freezer._externaldoorstate == DoorState.CLOSING || Freezer._externaldoorstate == DoorState.UNKNOWN)
            {
                Utilities.Log_Debug("DFExtDoorMgr Door is closed or closing or unknown return state false");
                return false;
            }
            else
            {
                Utilities.Log_Debug("DFExtDoorMgr Door is open or opening return state true");
                return true;
            }
        }
    }

    #endregion ExtDoorMgr
}