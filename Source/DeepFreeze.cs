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
using System.IO;
using System.Linq;
using System.Reflection;
using RSTUtils;
using UnityEngine; 

namespace DF
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class DeepFreeze : ScenarioModule
    {
        public static DeepFreeze Instance;
        public static bool APIReady;
        internal DFSettings DFsettings;
        internal DFGameSettings DFgameSettings;
        private readonly string globalConfigFilename;
        private ConfigNode globalNode = new ConfigNode();
        private readonly List<Component> children = new List<Component>();

        public Dictionary<string, KerbalInfo> FrozenKerbals
        {
            get
            {
                return DFgameSettings.KnownFrozenKerbals;
            }
        }

        public DeepFreeze()
        {
            Utilities.Log("DeepFreeze Constructor");
            Instance = this;
            APIReady = false;
            DFsettings = new DFSettings();
            DFgameSettings = new DFGameSettings();
            globalConfigFilename = Path.Combine(_AssemblyFolder, "Config.cfg").Replace("\\", "/");
            Utilities.Log("globalConfigFilename = " + globalConfigFilename);
            DeepFreezeEventAdd();
        }

        public override void OnAwake()
        {
            Utilities.Log("OnAwake in " + HighLogic.LoadedScene);
            base.OnAwake();

            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);

            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                Utilities.Log("Adding SpaceCenterManager");
                var DFMem = gameObject.AddComponent<DFIntMemory>();
                children.Add(DFMem);
                var child = gameObject.AddComponent<DeepFreezeGUI>();
                children.Add(child);
            }
            else if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                Utilities.Log("Adding FlightManager");
                var DFMem = gameObject.AddComponent<DFIntMemory>();
                children.Add(DFMem);
                var child = gameObject.AddComponent<DeepFreezeGUI>();
                children.Add(child);
            }
            else if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                Utilities.Log("Adding EditorController");
                var DFMem = gameObject.AddComponent<DFIntMemory>();
                children.Add(DFMem);

                //var child = gameObject.AddComponent<DeepFreezeGUI>();
                //children.Add(child);
            }
            else if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                Utilities.Log("Adding TrackingStationController");
                var DFMem = gameObject.AddComponent<DFIntMemory>();
                children.Add(DFMem);
                var child = gameObject.AddComponent<DeepFreezeGUI>();
                children.Add(child);
            }

            
        }

        public override void OnLoad(ConfigNode gameNode)
        {
            base.OnLoad(gameNode);
            DFgameSettings.Load(gameNode);
            // Load the global settings
            if (File.Exists(globalConfigFilename))
            {
                globalNode = ConfigNode.Load(globalConfigFilename);
                DFsettings.Load(globalNode);
                foreach (Savable s in children.Where(c => c is Savable))
                {                    
                    s.Load(globalNode);
                }
            }
            Utilities.debuggingOn = DFsettings.debugging;
            APIReady = true;
            Debug.Log("Scenario: " + HighLogic.LoadedScene + " OnLoad: \n " + gameNode + "\n" + globalNode);
        }

        public override void OnSave(ConfigNode gameNode)
        {
            //APIReady = false;
            base.OnSave(gameNode);
            DFgameSettings.Save(gameNode);
            foreach (Savable s in children.Where(c => c is Savable))
            {                
                s.Save(globalNode);
            }
            DFsettings.Save(globalNode);
            globalNode.Save(globalConfigFilename);
            Debug.Log("Scenario: " + HighLogic.LoadedScene + " OnSave: \n" + gameNode + "\n" + globalNode);
        }

        protected void OnGameSceneLoadRequested(GameScenes gameScene)
        {
            Utilities.Log("Game scene load requested: " + gameScene);
        }

        protected void OnDestroy()
        {
            Utilities.Log("OnDestroy");
            Instance = null;
            APIReady = false;
            foreach (Component child in children)
            {
                Utilities.Log("DeepFreeze Child Destroy for " + child.name);
                Destroy(child);
            }
            children.Clear();
            DeepFreezeEventRem();
            GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequested);
        }

        #region Events

        protected void DeepFreezeEventAdd()
        {
            Utilities.Log("DeepFreezeEvents DeepFreezeEventAdd");
            GameEvents.onVesselRecovered.Add(onVesselRecovered);
            GameEvents.onVesselTerminated.Add(onVesselTerminated);
            GameEvents.onVesselWillDestroy.Add(onVesselWillDestroy);
            Utilities.Log("DeepFreezeEvents DeepFreezeEventAdd ended");
        }

        protected void DeepFreezeEventRem()
        {
            Utilities.Log("DeepFreezeEvents DeepFreezeEventRem");
            GameEvents.onVesselRecovered.Remove(onVesselRecovered);
            GameEvents.onVesselTerminated.Remove(onVesselTerminated);
            GameEvents.onVesselWillDestroy.Remove(onVesselWillDestroy);
            Utilities.Log("DeepFreezeEvents DeepFreezeEventRem ended");
        }

        protected void onVesselRecovered(ProtoVessel vessel)
        {
            Utilities.Log("DeepFreezeEvents onVesselRecovered " + vessel.vesselID);
            List<string> frznKerbalkeys = new List<string>(DFgameSettings.KnownFrozenKerbals.Keys);
            foreach (string key in frznKerbalkeys)
            {
                KerbalInfo kerbalinfo = DFgameSettings.KnownFrozenKerbals[key];
                if (kerbalinfo.vesselID == vessel.vesselID)
                {
                    if (kerbalinfo.type == ProtoCrewMember.KerbalType.Unowned) //Frozen crew
                    {
                        if (Instance.DFsettings.AutoRecoverFznKerbals)
                        {
                             Utilities.Log_Debug("AutoRecover is ON");
                            Utilities.Log("Calling ThawFrozen Crew to thaw FrozenCrew " + key);
                            ThawFrozenCrew(key, vessel.vesselID);
                        }
                        else
                        {
                            Utilities.Log("DeepFreeze AutoRecovery of frozen kerbals is set to off. Must be thawed manually.");
                            Utilities.Log("DeepFreezeEvents frozenkerbal remains frozen =" + key);
                            ProtoCrewMember realkerbal = HighLogic.CurrentGame.CrewRoster.Unowned.FirstOrDefault(b => b.name == key);
                            if (realkerbal != null)
                            {
                                realkerbal.type = ProtoCrewMember.KerbalType.Unowned;
                                realkerbal.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                                 Utilities.Log_Debug("Kerbal " + realkerbal.name + " " + realkerbal.type + " " + realkerbal.rosterStatus);
                                ScreenMessages.PostScreenMessage(key + " was stored frozen at KSC", 5.0f, ScreenMessageStyle.UPPER_RIGHT);
                            }
                        }
                    }
                    else // Tourist/Comatose crew
                    {
                         Utilities.Log_Debug("Comatose crew - reset to crew " + key);
                        ProtoCrewMember crew = HighLogic.CurrentGame.CrewRoster.Tourist.FirstOrDefault(c => c.name == key);
                        if (crew != null)
                        {
                            crew.type = ProtoCrewMember.KerbalType.Crew;
                            crew.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                            Utilities.Log_Debug("Kerbal " + crew.name + " " + crew.type + " " + crew.rosterStatus);
                            crew.ArchiveFlightLog();
                            crew.rosterStatus = ProtoCrewMember.RosterStatus.Available;
                            DFgameSettings.KnownFrozenKerbals.Remove(crew.name);
                        }
                    }
                }
            }
            var alarmsToDelete = new List<string>();
            alarmsToDelete.AddRange(Instance.DFgameSettings.knownKACAlarms.Where(e => e.Value.VesselID == vessel.vesselID).Select(e => e.Key).ToList());
            alarmsToDelete.ForEach(id => Instance.DFgameSettings.knownKACAlarms.Remove(id));
            var partsToDelete = new List<uint>();
            partsToDelete.AddRange(Instance.DFgameSettings.knownFreezerParts.Where(e => e.Value.vesselID == vessel.vesselID).Select(e => e.Key).ToList());
            partsToDelete.ForEach(id => Instance.DFgameSettings.knownFreezerParts.Remove(id));
            if (DFgameSettings.knownVessels.ContainsKey(vessel.vesselID))
            {
                DFgameSettings.knownVessels.Remove(vessel.vesselID);
            }
        }

        protected void onVesselTerminated(ProtoVessel vessel)
        {
            Utilities.Log("DeepFreezeEvents onVesselTerminated " + vessel.vesselID);
            foreach (KeyValuePair<string, KerbalInfo> kerbal in Instance.DFgameSettings.KnownFrozenKerbals)
            {
                if (kerbal.Value.vesselID == vessel.vesselID)
                {
                    KillFrozenCrew(kerbal.Key);
                }
            }
            var alarmsToDelete = new List<string>();
            alarmsToDelete.AddRange(Instance.DFgameSettings.knownKACAlarms.Where(e => e.Value.VesselID == vessel.vesselID).Select(e => e.Key).ToList());
            alarmsToDelete.ForEach(id => Instance.DFgameSettings.knownKACAlarms.Remove(id));
            var partsToDelete = new List<uint>();
            partsToDelete.AddRange(Instance.DFgameSettings.knownFreezerParts.Where(e => e.Value.vesselID == vessel.vesselID).Select(e => e.Key).ToList());
            partsToDelete.ForEach(id => Instance.DFgameSettings.knownFreezerParts.Remove(id));
            if (DFgameSettings.knownVessels.ContainsKey(vessel.vesselID))
            {
                DFgameSettings.knownVessels.Remove(vessel.vesselID);
            }
        }

        protected void onVesselWillDestroy(Vessel vessel)
        {
            Utilities.Log("DeepFreezeEvents onVesselWillDestroy " + vessel.id);
            foreach (KeyValuePair<string, KerbalInfo> kerbal in Instance.DFgameSettings.KnownFrozenKerbals)
            {
                if (kerbal.Value.vesselID == vessel.id)
                {
                    KillFrozenCrew(kerbal.Key);
                }
            }
            var alarmsToDelete = new List<string>();
            alarmsToDelete.AddRange(Instance.DFgameSettings.knownKACAlarms.Where(e => e.Value.VesselID == vessel.id).Select(e => e.Key).ToList());
            alarmsToDelete.ForEach(id => Instance.DFgameSettings.knownKACAlarms.Remove(id));
            var partsToDelete = new List<uint>();
            partsToDelete.AddRange(Instance.DFgameSettings.knownFreezerParts.Where(e => e.Value.vesselID == vessel.id).Select(e => e.Key).ToList());
            partsToDelete.ForEach(id => Instance.DFgameSettings.knownFreezerParts.Remove(id));
            if (DFgameSettings.knownVessels.ContainsKey(vessel.id))
            {
                DFgameSettings.knownVessels.Remove(vessel.id);
            }
        }

        internal void ThawFrozenCrew(String FrozenCrew, Guid vesselID)
        {
            Utilities.Log("DeepFreezeEvents ThawFrozenCrew = " + FrozenCrew + "," + vesselID);
            bool fundstaken = false;
            ProtoCrewMember kerbal = HighLogic.CurrentGame.CrewRoster.Unowned.FirstOrDefault(a => a.name == FrozenCrew);
            if (kerbal != null)
            {
                Vessel vessel = FlightGlobals.Vessels.Find(v => v.id == vesselID);
                // Utilities.Log_Debug("vessel mainbody" + vessel.mainBody.name + " is homeworld? " + vessel.mainBody.isHomeWorld);
                     
                if (vessel == null ||
                    (vessel.mainBody.isHomeWorld
                    && (vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.PRELAUNCH || vessel.situation == Vessel.Situations.SPLASHED)))
                {
                    if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                    {
                        if (Funding.CanAfford(DFsettings.KSCcostToThawKerbal))
                        {
                            Funding.Instance.AddFunds(-DFsettings.KSCcostToThawKerbal, TransactionReasons.Vessels);
                            fundstaken = true;
                            Utilities.Log("Took funds to thaw kerbal");
                        }
                        else
                        {
                            Utilities.Log("Not enough funds to thaw kerbal");
                            ScreenMessages.PostScreenMessage("Insufficient funds to thaw " + kerbal.name + " at this time", 5.0f, ScreenMessageStyle.UPPER_RIGHT);
                            return;
                        }
                    }
                    kerbal.type = ProtoCrewMember.KerbalType.Crew;
                    kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                     Utilities.Log_Debug("Kerbal " + kerbal.name + " " + kerbal.type + " " + kerbal.rosterStatus);
                    kerbal.ArchiveFlightLog();
                    kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Available;
                     Utilities.Log_Debug("Kerbal " + kerbal.name + " " + kerbal.type + " " + kerbal.rosterStatus);
                    if (!fundstaken)
                    {
                        ScreenMessages.PostScreenMessage(kerbal.name + " was found and thawed out", 5.0f, ScreenMessageStyle.UPPER_RIGHT);
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage(kerbal.name + " was found and thawed out " + DFsettings.KSCcostToThawKerbal.ToString("########0") + " funds deducted from account", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    DFgameSettings.KnownFrozenKerbals.Remove(kerbal.name);
                }
                else
                {
                    Utilities.Log("Cannot thaw, vessel still exists " + vessel.situation + " at " + vessel.mainBody.bodyName);
                    ScreenMessages.PostScreenMessage("Cannot thaw " + kerbal.name + " vessel still exists " + vessel.situation + " at " + vessel.mainBody.bodyName, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                }
            }
        }

        internal void KillFrozenCrew(string FrozenCrew)
        {
            Utilities.Log("DeepFreezeEvents KillFrozenCrew " + FrozenCrew);
            Instance.DFgameSettings.KnownFrozenKerbals.Remove(FrozenCrew);
            ProtoCrewMember kerbal = HighLogic.CurrentGame.CrewRoster.Unowned.FirstOrDefault(a => a.name == FrozenCrew);
            if (kerbal != null)
            {
                Utilities.Log("DeepFreezeEvents " + kerbal.name + " killed");
                kerbal.type = ProtoCrewMember.KerbalType.Crew;
                kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                if (HighLogic.CurrentGame.Parameters.Difficulty.MissingCrewsRespawn)
                {
                    kerbal.StartRespawnPeriod();
                    Utilities.Log("DeepFreezeEvents " + kerbal.name + " respawn started.");
                }
            }
            else
            {
                // check if comatose crew
                ProtoCrewMember crew = HighLogic.CurrentGame.CrewRoster.Tourist.FirstOrDefault(a => a.name == FrozenCrew);
                if (crew != null)
                {
                    Utilities.Log("DeepFreezeEvents " + kerbal.name + " killed");
                    kerbal.type = ProtoCrewMember.KerbalType.Crew;
                    kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                    if (HighLogic.CurrentGame.Parameters.Difficulty.MissingCrewsRespawn)
                    {
                        kerbal.StartRespawnPeriod();
                        Utilities.Log("DeepFreezeEvents " + kerbal.name + " respawn started.");
                    }
                }
                else
                    Utilities.Log("DeepFreezeEvents " + kerbal.name + " couldn't find them to kill them.");
            }
        }

        internal bool setComatoseKerbal(ProtoCrewMember crew, ProtoCrewMember.KerbalType type)
        {
            try
            {
                crew.type = type;
                if (type == ProtoCrewMember.KerbalType.Crew)
                {
                    KerbalRoster.SetExperienceTrait(crew, "");
                    ScreenMessages.PostScreenMessage(crew.name + " has recovered from emergency thaw and resumed normal duties.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                }
                else
                {
                    KerbalRoster.SetExperienceTrait(crew, "Tourist");
                    ScreenMessages.PostScreenMessage(crew.name + " has been emergency thawed and cannot perform duties for " + Instance.DFsettings.comatoseTime / 60 + " minutes.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                }
                return true;
            }
            catch (Exception)
            {
                Utilities.Log("DeepFreeze Failed to set " + crew.name + " to status of " + type + " during emergency thaw processing.");
                return false;
            }
        }

        #endregion Events

        #region Assembly/Class Information

        /// <summary>
        /// Name of the Assembly that is running this MonoBehaviour
        /// </summary>
        internal static String _AssemblyName
        { get { return Assembly.GetExecutingAssembly().GetName().Name; } }

        /// <summary>
        /// Full Path of the executing Assembly
        /// </summary>
        internal static String _AssemblyLocation
        { get { return Assembly.GetExecutingAssembly().Location; } }

        /// <summary>
        /// Folder containing the executing Assembly
        /// </summary>
        internal static String _AssemblyFolder
        { get { return Path.GetDirectoryName(_AssemblyLocation); } }

        #endregion Assembly/Class Information
    }

    internal interface Savable
    {
        void Load(ConfigNode globalNode);

        void Save(ConfigNode globalNode);
    }
}