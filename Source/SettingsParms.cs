using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace DF
{
    public class DeepFreeze_SettingsParms : GameParameters.CustomParameterNode

    {
        public override string Title { get { return "DeepFreeze Options"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override bool HasPresets { get { return true; } }
        public override string Section { get { return "DeepFreeze"; } }
        public override int SectionOrder { get { return 1; } }

        
        [GameParameters.CustomParameterUI("ElectricCharge Required to run Freezers", autoPersistance = true, toolTip = "If on, EC is required to run freezers")]
        public bool ECreqdForFreezer = false;

        [GameParameters.CustomParameterUI("Fatal EC/Heat Option", autoPersistance = true, toolTip = "If on Kerbals will die if EC runs out or it gets too hot")]
        public bool fatalOption = false;

        [GameParameters.CustomIntParameterUI("Non Fatal Comatose Time(in secs)", minValue = 60, maxValue = 10000, stepSize = 60, autoPersistance = true, toolTip = "The time in seconds a kerbal is comatose\n if fatal EC / Heat option is off")]
        public int comatoseTime = 300;

        [GameParameters.CustomParameterUI("AutoRecover Frozen Kerbals at KSC", autoPersistance = true, toolTip = "If on, will AutoRecover Frozen Kerbals at the KSC\n and deduct the Cost from your funds")]
        public bool AutoRecoverFznKerbals = false;

        [GameParameters.CustomFloatParameterUI("Cost to Thaw a Kerbal at KSC",toolTip = "Amt of currency Reqd to Freeze a Kerbal from the KSC", minValue = 0, maxValue = 500000, gameMode = GameParameters.GameMode.CAREER)]
        public float KSCcostToThawKerbal = 10000f;

        [GameParameters.CustomIntParameterUI("EC Reqd to Freeze/Thaw a Kerbal", autoPersistance = true, minValue = 0, maxValue = 10000, stepSize = 10, toolTip = "Amt of ElecCharge Reqd to Freeze/Thaw a Kerbal.")]
        public int ECReqdToFreezeThaw = 3000;

        [GameParameters.CustomIntParameterUI("Glykerol Reqd to Freeze a Kerbal", autoPersistance = true, minValue = 0, maxValue = 50, toolTip = "Amt of Glykerol used to Freeze a Kerbal,\nOverrides Part values.")]
        public int GlykerolReqdToFreeze = 5;
        
        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            Debug.Log("Setting difficulty preset");
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    this.ECreqdForFreezer = false;
                    this.AutoRecoverFznKerbals = true;
                    this.KSCcostToThawKerbal = 5000f;
                    this.ECReqdToFreezeThaw = 1000;
                    this.GlykerolReqdToFreeze = 5;
                    break;
                case GameParameters.Preset.Normal:
                    this.ECreqdForFreezer = false;
                    this.AutoRecoverFznKerbals = true;
                    this.KSCcostToThawKerbal = 10000f;
                    this.ECReqdToFreezeThaw = 2000;
                    this.GlykerolReqdToFreeze = 5;
                    break;
                case GameParameters.Preset.Moderate:
                    this.ECreqdForFreezer = true;
                    this.AutoRecoverFznKerbals = false;
                    this.KSCcostToThawKerbal = 20000f;
                    this.ECReqdToFreezeThaw = 3000;
                    this.GlykerolReqdToFreeze = 10;
                    break;
                case GameParameters.Preset.Hard:
                    this.ECreqdForFreezer = true;
                    this.AutoRecoverFznKerbals = false;
                    this.KSCcostToThawKerbal = 30000f;
                    this.ECReqdToFreezeThaw = 4000;
                    this.GlykerolReqdToFreeze = 15;
                    break;
            }
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (HighLogic.fetch != null)
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (HighLogic.fetch != null)
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    return false;
                }
            }
            if (member.Name == "fatalOption")
            {
                return parameters.CustomParams<DeepFreeze_SettingsParms>().ECreqdForFreezer;
            }
            if (member.Name == "comatoseTime")
            {
                return (parameters.CustomParams<DeepFreeze_SettingsParms>().ECreqdForFreezer &&
                        !parameters.CustomParams<DeepFreeze_SettingsParms>().fatalOption);
            }
            if (member.Name == "KSCcostToThawKerbal")
            {
                return parameters.CustomParams<DeepFreeze_SettingsParms>().AutoRecoverFznKerbals;
            }
            
            return true;
        }
    }

    public class DeepFreeze_SettingsParms_Sec2 : GameParameters.CustomParameterNode

    {
        public override string Title { get { return "DeepFreeze Temperatures"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override bool HasPresets { get { return true; } }
        public override string Section { get { return "DeepFreeze"; } }
        public override int SectionOrder { get { return 2; } }
        
        [GameParameters.CustomStringParameterUI("Test String UI", lines = 3, title = "", toolTip = "Get your calculator out.")]
        public string CBstring = "Temps are in (K)elvin. (K) = (C)elcius + 273.15. (K) = ((F)arenheit + 459.67) × 5/9. Get your calculator out.";

        [GameParameters.CustomParameterUI("Regulated Temperatures Required", autoPersistance = true, toolTip = "If on, Regulated Temps apply to freeze\nand keep Kerbals Frozen.")]
        public bool RegTempReqd = false;

        [GameParameters.CustomFloatParameterUI("Min. Temp. for Freezer to Freeze(K)", autoPersistance = true, minValue = 0, maxValue = 400, toolTip = "The minimum temperature (in Kelvin) for a Freezer\nto be able to Freeze a Kerbal.")]
        public float RegTempFreeze = 300f;

        [GameParameters.CustomFloatParameterUI("Max. Temp. to keep Kerbals Frozen(K)", autoPersistance = true, minValue = 0, maxValue = 800, toolTip = "The maximum temperature (in Kelvin) for a Freezer\nto keep Kerbals frozen.")]
        public float RegTempMonitor = 400f;

        [GameParameters.CustomFloatParameterUI("Heat generated per kerbal (kW/min)", autoPersistance = true, minValue = 10, maxValue = 1000, toolTip = "Amount of thermal heat (kW) generated\nby equipment for each frozen kerbal per minute.")]
        public float heatamtMonitoringFrznKerbals = 100f;

        [GameParameters.CustomFloatParameterUI("Heat generated freezer process(kW)", autoPersistance = true, minValue = 10, maxValue = 3000, toolTip = "Amount of thermal heat (kW) generated\nwith each thaw/freeze process.")]
        public float heatamtThawFreezeKerbal = 1000f;

        [GameParameters.CustomParameterUI("Show Part Temperatures in Kelvin", autoPersistance = true, toolTip = "If on Part right click will show temp in Kelvin,\nif Off will show in Celcius.")]
        public bool TempinKelvin = false;
        
        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            Debug.Log("Setting difficulty preset");
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    this.RegTempReqd = false;
                    break;
                case GameParameters.Preset.Normal:
                    this.RegTempReqd = false;
                    break;
                case GameParameters.Preset.Moderate:
                    this.RegTempReqd = true;
                    break;
                case GameParameters.Preset.Hard:
                    this.RegTempReqd = true;
                    break;
            }
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (HighLogic.fetch != null)
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    if (member.Name != "TempinKelvin")
                        return false;
                }
            }
            
            if (member.Name == "RegTempFreeze" || member.Name == "RegTempMonitor" || member.Name == "heatamtMonitoringFrznKerbals" || member.Name == "heatamtThawFreezeKerbal")
            {
                return parameters.CustomParams<DeepFreeze_SettingsParms_Sec2>().RegTempReqd;
            }
            
            return true;
        }
    }

    public class DeepFreeze_SettingsParms_Sec3 : GameParameters.CustomParameterNode

    {
        public override string Title { get { return "DeepFreeze Misc."; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override bool HasPresets { get { return false; } }
        public override string Section { get { return "DeepFreeze"; } }
        public override int SectionOrder { get { return 3; } }
        
        [GameParameters.CustomParameterUI("Freezer Strip Lights On", autoPersistance = true, toolTip = "Turn off if you do not want the internal\nfreezer strip lights to function.")]
        public bool StripLightsActive = true;

        [GameParameters.CustomParameterUI("ToolTips On", autoPersistance = true, toolTip = "Turn the Tooltips on and off.")]
        public bool ToolTips = true;

        [GameParameters.CustomParameterUI("Editor Filter", autoPersistance = true, toolTip = "Turn the DeepFreeze Editor filter Category on and off.")]
        public bool EditorFilter = true;

        [GameParameters.CustomParameterUI("Use Stock App Launcher Icon", toolTip = "If on, the Stock Application launcher will be used,\nif off will use Blizzy Toolbar if installed.")]
        public bool UseAppLToolbar = true;

        [GameParameters.CustomParameterUI("Extra Debug Logging", toolTip = "Turn this On to capture lots of extra information\ninto the KSP log for reporting a problem.")]
        public bool DebugLogging = false;
        
        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (member.Name == "UseAppLToolbar")
            {
                if (RSTUtils.ToolbarManager.ToolbarAvailable)
                    return true;
                return false;
            }

            return true;
        }
    }
}
