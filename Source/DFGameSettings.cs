﻿/**
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
using RSTUtils;

namespace DF
{
    internal class DFGameSettings
    {
        // This class stores the DeepFreeze Gamesettings config node.
        // which includes the following Dictionaries
        // KnownFrozenKerbals - all known frozen kerbals in the current save game
        // knownVessels - all vessels in the save game that contain a DeepFreezer partmodule
        // knownFreezerPArts - all parts in the save game that contain a DeepFreezer partmodule
        // knownKACAlarms = all Kerbal Alarm Clock alarms that are associated with a DeppFreezer knownVessels entry

        public const string configNodeName = "DFGameSettings";
        public bool Enabled;
        internal Dictionary<string, KerbalInfo> KnownFrozenKerbals;
        internal Dictionary<Guid, VesselInfo> knownVessels;
        internal Dictionary<uint, PartInfo> knownFreezerParts;
        internal Dictionary<string, AlarmInfo> knownKACAlarms;

        internal DFGameSettings()
        {
            Enabled = true;
            KnownFrozenKerbals = new Dictionary<string, KerbalInfo>();
            knownVessels = new Dictionary<Guid, VesselInfo>();
            knownFreezerParts = new Dictionary<uint, PartInfo>();
            knownKACAlarms = new Dictionary<string, AlarmInfo>();
        }

        internal void Load(ConfigNode node)
        {
            KnownFrozenKerbals.Clear();
            knownVessels.Clear();
            knownFreezerParts.Clear();
            knownKACAlarms.Clear();

            if (node.HasNode(configNodeName))
            {
                ConfigNode DFsettingsNode = node.GetNode(configNodeName);
                DFsettingsNode.TryGetValue("Enabled", ref Enabled);

                KnownFrozenKerbals.Clear();
                var kerbalNodes = DFsettingsNode.GetNodes(KerbalInfo.ConfigNodeName);
                foreach (ConfigNode kerbalNode in kerbalNodes)
                {
                    if (kerbalNode.HasValue("kerbalName"))
                    {
                        string id = kerbalNode.GetValue("kerbalName");
                        Utilities.Log_Debug("DFGameSettings Loading kerbal = " + id);
                        KerbalInfo kerbalInfo = KerbalInfo.Load(kerbalNode);
                        KnownFrozenKerbals.Add(id, kerbalInfo);
                    }
                }
                Utilities.Log_Debug("DFGameSettings finished loading FrozenKerbals");
                knownVessels.Clear();
                var vesselNodes = DFsettingsNode.GetNodes(VesselInfo.ConfigNodeName);
                foreach (ConfigNode vesselNode in vesselNodes)
                {
                    if (vesselNode.HasValue("Guid"))
                    {
                        Guid id = new Guid(vesselNode.GetValue("Guid"));
                        Utilities.Log_Debug("DFGameSettings Loading Guid = " + id);
                        VesselInfo vesselInfo = VesselInfo.Load(vesselNode);
                        knownVessels[id] = vesselInfo;
                    }
                }
                Utilities.Log_Debug("DFGameSettings finished loading KnownVessels");
                knownFreezerParts.Clear();
                var partNodes = DFsettingsNode.GetNodes(PartInfo.ConfigNodeName);
                foreach (ConfigNode partNode in partNodes)
                {
                    if (partNode.HasValue("flightID"))
                    {
                        uint id = uint.Parse(partNode.GetValue("flightID"));
                        Utilities.Log_Debug("DFGameSettings Loading flightID = " + id);
                        PartInfo partInfo = PartInfo.Load(partNode);
                        knownFreezerParts[id] = partInfo;
                    }
                }
                Utilities.Log_Debug("DFGameSettings finished loading KnownParts");
                knownKACAlarms.Clear();
                var KACAlarmNodes = DFsettingsNode.GetNodes(AlarmInfo.ConfigNodeName);
                foreach (ConfigNode alarmNode in KACAlarmNodes)
                {
                    if (alarmNode.HasValue("alarmID"))
                    {
                        string alarmID = alarmNode.GetValue("alarmID");
                        Utilities.Log_Debug("DFGameSettings Loading alarmID = " + alarmID);
                        AlarmInfo alarmInfo = AlarmInfo.Load(alarmNode);
                        knownKACAlarms[alarmID] = alarmInfo;
                    }
                }
                SyncDictionaries();
            }
            Utilities.Log_Debug("DFGameSettings Loading Complete");
        }

        internal void Save(ConfigNode node)
        {
            ConfigNode settingsNode;
            if (node.HasNode(configNodeName))
            {
                settingsNode = node.GetNode(configNodeName);
            }
            else
            {
                settingsNode = node.AddNode(configNodeName);
            }

            settingsNode.AddValue("Enabled", Enabled);

            foreach (var entry in KnownFrozenKerbals)
            {
                ConfigNode vesselNode = entry.Value.Save(settingsNode);
                Utilities.Log_Debug("DFGameSettings Saving kerbal = " + entry.Key);
                vesselNode.AddValue("kerbalName", entry.Key);
            }

            foreach (var entry in knownVessels)
            {
                ConfigNode vesselNode = entry.Value.Save(settingsNode);
                Utilities.Log_Debug("DFGameSettings Saving Guid = " + entry.Key);
                vesselNode.AddValue("Guid", entry.Key);
            }

            foreach (var entry in knownFreezerParts)
            {
                ConfigNode partNode = entry.Value.Save(settingsNode);
                Utilities.Log_Debug("DFGameSettings Saving part flightID = " + entry.Key);
                partNode.AddValue("flightID", entry.Key);
            }

            foreach (var entry in knownKACAlarms)
            {
                ConfigNode alarmNode = entry.Value.Save(settingsNode);
                Utilities.Log_Debug("DFGameSettings Saving KACAlarm = " + entry.Key);
                alarmNode.AddValue("alarmID", entry.Key);
            }

            Utilities.Log_Debug("DFGameSettings Saving Complete");
        }

        internal void DmpKnownFznKerbals()
        {
            Utilities.Log_Debug("Dump of KnownFrozenKerbals");
            if (!KnownFrozenKerbals.Any())
            {
                Utilities.Log_Debug("KnownFrozenKerbals is EMPTY.");
            }
            else
            {
                foreach (KeyValuePair<string, KerbalInfo> kerbal in KnownFrozenKerbals)
                {
                    Utilities.Log_Debug("Kerbal = " + kerbal.Key + " status = " + kerbal.Value.status + " type = " +
                                        kerbal.Value.type + " vesselID = " + kerbal.Value.vesselID);
                }
            }
        }

        internal void DmpKnownVessels()
        {
            Utilities.Log_Debug("Dump of KnownVessels");
            if (!knownVessels.Any())
            {
                Utilities.Log_Debug("KnownVessels is EMPTY.");
            }
            else
            {
                foreach (KeyValuePair<Guid, VesselInfo> vessel in knownVessels)
                {
                    Utilities.Log_Debug("Vessel = " + vessel.Key + " Name = " + vessel.Value.vesselName + " ,crew = " +
                                        vessel.Value.numCrew + " ,frozencrew = " + vessel.Value.numFrznCrew);
                }
            }
        }

        internal void SyncDictionaries()
        {
            List<string> frznkerbalstoDelete = new List<string>();
            List<uint> freezerPartstoDelete = new List<uint>();
            //loop through all known frozen kerbals. Check there is a knownFreezerParts entry for them. If not, remove them.
            foreach (KeyValuePair<string, KerbalInfo> FrznKerbal in KnownFrozenKerbals)
            {
                if (!knownFreezerParts.ContainsKey(FrznKerbal.Value.partID))
                {
                    //Couldn't find one. Check the Roster, if they are in the roster set them to Missing.
                    //Remove from Dictionary
                    if (HighLogic.CurrentGame.CrewRoster != null)
                    {
                        var Kerbal = HighLogic.CurrentGame.CrewRoster.Crew.FirstOrDefault(a => a.name == FrznKerbal.Key);
                        if (Kerbal != null)
                        {
                            if (Kerbal.type == ProtoCrewMember.KerbalType.Crew ||
                                Kerbal.type == ProtoCrewMember.KerbalType.Unowned)
                            {
                                //set them to MIA
                            }
                        }
                    }
                    frznkerbalstoDelete.Add(FrznKerbal.Key);
                    Utilities.Log("Found orphaned Frozen Kerbal Entry in Database for " + FrznKerbal.Key +" Deleting entry");
                }
            }
            frznkerbalstoDelete.ForEach(id => KnownFrozenKerbals.Remove(id));
            //loop through all known frozen Freezer Parts. Check there is a knownVessels entry for them. If not, remove them.
            foreach (KeyValuePair<uint, PartInfo> FrzrPart in knownFreezerParts)
            {
                if (!knownVessels.ContainsKey(FrzrPart.Value.vesselID))
                {
                    //Couldn't find one. Remove from Dictionary
                    freezerPartstoDelete.Add(FrzrPart.Key);
                    Utilities.Log("Found orphaned Freezer Part Entry in Database for " + FrzrPart.Key + " Deleting entry");
                }
            }
            freezerPartstoDelete.ForEach(id => knownFreezerParts.Remove(id));
        }
    }
}