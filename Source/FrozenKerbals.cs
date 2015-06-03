/**
 * FrozenKerbals.cs
 *
 *
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 *
 *  This file is part of Jamie Leighton's Fork of DeepFreeze. Original Author of DeepFreeze is 'scottpaladin' on the KSP Forums.
 *  This File was not part of the original Deepfreeze but was written by Jamie Leighton.
 *  (C) Copyright 2015, Jamie Leighton
 *
 * Which is licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 4.0)
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
    public class FrozenKerbals : MonoBehaviour, Savable
    {
        private List<DeepFreezer> DpFrzrActVsl = new List<DeepFreezer>();
        private bool ActVslHasDpFrezr = false;
        
        //GUI Properties
        private IButton button1;
        private ApplicationLauncherButton stockToolbarButton = null; // Stock Toolbar Button
        private const float DFWINDOW_WIDTH = 300;
        private const float WINDOW_BASE_HEIGHT = 140;
        public Rect DFwindowPos = new Rect(40, Screen.height / 2 - 100, DFWINDOW_WIDTH, 200);
        private static int windowID = new System.Random().Next();
        private GUIStyle statusStyle, sectionTitleStyle;               

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
                    RenderingManager.AddToPostDrawQueue(5, this.onDraw);
                }
                else
                {
                    RenderingManager.RemoveFromPostDrawQueue(5, this.onDraw);
                }
            }
        }

        //DeepFreeze Savable settings
        private DFSettings DFsettings;
        private DFGameSettings DFgameSettings;
        public bool Useapplauncher = false;
        public bool FreezeAll = false;
        public bool ThawAll = false;        

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
                                          (Texture)GameDatabase.Instance.GetTexture("PaladinLabs/DeepFreeze/Icons/DeepFreezeOff", false));
            }
        }

        private void DummyVoid()
        {
        }

        private void onAppLaunchToggle()
        {
            GuiVisible = !GuiVisible;
            this.stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(GuiVisible ? "PaladinLabs/DeepFreeze/Icons/DeepFreezeOn" : "PaladinLabs/DeepFreeze/Icons/DeepFreezeOff", false));            
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
        }

        internal void Start()
        {
            // create toolbar button
            if (ToolbarManager.ToolbarAvailable && Useapplauncher == false)
            {
                button1 = ToolbarManager.Instance.add("DeepFreeze", "button1");
                button1.TexturePath = "PaladinLabs/DeepFreeze/Icons/DFtoolbar";
                button1.ToolTip = "DeepFreeze";
                button1.Visibility = new GameScenesVisibility(GameScenes.FLIGHT, GameScenes.EDITOR, GameScenes.SPACECENTER, GameScenes.TRACKSTATION);
                button1.OnClick += (e) => GuiVisible = !GuiVisible;
            }
            else
            {
                // Set up the stock toolbar
                this.Log_Debug("SCDeepFreeze Adding onGUIAppLauncher callbacks");
                if (ApplicationLauncher.Ready)
                {
                    OnGUIAppLauncherReady();
                }
                else
                    GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
            }

            // Check the roster list for any unknown dead kerbals (IE: Frozen) that were not in the save file and add them.
            List<ProtoCrewMember> unknownkerbals = HighLogic.CurrentGame.CrewRoster.Unowned.ToList();
            if (unknownkerbals != null)
            {
                this.Log("DeepFreeze have unknownKerbals " + unknownkerbals.Count());
                foreach (ProtoCrewMember CrewMember in unknownkerbals)
                {
                    if (CrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Dead)
                    {
                        this.Log("DeepFreeze we have dead unknown kerbals in roster");
                        if (!DFgameSettings.KnownFrozenKerbals.ContainsKey(CrewMember.name))
                        {
                            this.Log("DeepFreeze and they aren't in the dictionary so add them");
                            // Update the saved frozen kerbals dictionary
                            KerbalInfo kerbalInfo = new KerbalInfo(Planetarium.GetUniversalTime());                            
                            kerbalInfo.vesselID = Guid.Empty;                            
                            kerbalInfo.type = CrewMember.type;                            
                            kerbalInfo.status = CrewMember.rosterStatus;                            
                            //kerbalInfo.seatName = "Unknown";                            
                            kerbalInfo.seatIdx = 0;                            
                            kerbalInfo.partID = 0;                            
                            kerbalInfo.experienceTraitName = CrewMember.experienceTrait.Title;                            
                            try
                            {
                                DFgameSettings.KnownFrozenKerbals.Add(CrewMember.name, kerbalInfo);
                            }
                            catch (Exception ex)
                            {
                                this.Log("Add failed " + ex);
                            }
                            
                        }
                    }
                }
            }
            DFgameSettings.DmpKnownFznKerbals();     
        }

        internal void Update()
        {
                        
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                //chk if current active vessel Has a DeepFreezer attached
                //this.Log_Debug("Check for Freezer part on active vessel");
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
            
        }

        #region GUI
        private void onDraw()
        {
            if (!GuiVisible) return;

            if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.EDITOR
                || HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                GUI.skin = HighLogic.Skin;
                if (!Utilities.WindowVisibile(DFwindowPos))
                    Utilities.MakeWindowVisible(DFwindowPos);
                DFwindowPos = GUILayout.Window(windowID, DFwindowPos, windowDF, "DeepFreeze Kerbals",
                    GUILayout.Width(DFWINDOW_WIDTH), GUILayout.Height(WINDOW_BASE_HEIGHT));
            }
        }

        private void windowDF(int id)
        {
            //Init styles
            sectionTitleStyle = new GUIStyle(GUI.skin.label);
            sectionTitleStyle.alignment = TextAnchor.MiddleCenter;
            sectionTitleStyle.stretchWidth = true;
            sectionTitleStyle.normal.textColor = Color.blue;
            sectionTitleStyle.fontStyle = FontStyle.Bold;

            statusStyle = new GUIStyle(GUI.skin.label);
            statusStyle.alignment = TextAnchor.MiddleLeft;
            statusStyle.stretchWidth = true;
            statusStyle.normal.textColor = Color.blue;


            GUIContent label = new GUIContent("X", "Close Window");
            Rect rect = new Rect(280, 4, 16, 16);
            if (GUI.Button(rect, label))
            {
                onAppLaunchToggle();
                return;
            }

            GUILayout.BeginVertical();
            if (DFgameSettings.KnownFrozenKerbals.Count == 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("There are currently no Frozen Kerbals", statusStyle);
                GUILayout.EndHorizontal();
            }
            else
            {

                List<KeyValuePair<string, KerbalInfo>> ThawKeysToDelete = new List<KeyValuePair<string, KerbalInfo>>();
                foreach (KeyValuePair<string, KerbalInfo> kerbal in DFgameSettings.KnownFrozenKerbals)                
                {                    
                        GUILayout.BeginHorizontal();                        
                        GUILayout.Label(kerbal.Key + " - " + kerbal.Value.experienceTraitName, statusStyle);
                        if (HighLogic.LoadedScene == GameScenes.FLIGHT && ActVslHasDpFrezr)
                        //if in flight and active vessel has a Freezer part check if kerbal is part of this vessel and add a Thaw button to the GUI
                        {
                            foreach (DeepFreezer frzr in DpFrzrActVsl)
                            {                                
                                if (frzr.StoredCrewList.FirstOrDefault(a => a.CrewName == kerbal.Key) != null)
                                {
                                    if (frzr.crewXferFROMActive || frzr.crewXferTOActive || (DeepFreeze.Instance.SMInstalled && frzr.IsSMXferRunning())
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
                statusStyle.normal.textColor = Color.white;
                foreach (DeepFreezer frzr in DpFrzrActVsl)
                {
                    foreach (ProtoCrewMember crewMember in frzr.part.protoModuleCrew.FindAll(a => a.type == ProtoCrewMember.KerbalType.Crew))
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(crewMember.name + " - " + crewMember.experienceTrait.Title, statusStyle);
                        if (frzr.crewXferFROMActive || frzr.crewXferTOActive || (DeepFreeze.Instance.SMInstalled && frzr.IsSMXferRunning())
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

            GUILayout.EndVertical();

            if (!Input.GetMouseButtonDown(1))
            {
                GUI.DragWindow();
            }
        }

        
        #endregion GUI

        #region Savable

        //Class Load and Save of global settings
        public void Load(ConfigNode globalNode)
        {
            this.Log_Debug("FrozenKerbal Load");
            DFwindowPos.x = DFsettings.DFwindowPosX;
            DFwindowPos.y = DFsettings.DFwindowPosY;
            Useapplauncher = DFsettings.UseAppLauncher;
            this.Log_Debug("FrozenKerbals Load end");
        }

        public void Save(ConfigNode globalNode)
        {
            this.Log_Debug("FrozenKerbal Save");
            DFsettings.DFwindowPosX = DFwindowPos.x;
            DFsettings.DFwindowPosY = DFwindowPos.y;
            this.Log_Debug("FrozenKerbal Save end");
        }

        #endregion Savable
    }
}