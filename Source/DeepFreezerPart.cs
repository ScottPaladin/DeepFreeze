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
using DeepFreeze;
using RSTUtils;
using UnityEngine;
using Object = System.Object;
using Random = System.Random;
using KSP.Localization;

namespace DF
{
    [KSPModule("DeepFreeze Freezer Part")]
    public class DeepFreezer : PartModule, IResourceConsumer
    {
        private float lastUpdate;                  // time since we last updated the part menu
        private float lastRemove;                  // time since we last removed a part menu event
        private float updatetnterval = .5f;               // time between part menu updates
        internal static float updateECTempInterval = 2f;  // time between EC and Temp checks updates
        internal double deathCounter;                // time delay counter until the chance of a frozen kerbal dying due to lack of EC
        internal double tmpdeathCounter;             // time delay counter until the chance of a frozen kerbal dying due to part being too hot
        internal static float tmpdeathRoll = 120f;        // time delay until the chance of a frozen kerbal dying due to part being too hot
        internal static float deathRoll = 240f;           // time delay until the chance of a frozen kerbal dying due to lack of EC
        internal double timeLoadedOffrails;        // time the vessel this part is attached to was loaded or came off rails.
        private Transform External_Window_Occluder;
        private Transform CRY_0300_Doors_Occluder;

        // EC and Temp Functions Vars
        private Random rnd = new Random();  // Random seed for Killing Kerbals when we run out of EC to keep the Freezer running.

        private double heatamtMonitoringFrznKerbals = 5f;  //amount of heat generated when monitoring a frozen kerbal, can by overridden by DeepFreeze master settings
        private double heatamtThawFreezeKerbal = 50f;      //amount of heat generated when freezing or thawing a kerbal, can be overriddent by DeepFreeze master settings

        // Crew Transfer Vars
        public bool DFIcrewXferTOActive                   // Interface var for API = true if a Stock crewXfer to this part is active
        {
            get { return CrewHatchController.fetch.Active; }
        }
        public bool DFIcrewXferFROMActive                 //  Interface var for API = true if a Stock crewXfer from this part is active
        {
            get { return CrewHatchController.fetch.Active; }
        }
        private bool crewTransferInputLock = false;     //This is turned on if a stock Xfer is started and Off when it finishes.
        private List<Part> CrewMoveList = new List<Part>(); //Store temp list of Parts in Stock Crew Transfer.

        internal static ScreenMessage OnGoingECMsg, TempChkMsg;  // used for the bottom right screen messages, these ones are static because the background processor uses them.
        internal ScreenMessage ThawMsg, FreezeMsg, IVAKerbalName, IVAkerbalPart, IVAkerbalPod;  // used for the bottom right screen messages

        private bool RTlastKerbalFreezeWarn;     //  set to true if you are using RemoteTech and you attempt to freeze your last kerbal in active vessel

        [KSPField(isPersistant = true, guiActive = false, guiName = "Animated")] //Set to true if Internal contains Animated Cryopods, read from part.cfg.
        public bool isPartAnimated;

        [KSPField(isPersistant = true, guiActive = false, guiName = "PodExternal")] //Set to true if Cryopod is External part (eg. CRY-0300R), read from part.cfg.
        public bool isPodExternal = false;

        [KSPField(isPersistant = true, guiActive = true, guiName = "#autoLOC_DF_00054")] //Total Size of Freezer, get's read from part.cfg. #autoLOC_DF_00054 = Freezer Capacity
        public int FreezerSize;

        public int DFIFreezerSize
        {
            get
            {
                return FreezerSize;
            }
        }

        [KSPField(isPersistant = true, guiActive = true, guiName = "#autoLOC_DF_00055")] //WISOTT Total number of frozen kerbals, just a count of the list object. #autoLOC_DF_00055 = Total Frozen Kerbals
        public int TotalFrozen;

        public int DFITotalFrozen
        {
            get
            {
                return TotalFrozen;
            }
        }

        [KSPField(isPersistant = true, guiActive = true, guiName = "#autoLOC_DF_00056")] //Total space available for storage. Set by Part.cfg file. #autoLOC_DF_00056 = Freezer Space
        public int FreezerSpace;

        public int DFIFreezerSpace
        {
            get
            {
                return FreezerSpace;
            }
        }

        [KSPField(isPersistant = true, guiActive = true, guiName = "#autoLOC_DF_00057")] //Is set to true if the part is full (taking into account frozen kerbals in the part). #autoLOC_DF_00057 = Part is Full?
        public bool PartFull;

        public bool DFIPartFull
        {
            get
            {
                return PartFull;
            }
        }

        public bool DFIECReqd
        {
            get { return DeepFreeze.Instance.DFsettings.ECreqdForFreezer; }
        }

        [KSPField(isPersistant = false, guiName = "#autoLOC_DF_00058", guiActive = false)] //#autoLOC_DF_00058 = R/T Connection
        public bool isRTConnected;

        [KSPField(isPersistant = true, guiName = "#autoLOC_DF_00059", guiActive = true)] //#autoLOC_DF_00059 = Freezer Temp
        public FrzrTmpStatus _FrzrTmp = FrzrTmpStatus.OK;  // ok, warning and red alert flags for temperature monitoring of the freezer

        public FrzrTmpStatus DFIFrzrTmp                     //  Interface var for API = ok, warning and red alert flags for temperature monitoring of the freezer
        {
            get
            {
                return _FrzrTmp;
            }
        }

        internal FrzrTmpStatus DFFrzrTmp
        {
            get
            {
                return _FrzrTmp;
            }
            set
            {
                _FrzrTmp = value;
            }
        }

        [KSPField(isPersistant = true, guiName = "#autoLOC_DF_00060", guiUnits = "#autoLOC_DF_00061", guiFormat = "F1", guiActive = true)] //#autoLOC_DF_00060 = Cabin Temerature #autoLOC_DF_00061 = K
        public float CabinTemp;

        [KSPEvent(active = true, guiActive = true, name = "showMenu", guiName = "#autoLOC_DF_00062")] //#autoLOC_DF_00062 = DeepFreeze Menu
        public void showMenu()
        {
            DeepFreezeGUI obj = DeepFreeze.Instance.GetComponent("DeepFreezeGUI") as DeepFreezeGUI;
            if (obj != null)
                obj.DFMenuAppLToolBar.GuiVisible = !obj.DFMenuAppLToolBar.GuiVisible;
            else
                Utilities.Log("DeepFreezer ToggleMenu error");
        }

        [KSPField(isPersistant = true)]
        public float timeSinceLastECtaken; //This is the game time since EC was taken, for the ongoing EC usage while kerbal's are frozen

        [KSPField(isPersistant = true)]
        public float timeSinceLastTmpChk; //This is the game time since Temperature was checked, for the ongoing storage of frozen kerbal's

        [KSPField(isPersistant = true, guiName = "Freezer Out of EC", guiActive = true)]
        public bool _FreezerOutofEC;             // true if the freezer has run out of EC

        public bool DFIFreezerOutofEC                     //  Interface var for API = true if the freezer has run out of EC
        {
            get
            {
                return _FreezerOutofEC;
            }
        }

        internal bool DFFreezerOutofEC
        {
            get
            {
                return _FreezerOutofEC;
            }
            set
            {
                _FreezerOutofEC = value;
            }
        }

        [KSPField(isPersistant = false, guiName = "#autoLOC_DF_00063", guiUnits = "#autoLOC_DF_00064", guiActive = true)] //#autoLOC_DF_00063 = EC p/Kerbal to run #autoLOC_DF_00064 = \u0020p/min
        public Int32 FrznChargeRequired; //Set by part.cfg. Total EC value required to maintain a frozen kerbal per minute.

        public Int32 DFIFrznChargeRequired
        {
            get
            {
                return FrznChargeRequired;
            }
        }

        [KSPField(isPersistant = false, guiActive = true, guiName = "#autoLOC_DF_00065", guiUnits = "#autoLOC_DF_00066", guiFormat = "F3")] //#autoLOC_DF_00065 = Current EC Usage #autoLOC_DF_00066 = \u0020p/sec
        public float FrznChargeUsage;

        public float DFIFrznChargeUsage
        {
            get
            {
                return FrznChargeUsage;
            }
        }

        [KSPField(isPersistant = false, guiName = "#autoLOC_DF_00067", guiActive = true)] //#autoLOC_DF_00067 = Glykerol Reqd. to Freeze
        public Int32 GlykerolRequired; //Set by part.cfg. Total Glykerol value required to freeze a kerbal.

        [KSPField]                     // set to active while freezing a kerbal
        public bool IsFreezeActive;

        public bool DFIIsFreezeActive
        {
            get
            {
                return IsFreezeActive;
            }
        }

        [KSPField]                     // set to active while thawing a kerbal
        public bool IsThawActive;

        public bool DFIIsThawActive
        {
            get
            {
                return IsThawActive;
            }
        }

        [KSPField]
        public double StoredCharge;      // Stores up EC as we are freezing or thawing over time until we reach what we need.

        [KSPField(isPersistant = false, guiName = "#autoLOC_DF_00068", guiActive = true)] //#autoLOC_DF_00068 = EC p/Kerbal to Frze/Thaw
        public Int32 ChargeRequired; //Set by part.cfg. Total EC value required for a complete freeze or thaw.

        [KSPField(isPersistant = false)]
        public Int32 ChargeRate; //Set by part.cfg. EC draw per tick.

        [KSPField]
        public string animationName = string.Empty;  //Set by part.cfg. name of external animation name for doors if equipped.

        [KSPField]
        public bool PartHasDoor = false; //Set by part.cfg. true if part has external door (CRY-0300).

        private Animation externalDoorAnim;
        private Animation externalDoorAnimOccluder;

        [KSPField(isPersistant = true)]
        public bool ExternalDoorActive; //Set internal to partmodule. True if PartHasDoor and RPM/JSI TransparentPods is installed. Otherwise it is false.  Used to determine if the door is activated/enabled or not.

        //we persist the external door state in strings because KSP can't handle ENUMs
        [KSPField(isPersistant = true)]
        public string strexternaldoorstate = "CLOSED";

        internal DoorState _externaldoorstate = DoorState.CLOSED;

        [KSPField(isPersistant = true)]
        public string strprevexterndoorstate = "CLOSED";

        internal DoorState _prevexterndoorstate = DoorState.CLOSED;

        internal string _prevRPMTransparentpodSetting = string.Empty;

        

        [KSPField]
        public string transparentTransforms = string.Empty; //Set by part.cfg. contains list of transforms that should be transparent | separated.

        private bool hasJSITransparentPod;
        private bool checkRPMPodTransparencySettingError = false;
        private bool RPMPodOccluderProcessingError = false;

        [KSPEvent(active = false, guiActive = true, guiActiveUnfocused = true, guiActiveEditor = true, unfocusedRange = 5f, name = "eventOpenDoors", guiName = "Open Doors")]
        public void eventOpenDoors()
        {
            Events["eventOpenDoors"].active = false;
            Utilities.Log_Debug("eventOpenDoors triggered - open the bay doors Hal");
            try
            {
                Animation[] animators = part.internalModel.FindModelAnimators("DOORHandle");
                if (animators.Length > 0)
                {
                    var anim = animators[0];
                    anim["DOORHandle"].speed = float.MaxValue;
                    anim["DOORHandle"].normalizedTime = 0;
                    anim.Play("DOORHandle");
                }
                ext_door.Play();
            }
            catch (Exception ex)
            {
                Utilities.Log("Exception trying to run the Doorhandle animation");
                Utilities.Log("Err: " + ex);
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
            Utilities.Log_Debug("eventOpenDoors triggered - close the bay doors Hal");
            try
            {
                Animation[] animators = part.internalModel.FindModelAnimators("DOORHandle");
                if (animators.Length > 0)
                {
                    var anim = animators[0];
                    anim["DOORHandle"].speed = float.MinValue;
                    anim["DOORHandle"].normalizedTime = 1;
                    anim.Play("DOORHandle");
                }
                ext_door.Play();
            }
            catch (Exception ex)
            {
                Utilities.Log("Exception trying to run the Doorhandle animation");
                Utilities.Log("Err: " + ex);
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
        private int ToFrzeKerbalSeat;
        private string ToFrzeKerbalXformNme = "Unknown";
        private string ToThawKerbal = "";
        private int ToThawKerbalSeat;
        private bool skipThawStep1;
        private bool emergencyThawInProgress;
        private bool OpenPodAnimPlaying;
        private bool ClosePodAnimPlaying;
        private bool ThawWindowAnimPlaying;
        private bool FreezeWindowAnimPlaying;
        private int ThawStepInProgress;
        private int FreezeStepInProgress;
        private Animation _animation;
        private Animation _windowAnimation;
        private Shader TransparentSpecularShader;
        private Shader KSPSpecularShader;
        private object JSITransparentPodModule;

        private FrznCrewList _StoredCrewList = new FrznCrewList(); // This is the frozen StoredCrewList for the part

        public FrznCrewList DFIStoredCrewList                      //  Interface var for API = This is the frozen StoredCrewList for the part
        {
            get
            {
                return _StoredCrewList;
            }
        }

        //Various Vars about the part and the vessel it is attached to
        private string Glykerol = "Glykerol";

        private string EC = "ElectricCharge";
        private Guid CrntVslID;
        private uint CrntPartID;
        private string CrntVslName = "";
        private bool vesselisinIVA;
        private bool vesselisinInternal;
        private int internalSeatIdx;

        private bool setGameSettings;

        internal bool partHasInternals;
        private bool partHasStripLights;
        private bool onvslchgInternal;  //set to true if a VesselChange game event is triggered by this module
        private bool onvslchgExternal;  //set to true if a VesselChange game event is triggered outside of this module
        private bool onvslchgNotActive; //sets a timer count started when external VesselChange game event is triggered before resetting cryopod and extdoor animations.
        private float onvslchgNotActiveDelay; // timer as per previous var
        private double ResAvail;

        [KSPField(isPersistant = true)]  //we keep the game time the last cryopod reset occured here and only run if the last one was longer than cryopodResetTimeDelay ago.
        private double cryopodResetTime;

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

        public override string GetInfo()
        {
            string text = string.Empty;
            text += Localizer.Format("#autoLOC_DF_00069", FreezerSize); //#autoLOC_DF_00069 = \nCryopods: <<1>>

            return text;
        }

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

            //For some reason when we go on EVA or switch vessels the InternalModel is destroyed.
            //Which causes a problem when we re-board the part as the re-boarding kerbal ends up in a frozen kerbals seat.
            //So we check for the internalmodel existing while the vessel this part is attached to is loaded and if it isn't we re-instansiate it.
            if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && vessel.loaded && partHasInternals && part.internalModel == null)
            {
                Utilities.Log("Part " + part.name + "(" + part.flightID + ") is loaded and internalModel has disappeared, so re-instantiate it");
                //part.SpawnIVA();
                Utilities.spawnInternal(part);
                resetFrozenKerbals();
                resetCryopods(true); 
                if (vesselisinInternal)
                {
                    setIVAFrzrCam(internalSeatIdx);
                }
            }

            // If we have an external door (CRY-0300) or external pod (CRY-0300R) check RPM transparency setting and change the door settings as appropriate
            if ((PartHasDoor || isPodExternal) && DFInstalledMods.IsJSITransparentPodsInstalled && !IsFreezeActive && !IsThawActive)
            {
                try
                {
                    checkRPMPodTransparencySetting();
                }
                catch (Exception ex)
                {
                    Utilities.Log("Exception attempting to check RPM transparency settings. Report this error on the Forum Thread.");
                    Utilities.Log("Err: " + ex);
                }
            }

            //We have to process the occluders for the CRY-0300 and CRY-0300R for KSP 1.1
            if (PartHasDoor || isPodExternal)
            {
                if (DFInstalledMods.IsJSITransparentPodsInstalled)
                {
                    RPMPodOccluderProcessing();
                }
                else
                {
                    noRPMPodOccluderProcessing();
                }
            }

            if (!HighLogic.LoadedSceneIsFlight) // If scene is not flight we are done with onUpdate
                return;

            //This is necessary to override stock crew xfer behaviour. When the user cancels the xfer the Stock highlighting system
            // it makes the transparent pod opaque. 
            // so when a Transfer dialog is started we set crewTranferInputLock to true.
            // Here we check if it is true and the crewhatchcontroller is not active the user must have cancelled and we need
            // to reset the pod to transparent.
            // If they complete the transfer then the bool will be turned off by those gameevents and we don't have to reset the pod.
            if (crewTransferInputLock && !CrewHatchController.fetch.Active)
            {
                crewTransferInputLock = false;  //Reset the bool and then process.
                if (FlightGlobals.ready && vessel.loaded && isPodExternal && !IsFreezeActive && !IsThawActive && DFInstalledMods.IsJSITransparentPodsInstalled)
                {
                    if (_prevRPMTransparentpodSetting == "ON")
                    {
                        for (int i = 0; i < cryopodstateclosed.Length; i++)
                        {
                            if (!cryopodstateclosed[i])  //If there isn't a frozen kerbal inside
                            {
                                string windowname = "Cryopod-" + (i + 1) + "-Window";
                                Renderer extwindowrenderer = part.FindModelComponent<Renderer>(windowname);
                                if (extwindowrenderer != null)
                                {
                                    if (extwindowrenderer.material.shader != TransparentSpecularShader)
                                    {
                                        setCryopodWindowTransparent(i);
                                        External_Window_Occluder = Utilities.SetInternalDepthMask(part, false, "External_Window_Occluder", External_Window_Occluder);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (PartHasDoor && Utilities.IsInIVA)  //So if we are in IVA mode (inside this loop) and in a CRY-0300 (have External door) Open the Kerbals EYES WIDER!
            {
                //Kerbal currentKerbal = CameraManager.Instance.IVACameraActiveKerbal;
                CameraManager.Instance.IVACameraActiveKerbal.kerbalCam.nearClipPlane = 0.01f;
                CameraManager.Instance.IVACameraActiveKerbal.kerbalCam.fieldOfView = 95f;
                //CameraManager.Instance.ivaFOV = 95f;
                //Turn OFF the Door occluders so we can see outside.
                if (_externaldoorstate != DoorState.CLOSED)
                    CRY_0300_Doors_Occluder = Utilities.SetInternalDepthMask(part, false, "CRY_0300_Doors_Occluder", CRY_0300_Doors_Occluder);
            }

            if (Time.time - lastUpdate > updatetnterval && Time.time - lastRemove > updatetnterval) // We only update every updattnterval time interval.
            {
                lastUpdate = Time.time;
                if (FlightGlobals.ready && FlightGlobals.ActiveVessel != null)
                {
                    CrntVslID = vessel.id;
                    CrntVslName = vessel.vesselName;

                    //Set the Part temperature in the partmenu
                    if (DeepFreeze.Instance.DFsettings.TempinKelvin)
                    {
                        Fields["CabinTemp"].guiUnits = Localizer.Format("#autoLOC_DF_00061"); //#autoLOC_DF_00061 = K
                        CabinTemp = (float)part.temperature;
                    }
                    else
                    {
                        Fields["CabinTemp"].guiUnits = Localizer.Format("#autoLOC_DF_00070"); //#autoLOC_DF_00070 = C
                        CabinTemp = Utilities.KelvintoCelsius((float)part.temperature);
                    }

                    // If RemoteTech installed set the connection status
                    if (DFInstalledMods.IsRTInstalled)
                    {
                        try
                        {
                            if (DFInstalledMods.RTVesselConnected(part.vessel.id))
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
                            Utilities.Log("Exception attempting to check RemoteTech vessel connections. Report this error on the Forum Thread.");
                            Utilities.Log("Err: " + ex);
                            isRTConnected = false;
                        }
                    }

                    // If we have an external door (CRY-0300) check if the door state has changed and then set the helmet state
                    if (ExternalDoorActive)
                    {
                        // if the current and previous door states are different we need to do checks, otherwise we do nothing.
                        if (_externaldoorstate != _prevexterndoorstate)
                        {
                            // if the previous state was closing and now it's closed we can take our helmets off.
                            if (_prevexterndoorstate == DoorState.CLOSING || _externaldoorstate == DoorState.CLOSED)
                            {
                                part.setHelmets(false);
                            }
                            else // all other door states we keep our helmets on.
                            {
                                part.setHelmets(true);
                            }
                            _prevexterndoorstate = _externaldoorstate;
                        }
                    }
                    
                    //Refresh IVA mode Messages and Bools
                    if (IVAKerbalName != null) ScreenMessages.RemoveMessage(IVAKerbalName);
                    if (IVAkerbalPart != null) ScreenMessages.RemoveMessage(IVAkerbalPart);
                    if (IVAkerbalPod != null) ScreenMessages.RemoveMessage(IVAkerbalPod);
                    if (Utilities.VesselIsInIVA(part.vessel))
                    {
                        // Utilities.Log_Debug("Vessel is in IVA mode");
                        vesselisinIVA = true;
                        vesselisinInternal = false;
                        //Kerbal actkerbal = CameraManager.Instance.IVACameraActiveKerbal;
                        if (CameraManager.Instance.IVACameraActiveKerbal != null)
                        {
                            if (Utilities.ActiveKerbalIsLocal(part))
                            {
                                ProtoCrewMember crew = null;
                                List<ProtoCrewMember>.Enumerator enumerator = part.protoModuleCrew.GetEnumerator();
                                while (enumerator.MoveNext())
                                {
                                    if (enumerator.Current.name == CameraManager.Instance.IVACameraActiveKerbal.name)
                                        crew = enumerator.Current;
                                }
                                int SeatIndx = -1;
                                if (crew != null)
                                {
                                    SeatIndx = crew.seatIdx;
                                }
                                // Utilities.Log_Debug("ActiveKerbalFound, seatidx=" + SeatIndx);
                                if (SeatIndx != -1)
                                {
                                    SeatIndx++;
                                    IVAkerbalPod = new ScreenMessage(Localizer.Format("#autoLOC_DF_00071", SeatIndx), 1, ScreenMessageStyle.UPPER_LEFT); //#autoLOC_DF_00071 = Pod:<<1>>
                                    IVAkerbalPod.color = Color.white;
                                    ScreenMessages.PostScreenMessage(IVAkerbalPod);
                                }
                                IVAkerbalPart = new ScreenMessage(part.name.Substring(0, 8), 1, ScreenMessageStyle.UPPER_LEFT);
                                IVAkerbalPart.color = Color.white;
                                ScreenMessages.PostScreenMessage(IVAkerbalPart);

                                IVAKerbalName = new ScreenMessage(CameraManager.Instance.IVACameraActiveKerbal.crewMemberName, 1, ScreenMessageStyle.UPPER_LEFT);
                                IVAKerbalName.color = Color.white;
                                ScreenMessages.PostScreenMessage(IVAKerbalName);
                            }
                        }
                        //monitoring beep
                        if (TotalFrozen > 0 && !mon_beep.isPlaying)
                        {
                            mon_beep.Play();
                        }
                    }
                    else
                    {
                        if (IVAKerbalName != null) ScreenMessages.RemoveMessage(IVAKerbalName);
                        if (IVAkerbalPart != null) ScreenMessages.RemoveMessage(IVAkerbalPart);
                        if (IVAkerbalPod != null) ScreenMessages.RemoveMessage(IVAkerbalPod);
                        vesselisinIVA = false;
                        // Utilities.Log_Debug("Vessel is NOT in IVA mode");
                        if (Utilities.IsActiveVessel(vessel) && Utilities.IsInInternal)
                        {
                            vesselisinInternal = true;
                            if (TotalFrozen > 0 && !mon_beep.isPlaying)
                            {
                                mon_beep.Play();
                            }
                            // Utilities.Log_Debug("Vessel is in Internal mode");
                        }
                        else
                        {
                            vesselisinInternal = false;
                            if (mon_beep.isPlaying)
                            {
                                mon_beep.Stop();
                            }
                            // Utilities.Log_Debug("Vessel is NOT in Internal mode");
                        }
                    }
                    /*
                    // If we have Crew Xfers in progress then check and process to completion.
                    if (_crewXferFROMActive || _crewXferTOActive)
                    {
                        completeCrewTransferProcessing();
                    }
                    */
                    UpdateEvents(); // Update the Freeze/Thaw Events that are attached to this Part.
                }
            }
            //UpdateCounts(); // Update the Kerbal counters and stored crew lists for the part - MOVED to FixedUpdate
        }

        private void checkRPMPodTransparencySetting()
        {
            try
            {
                //Get the TransparentPodPartModule and only proceed if we find one.
                JSITransparentPodModule = part.Modules["JSIAdvTransparentPod"];
                if (JSITransparentPodModule != null)
                {
                    //Get the transparentPodSetting (ON/OFF/AUTO) and only proceed if we got it.
                    object outputField = Utilities.GetObjectField(JSITransparentPodModule, "transparentPodSetting");
                    if (outputField != null)
                    {
                        string transparentPodSetting = outputField.ToString();
                        //We only need to do this processing if the Pod Setting has CHANGED since last time we checked.
                        if (transparentPodSetting != _prevRPMTransparentpodSetting)
                        {
                            if (PartHasDoor) //CRY-0300
                            {
                                processRPMPodSettingsPartHasDoor(transparentPodSetting);
                            }
                            else
                            {
                                if (isPodExternal) //CRY-0300R
                                {
                                    processRPMPodSettingsPodisExternal(transparentPodSetting);
                                }
                            }
                            _prevRPMTransparentpodSetting = transparentPodSetting;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!checkRPMPodTransparencySettingError)
                {
                    Utilities.Log("DeepFreezer Error checking RPM TransparentPod Setting");
                    Utilities.Log("DeepFreezer ", ex.Message);
                    checkRPMPodTransparencySettingError = true;
                }
            }
        }

        private void processRPMPodSettingsPartHasDoor(string transparentPodSetting)
        {
            switch (transparentPodSetting)
            {
                case "ON":
                    // If the doors are closed or closing set open doors event active
                    if (_externaldoorstate != DoorState.CLOSED &&
                        _externaldoorstate != DoorState.CLOSING)
                    {
                        Events["eventOpenDoors"].active = false;
                        Events["eventCloseDoors"].active = true;
                    }
                    else
                    {
                        //If the doors are open or opening set close doors event active
                        if (_externaldoorstate != DoorState.OPEN &&
                            _externaldoorstate != DoorState.OPENING)
                        {
                            Events["eventOpenDoors"].active = true;
                            Events["eventCloseDoors"].active = false;
                        }
                    }
                    break;

                default:
                    Utilities.Log_Debug("RPM set to OFF or AUTO for transparent pod");
                    
                    // We must close the doors if they are not or we see an empty internal.
                    DoorState actualDoorState = getdoorState();
                    if (actualDoorState != DoorState.CLOSED)
                    {
                        try
                        {
                        //Animate the RPM Door Handle Prop
                        Animation anim;
                            Animation[] animators =
                                part.internalModel.FindModelAnimators("DOORHandle");
                            if (animators.Length > 0)
                            {
                                anim = animators[0];
                                anim["DOORHandle"].speed = float.MinValue;
                                anim["DOORHandle"].normalizedTime = 1;
                                anim.Play("DOORHandle");
                            }
                            //ext_door.Play();
                        }
                        catch (Exception ex)
                        {
                            Debug.Log("Exception trying to run the Doorhandle animation");
                            Debug.Log("Err: " + ex);
                        }

                        //Run the Close Door Animation.
                        if (animationName != null)
                        {
                            if (externalDoorAnimOccluder != null)
                            {
                                externalDoorAnimOccluder[animationName].normalizedTime = 1;
                                externalDoorAnimOccluder[animationName].speed = float.MinValue;
                                externalDoorAnimOccluder.Play(animationName);
                            }
                            externalDoorAnim[animationName].normalizedTime = 1;
                            externalDoorAnim[animationName].speed = float.MinValue;
                            externalDoorAnim.Play(animationName);
                        }
                        _prevexterndoorstate = _externaldoorstate;
                        _externaldoorstate = DoorState.CLOSED;
                    }
                    Events["eventOpenDoors"].active = false;
                    Events["eventCloseDoors"].active = false;
                    
                    break;
            }
        }

        private void processRPMPodSettingsPodisExternal(string transparentPodSetting)
        {
            switch (transparentPodSetting)
            {
                case "ON":
                    
                    for (int i = 0; i < FreezerSize; i++)
                    {
                        string windowname = "Cryopod-" + (i + 1) + "-Window";
                        Renderer extwindowrenderer = part.FindModelComponent<Renderer>(windowname);
                        if (extwindowrenderer != null)
                        {
                            if (HighLogic.LoadedSceneIsFlight)
                            //If in flight, we check the pod state
                            {
                                if (!cryopodstateclosed[i]) //No frozen kerbal inside
                                {
                                    if (extwindowrenderer.material.shader != TransparentSpecularShader)
                                    {
                                        setCryopodWindowTransparent(i);
                                    }
                                }
                                else //Frozen kerbal inside
                                {
                                    if (extwindowrenderer.material.shader != KSPSpecularShader)
                                    {
                                        setCryopodWindowSpecular(i);
                                    }
                                }
                            }
                            else //If in editor, always transparent
                            {
                                if (extwindowrenderer.material.shader != TransparentSpecularShader)
                                {
                                    setCryopodWindowTransparent(i);
                                }
                            }
                        }
                    }
                    break;

                default:
                    Utilities.Log_Debug("RPM set to OFF or AUTO for transparent pod");
                    
                    for (int i = 0; i < FreezerSize; i++)
                    {
                        string windowname = "Cryopod-" + (i + 1) + "-Window";
                        Renderer extwindowrenderer = part.FindModelComponent<Renderer>(windowname);

                        if (extwindowrenderer != null)
                        {
                            if (extwindowrenderer.material.shader != KSPSpecularShader)
                            {
                                setCryopodWindowSpecular(i);
                            }
                        }
                    }
                    break;
            }
        }

        private void RPMPodOccluderProcessing()
        {
            try
            {
                //We have to process the occluders for the CRY-0300 and CRY-0300R for KSP 1.1
                if (HighLogic.LoadedSceneIsFlight && !Utilities.IsInIVA)
                {
                    //Get the TransparentPodPartModule and only proceed if we find one.
                    JSITransparentPodModule = part.Modules["JSIAdvTransparentPod"];
                    if (JSITransparentPodModule != null)
                    {
                        //Get the transparentPodSetting (ON/OFF/AUTO) and only proceed if we got it.
                        object outputField = Utilities.GetObjectField(JSITransparentPodModule, "transparentPodSetting");
                        if (outputField != null)
                        {
                            string transparentPodSetting = outputField.ToString();
                            if (PartHasDoor) //If CRY-0300, set the door occluder ON so we see the closed/closing doors
                            {
                                if (transparentPodSetting == "ON")
                                {
                                    if (_externaldoorstate == DoorState.CLOSED)// ||
                                        //_externaldoorstate == DoorState.CLOSING ||
                                        //_externaldoorstate == DoorState.OPENING)
                                        //If the Door is Closed, closing or opening
                                    {
                                        //If Stock Overlay is on we turn the Occluder OFF so we can see inside.
                                        //Otherwise is it ON and we see the closed/closing/opening doors.
                                        if (Utilities.StockOverlayCamIsOn)
                                        {
                                            CRY_0300_Doors_Occluder = Utilities.SetInternalDepthMask(part, false, "CRY_0300_Doors_Occluder", CRY_0300_Doors_Occluder);
                                        }
                                        else
                                        {
                                            CRY_0300_Doors_Occluder = Utilities.SetInternalDepthMask(part, true, "CRY_0300_Doors_Occluder", CRY_0300_Doors_Occluder);
                                        }
                                    }
                                    else
                                    //Door is Open we turn the Occluder OFF so we can see inside RPM style or Stock style, doesn't matter.
                                    {
                                        CRY_0300_Doors_Occluder = Utilities.SetInternalDepthMask(part, false, "CRY_0300_Doors_Occluder", CRY_0300_Doors_Occluder);
                                    }
                                }
                                else //Podsetting is OFF or AUTO
                                {
                                    //If pod is OFF or AUTO the Doors are CLOSED so if Stock Overlay is on we turn the overlay OFF so we can see inside
                                    //This will be ok and the Internal should be there because when Stock Overlay is on TransparentPods turns itself OFF
                                    if (Utilities.StockOverlayCamIsOn)
                                    {
                                        External_Window_Occluder = Utilities.SetInternalDepthMask(part, false, "External_Window_Occluder", External_Window_Occluder);
                                    }
                                    else
                                    //So Stock Overlay is off we want to see the CLOSED Doors so overlay is ON so we see the doors.
                                    {
                                        External_Window_Occluder = Utilities.SetInternalDepthMask(part, true, "External_Window_Occluder", External_Window_Occluder);
                                    }
                                }

                            }
                            else //CRY-0300R
                            {
                                //The CRY-0300R is a bit backwards. Occluders when ON will SHOW the external part.
                                //So in the case of the CRY-0300R we want to SHOW the external PART when there is a frozen Kerbal only.
                                //But don't cycle the Occluder if a freeze or thaw is running otherwise we interrupt the nice freeze/thaw
                                //effect on the window glass.
                                if (IsThawActive || IsFreezeActive)
                                    return;
                                if (!cryopodstateclosed[0] && transparentPodSetting == "ON") //No frozen kerbal inside
                                {
                                    External_Window_Occluder = Utilities.SetInternalDepthMask(part, false, "External_Window_Occluder", External_Window_Occluder);

                                }
                                else //Frozen kerbal inside or OFF or AUTO model.
                                {
                                    External_Window_Occluder = Utilities.SetInternalDepthMask(part, true, "External_Window_Occluder", External_Window_Occluder);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!RPMPodOccluderProcessingError)
                {
                    Utilities.Log("DeepFreezer Error setting RPM Occluders");
                    Utilities.Log("DeepFreezer ", ex.Message);
                    RPMPodOccluderProcessingError = true;
                }
            }
        }

        private void noRPMPodOccluderProcessing()
        {
            try
            {
                //If RPM is not installed we need to set the depthshader occluders for the stock overlay processing.
                //We have to process the occluders for the CRY-0300 and CRY-0300R for KSP 1.1
                if (HighLogic.LoadedSceneIsFlight && !Utilities.IsInIVA)
                {
                    if (PartHasDoor)  //If CRY-0300, set the door occluder ON so we see the closed/closing doors
                    {
                        //If Stock Overlay is on we turn the Occluder OFF so we can see inside.
                        //Otherwise is it ON.
                        if (Utilities.StockOverlayCamIsOn)
                        {
                            CRY_0300_Doors_Occluder = Utilities.SetInternalDepthMask(part, false, "CRY_0300_Doors_Occluder", CRY_0300_Doors_Occluder);
                        }
                        else
                        {
                            CRY_0300_Doors_Occluder = Utilities.SetInternalDepthMask(part, true, "CRY_0300_Doors_Occluder", CRY_0300_Doors_Occluder);
                        }
                    }
                    else //CRY-0300R
                    {
                        //The CRY-0300R is a bit backwards. Occluders when ON will SHOW the external part.
                        //So in the case of the CRY-0300R we want to SHOW the external PART when there is a frozen Kerbal only.
                        //But don't cycle the Occluder if a freeze or thaw is running otherwise we interrupt the nice freeze/thaw
                        //effect on the window glass.
                        
                        if (!cryopodstateclosed[0] && (!IsThawActive || !IsFreezeActive)) //No frozen kerbal inside
                        {
                            External_Window_Occluder = Utilities.SetInternalDepthMask(part, false, "External_Window_Occluder", External_Window_Occluder);

                        }
                        else  //Frozen kerbal inside or freeze/thaw occuring. Can't see inside.
                        {
                            External_Window_Occluder = Utilities.SetInternalDepthMask(part, true, "External_Window_Occluder", External_Window_Occluder);
                        }
                    }
                }
            }
            catch (Exception)
            {
                Utilities.Log("DeepFreezer Error setting no RPM Occluders");
                //Utilities.Log("DeepFreezer ", ex.Message);
            }
        }

        private void onceoffSetup()
        {
            Utilities.Log_Debug("DeepFreezer OnUpdate onceoffSetup");
            _StoredCrewList.Clear();
            CrntVslID = vessel.id;
            CrntVslName = vessel.vesselName;
            CrntPartID = part.flightID;
            lastUpdate = Time.time;
            lastRemove = Time.time;
            if (DeepFreeze.Instance == null)
            {
                Utilities.Log("DeepFreezer Onceoffsetup - waiting for DeepFreeze settings instance");
                setGameSettings = false;
                return;
            }
            
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
            if (DeepFreeze.Instance.DFgameSettings.knownFreezerParts.TryGetValue(part.flightID, out partInfo))
            {
                timeSinceLastECtaken = (float)partInfo.timeLastElectricity;
                timeSinceLastTmpChk = (float)partInfo.timeLastTempCheck;
            }
            Utilities.Log_Debug("DeepFreezer This CrntVslID = " + CrntVslID + " This CrntPartID = " + CrntPartID + " This CrntVslName = " + CrntVslName);

            // Set a flag if this part has internals or not. If it doesn't we don't try to save/restore specific seats for the frozen kerbals
            if (part.partInfo.internalConfig.HasData)
            {
                partHasInternals = true;
            }
            else
            {
                partHasInternals = false;
            }
            //For some reason when we go on EVA or switch vessels the InternalModel is destroyed.
            //Which causes a problem when we re-board the part as the re-boarding kerbal ends up in a frozen kerbals seat.
            //So we check for the internalmodel existing while the vessel this part is attached to is loaded and if it isn't we re-instansiate it.
            if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && vessel.loaded && partHasInternals && part.internalModel == null)
            {
                Utilities.Log("Part " + part.name + "(" + part.flightID + ") is loaded and internalModel has disappeared, so re-instantiate it");
                Utilities.spawnInternal(part);
            }

            resetFrozenKerbals();

            Utilities.Log_Debug("Onceoffsetup resetcryopod doors");
            if (partHasInternals)
            {
                resetCryopods(true);
            }

            if (part.Modules.Contains("JSIAdvTransparentPod"))
            {
                hasJSITransparentPod = true;
            }

            //For all thawed crew in part, change their IVA animations to be less well.. animated?
            foreach (ProtoCrewMember crew in part.protoModuleCrew)
            {
                if (crew.KerbalRef != null)
                {
                    Utilities.subdueIVAKerbalAnimations(crew.KerbalRef);
                }
            }

            //If we have an external door (CRY-0300) enabled set the current door state and the helmet states
            if (ExternalDoorActive)
            {
                setHelmetstoDoorState();
            }
            //If we have lightstrips (CRY-5000) set them up
            if (partHasInternals)
            {
                try
                {
                    Animation[] animators = part.internalModel.FindModelAnimators("LightStrip");
                    if (animators.Length > 0)
                    {
                        Utilities.Log_Debug("Found " + animators.Length + " LightStrip animations starting");
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
                    Utilities.Log("DeepFreezer Error finding Internal LightStrip Animators");
                    Utilities.Log(ex.Message);
                }
            }
            setGameSettings = true; //set the flag so this method doesn't execute a second time
        }

        private void FixedUpdate()
        {
            if (Time.timeSinceLevelLoad < 2.0f || !setGameSettings) // Check not loading level
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
                    Utilities.Log_Debug("Emergency Thaw completed");
                }
            }

            // The following section is the on-going EC check and temperature checks and update the seat counts for the freezer, only in flight and activevessel
            if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && vessel.isActiveVessel && setGameSettings)
            {
                if (DeepFreeze.Instance.DFsettings.ECreqdForFreezer)
                {
                    Fields["FrznChargeRequired"].guiActive = true;
                    Fields["FrznChargeUsage"].guiActive = true;
                    Fields["_FreezerOutofEC"].guiActive = true;
                    if (Utilities.timewarpIsValid(5))  // EC usage and generation still seems to work up to warpfactor of 4.
                    {
                        PartInfo partInfo;
                        if (!DeepFreeze.Instance.DFgameSettings.knownFreezerParts.TryGetValue(part.flightID, out partInfo))
                        {
                            Utilities.Log("Freezer Part NOT found: " + part.name + "(" + part.flightID + ")" + " (" + vessel.id + ")");
                            partInfo = new PartInfo(vessel.id, part.name, Planetarium.GetUniversalTime());
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
                    Fields["FrznChargeRequired"].guiActive = false;
                    Fields["FrznChargeUsage"].guiActive = false;
                    Fields["_FreezerOutofEC"].guiActive = false;
                    timeSinceLastECtaken = (float)Planetarium.GetUniversalTime();
                }

                if (DeepFreeze.Instance.DFsettings.RegTempReqd)
                {
                    Fields["_FrzrTmp"].guiActive = true;
                    if (Utilities.timewarpIsValid(2)) // Temperature is buggy in timewarp so it is disabled whenever timewarp is on.
                    {
                        PartInfo partInfo;
                        if (!DeepFreeze.Instance.DFgameSettings.knownFreezerParts.TryGetValue(part.flightID, out partInfo))
                        {
                            Utilities.Log("Freezer Part NOT found: " + part.name + "(" + part.flightID + ")" + " (" + vessel.id + ")");
                            partInfo = new PartInfo(vessel.id, part.name, Planetarium.GetUniversalTime());
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
                    Fields["_FrzrTmp"].guiActive = false;
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
            double timeperiod = currenttime - timeSinceLastECtaken;
            // Utilities.Log_Debug("currenttime = " + currenttime + " timeperiod = " + timeperiod + " updateECTempInterval= " + updateECTempInterval);
            if (timeperiod > updateECTempInterval) //only update every updateECTempInterval to avoid request resource bug when amounts are too small
            {
                if (TotalFrozen > 0 && !CheatOptions.InfiniteElectricity) //We have frozen Kerbals, consume EC
                {                    
                    double ECreqd = FrznChargeRequired / 60.0f * timeperiod * TotalFrozen;
                    Utilities.Log_Debug("DeepFreezer Running the freezer parms currenttime = {0} timeperiod = {1} ecreqd = {2}" , currenttime.ToString(), timeperiod.ToString(), ECreqd.ToString());
                    double resTotal = 0f;
                    if (Utilities.requireResource(vessel, EC, ECreqd, false, true, false, out ResAvail, out resTotal))
                    {
                        if (OnGoingECMsg != null) ScreenMessages.RemoveMessage(OnGoingECMsg);
                        //Have resource
                        Utilities.requireResource(vessel, EC, ECreqd, true, true, false, out ResAvail, out resTotal);
                        FrznChargeUsage = (float)ResAvail;
                        Utilities.Log_Debug("DeepFreezer Consumed Freezer EC " + ECreqd + " units");
                        timeSinceLastECtaken = (float)currenttime;
                        deathCounter = currenttime;
                        _FreezerOutofEC = false;
                        partInfo.ECWarning = false;
                    }
                    else
                    {
                        if (currenttime - timeLoadedOffrails < 5.0f) // this is true if vessel just loaded or we just switched to this vessel or vessel just came off rails
                                              // we need to check if we aren't going to exhaust all EC in one call.. and???
                        {
                            ECreqd = resTotal * 95 / 100;
                            double ECtotal = 0f;
                            if (Utilities.requireResource(vessel, EC, ECreqd, false, true, false, out ResAvail, out resTotal))
                            {
                                Utilities.requireResource(vessel, EC, ECreqd, true, true, false, out ResAvail, out resTotal);
                                FrznChargeUsage = (float)ResAvail;
                                timeSinceLastECtaken = (float)currenttime;
                                deathCounter = currenttime;
                            }
                        }
                        //Debug.Log("DeepFreezer Ran out of EC to run the freezer");
                        if (!partInfo.ECWarning)
                        {
                            if (TimeWarp.CurrentRateIndex > 1) Utilities.stopWarp();
                            ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00072"), 10.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00072 = Insufficient electric charge to monitor frozen kerbals.
                            partInfo.ECWarning = true;
                            deathCounter = currenttime;
                        }
                        if (OnGoingECMsg != null) ScreenMessages.RemoveMessage(OnGoingECMsg);
                        OnGoingECMsg = ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00073", (deathRoll - (currenttime - deathCounter)).ToString("######0"))); //#autoLOC_DF_00073 = \u0020Freezer Out of EC : Systems critical in <<1>> secs
                        _FreezerOutofEC = true;
                        FrznChargeUsage = 0f;
                        Utilities.Log_Debug("DeepFreezer deathCounter = " + deathCounter);
                        if (currenttime - deathCounter > deathRoll)
                        {
                            if (DeepFreeze.Instance.DFsettings.fatalOption)
                            {
                                Utilities.Log_Debug("DeepFreezer deathRoll reached, Kerbals all die...");
                                deathCounter = currenttime;
                                //all kerbals die
                                var kerbalsToDelete = new List<FrznCrewMbr>();
                                foreach (FrznCrewMbr deathKerbal in _StoredCrewList)
                                {
                                    DeepFreeze.Instance.KillFrozenCrew(deathKerbal.CrewName);
                                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00074", deathKerbal.CrewName), 10.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00074 = <<1>> died due to lack of Electrical Charge to run cryogenics
                                    Utilities.Log("DeepFreezer - kerbal " + deathKerbal.CrewName + " died due to lack of Electrical charge to run cryogenics");
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
                                Utilities.Log_Debug("DeepFreezer deathRoll reached, Kerbals all don't die... They just Thaw out...");
                                deathCounter = currenttime;
                                //all kerbals thaw out
                                emergencyThawInProgress = true;  //This will trigger FixedUpdate to thaw all frozen kerbals in the part, one by one.
                            }
                        }
                    }
                }
                else  // no frozen kerbals, so just update last time EC checked
                {
                    if (CheatOptions.InfiniteElectricity)
                    {
                        Utilities.Log_Debug("Infinite EC cheat on");
                    }
                    else
                    {
                        Utilities.Log_Debug("No frozen kerbals for EC consumption in part " + part.name);
                    }
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
            double timeperiod = currenttime - timeSinceLastTmpChk;
            //Utilities.Log_Debug("ChkOngoingTemp start time=" + Time.time.ToString() + ",timeSinceLastTmpChk=" + timeSinceLastTmpChk.ToString() + ",Planetarium.UniversalTime=" + Planetarium.GetUniversalTime().ToString() + " timeperiod=" + timeperiod.ToString());
            if (timeperiod > updateECTempInterval) //only update every udpateECTempInterval to avoid request resource bug when amounts are too small
            {
                if (TotalFrozen > 0) //We have frozen Kerbals, generate and check heat
                {
                    //Add Heat for equipment monitoring frozen kerbals
                    double heatamt = heatamtMonitoringFrznKerbals / 60.0f * timeperiod * TotalFrozen;
                    if (heatamt > 0) part.AddThermalFlux(heatamt);
                    Utilities.Log_Debug("Added " + heatamt + " kW of heat for monitoring " + TotalFrozen + " frozen kerbals");
                    if (part.temperature < DeepFreeze.Instance.DFsettings.RegTempMonitor)
                    {
                        Utilities.Log_Debug("DeepFreezer Temperature check is good parttemp=" + part.temperature + ",MaxTemp=" + DeepFreeze.Instance.DFsettings.RegTempMonitor);
                        if (TempChkMsg != null) ScreenMessages.RemoveMessage(TempChkMsg);
                        _FrzrTmp = FrzrTmpStatus.OK;
                        tmpdeathCounter = currenttime;
                        // do warning if within 40 and 20 kelvin
                        double tempdiff = DeepFreeze.Instance.DFsettings.RegTempMonitor - part.temperature;
                        if (tempdiff <= 40)
                        {
                            _FrzrTmp = FrzrTmpStatus.WARN;
                            ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00075"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00075 = Check Temperatures, Freezer getting hot
                        }
                        else
                        {
                            if (tempdiff < 20)
                            {
                                _FrzrTmp = FrzrTmpStatus.RED;
                                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00076"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00076 = Warning!! Check Temperatures NOW, Freezer getting very hot
                            }
                        }
                        timeSinceLastTmpChk = (float)currenttime;
                        partInfo.TempWarning = false;
                    }
                    else
                    {
                        // OVER TEMP I'm Melting!!!!
                        Debug.Log("DeepFreezer Part Temp TOO HOT, Kerbals are going to melt parttemp=" + part.temperature);
                        if (!partInfo.TempWarning)
                        {
                            if (TimeWarp.CurrentRateIndex > 1) Utilities.stopWarp();
                            ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00077"), 10.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00077 = Temperature getting too hot for kerbals to remain frozen.
                            partInfo.TempWarning = true;
                        }
                        _FrzrTmp = FrzrTmpStatus.RED;
                        Utilities.Log_Debug("DeepFreezer tmpdeathCounter = {0}" , tmpdeathCounter.ToString());
                        if (TempChkMsg != null) ScreenMessages.RemoveMessage(TempChkMsg);
                        TempChkMsg = ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00078", (tmpdeathRoll - (currenttime - tmpdeathCounter)).ToString("######0"))); //#autoLOC_DF_00078 = Freezer Over Temp : Systems critical in <<1>> secs
                        if (currenttime - tmpdeathCounter > tmpdeathRoll)
                        {
                            Utilities.Log_Debug("DeepFreezer tmpdeathRoll reached, roll the dice...");
                            tmpdeathCounter = currenttime;
                            partInfo.TempWarning = false;
                            //a kerbal dies
                            if (DeepFreeze.Instance.DFsettings.fatalOption)
                            {
                                int dice = rnd.Next(1, _StoredCrewList.Count); // Randomly select a Kerbal to kill.
                                Utilities.Log_Debug("DeepFreezer A Kerbal dies dice=" + dice);
                                FrznCrewMbr deathKerbal = _StoredCrewList[dice - 1];
                                DeepFreeze.Instance.KillFrozenCrew(deathKerbal.CrewName);
                                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00079", deathKerbal.CrewName), 10.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00079 = <<1>> died due to overheating, cannot keep frozen
                                Debug.Log("DeepFreezer - kerbal " + deathKerbal.CrewName + " died due to overheating, cannot keep frozen");
                                _StoredCrewList.Remove(deathKerbal);

                                if (!flatline.isPlaying)
                                {
                                    flatline.Play();
                                }
                            }
                            else  //NON-fatal option set. Thaw them all.
                            {
                                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00080"), 10.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00080 = Over Temperature - Emergency Thaw in Progress.
                                Utilities.Log_Debug("DeepFreezer deathRoll reached, Kerbals all don't die... They just Thaw out...");
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
            //Debug.Log("DeepFreezer onLoad");
            base.OnLoad(node);
            cryopodstateclosed = new bool[FreezerSize];
            seatTakenbyFrznKerbal = new bool[FreezerSize];
            loadcryopodstatepersistent();
            loadexternaldoorstatepersistent();
            //Debug.Log("OnLoad: " + node);
            //Debug.Log("DeepFreezer end onLoad");
        }

        public override void OnStart(StartState state)
        {
            Debug.Log("DeepFreezer OnStart");
            base.OnStart(state);
            //Set the GameEvents we are interested in
            if (state != StartState.None && state != StartState.Editor)
            {
                GameEvents.onCrewTransferPartListCreated.Add(onCrewTransferPartListCreated);
                GameEvents.onCrewTransferred.Add(onCrewTransferred);
                GameEvents.onVesselChange.Add(OnVesselChange);
                GameEvents.onCrewBoardVessel.Add(OnCrewBoardVessel);
                GameEvents.onCrewOnEva.Add(onCrewOnEva);
                GameEvents.onVesselDestroy.Add(onVesselDestroy);
                GameEvents.OnCameraChange.Add(OnCameraChange);
                GameEvents.onVesselGoOffRails.Add(onVesselGoOffRails);
            }

            //Set Shaders for changing the Crypod Windows
            try
            {
                TransparentSpecularShader = Shader.Find("Legacy Shaders/Transparent/Specular");
            }
            catch (Exception ex)
            {
                Utilities.Log_Debug("Get transparentShader Legacy Shaders/Transparent/Specular failed. Error:" + ex);
            }
            if (TransparentSpecularShader == null)
            {
                Utilities.Log_Debug("transpartShader Legacy Shaders/Transparent/Specular not found.");
            }
            try
            {
                KSPSpecularShader = Shader.Find("KSP/Specular");
            }
            catch (Exception ex)
            {
                Utilities.Log_Debug("Get KSPSpecularShader KSP/Specular failed. Error:" + ex);
            }
            if (KSPSpecularShader == null)
            {
                Utilities.Log_Debug("KSPSpecularShader KSP/Specular not found.");
            }
            // Setup the sounds
            ext_door = gameObject.AddComponent<AudioSource>();
            ext_door.clip = GameDatabase.Instance.GetAudioClip("REPOSoftTech/DeepFreeze/Sounds/externaldoorswitch");
            ext_door.volume = .7F;
            ext_door.panStereo = 0;
            ext_door.spatialBlend = 0;
            ext_door.rolloffMode = AudioRolloffMode.Linear;
            ext_door.Stop();
            if (state != StartState.None && state != StartState.Editor)
            {
                mon_beep = gameObject.AddComponent<AudioSource>();
                mon_beep.clip = GameDatabase.Instance.GetAudioClip("REPOSoftTech/DeepFreeze/Sounds/mon_beep");
                mon_beep.volume = .2F;
                mon_beep.panStereo = 0;
                mon_beep.spatialBlend = 0;
                mon_beep.rolloffMode = AudioRolloffMode.Logarithmic;
                mon_beep.maxDistance = 10f;
                mon_beep.minDistance = 8f;
                mon_beep.dopplerLevel = 0f;
                //mon_beep.panLevel = 0f;
                mon_beep.playOnAwake = false;
                mon_beep.priority = 255;
                mon_beep.Stop();
                flatline = gameObject.AddComponent<AudioSource>();
                flatline.clip = GameDatabase.Instance.GetAudioClip("REPOSoftTech/DeepFreeze/Sounds/flatline");
                flatline.volume = 1;
                flatline.panStereo = 0;
                mon_beep.spatialBlend = 0;
                flatline.rolloffMode = AudioRolloffMode.Linear;
                flatline.Stop();
                hatch_lock = gameObject.AddComponent<AudioSource>();
                hatch_lock.clip = GameDatabase.Instance.GetAudioClip("REPOSoftTech/DeepFreeze/Sounds/hatch_lock");
                hatch_lock.volume = .5F;
                hatch_lock.panStereo = 0;
                mon_beep.spatialBlend = 0;
                hatch_lock.rolloffMode = AudioRolloffMode.Linear;
                hatch_lock.Stop();
                ice_freeze = gameObject.AddComponent<AudioSource>();
                ice_freeze.clip = GameDatabase.Instance.GetAudioClip("REPOSoftTech/DeepFreeze/Sounds/ice_freeze");
                ice_freeze.volume = 1;
                ice_freeze.panStereo = 0;
                mon_beep.spatialBlend = 0;
                ice_freeze.rolloffMode = AudioRolloffMode.Linear;
                ice_freeze.Stop();
                machine_hum = gameObject.AddComponent<AudioSource>();
                machine_hum.clip = GameDatabase.Instance.GetAudioClip("REPOSoftTech/DeepFreeze/Sounds/machine_hum");
                machine_hum.volume = .2F;
                machine_hum.panStereo = 0;
                mon_beep.spatialBlend = 0;
                machine_hum.rolloffMode = AudioRolloffMode.Linear;
                machine_hum.Stop();
                ding_ding = gameObject.AddComponent<AudioSource>();
                ding_ding.clip = GameDatabase.Instance.GetAudioClip("REPOSoftTech/DeepFreeze/Sounds/ding_ding");
                ding_ding.volume = .4F;
                ding_ding.panStereo = 0;
                mon_beep.spatialBlend = 0;
                ding_ding.rolloffMode = AudioRolloffMode.Linear;
                ding_ding.Stop();
                charge_up = gameObject.AddComponent<AudioSource>();
                charge_up.clip = GameDatabase.Instance.GetAudioClip("REPOSoftTech/DeepFreeze/Sounds/charge_up");
                charge_up.volume = 1;
                charge_up.panStereo = 0;
                mon_beep.spatialBlend = 0;
                charge_up.rolloffMode = AudioRolloffMode.Linear;
                charge_up.Stop();
            }
            //If we have an external door (CRY-0300) check if RPM is installed, if not disable the door, otherwise set it's current state (open/closed).
            if (animationName != string.Empty && PartHasDoor)
            {
                externalDoorAnim = null;
                var Dooranims = part.FindModelAnimators(animationName);
                if (Dooranims.Length > 0)
                {
                    externalDoorAnim = Dooranims[0];
                }
                if (externalDoorAnim == null)
                {
                    Utilities.Log_Debug("Part has external animation defined but cannot find the animation on the part");
                    ExternalDoorActive = false;
                    Events["eventOpenDoors"].active = false;
                    Events["eventCloseDoors"].active = false;
                    if (transparentTransforms != string.Empty)
                        part.setTransparentTransforms(transparentTransforms);
                }
                else
                {
                    if (part.internalModel != null)
                    {
                        externalDoorAnimOccluder = null;
                        var anims = part.internalModel.FindModelAnimators(animationName);
                        if (anims.Length > 0)
                        {
                            externalDoorAnimOccluder = anims[0];
                        }
                    }
                    else
                    {
                        externalDoorAnimOccluder = null;
                        Utilities.Log_Debug("No InternalModel found to check for external door occluder animation");
                    }
                    Utilities.Log_Debug("Part has external animation, check if JSITransparentPods is installed and process");
                    if (DFInstalledMods.IsJSITransparentPodsInstalled)
                    {
                        Utilities.Log_Debug("JSITransparentPods installed, set doorstate");
                        ExternalDoorActive = true;
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
                        Utilities.Log_Debug("JSITransparentPods NOT installed, set transparent transforms");
                        ExternalDoorActive = false;
                        Events["eventOpenDoors"].active = false;
                        Events["eventCloseDoors"].active = false;
                        Utilities.Log_Debug("door actions/events off");
                        if (transparentTransforms != string.Empty)
                            part.setTransparentTransforms(transparentTransforms);
                    }
                }
            }

            if (DFInstalledMods.IsRTInstalled)
            {
                Fields["isRTConnected"].guiActive = true;
            }
            else
            {
                Fields["isRTConnected"].guiActive = false;
            }

            timeLoadedOffrails = Planetarium.GetUniversalTime();

            Debug.Log("DeepFreezer  END OnStart");
        }

        public override void OnSave(ConfigNode node)
        {
            //Debug.Log("DeepFreezer onSave");
            savecryopodstatepersistent();
            saveexternaldoorstatepersistent();
            base.OnSave(node);
            //Debug.Log("OnSave: " + node);
            //Debug.Log("DeepFreezer end onSave");
        }

        private void OnDestroy()
        {
            //Remove GameEvent callbacks.
            Debug.Log("DeepFreezer OnDestroy");
            GameEvents.onCrewTransferPartListCreated.Remove(onCrewTransferPartListCreated);
            GameEvents.onCrewTransferred.Remove(onCrewTransferred);
            GameEvents.onVesselChange.Remove(OnVesselChange);
            GameEvents.onCrewBoardVessel.Remove(OnCrewBoardVessel);
            GameEvents.onCrewOnEva.Remove(onCrewOnEva);
            GameEvents.onVesselDestroy.Remove(onVesselDestroy);
            GameEvents.OnCameraChange.Remove(OnCameraChange);
            GameEvents.onVesselGoOffRails.Remove(onVesselGoOffRails);
            Debug.Log("DeepFreezer END OnDestroy");
        }

        #region Events

        //This Region controls the part right-click menu additions for thaw/freeze kerbals
        private void UpdateEvents()
        {
            // If we aren't Thawing or Freezing a kerbal right now, and no crewXfer i active we check all the events.
            if (!IsThawActive && !IsFreezeActive && !IsCrewXferRunning)
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
                            ProtoCrewMember crew = null;
                            List<ProtoCrewMember>.Enumerator enumerator = part.protoModuleCrew.GetEnumerator();
                            while (enumerator.MoveNext())
                            {
                                if (enumerator.Current.name == crewname)
                                    crew = enumerator.Current;
                            }
                            if (crew == null) // Search the part for the crewmember.
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
                if (item == null && (CrewMember.type == ProtoCrewMember.KerbalType.Crew || CrewMember.type == ProtoCrewMember.KerbalType.Tourist)) // Did we find one? and CrewMember is type=Crew? if so, add new Event.
                {
                    Events.Add(new BaseEvent(Events, "Freeze " + CrewMember.name, () =>
                    {
                        beginFreezeKerbal(CrewMember);
                    }, new KSPEvent { guiName = Localizer.Format("#autoLOC_DF_00199", CrewMember.name), guiActive = true }));
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
                    }, new KSPEvent { guiName = Localizer.Format("#autoLOC_DF_00200", frozenkerbal), guiActive = true }));
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
            Utilities.Log_Debug("DeepFreezer FreezeActive ToFrzeKerbal = " + ToFrzeKerbal + " Seat =" + ToFrzeKerbalSeat);
            switch (FreezeStepInProgress)
            {
                case 0:
                    //Begin
                    Utilities.Log_Debug("Freeze Step 0");
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
                    Utilities.Log_Debug("Freeze Step 1");                    
                    double ECTotal = 0f;
                    if (!CheatOptions.InfiniteElectricity && !Utilities.requireResource(vessel, EC, ChargeRate, false, true, false, out ResAvail, out ECTotal))
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00081"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00081 = Insufficient electric charge to freeze kerbal
                        FreezeKerbalAbort(ActiveFrzKerbal);
                    }
                    else
                    {
                        if (CheatOptions.InfiniteElectricity)
                        {
                            ECTotal = ChargeRate;
                        }
                        else
                        {
                            Utilities.requireResource(vessel, EC, ChargeRate, true, true, false, out ResAvail, out ECTotal);
                        }
                        StoredCharge = StoredCharge + ChargeRate;
                        if (FreezeMsg != null) ScreenMessages.RemoveMessage(FreezeMsg);
                        FreezeMsg = ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00082", StoredCharge.ToString("######0"))); //#autoLOC_DF_00082 = \u0020Cryopod - Charging: <<1>>
                        if (DeepFreeze.Instance.DFsettings.RegTempReqd)
                        {
                            part.AddThermalFlux(heatamtThawFreezeKerbal);
                        }
                        Utilities.Log_Debug("DeepFreezer Drawing Charge StoredCharge =" + StoredCharge.ToString("0000.00") + " ChargeRequired =" + ChargeRequired);
                        if (StoredCharge >= ChargeRequired)
                        {
                            if (FreezeMsg != null) ScreenMessages.RemoveMessage(FreezeMsg);
                            if (Utilities.requireResource(vessel, Glykerol, GlykerolRequired, true, true, false, out ResAvail, out ECTotal))
                            {
                                charge_up.Stop(); // stop the sound effects
                                FreezeStepInProgress = 2;
                            }
                            else  //Not enough Glykerol - abort
                            {
                                Utilities.Log_Debug("Not enough Glykerol - Aborting");
                                FreezeKerbalAbort(ActiveFrzKerbal);
                            }
                        }
                    }
                    break;

                case 2:
                    //close the Pod door Hal
                    Utilities.Log_Debug("Freeze Step 2");
                    
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
                                Utilities.Log_Debug("Closing the cryopod");
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
                                        Utilities.Log_Debug("waiting for the pod animation to complete the freeze");
                                        ClosePodAnimPlaying = true;
                                    }
                                    else
                                    {
                                        Utilities.Log_Debug("Animation has completed. go to step 3.");
                                        ClosePodAnimPlaying = false;
                                        FreezeStepInProgress = 3;
                                    }
                                }
                                else
                                {
                                    //There is no animation found? Skip to step 3.
                                    Utilities.Log_Debug("Animation disappeared. go to step 3.");
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
                    Utilities.Log_Debug("Freeze Step 3");
                    if (partHasInternals)
                    {
                        if (!FreezeWindowAnimPlaying)  // If animation not already playing start it playing.
                        {
                            Utilities.Log_Debug("freezing the cryopod window");
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
                                    Utilities.Log_Debug("waiting for the window animation to complete the freeze");
                                    FreezeWindowAnimPlaying = true;
                                }
                                else
                                {
                                    Utilities.Log_Debug("Animation has completed. go to step 4.");
                                    FreezeWindowAnimPlaying = false;
                                    FreezeStepInProgress = 4;
                                }
                            }
                            else
                            {
                                Utilities.Log_Debug("Animation disappeared. go to step 4.");
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
                    Utilities.Log_Debug("Freeze Step 4");
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
                if (FreezerSpace > 0 && part.protoModuleCrew.Contains(CrewMember)) // Freezer has space? and Part contains the CrewMember?
                {
                    double GlykTotal = 0f;
                    if (!Utilities.requireResource(vessel, Glykerol, GlykerolRequired, false, true, false, out ResAvail, out GlykTotal)) // check we have Glykerol on board. 5 units per freeze event. This should be a part config item not hard coded.
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00083"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00083 = Insufficient Glykerol to freeze kerbal
                    }
                    else // We have enough Glykerol
                    {
                        if (DeepFreeze.Instance.DFsettings.RegTempReqd) // Temperature check is required
                        {
                            if ((float)part.temperature > DeepFreeze.Instance.DFsettings.RegTempFreeze)
                            {
                                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00084", DeepFreeze.Instance.DFsettings.RegTempFreeze.ToString("######0") + Fields["CabinTemp"].guiUnits), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00084 = Cannot Freeze while Temperature greater than <<1>> 
                                return;
                            }
                        }
                        /*
                        if (DFInstalledMods.IsSMInstalled) // Check if Ship Manifest (SM) is installed?
                        {
                            if (IsSMXferRunning())  // SM is installed and is a Xfer running? If so we can't run a Freeze while a SMXfer is running.
                            {
                                ScreenMessages.PostScreenMessage("Cannot Freeze while Crew Xfer in progress", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                                return;
                            }
                        }*/
                        //if (_crewXferFROMActive || _crewXferTOActive)  // We can't run a freeze process if a crewXfer is active, this is catching Stock Xfers.
                        if (IsCrewXferRunning)  // We can't run a freeze process if a crewXfer is active, this is catching Stock Xfers.
                        {
                            ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00085"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00085 = Cannot Freeze while Crew Xfer in progress
                            return;
                        }
                        if (IsThawActive || IsFreezeActive)
                        {
                            ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00086"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00086 = Cannot run Freeze process on more than one Kerbal at a time
                            return;
                        }
                        if (DFInstalledMods.IsRTInstalled)
                        {
                            if (part.vessel.GetCrewCount() == 1 && RTlastKerbalFreezeWarn == false)
                            {
                                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00087"), 10.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00087 = RemoteTech Detected. Press Freeze Again if you want to Freeze your Last Active Kerbal
                                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00088"), 10.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00088 = An Active connection or Active Kerbal is Required On-Board to Initiate Thaw Process
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
                        ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00089"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00089 = Cannot freeze kerbal. Freezer is full
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Exception attempting to start Freeze for " + CrewMember);
                Debug.Log("Err: " + ex);
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00090"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00090 = Cannot freeze kerbal at this time
            }
        }

        private void FreezeKerbal(ProtoCrewMember CrewMember)
        {
            //this method sets all the vars for the kerbal about to be frozen and starts the freezing process (sound).
            //if we are in IVA camera mode it will switch to the view in front of the kerbal and run the cryopod closing animation.
            Utilities.Log_Debug("Freeze kerbal called");
            CrewHatchController.fetch.DisableInterface();
            ActiveFrzKerbal = CrewMember; // set the Active Freeze Kerbal
            ToFrzeKerbal = CrewMember.name;  // set the Active Freeze Kerbal name
            Utilities.Log_Debug("FreezeKerbal " + CrewMember.name);
            Utilities.dmpKerbalRefs(null, partHasInternals ? part.internalModel.seats[CrewMember.seatIdx].kerbalRef : null, CrewMember.KerbalRef);
            if (partHasInternals)
                Utilities.Log_Debug("Seatindx=" + CrewMember.seatIdx + ",Seatname=" + CrewMember.seat.seatTransformName);
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
            Utilities.Log_Debug("FreezeKerbal ACtiveFrzKerbal=" + ActiveFrzKerbal.name + ",ToFrzeKerbalSeat=" + ToFrzeKerbalSeat + ",ToFrzeKerbalXformNme=" + ToFrzeKerbalXformNme);
            FreezeStepInProgress = 0;
            IsFreezeActive = true; // Set the Freezer actively freezing mode on
            ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00091"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00091 = Starting Freeze process
            Utilities.Log_Debug("ActiveFrzKerbal=" + ActiveFrzKerbal.name + ",ToFrzeKerbal=" + ToFrzeKerbal + ",SeatIdx=" + ToFrzeKerbalSeat + ",seat transform name=" + ToFrzeKerbalXformNme);
            Utilities.Log_Debug("FreezeKerbal ended");
        }

        private void FreezeKerbalAbort(ProtoCrewMember CrewMember)
        {
            try
            {
                Utilities.Log_Debug("FreezeKerbalAbort " + CrewMember.name + " seat " + ToFrzeKerbalSeat);
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00092"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //autoLOC_DF_00092 = Freezing Aborted
                Utilities.setFrznKerbalLayer(part, CrewMember, true);
                if (partHasInternals)
                {
                    if (vesselisinIVA || vesselisinInternal)
                    {
                        setIVAFrzrCam(ToFrzeKerbalSeat);
                    }
                    if (isPartAnimated)
                        openCryopod(ToFrzeKerbalSeat, float.MaxValue);
                    if (isPartAnimated || (isPodExternal && DFInstalledMods.IsJSITransparentPodsInstalled && _prevRPMTransparentpodSetting == "ON"))
                        thawCryopodWindow(ToFrzeKerbalSeat, float.MaxValue);
                    if (isPodExternal && !DFInstalledMods.IsJSITransparentPodsInstalled)
                    {
                        
                    }
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
                //Add them to the portrait cams.
                //Portraits.RestorePortrait(part, CrewMember.KerbalRef);
                base.StartCoroutine(CallbackUtil.DelayedCallback(1, new Callback(this.fireOnVesselChange)));
                if (FreezeMsg != null) ScreenMessages.RemoveMessage(FreezeMsg);
                CrewHatchController.fetch.EnableInterface();
            }
            catch (Exception ex)
            {
                Utilities.Log("Unable to to cancel freeze of crewmember " + CrewMember.name);
                Utilities.Log("Err: " + ex);
            }
            Utilities.Log_Debug("FreezeKerbalAbort ended");
        }

        private void FreezeKerbalConfirm(ProtoCrewMember CrewMember)
        {
            //this method runs with the freeze process is complete (EC consumed)
            //it will store the frozen crew member's details in the _StorecrewList and KnownFrozenKerbals dictionary
            //it will remove the kerbal from the part and set their status to dead and unknown
            Utilities.Log_Debug("FreezeKerbalConfirm kerbal " + CrewMember.name + " seatIdx " + ToFrzeKerbalSeat);
            StoredCharge = 0;  // Discharge all EC stored
            //Make them invisible
            if (partHasInternals)
            {
                Utilities.setFrznKerbalLayer(part, CrewMember, false);
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
                
                if (DFInstalledMods.IskerbalismInstalled) // IF Kerbalism Installed, remove tracking.
                {
                    Utilities.Log_Debug("kerbalism installed untrack kerbal=" + CrewMember.name);
                    try
                    {
                        KBDisableKerbal(CrewMember.name, true);
                    }
                    catch (Exception ex)
                    {
                        Utilities.Log("DeepFreeze Exception attempting to untrack a kerbal in Kerbalism. Report this error on the Forum Thread.");
                        Utilities.Log("DeepFreeze Err: " + ex);
                    }
                }
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00093", CrewMember.name), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00093 = <<1>> frozen

                vessel.RebuildCrewList();
                DFGameEvents.onKerbalFrozen.Fire(this.part, CrewMember);
                CrewHatchController.fetch.EnableInterface();                
                GameEvents.onVesselWasModified.Fire(vessel);
                
                if (DFInstalledMods.IsUSILSInstalled) // IF USI LS Installed, remove tracking.
                {
                    Utilities.Log_Debug("USI/LS installed untrack kerbal=" + CrewMember.name);
                    try
                    {
                        USIUntrackKerbal(CrewMember.name);
                        if (this.part.vessel.GetVesselCrew().Count == 0)
                        {
                            Utilities.Log_Debug("USI/LS installed untrack vessel=" + this.part.vessel.id.ToString());
                            USIUntrackVessel(this.part.vessel.id.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        Utilities.Log("DeepFreeze Exception attempting to untrack a kerbal and/or vessel in USI/LS. Report this error on the Forum Thread.");
                        Utilities.Log("DeepFreeze Err: " + ex);
                    }
                }
                if ((vesselisinIVA || vesselisinInternal) && part.protoModuleCrew.Count == 0)
                {
                    CameraManager.Instance.SetCameraFlight();
                }
            }
            Utilities.Log_Debug("FreezeCompleted");
        }

        private void USIUntrackKerbal(string crewmember)
        //This will remove tracking of a frozen kerbal from USI Life Support MOD, so that they don't consume resources when they are thawed.
        {
            if (USIWrapper.APIReady && USIWrapper.InstanceExists)
            {
                USIWrapper.USIActualAPI.UntrackKerbal(crewmember);
                bool checkTracked = USIWrapper.USIActualAPI.IsKerbalTracked(crewmember);
                if (checkTracked)
                {
                    Debug.Log("DeepFreeze has been unable to untrack kerbal " + crewmember + " in USI LS mod. Report this error on the Forum Thread.");
                }

            }
            else
            {
                Debug.Log("DeepFreeze has been unable to connect to USI LS mod. API is not ready. Report this error on the Forum Thread.");
            }
        }

        private void USIUntrackVessel(string vesselId)
        //This will remove tracking of a frozen kerbal from USI Life Support MOD, so that they don't consume resources when they are thawed.
        {
            if (USIWrapper.APIReady && USIWrapper.InstanceExists)
            {
                USIWrapper.USIActualAPI.UntrackVessel(vesselId);
                bool checkTracked = USIWrapper.USIActualAPI.IsVesselTracked(vesselId);
                if (checkTracked)
                {
                    Debug.Log("DeepFreeze has been unable to untrack vessel " + vesselId + " in USI LS mod. Report this error on the Forum Thread.");
                }

            }
            else
            {
                Debug.Log("DeepFreeze has been unable to connect to USI LS mod. API is not ready. Report this error on the Forum Thread.");
            }
        }

        private void KBDisableKerbal(string crewmember, bool disable)
        //This will remove tracking of a frozen kerbal from USI Life Support MOD, so that they don't consume resources when they are thawed.
        {
            if (KBWrapper.APIReady)
            {
                KBWrapper.KBActualAPI.DisableKerbal(crewmember, disable);
            }
            else
            {
                Debug.Log("DeepFreeze has been unable to connect to Kerbalism mod. API is not ready. Report this error on the Forum Thread.");
            }
        }
        
        #endregion FrzKerbals

        #region ThwKerbals

        //This region contains the methods for thawing a kerbal
        private void ProcessThawKerbal()
        {
            Utilities.Log_Debug("DeepFreezer ThawActive Kerbal = " + ToThawKerbal);
            switch (ThawStepInProgress)
            {
                case 0:
                    //Begin
                    // Utilities.Log_Debug("Thaw Step 0");
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
                    // Utilities.Log_Debug("Thaw Step 1");
                    if (skipThawStep1)
                    {
                        Utilities.Log_Debug("Skipping step 1 of Thaw process");
                        charge_up.Stop();
                        ThawStepInProgress = 2;
                        break;
                    }
                    double totalAvail = 0f;
                    if (!CheatOptions.InfiniteElectricity && !Utilities.requireResource(vessel, EC, ChargeRate, false, true, false, out ResAvail, out totalAvail))
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00094"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00094 = Insufficient electric charge to thaw kerbal
                        ThawKerbalAbort(ToThawKerbal);
                    }
                    else
                    {
                        if (!CheatOptions.InfiniteElectricity)
                        {
                            Utilities.requireResource(vessel, EC, ChargeRate, true, true, false, out ResAvail, out totalAvail);
                        }
                        StoredCharge = StoredCharge + ChargeRate;
                        if (ThawMsg != null) ScreenMessages.RemoveMessage(ThawMsg);
                        ThawMsg = ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00095", StoredCharge.ToString("######0"))); //#autoLOC_DF_00095 = \u0020Cryopod - Charging:<<1>> 

                        if (DeepFreeze.Instance.DFsettings.RegTempReqd)
                        {
                            part.AddThermalFlux(heatamtThawFreezeKerbal);
                        }
                        if (StoredCharge >= ChargeRequired)
                        {
                            Utilities.Log_Debug("Stored charge requirement met. Have EC");
                            if (ThawMsg != null) ScreenMessages.RemoveMessage(ThawMsg);
                            charge_up.Stop();
                            ThawStepInProgress = 2;
                        }
                    }
                    break;

                case 2:
                    //thaw the cryopod window
                    // Utilities.Log_Debug("Thaw Step 2");
                    if (partHasInternals)
                    {
                        if (!ThawWindowAnimPlaying)  // If animation not already playing start it playing.
                        {
                            Utilities.Log_Debug("Thawing the cryopod window");
                            ice_freeze.Play();
                            ThawWindowAnimPlaying = true;
                            if (isPartAnimated || (isPodExternal && DFInstalledMods.IsJSITransparentPodsInstalled && _prevRPMTransparentpodSetting == "ON"))
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
                                    // Utilities.Log_Debug("waiting for the pod animation to complete the thaw");
                                    ThawWindowAnimPlaying = true;
                                }
                                else
                                {
                                    Utilities.Log_Debug("Animation has completed. go to step 3.");
                                    ThawWindowAnimPlaying = false;
                                    ThawStepInProgress = 3;
                                }
                            }
                            else
                            {
                                Utilities.Log_Debug("Animation disappeared. go to step 3.");
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
                    // Utilities.Log_Debug("Thaw Step 3");
                    if (partHasInternals && isPartAnimated)
                    {
                        if (!OpenPodAnimPlaying)  // If animation not already playing start it playing.
                        {
                            Utilities.Log_Debug("Opening the cryopod");
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
                                    // Utilities.Log_Debug("waiting for the pod animation to complete the thaw");
                                    OpenPodAnimPlaying = true;
                                }
                                else
                                {
                                    Utilities.Log_Debug("Animation has completed. go to step 4.");
                                    OpenPodAnimPlaying = false;
                                    ThawStepInProgress = 4;
                                }
                            }
                            else
                            {
                                //There is no animation found? Skip to step 4.
                                Utilities.Log_Debug("Animation disappeared. go to step 4.");
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
                    // Utilities.Log_Debug("Thaw Step 4");
                    ThawKerbalStep4(ToThawKerbal);
                    break;
            }
        }

        public void beginThawKerbal(string frozenkerbal)
        {
            try
            {
                Utilities.Log_Debug("beginThawKerbal " + frozenkerbal);
                if (part.protoModuleCrew.Count >= part.CrewCapacity)
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00096", frozenkerbal), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00096 = Cannot Thaw <<1>> Part is full
                    Utilities.Log_Debug("Cannot thaw " + frozenkerbal + " Part is full");
                }
                else
                {
                    /*
                     * if (DFInstalledMods.IsSMInstalled) // Check if Ship Manifest (SM) is installed?
                    {
                        if (IsSMXferRunning()) // SM is installed and is a Xfer running? If so we can't run a Freeze while a SMXfer is running.
                        {
                            ScreenMessages.PostScreenMessage("Cannot Thaw while Crew Xfer in progress", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                            return;
                        }
                    }
                    */
                    //if (_crewXferFROMActive || _crewXferTOActive)  // We can't run a thaw process if a crewXfer is active, this is catching Stock Xfers.
                    if (IsCrewXferRunning)  // We can't run a thaw process if a crewXfer is active, this is catching Stock Xfers.
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00097"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00097 = Cannot Thaw while Crew Xfer in progress
                        return;
                    }
                    if (IsThawActive || IsFreezeActive)
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00098"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00098 = Cannot run Thaw process on more than one Kerbal at a time
                        return;
                    }

                    ToThawKerbal = frozenkerbal;  // Set the Active Thaw Kerbal to frozenkerbal name
                    IsThawActive = true;  // Turn the Freezer actively thawing mode on
                    ThawStepInProgress = 0;
                    CrewHatchController.fetch.DisableInterface();
                    Utilities.Log_Debug("beginThawKerbal has started thawing process");
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Exception attempting to start Thaw for " + frozenkerbal);
                Debug.Log("Err: " + ex);
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00099"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00099 = Cannot thaw kerbal at this time
            }
        }

        private void ThawKerbalStep0(string frozenkerbal)
        {
            // First we find out Unowned Crewmember in the roster.
            ProtoCrewMember kerbal = null;
            IEnumerator<ProtoCrewMember> enumerator = HighLogic.CurrentGame.CrewRoster.Unowned.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.name == frozenkerbal)
                    kerbal = enumerator.Current;
            } 
            if (kerbal != null)
            {
                // Set our newly thawed Popsicle, er Kerbal, to Crew type again (from Unowned) and Assigned status (from Dead status).
                 Utilities.Log_Debug("set type to crew and assigned");
                kerbal.type = ProtoCrewMember.KerbalType.Crew;
                kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                 Utilities.Log_Debug("find the stored crew member");
                //Now we find our Crewmember in the stored crew list in the part.
                FrznCrewMbr tmpcrew = null;  // Find the thawed kerbal in the frozen kerbal list.
                List<FrznCrewMbr>.Enumerator enumerator2 = _StoredCrewList.GetEnumerator();
                while (enumerator2.MoveNext())
                {
                    if (enumerator2.Current.CrewName == frozenkerbal)
                        tmpcrew = enumerator2.Current;
                }
                if (tmpcrew != null)
                {
                    //check if seat is empty, if it is we have to seat them in next available seat
                     Utilities.Log_Debug("frozenkerbal " + tmpcrew.CrewName + ",seatindx=" + tmpcrew.SeatIdx);
                    ToThawKerbalSeat = tmpcrew.SeatIdx;
                    if (partHasInternals)  // All deepfreeze supplied parts have internals.
                    {
                         Utilities.Log_Debug("Part has internals");
                         Utilities.Log_Debug("Checking their seat taken=" + part.internalModel.seats[tmpcrew.SeatIdx].taken);
                        ProtoCrewMember crew = part.internalModel.seats[tmpcrew.SeatIdx].crew;
                        if (crew != null)
                        {
                            // we check the internal seat has our Crewmember in it. Not some other kerbal.
                            if (crew.name == frozenkerbal)
                            {
                                int codestep = 0;
                                try
                                {                                    
                                    //seat is taken and it is by themselves. Expected condition.
                                    //Check the KerbalRef isn't null. If it is we need to respawn them. (this shouldn't occur).
                                    if (kerbal.KerbalRef == null)
                                    {
                                         Utilities.Log_Debug("Kerbal kerbalref is still null, respawn");
                                        kerbal.seat = part.internalModel.seats[tmpcrew.SeatIdx];
                                        kerbal.seatIdx = tmpcrew.SeatIdx;
                                        ProtoCrewMember.Spawn(kerbal);
                                         Utilities.Log_Debug("Kerbal kerbalref = " + kerbal.KerbalRef.GetInstanceID());
                                    }
                                    codestep = 1;
                                    if (kerbal.KerbalRef != null)
                                    {
                                        Utilities.subdueIVAKerbalAnimations(kerbal.KerbalRef);
                                    }
                                    Utilities.setFrznKerbalLayer(part, kerbal, true);  //Set the Kerbal renderer layers on so they are visible again.
                                    kerbal.KerbalRef.InPart = part; //Put their kerbalref back in the part.
                                    kerbal.KerbalRef.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                                    codestep = 2;
                                    //Add them to the portrait cams.
                                    DFPortraits.RestorePortrait(part, kerbal.KerbalRef);
                                    base.StartCoroutine(CallbackUtil.DelayedCallback(1, new Callback(this.fireOnVesselChange)));
                                    base.StartCoroutine(CallbackUtil.DelayedCallback<Kerbal>(5, new Callback<Kerbal>(this.checkPortraitRegistered), kerbal.KerbalRef));
                                    Utilities.Log_Debug("Expected condition met, kerbal already in their seat.");
                                    codestep = 3;
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
                                    codestep = 4;
                                    // If in IVA mode set the camera to watch the process.
                                    if (vesselisinIVA || vesselisinInternal)
                                        setIVAFrzrCam(tmpcrew.SeatIdx);
                                    codestep = 5;
                                    if (ExternalDoorActive)
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
                                     Utilities.Log_Debug("Finishing ThawKerbalStep0");
                                }
                                catch (Exception ex)
                                {
                                    Debug.Log("Exception attempting to add to seat for " + frozenkerbal);
                                    Debug.Log("Part has Internals, and Frozen Kerbal was found codestep = " + codestep);
                                    Debug.Log("Err: " + ex);
                                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00100"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00100 = Code Error: Cannot thaw kerbal at this time, Check Log
                                    ThawKerbalAbort(frozenkerbal);
                                }
                            }
                            else  //Seat is taken, but not by our frozen Kerbal, we can't continue.
                            {
                                 Utilities.Log_Debug("Seat taken by someone else, Abort");
                                Debug.Log("Could not start kerbal Thaw process as seat is taken by another kerbal. Very Very Bad. Report this to Mod thread");
                                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00100"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00100 = Code Error: Cannot thaw kerbal at this time, Check Log
                                ThawKerbalAbort(frozenkerbal);
                            }
                        }
                        else
                        // The Seat's Crew is set to NULL. This could happen when on UPGRADE from V0.17 and below, or where vessel is loaded in range of the active vessel on flight scene startup.
                        // and then the user switches to this vessel and thaws a kerbal.
                        {
                             Utilities.Log_Debug("Seat Crew KerbalRef is NULL re-add them at seatidx=" + tmpcrew.SeatIdx);
                            //this.part.internalModel.seats[tmpcrew.SeatIdx].taken = false; // Set their seat to NotTaken before we assign them back to their seat, not sure we really need this.
                            int codestep = 0;
                            try
                            {
                                part.internalModel.SitKerbalAt(kerbal, part.internalModel.seats[tmpcrew.SeatIdx]);
                                if (ExternalDoorActive)
                                {
                                    //set the seat to allow helmet, this will cause the helmet to appear
                                    kerbal.seat.allowCrewHelmet = true;
                                }
                                codestep = 1;
                                kerbal.seat.SpawnCrew();
                                // Think this will get rid of the static that appears on the portrait camera
                                setseatstaticoverlay(part.internalModel.seats[tmpcrew.SeatIdx]);
                                if (kerbal.KerbalRef != null)
                                {
                                    Utilities.subdueIVAKerbalAnimations(kerbal.KerbalRef);
                                }
                                Utilities.setFrznKerbalLayer(part, kerbal, true);  //Set the Kerbal renderer layers on so they are visible again.
                                kerbal.KerbalRef.InPart = part; //Put their kerbalref back in the part.
                                kerbal.KerbalRef.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                                codestep = 2;
                                //Add them to the portrait cams.
                                DFPortraits.RestorePortrait(part, kerbal.KerbalRef);
                                base.StartCoroutine(CallbackUtil.DelayedCallback(1, new Callback(this.fireOnVesselChange)));
                                base.StartCoroutine(CallbackUtil.DelayedCallback<Kerbal>(5, new Callback<Kerbal>(this.checkPortraitRegistered), kerbal.KerbalRef));
                                Utilities.Log_Debug("Just thawing crew and added to GUIManager");
                                codestep = 3;
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
                                codestep = 4;
                                if (vesselisinIVA || vesselisinInternal)
                                    setIVAFrzrCam(tmpcrew.SeatIdx);
                                codestep = 5;
                                if (ExternalDoorActive)
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
                                 Utilities.Log_Debug("Finishing ThawKerbalStep0");
                            }
                            catch (Exception ex)
                            {
                                Debug.Log("Exception attempting to add to seat for " + frozenkerbal);
                                Debug.Log("Seat Crew KerbalRef is NULL re-add them at seatidx=" + tmpcrew.SeatIdx + " codestep = " + codestep);
                                Debug.Log("Err: " + ex);
                                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00100"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00100 = Code Error: Cannot thaw kerbal at this time, Check Log
                                ThawKerbalAbort(frozenkerbal);
                            }
                        }
                    }
                    else //All DeepFreeze supplied parts have an internal. this is in case someone adds their own part with DeepFreezer Module attached.
                    {
                         Utilities.Log_Debug("Part has no internals, just add");
                        try
                        {
                            part.AddCrewmember(kerbal);  // Add them to the part anyway.
                                                              //seatTakenbyFrznKerbal[ToThawKerbalSeat] = false;
                                                              //kerbal.seat.SpawnCrew();
                        }
                        catch (Exception ex)
                        {
                            Debug.Log("Exception attempting to add to seat for " + frozenkerbal);
                            Debug.Log("Where DeepFreezer Module is attached to internal-LESS part");
                            Debug.Log("Err: " + ex);
                            ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00100"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00100 = Code Error: Cannot thaw kerbal at this time, Check Log
                            ThawKerbalAbort(frozenkerbal);
                        }
                    }
                }
                else // This should NEVER occur.
                {
                    Debug.Log("Could not find frozen kerbal in _StoredCrewList to Thaw, Very Very Bad. Report this to Mod thread");
                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00100"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00100 = Code Error: Cannot thaw kerbal at this time, Check Log
                    ThawKerbalAbort(frozenkerbal);
                }
            }
            else // This should NEVER occur.
            {
                Debug.Log("Could not find frozen kerbal in Unowned Crew List to Thaw, Very Very Bad. Report this to Mod thread");
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00100"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00100 = Code Error: Cannot thaw kerbal at this time, Check Log
                ThawKerbalAbort(frozenkerbal);
            }
        }

        private void TexReplacerPersonaliseKerbal(Kerbal kerbal)
        {
            //This will re-personalise a kerbal who has been personalised using Texture replacer mod.
            try
            {
                 Utilities.Log_Debug("Texture Replacer installed. Re-PersonliseKerbal");
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
             Utilities.Log_Debug("ThawkerbalAbort called");
            ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00101"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00101 = Thawing Aborted
            IsThawActive = false; // Turn the Freezer actively thawing mode off
            ToThawKerbal = ""; // Set the Active Thaw Kerbal to null
            StoredCharge = 0; // Discharge all EC stored
            machine_hum.Stop(); //stop the sound effects
            charge_up.Stop();
            OpenPodAnimPlaying = false;
            ThawWindowAnimPlaying = false;
            ThawStepInProgress = 0;
            ProtoCrewMember kerbal = null;
            IEnumerator<ProtoCrewMember> enumerator = HighLogic.CurrentGame.CrewRoster.Crew.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.name == ThawKerbal)
                    kerbal = enumerator.Current;
            }
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
                // Utilities.Log_Debug("Time freezewindow started " + Planetarium.GetUniversalTime());
                freezeCryopodWindow(ToThawKerbalSeat, float.MaxValue);
                // Utilities.Log_Debug("Time freezewindow finished make them invisible " + Planetarium.GetUniversalTime());
                cryopodstateclosed[ToThawKerbalSeat] = true;
                savecryopodstatepersistent();
                if (partHasStripLights && DeepFreeze.Instance.DFsettings.StripLightsActive)
                {
                    stopStripLightFlash(ToThawKerbalSeat);
                }
            }
            //Make them invisible again
            Utilities.setFrznKerbalLayer(part, kerbal, false);
            if (ThawMsg != null) ScreenMessages.RemoveMessage(ThawMsg);
            CrewHatchController.fetch.EnableInterface();
            Utilities.Log_Debug("ThawkerbalAbort End");
        }

        private void ThawKerbalStep4(String frozenkerbal)
        {
             Utilities.Log_Debug("ThawKerbalConfirm start for " + frozenkerbal);
            machine_hum.Stop(); //stop sound effects
            StoredCharge = 0;   // Discharge all EC stored
            
            ProtoCrewMember kerbal = null;
            IEnumerator<ProtoCrewMember> enumerator = HighLogic.CurrentGame.CrewRoster.Crew.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.name == frozenkerbal)
                    kerbal = enumerator.Current;
            }
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
                     Utilities.Log_Debug("Animator " + anim.name + " for " + kerbal.KerbalRef.name + " turned off");
                    anim.enabled = false;
                }
            }
            ToThawKerbal = ""; // Set the Active Thaw Kerbal to null
            IsThawActive = false; // Turn the Freezer actively thawing mode off
            ThawStepInProgress = 0;
            skipThawStep1 = false;
            ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00102", frozenkerbal), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00102 = <<1>> thawed out
            if (emergencyThawInProgress)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00103", frozenkerbal), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00103 = <<1>> was thawed out due to lack of Electrical Charge to run cryogenics
                Debug.Log("DeepFreezer - kerbal " + frozenkerbal + " was thawed out due to lack of Electrical charge to run cryogenics");
                DeepFreeze.Instance.setComatoseKerbal(part, kerbal, ProtoCrewMember.KerbalType.Tourist, true);

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
                 Utilities.Log_Debug("Adding New Comatose Crew to dictionary");
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
                     Utilities.Log("Unable to add to knownfrozenkerbals comatose crewmember " + kerbal.name);
                     Utilities.Log("Err: " + ex);
                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00104"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00104 = DeepFreezer mechanical failure
                }
            }
            Debug.Log("Thawed out: " + frozenkerbal);
            UpdateCounts(); // Update the Crew counts
            removeThawEvent(frozenkerbal); // Remove the Thaw Event for this kerbal.
            ding_ding.Play();
            OpenPodAnimPlaying = false;            
            if (DFInstalledMods.IskerbalismInstalled) // IF Kerbalism Installed, add tracking.
            {
                Utilities.Log_Debug("kerbalism installed track kerbal=" + frozenkerbal);
                try
                {
                    KBDisableKerbal(frozenkerbal, false);
                }
                catch (Exception ex)
                {
                    Utilities.Log("DeepFreeze Exception attempting to track a kerbal in Kerbalism. Report this error on the Forum Thread.");
                    Utilities.Log("DeepFreeze Err: " + ex);
                }
            }
            CrewHatchController.fetch.EnableInterface();
            DFGameEvents.onKerbalThaw.Fire(this.part, kerbal);            
            GameEvents.onVesselWasModified.Fire(vessel);
            Utilities.Log_Debug("ThawKerbalConfirm End");
        }

        #endregion ThwKerbals

        private bool RemoveKerbal(ProtoCrewMember kerbal, int SeatIndx)
        //Removes a frozen kerbal from the vessel.
        {
            try
            {
                 Utilities.Log_Debug("RemoveKerbal " + kerbal.name + " seat " + SeatIndx);
                FrznCrewMbr tmpcrew = null;
                List<FrznCrewMbr>.Enumerator enumerator = _StoredCrewList.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.CrewName == kerbal.name)
                        tmpcrew = enumerator.Current;
                }
                if (tmpcrew == null)
                {
                    FrznCrewMbr frzncrew = new FrznCrewMbr(kerbal.name, SeatIndx, vessel.id, vessel.name);
                     Utilities.Log_Debug("Adding _StoredCrewList entry");
                    _StoredCrewList.Add(frzncrew);
                }
                else
                {
                     Utilities.Log("Found Kerbal in the stored frozen crew list for this part.");
                     Utilities.Log("Crewmember:" + tmpcrew.CrewName + " Seat:" + tmpcrew.SeatIdx);
                }
                // Update the saved frozen kerbals dictionary
                KerbalInfo kerbalInfo = new KerbalInfo(Planetarium.GetUniversalTime());
                kerbalInfo.vesselID = CrntVslID;
                kerbalInfo.vesselName = CrntVslName;
                kerbalInfo.type = ProtoCrewMember.KerbalType.Unowned;
                kerbalInfo.status = ProtoCrewMember.RosterStatus.Dead;
                if (partHasInternals)
                {
                    kerbalInfo.seatName = part.internalModel.seats[SeatIndx].seatTransformName;
                    kerbalInfo.seatIdx = SeatIndx;
                }
                else
                {
                    kerbalInfo.seatName = "Unknown";
                    kerbalInfo.seatIdx = -1;
                }
                kerbalInfo.partID = CrntPartID;
                kerbalInfo.experienceTraitName = kerbal.experienceTrait.Title;
                 Utilities.Log_Debug("Adding New Frozen Crew to dictionary");
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
                     Utilities.Log("Unable to add to knownfrozenkerbals frozen crewmember " + kerbal.name);
                     Utilities.Log("Err: " + ex);
                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00104"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00104 = DeepFreezer mechanical failure
                    return false;
                }
                if (partHasInternals && ExternalDoorActive)
                    Utilities.setHelmetshaders(kerbal.KerbalRef, true);
                // remove the CrewMember from the part crewlist and unregister their traits, because they are frozen, and this is the only way to trick the game.
                kerbal.UnregisterExperienceTraits(part);
                part.protoModuleCrew.Remove(kerbal);
                Vessel.CrewWasModified(vessel);
                if (partHasInternals)
                {
                    if (part.internalModel.seats[SeatIndx].kerbalRef != kerbal.KerbalRef)
                    {
                        part.internalModel.seats[SeatIndx].kerbalRef = kerbal.KerbalRef;
                        setseatstaticoverlay(part.internalModel.seats[SeatIndx]);
                    }
                    part.internalModel.seats[SeatIndx].taken = true; // Set their seat to Taken, because they are really still there. :)
                    seatTakenbyFrznKerbal[SeatIndx] = true;
                }
                // Set our newly frozen Popsicle, er Kerbal, to Unowned type (usually a Crew) and Dead status.
                kerbal.type = ProtoCrewMember.KerbalType.Unowned;
                kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                if (kerbal.KerbalRef != null)
                {
                    //Remove them from the GUIManager Portrait cams.
                    DFPortraits.DestroyPortrait(kerbal.KerbalRef);
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
            Utilities.Log_Debug("Start AddKerbal " + kerbal.name);
            ProtoCrewMember.KerbalType originaltype = ProtoCrewMember.KerbalType.Crew;
            try
            {
                try
                {
                    FrznCrewMbr tmpcrew = null; // Find the thawed kerbal in the frozen kerbal list.
                    List<FrznCrewMbr>.Enumerator enumerator = _StoredCrewList.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.CrewName == kerbal.name)
                            tmpcrew = enumerator.Current;
                    }
                    if (tmpcrew != null)
                    {
                         Utilities.Log_Debug("Removing _StoredCrewList entry");
                        _StoredCrewList.Remove(tmpcrew);
                    }
                }
                catch (Exception ex)
                {
                     Utilities.Log("Unable to remove _StoredCrewList frozen crewmember " + kerbal.name);
                     Utilities.Log("Err: " + ex);
                    //ScreenMessages.PostScreenMessage("DeepFreezer mechanical failure", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    //return false;
                }

                // Update the saved frozen kerbals dictionary
                 Utilities.Log_Debug("Removing Frozen Crew to dictionary");
                try
                {
                    if (DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.ContainsKey(kerbal.name))
                    {
                        KerbalInfo tmpFrzCrew = DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals[kerbal.name];
                        if (tmpFrzCrew.experienceTraitName == "Tourist")
                        {
                            originaltype = ProtoCrewMember.KerbalType.Tourist;
                        }
                        DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Remove(kerbal.name);
                    }
                    if (DeepFreeze.Instance.DFsettings.debugging) DeepFreeze.Instance.DFgameSettings.DmpKnownFznKerbals();
                }
                catch (Exception ex)
                {
                     Utilities.Log("Unable to remove knownfrozenkerbals frozen crewmember " + kerbal.name);
                     Utilities.Log("Err: " + ex);
                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00104"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00104 = DeepFreezer mechanical failure
                    return false;
                }
                if (partHasInternals && ExternalDoorActive)
                    Utilities.setHelmetshaders(kerbal.KerbalRef, true);

                // Set our newly thawed Popsicle, er Kerbal, to Original type and Assigned status.
                kerbal.type = originaltype;
                kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                
                if (partHasInternals)
                {
                    if (kerbal.seat != part.internalModel.seats[SeatIndx])
                    {
                        kerbal.seat = part.internalModel.seats[SeatIndx];
                        kerbal.seatIdx = SeatIndx;
                    }
                    if (part.internalModel.seats[SeatIndx].crew != kerbal)
                    {
                        part.internalModel.seats[SeatIndx].crew = kerbal;
                    }
                    if (part.internalModel.seats[SeatIndx].kerbalRef != kerbal.KerbalRef)
                    {
                        part.internalModel.seats[SeatIndx].kerbalRef = kerbal.KerbalRef;
                        part.internalModel.seats[SeatIndx].taken = true;
                        setseatstaticoverlay(part.internalModel.seats[SeatIndx]);
                    }
                    seatTakenbyFrznKerbal[SeatIndx] = false;
                }

                // add the CrewMember to the part crewlist and register their traits.
                kerbal.RegisterExperienceTraits(part);
                if (!part.protoModuleCrew.Contains(kerbal))
                {
                    part.protoModuleCrew.Add(kerbal);
                    Vessel.CrewWasModified(vessel);
                }

                /*if (kerbal.KerbalRef != null)
                {
                    if (kerbal.KerbalRef.InPart == null)
                    {
                        kerbal.KerbalRef.InPart = part;
                    }
                    //Add themto the GUIManager Portrait cams.
                    Portraits.RestorePortrait(kerbal.KerbalRef);
                }*/
                Utilities.Log_Debug("End AddKerbal");
                return true;
            }
            catch (Exception ex)
            {
                Debug.Log("Add Kerbal " + kerbal.name + " for DeepFreeze failed");
                Debug.Log("Err: " + ex);
                return false;
            }
        }

        internal void checkPortraitRegistered(Kerbal kerbal)
        {
            if (!DFPortraits.HasPortrait(kerbal, true))
            {
                vessel.DespawnCrew();
                base.StartCoroutine(CallbackUtil.DelayedCallback(3, new Callback(this.delayedSpawnCrew)));
            }
        }

        #region CrewXfers

        internal bool IsCrewXferRunning
        {
            get
            {
                if (CrewHatchController.fetch.Active)
                    return true;

                if (DFInstalledMods.IsSMInstalled)
                {
                    if (IsSMXferRunning)
                        return true;
                }
                return false;
        }
    }

        //This region contains the methods for handling Crew Transfers correctly
        internal bool IsSMXferRunning  // Checks if Ship Manifest is running a CrewXfer or Not.
        {
            get
            {
                try
                {

                    if (!SMWrapper.SMAPIReady)
                        SMWrapper.InitSMWrapper();
                    if (SMWrapper.ShipManifestAPI.CrewXferActive &&
                        (SMWrapper.ShipManifestAPI.FromPart == part || SMWrapper.ShipManifestAPI.ToPart == part))
                    {
                        Utilities.Log_Debug("DeepFreeze SMXfer running and it is from or to this part");
                        return true;
                    }
                    if (SMWrapper.ShipManifestAPI.CrewXferActive)
                    {
                        Utilities.Log_Debug("DeepFreeze SMXfer running but is it not from or to this part");
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Utilities.Log(
                        "DeepFreezer Error attempting to check Ship Manifest if there is a crew transfer active");
                    Utilities.Log(ex.Message);
                    return false;
                }
            }
        }
        
        /// <summary>
        /// Fired when a stock crew transfer is started by gameevent onCrewTransferPartListCreated
        /// Checks if This Freezer part is in the list and if it is, check if it is full or not taking into account frozen kerbal.
        /// If it is move it from the OK list to the NOT OK list so the user can't select this part.
        /// </summary>
        /// <param name="fromToAction">List<Part>, List<Part>> fromToAction two lists of parts.</param>
        private void onCrewTransferPartListCreated(GameEvents.HostedFromToAction<Part, List<Part>> HostedFromTo)
        {
            CrewMoveList.Clear();
            foreach (Part p in HostedFromTo.from)
            {
                if (p == part && PartFull)
                {
                    CrewMoveList.Add(p);
                }
            }

            CrewMoveList.ForEach(id => HostedFromTo.from.Remove(id));
            CrewMoveList.ForEach(id => HostedFromTo.to.Add(id));
            crewTransferInputLock = true;
        }
        
        //Delayed corountine to fire an internal onvesselchange, this forces the portraits system to refresh
        internal void fireOnVesselChange()
        {
            onvslchgInternal = true;
            GameEvents.onVesselChange.Fire(vessel);
        }

        //For crew Xfer borked by a full freezer and we transfer them back we have to spawn the vessel crew then fire the onvesselchange to get the 
        // portraits system to refresh
        internal void delayedSpawnCrew()
        {
            vessel.SpawnCrew();
            resetFrozenKerbals();
            fireOnVesselChange();
        }

        // this is called when a vessel change event fires.
        // Triggered when switching to a different vessel, loading a vessel, or launching
        private void OnVesselChange(Vessel vessel)
        {
            Debug.Log("DeepFreezer OnVesselChange onvslchgInternal " + onvslchgInternal + " activevessel " + FlightGlobals.ActiveVessel.id + " parametervesselid " + vessel.id + " this vesselid " + this.vessel.id + " this partid " + part.flightID);
            if (onvslchgInternal)
            {
                onvslchgInternal = false;
                return;
            }
            //Check a Freeze or Thaw is not in progress, if it is, we must abort.
            if (IsThawActive)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00105"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00105 = Vessel about to change, Aborting Thaw process
                Utilities.Log_Debug("Thawisactive - abort");
                ThawKerbalAbort(ToThawKerbal);
            }
            if (IsFreezeActive)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00106"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00106 = Vessel about to change, Aborting Freeze process
                Utilities.Log_Debug("Freezeisactive - abort");
                FreezeKerbalAbort(ActiveFrzKerbal);
            }
            //If the vessel we have changed to is the same as the vessel this partmodule is attached to we LOAD persistent vars, otherwise we SAVE persistent vars.
            if (vessel.id == this.vessel.id)
            {
                loadcryopodstatepersistent();
                loadexternaldoorstatepersistent();
                if (ExternalDoorActive)
                {
                    setHelmetstoDoorState();
                    setDoorHandletoDoorState();
                }
                resetFrozenKerbals();
                if (partHasInternals)
                    resetCryopods(true); 
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
            crewTransferInputLock = false;
        }

        // this is called when the vessel comes off rails.
        private void onVesselGoOffRails(Vessel vessel)
        {
            if (vessel == this.vessel)
            {
                timeLoadedOffrails = Planetarium.GetUniversalTime();
            }    
        }

        // when the camera mode changes reset the frozen kerbal portrait cams.
        private void OnCameraChange(CameraManager.CameraMode cammode)
        {
            resetFrozenPortraits();
        }

        // this is called when vessel is destroyed.
        //     Triggered when a vessel instance is destroyed; any time a vessel is unloaded,
        //     ie scene changes, exiting loading distance
        private void onVesselDestroy(Vessel vessel)
        {
            Utilities.Log_Debug("OnVesselDestroy");
            //Check a Freeze or Thaw is not in progress, if it is, we must abort.
            if (IsThawActive)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00105"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00105 = Vessel about to change, Aborting Thaw process
                Utilities.Log_Debug("Thawisactive - abort");
                ThawKerbalAbort(ToThawKerbal);
            }
            if (IsFreezeActive)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00106"), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00106 = Vessel about to change, Aborting Freeze process
                Utilities.Log_Debug("Freezeisactive - abort");
                FreezeKerbalAbort(ActiveFrzKerbal);
            }
        }
        
        /// <summary>
        /// This Method will get a list of all frozen kerbals in this part and remove their Portrait Cameras from the PortraitGallery if there is one.
        /// It is called when GameEvent OnCameraChange is fired.
        /// </summary>
        internal void resetFrozenPortraits()
        {
            // create a list of kerbal that are in this part in this vessel & they are not comatose/tourist
            List<KeyValuePair<string, KerbalInfo>> FrznKerbalsinPart = new List<KeyValuePair<string, KerbalInfo>>();
            foreach (var frznKerbal in DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals)
            {
                if (frznKerbal.Value.partID == CrntPartID && frznKerbal.Value.vesselID == CrntVslID &&
                    frznKerbal.Value.type != ProtoCrewMember.KerbalType.Tourist)
                {
                    FrznKerbalsinPart.Add(frznKerbal);
                }
            }
            for (int i = 0; i < FrznKerbalsinPart.Count; i++)
            {
                ProtoCrewMember crewmember = null;
                IEnumerator<ProtoCrewMember> enumerator = HighLogic.CurrentGame.CrewRoster.Unowned.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.name == FrznKerbalsinPart[i].Key)
                        crewmember = enumerator.Current;
                }
                if (crewmember != null)
                {
                    DFPortraits.DestroyPortrait(crewmember.KerbalRef);
                }
            }
        }

        internal void resetFrozenKerbals()
        {
            try
            {
                // Create a list of kerbals that are in Invalid Seats (SeatIndx == -1 where kerbal is in this part in this vessel & they are not comatose/tourist
                List<KeyValuePair<string, KerbalInfo>> kerbalsInvSeats = new List<KeyValuePair<string, KerbalInfo>>();
                foreach (var frznKerbal in DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals)
                {
                    if (frznKerbal.Value.partID == CrntPartID && frznKerbal.Value.vesselID == CrntVslID &&
                        frznKerbal.Value.type != ProtoCrewMember.KerbalType.Tourist && frznKerbal.Value.seatIdx == -1)
                    {
                        kerbalsInvSeats.Add(frznKerbal);
                    }
                }
                
                // create a list of kerbal that are in this part in this vessel & they are not comatose/tourist
                List<KeyValuePair<string, KerbalInfo>> FrznKerbalsinPart = new List<KeyValuePair<string, KerbalInfo>>();
                foreach (var frznKerbal in DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals)
                {
                    if (frznKerbal.Value.partID == CrntPartID && frznKerbal.Value.vesselID == CrntVslID &&
                        frznKerbal.Value.type != ProtoCrewMember.KerbalType.Tourist)
                    {
                        FrznKerbalsinPart.Add(frznKerbal);
                    }
                }
                
                //If we found any Invalid Seat assignments we need to find them empty seats
                if (kerbalsInvSeats.Count > 0) 
                {
                    bool[] seatIndxs = new bool[FreezerSize];  //Create a bool array to store whether seats are taken or not
                                                               //go through all the frozen kerbals in the part that don't have invalid seats and set bool array seat index to true (taken) for each
                    foreach (KeyValuePair<string, KerbalInfo> frznkerbal in FrznKerbalsinPart)
                    {
                        if (frznkerbal.Value.seatIdx > -1 && frznkerbal.Value.seatIdx < FreezerSize - 1)
                            seatIndxs[frznkerbal.Value.seatIdx] = true;
                    }
                    //go through all the thawed kerbals in the part and set bool array seat index to true (taken) for each
                    foreach (ProtoCrewMember crew in part.protoModuleCrew)
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
                                frznkerbal.Value.seatName = part.internalModel.seats[i].seatTransformName;
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
                    FrznCrewMbr tmpcrew = null; // Find the thawed kerbal in the frozen kerbal list.
                    List<FrznCrewMbr>.Enumerator enumerator2 = _StoredCrewList.GetEnumerator();
                    while (enumerator2.MoveNext())
                    {
                        if (enumerator2.Current.CrewName == kerbal.Key)
                            tmpcrew = enumerator2.Current;
                    }

                    if (tmpcrew == null)
                    {
                        //add them to our storedcrewlist for this part.
                        Utilities.Log_Debug("DeepFreezer Adding frozen kerbal to this part storedcrewlist " + kerbal.Key);
                        _StoredCrewList.Add(fzncrew);
                    }

                    //check if they are in the part and spawned, if not do so.
                    ProtoCrewMember crewmember = null;
                    IEnumerator<ProtoCrewMember> enumerator = HighLogic.CurrentGame.CrewRoster.Unowned.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.name == kerbal.Key)
                            crewmember = enumerator.Current;
                    }
                    if (crewmember != null)
                    {
                        if (partHasInternals)
                        {
                            crewmember.seatIdx = kerbal.Value.seatIdx;
                            if (crewmember.seatIdx != -1 && crewmember.seatIdx < FreezerSize)
                                crewmember.seat = part.internalModel.seats[crewmember.seatIdx];
                            if (crewmember.KerbalRef == null)
                            {
                                ProtoCrewMember.Spawn(crewmember);
                            }
                            crewmember.KerbalRef.transform.parent =
                                part.internalModel.seats[crewmember.seatIdx].seatTransform;
                            crewmember.KerbalRef.transform.localPosition = Vector3.zero;
                            crewmember.KerbalRef.transform.localRotation = Quaternion.identity;
                            crewmember.KerbalRef.InPart = null;
                            if (ExternalDoorActive)
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
                            Utilities.setFrznKerbalLayer(part, crewmember, false);
                            part.internalModel.seats[crewmember.seatIdx].taken = true;
                            part.internalModel.seats[crewmember.seatIdx].kerbalRef = crewmember.KerbalRef;
                            part.internalModel.seats[crewmember.seatIdx].crew = crewmember;
                            setseatstaticoverlay(part.internalModel.seats[crewmember.seatIdx]);
                        }
                        //Unregister their traits/abilities and remove them from the Portrait Cameras if they are there.
                        crewmember.UnregisterExperienceTraits(part);
                        part.protoModuleCrew.Remove(crewmember);
                        vessel.RemoveCrew(crewmember);
                        DFPortraits.DestroyPortrait(crewmember.KerbalRef);
                    }
                    else
                    {
                        Utilities.Log("DeepFreezer Error attempting to resetFrozenKerbal {0}, cannot find them in the Roster", kerbal.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.Log("DeepFreezer Error attempting to resetFrozenKerbals, Critical ERROR, Report on the forum");
                Utilities.Log(ex.Message);
            }            
        }

        private void onCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> HostedFromTo)
        {
            bool fromEVA = false;
            for (int i = 0; i < HostedFromTo.from.Modules.Count; ++i)
            {
                if (HostedFromTo.from.Modules[i] as KerbalEVA)
                {
                    fromEVA = true;
                }
            }
            if (fromEVA && PartFull)
            {
                Utilities.Log_Debug("DeepFreezer EVA kerbal tried to enter a FULL Freezer part, so we kick them out");
                if (ExternalDoorActive)
                {
                    setHelmetstoDoorState();
                    setDoorHandletoDoorState();
                }
                resetFrozenKerbals();
                if (partHasInternals)
                {
                    resetCryopods(true);
                }
                FlightEVA.fetch.spawnEVA(HostedFromTo.host, HostedFromTo.to, HostedFromTo.to.airlock);
                CameraManager.Instance.SetCameraFlight();
            }
            crewTransferInputLock = false;
        }

        private void OnCrewBoardVessel(GameEvents.FromToAction<Part, Part> fromToAction)
        {
            Debug.Log("OnCrewBoardVessel " + vessel.id + " " + part.flightID);
        }

        private void onCrewOnEva(GameEvents.FromToAction<Part, Part> fromToAction)
        {
            Debug.Log("OnCrewOnEva " + vessel.id + " " + part.flightID);
        }

        #endregion CrewXfers

        private void UpdateCounts()
        {
            // Update the part counts.
            try
            {
                if (!IsThawActive && !IsFreezeActive)
                {
                    FreezerSpace = FreezerSize - _StoredCrewList.Count;
                    TotalFrozen = _StoredCrewList.Count;
                    PartFull = TotalFrozen + part.protoModuleCrew.Count >= part.CrewCapacity;
                    //Utilities.Log_Debug("DeepFreezer UpdateCounts FreezerSpace=" + FreezerSpace + ",TotalFrozen=" + TotalFrozen + ",Partfull=" + PartFull);
                    // Reset the seat status for frozen crew to taken - true, because it seems to reset by something?? So better safe than sorry.
                    if (partHasInternals)
                    {
                        // reset seats to TAKEN for all frozen kerbals in the part, check KerbalRef is still in place or re-instantiate it and check frozen kerbals
                        // are not appearing in the Portrait Cameras, if they are remove them.
                        //Utilities.Log_Debug("DeepFreezer StoredCrewList");
                        for (int i = 0; i < _StoredCrewList.Count; i++)
                        //{
                        //    foreach (FrznCrewMbr lst in _StoredCrewList)
                        {
                            part.internalModel.seats[_StoredCrewList[i].SeatIdx].taken = true;
                            seatTakenbyFrznKerbal[_StoredCrewList[i].SeatIdx] = true;                          
                            setCryopodWindowSpecular(_StoredCrewList[i].SeatIdx);
                            
                            ProtoCrewMember kerbal = null;
                            IEnumerator<ProtoCrewMember> enumerator = HighLogic.CurrentGame.CrewRoster.Unowned.GetEnumerator();
                            while (enumerator.MoveNext())
                            {
                                if (enumerator.Current.name == _StoredCrewList[i].CrewName)
                                    kerbal = enumerator.Current;
                            }
                            if (kerbal == null)
                            {
                                Utilities.Log("DeepFreezer Frozen Kerbal " + _StoredCrewList[i].CrewName + " is not found in the currentgame.crewroster.unowned, this should never happen");
                            }
                            else
                            {
                                if (kerbal.KerbalRef == null)  // Check if the KerbalRef is null, as this causes issues with CrewXfers, if it is, respawn it.
                                {
                                     Utilities.Log_Debug("Kerbalref = null");
                                    part.internalModel.seats[_StoredCrewList[i].SeatIdx].crew = kerbal;
                                    part.internalModel.seats[_StoredCrewList[i].SeatIdx].SpawnCrew();  // This spawns the Kerbal and sets the seat.kerbalref
                                    setseatstaticoverlay(part.internalModel.seats[_StoredCrewList[i].SeatIdx]);
                                    kerbal.KerbalRef.InPart = null;
                                    kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                                    kerbal.type = ProtoCrewMember.KerbalType.Unowned;
                                }
                                //Remove them from the GUIManager Portrait cams.
                                DFPortraits.DestroyPortrait(kerbal.KerbalRef);
                                Utilities.setFrznKerbalLayer(part, kerbal, false);  // Double check kerbal is invisible.
                            }
                        }
                    }
                    //Utilities.Log_Debug("DeepFreezer UpdateCounts end");
                }
            }
            catch (Exception ex)
            {
                Utilities.Log("DeepFreezer Error attempting to updatePartCounts, Critical ERROR, Report on the forum");
                Utilities.Log(ex.Message);
            }
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
                         Utilities.Log_Debug("parse cryopodstring " + i + " " + cryopodstatestring[i]);
                        if (cryopodstatestring[i] != string.Empty)
                        {
                            cryopodstateclosed[i] = bool.Parse(cryopodstatestring[i]);
                        }
                    }
                    //Debug.Log("Load cryopodstatepersistent value " + cryopodstateclosedstring);
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
                    string[] tmpCryopodstateclosedarray = new string[cryopodstateclosed.Length];
                    for (int i = 0; i < cryopodstateclosed.Length; i++)
                    {
                        tmpCryopodstateclosedarray[i] = cryopodstateclosed[i].ToString();
                    }
                    cryopodstateclosedstring = string.Join(", ", tmpCryopodstateclosedarray);
                    //Debug.Log("Save cryopodstatepersistent value " + cryopodstateclosedstring);
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
                // we skip this resetcryopods call (so we don't see flickering).
                //Otherwise we set all cryopodstatclosed to true which will force processing further down to open pods that
                // may already be open , regardless.
                if (resetall)
                {
                    double currenttime = Planetarium.GetUniversalTime();
                    if (currenttime - cryopodResetTime < DeepFreeze.Instance.DFsettings.cryopodResettimeDelay)
                    {
                         Utilities.Log_Debug("Last cryopod resetall occurred at: " + cryopodResetTime + " currenttime: " + currenttime + " is less than " + DeepFreeze.Instance.DFsettings.cryopodResettimeDelay + " secs ago, Ignoring request.");
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
                     Utilities.Log_Debug("resetCryopod " + i + " contains frozen kerbal? " + closedpods[i]);
                    if (closedpods[i]) //Pod contains a frozen kerbal
                    {
                        if (!cryopodstateclosed[i])  //If we think the pod is not closed, we close it.
                        {
                             Utilities.Log_Debug("pod is open so close it");
                            if (isPartAnimated)
                                closeCryopod(i, float.MaxValue);
                            cryopodstateclosed[i] = true;
                            freezeCryopodWindow(i, float.MaxValue);
                        }
                        else
                        {
                             Utilities.Log_Debug("pod is already closed");
                            freezeCryopodWindow(i, float.MaxValue);
                        }
                    }
                    else  //Pod does not contain a frozen kerbal
                    {
                        if (cryopodstateclosed[i]) //If we think the pod is closed, we open it.
                        {
                             Utilities.Log_Debug("pod is closed so open it");
                            if (isPartAnimated)
                            {                                
                                openCryopod(i, float.MaxValue);
                            }
                            if (isPartAnimated || (isPodExternal && DFInstalledMods.IsJSITransparentPodsInstalled && _prevRPMTransparentpodSetting == "ON"))
                                thawCryopodWindow(i, float.MaxValue);
                            cryopodstateclosed[i] = false;
                        }
                        else
                        {
                             Utilities.Log_Debug("pod is already open");
                            if (isPartAnimated || (isPodExternal && DFInstalledMods.IsJSITransparentPodsInstalled && _prevRPMTransparentpodSetting == "ON"))
                                thawCryopodWindow(i, float.MaxValue);
                        }
                    }
                    setseatstaticoverlay(part.internalModel.seats[i]);
                }
                savecryopodstatepersistent();
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to reset cryopods in internal model for " + part.vessel.id + " " + part.flightID);
                Debug.Log("Err: " + ex);
            }
        }

        private void openCryopod(int seatIndx, float speed) //only called for animated internal parts
        {
            string podname = "Animated-Cryopod-" + (seatIndx + 1);
            try
            {
                _animation = part.internalModel.FindModelComponent<Animation>(podname);
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
                     Utilities.Log_Debug("animation not found");
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
                windowname = "Animated-Cryopod-" + (seatIndx + 1) + "-Window";
            else
                windowname = "Cryopod-" + (seatIndx + 1) + "-Window";

            _windowAnimation = part.internalModel.FindModelComponent<Animation>(windowname);
            Animation _extwindowAnimation = null;
            if (isPodExternal)
            {
                _extwindowAnimation = part.FindModelComponent<Animation>(windowname);
                External_Window_Occluder = Utilities.SetInternalDepthMask(part, false, "External_Window_Occluder", External_Window_Occluder); //Set window occluder off
            }

            if (_windowAnimation == null)
            {
                 Utilities.Log_Debug("Why can't I find the window animation?");
            }
            else
            {
                _windowAnimation["CryopodWindowOpen"].speed = speed;
                _windowAnimation.Play("CryopodWindowOpen");
            }
            if (isPodExternal && _extwindowAnimation != null)
            {
                _extwindowAnimation["CryopodWindowOpen"].speed = speed;
                _extwindowAnimation.Play("CryopodWindowOpen");
            }
        }

        private void setCryopodWindowOpaque(int seatIndx)
        {
            try
            {
                //Set their Window glass to fully opaque. - Just in case.
                string windowname = "";
                if (isPartAnimated)
                    windowname = "Animated-Cryopod-" + (seatIndx + 1) + "-Window";
                else
                    windowname = "Cryopod-" + (seatIndx + 1) + "-Window";
                
                Renderer windowrenderer = part.internalModel.FindModelComponent<Renderer>(windowname);
                if (windowrenderer != null)
                {
                    windowrenderer.material.shader = TransparentSpecularShader;
                    Color savedwindowcolor = windowrenderer.material.color;
                    savedwindowcolor.a = 1f;
                    windowrenderer.material.color = savedwindowcolor;
                }                
                if (isPodExternal)
                {
                    Renderer extwindowrenderer = part.FindModelComponent<Renderer>(windowname);
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
                    windowname = "Animated-Cryopod-" + (seatIndx + 1) + "-Window";
                else
                    windowname = "Cryopod-" + (seatIndx + 1) + "-Window"; 

                Renderer windowrenderer = part.internalModel.FindModelComponent<Renderer>(windowname);
                Renderer extwindowrenderer = null;
                if (isPodExternal)
                    extwindowrenderer = part.FindModelComponent<Renderer>(windowname);

                if (windowrenderer != null)
                {
                    if (windowrenderer.material.shader != KSPSpecularShader)
                        windowrenderer.material.shader = KSPSpecularShader;
                }
                
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
            string stripname = "lightStrip-Animated-Cryopod-" + (seatIndx + 1);
            // Utilities.Log_Debug("playing animation PodActive " + stripname);
            try
            {
                Animation strip_animation = part.internalModel.FindModelComponent<Animation>(stripname);
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
                     Utilities.Log_Debug("animation PodActive not found for " + stripname);
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
            string podname = "Animated-Cryopod-" + (seatIndx + 1);
            string windowname = "Animated-Cryopod-" + (seatIndx + 1) + "-Window";
             Utilities.Log_Debug("playing animation closecryopod " + podname + " " + windowname);
            try
            {
                _animation = part.internalModel.FindModelComponent<Animation>(podname);
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
                     Utilities.Log_Debug("Cryopod animation not found");
            }
            catch (Exception ex)
            {
                Debug.Log("Unable to find animation in internal model for this part called " + podname);
                Debug.Log("Err: " + ex);
            }
        }

        private void freezeCryopodWindow(int seatIndx, float speed)
        {
            if (isPartAnimated || (isPodExternal && DFInstalledMods.IsJSITransparentPodsInstalled && _prevRPMTransparentpodSetting == "ON"))
                setCryopodWindowTransparent(seatIndx);
            else
                speed = float.MaxValue;
            string windowname = "";
            if (isPartAnimated)
                windowname = "Animated-Cryopod-" + (seatIndx + 1) + "-Window";
            else
                windowname = "Cryopod-" + (seatIndx + 1) + "-Window";

            _windowAnimation = part.internalModel.FindModelComponent<Animation>(windowname);
            Animation _extwindowAnimation = null;
            if (isPodExternal)
            {
                _extwindowAnimation = part.FindModelComponent<Animation>(windowname);
                //Utilities.SetInternalDepthMask(part, true, "External_Window_Occluder"); //Set window occluder visible (block internals)
            }

            if (_windowAnimation == null)
            {
                 Utilities.Log_Debug("Why can't I find the window animation?");
            }
            else
            {
                _windowAnimation["CryopodWindowClose"].speed = speed;
                _windowAnimation.Play("CryopodWindowClose");
            }
            if (isPodExternal && _extwindowAnimation != null)
            {
                _extwindowAnimation["CryopodWindowClose"].speed = speed;
                _extwindowAnimation.Play("CryopodWindowClose");
            }
        }

        private void setCryopodWindowTransparent(int seatIndx)
        {
            try
            {
                //Set their Window glass to see-through. - Just in case.
                string windowname = "";
                if (isPartAnimated)
                    windowname = "Animated-Cryopod-" + (seatIndx + 1) + "-Window";
                else
                    windowname = "Cryopod-" + (seatIndx + 1) + "-Window";
                Renderer windowrenderer = part.internalModel.FindModelComponent<Renderer>(windowname);
                if (windowrenderer != null)
                {
                    windowrenderer.material.shader = TransparentSpecularShader;
                    Color savedwindowcolor = windowrenderer.material.color;
                    savedwindowcolor.a = 0.3f;
                    windowrenderer.material.color = savedwindowcolor;
                }
                
                if (isPodExternal)
                {
                    Renderer extwindowrenderer = part.FindModelComponent<Renderer>(windowname);
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
            string stripname = "lightStrip-Animated-Cryopod-" + (seatIndx + 1);
            // Utilities.Log_Debug("playing animation LightStrip " + stripname);
            try
            {
                Animation strip_animation = part.internalModel.FindModelComponent<Animation>(stripname);
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
                     Utilities.Log_Debug("animation LightStrip not found for " + stripname);
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
            string camname = "FrzCam" + (seatIndx + 1);
            internalSeatIdx = seatIndx;
            Camera cam = part.internalModel.FindModelComponent<Camera>(camname);
            if (cam != null)  //Found Freezer Camera so switch to it.
            {
                Transform camxform = cam.transform;
                if (camxform != null)
                {
                    CameraManager.Instance.SetCameraInternal(part.internalModel, camxform);
                    DFIntMemory.Instance.lastFrzrCam = seatIndx;
                }
            }
            else  //Didn't find Freezer Camera so kick out to flight camera.
            {
                CameraManager.Instance.SetCameraMode(CameraManager.CameraMode.Flight);
            }
             Utilities.Log_Debug("Finished Setting FrzrCam " + camname);
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
                Utilities.Log("DeepFreezer Error attempting to change staticoverlayduration");
                Utilities.Log(ex.Message);
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
                if (externalDoorAnimOccluder != null)
                {
                    externalDoorAnimOccluder[animationName].normalizedTime = 0;
                    externalDoorAnimOccluder[animationName].speed = speed;
                    externalDoorAnimOccluder.Play(animationName);
                }
                externalDoorAnim.Play(animationName);
                IEnumerator wait = Utilities.WaitForAnimation(externalDoorAnim, animationName);
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
                if (externalDoorAnimOccluder != null)
                {
                    externalDoorAnimOccluder[animationName].normalizedTime = 1;
                    externalDoorAnimOccluder[animationName].speed = speed;
                    externalDoorAnimOccluder.Play(animationName);
                }
                externalDoorAnim.Play(animationName);
                IEnumerator wait = Utilities.WaitForAnimation(externalDoorAnim, animationName);
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
                part.setHelmets(false);
            }
            else
            {
                part.setHelmets(true);
            }
        }

        private void setDoorHandletoDoorState()
        {
            if (_externaldoorstate == DoorState.OPEN || _externaldoorstate == DoorState.OPENING)
            {
                try
                {
                    Animation[] animators = part.internalModel.FindModelAnimators("DOORHandle");
                    if (animators.Length > 0)
                    {
                        var anim = animators[0];
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
                if (strexternaldoorstate == "OPEN")
                {
                    _externaldoorstate = DoorState.OPEN;
                }
                else
                {
                    _externaldoorstate = DoorState.CLOSED;
                }
                if (strprevexterndoorstate == "OPEN")
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
                    strexternaldoorstate = "OPEN";
                }
                else
                {
                    strexternaldoorstate = "CLOSED";
                }
                if (_prevexterndoorstate == DoorState.OPEN || _externaldoorstate == DoorState.OPENING)
                { 
                    strprevexterndoorstate = "OPEN";
                }
                else
                {
                    strprevexterndoorstate = "CLOSED";
                }
            }
            else
            {
                strexternaldoorstate = "CLOSED";
                strprevexterndoorstate = "CLOSED";
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
            Utilities.Log_Debug("getdoorState Animation not found");
            return DoorState.UNKNOWN;
        }

        #endregion ExternalDoor

        #region BackgroundProcessing

        private const String MAIN_POWER_NAME = "ElectricCharge";

        //This method is called by the BackgroundProcessing DLL, if the user has installed it. Otherwise it will never be called.
        //It will consume ElectricCharge for Freezer that contain frozen kerbals for vessels that are unloaded, if the user has turned on the ECreqdForFreezer option in the settings menu.
        public static void FixedBackgroundUpdate(Vessel v, uint partFlightID, Func<Vessel, float, string, float> resourceRequest, ref Object data)
        {
            if (Time.timeSinceLevelLoad < 2.0f || CheatOptions.InfiniteElectricity) // Check not loading level
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
                Utilities.Log("DeepFreeze FixedBackgroundUpdate failed to get debug setting");
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
                double timeperiod = currenttime - partInfo.timeLastElectricity;
                if (timeperiod >= 1f && partInfo.numFrznCrew > 0) //We have frozen Kerbals, consume EC
                {
                    double Ecreqd = partInfo.frznChargeRequired / 60.0f * timeperiod * vslinfo.numFrznCrew;
                    if (debug) Debug.Log("FixedBackgroundUpdate timeperiod = " + timeperiod + " frozenkerbals onboard part = " + vslinfo.numFrznCrew + " ECreqd = " + Ecreqd);
                    float Ecrecvd = 0f;
                    Ecrecvd = resourceRequest(v, (float)Ecreqd, MAIN_POWER_NAME);

                    if (debug) Debug.Log("Consumed Freezer EC " + Ecreqd + " units");

                    if (Ecrecvd >= (float)Ecreqd * 0.99)
                    {
                        if (OnGoingECMsg != null) ScreenMessages.RemoveMessage(OnGoingECMsg);
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
                            ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00072"), 10.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00072 = Insufficient electric charge to monitor frozen kerbals.
                            partInfo.ECWarning = true;
                            partInfo.deathCounter = currenttime;
                        }
                        if (OnGoingECMsg != null) ScreenMessages.RemoveMessage(OnGoingECMsg);
                        OnGoingECMsg = ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00073", (deathRoll - (currenttime - partInfo.deathCounter)).ToString("######0"))); //#autoLOC_DF_00073 = \u0020Freezer Out of EC : Systems critical in <<1>> secs
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
                                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00074", deathKerbal), 10.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00074 = <<1>> died due to lack of Electrical Charge to run cryogenics
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
   
}