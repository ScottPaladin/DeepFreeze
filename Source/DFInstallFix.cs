/**
 * DFInstallFix.cs
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

namespace DF
{
    public class DFInstallFix
    {
        private const string configNodeName = "DFFixes";

        public bool fixtimecounts { get; set; }        

        internal DFInstallFix()
        {
            fixtimecounts = false;            
        }

        //Settings Functions Follow

        internal void Load(ConfigNode node)
        {
            if (node.HasNode(configNodeName))
            {
                ConfigNode DFsettingsNode = node.GetNode(configNodeName);

                fixtimecounts = Utilities.GetNodeValue(DFsettingsNode, "fixtimecounts", fixtimecounts);                
            }
        }

        internal void Save(ConfigNode node)
        {
            ConfigNode settingsNode;
            if (node.HasNode(configNodeName))
            {
                settingsNode = node.GetNode(configNodeName);
                settingsNode.ClearData();
            }
            else
            {
                settingsNode = node.AddNode(configNodeName);
            }

            settingsNode.AddValue("fixtimecounts", false);            
        }
    }
}