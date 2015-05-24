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
using UnityEngine;

namespace DF
{
    public class FrozenKerbals : MonoBehaviour, Savable
    {
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

        public bool Useapplauncher = false;

        public bool FreezeAll = false;
        public bool ThawAll = false;

        public void Awake()
        {
            DFsettings = DeepFreeze.Instance.DFsettings;
        }

        #region AppLauncher

        private void OnGUIAppLauncherReady()
        {
            this.Log_Debug("OnGUIAppLauncherReady");
            if (ApplicationLauncher.Ready)
            {
                this.Log_Debug("Adding AppLauncherButton");
                this.stockToolbarButton = ApplicationLauncher.Instance.AddModApplication(onAppLaunchToggleOn, onAppLaunchToggleOff, DummyVoid,
                                          DummyVoid, DummyVoid, DummyVoid, ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT |
                                          ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB |
                                          ApplicationLauncher.AppScenes.TRACKSTATION,
                                          (Texture)GameDatabase.Instance.GetTexture("PaladinLabs/DeepFreeze/Icons/DeepFreezeOff", false));
            }
        }

        private void DummyVoid()
        {
        }

        private void onAppLaunchToggleOn()
        {
            this.stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture("PaladinLabs/DeepFreeze/Icons/DeepFreezeOn", false));
            GuiVisible = true;
        }

        private void onAppLaunchToggleOff()
        {
            this.stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture("PaladinLabs/DeepFreeze/Icons/DeepFreezeOff", false));
            GuiVisible = false;
        }

        #endregion AppLauncher

        public void OnDestroy()
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

        public void Start()
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
        }

        public void OnUpdate()
        {
        }

        private void onDraw()
        {
            if (!GuiVisible) return;

            if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.EDITOR
                || HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                GUI.skin = HighLogic.Skin;
                if (!Utilities.WindowVisibile(DFwindowPos))
                    Utilities.MakeWindowVisible(DFwindowPos);
                DFwindowPos = GUILayout.Window(windowID, DFwindowPos, windowSC, "DeepFreeze Kerbals",
                    GUILayout.Width(DFWINDOW_WIDTH), GUILayout.Height(WINDOW_BASE_HEIGHT));
            }
        }

        private void windowSC(int id)
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

            DeepFreezer DpFrzrActVsl = null;
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                //chk if current active vessel Has a DeepFreezer attached
                DpFrzrActVsl = FlightGlobals.ActiveVessel.FindPartModulesImplementing<DeepFreezer>().First();
            }

            GUILayout.BeginVertical();
            if (HighLogic.CurrentGame.CrewRoster.Unowned.Count() == 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("There are currently no Frozen Kerbals", statusStyle);
                GUILayout.EndHorizontal();
            }
            else
            {
                foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Unowned)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(kerbal.name + " - " + kerbal.experienceTrait.Title, statusStyle);
                    if (HighLogic.LoadedScene == GameScenes.FLIGHT && DpFrzrActVsl != null)
                    //if in flight and active vessel has a Freezer part check if kerbal is part of this vessel and add a Thaw button to the GUI
                    {
                        if (DpFrzrActVsl.StoredCrew.FirstOrDefault(a => a == kerbal.name) != null)
                        {
                            if (GUILayout.Button("Thaw", GUILayout.Width(50f)))
                            {
                                DpFrzrActVsl.beginThawKerbal(kerbal.name);
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }

            if (HighLogic.LoadedScene == GameScenes.FLIGHT && DpFrzrActVsl != null)
            {
                statusStyle.normal.textColor = Color.white;
                foreach (ProtoCrewMember crewMember in DpFrzrActVsl.part.protoModuleCrew.FindAll(a => a.type == ProtoCrewMember.KerbalType.Crew))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(crewMember.name + " - " + crewMember.experienceTrait.Title, statusStyle);
                    if (!DpFrzrActVsl.crewXferActive || (DpFrzrActVsl.crewXferActive && DpFrzrActVsl.xfercrew != crewMember))
                    {
                        if (GUILayout.Button("Freeze", GUILayout.Width(50f)))
                        {
                            DpFrzrActVsl.beginFreezeKerbal(crewMember);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();

            if (!Input.GetMouseButtonDown(1))
            {
                GUI.DragWindow();
            }
        }

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