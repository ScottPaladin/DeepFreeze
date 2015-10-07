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

namespace DF
{
    internal class DFSettings
    {
        // this class stores the DeepFreeze Settings from the config file.
        private const string configNodeName = "DFSettings";

        internal float DFwindowPosX { get; set; }
        internal float DFwindowPosY { get; set; }
        internal float CFwindowPosX { get; set; }
        internal float CFwindowPosY { get; set; }
        internal float DFKACwindowPosX { get; set; }
        internal float DFKACwindowPosY { get; set; }
        internal bool UseAppLauncher { get; set; }
        internal bool debugging { get; set; }
        internal bool ECreqdForFreezer { get; set; }
        internal bool fatalOption { get; set; }
        internal bool AutoRecoverFznKerbals { get; set; }
        internal float KSCcostToThawKerbal { get; set; }
        internal int ECReqdToFreezeThaw { get; set; }
        internal int GlykerolReqdToFreeze { get; set; }
        internal bool RegTempReqd { get; set; }
        internal double RegTempFreeze { get; set; }
        internal double RegTempMonitor { get; set; }
        internal double heatamtMonitoringFrznKerbals { get; set; }
        internal double heatamtThawFreezeKerbal { get; set; }
        internal bool TempinKelvin { get; set; }
        internal double defaultTimeoutforCrewXfer { get; set; }
        internal double cryopodResettimeDelay { get; set; }
        internal float DFWindowWidth { get; set; }
        internal float CFWindowWidth { get; set; }
        internal float KACWindowWidth { get; set; }
        internal float WindowbaseHeight { get; set; }
        internal float ECLowWarningTime { get; set; }
        internal float EClowCriticalTime { get; set; }
        internal bool StripLightsActive { get; set; }
        internal int internalFrzrCamCode { get; set; }
        internal int internalNxtFrzrCamCode { get; set; }
        internal int internalPrvFrzrCamCode { get; set; }

        internal DFSettings()
        {
            DFwindowPosX = 40;
            DFwindowPosY = 50;
            CFwindowPosX = 380;
            CFwindowPosY = 50;
            DFKACwindowPosX = 600;
            DFKACwindowPosY = 50;
            UseAppLauncher = true;
            debugging = true;
            ECreqdForFreezer = true;
            fatalOption = true;
            AutoRecoverFznKerbals = true;
            KSCcostToThawKerbal = 10000f;
            ECReqdToFreezeThaw = 3000;
            GlykerolReqdToFreeze = 5;
            RegTempReqd = false;
            RegTempFreeze = 300f;
            RegTempMonitor = 400f;
            heatamtMonitoringFrznKerbals = 10f;
            heatamtThawFreezeKerbal = 100f;
            TempinKelvin = true;
            defaultTimeoutforCrewXfer = 30;
            cryopodResettimeDelay = 5;
            DFWindowWidth = 420f;
            CFWindowWidth = 340f;
            KACWindowWidth = 485f;
            ECLowWarningTime = 3600f;
            EClowCriticalTime = 900f;
            StripLightsActive = true;
            internalFrzrCamCode = 100;
            internalNxtFrzrCamCode = 110;
            internalPrvFrzrCamCode = 98;
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
                DFKACwindowPosX = Utilities.GetNodeValue(DFsettingsNode, "DFKACwindowPosX", DFKACwindowPosX);
                DFKACwindowPosY = Utilities.GetNodeValue(DFsettingsNode, "DFKACwindowPosY", DFKACwindowPosY);
                ECreqdForFreezer = Utilities.GetNodeValue(DFsettingsNode, "ECreqdForFreezer", ECreqdForFreezer);
                fatalOption = Utilities.GetNodeValue(DFsettingsNode, "fatalOption", fatalOption);
                UseAppLauncher = Utilities.GetNodeValue(DFsettingsNode, "UseAppLauncher", UseAppLauncher);
                debugging = Utilities.GetNodeValue(DFsettingsNode, "debugging", debugging);
                AutoRecoverFznKerbals = Utilities.GetNodeValue(DFsettingsNode, "AutoRecoverFznKerbals", AutoRecoverFznKerbals);
                KSCcostToThawKerbal = Utilities.GetNodeValue(DFsettingsNode, "KSCcostToThawKerbal", KSCcostToThawKerbal);
                ECReqdToFreezeThaw = Utilities.GetNodeValue(DFsettingsNode, "ECReqdToFreezeThaw", ECReqdToFreezeThaw);
                GlykerolReqdToFreeze = Utilities.GetNodeValue(DFsettingsNode, "GlykerolReqdToFreeze", GlykerolReqdToFreeze);
                RegTempReqd = Utilities.GetNodeValue(DFsettingsNode, "RegTempReqd", RegTempReqd);
                RegTempFreeze = Utilities.GetNodeValue(DFsettingsNode, "RegTempFreeze", RegTempFreeze);
                RegTempMonitor = Utilities.GetNodeValue(DFsettingsNode, "RegTempMonitor", RegTempMonitor);
                heatamtMonitoringFrznKerbals = Utilities.GetNodeValue(DFsettingsNode, "heatamtMonitoringFrznKerbals", heatamtMonitoringFrznKerbals);
                heatamtThawFreezeKerbal = Utilities.GetNodeValue(DFsettingsNode, "heatamtThawFreezeKerbal", heatamtThawFreezeKerbal);
                TempinKelvin = Utilities.GetNodeValue(DFsettingsNode, "TempinKelvin", TempinKelvin);
                defaultTimeoutforCrewXfer = Utilities.GetNodeValue(DFsettingsNode, "defaultTimeoutforCrewXfer", defaultTimeoutforCrewXfer);
                cryopodResettimeDelay = Utilities.GetNodeValue(DFsettingsNode, "cryopodResettimeDelay", cryopodResettimeDelay);
                DFWindowWidth = Utilities.GetNodeValue(DFsettingsNode, "DFWindowWidth", DFWindowWidth);
                CFWindowWidth = Utilities.GetNodeValue(DFsettingsNode, "CFWindowWidth", CFWindowWidth);
                KACWindowWidth = Utilities.GetNodeValue(DFsettingsNode, "KACWindowWidth", KACWindowWidth);
                ECLowWarningTime = Utilities.GetNodeValue(DFsettingsNode, "ECLowWarningTime", ECLowWarningTime);
                EClowCriticalTime = Utilities.GetNodeValue(DFsettingsNode, "EClowCriticalTime", EClowCriticalTime);
                StripLightsActive = Utilities.GetNodeValue(DFsettingsNode, "StripLightsActive", StripLightsActive);
                internalFrzrCamCode = Utilities.GetNodeValue(DFsettingsNode, "internalFrzrCamCode", internalFrzrCamCode);
                internalNxtFrzrCamCode = Utilities.GetNodeValue(DFsettingsNode, "internalNxtFrzrCamCode", internalNxtFrzrCamCode);
                internalPrvFrzrCamCode = Utilities.GetNodeValue(DFsettingsNode, "internalPrvFrzrCamCode", internalPrvFrzrCamCode);
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
            settingsNode.AddValue("DFKACwindowPosX", DFKACwindowPosX);
            settingsNode.AddValue("DFKACwindowPosY", DFKACwindowPosY);
            settingsNode.AddValue("ECreqdForFreezer", ECreqdForFreezer);
            settingsNode.AddValue("fatalOption", fatalOption);
            settingsNode.AddValue("UseAppLauncher", UseAppLauncher);
            settingsNode.AddValue("debugging", debugging);
            settingsNode.AddValue("AutoRecoverFznKerbals", AutoRecoverFznKerbals);
            settingsNode.AddValue("KSCcostToThawKerbal", KSCcostToThawKerbal);
            settingsNode.AddValue("ECReqdToFreezeThaw", ECReqdToFreezeThaw);
            settingsNode.AddValue("GlykerolReqdToFreeze", GlykerolReqdToFreeze);
            settingsNode.AddValue("RegTempReqd", RegTempReqd);
            settingsNode.AddValue("RegTempFreeze", RegTempFreeze);
            settingsNode.AddValue("RegTempMonitor", RegTempMonitor);
            settingsNode.AddValue("heatamtMonitoringFrznKerbals", heatamtMonitoringFrznKerbals);
            settingsNode.AddValue("heatamtThawFreezeKerbal", heatamtThawFreezeKerbal);
            settingsNode.AddValue("TempinKelvin", TempinKelvin);
            settingsNode.AddValue("defaultTimeoutforCrewXfer", defaultTimeoutforCrewXfer);
            settingsNode.AddValue("cryopodResettimeDelay", cryopodResettimeDelay);
            settingsNode.AddValue("DFWindowWidth", DFWindowWidth);
            settingsNode.AddValue("CFWindowWidth", CFWindowWidth);
            settingsNode.AddValue("KACWindowWidth", KACWindowWidth);
            settingsNode.AddValue("ECLowWarningTime", ECLowWarningTime);
            settingsNode.AddValue("EClowCriticalTime", EClowCriticalTime);
            settingsNode.AddValue("StripLightsActive", StripLightsActive);
            settingsNode.AddValue("internalFrzrCamCode", internalFrzrCamCode);
            settingsNode.AddValue("internalNxtFrzrCamCode", internalNxtFrzrCamCode);
            settingsNode.AddValue("internalPrvFrzrCamCode", internalPrvFrzrCamCode);
            this.Log_Debug("DFSettings save complete");
        }
    }
}