/**
 * DFSettings.cs
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
    public class DFSettings
    {
        private const string configNodeName = "DFSettings";

        public float DFwindowPosX { get; set; }
        public float DFwindowPosY { get; set; }
        public bool UseAppLauncher { get; set; }
        public bool debugging { get; set; }
        public bool ECreqdForFreezer { get; set; }
        public bool AutoRecoverFznKerbals { get; set; }
        public float KSCcostToThawKerbal { get; set; }

        public DFSettings()
        {
            DFwindowPosX = 40;
            DFwindowPosY = 50;
            UseAppLauncher = true;
            debugging = true;
            ECreqdForFreezer = true;
            AutoRecoverFznKerbals = true;
            KSCcostToThawKerbal = 10000f;
        }

        //Settings Functions Follow

        public void Load(ConfigNode node)
        {
            if (node.HasNode(configNodeName))
            {
                ConfigNode DFsettingsNode = node.GetNode(configNodeName);

                DFwindowPosX = Utilities.GetNodeValue(DFsettingsNode, "DFwindowPosX", DFwindowPosX);
                DFwindowPosY = Utilities.GetNodeValue(DFsettingsNode, "DFwindowPosY", DFwindowPosY);
                ECreqdForFreezer = Utilities.GetNodeValue(DFsettingsNode, "ECreqdForFreezer", ECreqdForFreezer);
                UseAppLauncher = Utilities.GetNodeValue(DFsettingsNode, "UseAppLauncher", UseAppLauncher);
                debugging = Utilities.GetNodeValue(DFsettingsNode, "debugging", debugging);
                AutoRecoverFznKerbals = Utilities.GetNodeValue(DFsettingsNode, "AutoRecoverFznKerbals", AutoRecoverFznKerbals);
                KSCcostToThawKerbal = Utilities.GetNodeValue(DFsettingsNode, "KSCcostToThawKerbal", KSCcostToThawKerbal);
                this.Log_Debug("DFSettings load complete");
            }
        }

        public void Save(ConfigNode node)
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

            settingsNode.AddValue("DFwindowPosX", DFwindowPosX);
            settingsNode.AddValue("DFwindowPosY", DFwindowPosY);
            settingsNode.AddValue("ECreqdForFreezer", ECreqdForFreezer);
            settingsNode.AddValue("UseAppLauncher", UseAppLauncher);
            settingsNode.AddValue("debugging", debugging);
            settingsNode.AddValue("AutoRecoverFznKerbals", AutoRecoverFznKerbals);
            settingsNode.AddValue("KSCcostToThawKerbal", KSCcostToThawKerbal);
            this.Log_Debug("DFSettings save complete");
        }
    }
}