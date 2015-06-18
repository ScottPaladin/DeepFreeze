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
        public float CFwindowPosX { get; set; }
        public float CFwindowPosY { get; set; }
        public bool UseAppLauncher { get; set; }
        public bool debugging { get; set; }
        public bool ECreqdForFreezer { get; set; }
        public bool AutoRecoverFznKerbals { get; set; }
        public float KSCcostToThawKerbal { get; set; }
        public int ECReqdToFreezeThaw { get; set; }
        public int GlykerolReqdToFreeze { get; set; }
        public bool RegTempReqd { get; set; }
        public double RegTempFreeze { get; set; }
        public double RegTempMonitor { get; set; }
        public bool TempinKelvin { get; set; }

        internal DFSettings()
        {
            DFwindowPosX = 40;
            DFwindowPosY = 50;
            CFwindowPosX = 380;
            CFwindowPosY = 50;
            UseAppLauncher = true;
            debugging = true;
            ECreqdForFreezer = true;
            AutoRecoverFznKerbals = true;
            KSCcostToThawKerbal = 10000f;
            ECReqdToFreezeThaw = 3000;
            GlykerolReqdToFreeze = 5;
            RegTempReqd = false;
            RegTempFreeze = 300f;
            RegTempMonitor = 400f;
            TempinKelvin = true;
        }

        //Settings Functions Follow

        internal void Load(ConfigNode node)
        {
            if (node.HasNode(configNodeName))
            {
                ConfigNode DFsettingsNode = node.GetNode(configNodeName);

                DFwindowPosX = Utilities.GetNodeValue(DFsettingsNode, "DFwindowPosX", DFwindowPosX);
                DFwindowPosY = Utilities.GetNodeValue(DFsettingsNode, "DFwindowPosY", DFwindowPosY);
                CFwindowPosX = Utilities.GetNodeValue(DFsettingsNode, "CFwindowPosX", CFwindowPosX);
                CFwindowPosY = Utilities.GetNodeValue(DFsettingsNode, "CFwindowPosY", CFwindowPosY);
                ECreqdForFreezer = Utilities.GetNodeValue(DFsettingsNode, "ECreqdForFreezer", ECreqdForFreezer);
                UseAppLauncher = Utilities.GetNodeValue(DFsettingsNode, "UseAppLauncher", UseAppLauncher);
                debugging = Utilities.GetNodeValue(DFsettingsNode, "debugging", debugging);
                AutoRecoverFznKerbals = Utilities.GetNodeValue(DFsettingsNode, "AutoRecoverFznKerbals", AutoRecoverFznKerbals);
                KSCcostToThawKerbal = Utilities.GetNodeValue(DFsettingsNode, "KSCcostToThawKerbal", KSCcostToThawKerbal);
                ECReqdToFreezeThaw = Utilities.GetNodeValue(DFsettingsNode, "ECReqdToFreezeThaw", ECReqdToFreezeThaw);
                GlykerolReqdToFreeze = Utilities.GetNodeValue(DFsettingsNode, "GlykerolReqdToFreeze", GlykerolReqdToFreeze);
                RegTempReqd = Utilities.GetNodeValue(DFsettingsNode, "RegTempReqd", RegTempReqd);
                RegTempFreeze = Utilities.GetNodeValue(DFsettingsNode, "RegTempFreeze", RegTempFreeze);
                RegTempMonitor = Utilities.GetNodeValue(DFsettingsNode, "RegTempMonitor", RegTempMonitor);
                TempinKelvin = Utilities.GetNodeValue(DFsettingsNode, "TempinKelvin", TempinKelvin);
                this.Log_Debug("DFSettings load complete");
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

            settingsNode.AddValue("DFwindowPosX", DFwindowPosX);
            settingsNode.AddValue("DFwindowPosY", DFwindowPosY);
            settingsNode.AddValue("CFwindowPosX", CFwindowPosX);
            settingsNode.AddValue("CFwindowPosY", CFwindowPosY);
            settingsNode.AddValue("ECreqdForFreezer", ECreqdForFreezer);
            settingsNode.AddValue("UseAppLauncher", UseAppLauncher);
            settingsNode.AddValue("debugging", debugging);
            settingsNode.AddValue("AutoRecoverFznKerbals", AutoRecoverFznKerbals);
            settingsNode.AddValue("KSCcostToThawKerbal", KSCcostToThawKerbal);
            settingsNode.AddValue("ECReqdToFreezeThaw", ECReqdToFreezeThaw);
            settingsNode.AddValue("GlykerolReqdToFreeze", GlykerolReqdToFreeze);
            settingsNode.AddValue("RegTempReqd", RegTempReqd);
            settingsNode.AddValue("RegTempFreeze", RegTempFreeze);
            settingsNode.AddValue("RegTempMonitor", RegTempMonitor);
            settingsNode.AddValue("TempinKelvin", TempinKelvin);
            this.Log_Debug("DFSettings save complete");
        }
    }
}