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
 */

using System;
using System.Collections.Generic;

namespace DF
{
    internal class AlarmInfo
    {
        //This class stores Info about Kerbal Alarm Clock Alarms that are associated with DeepFreeze Vessels.
        //VesselID              - VesselID of the Vessel this Alarm is attached to
        //string Name           - The name of the KAC Alarm
        //AlarmType             - The type of KAC Alarm
        //Notes                 - The KAC Alarm notes, has THAW xxxx and FREEZE xxxx lines added to it for Freeze and Thaw DeepFreeze commands to execute when the alarm triggers.
        //AlarmTime             - The time of the Alarm from KAC
        //AlarmMargin           - The margin of the Alarm from KAC
        //FrzKerbals            - a List of kerbals to freeze from the vessel when the alarm fires.
        //ThwKerbals            - a List of kerbals to thaw from the vessel when the alarm fires.
        //AlarmExecute          - Bool is true when Alarm has fired from KAC and we are processing it in DeepFreeze.
        //
        internal const string ConfigNodeName = "AlarmInfo";

        internal Guid VesselID;
        internal string Name;
        internal KACWrapper.KACAPI.AlarmTypeEnum AlarmType;
        internal string Notes;
        internal double AlarmTime;
        internal double AlarmMargin;
        internal List<string> FrzKerbals;
        internal List<string> ThwKerbals;
        internal bool AlarmExecute;

        internal AlarmInfo(string Name, Guid vesselID)
        {
            this.Name = Name;
            this.VesselID = vesselID;
            this.FrzKerbals = new List<string>();
            this.ThwKerbals = new List<string>();
        }

        internal static AlarmInfo Load(ConfigNode node)
        {
            Guid vesselID = Utilities.GetNodeValue(node, "vesselID");
            string Name = Utilities.GetNodeValue(node, "Name", string.Empty);
            AlarmInfo info = new AlarmInfo(Name, vesselID);
            info.AlarmType = Utilities.GetNodeValue(node, "AlarmType", KACWrapper.KACAPI.AlarmTypeEnum.Raw);
            info.Notes = Utilities.GetNodeValue(node, "Notes", string.Empty);
            info.AlarmTime = Utilities.GetNodeValue(node, "AlarmTime", 0d);
            info.AlarmMargin = Utilities.GetNodeValue(node, "AlarmMargin", 0d);
            info.AlarmExecute = Utilities.GetNodeValue(node, "AlarmExecute", false);
            string frzkbllst = Utilities.GetNodeValue(node, "FrzKerbals", string.Empty);
            string thwkbllst = Utilities.GetNodeValue(node, "ThwKerbals", string.Empty);
            string[] frzStrings = frzkbllst.Split(',');
            if (frzStrings.Length > 0)
            {
                for (int i = 0; i < frzStrings.Length; i++)
                {
                    info.FrzKerbals.Add(frzStrings[i]);
                }
            }
            string[] thwStrings = thwkbllst.Split(',');
            if (thwStrings.Length > 0)
            {
                for (int i = 0; i < thwStrings.Length; i++)
                {
                    if (thwStrings[i].Length > 0)
                        info.ThwKerbals.Add(thwStrings[i]);
                }
            }
            return info;
        }

        internal ConfigNode Save(ConfigNode config)
        {
            ConfigNode node = config.AddNode(ConfigNodeName);
            node.AddValue("vesselID", VesselID);
            node.AddValue("Name", Name);
            node.AddValue("AlarmType", AlarmType.ToString());
            node.AddValue("Notes", Notes);
            node.AddValue("AlarmTime", AlarmTime);
            node.AddValue("AlarmMargin", AlarmMargin);
            node.AddValue("AlarmExecute", AlarmExecute);
            string frzkbllst = string.Join(",", FrzKerbals.ToArray());
            node.AddValue("FrzKerbals", frzkbllst);
            string thwkbllst = string.Join(",", ThwKerbals.ToArray());
            node.AddValue("ThwKerbals", thwkbllst);
            return node;
        }

        internal void ClearFrzKerbals()
        {
            FrzKerbals.Clear();
        }

        internal void ClearThwKerbals()
        {
            ThwKerbals.Clear();
        }
    }
}