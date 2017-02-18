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
//using System.Text.RegularExpressions;
using RSTUtils;
using UnityEngine;
//using Random = System.Random;
using KSP.UI.Screens;
using RSTUtils.Extensions;

namespace DF
{
    internal class DeepFreezeGUI : MonoBehaviour, Savable
    {
        //GUI Properties
        internal AppLauncherToolBar DFMenuAppLToolBar;
        private float DFWINDOW_WIDTH = 560;
        //private float CFWINDOW_WIDTH = 340;
        private float KACWINDOW_WIDTH = 485;
        private float VSWINDOW_WIDTH = 340;
        private float WINDOW_BASE_HEIGHT = 350;
        private Rect DFwindowPos;
        //private Rect CFwindowPos;
        private Rect DFKACwindowPos;
        private Rect DFVSwindowPos;
        private Rect DFVSFwindowPos;
        private static int windowID = 199999;
        //private static int CFwindowID = 200000;
        private static int KACwindowID = 2000001;
        private static int VSwindowID = 2000002;
        private static int VSFwindowID = 2000003;
        private Vector2 GUIscrollViewVector, GUIscrollViewVector2,// GUIscrollViewVectorSettings,
            GUIscrollViewVectorKAC, GUIscrollViewVectorKACKerbals = Vector2.zero;
        private bool mouseDownDF;
        private bool mouseDownCF;
        private bool mouseDownKAC;
        private float DFtxtWdthName;
        private float DFtxtWdthProf;
        private float DFtxtWdthVslN;

        private float KACtxtWdthName;
        private float KACtxtWdthAtyp;
        private float KACtxtWdthATme;
        private float KACtxtWdthKName;
        private float KACtxtWdthKTyp;
        private float KACtxtWdthKTg1;
        private float KACtxtWdthKTg2;

        private float DFvslWdthName;
        private float DFvslPrtName;
        private float DFvslPrtTmp;
        private float DFvslPrtElec;
        private float DFvslAlarms;
        private float DFvslLstUpd;
        private float DFvslRT;

        private bool showKACGUI;
        private bool showConfigGUI;
        private bool LoadConfig = true;
        private bool ModKACAlarm;
        private KACWrapper.KACAPI.KACAlarm KACalarmMod;
        private List<string> KACAlarm_FrzKbls = new List<string>();
        private List<string> KACAlarm_ThwKbls = new List<string>();
        
        //SwitchVessel vars
        private bool showUnabletoSwitchVessel;
        private bool showSwitchVessel;
        private bool switchVesselManual;
        private string showSwitchVesselStr = string.Empty;
        private Vessel switchVessel;
        private double switchVesselManualTimer;
        internal bool chgECHeatsettings;
        internal double chgECHeatsettingsTimer;
        private bool switchNextUpdate = false;
        
        public bool Useapplauncher;
        private double currentTime;
        
        internal void OnDestroy()
        {
            DFMenuAppLToolBar.Destroy();
        }

        internal void Start()
        {
            Utilities.Log_Debug("DeepFreezeGUI startup");
            windowID = Utilities.getnextrandomInt();
            KACwindowID = Utilities.getnextrandomInt();
            VSwindowID = Utilities.getnextrandomInt();
            VSFwindowID = Utilities.getnextrandomInt();

            DFwindowPos = new Rect(40, Screen.height / 2 - 100, DFWINDOW_WIDTH, WINDOW_BASE_HEIGHT);
            DFKACwindowPos = new Rect(600, Screen.height / 2 - 100, KACWINDOW_WIDTH, WINDOW_BASE_HEIGHT);
            DFVSwindowPos = new Rect(Screen.width / 2 - VSWINDOW_WIDTH / 2, Screen.height / 2 - 100, VSWINDOW_WIDTH, WINDOW_BASE_HEIGHT);
            DFVSFwindowPos = new Rect(Screen.width / 2 - VSWINDOW_WIDTH / 2, Screen.height / 2 - 100, VSWINDOW_WIDTH, WINDOW_BASE_HEIGHT);
            DFtxtWdthName = Mathf.Round((DFWINDOW_WIDTH - 28f) * .28f);
            DFtxtWdthProf = Mathf.Round((DFWINDOW_WIDTH - 28f) * .2f);
            DFtxtWdthVslN = Mathf.Round((DFWINDOW_WIDTH - 28f) * .28f);

            KACtxtWdthName = Mathf.Round((KACWINDOW_WIDTH - 38f) * .2f);
            KACtxtWdthAtyp = Mathf.Round((KACWINDOW_WIDTH - 38f) * .1f);
            KACtxtWdthATme = Mathf.Round((KACWINDOW_WIDTH - 38f) * .2f);
            KACtxtWdthKName = Mathf.Round((KACWINDOW_WIDTH - 48f) * .2f);
            KACtxtWdthKTyp = Mathf.Round((KACWINDOW_WIDTH - 48f) * .2f);
            KACtxtWdthKTg1 = Mathf.Round((KACWINDOW_WIDTH - 48f) * .16f);
            KACtxtWdthKTg2 = Mathf.Round((KACWINDOW_WIDTH - 48f) * .16f);

            DFvslWdthName = Mathf.Round((DFWINDOW_WIDTH - 28f) * .28f);
            DFvslPrtName = Mathf.Round((DFWINDOW_WIDTH - 28f) * .2f);
            DFvslPrtTmp = Mathf.Round((DFWINDOW_WIDTH - 28f) * .1f);
            DFvslPrtElec = Mathf.Round((DFWINDOW_WIDTH - 28f) * .1f);
            DFvslAlarms = Mathf.Round((DFWINDOW_WIDTH - 28f) * .12f);
            DFvslLstUpd = Mathf.Round((DFWINDOW_WIDTH - 28f) * .18f);
            DFvslRT = Mathf.Round((DFWINDOW_WIDTH - 28f) * .12f);

            Useapplauncher = DeepFreeze.Instance.DFsettings.UseAppLauncher;

            Utilities.setScaledScreen();
            
            DFMenuAppLToolBar = new AppLauncherToolBar("DeepFreeze", "DeepFreeze",
                "REPOSoftTech/DeepFreeze/Icons/DFtoolbar",
                ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT |
                ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB |
                ApplicationLauncher.AppScenes.TRACKSTATION,
                GameDatabase.Instance.GetTexture("REPOSoftTech/DeepFreeze/Icons/DeepFreezeOn", false),
                GameDatabase.Instance.GetTexture("REPOSoftTech/DeepFreeze/Icons/DeepFreezeOff", false),
                GameScenes.FLIGHT, GameScenes.EDITOR, GameScenes.SPACECENTER, GameScenes.TRACKSTATION);

            //If Settings wants to use ToolBar mod, check it is installed and available. If not set the TST Setting to use Stock.
            if (!ToolbarManager.ToolbarAvailable && !Useapplauncher)
            {
                Useapplauncher = true;
            }

            DFMenuAppLToolBar.Start(Useapplauncher);
            
            Utilities.Log_Debug("DeepFreezeGUI END startup");
        }
        
        private void FixedUpdate()
        {
            if (Time.timeSinceLevelLoad < 2f) return; //Wait 2 seconds on level load before executing

            if (chgECHeatsettings)
            {
                currentTime = Planetarium.GetUniversalTime();
                if (currentTime - chgECHeatsettingsTimer > 2)
                {
                    chgECHeatsettings = false;
                }
            }
        }
        
        private void Update()
        {
            if (Time.timeSinceLevelLoad < 2f) return; //Wait 2 seconds on level load before executing
            if (switchNextUpdate)
            {
                //Jump to vessel code here.
                switchNextUpdate = false;
                int intVesselidx = Utilities.getVesselIdx(switchVessel);
                if (intVesselidx < 0)
                {
                    Utilities.Log("Couldn't find the index for the vessel " + switchVessel.vesselName + "(" +
                                  switchVessel.id + ")");
                    showUnabletoSwitchVessel = true;
                }
                else
                {
                    
                    if (HighLogic.LoadedSceneIsFlight)
                    {
                        FlightGlobals.SetActiveVessel(switchVessel);
                    }
                    else
                    {
                        String strret = GamePersistence.SaveGame("DFJumpToShip", HighLogic.SaveFolder,
                            SaveMode.OVERWRITE);
                        Game tmpGame = GamePersistence.LoadGame(strret, HighLogic.SaveFolder, false, false);
                        FlightDriver.StartAndFocusVessel(tmpGame, intVesselidx);
                    }
                }
            }
            if (Useapplauncher == false || !HighLogic.LoadedSceneIsFlight)
            {
                return;
            }
            if (DFIntMemory.Instance != null)
            {
                if (DFIntMemory.Instance.DpFrzrActVsl.Count == 0)
                {
                    DFMenuAppLToolBar.setAppLSceneVisibility(ApplicationLauncher.AppScenes.SPACECENTER |
                                                             ApplicationLauncher.AppScenes.SPH |
                                                             ApplicationLauncher.AppScenes.VAB |
                                                             ApplicationLauncher.AppScenes.TRACKSTATION);
                }
                else
                {
                    DFMenuAppLToolBar.setAppLSceneVisibility(ApplicationLauncher.AppScenes.SPACECENTER |
                                                             ApplicationLauncher.AppScenes.FLIGHT |
                                                             ApplicationLauncher.AppScenes.MAPVIEW |
                                                             ApplicationLauncher.AppScenes.SPH |
                                                             ApplicationLauncher.AppScenes.VAB |
                                                             ApplicationLauncher.AppScenes.TRACKSTATION);
                }
            }
        }

        #region GUI

        private void OnGUI()
        {
            if (!Textures.StylesSet)
                Textures.SetupStyles();

            if (showSwitchVessel)
            {

                DFVSwindowPos.ClampToScreen();
                DFVSwindowPos = GUILayout.Window(VSwindowID, DFVSwindowPos, windowVS, "DeepFreeze Vessel Switch", GUILayout.ExpandWidth(false),
                    GUILayout.ExpandHeight(true), GUILayout.Width(320), GUILayout.MinHeight(100));
            }
            if (showUnabletoSwitchVessel && !switchVesselManual)
            {
                DFVSFwindowPos.ClampToScreen();
                DFVSFwindowPos = GUILayout.Window(VSFwindowID, DFVSFwindowPos, windowVSF, "DeepFreeze Vessel Switch Failed", GUILayout.ExpandWidth(false),
                    GUILayout.ExpandHeight(true), GUILayout.Width(320), GUILayout.MinHeight(100));
            }
            if (switchVesselManual)
            {
                if (Planetarium.GetUniversalTime() - switchVesselManualTimer > 120)
                {
                    switchVesselManualTimer = 0;
                    switchVesselManual = false;
                }
            }
            if (!DFMenuAppLToolBar.GuiVisible || DFMenuAppLToolBar.gamePaused || DFMenuAppLToolBar.hideUI)
            {
                return;
            }
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                GUI.skin = HighLogic.Skin;
                DFwindowPos.ClampInsideScreen();
                DFwindowPos = GUILayout.Window(windowID, DFwindowPos, windowDF, "DeepFreeze Kerbals", GUILayout.ExpandWidth(true),
                        GUILayout.ExpandHeight(true), GUILayout.MinWidth(200), GUILayout.MinHeight(250));
                
                if (showKACGUI)
                {
                    DFKACwindowPos.ClampInsideScreen();
                    DFKACwindowPos = GUILayout.Window(KACwindowID, DFKACwindowPos, windowKAC, "DeepFreeze Alarms", GUILayout.ExpandWidth(true),
                        GUILayout.ExpandHeight(true), GUILayout.MinWidth(360), GUILayout.MinHeight(150));
                }
            }

            if (DeepFreeze.Instance.DFsettings.ToolTips)
                Utilities.DrawToolTip();
        }

        private void windowDF(int id)
        {
            GUIContent closeContent = new GUIContent(Textures.BtnRedCross, "Close Window");
            Rect closeRect = new Rect(DFwindowPos.width - 21, 4, 16, 16);
            if (GUI.Button(closeRect, closeContent, Textures.ClosebtnStyle))
            {
                DFMenuAppLToolBar.onAppLaunchToggle();
                return;
            }

            GUIscrollViewVector2 = GUILayout.BeginScrollView(GUIscrollViewVector2, false, false, GUILayout.MaxHeight(140f));
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Vessel","Vessel Name"), Textures.sectionTitleLeftStyle, GUILayout.Width(DFvslWdthName));
            GUILayout.Label(new GUIContent("Part", "Part Name"), Textures.sectionTitleLeftStyle, GUILayout.Width(DFvslPrtName));
            GUILayout.Label(new GUIContent("Tmp", "Part Temperature Status"), Textures.sectionTitleLeftStyle, GUILayout.Width(DFvslPrtTmp));
            GUILayout.Label(new GUIContent("EC", "Electric Charge Status"), Textures.sectionTitleLeftStyle, GUILayout.Width(DFvslPrtElec));
            if (DFInstalledMods.IsRTInstalled)
                GUILayout.Label(new GUIContent("R.T", "Remote Tech Status"), Textures.sectionTitleLeftStyle, GUILayout.Width(DFvslRT));
            GUILayout.Label(new GUIContent("Alarms", "Press the button for Kerbal Alarm Clock Alarms assigned to this part"), Textures.sectionTitleLeftStyle, GUILayout.Width(DFvslAlarms));
            GUILayout.Label(new GUIContent("LastUpd", "The Time the part was last updated"), Textures.sectionTitleLeftStyle, GUILayout.Width(DFvslLstUpd));
            if (DeepFreeze.Instance.DFsettings.ECreqdForFreezer)
                GUILayout.Label(new GUIContent("TimeRem", "Approx. time remaining before Electric Charge will run out"), Textures.sectionTitleLeftStyle, GUILayout.Width(DFvslLstUpd));
            GUILayout.EndHorizontal();
            foreach (KeyValuePair<uint, PartInfo> frzr in DeepFreeze.Instance.DFgameSettings.knownFreezerParts)
            {
                GUILayout.BeginHorizontal();
                VesselInfo vsl = DeepFreeze.Instance.DFgameSettings.knownVessels[frzr.Value.vesselID];
                GUILayout.Label(vsl.vesselName, Textures.statusStyle, GUILayout.Width(DFvslWdthName));
                string partname = string.Empty;
                if (frzr.Value.PartName.Substring(8, 1) == "R")
                    partname = frzr.Value.PartName.Substring(0, 9);
                else
                    partname = frzr.Value.PartName.Substring(0, 8);
                GUILayout.Label(partname, Textures.statusStyle, GUILayout.Width(DFvslPrtName));
                string TempVar;
                if (DeepFreeze.Instance.DFsettings.TempinKelvin)
                {
                    TempVar = frzr.Value.cabinTemp.ToString("###0") + "K";
                }
                else
                {
                    TempVar = Utilities.KelvintoCelsius(frzr.Value.cabinTemp).ToString("###0") + "C";
                }

                if (DeepFreeze.Instance.DFsettings.RegTempReqd && !chgECHeatsettings)
                {
                    switch (frzr.Value.TmpStatus)
                    {
                        case FrzrTmpStatus.OK:
                            {
                                GUILayout.Label(TempVar, Textures.StatusOKStyle, GUILayout.Width(DFvslPrtTmp));
                                break;
                            }
                        case FrzrTmpStatus.WARN:
                            {
                                GUILayout.Label(TempVar, Textures.StatusWarnStyle, GUILayout.Width(DFvslPrtTmp));
                                break;
                            }
                        case FrzrTmpStatus.RED:
                            {
                                GUILayout.Label(TempVar, Textures.StatusRedStyle, GUILayout.Width(DFvslPrtTmp));
                                switchVessel = FlightGlobals.Vessels.Find(a => a.id == frzr.Value.vesselID);
                                showSwitchVesselStr = "Vessel " + switchVessel.vesselName + " is Over-Heating.";
                                if (HighLogic.LoadedSceneIsFlight)
                                {
                                    if (FlightGlobals.ActiveVessel.id != frzr.Value.vesselID && !switchVesselManual)
                                    {
                                        showSwitchVessel = true;
                                    }
                                }
                                break;
                            }
                    }
                }
                else
                {
                    GUILayout.Label("OFF", Textures.StatusGrayStyle, GUILayout.Width(DFvslPrtTmp));
                }

                if (DeepFreeze.Instance.DFsettings.ECreqdForFreezer && !chgECHeatsettings)
                {
                    if (frzr.Value.numFrznCrew == 0)
                    {
                        GUILayout.Label("S/BY", Textures.StatusOKStyle, GUILayout.Width(DFvslPrtElec));
                    }
                    else
                    {
                        if (frzr.Value.outofEC)
                        {
                            GUILayout.Label("OUT", Textures.StatusRedStyle, GUILayout.Width(DFvslPrtElec));
                            switchVessel = FlightGlobals.Vessels.Find(a => a.id == frzr.Value.vesselID);
                            showSwitchVesselStr = "Vessel " + switchVessel.vesselName + " is out of ElectricCharge.\n Situation Critical.";
                            if (HighLogic.LoadedSceneIsFlight)
                            {
                                if (FlightGlobals.ActiveVessel.id != frzr.Value.vesselID && !switchVesselManual)
                                {
                                    showSwitchVessel = true;
                                }
                            }
                        }
                        else
                        {
                            if (vsl.predictedECOut < DeepFreeze.Instance.DFsettings.EClowCriticalTime)
                            {
                                GUILayout.Label("ALRT", Textures.StatusRedStyle, GUILayout.Width(DFvslPrtElec));
                                switchVessel = FlightGlobals.Vessels.Find(a => a.id == frzr.Value.vesselID);
                                showSwitchVesselStr = "Vessel " + switchVessel.vesselName + " is almost out of ElectricCharge.";
                                if (HighLogic.LoadedSceneIsFlight)
                                {
                                    if (FlightGlobals.ActiveVessel.id != frzr.Value.vesselID && !switchVesselManual)
                                    {
                                        showSwitchVessel = true;
                                    }
                                }
                            }
                            else
                            {
                                if (vsl.predictedECOut < DeepFreeze.Instance.DFsettings.ECLowWarningTime)  // ONE HOUR OF EC WARNING
                                {
                                    // Utilities.Log_Debug("Remaining EC time " + vsl.predictedECOut);
                                    GUILayout.Label("LOW", Textures.StatusWarnStyle, GUILayout.Width(DFvslPrtElec));
                                }
                                else
                                {
                                    GUILayout.Label("OK", Textures.StatusOKStyle, GUILayout.Width(DFvslPrtElec));
                                }
                            }
                        }
                    }
                }
                else
                {
                    GUILayout.Label("OFF", Textures.StatusGrayStyle, GUILayout.Width(DFvslPrtElec));
                }

                if (DFInstalledMods.IsRTInstalled)
                {
                    if (DFInstalledMods.RTVesselConnected(frzr.Value.vesselID))
                    {
                        GUILayout.Label("OK", Textures.StatusOKStyle, GUILayout.Width(DFvslRT));
                    }
                    else
                    {
                        GUILayout.Label("NC", Textures.StatusRedStyle, GUILayout.Width(DFvslRT));
                    }
                }

                //if (DeepFreeze.Instance.DFgameSettings.knownKACAlarms.Where(e => (e.Value.VesselID == frzr.Value.vesselID) &&
                //((e.Value.FrzKerbals.Count() > 0) || (e.Value.ThwKerbals.Count() > 0))).Count() > 0)
                if (DeepFreeze.Instance.DFgameSettings.knownKACAlarms.Any(e => e.Value.VesselID == frzr.Value.vesselID))
                {
                    //GUILayout.Label("Active", StatusOKStyle, GUILayout.Width(DFvslAlarms));
                    if (GUILayout.Button(new GUIContent("Alarm", "Go to Alarms"), GUILayout.Width(DFvslAlarms)))
                    {
                        showKACGUI = !showKACGUI;
                    }
                }
                else
                {
                    GUILayout.Label("    ", Textures.StatusGrayStyle, GUILayout.Width(DFvslAlarms));
                }

                if (HighLogic.LoadedSceneIsFlight)
                {
                    if (frzr.Value.vesselID == FlightGlobals.ActiveVessel.id)
                    {
                        GUILayout.Label("    ", Textures.StatusOKStyle, GUILayout.Width(DFvslLstUpd));
                    }
                    else
                    {
                        if (DeepFreeze.Instance.DFsettings.ECreqdForFreezer && !chgECHeatsettings)
                        {
                            if (vsl.predictedECOut < DeepFreeze.Instance.DFsettings.EClowCriticalTime)
                            {
                                GUILayout.Label(Utilities.FormatDateString(Planetarium.GetUniversalTime() - frzr.Value.timeLastElectricity), Textures.StatusRedStyle, GUILayout.Width(DFvslLstUpd));
                            }
                            else
                            {
                                if (vsl.predictedECOut < DeepFreeze.Instance.DFsettings.ECLowWarningTime)
                                {
                                    GUILayout.Label(Utilities.FormatDateString(Planetarium.GetUniversalTime() - frzr.Value.timeLastElectricity), Textures.StatusWarnStyle, GUILayout.Width(DFvslLstUpd));
                                }
                                else
                                {
                                    GUILayout.Label(Utilities.FormatDateString(Planetarium.GetUniversalTime() - frzr.Value.timeLastElectricity), Textures.StatusOKStyle, GUILayout.Width(DFvslLstUpd));
                                }
                            }
                        }
                        else
                        {
                            GUILayout.Label(Utilities.FormatDateString(Planetarium.GetUniversalTime() - frzr.Value.timeLastElectricity), Textures.StatusGrayStyle, GUILayout.Width(DFvslLstUpd));
                        }
                    }
                }
                else
                {
                    if (DeepFreeze.Instance.DFsettings.ECreqdForFreezer && !chgECHeatsettings)
                    {
                        if (vsl.predictedECOut < DeepFreeze.Instance.DFsettings.EClowCriticalTime)
                        {
                            GUILayout.Label(Utilities.FormatDateString(Planetarium.GetUniversalTime() - frzr.Value.timeLastElectricity), Textures.StatusRedStyle, GUILayout.Width(DFvslLstUpd));
                        }
                        else
                        {
                            if (vsl.predictedECOut < DeepFreeze.Instance.DFsettings.ECLowWarningTime)
                            {
                                GUILayout.Label(Utilities.FormatDateString(Planetarium.GetUniversalTime() - frzr.Value.timeLastElectricity), Textures.StatusWarnStyle, GUILayout.Width(DFvslLstUpd));
                            }
                            else
                            {
                                GUILayout.Label(Utilities.FormatDateString(Planetarium.GetUniversalTime() - frzr.Value.timeLastElectricity), Textures.StatusOKStyle, GUILayout.Width(DFvslLstUpd));
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Label(Utilities.FormatDateString(Planetarium.GetUniversalTime() - frzr.Value.timeLastElectricity), Textures.StatusGrayStyle, GUILayout.Width(DFvslLstUpd));
                    }
                }

                if (DeepFreeze.Instance.DFsettings.ECreqdForFreezer && !chgECHeatsettings)
                {
                    if (vsl.predictedECOut < DeepFreeze.Instance.DFsettings.EClowCriticalTime)
                    {
                        GUILayout.Label(Utilities.FormatDateString(vsl.predictedECOut), Textures.StatusRedStyle, GUILayout.Width(DFvslLstUpd));
                    }
                    else
                    {
                        if (vsl.predictedECOut < DeepFreeze.Instance.DFsettings.ECLowWarningTime)
                        {
                            GUILayout.Label(Utilities.FormatDateString(vsl.predictedECOut), Textures.StatusWarnStyle, GUILayout.Width(DFvslLstUpd));
                        }
                        else
                        {
                            GUILayout.Label(Utilities.FormatDateString(vsl.predictedECOut), Textures.StatusOKStyle, GUILayout.Width(DFvslLstUpd));
                        }
                    }
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            bool Headers = false;
            GUIscrollViewVector = GUILayout.BeginScrollView(GUIscrollViewVector, false, false);
            GUILayout.BeginVertical();
            if (DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Count == 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("There are currently no Frozen Kerbals", Textures.frozenStyle);
                GUILayout.EndHorizontal();
            }
            else
            {
                Headers = true;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Kerbal Name", Textures.sectionTitleLeftStyle, GUILayout.Width(DFtxtWdthName));
                GUILayout.Label("Profession", Textures.sectionTitleLeftStyle, GUILayout.Width(DFtxtWdthProf));
                GUILayout.Label("Vessel Name", Textures.sectionTitleLeftStyle, GUILayout.Width(DFtxtWdthVslN));
                GUILayout.EndHorizontal();
                List<KeyValuePair<string, KerbalInfo>> ThawKeysToDelete = new List<KeyValuePair<string, KerbalInfo>>();
                foreach (KeyValuePair<string, KerbalInfo> kerbal in DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals)
                {
                    GUILayout.BeginHorizontal();
                    GUIStyle dispstyle = kerbal.Value.type != ProtoCrewMember.KerbalType.Tourist ? Textures.frozenStyle : Textures.comaStyle;
                    GUILayout.Label(kerbal.Key, dispstyle, GUILayout.Width(DFtxtWdthName));
                    GUILayout.Label(kerbal.Value.experienceTraitName, dispstyle, GUILayout.Width(DFtxtWdthProf));
                    GUILayout.Label(kerbal.Value.vesselName, dispstyle, GUILayout.Width(DFtxtWdthVslN));
                    if (kerbal.Value.type != ProtoCrewMember.KerbalType.Tourist)
                    {
                        if (HighLogic.LoadedScene == GameScenes.FLIGHT && DFIntMemory.Instance.ActVslHasDpFrezr)
                        //if in flight and active vessel has a Freezer part check if kerbal is part of this vessel and add a Thaw button to the GUI
                        {
                            //foreach (DeepFreezer frzr in DFIntMemory.Instance.DpFrzrActVsl)
                            //{
                            //if (frzr.DFIStoredCrewList.FirstOrDefault(a => a.CrewName == kerbal.Key) != null)
                            if (kerbal.Value.vesselID == FlightGlobals.ActiveVessel.id && kerbal.Value.type != ProtoCrewMember.KerbalType.Tourist)
                            {
                                if (DFInstalledMods.IsRTInstalled && !DFInstalledMods.RTVesselConnected(DFIntMemory.Instance.ActVslID))
                                {
                                    GUI.enabled = false;
                                }
                                if (GUILayout.Button(new GUIContent("Thaw", "Thaw this Kerbal"), GUILayout.Width(50f)))
                                {
                                    DeepFreezer frzr = DFIntMemory.Instance.DpFrzrActVsl.FirstOrDefault(a => a.part.flightID == kerbal.Value.partID);
                                    if (frzr != null)
                                        frzr.beginThawKerbal(kerbal.Key);
                                }
                                GUI.enabled = true;
                            }
                            //}
                        }
                        if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
                        {
                            if (GUILayout.Button(new GUIContent("Thaw", "Thaw this Kerbal"), GUILayout.Width(50f)))
                            {
                                // We need to check kerbal isn't in a vessel still out there somewhere....
                                Vessel vessel = FlightGlobals.Vessels.Find(v => v.id == kerbal.Value.vesselID);
                                if (vessel != null)
                                {
                                    Utilities.Log_Debug("Cannot thaw, vessel still exists " + vessel.situation + " at " + vessel.mainBody.bodyName);
                                    ScreenMessages.PostScreenMessage("Cannot thaw " + kerbal.Key + " from KSC. Vessel still exists " + vessel.situation + " at " + vessel.mainBody.bodyName, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                                }
                                else
                                {
                                    ThawKeysToDelete.Add(new KeyValuePair<string, KerbalInfo>(kerbal.Key, kerbal.Value));
                                }
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                    //}
                }
                foreach (KeyValuePair<string, KerbalInfo> entries in ThawKeysToDelete)
                {
                    DeepFreeze.Instance.ThawFrozenCrew(entries.Key, entries.Value.vesselID);
                }
            }

            if (HighLogic.LoadedScene == GameScenes.FLIGHT && DFIntMemory.Instance.ActVslHasDpFrezr)
            {
                if (!Headers)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Kerbal Name", Textures.sectionTitleLeftStyle, GUILayout.Width(DFtxtWdthName));
                    GUILayout.Label("Profession", Textures.sectionTitleLeftStyle, GUILayout.Width(DFtxtWdthProf));
                    GUILayout.Label("Vessel Name", Textures.sectionTitleLeftStyle, GUILayout.Width(DFtxtWdthVslN));
                    GUILayout.EndHorizontal();
                    Headers = true;
                }
                foreach (DeepFreezer frzr in DFIntMemory.Instance.DpFrzrActVsl)
                {
                    List<ProtoCrewMember> crew = new List<ProtoCrewMember>();
                    for (int i = 0; i < frzr.part.protoModuleCrew.Count; i++)
                    {
                        if (!DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.ContainsKey(frzr.part.protoModuleCrew[i].name))
                        {
                            crew.Add(frzr.part.protoModuleCrew[i]);
                        }
                    }
                    for (int i =0; i < crew.Count; i++)
                    { 
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(crew[i].name, Textures.statusStyle, GUILayout.Width(DFtxtWdthName));
                        GUILayout.Label(crew[i].experienceTrait.Title, Textures.statusStyle, GUILayout.Width(DFtxtWdthProf));
                        GUILayout.Label(frzr.part.vessel.vesselName, Textures.statusStyle, GUILayout.Width(DFtxtWdthVslN));
                        
                        if (frzr.DFIcrewXferFROMActive || frzr.DFIcrewXferTOActive || (DFInstalledMods.IsSMInstalled && frzr.IsCrewXferRunning)
                                                    || frzr.IsFreezeActive || frzr.IsThawActive || (DFInstalledMods.IsRTInstalled && !DFInstalledMods.RTVesselConnected(DFIntMemory.Instance.ActVslID)))
                        {
                            GUI.enabled = false;
                        }
                        if (GUILayout.Button(new GUIContent("Freeze", "Freeze this Kerbal"), GUILayout.Width(50f)))
                        {
                            frzr.beginFreezeKerbal(crew[i]);
                        }
                        GUI.enabled = true;
                        
                        GUILayout.EndHorizontal();
                    }
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(24);
            if (KACWrapper.AssemblyExists && KACWrapper.InstanceExists && KACWrapper.APIReady)
            {
                GUIContent AlarmsContent = new GUIContent("Alarms", "KAC Alarms");
                Rect AlarmsRect = new Rect(DFwindowPos.width - 95, DFwindowPos.height - 22, 70, 20);
                if (GUI.Button(AlarmsRect, AlarmsContent))
                {
                    showKACGUI = !showKACGUI;
                }
            }
            
            GUIContent resizeContent = new GUIContent(Textures.BtnResize, "Resize Window");
            Rect resizeRect = new Rect(DFwindowPos.width - 21, DFwindowPos.height - 22, 16, 16);
            GUI.Label(resizeRect, resizeContent, Textures.ResizeStyle);

            HandleResizeEventsDF(resizeRect);

            if (DeepFreeze.Instance.DFsettings.ToolTips)
                Utilities.SetTooltipText();

            GUI.DragWindow();
        }
        
        private void windowKAC(int id)
        {
            GUIContent closeContent = new GUIContent(Textures.BtnRedCross, "Close Window");
            Rect closeRect = new Rect(DFKACwindowPos.width - 21, 4, 16, 16);
            if (GUI.Button(closeRect, closeContent, Textures.ClosebtnStyle))
            {
                showKACGUI = false;
                return;
            }

            // Utilities.Log_Debug("start WindowKAC ModKacAlarm active=" + ModKACAlarm);

            //Draw the alarms that KAC has that are for the CURRENT Vessel ONLY, so ONLY in FLIGHT mode.
            GUIscrollViewVectorKAC = GUILayout.BeginScrollView(GUIscrollViewVectorKAC, false, false);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Name", "Alarm Name"), Textures.sectionTitleLeftStyle, GUILayout.Width(KACtxtWdthName));
            GUILayout.Label(new GUIContent("Alarm Type", "KAC Alarm Type"), Textures.sectionTitleLeftStyle, GUILayout.Width(KACtxtWdthAtyp));
            GUILayout.Label(new GUIContent("Time Remain.", "Time remaining before Alarm is triggered"), Textures.sectionTitleLeftStyle, GUILayout.Width(KACtxtWdthATme));
            GUILayout.EndHorizontal();
            if (KACWrapper.KAC.Alarms.Count == 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("There are currently no KAC alarms associated to a DeepFreeze vessel", Textures.frozenStyle);
                GUILayout.EndHorizontal();
            }
            else
            {
                foreach (KACWrapper.KACAPI.KACAlarm alarm in KACWrapper.KAC.Alarms)
                {
                    //Only show KAC alarms that are in the DeepFreeze known vessels list. (IE: vessels that have a freezer)
                    if (alarm.VesselID == "" || alarm.AlarmType == KACWrapper.KACAPI.AlarmTypeEnum.Crew)
                    {
                        continue;
                    }
                    Guid tmpid = new Guid(alarm.VesselID);
                    if (DeepFreeze.Instance.DFgameSettings.knownVessels.ContainsKey(tmpid))
                    {
                        GUILayout.BeginHorizontal();
                        double TmeRemaining = Math.Round(alarm.AlarmTime - Planetarium.GetUniversalTime(), 0);

                        if (TmeRemaining <= 5)
                        {
                            GUILayout.Label(alarm.Name, Textures.StatusWarnStyle, GUILayout.Width(KACtxtWdthName));
                            GUILayout.Label(alarm.AlarmType.ToString(), Textures.StatusWarnStyle, GUILayout.Width(KACtxtWdthAtyp));
                            GUILayout.Label(Utilities.FormatDateString(TmeRemaining), Textures.StatusWarnStyle, GUILayout.Width(KACtxtWdthATme));
                        }
                        else
                        {
                            GUILayout.Label(alarm.Name, Textures.statusStyle, GUILayout.Width(KACtxtWdthName));
                            GUILayout.Label(alarm.AlarmType.ToString(), Textures.statusStyle, GUILayout.Width(KACtxtWdthAtyp));
                            GUILayout.Label(Utilities.FormatDateString(TmeRemaining), Textures.statusStyle, GUILayout.Width(KACtxtWdthATme));
                        }
                        // Utilities.Log_Debug("Show alarm  from KAC " + alarm.ID + " " + alarm.Name + " " + alarm.VesselID);

                        //Option to delete each alarm
                        if (ModKACAlarm || (DFInstalledMods.IsRTInstalled && !DFInstalledMods.RTVesselConnected(tmpid)))
                        {
                            //If a modify is in progress we turn off the delete button
                            GUI.enabled = false;
                            GUILayout.Button(new GUIContent("Delete", "Delete this KAC alarm completely"), GUILayout.Width(50));
                            GUI.enabled = true;
                            // Utilities.Log_Debug("Delete button disabled");
                        }
                        else
                        {
                            if (TmeRemaining <= 0) GUI.enabled = false;
                            if (GUILayout.Button(new GUIContent("Delete", "Delete this KAC alarm completely"), GUILayout.Width(50)))
                            {
                                KACWrapper.KAC.DeleteAlarm(alarm.ID);
                            }
                            GUI.enabled = true;
                        }

                        //Option to modify
                        if (ModKACAlarm) // If a Modify is in progress
                        {
                            if (KACalarmMod.ID != alarm.ID) //If it isn't this alarm we disable the button
                            {
                                GUI.enabled = false;
                                GUILayout.Button(new GUIContent("Modify", "Modify this Alarm"), GUILayout.Width(50));
                                GUI.enabled = true;
                                // Utilities.Log_Debug("Modify button disabled");
                            }
                            else //We are modifying an alarm and it's this one. So we draw a SAVE and Cancel button to save/cancel changes.
                            {
                                // Utilities.Log_Debug("mod in progress and it's this one, change to Save/Cancel");
                                if (GUILayout.Button(new GUIContent("Save", "Save Alarm Changes"), GUILayout.Width(50)))
                                {
                                    if (DFInstalledMods.IsRTInstalled && !DFInstalledMods.RTVesselConnected(tmpid))
                                    {
                                        ScreenMessages.PostScreenMessage("Cannot Save Alarm. No R/Tech Connection to vessel.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                                    }
                                    else
                                    {
                                        if (TmeRemaining > 0)
                                        {
                                            DFIntMemory.Instance.ModifyKACAlarm(KACalarmMod, KACAlarm_FrzKbls, KACAlarm_ThwKbls);
                                            ScreenMessages.PostScreenMessage("DeepFreeze Alarm changes Saved.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                                            // Utilities.Log_Debug("DF KAC Modified alarm " + KACalarmMod.ID + " " + KACalarmMod.Name);
                                        }
                                        else
                                        {
                                            ScreenMessages.PostScreenMessage("DeepFreeze Cannot Save alarm changes, Time is up.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                                            Utilities.Log_Debug("DF KAC Couldn't save Modified alarm time is up");
                                        }
                                    }
                                    ModKACAlarm = false;
                                }
                                if (GUILayout.Button(new GUIContent("Cancel", "Cancel any changes"), GUILayout.Width(50)))
                                {
                                    // Utilities.Log_Debug("User cancelled mod");
                                    ModKACAlarm = false;
                                }
                                GUILayout.EndHorizontal();
                                GUIscrollViewVectorKACKerbals = GUILayout.BeginScrollView(GUIscrollViewVectorKACKerbals, false, false, GUILayout.MaxHeight(100f));
                                GUILayout.BeginVertical();
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(new GUIContent("Name", "The Kerbals Name"), Textures.sectionTitleLeftStyle, GUILayout.Width(KACtxtWdthKName));
                                GUILayout.Label(new GUIContent("Trait", "The Kerbals Profession"), Textures.sectionTitleLeftStyle, GUILayout.Width(KACtxtWdthKTyp));
                                GUILayout.Label(new GUIContent("Thaw", "Thaw this kerbal on alarm activation"), Textures.sectionTitleLeftStyle, GUILayout.Width(KACtxtWdthKTg1));
                                GUILayout.Label(new GUIContent("Freeze", "Freeze this kerbal on alarm activation"), Textures.sectionTitleLeftStyle, GUILayout.Width(KACtxtWdthKTg2));
                                GUILayout.EndHorizontal();
                                //Build the Crew list for the alarm and allow modifications
                                List<KeyValuePair<uint, PartInfo>> frzrs = DeepFreeze.Instance.DFgameSettings.knownFreezerParts.Where(a => a.Value.vesselID == tmpid).ToList();
                                //foreach (DeepFreezer frzr in DFIntMemory.Instance.DpFrzrActVsl)
                                foreach (KeyValuePair<uint, PartInfo> frzr in frzrs)
                                {
                                    //Thawed Crew List
                                    for (int i = 0; i < frzr.Value.crewMembers.Count; i++)
                                    {
                                        GUILayout.BeginHorizontal();
                                        bool ThawCrew = KACAlarm_ThwKbls.Contains(frzr.Value.crewMembers[i]);
                                        bool FrzCrew = KACAlarm_FrzKbls.Contains(frzr.Value.crewMembers[i]);
                                        GUILayout.Label(frzr.Value.crewMembers[i], Textures.statusStyle, GUILayout.Width(KACtxtWdthKName));
                                        GUILayout.Label(frzr.Value.crewMemberTraits[i], Textures.statusStyle, GUILayout.Width(KACtxtWdthKTyp));
                                        if (FrzCrew) GUI.enabled = false;
                                        ThawCrew = GUILayout.Toggle(ThawCrew, "", Textures.ButtonStyle, GUILayout.Width(KACtxtWdthKTg1));
                                        GUI.enabled = true;
                                        if (ThawCrew)
                                        {
                                            if (!KACAlarm_ThwKbls.Contains(frzr.Value.crewMembers[i]))
                                            {
                                                KACAlarm_ThwKbls.Add(frzr.Value.crewMembers[i]);
                                            }
                                        }
                                        else
                                        {
                                            if (KACAlarm_ThwKbls.Contains(frzr.Value.crewMembers[i]))
                                            {
                                                KACAlarm_ThwKbls.Remove(frzr.Value.crewMembers[i]);
                                            }
                                        }
                                        if (ThawCrew) GUI.enabled = false;
                                        FrzCrew = GUILayout.Toggle(FrzCrew, "", Textures.ButtonStyle, GUILayout.Width(KACtxtWdthKTg2));
                                        GUI.enabled = true;

                                        if (FrzCrew)
                                        {
                                            if (!KACAlarm_FrzKbls.Contains(frzr.Value.crewMembers[i]))
                                            {
                                                KACAlarm_FrzKbls.Add(frzr.Value.crewMembers[i]);
                                            }
                                        }
                                        else
                                        {
                                            if (KACAlarm_FrzKbls.Contains(frzr.Value.crewMembers[i]))
                                            {
                                                KACAlarm_FrzKbls.Remove(frzr.Value.crewMembers[i]);
                                            }
                                        }

                                        GUILayout.EndHorizontal();
                                    }
                                    //Frozen Crew List
                                    List<KeyValuePair<string, KerbalInfo>> frzncrew = DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Where(f => f.Value.partID == frzr.Key && f.Value.type != ProtoCrewMember.KerbalType.Tourist).ToList();
                                    foreach (KeyValuePair<string, KerbalInfo> crew in frzncrew)
                                    {
                                        GUILayout.BeginHorizontal();
                                        bool ThawCrew = KACAlarm_ThwKbls.Contains(crew.Key);
                                        bool FrzCrew = KACAlarm_FrzKbls.Contains(crew.Key);
                                        GUILayout.Label(crew.Key, Textures.frozenStyle, GUILayout.Width(KACtxtWdthKName));
                                        GUILayout.Label(crew.Value.experienceTraitName, Textures.frozenStyle, GUILayout.Width(KACtxtWdthKTyp));
                                        if (FrzCrew) GUI.enabled = false;
                                        ThawCrew = GUILayout.Toggle(ThawCrew, "", Textures.ButtonStyle, GUILayout.Width(KACtxtWdthKTg1));
                                        GUI.enabled = true;
                                        if (ThawCrew)
                                        {
                                            if (!KACAlarm_ThwKbls.Contains(crew.Key))
                                            {
                                                KACAlarm_ThwKbls.Add(crew.Key);
                                            }
                                        }
                                        else
                                        {
                                            if (KACAlarm_ThwKbls.Contains(crew.Key))
                                            {
                                                KACAlarm_ThwKbls.Remove(crew.Key);
                                            }
                                        }
                                        if (ThawCrew) GUI.enabled = false;
                                        FrzCrew = GUILayout.Toggle(FrzCrew, "", Textures.ButtonStyle, GUILayout.Width(KACtxtWdthKTg2));
                                        GUI.enabled = true;
                                        if (FrzCrew)
                                        {
                                            if (!KACAlarm_FrzKbls.Contains(crew.Key))
                                            {
                                                KACAlarm_FrzKbls.Add(crew.Key);
                                            }
                                        }
                                        else
                                        {
                                            if (KACAlarm_FrzKbls.Contains(crew.Key))
                                            {
                                                KACAlarm_FrzKbls.Remove(crew.Key);
                                            }
                                        }
                                        GUILayout.EndHorizontal();
                                    }
                                }
                                GUILayout.EndVertical();
                                GUILayout.EndScrollView();
                                continue;
                            }
                        }
                        else  // no modify is in progress so we draw modify buttons on all alarms
                        {
                            // Utilities.Log_Debug("no modify in progress so just show modify buttons on KAC alarm");
                            if (TmeRemaining <= 0) GUI.enabled = false;
                            if (GUILayout.Button(new GUIContent("Modify", "Modify this Alarms settings"), GUILayout.Width(50)))
                            {
                                KACalarmMod = alarm;
                                KACAlarm_FrzKbls.Clear();
                                KACAlarm_ThwKbls.Clear();
                                string tmpnotes = DFIntMemory.Instance.ParseKACNotes(alarm.Notes, out KACAlarm_FrzKbls, out KACAlarm_ThwKbls);
                                ModKACAlarm = true;
                                // Utilities.Log_Debug("Modify in progress " + alarm.ID);
                            }
                            GUI.enabled = true;
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.Space(14);

            GUIContent resizeContent = new GUIContent(Textures.BtnResize, "Resize Window");
            Rect resizeRect = new Rect(DFKACwindowPos.width - 17, DFKACwindowPos.height - 17, 16, 16);
            GUI.Label(resizeRect, resizeContent, Textures.ResizeStyle);
            
            HandleResizeEventsKAC(resizeRect);
            if (DeepFreeze.Instance.DFsettings.ToolTips)
                Utilities.SetTooltipText();
            GUI.DragWindow();
        }

        private void windowVS(int id)
        {
            //Pause the game
            TimeWarp.SetRate(0, true);
            if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && !FlightDriver.Pause)
                FlightDriver.SetPause(true);
            
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            //GUILayout.Box(new GUIContent("ElectricCharge is running out on vessel, you must switch to the vessel now.", "Switch to DeepFreeze vessel required"), statusStyle, GUILayout.Width(280));
            GUILayout.Box(new GUIContent(showSwitchVesselStr, showSwitchVesselStr), Textures.statusStyle, GUILayout.Width(320));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Switch to Vessel", "Switch to Vessel"), GUILayout.Width(160)))
            {
                showSwitchVessel = false;
                if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready)
                    FlightDriver.SetPause(false);
                switchNextUpdate = true;
            }
            if (GUILayout.Button(new GUIContent("Not Now", "Don't switch vessel now"), GUILayout.Width(160)))
            {
                showSwitchVessel = false;
                switchVesselManual = true;
                switchVesselManualTimer = Planetarium.GetUniversalTime();
                if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready)
                    FlightDriver.SetPause(false);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            if (DeepFreeze.Instance.DFsettings.ToolTips)
                Utilities.SetTooltipText();
            GUI.DragWindow();
        }

        private void windowVSF(int id)
        {
            TimeWarp.SetRate(0, true);
            if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready)
                FlightDriver.SetPause(true);

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Box(new GUIContent("Automatic Switch to vessel failed.\nPlease switch manually to vessel Immediately", "Switch to DeepFreeze vessel required"), Textures.statusStyle, GUILayout.Width(280));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("OK", "OK")))
            {
                showSwitchVessel = false;
                showUnabletoSwitchVessel = false;
                switchVesselManual = true;
                switchVesselManualTimer = Planetarium.GetUniversalTime();
                if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready)
                    FlightDriver.SetPause(false);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            if (DeepFreeze.Instance.DFsettings.ToolTips)
                Utilities.SetTooltipText();
            GUI.DragWindow();
        }

        private void HandleResizeEventsDF(Rect resizeRect)
        {
            var theEvent = Event.current;
            if (theEvent != null)
            {
                if (!mouseDownDF)
                {
                    if (theEvent.type == EventType.MouseDown && theEvent.button == 0 && resizeRect.Contains(theEvent.mousePosition))
                    {
                        mouseDownDF = true;
                        theEvent.Use();
                    }
                }
                else if (theEvent.type != EventType.Layout)
                {
                    if (Input.GetMouseButton(0))
                    {
                        // Flip the mouse Y so that 0 is at the top
                        float mouseY = Screen.height - Input.mousePosition.y;

                        DFwindowPos.width = Mathf.Clamp(Input.mousePosition.x - DFwindowPos.x + resizeRect.width / 2, 50, Screen.width - DFwindowPos.x);
                        DFwindowPos.height = Mathf.Clamp(mouseY - DFwindowPos.y + resizeRect.height / 2, 50, Screen.height - DFwindowPos.y);
                        DFtxtWdthName = Mathf.Round((DFWINDOW_WIDTH - 28f) * .28f);
                        DFtxtWdthProf = Mathf.Round((DFWINDOW_WIDTH - 28f) * .2f);
                        DFtxtWdthVslN = Mathf.Round((DFWINDOW_WIDTH - 28f) * .28f);
                        DFvslWdthName = Mathf.Round((DFWINDOW_WIDTH - 28f) * .28f);
                        DFvslPrtName = Mathf.Round((DFWINDOW_WIDTH - 28f) * .2f);
                        DFvslPrtTmp = Mathf.Round((DFWINDOW_WIDTH - 28f) * .1f);
                        DFvslPrtElec = Mathf.Round((DFWINDOW_WIDTH - 28f) * .1f);
                        DFvslAlarms = Mathf.Round((DFWINDOW_WIDTH - 28f) * .12f);
                        DFvslLstUpd = Mathf.Round((DFWINDOW_WIDTH - 28f) * .18f);
                        DFvslRT = Mathf.Round((DFWINDOW_WIDTH - 28f) * .12f);
                    }
                    else
                    {
                        mouseDownDF = false;
                    }
                }
            }
        }
        
        private void HandleResizeEventsKAC(Rect resizeRect)
        {
            var theEvent = Event.current;
            if (theEvent != null)
            {
                if (!mouseDownKAC)
                {
                    if (theEvent.type == EventType.MouseDown && theEvent.button == 0 && resizeRect.Contains(theEvent.mousePosition))
                    {
                        mouseDownKAC = true;
                        theEvent.Use();
                    }
                }
                else if (theEvent.type != EventType.Layout)
                {
                    if (Input.GetMouseButton(0))
                    {
                        // Flip the mouse Y so that 0 is at the top
                        float mouseY = Screen.height - Input.mousePosition.y;

                        DFKACwindowPos.width = Mathf.Clamp(Input.mousePosition.x - DFKACwindowPos.x + resizeRect.width / 2, 50, Screen.width - DFKACwindowPos.x);
                        DFKACwindowPos.height = Mathf.Clamp(mouseY - DFKACwindowPos.y + resizeRect.height / 2, 50, Screen.height - DFKACwindowPos.y);
                        KACtxtWdthName = Mathf.Round((KACWINDOW_WIDTH - 38f) * .2f);
                        KACtxtWdthAtyp = Mathf.Round((KACWINDOW_WIDTH - 38f) * .1f);
                        KACtxtWdthATme = Mathf.Round((KACWINDOW_WIDTH - 38f) * .2f);
                        KACtxtWdthKName = Mathf.Round((KACWINDOW_WIDTH - 48f) * .2f);
                        KACtxtWdthKTyp = Mathf.Round((KACWINDOW_WIDTH - 48f) * .2f);
                        KACtxtWdthKTg1 = Mathf.Round((KACWINDOW_WIDTH - 48f) * .16f);
                        KACtxtWdthKTg2 = Mathf.Round((KACWINDOW_WIDTH - 48f) * .16f);
                    }
                    else
                    {
                        mouseDownKAC = false;
                    }
                }
            }
        }

        #endregion GUI

        #region Savable

        //Class Load and Save of global settings
        public void Load(ConfigNode globalNode)
        {
            Utilities.Log_Debug("DeepFreezeGUI Load");
            DFwindowPos.x = DeepFreeze.Instance.DFsettings.DFwindowPosX;
            DFwindowPos.y = DeepFreeze.Instance.DFsettings.DFwindowPosY;
            DFKACwindowPos.x = DeepFreeze.Instance.DFsettings.DFKACwindowPosX;
            DFKACwindowPos.y = DeepFreeze.Instance.DFsettings.DFKACwindowPosY;
            DFWINDOW_WIDTH = DeepFreeze.Instance.DFsettings.DFWindowWidth;
            KACWINDOW_WIDTH = DeepFreeze.Instance.DFsettings.KACWindowWidth;
            WINDOW_BASE_HEIGHT = DeepFreeze.Instance.DFsettings.WindowbaseHeight;
            Utilities.Log_Debug("DeepFreezeGUI Load end");
        }

        public void Save(ConfigNode globalNode)
        {
            Utilities.Log_Debug("DeepFreezeGUI Save");
            DeepFreeze.Instance.DFsettings.DFwindowPosX = DFwindowPos.x;
            DeepFreeze.Instance.DFsettings.DFwindowPosY = DFwindowPos.y;
            DeepFreeze.Instance.DFsettings.DFKACwindowPosX = DFKACwindowPos.x;
            DeepFreeze.Instance.DFsettings.DFKACwindowPosY = DFKACwindowPos.y;
            Utilities.Log_Debug("DeepFreezeGUI Save end");
        }

        #endregion Savable
    }
}