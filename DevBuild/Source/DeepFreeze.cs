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

using KSP.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace DF
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class DeepFreeze : ScenarioModule, IDFInterface
    {
        public static DeepFreeze Instance { get; private set; }
        internal DFSettings DFsettings { get; private set; }
        internal DFGameSettings DFgameSettings { get; private set; }

        private readonly string globalConfigFilename;
        //private readonly string FilePath;
        private ConfigNode globalNode = new ConfigNode();

        private readonly List<Component> children = new List<Component>();
        

        public Dictionary<string, KerbalInfo> FrozenKerbals
        {
            get
            {
                return this.DFgameSettings.KnownFrozenKerbals;
            }
        }  
 
        protected DeepFreeze()
        {
            Utilities.Log("DeepFreeze", "Constructor");
            Instance = this;
            DFsettings = new DFSettings();
            DFgameSettings = new DFGameSettings();
            globalConfigFilename = System.IO.Path.Combine(_AssemblyFolder, "Config.cfg").Replace("\\", "/");
            this.Log("globalConfigFilename = " + globalConfigFilename);

            
            DeepFreezeEventAdd();
                        
        }

        public override void OnAwake()
        {
            this.Log("OnAwake in " + HighLogic.LoadedScene);
            base.OnAwake();

            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);

            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                this.Log("Adding SpaceCenterManager");
                var child = gameObject.AddComponent<DeepFreezeGUI>();
                children.Add(child);
            }
            else if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                this.Log("Adding FlightManager");
                var child = gameObject.AddComponent<DeepFreezeGUI>();
                children.Add(child);
            }
                /*
            else if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                this.Log("Adding EditorController");
                var child = gameObject.AddComponent<DeepFreezeGUI>();
                children.Add(child);
            } */
            else if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                this.Log("Adding TrackingStationController");
                var child = gameObject.AddComponent<DeepFreezeGUI>();
                children.Add(child);
            }
        }

        public override void OnLoad(ConfigNode gameNode)
        {
            base.OnLoad(gameNode);
            DFgameSettings.Load(gameNode);
            // Load the global settings
            if (System.IO.File.Exists(globalConfigFilename))
            {
                globalNode = ConfigNode.Load(globalConfigFilename);
                DFsettings.Load(globalNode);
                foreach (Savable s in children.Where(c => c is Savable))
                {
                    this.Log("DeepFreeze Child Load Call for " + s.ToString());
                    s.Load(globalNode);
                }
            }
            

            this.Log("OnLoad: \n " + gameNode + "\n" + globalNode);
        }

        public override void OnSave(ConfigNode gameNode)
        {
            base.OnSave(gameNode);
            DFgameSettings.Save(gameNode);
            foreach (Savable s in children.Where(c => c is Savable))
            {
                this.Log("DeepFreeze Child Save Call for " + s.ToString());
                s.Save(globalNode);
            }
            DFsettings.Save(globalNode);
            globalNode.Save(globalConfigFilename);

            this.Log("OnSave: " + gameNode + "\n" + globalNode);
        }

        protected void OnGameSceneLoadRequested(GameScenes gameScene)
        {
            this.Log("Game scene load requested: " + gameScene);
        }

        protected void OnDestroy()
        {
            this.Log("OnDestroy");
            foreach (Component child in children)
            {
                this.Log("DeepFreeze Child Destroy for " + child.name);
                Destroy(child);
            }
            children.Clear();
            DeepFreezeEventRem();
            GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequested);
        }

        #region Events

        protected void DeepFreezeEventAdd()
        {
            this.Log("DeepFreezeEvents DeepFreezeEventAdd");
            //GameEvents.OnVesselRecoveryRequested.Add(this.OnVesselRecoveryRequested);
            GameEvents.onVesselRecovered.Add(this.onVesselRecovered);
            GameEvents.onVesselTerminated.Add(this.onVesselTerminated);
            GameEvents.onVesselWillDestroy.Add(this.onVesselWillDestroy);
            this.Log("DeepFreezeEvents DeepFreezeEventAdd ended");
        }

        protected void DeepFreezeEventRem()
        {
            this.Log("DeepFreezeEvents DeepFreezeEventRem");
            //GameEvents.OnVesselRecoveryRequested.Remove(this.OnVesselRecoveryRequested);
            GameEvents.onVesselRecovered.Remove(this.onVesselRecovered);
            GameEvents.onVesselTerminated.Remove(this.onVesselTerminated);
            GameEvents.onVesselWillDestroy.Remove(this.onVesselWillDestroy);
            this.Log("DeepFreezeEvents DeepFreezeEventRem ended");
        }

        /*
        private void OnVesselRecoveryRequested(Vessel vessel)
        {
            Debug.Log("DeepFreezeEvents OnVesselRecoveryRequested");
            if (vessel.FindPartModulesImplementing<DeepFreezer>().Count > 0 && DeepFreeze.Instance.DFsettings.AutoRecoverFznKerbals)
            {
                foreach (DeepFreezer freezer in vessel.FindPartModulesImplementing<DeepFreezer>())
                {
                    freezer.part.CrewCapacity = freezer.StoredCrewList.Count;
                    foreach (var crewmember in freezer.StoredCrewList)
                    {
                        foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew)
                        {
                            if (kerbal.name == crewmember.CrewName)
                            {
                                kerbal.type = ProtoCrewMember.KerbalType.Crew;
                                kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                                freezer.part.AddCrewmember(kerbal);
                                Debug.Log("DeepFreezeEvents Crew Added" + kerbal.name);
                            }
                        }
                    }
                }
            }
        }
        */
        protected void onVesselRecovered(ProtoVessel vessel)
        {
            this.Log("DeepFreezeEvents onVesselRecovered " + vessel.vesselID);  
            List<string> frznKerbalkeys = new List<string>(DFgameSettings.KnownFrozenKerbals.Keys);
            foreach (string key in frznKerbalkeys)
            {
                KerbalInfo kerbalinfo = DFgameSettings.KnownFrozenKerbals[key];            
                if (kerbalinfo.vesselID == vessel.vesselID)
                {
                    if (DeepFreeze.Instance.DFsettings.AutoRecoverFznKerbals)
                    {
                        this.Log_Debug("AutoRecover is ON");
                        this.Log("Calling ThawFrozen Crew to thaw FrozenCrew " + key);
                        ThawFrozenCrew(key, vessel.vesselID);
                    }
                    else
                    {
                        this.Log("DeepFreeze AutoRecovery of frozen kerbals is set to off. Must be thawed manually.");
                        this.Log("DeepFreezeEvents frozenkerbal remains frozen =" + key);
                        ProtoCrewMember realkerbal = HighLogic.CurrentGame.CrewRoster.Unowned.FirstOrDefault(b => b.name == key);
                        if (realkerbal != null)
                        {
                            realkerbal.type = ProtoCrewMember.KerbalType.Unowned;
                            realkerbal.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                            this.Log_Debug("Kerbal " + realkerbal.name + " " + realkerbal.type + " " + realkerbal.rosterStatus);
                            ScreenMessages.PostScreenMessage(key + " was stored frozen at KSC", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        }
                    }
                }                        
            }                       
        }

        protected void onVesselTerminated(ProtoVessel vessel)
        {
            this.Log("DeepFreezeEvents onVesselTerminated " + vessel.vesselID);
            foreach (KeyValuePair<string, KerbalInfo> kerbal in DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals)
            {
                if (kerbal.Value.vesselID == vessel.vesselID)
                {                
                    KillFrozenCrew(kerbal.Key);
                }
            }
        }

        protected void onVesselWillDestroy(Vessel vessel)
        {
            this.Log("DeepFreezeEvents onVesselWillDestroy " + vessel.id);
            foreach (KeyValuePair<string, KerbalInfo> kerbal in DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals)
            {
                if (kerbal.Value.vesselID == vessel.id)
                {                    
                    KillFrozenCrew(kerbal.Key);
                }
            }
        }

        internal void ThawFrozenCrew(String FrozenCrew, Guid vesselID)
        {
            this.Log("DeepFreezeEvents ThawFrozenCrew = " + FrozenCrew + "," + vesselID);
            bool fundstaken = false;                        
            ProtoCrewMember kerbal = HighLogic.CurrentGame.CrewRoster.Unowned.FirstOrDefault(a => a.name == FrozenCrew);
            if (kerbal != null)
            {
                Vessel vessel = FlightGlobals.Vessels.Find(v => v.id == vesselID);                                
                if (vessel == null ||
                    (vessel.mainBody == FlightGlobals.Bodies[1] 
                    && (vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.PRELAUNCH || vessel.situation == Vessel.Situations.SPLASHED)))
                {
                        if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                        {
                            if (Funding.CanAfford(DFsettings.KSCcostToThawKerbal))
                            {
                                Funding.Instance.AddFunds(-DFsettings.KSCcostToThawKerbal, TransactionReasons.Vessels);
                                fundstaken = true;                                
                                this.Log("Took funds to thaw kerbal");
                            }
                            else
                            {
                                this.Log("Not enough funds to thaw kerbal");
                                ScreenMessages.PostScreenMessage("Insufficient funds to thaw " + kerbal.name + " at this time", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                                return;
                            }
                        }
                        kerbal.type = ProtoCrewMember.KerbalType.Crew;
                        kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                        this.Log_Debug("Kerbal " + kerbal.name + " " + kerbal.type + " " + kerbal.rosterStatus);
                        kerbal.ArchiveFlightLog();
                        kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Available;
                        this.Log_Debug("Kerbal " + kerbal.name + " " + kerbal.type + " " + kerbal.rosterStatus);
                        if (!fundstaken)
                        {
                            ScreenMessages.PostScreenMessage(kerbal.name + " was found and thawed out", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        }
                        else
                        {                            
                            ScreenMessages.PostScreenMessage(kerbal.name + " was found and thawed out " + DFsettings.KSCcostToThawKerbal.ToString("########0") + " funds deducted from account", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        }                        
                        DFgameSettings.KnownFrozenKerbals.Remove(kerbal.name);                        
                }
                else
                {
                    this.Log("Cannot thaw, vessel still exists " + vessel.situation.ToString() + " at " + vessel.mainBody.bodyName);
                    ScreenMessages.PostScreenMessage("Cannot thaw " + kerbal.name + " vessel still exists " + vessel.situation + " at " + vessel.mainBody.bodyName, 5.0f, ScreenMessageStyle.UPPER_CENTER); 
                }                                                                                                    
            }            
        }

       internal void KillFrozenCrew(string FrozenCrew)
        {
            this.Log("DeepFreezeEvents KillFrozenCrew " + FrozenCrew);                       
            DeepFreeze.Instance.DFgameSettings.KnownFrozenKerbals.Remove(FrozenCrew);
            ProtoCrewMember kerbal = HighLogic.CurrentGame.CrewRoster.Unowned.FirstOrDefault(a => a.name == FrozenCrew);
            if (kerbal != null)
            {
                this.Log("DeepFreezeEvents " + kerbal.name + " killed");
                kerbal.type = ProtoCrewMember.KerbalType.Crew;
                kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                if (HighLogic.CurrentGame.Parameters.Difficulty.MissingCrewsRespawn == true)
                {
                    kerbal.StartRespawnPeriod();
                    this.Log("DeepFreezeEvents " + kerbal.name + " respawn started.");
                }
            }
            else
                this.Log("DeepFreezeEvents " + kerbal.name + " couldn't find them to kill them.");                
        }

        #endregion Events

        #region Assembly/Class Information

        /// <summary>
        /// Name of the Assembly that is running this MonoBehaviour
        /// </summary>
        internal static String _AssemblyName
        { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; } }

        /// <summary>
        /// Full Path of the executing Assembly
        /// </summary>
        internal static String _AssemblyLocation
        { get { return System.Reflection.Assembly.GetExecutingAssembly().Location; } }

        /// <summary>
        /// Folder containing the executing Assembly
        /// </summary>
        internal static String _AssemblyFolder
        { get { return System.IO.Path.GetDirectoryName(_AssemblyLocation); } }

        #endregion Assembly/Class Information
    }

    internal interface Savable
    {
        void Load(ConfigNode globalNode);

        void Save(ConfigNode globalNode);
    }
}