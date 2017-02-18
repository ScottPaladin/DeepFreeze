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

using DeepFreeze;
using RSTUtils;

namespace DF
{
    internal class DFSettings
    {
        // this class stores the DeepFreeze Settings from the config file.
        private const string configNodeName = "DFSettings";

        internal float DFwindowPosX ;
        internal float DFwindowPosY ;
        //internal float CFwindowPosX ;
        //internal float CFwindowPosY ;
        internal float DFKACwindowPosX ;
        internal float DFKACwindowPosY ;
        internal bool UseAppLauncher ;
        internal bool debugging ;
        internal bool ECreqdForFreezer ;
        internal bool fatalOption ;
        internal float comatoseTime ;
        internal bool AutoRecoverFznKerbals ;
        internal float KSCcostToThawKerbal ;
        internal int ECReqdToFreezeThaw ;
        internal int GlykerolReqdToFreeze ;
        internal bool RegTempReqd ;
        internal double RegTempFreeze ;
        internal double RegTempMonitor ;
        internal double heatamtMonitoringFrznKerbals ;
        internal double heatamtThawFreezeKerbal ;
        internal bool TempinKelvin ;
        internal double defaultTimeoutforCrewXfer ;
        internal double cryopodResettimeDelay ;
        internal float DFWindowWidth ;
        internal float CFWindowWidth ;
        internal float KACWindowWidth ;
        internal float WindowbaseHeight ;
        internal float ECLowWarningTime ;
        internal float EClowCriticalTime ;
        internal bool StripLightsActive ;
        internal int internalFrzrCamCode ;
        internal int internalNxtFrzrCamCode ;
        internal int internalPrvFrzrCamCode ;
        internal bool ToolTips ;
        internal bool EditorFilter;

        internal DFSettings()
        {
            DFwindowPosX = 40;
            DFwindowPosY = 50;
            //CFwindowPosX = 500;
            //CFwindowPosY = 140;
            DFKACwindowPosX = 600;
            DFKACwindowPosY = 50;
            UseAppLauncher = true;
            debugging = true;
            ECreqdForFreezer = true;
            fatalOption = true;
            comatoseTime = 300;
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
            cryopodResettimeDelay = 2;
            DFWindowWidth = 560f;
            CFWindowWidth = 340f;
            KACWindowWidth = 485f;
            ECLowWarningTime = 3600f;
            EClowCriticalTime = 900f;
            StripLightsActive = true;
            internalFrzrCamCode = 100;
            internalNxtFrzrCamCode = 110;
            internalPrvFrzrCamCode = 98;
            ToolTips = true;
            EditorFilter = true;
        }

        //Settings Functions Follow

        internal void Load(ConfigNode node)
        {
            if (node.HasNode(configNodeName))
            {
                ConfigNode DFsettingsNode = node.GetNode(configNodeName);

                DFsettingsNode.TryGetValue("DFwindowPosX", ref DFwindowPosX);
                DFsettingsNode.TryGetValue("DFwindowPosY", ref DFwindowPosY);
                //DFsettingsNode.TryGetValue("CFwindowPosX", ref CFwindowPosX);
                //DFsettingsNode.TryGetValue("CFwindowPosY", ref CFwindowPosY);
                DFsettingsNode.TryGetValue("DFKACwindowPosX", ref DFKACwindowPosX);
                DFsettingsNode.TryGetValue("DFKACwindowPosY", ref DFKACwindowPosY);
                DFsettingsNode.TryGetValue("defaultTimeoutforCrewXfer", ref defaultTimeoutforCrewXfer);
                DFsettingsNode.TryGetValue("cryopodResettimeDelay", ref cryopodResettimeDelay);
                DFsettingsNode.TryGetValue("DFWindowWidth", ref DFWindowWidth);
                DFsettingsNode.TryGetValue("CFWindowWidth", ref CFWindowWidth);
                DFsettingsNode.TryGetValue("KACWindowWidth", ref KACWindowWidth);
                DFsettingsNode.TryGetValue("ECLowWarningTime", ref ECLowWarningTime);
                DFsettingsNode.TryGetValue("EClowCriticalTime", ref EClowCriticalTime);
                DFsettingsNode.TryGetValue("internalFrzrCamCode", ref internalFrzrCamCode);
                DFsettingsNode.TryGetValue("internalNxtFrzrCamCode", ref internalNxtFrzrCamCode);
                DFsettingsNode.TryGetValue("internalPrvFrzrCamCode", ref internalPrvFrzrCamCode);


                ApplySettings();
                /*
                                DFsettingsNode.TryGetValue("ECreqdForFreezer", ref ECreqdForFreezer);
                                DFsettingsNode.TryGetValue("fatalOption", ref fatalOption);
                                DFsettingsNode.TryGetValue("comatoseTime", ref comatoseTime);
                                DFsettingsNode.TryGetValue("UseAppLauncher", ref UseAppLauncher);
                                DFsettingsNode.TryGetValue("debugging", ref debugging);
                                DFsettingsNode.TryGetValue("ToolTips", ref ToolTips);
                                DFsettingsNode.TryGetValue("AutoRecoverFznKerbals", ref AutoRecoverFznKerbals);
                                DFsettingsNode.TryGetValue("KSCcostToThawKerbal", ref KSCcostToThawKerbal);
                                DFsettingsNode.TryGetValue("ECReqdToFreezeThaw", ref ECReqdToFreezeThaw);
                                DFsettingsNode.TryGetValue("GlykerolReqdToFreeze", ref GlykerolReqdToFreeze);
                                DFsettingsNode.TryGetValue("RegTempReqd", ref RegTempReqd);
                                DFsettingsNode.TryGetValue("RegTempFreeze", ref RegTempFreeze);
                                DFsettingsNode.TryGetValue("RegTempMonitor", ref RegTempMonitor);
                                DFsettingsNode.TryGetValue("heatamtMonitoringFrznKerbals", ref heatamtMonitoringFrznKerbals);
                                DFsettingsNode.TryGetValue("heatamtThawFreezeKerbal", ref heatamtThawFreezeKerbal);
                                DFsettingsNode.TryGetValue("TempinKelvin", ref TempinKelvin);
                                DFsettingsNode.TryGetValue("EditorFilter", ref EditorFilter);
                                DFsettingsNode.TryGetValue("StripLightsActive", ref StripLightsActive);
                                Utilities.Log_Debug("DFSettings load complete");*/
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
            //settingsNode.AddValue("CFwindowPosX", CFwindowPosX);
            //settingsNode.AddValue("CFwindowPosY", CFwindowPosY);
            settingsNode.AddValue("DFKACwindowPosX", DFKACwindowPosX);
            settingsNode.AddValue("DFKACwindowPosY", DFKACwindowPosY);
            settingsNode.AddValue("defaultTimeoutforCrewXfer", defaultTimeoutforCrewXfer);
            settingsNode.AddValue("cryopodResettimeDelay", cryopodResettimeDelay);
            settingsNode.AddValue("DFWindowWidth", DFWindowWidth);
            settingsNode.AddValue("CFWindowWidth", CFWindowWidth);
            settingsNode.AddValue("KACWindowWidth", KACWindowWidth);
            settingsNode.AddValue("ECLowWarningTime", ECLowWarningTime);
            settingsNode.AddValue("EClowCriticalTime", EClowCriticalTime);
            settingsNode.AddValue("internalFrzrCamCode", internalFrzrCamCode);
            settingsNode.AddValue("internalNxtFrzrCamCode", internalNxtFrzrCamCode);
            settingsNode.AddValue("internalPrvFrzrCamCode", internalPrvFrzrCamCode);

            
            /*
            settingsNode.AddValue("ECreqdForFreezer", ECreqdForFreezer);
            settingsNode.AddValue("fatalOption", fatalOption);
            settingsNode.AddValue("comatoseTime", comatoseTime);
            settingsNode.AddValue("UseAppLauncher", UseAppLauncher);
            settingsNode.AddValue("debugging", debugging);
            settingsNode.AddValue("ToolTips", ToolTips);
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
            settingsNode.AddValue("EditorFilter", EditorFilter);
            settingsNode.AddValue("StripLightsActive", StripLightsActive);*/
            Utilities.Log_Debug("DFSettings save complete");
        }

        internal void ApplySettings()
        {
            Utilities.Log_Debug("DFSettings ApplySettings Start");
            if (HighLogic.CurrentGame != null)
            {
                var DF_SettingsParms = HighLogic.CurrentGame.Parameters.CustomParams<DeepFreeze_SettingsParms>();
                var DF_SettingsParms_Sec2 = HighLogic.CurrentGame.Parameters.CustomParams<DeepFreeze_SettingsParms_Sec2>();
                var DF_SettingsParms_Sec3 = HighLogic.CurrentGame.Parameters.CustomParams<DeepFreeze_SettingsParms_Sec3>();
                if (DF_SettingsParms != null)
                {
                    var GUI =
                        DeepFreeze.Instance.children.Find(a => a.GetType() == typeof(DeepFreezeGUI)) as DeepFreezeGUI;
                    if (ECreqdForFreezer != DF_SettingsParms.ECreqdForFreezer)
                    {
                        if (GUI != null)
                        {
                            GUI.chgECHeatsettings = true;
                            GUI.chgECHeatsettingsTimer = Planetarium.GetUniversalTime();
                        }
                    }
                    ECreqdForFreezer = DF_SettingsParms.ECreqdForFreezer;
                    fatalOption = DF_SettingsParms.fatalOption;
                    comatoseTime = DF_SettingsParms.comatoseTime;
                    if (UseAppLauncher != DF_SettingsParms_Sec3.UseAppLToolbar)
                    {
                        UseAppLauncher = DF_SettingsParms_Sec3.UseAppLToolbar;

                        if (GUI != null)
                        {
                            GUI.DFMenuAppLToolBar.chgAppIconStockToolBar(UseAppLauncher);
                        }
                    }
                    debugging = DF_SettingsParms_Sec3.DebugLogging;
                    Utilities.debuggingOn = debugging;
                    ToolTips = DF_SettingsParms_Sec3.ToolTips;
                    AutoRecoverFznKerbals = DF_SettingsParms.AutoRecoverFznKerbals;
                    KSCcostToThawKerbal = DF_SettingsParms.KSCcostToThawKerbal;
                    ECReqdToFreezeThaw = DF_SettingsParms.ECReqdToFreezeThaw;
                    GlykerolReqdToFreeze = DF_SettingsParms.GlykerolReqdToFreeze;
                    if (RegTempReqd != DF_SettingsParms_Sec2.RegTempReqd)
                    {
                        if (GUI != null)
                        {
                            GUI.chgECHeatsettings = true;
                            GUI.chgECHeatsettingsTimer = Planetarium.GetUniversalTime();
                        }
                    }
                    RegTempReqd = DF_SettingsParms_Sec2.RegTempReqd;
                    RegTempFreeze = DF_SettingsParms_Sec2.RegTempFreeze;
                    RegTempMonitor = DF_SettingsParms_Sec2.RegTempMonitor;
                    heatamtMonitoringFrznKerbals = DF_SettingsParms_Sec2.heatamtMonitoringFrznKerbals;
                    TempinKelvin = DF_SettingsParms_Sec2.TempinKelvin;
                    StripLightsActive = DF_SettingsParms_Sec3.StripLightsActive;
                    if (EditorFilter != DF_SettingsParms_Sec3.EditorFilter)
                    {
                        EditorFilter = DF_SettingsParms_Sec3.EditorFilter;
                        if (DFEditorFilter.Instance != null)
                            DFEditorFilter.Instance.Setup();
                    }
                }
                else
                    Utilities.Log_Debug("DFSettings ApplySettings Settings Params Not Set!");
            }
            else
                Utilities.Log_Debug("DFSettings ApplySettings CurrentGame is NULL!");
            Utilities.Log_Debug("DFSettings ApplySettings End");
        }
    }
}