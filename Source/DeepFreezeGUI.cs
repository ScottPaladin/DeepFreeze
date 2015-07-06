/**
 * DeepFreezeGUI.cs
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
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DF
{
    public class DeepFreezeGUI : MonoBehaviour, Savable
    {
        private List<DeepFreezer> DpFrzrActVsl = new List<DeepFreezer>();
        private bool ActVslHasDpFrezr = false;
        
        //GUI Properties
        private bool gamePaused = false;
        private IButton button1;
        private ApplicationLauncherButton stockToolbarButton = null; // Stock Toolbar Button
        private const float DFWINDOW_WIDTH = 400;
        private const float CFWINDOW_WIDTH = 320;
        private const float WINDOW_BASE_HEIGHT = 350;
        public Rect DFwindowPos = new Rect(40, Screen.height / 2 - 100, DFWINDOW_WIDTH, WINDOW_BASE_HEIGHT);
        public Rect CFwindowPos = new Rect(450, Screen.height / 2 - 100, CFWINDOW_WIDTH, 250);
        private static int windowID = 199999;
        private static int CFwindowID = 200000;
        private GUIStyle statusStyle, frozenStyle, sectionTitleStyle, resizeStyle, StatusOKStyle, StatusWarnStyle, StatusRedStyle;
        private Vector2 GUIscrollViewVector, GUIscrollViewVector2 = Vector2.zero;
        private bool mouseDown = false;              
        private float txtWdthName = Mathf.Round((DFWINDOW_WIDTH - 28f) / 3.5f);
        private float txtWdthProf = Mathf.Round((DFWINDOW_WIDTH - 28f) / 4.8f);
        private float txtWdthVslN = Mathf.Round((DFWINDOW_WIDTH - 28f) / 3.5f);
           
        private bool showConfigGUI = false;
        private bool LoadConfig = true;
        //settings vars for GUI
        private bool InputVautoRecover = false;
        private bool InputVdebug = false;
        private bool InputVECReqd = false;
        private bool InputAppL = true;
        private string InputScostThawKerbal = "";
        private float InputVcostThawKerbal = 0f;
        private string InputSecReqdToFreezeThaw = "";
        private int InputVecReqdToFreezeThaw = 0;
        private string InputSglykerolReqdToFreeze = "";
        private int InputVglykerolReqdToFreeze = 0;
        private bool InputVRegTempReqd = false;
        private string InputSRegTempFreeze = "";
        private double InputVRegTempFreeze = 0f;
        private string InputSRegTempMonitor = "";
        private double InputVRegTempMonitor = 0f;
        private string InputSheatamtMonitoringFrznKerbals = "";
        private double InputVheatamtMonitoringFrznKerbals = 0f;
        private string InputSheatamtThawFreezeKerbal = "";
        private double InputVheatamtThawFreezeKerbal = 0f;
        private bool InputVTempinKelvin = true;

        //Settings vars
        private bool ECreqdForFreezer;
        private bool debugging;
        private bool AutoRecoverFznKerbals;
        private float KSCcostToThawKerbal;
        private int ECReqdToFreezeThaw;
        private int GlykerolReqdToFreeze;
        private bool RegTempReqd;
        private double RegTempFreeze;
        private double RegtempMonitor;
        private double heatamtMonitoringFrznKerbals;
        private double heatamtThawFreezeKerbal;
        private bool TempinKelvin;
        
        //GuiVisibility
        private bool _Visible = false;

        public Boolean GuiVisible
        {
            get { return _Visible; }
            set
            {
                _Visible = value;      //Set the private variable
                if (_Visible)
                {
                    RenderingManager.AddToPostDrawQueue(3, this.onDraw);
                }
                else
                {
                    RenderingManager.RemoveFromPostDrawQueue(3, this.onDraw);
                }
            }
        }

        //DeepFreeze Savable settings
        private DFSettings DFsettings;
        private DFGameSettings DFgameSettings;
        public bool Useapplauncher = false;
                
       internal void Awake()
        {
            DFsettings = DeepFreeze.Instance.DFsettings;
            DFgameSettings = DeepFreeze.Instance.DFgameSettings;
        }

        #region AppLauncher

        private void OnGUIAppLauncherReady()
        {
            this.Log_Debug("OnGUIAppLauncherReady");
            if (ApplicationLauncher.Ready)
            {
                this.Log_Debug("Adding AppLauncherButton");
                this.stockToolbarButton = ApplicationLauncher.Instance.AddModApplication(
                    onAppLaunchToggle, 
                    onAppLaunchToggle, 
                    DummyVoid,
                    DummyVoid, 
                    DummyVoid, 
                    DummyVoid, 
                    ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT |
                                          ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB |
                                          ApplicationLauncher.AppScenes.TRACKSTATION,
                                          (Texture)GameDatabase.Instance.GetTexture("REPOSoftTech/DeepFreeze/Icons/DeepFreezeOff", false));
            }
        }

        private void DummyVoid()
        {
        }

        private void onAppLaunchToggle()
        {
            GuiVisible = !GuiVisible;
            this.stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(GuiVisible ? "REPOSoftTech/DeepFreeze/Icons/DeepFreezeOn" : "REPOSoftTech/DeepFreeze/Icons/DeepFreezeOff", false));            
        }

        #endregion AppLauncher

        internal void OnDestroy()
        {
            
                if (ToolbarManager.ToolbarAvailable && Useapplauncher == false)
                {
                    button1.Destroy();
                }
                else
                {
                    // Set up the stock toolbar
                    this.Log_Debug("Removing onGUIAppLauncher callbacks");
                    GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
                    if (this.stockToolbarButton != null)
                    {
                        ApplicationLauncher.Instance.RemoveModApplication(this.stockToolbarButton);
                        this.stockToolbarButton = null;
                    }
                }
                if (GuiVisible) GuiVisible = !GuiVisible;
                GameEvents.onGamePause.Remove(GamePaused);
                GameEvents.onGameUnpause.Remove(GameUnPaused);
            
        }

        internal void Start()
        {
            this.Log_Debug("DeepFreezeGUI startup");
            windowID = new System.Random().Next();
            CFwindowID = windowID + 1;
            // create toolbar button

            if (ToolbarManager.ToolbarAvailable && Useapplauncher == false)
            {
                button1 = ToolbarManager.Instance.add("DeepFreeze", "button1");
                button1.TexturePath = "REPOSoftTech/DeepFreeze/Icons/DFtoolbar";
                button1.ToolTip = "DeepFreeze";
                button1.Visibility = new GameScenesVisibility(GameScenes.FLIGHT, GameScenes.EDITOR, GameScenes.SPACECENTER, GameScenes.TRACKSTATION);
                button1.OnClick += (e) => GuiVisible = !GuiVisible;
            }
            else
            {
                // Set up the stock toolbar
                this.Log_Debug("Adding onGUIAppLauncher callbacks");
                if (ApplicationLauncher.Ready)
                {
                    OnGUIAppLauncherReady();
                }
                else
                    GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
            }
            ChkUnknownFrozenKerbals();
            ChkActiveFrozenKerbals();
            DFgameSettings.DmpKnownFznKerbals();
            GameEvents.onGamePause.Add(GamePaused);
            GameEvents.onGameUnpause.Add(GameUnPaused);
            this.Log_Debug("DeepFreezeGUI END startup");
        }
          
        private void GamePaused()
        {
            gamePaused = true;
        }

        private void GameUnPaused()
        {
            gamePaused = false;
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

                        if (!DFgameSettings.KnownFrozenKerbals.ContainsKey(CrewMember.name))
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
                                DFgameSettings.KnownFrozenKerbals.Add(CrewMember.name, kerbalInfo);
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
            // Check the roster list for any crew kerbals that we think are frozen and delete them.
            List<ProtoCrewMember> crewkerbals = HighLogic.CurrentGame.CrewRoster.Crew.ToList();
            if (crewkerbals != null)
            {
                this.Log("There are " + crewkerbals.Count() + " crew Kerbals in the game roster.");
                foreach (ProtoCrewMember CrewMember in crewkerbals)
                {                    
                        if (!DFgameSettings.KnownFrozenKerbals.ContainsKey(CrewMember.name))
                        {
                            // Remove the saved frozen kerbals dictionary                            
                            try
                            {
                                this.Log_Debug("Removing crew kerbal " + CrewMember.name + " from DeepFreeze List");
                                DFgameSettings.KnownFrozenKerbals.Remove(CrewMember.name);
                            }
                            catch (Exception ex)
                            {
                                this.Log("Removal of crew kerbal " + CrewMember.name + " from frozen list failed " + ex);
                            }

                        }                    
                }
            }
        }

        internal void Update()
        {
                        
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                //chk if current active vessel Has one or more DeepFreezer modules attached                
                if (FlightGlobals.ActiveVessel.FindPartModulesImplementing<DeepFreezer>().Count() == 0)
                {
                    ActVslHasDpFrezr = false;
                }
                else
                {
                    ActVslHasDpFrezr = true;
                    DpFrzrActVsl = FlightGlobals.ActiveVessel.FindPartModulesImplementing<DeepFreezer>();
                }                
            }
            
            //Update Vessel names for all frozen kerbals
            List<string> frznKerbalkeys = new List<string>(DFgameSettings.KnownFrozenKerbals.Keys);
            foreach (string key in frznKerbalkeys)
            {
                KerbalInfo kerbalinfo = DFgameSettings.KnownFrozenKerbals[key];
                Vessel vessel = FlightGlobals.Vessels.Find(v => v.id == kerbalinfo.vesselID);
                if (vessel != null)
                {
                    kerbalinfo.vesselName = vessel.vesselName;
                    this.Log_Debug("Updating Frozen Kerbal " + key + ",VesselName=" + kerbalinfo.vesselName + ",VesselID=" + kerbalinfo.vesselID);
                    DFgameSettings.KnownFrozenKerbals[key] = kerbalinfo;
                }
            }
            
        }

        #region GUI
        private void onDraw()
        {
            if (!GuiVisible) return;

            if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                
                if (!gamePaused)
                {
                    GUI.skin = HighLogic.Skin;
                    if (!Utilities.WindowVisibile(DFwindowPos))
                        Utilities.MakeWindowVisible(DFwindowPos);                    
                    DFwindowPos = GUILayout.Window(windowID, DFwindowPos, windowDF, "DeepFreeze Kerbals", GUILayout.ExpandWidth(true),
                            GUILayout.ExpandHeight(true), GUILayout.MinWidth(200), GUILayout.MinHeight(250));
                    if (showConfigGUI)
                    {
                        if (LoadConfig)
                        {
                            InputAppL = Useapplauncher;
                            InputVECReqd = ECreqdForFreezer;
                            InputVdebug = debugging;
                            InputVautoRecover = AutoRecoverFznKerbals;
                            InputScostThawKerbal = KSCcostToThawKerbal.ToString();
                            InputSecReqdToFreezeThaw = ECReqdToFreezeThaw.ToString();
                            InputSglykerolReqdToFreeze = GlykerolReqdToFreeze.ToString();
                            InputVRegTempReqd = RegTempReqd;                            
                            InputSRegTempFreeze = RegTempFreeze.ToString();
                            InputSRegTempMonitor = RegtempMonitor.ToString();
                            InputSheatamtMonitoringFrznKerbals = heatamtMonitoringFrznKerbals.ToString();
                            InputSheatamtThawFreezeKerbal = heatamtThawFreezeKerbal.ToString();
                            InputVTempinKelvin = TempinKelvin;
                            LoadConfig = false;                            
                        }
                        if (!Utilities.WindowVisibile(CFwindowPos))
                            Utilities.MakeWindowVisible(CFwindowPos);    
                        CFwindowPos = GUILayout.Window(CFwindowID, CFwindowPos, windowCF, "DeepFreeze Settings", GUILayout.ExpandWidth(false),
                            GUILayout.ExpandHeight(true), GUILayout.Width(320), GUILayout.MinHeight(100));
                    }
                        
                }
            }
        }

        private void windowDF(int id)
        {
            
            //Init styles
            sectionTitleStyle = new GUIStyle(GUI.skin.label);
            sectionTitleStyle.alignment = TextAnchor.MiddleLeft;
            sectionTitleStyle.stretchWidth = true;
            sectionTitleStyle.normal.textColor = Color.blue;
            sectionTitleStyle.fontStyle = FontStyle.Bold;

            statusStyle = new GUIStyle(GUI.skin.label);
            statusStyle.alignment = TextAnchor.MiddleLeft;
            statusStyle.stretchWidth = true;
            statusStyle.normal.textColor = Color.white;

            frozenStyle = new GUIStyle(GUI.skin.label);
            frozenStyle.alignment = TextAnchor.MiddleLeft;
            frozenStyle.stretchWidth = true;
            frozenStyle.normal.textColor = Color.cyan;

            StatusOKStyle = new GUIStyle(GUI.skin.label);
            StatusOKStyle.alignment = TextAnchor.MiddleLeft;
            StatusOKStyle.stretchWidth = true;
            StatusOKStyle.normal.textColor = Color.green;

            StatusWarnStyle = new GUIStyle(GUI.skin.label);
            StatusWarnStyle.alignment = TextAnchor.MiddleLeft;
            StatusWarnStyle.stretchWidth = true;
            StatusWarnStyle.normal.textColor = Color.yellow;

            StatusRedStyle = new GUIStyle(GUI.skin.label);
            StatusRedStyle.alignment = TextAnchor.MiddleLeft;
            StatusRedStyle.stretchWidth = true;
            StatusRedStyle.normal.textColor = Color.red;

            resizeStyle = new GUIStyle(GUI.skin.button);
            resizeStyle.alignment = TextAnchor.MiddleCenter;
            resizeStyle.padding = new RectOffset(1, 1, 1, 1);


            GUIContent closeContent = new GUIContent("X", "Close Window");
            Rect closeRect = new Rect(DFwindowPos.width - 17, 4, 16, 16);
            if (GUI.Button(closeRect, closeContent))
            {
                onAppLaunchToggle();
                return;
            }
            

            if ((DFsettings.ECreqdForFreezer || DFsettings.RegTempReqd) && (HighLogic.LoadedScene == GameScenes.FLIGHT && ActVslHasDpFrezr))
            {
                GUIscrollViewVector2 = GUILayout.BeginScrollView(GUIscrollViewVector2, false, false, GUILayout.MaxHeight(80f));
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Part", sectionTitleStyle, GUILayout.Width(txtWdthName));
                if (DFsettings.RegTempReqd)
                {
                    GUILayout.Label("Temp.", sectionTitleStyle, GUILayout.Width(txtWdthProf));
                }
                if (DFsettings.ECreqdForFreezer)
                {
                    GUILayout.Label("Elec.Charge", sectionTitleStyle, GUILayout.Width(txtWdthVslN));
                }
                GUILayout.EndHorizontal();
                foreach (DeepFreezer frzr in DpFrzrActVsl)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(frzr.name, statusStyle, GUILayout.Width(txtWdthName));
                    if (DFsettings.RegTempReqd)
                    {
                        string TempVar;
                        if (DFsettings.TempinKelvin)
                        {
                            TempVar = frzr.CabinTemp.ToString("####0") + "K";
                        }
                        else
                        {
                            TempVar = Utilities.KelvintoCelsius((float)frzr.CabinTemp).ToString("####0") + "C";
                        }
                        switch (frzr.DFIFrzrTmp)
                        {
                            case FrzrTmpStatus.OK:
                                GUILayout.Label(TempVar, StatusOKStyle, GUILayout.Width(txtWdthProf));
                                break;
                            case FrzrTmpStatus.WARN:
                                GUILayout.Label(TempVar, StatusWarnStyle, GUILayout.Width(txtWdthProf));
                                break;
                            case FrzrTmpStatus.RED:
                                GUILayout.Label(TempVar, StatusRedStyle, GUILayout.Width(txtWdthProf));
                                break;
                        }       
                    }                   
                    if (DFsettings.ECreqdForFreezer)
                    {
                        if (frzr.DFIFreezerOutofEC)
                        {
                            GUILayout.Label("EC = OUT", StatusRedStyle, GUILayout.Width(txtWdthVslN));
                        }
                        else
                        {
                            GUILayout.Label("EC = OK", StatusOKStyle, GUILayout.Width(txtWdthVslN));
                        }       
                    }                                                    
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
            }
            bool Headers = false;
            GUIscrollViewVector = GUILayout.BeginScrollView(GUIscrollViewVector, false, false);
            GUILayout.BeginVertical();            
            if (DFgameSettings.KnownFrozenKerbals.Count == 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("There are currently no Frozen Kerbals", frozenStyle);
                GUILayout.EndHorizontal();
            }
            else
            {
                Headers = true;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Kerbal Name", sectionTitleStyle, GUILayout.Width(txtWdthName));
                GUILayout.Label("Profession", sectionTitleStyle, GUILayout.Width(txtWdthProf));
                GUILayout.Label("Vessel Name", sectionTitleStyle, GUILayout.Width(txtWdthVslN));
                GUILayout.EndHorizontal();
                List<KeyValuePair<string, KerbalInfo>> ThawKeysToDelete = new List<KeyValuePair<string, KerbalInfo>>();
                foreach (KeyValuePair<string, KerbalInfo> kerbal in DFgameSettings.KnownFrozenKerbals)                
                {                        
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(kerbal.Key, frozenStyle, GUILayout.Width(txtWdthName));
                        GUILayout.Label(kerbal.Value.experienceTraitName, frozenStyle, GUILayout.Width(txtWdthProf));
                        GUILayout.Label(kerbal.Value.vesselName, frozenStyle, GUILayout.Width(txtWdthVslN));
                        if (HighLogic.LoadedScene == GameScenes.FLIGHT && ActVslHasDpFrezr)
                        //if in flight and active vessel has a Freezer part check if kerbal is part of this vessel and add a Thaw button to the GUI
                        {
                            foreach (DeepFreezer frzr in DpFrzrActVsl)
                            {                                
                                if (frzr.DFIStoredCrewList.FirstOrDefault(a => a.CrewName == kerbal.Key) != null)
                                {
                                    if (frzr.DFIcrewXferFROMActive || frzr.DFIcrewXferTOActive || (DFInstalledMods.SMInstalled && frzr.IsSMXferRunning())
                                        || frzr.IsFreezeActive || frzr.IsThawActive)
                                    {
                                        GUI.enabled = false;
                                    }
                                    if (GUILayout.Button(new GUIContent("Thaw", "Thaw this Kerbal"), GUILayout.Width(50f)))
                                    {                                        
                                        frzr.beginThawKerbal(kerbal.Key);
                                    }
                                    GUI.enabled = true;
                                }
                            }
                        }
                        if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
                        {
                            if (GUILayout.Button(new GUIContent("Thaw", "Thaw this Kerbal"), GUILayout.Width(50f)))
                            {
                                // We need to check kerbal isn't in a vessel still out there somewhere....
                                Vessel vessel = FlightGlobals.Vessels.Find(v => v.id == kerbal.Value.vesselID);
                                if (vessel != null)
                                {
                                    this.Log_Debug("Cannot thaw, vessel still exists " + vessel.situation.ToString() + " at " + vessel.mainBody.bodyName);
                                    ScreenMessages.PostScreenMessage("Cannot thaw " + kerbal.Key + " vessel still exists " + vessel.situation.ToString() + " at " + vessel.mainBody.bodyName, 5.0f, ScreenMessageStyle.UPPER_CENTER); 
                                }
                                else
                                {                                   
                                    //DeepFreeze.Instance.ThawFrozenCrew(kerbal.Key, kerbal.Value.vesselID); 
                                    ThawKeysToDelete.Add(new KeyValuePair<string, KerbalInfo>(kerbal.Key, kerbal.Value));                                     
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

            if (HighLogic.LoadedScene == GameScenes.FLIGHT && ActVslHasDpFrezr)
            {
                
                if (!Headers)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Kerbal Name", sectionTitleStyle, GUILayout.Width(txtWdthName));
                    GUILayout.Label("Profession", sectionTitleStyle, GUILayout.Width(txtWdthProf));
                    GUILayout.Label("Vessel Name", sectionTitleStyle, GUILayout.Width(txtWdthVslN));
                    GUILayout.EndHorizontal();
                    Headers = true;
                }
                foreach (DeepFreezer frzr in DpFrzrActVsl)
                {
                    foreach (ProtoCrewMember crewMember in frzr.part.protoModuleCrew.FindAll(a => a.type == ProtoCrewMember.KerbalType.Crew))
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(crewMember.name, statusStyle, GUILayout.Width(txtWdthName));
                        GUILayout.Label(crewMember.experienceTrait.Title, statusStyle, GUILayout.Width(txtWdthProf));
                        GUILayout.Label(frzr.part.vessel.vesselName, statusStyle, GUILayout.Width(txtWdthVslN));
                        if (frzr.DFIcrewXferFROMActive || frzr.DFIcrewXferTOActive || (DFInstalledMods.SMInstalled && frzr.IsSMXferRunning())
                            || frzr.IsFreezeActive || frzr.IsThawActive)                        
                        {
                            GUI.enabled = false;
                        }
                        if (GUILayout.Button(new GUIContent("Freeze", "Freeze this Kerbal"), GUILayout.Width(50f)))
                        {
                            frzr.beginFreezeKerbal(crewMember);
                        }
                        GUI.enabled = true;
                        GUILayout.EndHorizontal();
                    }
                }                              
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                GUILayout.Space(14);
                GUIContent settingsContent = new GUIContent("Settings", "Config Settings");
                Rect settingsRect = new Rect(DFwindowPos.width - 90, DFwindowPos.height - 17, 70, 16);
                if (GUI.Button(settingsRect, settingsContent))
                {
                    showConfigGUI = !showConfigGUI;                    
                }                
            }
            else
            {
                GUILayout.Space(14);
            }            
            

            
            GUIContent resizeContent = new GUIContent("R", "Resize Window");
            Rect resizeRect = new Rect(DFwindowPos.width - 17, DFwindowPos.height - 17, 16, 16);
            GUI.Label(resizeRect, resizeContent, resizeStyle);
            HandleResizeEvents(resizeRect);                       

            GUI.DragWindow();
            
        }

        private void windowCF(int id)
        {
            
            //Init styles
            sectionTitleStyle = new GUIStyle(GUI.skin.label);
            sectionTitleStyle.alignment = TextAnchor.MiddleCenter;
            sectionTitleStyle.stretchWidth = true;
            sectionTitleStyle.fontStyle = FontStyle.Bold;

            statusStyle = new GUIStyle(GUI.skin.label);
            statusStyle.alignment = TextAnchor.MiddleLeft;
            statusStyle.stretchWidth = true;
            statusStyle.normal.textColor = Color.white;            
            
            GUIContent closeContent = new GUIContent("X", "Close Window");
            Rect closeRect = new Rect(CFwindowPos.width - 17, 4, 16, 16);
            if (GUI.Button(closeRect, closeContent))
            {
                showConfigGUI = false;
                LoadConfig = true;
                return;
            }
            
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Box(new GUIContent("ElectricCharge Required to run Freezers", "If on, EC is required to run freezers"), statusStyle, GUILayout.Width(280));
            InputVECReqd = GUILayout.Toggle(InputVECReqd, "", GUILayout.MinWidth(30.0F)); //you can play with the width of the text box
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Box(new GUIContent("AutoRecover Frozen Kerbals at KSC", "If on, will AutoRecover Frozen Kerbals at the KSC and deduct the Cost from your funds"), statusStyle, GUILayout.Width(280));
            InputVautoRecover = GUILayout.Toggle(InputVautoRecover, "", GUILayout.MinWidth(30.0F)); //you can play with the width of the text box
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Box(new GUIContent("Cost to Thaw a Kerbal at KSC", "Amt of currency Reqd to Freeze a Kerbal from the KSC"), statusStyle, GUILayout.Width(250));
            InputScostThawKerbal = Regex.Replace(GUILayout.TextField(InputScostThawKerbal, 5, GUILayout.MinWidth(30.0F)), "[^.0-9]", "");  //you can play with the width of the text box
            GUILayout.EndHorizontal();            

            if (!float.TryParse(InputScostThawKerbal, out InputVcostThawKerbal))
                {
                    InputVcostThawKerbal = KSCcostToThawKerbal;
                }           
                        
            GUILayout.BeginHorizontal();
            GUILayout.Box(new GUIContent("ElecCharge Reqd to Freeze/Thaw a Kerbal", "Amt of ElecCharge Reqd to Freeze/Thaw a Kerbal"), statusStyle, GUILayout.Width(250));
            InputSecReqdToFreezeThaw = Regex.Replace(GUILayout.TextField(InputSecReqdToFreezeThaw, 5, GUILayout.MinWidth(30.0F)), "[^.0-9]", "");  //you can play with the width of the text box
            GUILayout.EndHorizontal();

            if (!int.TryParse(InputSecReqdToFreezeThaw, out InputVecReqdToFreezeThaw))
            {
                InputVecReqdToFreezeThaw = ECReqdToFreezeThaw;
            }
                        
            GUILayout.BeginHorizontal();
            GUILayout.Box(new GUIContent("Glykerol Reqd to Freeze a Kerbal", "Amt of Glykerol used to Freeze a Kerbal, Overrides Part values"), statusStyle, GUILayout.Width(250));
            InputSglykerolReqdToFreeze = Regex.Replace(GUILayout.TextField(InputSglykerolReqdToFreeze, 5, GUILayout.MinWidth(30.0F)), "[^.0-9]", "");  //you can play with the width of the text box
            GUILayout.EndHorizontal();

            if (!int.TryParse(InputSglykerolReqdToFreeze, out InputVglykerolReqdToFreeze))
            {
                InputVglykerolReqdToFreeze = GlykerolReqdToFreeze;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Box(new GUIContent("Temps are in (K)elvin. (K) = (C)elcius + 273.15. (K) = ((F)arenheit + 459.67) × 5/9", "Get your calculator out"), statusStyle, GUILayout.Width(280));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Box(new GUIContent("Regulated Temperatures Required", "If on, Regulated Temps apply to freeze and keep Kerbals Frozen"), statusStyle, GUILayout.Width(280));
            InputVRegTempReqd = GUILayout.Toggle(InputVRegTempReqd, "", GUILayout.MinWidth(30.0F)); //you can play with the width of the text box
            GUILayout.EndHorizontal();

            if (!InputVRegTempReqd) GUI.enabled = false;
            GUILayout.BeginHorizontal();
            GUILayout.Box(new GUIContent("Minimum Part Temp. for Freezer to Freeze (K)", "Minimum part temperature for Freezer to be able to Freeze"), statusStyle, GUILayout.Width(250));
            InputSRegTempFreeze = Regex.Replace(GUILayout.TextField(InputSRegTempFreeze, 3, GUILayout.MinWidth(30.0F)), "[^.0-9]", "");  //you can play with the width of the text box
            GUILayout.EndHorizontal();
            if (!double.TryParse(InputSRegTempFreeze, out InputVRegTempFreeze))
            {
                InputVRegTempFreeze = RegTempFreeze;
            }
                       
            GUILayout.BeginHorizontal();
            GUILayout.Box(new GUIContent("maximum Part Temp. to Keep Kerbals Frozen (K)", "Maximum part temperature for Freezer to keep Kerbals frozen"), statusStyle, GUILayout.Width(250));
            InputSRegTempMonitor = Regex.Replace(GUILayout.TextField(InputSRegTempMonitor, 3, GUILayout.MinWidth(30.0F)), "[^.0-9]", "");  //you can play with the width of the text box
            GUILayout.EndHorizontal();
            if (!double.TryParse(InputSRegTempMonitor, out InputVRegTempMonitor))
            {
                InputVRegTempMonitor = RegtempMonitor;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Box(new GUIContent("Heat generated in thaw/freeze process (kW)", "Amount of thermal heat (kW) generated with each thaw/freeze process"), statusStyle, GUILayout.Width(250));
            InputSheatamtThawFreezeKerbal = Regex.Replace(GUILayout.TextField(InputSheatamtThawFreezeKerbal, 3, GUILayout.MinWidth(30.0F)), "[^.0-9]", "");  //you can play with the width of the text box
            GUILayout.EndHorizontal();
            if (!double.TryParse(InputSheatamtThawFreezeKerbal, out InputVheatamtThawFreezeKerbal))
            {
                InputVheatamtThawFreezeKerbal = heatamtThawFreezeKerbal;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Box(new GUIContent("Equip. heat generated per frozen kerbal (kW)", "Amount of thermal heat (kW) generated by equip. for each frozen kerbal"), statusStyle, GUILayout.Width(250));
            InputSheatamtMonitoringFrznKerbals = Regex.Replace(GUILayout.TextField(InputSheatamtMonitoringFrznKerbals, 3, GUILayout.MinWidth(30.0F)), "[^.0-9]", "");  //you can play with the width of the text box
            GUILayout.EndHorizontal();
            if (!double.TryParse(InputSheatamtMonitoringFrznKerbals, out InputVheatamtMonitoringFrznKerbals))
            {
                InputVheatamtMonitoringFrznKerbals = heatamtMonitoringFrznKerbals;
            }
            
            GUI.enabled = true;

            GUILayout.BeginHorizontal();
            GUILayout.Box(new GUIContent("Show Part Temperature in Kelvin", "If on Part right click will show temp in Kelvin, if Off will show in Celcius"), statusStyle, GUILayout.Width(280));
            InputVTempinKelvin = GUILayout.Toggle(InputVTempinKelvin, "", GUILayout.MinWidth(30.0F)); //you can play with the width of the text box
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            GUILayout.Box(new GUIContent("Use Application Launcher (restart required)", "If on uses AppLauncher, If Off Uses Toolbar"), statusStyle, GUILayout.Width(280));
            InputAppL = GUILayout.Toggle(InputAppL, "", GUILayout.MinWidth(30.0F)); //you can play with the width of the text box
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Box("Debug Mode", statusStyle, GUILayout.Width(280));
            InputVdebug = GUILayout.Toggle(InputVdebug, "", GUILayout.MinWidth(30.0F)); //you can play with the width of the text box
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Save & Exit Settings", "Save & Exit Settings"), GUILayout.Width(155f)))
            {
                Useapplauncher = InputAppL;
                ECreqdForFreezer = InputVECReqd;
                debugging = InputVdebug;
                AutoRecoverFznKerbals = InputVautoRecover;
                KSCcostToThawKerbal = InputVcostThawKerbal;
                ECReqdToFreezeThaw = InputVecReqdToFreezeThaw;
                GlykerolReqdToFreeze = InputVglykerolReqdToFreeze;
                RegTempReqd = InputVRegTempReqd;
                RegTempFreeze = InputVRegTempFreeze;
                RegtempMonitor = InputVRegTempMonitor;
                heatamtMonitoringFrznKerbals = InputVheatamtMonitoringFrznKerbals;
                heatamtThawFreezeKerbal = InputVheatamtThawFreezeKerbal;
                TempinKelvin = InputVTempinKelvin;
                showConfigGUI = false;
                LoadConfig = true;
            }
            if (GUILayout.Button(new GUIContent("Reset Settings", "Reset Settings"), GUILayout.Width(155f)))
            {
                LoadConfig = true;
            }
            GUILayout.EndHorizontal();                                           
            GUILayout.EndVertical();                                                    
            GUI.DragWindow();                       
        }

        private void HandleResizeEvents(Rect resizeRect)
        {
            var theEvent = Event.current;
            if (theEvent != null)
            {
                if (!mouseDown)
                {
                    if (theEvent.type == EventType.MouseDown && theEvent.button == 0 && resizeRect.Contains(theEvent.mousePosition))
                    {
                        mouseDown = true;
                        theEvent.Use();
                    }
                }
                else if (theEvent.type != EventType.Layout)
                {
                    if (Input.GetMouseButton(0))
                    {
                        // Flip the mouse Y so that 0 is at the top
                        float mouseY = Screen.height - Input.mousePosition.y;

                        DFwindowPos.width = Mathf.Clamp(Input.mousePosition.x - DFwindowPos.x + (resizeRect.width / 2), 50, Screen.width - DFwindowPos.x);
                        DFwindowPos.height = Mathf.Clamp(mouseY - DFwindowPos.y + (resizeRect.height / 2), 50, Screen.height - DFwindowPos.y);
                        txtWdthName = Mathf.Round((DFwindowPos.width - 28f) / 3.5f);
                        txtWdthProf = Mathf.Round((DFwindowPos.width - 28f) / 4.8f);
                        txtWdthVslN = Mathf.Round((DFwindowPos.width -28f) / 3.5f);
                        
                        
                    }
                    else
                    {
                        mouseDown = false;
                    }
                }
            }
        }

        #endregion GUI

        #region Savable

        //Class Load and Save of global settings
        public void Load(ConfigNode globalNode)
        {
            this.Log_Debug("DeepFreezeGUI Load");
            DFwindowPos.x = DFsettings.DFwindowPosX;
            DFwindowPos.y = DFsettings.DFwindowPosY;
            CFwindowPos.x = DFsettings.CFwindowPosX;
            CFwindowPos.y = DFsettings.CFwindowPosY;
            Useapplauncher = DFsettings.UseAppLauncher;
            AutoRecoverFznKerbals = DFsettings.AutoRecoverFznKerbals;
            debugging = DFsettings.debugging;
            ECreqdForFreezer = DFsettings.ECreqdForFreezer;
            KSCcostToThawKerbal = DFsettings.KSCcostToThawKerbal;
            ECReqdToFreezeThaw = DFsettings.ECReqdToFreezeThaw;
            GlykerolReqdToFreeze = DFsettings.GlykerolReqdToFreeze;
            RegTempReqd = DFsettings.RegTempReqd;
            RegTempFreeze = DFsettings.RegTempFreeze;
            RegtempMonitor = DFsettings.RegTempMonitor;
            heatamtMonitoringFrznKerbals = DFsettings.heatamtMonitoringFrznKerbals;
            heatamtThawFreezeKerbal = DFsettings.heatamtThawFreezeKerbal;
            TempinKelvin = DFsettings.TempinKelvin;
            this.Log_Debug("DeepFreezeGUI Load end");
        }

        public void Save(ConfigNode globalNode)
        {
            this.Log_Debug("DeepFreezeGUI Save");
            DFsettings.DFwindowPosX = DFwindowPos.x;
            DFsettings.DFwindowPosY = DFwindowPos.y;
            DFsettings.CFwindowPosX = CFwindowPos.x;
            DFsettings.CFwindowPosY = CFwindowPos.y;
            DFsettings.UseAppLauncher = Useapplauncher;
            DFsettings.AutoRecoverFznKerbals = AutoRecoverFznKerbals;
            DFsettings.debugging = debugging;
            DFsettings.ECreqdForFreezer = ECreqdForFreezer;
            DFsettings.KSCcostToThawKerbal = KSCcostToThawKerbal;
            DFsettings.ECReqdToFreezeThaw = ECReqdToFreezeThaw;
            DFsettings.GlykerolReqdToFreeze = GlykerolReqdToFreeze;
            DFsettings.RegTempReqd = RegTempReqd;
            DFsettings.RegTempFreeze = RegTempFreeze;
            DFsettings.RegTempMonitor = RegtempMonitor;
            DFsettings.heatamtThawFreezeKerbal = heatamtThawFreezeKerbal;
            DFsettings.heatamtMonitoringFrznKerbals = heatamtMonitoringFrznKerbals;
            DFsettings.TempinKelvin = TempinKelvin;
            this.Log_Debug("DeepFreezeGUI Save end");
        }

        #endregion Savable
    }
}
