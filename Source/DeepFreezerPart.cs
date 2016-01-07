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
    public class DeepFreezer : PartModule, IDeepFreezer, IResourceConsumer
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

        [KSPField(isPersistant = false, guiActive = false, guiName = "PodExternal")] //Set to true if Cryopod is External part (eg. CRY-0300R), read from part.cfg.
        public bool isPodExternal = false;

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

        [KSPEvent(active = true, guiActive = true, name = "showMenu", guiName = "DeepFreeze Menu")]
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

        internal string _prevRPMTransparentpodSetting = string.Empty;

        [KSPField(isPersistant = true)]
        public bool hasExternalDoor = false;

        [KSPField]
        public string transparentTransforms = string.Empty; //Set by part.cfg. contains list of transforms that should be transparent | separated.

        private bool hasJSITransparentPod = false;

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
        private bool skipThawStep1 = false;
        private bool emergencyThawInProgress = false;
        private bool OpenPodAnimPlaying = false;
        private bool ClosePodAnimPlaying = false;
        private bool ThawWindowAnimPlaying = false;
        private bool FreezeWindowAnimPlaying = false;
        private int ThawStepInProgress = 0;
        private int FreezeStepInProgress = 0;
        private Animation _animation;
        private Animation _windowAnimation;
        private Shader TransparentSpecularShader;
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
        private string Glykerol = "Glykerol";

        private string EC = "ElectricCharge";
        private Guid CrntVslID;
        private uint CrntPartID;
        private string CrntVslName;
        private bool vesselisinIVA;
        private bool vesselisinInternal;

        private bool setGameSettings = false;

        internal bool partHasInternals = false;
        private bool partHasStripLights = false;
        private bool onvslchgInternal = false;  //set to true if a VesselChange game event is triggered by this module
        private bool onvslchgExternal = false;  //set to true if a VesselChange game event is triggered outside of this module
        private bool onvslchgNotActive = false; //sets a timer count started when external VesselChange game event is triggered before resetting cryopod and extdoor animations.
        private float onvslchgNotActiveDelay = 0f; // timer as per previous var
        private double ResAvail = 0f;

        [KSPField(isPersistant = true)]  //we keep the game time the last cryopod reset occured here and only run if the last one was longer than cryopodResetTimeDelay ago.
        private double cryopodResetTime = 0f;

        [KSPField(isPersistant = true)]  //we persist the cryopod animation states in a string because KSP can't handle bool arrays
        public string cryopodstateclosedstring;

        private bool[] cryopodstateclosed;    //This bool array is set to true for each cryopod on the part when the cryopod is in closed state.
        private bool[] seatTakenbyFrznKerbal; //This bool array is set to true for each seat that is currently being taken by a frozen kerbal.

        //Audio Sounds
        private AudioSource mon_beep;
        private AudioSource flatline;
        private AudioSource hatch_lock;
        private AudioSource ice_freeze;
        private AudioSource machine_hum;
        private AudioSource ding_ding;
        private AudioSource ext_door;
        private AudioSource charge_up;

        public List<PartResourceDefinition> GetConsumedResources()
        {
            List<PartResourceDefinition> resources = new List<PartResourceDefinition>();
            PartResourceDefinition glykerol = PartResourceLibrary.Instance.GetDefinition(Glykerol);
            resources.Add(glykerol);
            PartResourceDefinition electricCharge = PartResourceLibrary.Instance.GetDefinition(EC);
            resources.Add(electricCharge);
            return resources;
        }

        public void Update() 
        {
            if (Time.timeSinceLevelLoad < 2.0f) // Check not loading level
                return;

            // This should only happen once in flight only, we need to load the StoredCrewList of frozen kerbals for this part from the DeepFreeze master list
            //This should be done in onload, but it doesn't seem to be working, probably should be checking and doing when vessel loads/unloads/switches/etc.
            if (!setGameSettings && HighLogic.LoadedSceneIsFlight)
            {
                onceoffSetup();
            }

            // If we have an external door (CRY-0300) or external pod (CRY-0300R) check RPM transparency setting and change the door settings as appropriate
            if ((hasExternalDoor || isPodExternal) && (DFInstalledMods.IsRPMInstalled) && !IsFreezeActive && !IsThawActive)
            {
                try
                {
                    checkRPMPodTransparencySetting();
                }
                catch (Exception ex)
                {
                    this.Log("Exception attempting to check RPM transparency settings. Report this error on the Forum Thread.");
                    this.Log("Err: " + ex);
                }
            }

            if (!HighLogic.LoadedSceneIsFlight) // If scene is not flight we are done with onUpdate
                return;

            //This is necessary to override stock crew xfer behaviour. When the user cancels the xfer the Stock highlighting system
            // makes the transparent pod opaque. There is no way (that I can find) to know when this occurs.
            // So we look through the parts pod states for any part that is not animated (IE: CRY-0300R) any that are OPEN we
            // set their window to transparent..... Maybe we should check it first.
            if (FlightGlobals.ready && this.vessel.loaded && isPodExternal && !IsFreezeActive && !IsThawActive && DFInstalledMods.IsRPMInstalled)
            {
                if (_prevRPMTransparentpodSetting == "ON")
                {
                    for (int i = 0; i < cryopodstateclosed.Length; i++)
                    {
                        if (!cryopodstateclosed[i])
                        {
                            string windowname = "Cryopod-" + (i + 1).ToString() + "-Window";
                            Renderer extwindowrenderer = this.part.FindModelComponent<Renderer>(windowname);
                            if (extwindowrenderer != null)
                            {
                                if (extwindowrenderer.material.shader != TransparentSpecularShader)
                                    setCryopodWindowTransparent(i);
                            }
                        }
                    }
                }
            } 

            //For some reason when we go on EVA or switch vessels the InternalModel is destroyed.
            //Which causes a problem when we re-board the part as the re-boarding kerbal ends up in a frozen kerbals seat.
            //So we check for the internmodel existing while the vessel this part is attached to is loaded and if it isn't we re-instansiate it.
            if (FlightGlobals.ready && this.vessel.loaded && partHasInternals && this.part.internalModel == null)
            {
                this.Log("Part " + this.part.name + "(" + this.part.flightID + ") is loaded and internalModel has disappeared, so re-instansiate it");
                this.part.SpawnCrew();
                resetFrozenKerbals();
                resetCryopods(true);
                // If part does not have JSITransparentPod module we check the portrait cams. Otherwise JSITransparenPod will do it for us.
                if (!hasJSITransparentPod)
                {
                    Utilities.CheckPortraitCams(vessel);
                }
            }

            if ((Time.time - lastUpdate) > updatetnterval && (Time.time - lastRemove) > updatetnterval) // We only update every updattnterval time interval.
            {
                lastUpdate = Time.time;
                if (FlightGlobals.ready && FlightGlobals.ActiveVessel != null)
                {
                    CrntVslID = this.vessel.id;
                    CrntVslName = this.vessel.vesselName;

                    //Set the Part temperature in the partmenu
                    if (DeepFreeze.Instance.DFsettings.TempinKelvin)
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
                        try
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
                        catch (Exception ex)
                        {
                            this.Log("Exception attempting to check RemoteTech vessel connections. Report this error on the Forum Thread.");
                            this.Log("Err: " + ex);
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
                            if (_prevexterndoorstate == DoorState.CLOSING || _externaldoorstate == DoorState.CLOSED)
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
                        //this.Log_Debug("Vessel is in IVA mode");
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
                            //this.Log_Debug("ActiveKerbalFound, seatidx=" + SeatIndx);
                            if (SeatIndx != -1)
                            {
                                SeatIndx++;
                                IVAkerbalPod = ScreenMessages.PostScreenMessage("Pod:" + SeatIndx);
                            }
                            IVAkerbalPart = ScreenMessages.PostScreenMessage(this.part.name.Substring(0, 8));
                            IVAKerbalName = ScreenMessages.PostScreenMessage(actkerbal.name);
                            //monitoring beep
                            if (TotalFrozen > 0 && !mon_beep.isPlaying)
                            {
                                mon_beep.Play();
                            }
                        }
                    }
                    else
                    {
                        ScreenMessages.RemoveMessage(IVAKerbalName);
                        ScreenMessages.RemoveMessage(IVAkerbalPart);
                        ScreenMessages.RemoveMessage(IVAkerbalPod);
                        vesselisinIVA = false;
                        //this.Log_Debug("Vessel is NOT in IVA mode");
                        if (Utilities.IsInInternal())
                        {
                            vesselisinInternal = true;
                            if (TotalFrozen > 0 && !mon_beep.isPlaying)
                            {
                                mon_beep.Play();
                            }
                            //this.Log_Debug("Vessel is in Internal mode");
                        }
                        else
                        {
                            vesselisinInternal = false;
                            if (mon_beep.isPlaying)
                            {
                                mon_beep.Stop();
                            }
                            //this.Log_Debug("Vessel is NOT in Internal mode");
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
            //UpdateCounts(); // Update the Kerbal counters and stored crew lists for the part - MOVED to FixedUpdate
        }

        private void checkRPMPodTransparencySetting()
        {
            try
            {
                string transparentPodSetting = string.Empty;
                object JSITransparentPodModule = this.part.Modules["JSITransparentPod"];
                if (JSITransparentPodModule != null)
                {
                    object outputField = Utilities.GetObjectField(JSITransparentPodModule, "transparentPodSetting");
                    if (outputField != null)
                    {
                        transparentPodSetting = outputField.ToString();
                        if (transparentPodSetting != _prevRPMTransparentpodSetting)
                        {
                            switch (transparentPodSetting)
                            {
                                case "ON":
                                    if (hasExternalDoor)  //CRY-0300
                                    {
                                        // If the doors are closed or closing set open doors event active
                                        if (_externaldoorstate != DoorState.CLOSED && _externaldoorstate != DoorState.CLOSING)
                                        {
                                            Events["eventOpenDoors"].active = false;
                                            Events["eventCloseDoors"].active = true;
                                        }
                                        else
                                        {
                                            //If the doors are open or opening set close doors event active
                                            if (_externaldoorstate != DoorState.OPEN && _externaldoorstate != DoorState.OPENING)
                                            {
                                                Events["eventOpenDoors"].active = true;
                                                Events["eventCloseDoors"].active = false;
                                            }
                                        }
                                    }
                                    else //CRY-0300R
                                    {
                                        for (int i = 0; i < FreezerSize; i++)
                                        {
                                            string windowname = "Cryopod-" + (i + 1).ToString() + "-Window";
                                            Renderer extwindowrenderer = this.part.FindModelComponent<Renderer>(windowname);
                                            if (extwindowrenderer != null)
                                            {
                                                if (HighLogic.LoadedSceneIsFlight)  //If in flight, we check the pod state
                                                {
                                                    if (!cryopodstateclosed[i])  //Pod is open
                                                    {
                                                        if (extwindowrenderer.material.shader != TransparentSpecularShader)
                                                            setCryopodWindowTransparent(i);
                                                    }
                                                    else  //Pod is closed
                                                    {
                                                        if (extwindowrenderer.material.shader != KSPSpecularShader)
                                                            setCryopodWindowSpecular(i);
                                                    }
                                                }
                                                else  //If in editor, always transparent
                                                {
                                                    if (extwindowrenderer.material.shader != TransparentSpecularShader)
                                                        setCryopodWindowTransparent(i);
                                                }
                                            }
                                        }
                                    }

                                    break;

                                default:
                                    this.Log_Debug("RPM set to OFF or AUTO for transparent pod");
                                    if (hasExternalDoor)  //CRY-0300
                                    {
                                        //hasExternalDoor = false;
                                        // We must close the doors if they are not or we see an empty internal.

                                        DoorState actualDoorState = getdoorState();
                                        if (actualDoorState != DoorState.CLOSED)
                                        {
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
                                            if (animationName != null)
                                            {
                                                externalDoorAnim[animationName].normalizedTime = 1;
                                                externalDoorAnim[animationName].speed = float.MinValue;
                                                externalDoorAnim.Play("Open");
                                            }
                                            _prevexterndoorstate = _externaldoorstate;
                                            _externaldoorstate = DoorState.CLOSED;
                                        }
                                        Events["eventOpenDoors"].active = false;
                                        Events["eventCloseDoors"].active = false;
                                    }
                                    else  //CRY-0300R
                                    {
                                        for (int i = 0; i < FreezerSize; i++)
                                        {
                                            string windowname = "Cryopod-" + (i + 1).ToString() + "-Window";
                                            Renderer extwindowrenderer = this.part.FindModelComponent<Renderer>(windowname);

                                            if (extwindowrenderer != null)
                                            {
                                                if (extwindowrenderer.material.shader != KSPSpecularShader)
                                                    setCryopodWindowSpecular(i);
                                            }
                                        }
                                    }
                                    break;
                            }
                            _prevRPMTransparentpodSetting = transparentPodSetting;
                        }
                    }
                }
            }
            catch (Exception)
            {
                Utilities.Log("DeepFreezer", " Error checking RPM TransparentPod Setting");
                //Utilities.Log("DeepFreezer ", ex.Message);
            }
        }

        private void onceoffSetup()
        {
            Utilities.Log_Debug("DeepFreezer", "OnUpdate SetGameSettings");
            _StoredCrewList.Clear();
            CrntVslID = this.vessel.id;
            CrntVslName = this.vessel.vesselName;
            CrntPartID = this.part.flightID;
            lastUpdate = Time.time;
            lastRemove = Time.time;
            // Master settings values override the part values for EC required and Glykerol required
            if (DeepFreeze.Instance.DFsettings.ECReqdToFreezeThaw != ChargeRequired)
            {
                ChargeRequired = DeepFreeze.Instance.DFsettings.ECReqdToFreezeThaw;
            }
            if (DeepFreeze.Instance.DFsettings.GlykerolReqdToFreeze != GlykerolRequired)
            {
                GlykerolRequired = DeepFreeze.Instance.DFsettings.GlykerolReqdToFreeze;
            }
            if (DeepFreeze.Instance.DFsettings.RegTempReqd)
            {
                heatamtMonitoringFrznKerbals = DeepFreeze.Instance.DFsettings.heatamtMonitoringFrznKerbals;
                heatamtThawFreezeKerbal = DeepFreeze.Instance.DFsettings.heatamtThawFreezeKerbal;
            }
            PartInfo partInfo;
            if (DeepFreeze.Instance.DFgameSettings.knownFreezerParts.TryGetValue(this.part.flightID, out partInfo))
            {
                timeSinceLastECtaken = (float)partInfo.timeLastElectricity;
                timeSinceLastTmpChk = (float)partInfo.timeLastTempCheck;
            }
            Utilities.Log_Debug("DeepFreezer", "This CrntVslID = " + CrntVslID + " This CrntPartID = " + CrntPartID + " This CrntVslName = " + CrntVslName);

            // Set a flag if this part has internals or not. If it doesn't we don't try to save/restore specific seats for the frozen kerbals
            if (this.part.partInfo.internalConfig.HasData)
            {
                partHasInternals = true;
            }
            else
            {
                partHasInternals = false;
            }

            resetFrozenKerbals();
                        
            this.Log_Debug("Onceoffsetup resetcryopod doors");
            if (partHasInternals)
            {
                resetCryopods(true);
            }

            if (this.part.Modules.Contains("JSITransparentPod"))
            {
                hasJSITransparentPod = true;
            }

            //For all thawed crew in part, change their IVA animations to be less well.. animated?
            foreach (ProtoCrewMember crew in this.part.protoModuleCrew)
            {
                if (crew.KerbalRef != null)
                {
                    Utilities.subdueIVAKerbalAnimations(crew.KerbalRef);
                }
            }

            //If we have an external door (CRY-0300) enabled set the current door state and the helmet states
            if (hasExternalDoor)
            {
                setHelmetstoDoorState();
            }
            //If we have lightstrips (CRY-5000) set them up
            if (partHasInternals)
            {
                try
                {
                    Animation[] animators = this.part.internalModel.FindModelAnimators("LightStrip");
                    if (animators.Length > 0)
                    {
                        this.Log_Debug("Found " + animators.Length + " LightStrip animations starting");
                        partHasStripLights = true;
                        if (DeepFreeze.Instance.DFsettings.StripLightsActive)
                        {
                            foreach (Animation anim in animators)
                            {
                                anim["LightStrip"].speed = 1;
                                anim["LightStrip"].normalizedTime = 0;
                                anim.wrapMode = WrapMode.Loop;
                                anim.Play("LightStrip");
                            }
                        }
                        else
                        {
                            foreach (Animation anim in animators)
                            {
                                anim.Stop();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utilities.Log("DeepFreezer", " Error finding Internal LightStrip Animators");
                    Utilities.Log("DeepFreezer ", ex.Message);
                }
            }
            setGameSettings = true; //set the flag so this method doesn't execute a second time
        }

        private void FixedUpdate()
        {
            if (Time.timeSinceLevelLoad < 2.0f) // Check not loading level
            {
                return;
            }

            if (IsFreezeActive) // Process active freezing process
            {
                ProcessFreezeKerbal();
            }

            if (IsThawActive) // Process active thawing process
            {
                ProcessThawKerbal();
            }

            //If an emergency thaw has been called, thaw the first kerbal in the stored frozen kerbal list until none remain.
            if (!IsFreezeActive && !IsThawActive && emergencyThawInProgress && vessel.isActiveVessel)
            {
                if (_StoredCrewList.Count > 0)
                {
                    skipThawStep1 = true;  // we don't use any EC for emergency thaw
                    beginThawKerbal(_StoredCrewList[0].CrewName);
                }
                else
                {
                    this.Log_Debug("Emergency Thaw completed");
                }
            }

            // The following section is the on-going EC check and temperature checks and update the seat counts for the freezer, only in flight and activevessel
            if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && vessel.isActiveVessel && setGameSettings)
            {
                if (DeepFreeze.Instance.DFsettings.ECreqdForFreezer)
                {
                    this.Fields["FrznChargeRequired"].guiActive = true;
                    this.Fields["FrznChargeUsage"].guiActive = true;
                    this.Fields["_FreezerOutofEC"].guiActive = true;
                    if (Utilities.timewarpIsValid(5))  // EC usage and generation still seems to work up to warpfactor of 4.
                    {
                        PartInfo partInfo;
                        if (!DeepFreeze.Instance.DFgameSettings.knownFreezerParts.TryGetValue(this.part.flightID, out partInfo))
                        {
                            this.Log("Freezer Part NOT found: " + this.part.name + "(" + this.part.flightID + ")" + " (" + vessel.id + ")");
                            partInfo = new PartInfo(vessel.id, this.part.name, Planetarium.GetUniversalTime());
                            partInfo.ECWarning = false;
                        }
                        ChkOngoingEC(partInfo); // Check the on-going EC usage
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
                    timeSinceLastECtaken = (float)Planetarium.GetUniversalTime();
                }

                if (DeepFreeze.Instance.DFsettings.RegTempReqd)
                {
                    this.Fields["_FrzrTmp"].guiActive = true;
                    if (Utilities.timewarpIsValid(2)) // Temperature is buggy in timewarp so it is disabled whenever timewarp is on.
                    {
                        PartInfo partInfo;
                        if (!DeepFreeze.Instance.DFgameSettings.knownFreezerParts.TryGetValue(this.part.flightID, out partInfo))
                        {
                            this.Log("Freezer Part NOT found: " + this.part.name + "(" + this.part.flightID + ")" + " (" + vessel.id + ")");
                            partInfo = new PartInfo(vessel.id, this.part.name, Planetarium.GetUniversalTime());
                            partInfo.TempWarning = false;
                        }
                        ChkOngoingTemp(partInfo); // Check the on-going Temperature
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
                        if (partHasInternals)
                        {
                            resetCryopods(true);
                        }
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

        private void ChkOngoingEC(PartInfo partInfo)
        {
            // The following section of code consumes EC when we have ECreqdForFreezer set to true in the part config.
            // This consumes electric charge when we have frozen kerbals on board.
            // But due to bugs in KSP ith EC and SolarPanels at high timewarp if timewarp is > 4x we turn it off.
            // If we run out of EC and Lethal setting is on, we roll the dice. There is a 1 in 3 chance a Kerbal will DIE!!!!
            // If lethal setting is off an emergency thaw of all frozen crew occurs.

            //Utilities.Log_Debug("ChkOngoingEC start");
            double currenttime = Planetarium.GetUniversalTime();
            double timeperiod = currenttime - (double)timeSinceLastECtaken;
            //this.Log_Debug("currenttime = " + currenttime + " timeperiod = " + timeperiod + " updateECTempInterval= " + updateECTempInterval);
            if (timeperiod > updateECTempInterval) //only update every udpateECTempInterval to avoid request resource bug when amounts are too small
            {
                if (TotalFrozen > 0) //We have frozen Kerbals, consume EC
                {
                    double ECreqd = (((FrznChargeRequired / 60.0f) * timeperiod) * TotalFrozen);
                    Utilities.Log_Debug("DeepFreezer", "Running the freezer parms currenttime =" + currenttime + " timeperiod =" + timeperiod + " ecreqd =" + ECreqd);
                    if (requireResource(vessel, EC, ECreqd, false, out ResAvail))
                    {
                        ScreenMessages.RemoveMessage(OnGoingECMsg);
                        //Have resource
                        requireResource(vessel, EC, ECreqd, true, out ResAvail);
                        FrznChargeUsage = (float)ResAvail;
                        Utilities.Log_Debug("DeepFreezer", "Consumed Freezer EC " + ECreqd + " units");
                        timeSinceLastECtaken = (float)currenttime;
                        deathCounter = currenttime;
                        _FreezerOutofEC = false;
                        partInfo.ECWarning = false;
                    }
                    else
                    {
                        if (onvslchgExternal) // this is true if vessel just loaded or we just switched to this vessel
                                              // we need to check if we aren't going to exhaust all EC in one call.. and???
                        {
                            ECreqd = ResAvail * 95 / 100;
                            if (requireResource(vessel, EC, ECreqd, false, out ResAvail))
                            {
                                requireResource(vessel, EC, ECreqd, true, out ResAvail);
                                FrznChargeUsage = (float)ResAvail;
                            }
                        }
                        //Debug.Log("DeepFreezer Ran out of EC to run the freezer");
                        if (!partInfo.ECWarning)
                        {
                            if (TimeWarp.CurrentRateIndex > 1) Utilities.stopWarp();
                            ScreenMessages.PostScreenMessage("Insufficient electric charge to monitor frozen kerbals.", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                            partInfo.ECWarning = true;
                            deathCounter = currenttime;
                        }
                        ScreenMessages.RemoveMessage(OnGoingECMsg);
                        OnGoingECMsg = ScreenMessages.PostScreenMessage(" Freezer Out of EC : Systems critical in " + (deathRoll - (currenttime - deathCounter)).ToString("######0") + " secs");
                        _FreezerOutofEC = true;
                        FrznChargeUsage = 0f;
                        Utilities.Log_Debug("DeepFreezer", "deathCounter = " + deathCounter);
                        if (currenttime - deathCounter > deathRoll)
                        {
                            if (DeepFreeze.Instance.DFsettings.fatalOption)
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
                                    if (!flatline.isPlaying)
                                    {
                                        flatline.Play();
                                    }
                                }
                                kerbalsToDelete.ForEach(id => _StoredCrewList.Remove(id));
                            }
                            else  //NON-Fatal option set. Thaw them all.
                            {
                                Utilities.Log_Debug("DeepFreezer", "deathRoll reached, Kerbals all don't die... They just Thaw out...");
                                deathCounter = currenttime;
                                //all kerbals thaw out
                                emergencyThawInProgress = true;  //This will trigger FixedUpdate to thaw all frozen kerbals in the part, one by one.
                            }
                        }
                    }
                }
                else  // no frozen kerbals, so just update last time EC checked
                {
                    this.Log_Debug("No frozen kerbals for EC consumption in part " + this.part.name);
                    timeSinceLastECtaken = (float)currenttime;
                    deathCounter = currenttime;
                    FrznChargeUsage = 0f;
                }
            }
            //Utilities.Log_Debug("ChkOngoingEC end");
        }

        private void ChkOngoingTemp(PartInfo partInfo)
        {
            // The follow section of code checks Temperatures when we have RegTempReqd set to true in the master config file.
            // This check is done when we have frozen kerbals on board.
            // But due to bugs in KSP with EC and SolarPanels at high timewarp if timewarp is > 4x we turn it off.
            // If the temperature is too high and fatal option is on, we roll the dice. There is a 1 in 3 chance a Kerbal will DIE!!!!
            // If fatal option is off all frozen kerbals are thawed
            double currenttime = Planetarium.GetUniversalTime();
            double timeperiod = currenttime - (double)timeSinceLastTmpChk;
            //Utilities.Log_Debug("ChkOngoingTemp start time=" + Time.time.ToString() + ",timeSinceLastTmpChk=" + timeSinceLastTmpChk.ToString() + ",Planetarium.UniversalTime=" + Planetarium.GetUniversalTime().ToString() + " timeperiod=" + timeperiod.ToString());
            if (timeperiod > updateECTempInterval) //only update every udpateECTempInterval to avoid request resource bug when amounts are too small
            {
                if (TotalFrozen > 0) //We have frozen Kerbals, generate and check heat
                {
                    //Add Heat for equipment monitoring frozen kerbals
                    double heatamt = (((heatamtMonitoringFrznKerbals / 60.0f) * timeperiod) * TotalFrozen);
                    if (heatamt > 0) this.part.AddThermalFlux(heatamt);
                    Utilities.Log_Debug("Added " + heatamt + " kW of heat for monitoring " + TotalFrozen + " frozen kerbals");
                    if (this.part.temperature < DeepFreeze.Instance.DFsettings.RegTempMonitor)
                    {
                        Utilities.Log_Debug("DeepFreezer", "Temperature check is good parttemp=" + this.part.temperature + ",MaxTemp=" + DeepFreeze.Instance.DFsettings.RegTempMonitor);
                        ScreenMessages.RemoveMessage(TempChkMsg);
                        _FrzrTmp = FrzrTmpStatus.OK;
                        tmpdeathCounter = currenttime;
                        // do warning if within 40 and 20 kelvin
                        double tempdiff = DeepFreeze.Instance.DFsettings.RegTempMonitor - this.part.temperature;
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
                        partInfo.TempWarning = false;
                    }
                    else
                    {
                        // OVER TEMP I'm Melting!!!!
                        Debug.Log("DeepFreezer Part Temp TOO HOT, Kerbals are going to melt parttemp=" + this.part.temperature);
                        if (!partInfo.TempWarning)
                        {
                            if (TimeWarp.CurrentRateIndex > 1) Utilities.stopWarp();
                            ScreenMessages.PostScreenMessage("Temperature getting too hot for kerbals to remain frozen.", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                            partInfo.TempWarning = true;
                        }
                        _FrzrTmp = FrzrTmpStatus.RED;
                        Utilities.Log_Debug("DeepFreezer", "tmpdeathCounter = " + tmpdeathCounter);
                        ScreenMessages.RemoveMessage(TempChkMsg);
                        TempChkMsg = ScreenMessages.PostScreenMessage(" Freezer Over Temp : Systems critical in " + (tmpdeathRoll - (currenttime - tmpdeathCounter)).ToString("######0") + " secs");
                        if (currenttime - tmpdeathCounter > tmpdeathRoll)
                        {
                            Utilities.Log_Debug("DeepFreezer", "tmpdeathRoll reached, roll the dice...");
                            tmpdeathCounter = currenttime;
                            partInfo.TempWarning = false;
                            //a kerbal dies
                            if (DeepFreeze.Instance.DFsettings.fatalOption)
                            {
                                int dice = rnd.Next(1, _StoredCrewList.Count); // Randomly select a Kerbal to kill.
                                Utilities.Log_Debug("DeepFreezer", "A Kerbal dies dice=" + dice);
                                FrznCrewMbr deathKerbal = _StoredCrewList[dice - 1];
                                DeepFreeze.Instance.KillFrozenCrew(deathKerbal.CrewName);
                                ScreenMessages.PostScreenMessage(deathKerbal.CrewName + " died due to overheating, cannot keep frozen", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                                Debug.Log("DeepFreezer - kerbal " + deathKerbal.CrewName + " died due to overheating, cannot keep frozen");
                                _StoredCrewList.Remove(deathKerbal);

                                if (!flatline.isPlaying)
                                {
                                    flatline.Play();
                                }
                            }
                            else  //NON-fatal option set. Thaw them all.
                            {
                                ScreenMessages.PostScreenMessage("Over Temperature - Emergency Thaw in Progress.", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                                Utilities.Log_Debug("DeepFreezer", "deathRoll reached, Kerbals all don't die... They just Thaw out...");
                                //all kerbals thaw out
                                emergencyThawInProgress = true;  //This will trigger FixedUpdate to thaw all frozen kerbals in the part, one by one.
                            }
                        }
                    }
                }
                else  // no frozen kerbals, so just update last time tmp checked
                {
                    timeSinceLastTmpChk = (float)currenttime;
                }
            }
            //Utilities.Log_Debug("ChkOngoingTemp end");
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
            if ((state != StartState.None && state != StartState.Editor))
            {
                GameEvents.onCrewTransferred.Add(this.OnCrewTransferred);
                GameEvents.onVesselChange.Add(this.OnVesselChange);
                GameEvents.onCrewBoardVessel.Add(this.OnCrewBoardVessel);
                GameEvents.onCrewOnEva.Add(this.onCrewOnEva);
                GameEvents.onVesselDestroy.Add(this.onVesselDestroy);
            }

            //Set Shaders for changing the Crypod Windows
            HashSet<Shader> shaders = new HashSet<Shader>();
            Resources.FindObjectsOfTypeAll<Shader>().ToList().ForEach(sh => shaders.Add(sh));
            List<Shader> listshaders = new List<Shader>(shaders);
            TransparentSpecularShader = listshaders.Find(a => a.name == "Transparent/Specular");
            KSPSpecularShader = listshaders.Find(b => b.name == "KSP/Specular");

            // Setup the sounds
            if ((state != StartState.None && state != StartState.Editor))
            {
                mon_beep = gameObject.AddComponent<AudioSource>();
                mon_beep.clip = GameDatabase.Instance.GetAudioClip("REPOSoftTech/DeepFreeze/Sounds/mon_beep");
                mon_beep.volume = .2F;
                mon_beep.panLevel = 0;
                mon_beep.rolloffMode = AudioRolloffMode.Logarithmic;
                mon_beep.audio.maxDistance = 10f;
                mon_beep.audio.minDistance = 8f;
                mon_beep.audio.dopplerLevel = 0f;
                mon_beep.audio.panLevel = 0f;
                mon_beep.audio.playOnAwake = false;
                mon_beep.audio.priority = 255;
                mon_beep.Stop();
                flatline = gameObject.AddComponent<AudioSource>();
                flatline.clip = GameDatabase.Instance.GetAudioClip("REPOSoftTech/DeepFreeze/Sounds/flatline");
                flatline.volume = 1;
                flatline.panLevel = 0;
                flatline.rolloffMode = AudioRolloffMode.Linear;
                flatline.Stop();
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
                ding_ding.volume = .4F;
                ding_ding.panLevel = 0;
                ding_ding.rolloffMode = AudioRolloffMode.Linear;
                ding_ding.Stop();
                List<UrlDir.UrlFile> databaseAudioFiles = new List<UrlDir.UrlFile>();
                databaseAudioFiles = GameDatabase.Instance.databaseAudioFiles;
                ext_door = gameObject.AddComponent<AudioSource>();
                ext_door.clip = GameDatabase.Instance.GetAudioClip("REPOSoftTech/DeepFreeze/Sounds/externaldoorswitch");
                ext_door.volume = .7F;
                ext_door.panLevel = 0;
                ext_door.rolloffMode = AudioRolloffMode.Linear;
                ext_door.Stop();
                charge_up = gameObject.AddComponent<AudioSource>();
                charge_up.clip = GameDatabase.Instance.GetAudioClip("REPOSoftTech/DeepFreeze/Sounds/charge_up");
                charge_up.volume = 1;
                charge_up.panLevel = 0;
                charge_up.rolloffMode = AudioRolloffMode.Linear;
                charge_up.Stop();
            }
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
                        if (_externaldoorstate == DoorState.OPEN)
                        {
                            StartCoroutine(openDoors(float.MaxValue));
                        }
                        else
                        {
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
            GameEvents.onVesselDestroy.Remove(this.onVesselDestroy);
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
                if (!DFInstalledMods.IsRTInstalled || (DFInstalledMods.IsRTInstalled && isRTConnected))
                {
                    if (_StoredCrewList.Count < FreezerSize) // If the Freezer isn't full
                    {
                        foreach (var CrewMember in part.protoModuleCrew) // We Add Freeze Events for all active crew in the part
                        {
                            if (CrewMember.type != ProtoCrewMember.KerbalType.Tourist)
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

        private void ProcessFreezeKerbal()
        {
            Utilities.Log_Debug("DeepFreezer", "FreezeActive ToFrzeKerbal = " + ToFrzeKerbal + " Seat =" + ToFrzeKerbalSeat);
            switch (FreezeStepInProgress)
            {
                case 0:
                    //Begin
                    this.Log_Debug("Freeze Step 0");
                    charge_up.Play();  // Play the sound effects.
                    charge_up.loop = true;
                    // If we are in IVA mode we switch to the internal camera in front of their cryopod.
                    if (vesselisinIVA || vesselisinInternal)
                    {
                        setIVAFrzrCam(ToFrzeKerbalSeat);
                    }

                    if (partHasStripLights && DeepFreeze.Instance.DFsettings.StripLightsActive)
                    {
                        startStripLightFlash(ToFrzeKerbalSeat);
                    }
                    FreezeStepInProgress = 1;
                    break;

                case 1:
                    //get Electric Charge and Glykerol
                    this.Log_Debug("Freeze Step 1");
                    if (!requireResource(vessel, EC, ChargeRate, false, out ResAvail) == true)
                    {
                        ScreenMessages.PostScreenMessage("Insufficient electric charge to freeze kerbal", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        FreezeKerbalAbort(ActiveFrzKerbal);
                    }
                    else
                    {
                        requireResource(vessel, EC, ChargeRate, true, out ResAvail);
                        StoredCharge = StoredCharge + ChargeRate;
                        ScreenMessages.RemoveMessage(FreezeMsg);
                        FreezeMsg = ScreenMessages.PostScreenMessage(" Cryopod - Charging: " + StoredCharge.ToString("######0"));
                        if (DeepFreeze.Instance.DFsettings.RegTempReqd)
                        {
                            this.part.AddThermalFlux(heatamtThawFreezeKerbal);
                        }
                        Utilities.Log_Debug("DeepFreezer", "Drawing Charge StoredCharge =" + StoredCharge.ToString("0000.00") + " ChargeRequired =" + ChargeRequired);
                        if (StoredCharge >= ChargeRequired)
                        {
                            ScreenMessages.RemoveMessage(FreezeMsg);
                            if (requireResource(vessel, Glykerol, GlykerolRequired, true, out ResAvail))
                            {
                                charge_up.Stop(); // stop the sound effects
                                FreezeStepInProgress = 2;
                            }
                            else  //Not enough Glykerol - abort
                            {
                                this.Log_Debug("Not enough Glykerol - Aborting");
                                FreezeKerbalAbort(ActiveFrzKerbal);
                            }
                        }
                    }
                    break;

                case 2:
                    //close the Pod door Hal
                    this.Log_Debug("Freeze Step 2");
                    if (partHasInternals && isPodExternal)
                    // Part has no animated cryopods but has internals. skip to step 3.
                    {                        
                        cryopodstateclosed[ToFrzeKerbalSeat] = true;
                        savecryopodstatepersistent();
                        FreezeStepInProgress = 3;
                    }
                    else
                    {
                        if (partHasInternals && isPartAnimated)
                        {
                            if (!ClosePodAnimPlaying)  // If animation not already playing start it playing.
                            {
                                this.Log_Debug("Closing the cryopod");
                                hatch_lock.Play();  // Play the sound effects.
                                machine_hum.Play();
                                machine_hum.loop = true;
                                ClosePodAnimPlaying = true;
                                closeCryopod(ToFrzeKerbalSeat, 1f);
                                //cryopodstateclosed[ToFrzeKerbalSeat] = true;
                                //savecryopodstatepersistent();
                            }
                            else  // Animation is already playing, check if it has finished.
                            {
                                if (_animation != null)
                                {
                                    if (_animation.IsPlaying("Close"))
                                    {
                                        this.Log_Debug("waiting for the pod animation to complete the freeze");
                                        ClosePodAnimPlaying = true;
                                    }
                                    else
                                    {
                                        this.Log_Debug("Animation has completed. go to step 3.");
                                        ClosePodAnimPlaying = false;
                                        FreezeStepInProgress = 3;
                                    }
                                }
                                else
                                {
                                    //There is no animation found? Skip to step 3.
                                    this.Log_Debug("Animation disappeared. go to step 3.");
                                    ClosePodAnimPlaying = false;
                                    FreezeStepInProgress = 3;
                                }
                            }
                        }
                        else
                        {
                            //Part is not animated, skip to step 4.
                            ClosePodAnimPlaying = false;
                            cryopodstateclosed[ToFrzeKerbalSeat] = true;
                            savecryopodstatepersistent();
                            FreezeStepInProgress = 4;
                        }
                    }
                    break;

                case 3:
                    //Freeze the window
                    this.Log_Debug("Freeze Step 3");
                    if (partHasInternals)
                    {
                        if (!FreezeWindowAnimPlaying)  // If animation not already playing start it playing.
                        {
                            this.Log_Debug("freezing the cryopod window");
                            machine_hum.Stop(); // stop the sound effects
                            ice_freeze.Play();
                            FreezeWindowAnimPlaying = true;
                            freezeCryopodWindow(ToFrzeKerbalSeat, 1f);
                        }
                        else  // Animation is already playing, check if it has finished.
                        {
                            if (_windowAnimation != null)
                            {
                                if (_windowAnimation.IsPlaying("CryopodWindowClose"))
                                {
                                    this.Log_Debug("waiting for the window animation to complete the freeze");
                                    FreezeWindowAnimPlaying = true;
                                }
                                else
                                {
                                    this.Log_Debug("Animation has completed. go to step 4.");
                                    FreezeWindowAnimPlaying = false;
                                    FreezeStepInProgress = 4;
                                }
                            }
                            else
                            {
                                this.Log_Debug("Animation disappeared. go to step 4.");
                                //There is no animation found? Skip to step 4.
                                FreezeWindowAnimPlaying = false;
                                FreezeStepInProgress = 4;
                            }
                        }
                    }
                    else
                    {
                        //Part is not animated, skip to step 4.
                        FreezeWindowAnimPlaying = false;
                        FreezeStepInProgress = 4;
                    }
                    break;

                case 4:
                    //Finalise
                    this.Log_Debug("Freeze Step 4");
                    if (partHasInternals)
                    {
                        setCryopodWindowSpecular(ToFrzeKerbalSeat);
                    }
                    FreezeKerbalConfirm(ActiveFrzKerbal);
                    break;
            }
        }

        public void beginFreezeKerbal(ProtoCrewMember CrewMember)
        {
            //This method is the first called to Freeze a Kerbal it will check all the pre-conditions are right for freezing and then call FreezeKerbal if they are
            try
            {
                if (this.FreezerSpace > 0 && this.part.protoModuleCrew.Contains(CrewMember)) // Freezer has space? and Part contains the CrewMember?
                {
                    if (!requireResource(vessel, Glykerol, GlykerolRequired, false, out ResAvail)) // check we have Glykerol on board. 5 units per freeze event. This should be a part config item not hard coded.
                    {
                        ScreenMessages.PostScreenMessage("Insufficient Glykerol to freeze kerbal", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        return;
                    }
                    else // We have enough Glykerol
                    {
                        if (DeepFreeze.Instance.DFsettings.RegTempReqd) // Temperature check is required
                        {
                            if ((float)this.part.temperature > DeepFreeze.Instance.DFsettings.RegTempFreeze)
                            {
                                ScreenMessages.PostScreenMessage("Cannot Freeze while Temperature > " + DeepFreeze.Instance.DFsettings.RegTempFreeze.ToString("######0") + this.Fields["CabinTemp"].guiUnits, 5.0f, ScreenMessageStyle.UPPER_CENTER);
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
            this.Log_Debug("FreezeKerbal " + CrewMember.name);
            Utilities.dmpKerbalRefs(null, partHasInternals ? this.part.internalModel.seats[CrewMember.seatIdx].kerbalRef : null, CrewMember.KerbalRef);
            if (partHasInternals)
                this.Log_Debug("Seatindx=" + CrewMember.seatIdx + ",Seatname=" + CrewMember.seat.seatTransformName);
            try
            {
                ToFrzeKerbalSeat = CrewMember.seatIdx;
            }
            catch (Exception)
            {
                Debug.Log("Unable to find internal seat index for " + CrewMember.name);
                //Debug.Log("Err: " + ex);
                ToFrzeKerbalSeat = -1; // Set their seat
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
            FreezeStepInProgress = 0;
            IsFreezeActive = true; // Set the Freezer actively freezing mode on
            ScreenMessages.PostScreenMessage("Starting Freeze process", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            this.Log_Debug("ActiveFrzKerbal=" + ActiveFrzKerbal.name + ",ToFrzeKerbal=" + ToFrzeKerbal + ",SeatIdx=" + ToFrzeKerbalSeat + ",seat transform name=" + ToFrzeKerbalXformNme);
            this.Log_Debug("FreezeKerbal ended");
        }

        private void FreezeKerbalAbort(ProtoCrewMember CrewMember)
        {
            try
            {
                this.Log_Debug("FreezeKerbalAbort " + CrewMember.name + " seat " + ToFrzeKerbalSeat);
                ScreenMessages.PostScreenMessage("Freezing Aborted", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                Utilities.setFrznKerbalLayer(CrewMember, true, false);
                if (partHasInternals)
                {
                    if (vesselisinIVA || vesselisinInternal)
                    {
                        setIVAFrzrCam(ToFrzeKerbalSeat);
                    }
                    if (isPartAnimated)
                        openCryopod(ToFrzeKerbalSeat, float.MaxValue);
                    if (isPartAnimated || (isPodExternal && DFInstalledMods.IsRPMInstalled && _prevRPMTransparentpodSetting == "ON"))
                        thawCryopodWindow(ToFrzeKerbalSeat, float.MaxValue);
                    cryopodstateclosed[ToFrzeKerbalSeat] = false;
                    savecryopodstatepersistent();
                    if (partHasStripLights && DeepFreeze.Instance.DFsettings.StripLightsActive)
                    {
                        stopStripLightFlash(ToFrzeKerbalSeat);
                    }
                }

                if (!AddKerbal(CrewMember, ToFrzeKerbalSeat))
                {
                    Debug.Log("FreezeKerbalAbort Procedure FAILED! Critical error");
                }

                IsFreezeActive = false; // Turn the Freezer actively freezing mode off
                FreezeStepInProgress = 0;
                ClosePodAnimPlaying = false;
                FreezeWindowAnimPlaying = false;
                ToFrzeKerbal = ""; // Set the Active Freeze Kerbal to null
                machine_hum.Stop(); // Stop the sound effects
                charge_up.Stop();
                StoredCharge = 0; // Discharge all EC stored
                UpdateCounts();  // Update the Crew counts
                onvslchgInternal = true;
                GameEvents.onVesselChange.Fire(vessel);
                ScreenMessages.RemoveMessage(FreezeMsg);
                //Add them from the GUIManager Portrait cams.
                if (!KerbalGUIManager.ActiveCrew.Contains(CrewMember.KerbalRef))
                {
                    KerbalGUIManager.AddActiveCrew(CrewMember.KerbalRef);
                    KerbalGUIManager.PrintActiveCrew();
                }
            }
            catch (Exception ex)
            {
                this.Log("Unable to to cancel freeze of crewmember " + CrewMember.name);
                this.Log("Err: " + ex);
            }
            this.Log_Debug("FreezeKerbalAbort ended");
        }

        private void FreezeKerbalConfirm(ProtoCrewMember CrewMember)
        {
            //this method runs with the freeze process is complete (EC consumed)
            //it will store the frozen crew member's details in the _StorecrewList and KnownFrozenKerbals dictionary
            //it will remove the kerbal from the part and set their status to dead and unknown
            this.Log_Debug("FreezeKerbalConfirm kerbal " + CrewMember.name + " seatIdx " + ToFrzeKerbalSeat);
            StoredCharge = 0;  // Discharge all EC stored
            //Make them invisible
            if (partHasInternals)
            {
                Utilities.setFrznKerbalLayer(CrewMember, false, false);
            }
            //Remove them
            if (!RemoveKerbal(CrewMember, ToFrzeKerbalSeat))
            {
                FreezeKerbalAbort(CrewMember);
            }
            else
            {
                //Set internal cam if in IVA mode
                if (vesselisinIVA || vesselisinInternal)
                {
                    setIVAFrzrCam(ToFrzeKerbalSeat);
                }
                if (partHasStripLights && DeepFreeze.Instance.DFsettings.StripLightsActive)
                {
                    stopStripLightFlash(ToFrzeKerbalSeat);
                }
                UpdateCounts();                       // Update the Crew counts
                IsFreezeActive = false;               // Turn the Freezer actively freezing mode off
                ToFrzeKerbal = "";                    // Set the Active Freeze Kerbal to null
                ActiveFrzKerbal = null;               // Set the Active Freeze Kerbal to null
                removeFreezeEvent(CrewMember.name);   // Remove the Freeze Event for this kerbal.
                if (DFInstalledMods.IsUSILSInstalled) // IF USI LS Installed, remove tracking.
                {
                    this.Log_Debug("USI/LS installed untrack kerbal=" + CrewMember.name);
                    try
                    {
                        USIUntrackKerbal(CrewMember.name);
                    }
                    catch (Exception ex)
                    {
                        Utilities.Log("DeepFreeze", "Exception attempting to untrack a kerbal in USI/LS. Report this error on the Forum Thread.");
                        Utilities.Log("DeepFreeze", "Err: " + ex);
                    }
                }
                ScreenMessages.PostScreenMessage(CrewMember.name + " frozen", 5.0f, ScreenMessageStyle.UPPER_CENTER);

                onvslchgInternal = true;
                GameEvents.onVesselChange.Fire(vessel);
                GameEvents.onVesselWasModified.Fire(vessel);
            }
            this.Log_Debug("FreezeCompleted");
        }

        private void USIUntrackKerbal(string crewmember)
        //This will remove tracking of a frozen kerbal from USI Life Support MOD, so that they don't consume resources when they are thawed.
        {
            if (USIWrapper.APIReady && USIWrapper.InstanceExists)
            {
                USIWrapper.USIUntrackKerbal.UntrackKerbal(crewmember);
            }
            else
            {
                Debug.Log("DeepFreeze has been unable to connect to Texture Replacer mod. API is not ready. Report this error on the Forum Thread.");
            }
        }

        #endregion FrzKerbals

        #region ThwKerbals

        //This region contains the methods for thawing a kerbal
        private void ProcessThawKerbal()
        {
            Utilities.Log_Debug("DeepFreezer", "ThawActive Kerbal = " + ToThawKerbal);
            switch (ThawStepInProgress)
            {
                case 0:
                    //Begin
                    //this.Log_Debug("Thaw Step 0");
                    ThawKerbalStep0(ToThawKerbal);
                    if (vesselisinInternal)
                    {
                        setIVAFrzrCam(ToThawKerbalSeat);
                    }
                    charge_up.Play();  // Play the sound effects.
                    charge_up.loop = true;

                    ThawStepInProgress = 1;
                    break;

                case 1:
                    //Get EC and Glykerol
                    //this.Log_Debug("Thaw Step 1");
                    if (skipThawStep1)
                    {
                        this.Log_Debug("Skipping step 1 of Thaw process");
                        charge_up.Stop();
                        ThawStepInProgress = 2;
                        break;
                    }
                    if (!requireResource(vessel, EC, ChargeRate, false, out ResAvail))
                    {
                        ScreenMessages.PostScreenMessage("Insufficient electric charge to thaw kerbal", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        ThawKerbalAbort(ToThawKerbal);
                    }
                    else
                    {
                        requireResource(vessel, EC, ChargeRate, true, out ResAvail);
                        StoredCharge = StoredCharge + ChargeRate;
                        ScreenMessages.RemoveMessage(ThawMsg);
                        ThawMsg = ScreenMessages.PostScreenMessage(" Cryopod - Charging: " + StoredCharge.ToString("######0"));

                        if (DeepFreeze.Instance.DFsettings.RegTempReqd)
                        {
                            this.part.AddThermalFlux(heatamtThawFreezeKerbal);
                        }
                        if (StoredCharge >= ChargeRequired)
                        {
                            this.Log_Debug("Stored charge requirement met. Have EC");
                            ScreenMessages.RemoveMessage(ThawMsg);
                            charge_up.Stop();
                            ThawStepInProgress = 2;
                        }
                    }
                    break;

                case 2:
                    //thaw the cryopod window
                    //this.Log_Debug("Thaw Step 2");
                    if (partHasInternals)
                    {
                        if (!ThawWindowAnimPlaying)  // If animation not already playing start it playing.
                        {
                            this.Log_Debug("Thawing the cryopod window");
                            ice_freeze.Play();
                            ThawWindowAnimPlaying = true;
                            if (isPartAnimated || (isPodExternal && DFInstalledMods.IsRPMInstalled && _prevRPMTransparentpodSetting == "ON"))
                                thawCryopodWindow(ToThawKerbalSeat, 1f);
                            if (partHasStripLights && DeepFreeze.Instance.DFsettings.StripLightsActive)
                            {
                                startStripLightFlash(ToThawKerbalSeat);
                            }
                        }
                        else  // Animation is already playing, check if it has finished.
                        {
                            if (_windowAnimation != null)
                            {
                                if (_windowAnimation.IsPlaying("CryopodWindowOpen"))
                                {
                                    //this.Log_Debug("waiting for the pod animation to complete the thaw");
                                    ThawWindowAnimPlaying = true;
                                }
                                else
                                {
                                    this.Log_Debug("Animation has completed. go to step 3.");
                                    ThawWindowAnimPlaying = false;
                                    ThawStepInProgress = 3;
                                }
                            }
                            else
                            {
                                this.Log_Debug("Animation disappeared. go to step 3.");
                                //There is no animation found? Skip to step 3.
                                ThawWindowAnimPlaying = false;
                                ThawStepInProgress = 3;
                            }
                        }
                    }
                    else
                    {
                        //Part is not animated, skip to step 4.
                        ThawWindowAnimPlaying = false;
                        ThawStepInProgress = 4;
                    }
                    //}
                    break;

                case 3:
                    //open the Pod door Hal
                    //this.Log_Debug("Thaw Step 3");
                    if (partHasInternals && isPartAnimated)
                    {
                        if (!OpenPodAnimPlaying)  // If animation not already playing start it playing.
                        {
                            this.Log_Debug("Opening the cryopod");
                            hatch_lock.Play();  // Play the sound effects.
                            machine_hum.Play();
                            machine_hum.loop = true;
                            OpenPodAnimPlaying = true;
                            openCryopod(ToThawKerbalSeat, 1f);
                            //cryopodstateclosed[ToThawKerbalSeat] = false;
                            //savecryopodstatepersistent();
                        }
                        else  // Animation is already playing, check if it has finished.
                        {
                            if (_animation != null)
                            {
                                if (_animation.IsPlaying("Open"))
                                {
                                    //this.Log_Debug("waiting for the pod animation to complete the thaw");
                                    OpenPodAnimPlaying = true;
                                }
                                else
                                {
                                    this.Log_Debug("Animation has completed. go to step 4.");
                                    OpenPodAnimPlaying = false;
                                    ThawStepInProgress = 4;
                                }
                            }
                            else
                            {
                                //There is no animation found? Skip to step 4.
                                this.Log_Debug("Animation disappeared. go to step 4.");
                                OpenPodAnimPlaying = false;
                                ThawStepInProgress = 4;
                            }
                        }
                    }
                    else
                    {
                        //Part is not animated, skip to step 4.
                        OpenPodAnimPlaying = false;
                        cryopodstateclosed[ToThawKerbalSeat] = false;
                        savecryopodstatepersistent();
                        ThawStepInProgress = 4;
                    }
                    break;

                case 4:
                    //Finalise
                    //this.Log_Debug("Thaw Step 4");
                    ThawKerbalStep4(ToThawKerbal);
                    break;
            }
        }

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

                    ToThawKerbal = frozenkerbal;  // Set the Active Thaw Kerbal to frozenkerbal name
                    IsThawActive = true;  // Turn the Freezer actively thawing mode on
                    ThawStepInProgress = 0;
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

        private void ThawKerbalStep0(string frozenkerbal)
        {
            // First we find out Unowned Crewmember in the roster.
            ProtoCrewMember kerbal = HighLogic.CurrentGame.CrewRoster.Unowned.FirstOrDefault(a => a.name == frozenkerbal);
            if (kerbal != null)
            {
                // Set our newly thawed Popsicle, er Kerbal, to Crew type again (from Unowned) and Assigned status (from Dead status).
                this.Log_Debug("set type to crew and assigned");
                kerbal.type = ProtoCrewMember.KerbalType.Crew;
                kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                this.Log_Debug("find the stored crew member");
                //Now we find our Crewmember in the stored crew list in the part.
                FrznCrewMbr tmpcrew = _StoredCrewList.Find(a => a.CrewName == frozenkerbal);  // Find the thawed kerbal in the frozen kerbal list.
                if (tmpcrew != null)
                {
                    //check if seat is empty, if it is we have to seat them in next available seat
                    this.Log_Debug("frozenkerbal " + tmpcrew.CrewName + ",seatindx=" + tmpcrew.SeatIdx);
                    ToThawKerbalSeat = tmpcrew.SeatIdx;
                    if (partHasInternals)  // All deepfreeze supplied parts have internals.
                    {
                        this.Log_Debug("Part has internals");
                        this.Log_Debug("Checking their seat taken=" + this.part.internalModel.seats[tmpcrew.SeatIdx].taken);
                        ProtoCrewMember crew = this.part.internalModel.seats[tmpcrew.SeatIdx].crew;
                        if (crew != null)
                        {
                            // we check the internal seat has our Crewmember in it. Not some other kerbal.
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
                                    }
                                    if (kerbal.KerbalRef != null)
                                    {
                                        Utilities.subdueIVAKerbalAnimations(kerbal.KerbalRef);
                                    }
                                    Utilities.setFrznKerbalLayer(kerbal, true, false);  //Set the Kerbal renderer layers on so they are visible again.
                                    kerbal.KerbalRef.InPart = this.part; //Put their kerbalref back in the part.
                                    kerbal.KerbalRef.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                                    try
                                    {
                                        if (DFInstalledMods.IsTexReplacerInstalled)
                                        {
                                            TexReplacerPersonaliseKerbal(kerbal.KerbalRef);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.Log("Exception attempting to restore Kerbals Texture Replacer mod customisations. Report this error on the Forum Thread.");
                                        Debug.Log("Err: " + ex);
                                    }

                                    KerbalGUIManager.AddActiveCrew(kerbal.KerbalRef); //Add them to the portrait cams.
                                    this.Log_Debug("Just thawing crew and added to GUIManager");
                                    KerbalGUIManager.PrintActiveCrew();
                                    //Utilities.setFrznKerbalLayer(kerbal, false, true);
                                    this.Log_Debug("Expected condition met, kerbal already in their seat.");
                                    // If in IVA mode set the camera to watch the process.
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
                        // The Seat's Crew is set to NULL. This could happen when on UPGRADE from V0.17 and below, or where vessel is loaded in range of the active vessel on flight scene startup.
                        // and then the user switches to this vessel and thaws a kerbal.
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
                                if (kerbal.KerbalRef != null)
                                {
                                    Utilities.subdueIVAKerbalAnimations(kerbal.KerbalRef);
                                }
                                Utilities.setFrznKerbalLayer(kerbal, true, false);  //Set the Kerbal renderer layers on so they are visible again.
                                kerbal.KerbalRef.InPart = this.part; //Put their kerbalref back in the part.
                                kerbal.KerbalRef.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                                try
                                {
                                    if (DFInstalledMods.IsTexReplacerInstalled)
                                    {
                                        TexReplacerPersonaliseKerbal(kerbal.KerbalRef);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.Log("Exception attempting to restore Kerbals Texture Replacer mod customisations. Report this error on the Forum Thread.");
                                    Debug.Log("Err: " + ex);
                                }

                                KerbalGUIManager.AddActiveCrew(kerbal.KerbalRef); //Add them to the portrait cams.
                                this.Log_Debug("Just thawing crew and added to GUIManager");
                                KerbalGUIManager.PrintActiveCrew();
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
                                                              //seatTakenbyFrznKerbal[ToThawKerbalSeat] = false;
                                                              //kerbal.seat.SpawnCrew();
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
                    Debug.Log("Could not find frozen kerbal in _StoredCrewList to Thaw, Very Very Bad. Report this to Mod thread");
                    ScreenMessages.PostScreenMessage("Code Error: Cannot thaw kerbal at this time, Check Log", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    ThawKerbalAbort(frozenkerbal);
                    return;
                }
            }
            else // This should NEVER occur.
            {
                Debug.Log("Could not find frozen kerbal in Unowned Crew List to Thaw, Very Very Bad. Report this to Mod thread");
                ScreenMessages.PostScreenMessage("Code Error: Cannot thaw kerbal at this time, Check Log", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                ThawKerbalAbort(frozenkerbal);
                return;
            }
        }

        private void TexReplacerPersonaliseKerbal(Kerbal kerbal)
        {
            //This will re-personalise a kerbal who has been personalised using Texture replacer mod.
            try
            {
                this.Log_Debug("Texture Replacer installed. Re-PersonliseKerbal");
                if (TRWrapper.APIReady && TRWrapper.InstanceExists)
                {
                    TRWrapper.TexRepPersonaliser.personaliseIva(kerbal);
                }
                else
                {
                    Debug.Log("DeepFreeze has been unable to connect to Texture Replacer mod. API is not ready. Report this error on the Forum Thread.");
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Exception attempting to restore Kerbals Texture Replacer mod customisations. Report this error on the Forum Thread.");
                Debug.Log("Err: " + ex);
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
            charge_up.Stop();
            OpenPodAnimPlaying = false;
            ThawWindowAnimPlaying = false;
            ThawStepInProgress = 0;
            ProtoCrewMember kerbal = HighLogic.CurrentGame.CrewRoster.Crew.FirstOrDefault(a => a.name == ThawKerbal);
            if (!RemoveKerbal(kerbal, ToThawKerbalSeat))  // remove the CrewMember from the part, because they are frozen, and this is the only way to trick the game.
            {
                Debug.Log("ThawKerbalAbort Procedure FAILED! Critical error");
            }
            //this.part.RemoveCrewmember(kerbal);
            kerbal.type = ProtoCrewMember.KerbalType.Unowned;
            kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
            seatTakenbyFrznKerbal[ToThawKerbalSeat] = true;
            if (vesselisinIVA || vesselisinInternal)
            {
                setIVAFrzrCam(ToThawKerbalSeat);
            }
            if (partHasInternals)
            {
                if (isPartAnimated)
                    closeCryopod(ToThawKerbalSeat, float.MaxValue);
                this.Log_Debug("Time freezewindow started " + Planetarium.GetUniversalTime());
                freezeCryopodWindow(ToThawKerbalSeat, float.MaxValue);
                this.Log_Debug("Time freezewindow finished make them invisible " + Planetarium.GetUniversalTime());
                cryopodstateclosed[ToThawKerbalSeat] = true;
                savecryopodstatepersistent();
                if (partHasStripLights && DeepFreeze.Instance.DFsettings.StripLightsActive)
                {
                    stopStripLightFlash(ToThawKerbalSeat);
                }
            }
            //Make them invisible again
            Utilities.setFrznKerbalLayer(kerbal, false, false);
            ScreenMessages.RemoveMessage(ThawMsg);
            this.Log_Debug("ThawkerbalAbort End");
        }

        private void ThawKerbalStep4(String frozenkerbal)
        {
            this.Log_Debug("ThawKerbalConfirm start for " + frozenkerbal);
            machine_hum.Stop(); //stop sound effects
            StoredCharge = 0;   // Discharge all EC stored

            ProtoCrewMember kerbal = HighLogic.CurrentGame.CrewRoster.Crew.FirstOrDefault(a => a.name == frozenkerbal);

            if (!AddKerbal(kerbal, ToThawKerbalSeat))
            {
                ThawKerbalAbort(frozenkerbal);
                return;
            }
            if (partHasStripLights && DeepFreeze.Instance.DFsettings.StripLightsActive)
            {
                stopStripLightFlash(ToThawKerbalSeat);
            }
            foreach (Animator anim in kerbal.KerbalRef.gameObject.GetComponentsInChildren<Animator>())
            {
                if (anim.name == "kbIVA@idle")
                {
                    this.Log_Debug("Animator " + anim.name + " for " + kerbal.KerbalRef.name + " turned off");
                    anim.enabled = false;
                }
            }
            ToThawKerbal = ""; // Set the Active Thaw Kerbal to null
            IsThawActive = false; // Turn the Freezer actively thawing mode off
            ThawStepInProgress = 0;
            skipThawStep1 = false;
            ScreenMessages.PostScreenMessage(frozenkerbal + " thawed out", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            if (emergencyThawInProgress)
            {
                ScreenMessages.PostScreenMessage(frozenkerbal + " was thawed out due to lack of Electrical Charge to run cryogenics", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                Debug.Log("DeepFreezer - kerbal " + frozenkerbal + " was thawed out due to lack of Electrical charge to run cryogenics");
                Utilities.setComatoseKerbal(kerbal, ProtoCrewMember.KerbalType.Tourist);

                // Update the saved frozen kerbals dictionary
                KerbalInfo kerbalInfo = new KerbalInfo(Planetarium.GetUniversalTime());
                kerbalInfo.vesselID = CrntVslID;
                kerbalInfo.vesselName = CrntVslName;
                kerbalInfo.experienceTraitName = kerbal.experienceTrait.Title;
                kerbalInfo.type = ProtoCrewMember.KerbalType.Tourist;
                kerbalInfo.status = ProtoCrewMember.RosterStatus.Assigned;
                if (partHasInternals)
                {
                    kerbalInfo.seatName = ToFrzeKerbalXformNme;
                    kerbalInfo.seatIdx = ToFrzeKerbalSeat;
                }
                else
                {
                    kerbalInfo.seatName = "Unknown";
                    kerbalInfo.seatIdx = -1;
                }
                kerbalInfo.partID = CrntPartID;
                this.Log_Debug("Adding New Comatose Crew to dictionary");
                try
                {
                    if (!DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.ContainsKey(kerbal.name))
                    {
                        DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Add(kerbal.name, kerbalInfo);
                    }
                    if (DeepFreeze.Instance.DFsettings.debugging) DeepFreeze.Instance.DFgameSettings.DmpKnownFznKerbals();
                }
                catch (Exception ex)
                {
                    this.Log("Unable to add to knownfrozenkerbals comatose crewmember " + kerbal.name);
                    this.Log("Err: " + ex);
                    ScreenMessages.PostScreenMessage("DeepFreezer mechanical failure", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                }
            }
            Debug.Log("Thawed out: " + frozenkerbal);
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

        private bool RemoveKerbal(ProtoCrewMember kerbal, int SeatIndx)
        //Removes a frozen kerbal from the vessel.
        {
            try
            {
                this.Log_Debug("RemoveKerbal " + kerbal.name + " seat " + SeatIndx);
                FrznCrewMbr tmpcrew = _StoredCrewList.Find(a => a.CrewName == kerbal.name);  // Find the thawed kerbal in the frozen kerbal list.
                if (tmpcrew == null)
                {
                    FrznCrewMbr frzncrew = new FrznCrewMbr(kerbal.name, SeatIndx, this.vessel.id, this.vessel.name);
                    this.Log_Debug("Adding _StoredCrewList entry");
                    _StoredCrewList.Add(frzncrew);
                }
                else
                {
                    this.Log("Found Kerbal in the stored frozen crew list for this part, critical error. Report this on the forum.");
                    this.Log("Crewmember:" + tmpcrew.CrewName + " Seat:" + tmpcrew.SeatIdx);
                }
                // Update the saved frozen kerbals dictionary
                KerbalInfo kerbalInfo = new KerbalInfo(Planetarium.GetUniversalTime());
                kerbalInfo.vesselID = CrntVslID;
                kerbalInfo.vesselName = CrntVslName;
                kerbalInfo.type = ProtoCrewMember.KerbalType.Unowned;
                kerbalInfo.status = ProtoCrewMember.RosterStatus.Dead;
                if (partHasInternals)
                {
                    kerbalInfo.seatName = this.part.internalModel.seats[SeatIndx].seatTransformName;
                    kerbalInfo.seatIdx = SeatIndx;
                }
                else
                {
                    kerbalInfo.seatName = "Unknown";
                    kerbalInfo.seatIdx = -1;
                }
                kerbalInfo.partID = CrntPartID;
                kerbalInfo.experienceTraitName = kerbal.experienceTrait.Title;
                this.Log_Debug("Adding New Frozen Crew to dictionary");
                try
                {
                    if (!DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.ContainsKey(kerbal.name))
                    {
                        DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Add(kerbal.name, kerbalInfo);
                    }
                    if (DeepFreeze.Instance.DFsettings.debugging) DeepFreeze.Instance.DFgameSettings.DmpKnownFznKerbals();
                }
                catch (Exception ex)
                {
                    this.Log("Unable to add to knownfrozenkerbals frozen crewmember " + kerbal.name);
                    this.Log("Err: " + ex);
                    ScreenMessages.PostScreenMessage("DeepFreezer mechanical failure", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return false;
                }
                if (partHasInternals && hasExternalDoor)
                    Utilities.setHelmetshaders(kerbal.KerbalRef, true);
                // remove the CrewMember from the part crewlist and unregister their traits, because they are frozen, and this is the only way to trick the game.
                kerbal.UnregisterExperienceTraits(this.part);
                this.part.protoModuleCrew.Remove(kerbal);
                if (partHasInternals)
                {
                    if (this.part.internalModel.seats[SeatIndx].kerbalRef != kerbal.KerbalRef)
                    {
                        this.part.internalModel.seats[SeatIndx].kerbalRef = kerbal.KerbalRef;
                        setseatstaticoverlay(this.part.internalModel.seats[SeatIndx]);
                    }
                    this.part.internalModel.seats[SeatIndx].taken = true; // Set their seat to Taken, because they are really still there. :)
                    seatTakenbyFrznKerbal[SeatIndx] = true;
                }
                // Set our newly frozen Popsicle, er Kerbal, to Unowned type (usually a Crew) and Dead status.
                kerbal.type = ProtoCrewMember.KerbalType.Unowned;
                kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                if (kerbal.KerbalRef != null)
                {
                    kerbal.KerbalRef.InPart = null;
                    //Remove them from the GUIManager Portrait cams.
                    if (KerbalGUIManager.ActiveCrew.Contains(kerbal.KerbalRef))
                    {
                        KerbalGUIManager.RemoveActiveCrew(kerbal.KerbalRef);
                        KerbalGUIManager.PrintActiveCrew();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.Log("Remove Kerbal " + kerbal.name + " for DeepFreeze failed");
                Debug.Log("Err: " + ex);
                return false;
            }
        }

        private bool AddKerbal(ProtoCrewMember kerbal, int SeatIndx)
        //Adds a just thawed kerbal to the vessel.
        {
            this.Log_Debug("Start AddKerbal " + kerbal.name);
            try
            {
                //FrznCrewMbr tmpcrew = new FrznCrewMbr(kerbal.name, SeatIndx, this.vessel.id, this.vessel.name);
                try
                {
                    FrznCrewMbr tmpcrew = _StoredCrewList.Find(a => a.CrewName == kerbal.name);  // Find the thawed kerbal in the frozen kerbal list.
                    if (_StoredCrewList.Contains(tmpcrew))
                    {
                        this.Log_Debug("Removing _StoredCrewList entry");
                        _StoredCrewList.Remove(tmpcrew);
                    }
                }
                catch (Exception ex)
                {
                    this.Log("Unable to remove knownfrozenkerbals frozen crewmember " + kerbal.name);
                    this.Log("Err: " + ex);
                    //ScreenMessages.PostScreenMessage("DeepFreezer mechanical failure", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    //return false;
                }

                // Update the saved frozen kerbals dictionary
                this.Log_Debug("Removing Frozen Crew to dictionary");
                try
                {
                    if (DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.ContainsKey(kerbal.name))
                    {
                        DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Remove(kerbal.name);
                    }
                    if (DeepFreeze.Instance.DFsettings.debugging) DeepFreeze.Instance.DFgameSettings.DmpKnownFznKerbals();
                }
                catch (Exception ex)
                {
                    this.Log("Unable to remove knownfrozenkerbals frozen crewmember " + kerbal.name);
                    this.Log("Err: " + ex);
                    ScreenMessages.PostScreenMessage("DeepFreezer mechanical failure", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return false;
                }
                if (partHasInternals && hasExternalDoor)
                    Utilities.setHelmetshaders(kerbal.KerbalRef, true);
                // add the CrewMember to the part crewlist and register their traits.
                kerbal.RegisterExperienceTraits(this.part);
                if (!this.part.protoModuleCrew.Contains(kerbal))
                {
                    this.part.protoModuleCrew.Add(kerbal);
                }
                // Set our newly thawed Popsicle, er Kerbal, to Crew type and Assigned status.
                if (kerbal.type != ProtoCrewMember.KerbalType.Crew)
                {
                    kerbal.type = ProtoCrewMember.KerbalType.Crew;
                    kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                }
                if (partHasInternals)
                {
                    if (kerbal.seat != this.part.internalModel.seats[SeatIndx])
                    {
                        kerbal.seat = this.part.internalModel.seats[SeatIndx];
                        kerbal.seatIdx = SeatIndx;
                    }
                    if (this.part.internalModel.seats[SeatIndx].crew != kerbal)
                    {
                        this.part.internalModel.seats[SeatIndx].crew = kerbal;
                    }
                    if (this.part.internalModel.seats[SeatIndx].kerbalRef != kerbal.KerbalRef)
                    {
                        this.part.internalModel.seats[SeatIndx].kerbalRef = kerbal.KerbalRef;
                        this.part.internalModel.seats[SeatIndx].taken = true;
                        setseatstaticoverlay(this.part.internalModel.seats[SeatIndx]);
                    }
                    seatTakenbyFrznKerbal[SeatIndx] = false;
                }
                if (kerbal.KerbalRef != null)
                {
                    if (kerbal.KerbalRef.InPart == null)
                    {
                        kerbal.KerbalRef.InPart = this.part;
                    }
                    //Add themto the GUIManager Portrait cams.
                    if (!KerbalGUIManager.ActiveCrew.Contains(kerbal.KerbalRef))
                    {
                        KerbalGUIManager.AddActiveCrew(kerbal.KerbalRef);
                        KerbalGUIManager.PrintActiveCrew();
                    }
                }
                this.Log_Debug("End AddKerbal");
                return true;
            }
            catch (Exception ex)
            {
                Debug.Log("Add Kerbal " + kerbal.name + " for DeepFreeze failed");
                Debug.Log("Err: " + ex);
                return false;
            }
        }

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
                        if (xfercrew.KerbalRef != null)
                        {
                            Utilities.reinvigerateIVAKerbalAnimations(xfercrew.KerbalRef);
                        }
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
                if (xfercrew.KerbalRef != null)
                {
                    Utilities.reinvigerateIVAKerbalAnimations(xfercrew.KerbalRef);
                }
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
            double TimeDelay = DeepFreeze.Instance.DFsettings.defaultTimeoutforCrewXfer + crewXferSMTimeDelay;
            if (_crewXferFROMActive && (Time.time - timecrewXferFROMfired) > TimeDelay)
            {
                //Cancel
                Debug.Log("CrewXfer Timed OUT, Cancelling Tracking of CrewXfer");
                resetFrozenKerbals();
                if (partHasInternals)
                {
                    resetCryopods(true);
                }
                _crewXferFROMActive = false;
                crewXferSMActive = false;
                crewXferSMStock = false;
                FlightEVA.fetch.EnableInterface();
                return;
            }

            if (_crewXferTOActive && (Time.time - timecrewXferTOfired) > TimeDelay)
            {
                //Cancel
                Debug.Log("CrewXfer Timed OUT, Cancelling Tracking of CrewXfer");
                resetFrozenKerbals();
                if (partHasInternals)
                {
                    resetCryopods(true);
                }
                _crewXferTOActive = false;
                crewXferSMActive = false;
                crewXferSMStock = false;
                FlightEVA.fetch.EnableInterface();
                return;
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
                        if (partHasInternals)
                        {
                            resetCryopods(true);
                        }
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
                        if (partHasInternals)
                        {
                            resetCryopods(true);
                        }
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
                            if (partHasInternals)
                            {
                                resetCryopods(true);
                            }
                        }
                        else
                        {
                            if (partHasInternals)
                            {
                                resetCryopods(true);
                            }
                        }
                        if (xfercrew.KerbalRef != null)
                        {
                            Utilities.subdueIVAKerbalAnimations(xfercrew.KerbalRef);
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
                            if (partHasInternals)
                            {
                                resetCryopods(true);
                            }
                        }
                        else
                        {
                            if (partHasInternals)
                            {
                                resetCryopods(true);
                            }
                        }
                        if (crew.KerbalRef != null)
                        {
                            Utilities.subdueIVAKerbalAnimations(crew.KerbalRef);
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
                if (partHasInternals)
                {
                    resetCryopods(true);
                }
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
                if (partHasInternals)
                {
                    resetCryopods(true);
                }
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
        // Triggered when switching to a different vessel, loading a vessel, or launching
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
            //Check a Freeze or Thaw is not in progress, if it is, we must abort.
            if (IsThawActive)
            {
                ScreenMessages.PostScreenMessage("Vessel about to change, Aborting Thaw process", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                this.Log_Debug("Thawisactive - abort");
                ThawKerbalAbort(ToThawKerbal);
            }
            if (IsFreezeActive)
            {
                ScreenMessages.PostScreenMessage("Vessel about to change, Aborting Freeze process", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                this.Log_Debug("Freezeisactive - abort");
                FreezeKerbalAbort(ActiveFrzKerbal);
            }
            //If the vessel we have changed to is the same as the vessel this partmodule is attached to we LOAD persistent vars, otherwise we SAVE persistent vars.
            if (vessel.id == this.vessel.id)
            {
                loadcryopodstatepersistent();
                loadexternaldoorstatepersistent();
                resetFrozenKerbals();
                if (!hasJSITransparentPod)
                {
                    Utilities.CheckPortraitCams(vessel);
                }
            }
            else
            {
                savecryopodstatepersistent();
                saveexternaldoorstatepersistent();
                if (mon_beep.isPlaying)
                {
                    mon_beep.Stop();
                }
            }
        }

        // this is called when vessel is destroyed.
        //     Triggered when a vessel instance is destroyed; any time a vessel is unloaded,
        //     ie scene changes, exiting loading distance
        private void onVesselDestroy(Vessel vessel)
        {
            this.Log_Debug("OnVesselDestroy");
            //Check a Freeze or Thaw is not in progress, if it is, we must abort.
            if (IsThawActive)
            {
                ScreenMessages.PostScreenMessage("Vessel about to change, Aborting Thaw process", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                this.Log_Debug("Thawisactive - abort");
                ThawKerbalAbort(ToThawKerbal);
            }
            if (IsFreezeActive)
            {
                ScreenMessages.PostScreenMessage("Vessel about to change, Aborting Freeze process", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                this.Log_Debug("Freezeisactive - abort");
                FreezeKerbalAbort(ActiveFrzKerbal);
            }
        }

        internal void resetFrozenKerbals()
        {
            try
            {
                // Create a list of kerbals that are in InvSeats (SeatIndx == -1 where kerbal is in this part in this vessel & they are not comatose/tourist
                List<KeyValuePair<string, KerbalInfo>> kerbalsInvSeats = DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Where(e => e.Value.partID == CrntPartID && e.Value.vesselID == CrntVslID && e.Value.type != ProtoCrewMember.KerbalType.Tourist && e.Value.seatIdx == -1).ToList();
                // create a list of kerbal that are in this part in this vessel & they are not comatose/tourist
                List<KeyValuePair<string, KerbalInfo>> FrznKerbalsinPart = DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Where(e => e.Value.partID == CrntPartID && e.Value.vesselID == CrntVslID && e.Value.type != ProtoCrewMember.KerbalType.Tourist).ToList();

                if (kerbalsInvSeats.Count() > 0) //If we found any Invalid Seat assignments we need to find them empty seats
                {
                    bool[] seatIndxs = new bool[FreezerSize];  //Create a bool array to store whether seats are taken or not
                                                               //go through all the frozen kerbals in the part that don't have invalid seats and set bool array seat index to true (taken) for each
                    foreach (KeyValuePair<string, KerbalInfo> frznkerbal in FrznKerbalsinPart)
                    {
                        if (frznkerbal.Value.seatIdx > -1 && frznkerbal.Value.seatIdx < FreezerSize - 1)
                            seatIndxs[frznkerbal.Value.seatIdx] = true;
                    }
                    //go through all the thawed kerbals in the part and set bool array seat index to true (taken) for each
                    foreach (ProtoCrewMember crew in this.part.protoModuleCrew)
                    {
                        seatIndxs[crew.seatIdx] = true;
                    }
                    //Go through all our kerbals with invalid seats and find them an empty seat.
                    foreach (KeyValuePair<string, KerbalInfo> frznkerbal in kerbalsInvSeats)
                    {
                        //Iterate for the number of seats in the part
                        for (int i = 0; i < FreezerSize; i++)
                        {
                            if (seatIndxs[i] == false)  //If seat not already taken we take it
                            {
                                seatIndxs[i] = true;
                                frznkerbal.Value.seatIdx = i;
                                frznkerbal.Value.seatName = this.part.internalModel.seats[i].seatTransformName;
                                break;
                            }
                        }
                    }
                }
                // Iterate through the dictionary of all known frozen kerbals where kerbal is in this part in this vessel & they are not comatose/tourist
                foreach (KeyValuePair<string, KerbalInfo> kerbal in FrznKerbalsinPart)
                {
                    //Check if they are in the _StoredCrewList and if they aren't Add them in.
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
                    if (partHasInternals)
                    {
                        crewmember.seatIdx = kerbal.Value.seatIdx;
                        if (crewmember.seatIdx != -1 && crewmember.seatIdx < FreezerSize)
                            crewmember.seat = this.part.internalModel.seats[crewmember.seatIdx];
                        if (crewmember.KerbalRef == null)
                        {
                            crewmember.Spawn();
                        }
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
                        seatTakenbyFrznKerbal[crewmember.seatIdx] = true;
                        //setup seat and part settings for frozen kerbal.
                        Utilities.setFrznKerbalLayer(crewmember, false, false);
                        this.part.internalModel.seats[crewmember.seatIdx].taken = true;
                        this.part.internalModel.seats[crewmember.seatIdx].kerbalRef = crewmember.KerbalRef;
                        this.part.internalModel.seats[crewmember.seatIdx].crew = crewmember;
                        setseatstaticoverlay(this.part.internalModel.seats[crewmember.seatIdx]);
                    }
                    //Unregister their traits/abilities and remove them from the Portrait Cameras if they are there.
                    crewmember.UnregisterExperienceTraits(this.part);
                    this.part.protoModuleCrew.Remove(crewmember);
                    if (KerbalGUIManager.ActiveCrew.Contains(crewmember.KerbalRef))
                    {
                        KerbalGUIManager.RemoveActiveCrew(crewmember.KerbalRef);
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.Log("DeepFreezer", " Error attempting to resetFrozenKerbals, Critical ERROR, Report on the forum");
                Utilities.Log("DeepFreezer ", ex.Message);
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
            try
            {
                if (!IsThawActive && !IsFreezeActive)
                {
                    FreezerSpace = (FreezerSize - _StoredCrewList.Count);
                    TotalFrozen = _StoredCrewList.Count;
                    PartFull = (TotalFrozen + this.part.protoModuleCrew.Count >= this.part.CrewCapacity);
                    //Utilities.Log_Debug("DeepFreezer", "UpdateCounts FreezerSpace=" + FreezerSpace + ",TotalFrozen=" + TotalFrozen + ",Partfull=" + PartFull);
                    // Reset the seat status for frozen crew to taken - true, because it seems to reset by something?? So better safe than sorry.
                    if (partHasInternals)
                    {
                        // reset seats to TAKEN for all frozen kerbals in the part, check KerbalRef is still in place or re-instantiate it and check frozen kerbals
                        // are not appearing in the Portrait Cameras, if they are remove them.
                        //Utilities.Log_Debug("DeepFreezer", "StoredCrewList");
                        foreach (FrznCrewMbr lst in _StoredCrewList)
                        {
                            this.part.internalModel.seats[lst.SeatIdx].taken = true;
                            seatTakenbyFrznKerbal[lst.SeatIdx] = true;                            
                            if (partHasInternals)
                            {
                                setCryopodWindowSpecular(lst.SeatIdx);
                            }
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
                                    //Remove them from the GUIManager Portrait cams.
                                    if (KerbalGUIManager.ActiveCrew.Contains(kerbal.KerbalRef))
                                    {
                                        KerbalGUIManager.RemoveActiveCrew(kerbal.KerbalRef);
                                        KerbalGUIManager.PrintActiveCrew();
                                    }
                                }
                                Utilities.setFrznKerbalLayer(kerbal, false, false);  // Double check kerbal is invisible.
                            }
                            string kerblrefstring;
                            if (this.part.internalModel.seats[lst.SeatIdx].kerbalRef == null) kerblrefstring = "kerbalref not found";
                            else kerblrefstring = this.part.internalModel.seats[lst.SeatIdx].kerbalRef.crewMemberName;
                            //Utilities.Log_Debug("DeepFreezer", "Frozen Crew SeatIdx= " + lst.SeatIdx + ",Seattaken=" + this.part.internalModel.seats[lst.SeatIdx].taken + ",KerbalRef=" + kerblrefstring);
                        }
                    }
                    //Utilities.Log_Debug("DeepFreezer", "UpdateCounts end");
                }
            }
            catch (Exception ex)
            {
                Utilities.Log("DeepFreezer", " Error attempting to updatePartCounts, Critical ERROR, Report on the forum");
                Utilities.Log("DeepFreezer ", ex.Message);
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
            if (HighLogic.LoadedSceneIsFlight)
            {
                try
                {
                    var cryopodstatestring = cryopodstateclosedstring.Split(',');
                    for (int i = 0; i < cryopodstatestring.Length; i++)
                    {
                        this.Log_Debug("parse cryopodstring " + i + " " + cryopodstatestring[i]);
                        if (cryopodstatestring[i] != string.Empty)
                        {
                            cryopodstateclosed[i] = bool.Parse(cryopodstatestring[i]);
                        }
                    }
                    Debug.Log("Load cryopodstatepersistent value " + cryopodstateclosedstring);
                }
                catch (Exception ex)
                {
                    Debug.Log("Exception OnLoad of cryopod state string");
                    Debug.Log("Err: " + ex);
                }
            }
        }

        private void savecryopodstatepersistent()
        {
            if (HighLogic.LoadedSceneIsFlight && Time.timeSinceLevelLoad > 3f)
            {
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
            }
        }

        public void resetCryopods(bool resetall)
        {
            try
            {
                // If resetall is true we check the last time a resetall was done, if it is within cryopodREsettimeDelay seconds
                // we skil this resetcryopods call (so we don't see flickering).
                //Otherwise we set all cryopodstatclosed to true which will force processing further down to open pods that
                // may already be open , regardless.
                if (resetall)
                {
                    double currenttime = Planetarium.GetUniversalTime();
                    if (currenttime - cryopodResetTime < DeepFreeze.Instance.DFsettings.cryopodResettimeDelay)
                    {
                        this.Log_Debug("Last cryopod resetall occurred at: " + cryopodResetTime + " currenttime: " + currenttime + " is less than " + DeepFreeze.Instance.DFsettings.cryopodResettimeDelay + " secs ago, Ignoring request.");
                        return;
                    }
                    cryopodResetTime = currenttime;
                    for (int i = 0; i < FreezerSize; i++)
                    {
                        cryopodstateclosed[i] = true;
                    }
                }

                //Create a temporary array and set entries to true where that seat index contains a frozen kerbal.
                bool[] closedpods = new bool[FreezerSize];                
                foreach (FrznCrewMbr frzncrew in _StoredCrewList)
                {
                    closedpods[frzncrew.SeatIdx] = true;
                }
                //Iterate through the closedpods array. If true (frozen kerbal in this pod) we check the state of the pod
                // is closed already or not. If it is not closed we close it.
                // If false (no frozen kerbal in this pod) we check the state of the pod is closed already or not.
                // If it is closed we open it.
                for (int i = 0; i < closedpods.Length; i++)
                {
                    this.Log_Debug("resetCryopod " + i + " contains frozen kerbal? " + closedpods[i]);
                    if (closedpods[i]) //Pod contains a frozen kerbal
                    {
                        if (!cryopodstateclosed[i])  //If we think the pod is not closed, we close it.
                        {
                            this.Log_Debug("pod is open so close it");
                            if (isPartAnimated)
                                closeCryopod(i, float.MaxValue);
                            cryopodstateclosed[i] = true;
                            //this.Log_Debug("Time freezewindow started " + Planetarium.GetUniversalTime());
                            freezeCryopodWindow(i, float.MaxValue);
                            //this.Log_Debug("Time freezewindow finished make them invisible " + Planetarium.GetUniversalTime());
                        }
                        else
                        {
                            this.Log_Debug("pod is already closed");
                            freezeCryopodWindow(i, float.MaxValue);
                        }
                    }
                    else  //Pod does not contain a frozen kerbal
                    {
                        if (cryopodstateclosed[i]) //If we think the pod is closed, we open it.
                        {
                            this.Log_Debug("pod is closed so open it");
                            if (isPartAnimated)
                            {                                
                                openCryopod(i, float.MaxValue);
                            }
                            thawCryopodWindow(i, float.MaxValue);                            
                            cryopodstateclosed[i] = false;
                        }
                        else
                        {
                            this.Log_Debug("pod is already open");                            
                            thawCryopodWindow(i, float.MaxValue);
                        }
                    }
                    setseatstaticoverlay(this.part.internalModel.seats[i]);
                }
                savecryopodstatepersistent();
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to reset cryopods in internal model for " + this.part.vessel.id.ToString() + " " + this.part.flightID);
                Debug.Log("Err: " + ex);
            }
        }

        private void openCryopod(int seatIndx, float speed) //only called for animated internal parts
        {
            string podname = "Animated-Cryopod-" + (seatIndx + 1).ToString();
            string windowname = "Animated-Cryopod-" + (seatIndx + 1).ToString() + "-Window";
            try
            {
                _animation = this.part.internalModel.FindModelComponent<Animation>(podname);
                if (_animation != null)
                {
                    if (cryopodstateclosed[seatIndx])
                    {
                        _animation["Open"].speed = speed;
                        _animation.Play("Open");
                        cryopodstateclosed[seatIndx] = false;
                        savecryopodstatepersistent();
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
        }

        private void thawCryopodWindow(int seatIndx, float speed)
        {
            setCryopodWindowOpaque(seatIndx);
            string windowname = "";
            if (isPartAnimated)
                windowname = "Animated-Cryopod-" + (seatIndx + 1).ToString() + "-Window";
            else
                windowname = "Cryopod-" + (seatIndx + 1).ToString() + "-Window";

            _windowAnimation = this.part.internalModel.FindModelComponent<Animation>(windowname);
            Animation _extwindowAnimation = null;
            if (isPodExternal)
            {
                _extwindowAnimation = this.part.FindModelComponent<Animation>(windowname);
            }

            if (_windowAnimation == null)
            {
                this.Log_Debug("Why can't I find the window animation?");
            }
            else
            {
                _windowAnimation["CryopodWindowOpen"].speed = speed;
                _windowAnimation.Play("CryopodWindowOpen");
                if (isPodExternal && _extwindowAnimation != null)
                {
                    _extwindowAnimation["CryopodWindowOpen"].speed = speed;
                    _extwindowAnimation.Play("CryopodWindowOpen");
                }
            }
        }

        private void setCryopodWindowOpaque(int seatIndx)
        {
            try
            {
                //Set their Window glass to fully opaque. - Just in case.
                string windowname = "";
                if (isPartAnimated)
                    windowname = "Animated-Cryopod-" + (seatIndx + 1).ToString() + "-Window";
                else
                    windowname = "Cryopod-" + (seatIndx + 1).ToString() + "-Window";
                Renderer windowrenderer = this.part.internalModel.FindModelComponent<Renderer>(windowname);
                if (windowrenderer != null)
                {
                    windowrenderer.material.shader = TransparentSpecularShader;
                    Color savedwindowcolor = windowrenderer.material.color;
                    savedwindowcolor.a = 1f;
                    windowrenderer.material.color = savedwindowcolor;
                }                
                if (isPodExternal)
                {
                    Renderer extwindowrenderer = this.part.FindModelComponent<Renderer>(windowname);
                    if (extwindowrenderer != null)
                    {
                        extwindowrenderer.material.shader = TransparentSpecularShader;
                        Color extsavedwindowcolor = extwindowrenderer.material.color;
                        extsavedwindowcolor.a = 1f;
                        extwindowrenderer.material.color = extsavedwindowcolor;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to setCryopodWindowOpaque seat " + seatIndx);
                Debug.Log("Err: " + ex);
            }
        }

        private void setCryopodWindowSpecular(int seatIndx)
        {
            try
            {
                //Set the window glass to specular shader
                string windowname = "";
                if (isPartAnimated)
                    windowname = "Animated-Cryopod-" + (seatIndx + 1).ToString() + "-Window";
                else
                    windowname = "Cryopod-" + (seatIndx + 1).ToString() + "-Window";

                Renderer windowrenderer = this.part.internalModel.FindModelComponent<Renderer>(windowname);
                Renderer extwindowrenderer = null;
                if (isPodExternal)
                    extwindowrenderer = this.part.FindModelComponent<Renderer>(windowname);

                if (windowrenderer != null && windowrenderer.material.shader != KSPSpecularShader)
                    windowrenderer.material.shader = KSPSpecularShader;
                
                if (isPodExternal && extwindowrenderer != null)
                {
                    if (extwindowrenderer.material.shader != KSPSpecularShader)
                        extwindowrenderer.material.shader = KSPSpecularShader;
                }                                    
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to setCryopodWindowSpecular seat " + seatIndx);
                Debug.Log("Err: " + ex);
            }
        }

        private void startStripLightFlash(int seatIndx)
        {
            string stripname = "lightStrip-Animated-Cryopod-" + (seatIndx + 1).ToString();
            //this.Log_Debug("playing animation PodActive " + stripname);
            try
            {
                Animation strip_animation = this.part.internalModel.FindModelComponent<Animation>(stripname);
                if (strip_animation != null)
                {
                    strip_animation.Stop();
                    strip_animation["PodActive"].speed = 1;
                    strip_animation["PodActive"].normalizedTime = 0;
                    strip_animation.wrapMode = WrapMode.Loop;
                    strip_animation.Play("PodActive");
                }
                else
                {
                    this.Log_Debug("animation PodActive not found for " + stripname);
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to run lightstrip animations in internal model for this part called " + stripname);
                Debug.Log("Err: " + ex);
            }
        }

        private void closeCryopod(int seatIndx, float speed) //only called for animated internal parts
        {
            string podname = "Animated-Cryopod-" + (seatIndx + 1).ToString();
            string windowname = "Animated-Cryopod-" + (seatIndx + 1).ToString() + "-Window";
            this.Log_Debug("playing animation closecryopod " + podname + " " + windowname);
            try
            {
                _animation = this.part.internalModel.FindModelComponent<Animation>(podname);
                if (_animation != null)
                {
                    if (!cryopodstateclosed[seatIndx])
                    {
                        _animation["Close"].speed = speed;
                        _animation.Play("Close");
                        cryopodstateclosed[seatIndx] = true;
                        savecryopodstatepersistent();
                    }
                }
                else
                    this.Log_Debug("Cryopod animation not found");
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to find animation in internal model for this part called " + podname);
                Debug.Log("Err: " + ex);
            }
        }

        private void freezeCryopodWindow(int seatIndx, float speed)
        {
            if (isPartAnimated || (isPodExternal && DFInstalledMods.IsRPMInstalled && _prevRPMTransparentpodSetting == "ON"))
                setCryopodWindowTransparent(seatIndx);
            else
                speed = float.MaxValue;
            string windowname = "";
            if (isPartAnimated)
                windowname = "Animated-Cryopod-" + (seatIndx + 1).ToString() + "-Window";
            else
                windowname = "Cryopod-" + (seatIndx + 1).ToString() + "-Window";

            _windowAnimation = this.part.internalModel.FindModelComponent<Animation>(windowname);
            Animation _extwindowAnimation = null;
            if (isPodExternal)
            {
                _extwindowAnimation = this.part.FindModelComponent<Animation>(windowname);
            }

            if (_windowAnimation == null)
            {
                this.Log_Debug("Why can't I find the window animation?");
            }
            else
            {
                _windowAnimation["CryopodWindowClose"].speed = speed;
                _windowAnimation.Play("CryopodWindowClose");
                if (isPodExternal && _extwindowAnimation != null)
                {
                    _extwindowAnimation["CryopodWindowClose"].speed = speed;
                    _extwindowAnimation.Play("CryopodWindowClose");
                }
            }
        }

        private void setCryopodWindowTransparent(int seatIndx)
        {
            try
            {
                //Set their Window glass to see-through. - Just in case.
                string windowname = "";
                if (isPartAnimated)
                    windowname = "Animated-Cryopod-" + (seatIndx + 1).ToString() + "-Window";
                else
                    windowname = "Cryopod-" + (seatIndx + 1).ToString() + "-Window";
                Renderer windowrenderer = this.part.internalModel.FindModelComponent<Renderer>(windowname);                
                    windowrenderer.material.shader = TransparentSpecularShader;
                    Color savedwindowcolor = windowrenderer.material.color;
                    savedwindowcolor.a = 0.3f;
                    windowrenderer.material.color = savedwindowcolor;
                
                if (isPodExternal)
                {
                    Renderer extwindowrenderer = this.part.FindModelComponent<Renderer>(windowname);
                    if (extwindowrenderer != null)
                    {
                        extwindowrenderer.material.shader = TransparentSpecularShader;
                        Color extsavedwindowcolor = extwindowrenderer.material.color;
                        extsavedwindowcolor.a = 0.3f;
                        extwindowrenderer.material.color = extsavedwindowcolor;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to setCryopodWindowTransparent seat " + seatIndx);
                Debug.Log("Err: " + ex);
            }
        }

        private void stopStripLightFlash(int seatIndx)
        {
            string stripname = "lightStrip-Animated-Cryopod-" + (seatIndx + 1).ToString();
            //this.Log_Debug("playing animation LightStrip " + stripname);
            try
            {
                Animation strip_animation = this.part.internalModel.FindModelComponent<Animation>(stripname);
                if (strip_animation != null)
                {
                    strip_animation.Stop();
                    strip_animation["LightStrip"].speed = 1;
                    strip_animation["LightStrip"].normalizedTime = 0;
                    strip_animation.wrapMode = WrapMode.Loop;
                    strip_animation.Play("LightStrip");
                }
                else
                {
                    this.Log_Debug("animation LightStrip not found for " + stripname);
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to run lightstrip animations in internal model for this part called " + stripname);
                Debug.Log("Err: " + ex);
            }
        }

        //This method sets the internal camera to the Freezer view prior to thawing or freezing a kerbal so we can see the nice animations.
        private void setIVAFrzrCam(int seatIndx)
        {
            string camname = "FrzCam" + (seatIndx + 1).ToString();
            //this.Log_Debug("Setting FrzrCam " + camname);
            Camera cam = this.part.internalModel.FindModelComponent<Camera>(camname);
            if (cam != null)  //Found Freezer Camera so switch to it.
            {
                Transform camxform = cam.transform;
                if (camxform != null)
                {
                    CameraManager.Instance.SetCameraInternal(this.part.internalModel, camxform);
                    DFIntMemory.Instance.lastFrzrCam = seatIndx;
                }
            }
            else  //Didn't find Freezer Camera so kick out to flight camera.
            {
                CameraManager.Instance.SetCameraMode(CameraManager.CameraMode.Flight);
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
            Events["eventOpenDoors"].active = false;
            Events["eventCloseDoors"].active = false;
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
            Events["eventOpenDoors"].active = false;
            Events["eventCloseDoors"].active = false;
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

        private DoorState getdoorState()
        {
            if (externalDoorAnim != null)
            {
                if (externalDoorAnim[animationName].normalizedTime == 1f) //closed
                {
                    Utilities.Log_Debug("getdoorState closed");
                    return DoorState.CLOSED;
                }
                if (externalDoorAnim[animationName].normalizedTime == 0f) //open
                {
                    Utilities.Log_Debug("getdoorState open");
                    return DoorState.OPEN;
                }
                Utilities.Log_Debug("getdoorState unknown");
                return DoorState.UNKNOWN;
            }
            else
            {
                Utilities.Log_Debug("getdoorState Animation not found");
                return DoorState.UNKNOWN;
            }
        }

        #endregion ExternalDoor

        #region BackgroundProcessing

        private const String MAIN_POWER_NAME = "ElectricCharge";

        //This method is called by the BackgroundProcessing DLL, if the user has installed it. Otherwise it will never be called.
        //It will consume ElectricCharge for Freezer that contain frozen kerbals for vessels that are unloaded, if the user has turned on the ECreqdForFreezer option in the settings menu.
        public static void FixedBackgroundUpdate(Vessel v, uint partFlightID, Func<Vessel, float, string, float> resourceRequest, ref System.Object data)
        {
            if (Time.timeSinceLevelLoad < 2.0f) // Check not loading level
            {
                return;
            }
            bool debug = true;
            try
            {
                debug = DeepFreeze.Instance.DFsettings.debugging;
            }
            catch
            {
                debug.Log("DeepFreeze FixedBackgroundUpdate failed to get debug setting");
            }
            if (debug) Debug.Log("FixedBackgroundUpdate vesselID " + v.id + " partID " + partFlightID);
            // If the user does not have ECreqdForFreezer option ON, then we do nothing and return
            if (!DeepFreeze.Instance.DFsettings.ECreqdForFreezer)
            {
                //if (debug) Debug.Log("FixedBackgroundUpdate ECreqdForFreezer is OFF, nothing to do");
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
                //if (debug) Debug.Log("FixedBackgroundUpdate No Frozen Crew on-board, nothing to do");
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
                        partInfo.ECWarning = false;
                        vslinfo.storedEC -= Ecrecvd;
                    }
                    else
                    {
                        if (debug) Debug.Log("FixedBackgroundUpdate DeepFreezer Ran out of EC to run the freezer");
                        if (!partInfo.ECWarning)
                        {
                            ScreenMessages.PostScreenMessage("Insufficient electric charge to monitor frozen kerbals.", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                            partInfo.ECWarning = true;
                            partInfo.deathCounter = currenttime;
                        }
                        ScreenMessages.RemoveMessage(OnGoingECMsg);
                        OnGoingECMsg = ScreenMessages.PostScreenMessage(" Freezer Out of EC : Systems critical in " + (deathRoll - (currenttime - partInfo.deathCounter)).ToString("######0") + " secs");
                        partInfo.outofEC = true;
                        if (debug) Debug.Log("FixedBackgroundUpdate deathCounter = " + partInfo.deathCounter);
                        if (currenttime - partInfo.deathCounter > deathRoll)
                        {
                            if (DeepFreeze.Instance.DFsettings.fatalOption)
                            {
                                if (debug) Debug.Log("FixedBackgroundUpdate deathRoll reached, Kerbals all die...");
                                partInfo.deathCounter = currenttime;
                                //all kerbals dies
                                var kerbalsToDelete = new List<string>();
                                foreach (KeyValuePair<string, KerbalInfo> kerbal in DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals)
                                {
                                    if (kerbal.Value.partID == partFlightID && kerbal.Value.vesselID == v.id && kerbal.Value.type != ProtoCrewMember.KerbalType.Tourist)
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
                            else //NON Fatal option - emergency thaw all kerbals.
                            {
                                // Cannot emergency thaw in background processing. It is expected that DeepFreezeGUI will pick up that EC has run out and prompt the user to switch to the vessel.
                                // When the user switches to the vessel the DeepFreezer partmodule will detect no EC is available and perform an emergency thaw procedure.
                                if (debug) Debug.Log("FixedBackgroundUpdate DeepFreezer - EC has run out non-fatal option");
                            }
                        }
                    }
                }
            }
            else  //Timewarp is too high
            {
                if (debug) Debug.Log("FixedBackgroundUpdate Timewarp is too high to backgroundprocess");
                partInfo.deathCounter = currenttime;
                partInfo.outofEC = false;
                partInfo.ECWarning = false;
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