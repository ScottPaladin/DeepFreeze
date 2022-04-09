using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace DF
{
    public class DeepFreeze_SettingsParms : GameParameters.CustomParameterNode

    {
        public override string Title { get { return Localizer.Format("#autoLOC_DF_00144"); } } //#autoLOC_DF_00144 = DeepFreeze Options
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override bool HasPresets { get { return true; } }
        public override string Section { get { return "DeepFreeze"; } }
        public override string DisplaySection { get { return Localizer.Format("#autoLOC_DF_00003"); } } //#autoLOC_DF_00003 = DeepFreeze
        public override int SectionOrder { get { return 1; } }

        
        [GameParameters.CustomParameterUI("#autoLOC_DF_00145", autoPersistance = true, toolTip = "#autoLOC_DF_00146")] //#autoLOC_DF_00145 = ElectricCharge Required to run Freezers #autoLOC_DF_00146 = If on, EC is required to run freezers
        public bool ECreqdForFreezer = false;

        [GameParameters.CustomParameterUI("#autoLOC_DF_00201", autoPersistance = true, toolTip = "#autoLOC_DF_00202")] //#autoLOC_DF_00201 = Unloaded Vessel Processing  //#autoLOC_DF_00202 = If enabled DeepFreeze will process resources on unloaded vessels. If disabled, it won't and play the catchup and estimation game.
        public bool backgroundresources = true;
        
        [GameParameters.CustomParameterUI("#autoLOC_DF_00147", autoPersistance = true, toolTip = "#autoLOC_DF_00148")] //#autoLOC_DF_00147 = Fatal EC/Heat Option #autoLOC_DF_00148 = If on Kerbals will die if EC runs out or it gets too hot
        public bool fatalOption = false;

        [GameParameters.CustomIntParameterUI("#autoLOC_DF_00149", minValue = 60, maxValue = 10000, stepSize = 60, autoPersistance = true, toolTip = "#autoLOC_DF_00150")] //#autoLOC_DF_00149 = Non Fatal Comatose Time(in secs) #autoLOC_DF_00150 = The time in seconds a kerbal is comatose\n if fatal EC / Heat option is off
        public int comatoseTime = 300;

        [GameParameters.CustomParameterUI("#autoLOC_DF_00151", autoPersistance = true, toolTip = "#autoLOC_DF_00152")] //#autoLOC_DF_00151 = AutoRecover Frozen Kerbals at KSC #autoLOC_DF_00152 = If on, will AutoRecover Frozen Kerbals at the KSC\n and deduct the Cost from your funds
        public bool AutoRecoverFznKerbals = false;

        [GameParameters.CustomFloatParameterUI("#autoLOC_DF_00153", toolTip = "#autoLOC_DF_00154", minValue = 0, maxValue = 500000, gameMode = GameParameters.GameMode.CAREER)] //#autoLOC_DF_00153 = Cost to Thaw a Kerbal at KSC #autoLOC_DF_00154 = Amt of currency Reqd to Freeze a Kerbal from the KSC
        public float KSCcostToThawKerbal = 10000f;

        [GameParameters.CustomIntParameterUI("#autoLOC_DF_00155", autoPersistance = true, minValue = 0, maxValue = 10000, stepSize = 10, toolTip = "#autoLOC_DF_00156")] //#autoLOC_DF_00155 = EC Reqd to Freeze/Thaw a Kerbal #autoLOC_DF_00156 = Amt of ElecCharge Reqd to Freeze/Thaw a Kerbal.
        public int ECReqdToFreezeThaw = 3000;

        [GameParameters.CustomIntParameterUI("#autoLOC_DF_00157", autoPersistance = true, minValue = 0, maxValue = 50, toolTip = "#autoLOC_DF_00158")] //#autoLOC_DF_00157 = Glykerol Reqd to Freeze a Kerbal #autoLOC_DF_00158 = Amt of Glykerol used to Freeze a Kerbal,\nOverrides Part values.
        public int GlykerolReqdToFreeze = 5;
        
        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            Debug.Log("Setting difficulty preset");
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    this.ECreqdForFreezer = false;
                    this.backgroundresources = false;
                    this.AutoRecoverFznKerbals = true;
                    this.KSCcostToThawKerbal = 5000f;
                    this.ECReqdToFreezeThaw = 1000;
                    this.GlykerolReqdToFreeze = 5;
                    break;
                case GameParameters.Preset.Normal:
                    this.ECreqdForFreezer = false;
                    this.backgroundresources = false;
                    this.AutoRecoverFznKerbals = true;
                    this.KSCcostToThawKerbal = 10000f;
                    this.ECReqdToFreezeThaw = 2000;
                    this.GlykerolReqdToFreeze = 5;
                    break;
                case GameParameters.Preset.Moderate:
                    this.ECreqdForFreezer = true;
                    this.backgroundresources = true;
                    this.AutoRecoverFznKerbals = false;
                    this.KSCcostToThawKerbal = 20000f;
                    this.ECReqdToFreezeThaw = 3000;
                    this.GlykerolReqdToFreeze = 10;
                    break;
                case GameParameters.Preset.Hard:
                    this.ECreqdForFreezer = true;
                    this.backgroundresources = true;
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
            if (member.Name == "backgroundresources")
            {
                return DFInstalledMods.IsBGRInstalled;
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
        public override string Title { get { return Localizer.Format("#autoLOC_DF_00159"); } } //#autoLOC_DF_00159 = DeepFreeze Temperatures
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override bool HasPresets { get { return true; } }
        public override string Section { get { return "DeepFreeze"; } }
        public override string DisplaySection { get { return Localizer.Format("#autoLOC_DF_00003"); } } //#autoLOC_DF_00003 = DeepFreeze
        public override int SectionOrder { get { return 2; } }
        
        [GameParameters.CustomStringParameterUI("Test String UI", lines = 3, title = "", toolTip = "#autoLOC_DF_00160")] //#autoLOC_DF_00160 = Get your calculator out.
        public string CBstring = "#autoLOC_DF_00161"; //#autoLOC_DF_00161 = Temps are in (K)elvin. (K) = (C)elcius + 273.15. (K) = ((F)arenheit + 459.67) × 5/9. Get your calculator out

        [GameParameters.CustomParameterUI("#autoLOC_DF_00162", autoPersistance = true, toolTip = "#autoLOC_DF_00163")] //#autoLOC_DF_00162 = Regulated Temperatures Required #autoLOC_DF_00163 = If on, Regulated Temps apply to freeze\nand keep Kerbals Frozen.
        public bool RegTempReqd = false;

        [GameParameters.CustomFloatParameterUI("#autoLOC_DF_00164", autoPersistance = true, minValue = 0, maxValue = 400, toolTip = "#autoLOC_DF_00165")] //#autoLOC_DF_00164 = Min. Temp. for Freezer to Freeze(K) #autoLOC_DF_00165 = The minimum temperature (in Kelvin) for a Freezer\nto be able to Freeze a Kerbal.
        public float RegTempFreeze = 300f;

        [GameParameters.CustomFloatParameterUI("#autoLOC_DF_00166", autoPersistance = true, minValue = 0, maxValue = 800, toolTip = "#autoLOC_DF_00167")] //#autoLOC_DF_00166 = Max. Temp. to keep Kerbals Frozen(K) #autoLOC_DF_00167 = The maximum temperature (in Kelvin) for a Freezer\nto keep Kerbals frozen.
        public float RegTempMonitor = 400f;

        [GameParameters.CustomFloatParameterUI("#autoLOC_DF_00168", autoPersistance = true, minValue = 10, maxValue = 1000, toolTip = "#autoLOC_DF_00169")] //#autoLOC_DF_00168 = Heat generated per kerbal (kW/min) #autoLOC_DF_00169 = Amount of thermal heat (kW) generated\nby equipment for each frozen kerbal per minute.
        public float heatamtMonitoringFrznKerbals = 100f;

        [GameParameters.CustomFloatParameterUI("#autoLOC_DF_00170", autoPersistance = true, minValue = 10, maxValue = 3000, toolTip = "#autoLOC_DF_00171")] //#autoLOC_DF_00170 = Heat generated freezer process(kW) #autoLOC_DF_00171 = Amount of thermal heat (kW) generated\nwith each thaw/freeze process.
        public float heatamtThawFreezeKerbal = 1000f;

        [GameParameters.CustomParameterUI("#autoLOC_DF_00172", autoPersistance = true, toolTip = "#autoLOC_DF_00173")] //#autoLOC_DF_00172 = Show Part Temperatures in Kelvin #autoLOC_DF_00173 = If on Part right click will show temp in Kelvin,\nif Off will show in Celcius.
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
        public override string Title { get { return Localizer.Format("#autoLOC_DF_00174"); } } //#autoLOC_DF_00174 = DeepFreeze Misc.
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override bool HasPresets { get { return false; } }
        public override string Section { get { return "DeepFreeze"; } }
        public override string DisplaySection { get { return Localizer.Format("#autoLOC_DF_00003"); } } //#autoLOC_DF_00003 = DeepFreeze
        public override int SectionOrder { get { return 3; } }
        
        [GameParameters.CustomParameterUI("#autoLOC_DF_00175", autoPersistance = true, toolTip = "#autoLOC_DF_00176")] //#autoLOC_DF_00175 = Freezer Strip Lights On #autoLOC_DF_00176 = Turn off if you do not want the internal\nfreezer strip lights to function.
        public bool StripLightsActive = true;

        [GameParameters.CustomParameterUI("#autoLOC_DF_00203", autoPersistance = true, toolTip = "#autoLOC_DF_00204")] //#autoLOC_DF_00203 = Beep Sounds On. #autoLOC_DF_00204 = If enabled Beep sounds are heard inside freezer parts with frozen kerbals.	
        public bool BeepSoundsActive = true;

        [GameParameters.CustomParameterUI("#autoLOC_DF_00205", autoPersistance = true, toolTip = "#autoLOC_DF_00206")] //#autoLOC_DF_00205 = Other Sounds On. #autoLOC_DF_00206 = If enabled all other DeepFreeze sounds are heard.	
        public bool OtherSoundsActive = true;

        [GameParameters.CustomParameterUI("#autoLOC_DF_00177", autoPersistance = true, toolTip = "#autoLOC_DF_00178")] //#autoLOC_DF_00177 = ToolTips On #autoLOC_DF_00178 = Turn the Tooltips on and off.
        public bool ToolTips = true;

        [GameParameters.CustomParameterUI("#autoLOC_DF_00179", autoPersistance = true, toolTip = "#autoLOC_DF_00180")] //#autoLOC_DF_00179 = Editor Filter #autoLOC_DF_00180 = Turn the DeepFreeze Editor filter Category on and off.
        public bool EditorFilter = true;

        [GameParameters.CustomParameterUI("#autoLOC_DF_00181", toolTip = "#autoLOC_DF_00182")] //#autoLOC_DF_00181 = Use Stock App Launcher Icon #autoLOC_DF_00182 = If on, the Stock Application launcher will be used,\nif off will use Blizzy Toolbar if installed.
        public bool UseAppLToolbar = true;

        [GameParameters.CustomParameterUI("#autoLOC_DF_00183", toolTip = "#autoLOC_DF_00184")] //#autoLOC_DF_00183 = Extra Debug Logging #autoLOC_DF_00184 = Turn this On to capture lots of extra information\ninto the KSP log for reporting a problem.
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
