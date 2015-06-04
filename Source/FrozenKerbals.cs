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
        private const float WINDOW_BASE_HEIGHT = 200;
        public Rect DFwindowPos = new Rect(40, Screen.height / 2 - 100, DFWINDOW_WIDTH, WINDOW_BASE_HEIGHT);
        private static int windowID = new System.Random().Next();
        private GUIStyle statusStyle, sectionTitleStyle, resizeStyle;
        private bool mouseDown = false;      

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
            this.Log_Debug("FrozenKerbals startup");
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
                this.Log_Debug("Adding onGUIAppLauncher callbacks");
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
                this.Log("There are " + unknownkerbals.Count() + " unknownKerbals in the game roster." );
                foreach (ProtoCrewMember CrewMember in unknownkerbals)
                {
                    if (CrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Dead)
                    {
                        
                        if (!DFgameSettings.KnownFrozenKerbals.ContainsKey(CrewMember.name))
                        {                            
                            // Update the saved frozen kerbals dictionary
                            KerbalInfo kerbalInfo = new KerbalInfo(Planetarium.GetUniversalTime());                            
                            kerbalInfo.vesselID = Guid.Empty;                            
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
            DFgameSettings.DmpKnownFznKerbals();
            this.Log_Debug("FrozenKerbals END startup");
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
                DFwindowPos = GUILayout.Window(windowID, DFwindowPos, windowDF, "DeepFreeze Kerbals", GUILayout.ExpandWidth(true),
                        GUILayout.ExpandHeight(true), GUILayout.MinWidth(100), GUILayout.MinHeight(100));
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

            resizeStyle = new GUIStyle(GUI.skin.button);
            resizeStyle.alignment = TextAnchor.MiddleCenter;
            resizeStyle.padding = new RectOffset(1, 1, 1, 1);


            GUIContent closeContent = new GUIContent("X", "Close Window");
            Rect closeRect = new Rect(DFwindowPos.width - 17, 4, 16, 16);
            if (GUI.Button(closeRect, closeContent, resizeStyle))
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

            
            GUIContent resizeContent = new GUIContent("R", "Resize Window");
            Rect resizeRect = new Rect(DFwindowPos.width - 17, DFwindowPos.height - 17, 16, 16);
            GUI.Label(resizeRect, resizeContent, resizeStyle);
            HandleResizeEvents(resizeRect);                       

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
            this.Log_Debug("FrozenKerbals Load");
            DFwindowPos.x = DFsettings.DFwindowPosX;
            DFwindowPos.y = DFsettings.DFwindowPosY;
            Useapplauncher = DFsettings.UseAppLauncher;
            this.Log_Debug("FrozenKerbals Load end");
        }

        public void Save(ConfigNode globalNode)
        {
            this.Log_Debug("FrozenKerbals Save");
            DFsettings.DFwindowPosX = DFwindowPos.x;
            DFsettings.DFwindowPosY = DFwindowPos.y;
            this.Log_Debug("FrozenKerbals Save end");
        }

        #endregion Savable
    }
}