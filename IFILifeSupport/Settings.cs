using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using static IFILifeSupport.RegisterToolbar;


namespace IFILifeSupport
{
    // http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
    // search for "Mod integration into Stock Settings

    //   HighLogic.CurrentGame.Parameters.CustomParams<IFILS>()

    public class IFILS1 : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "Level"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "IFI Life Support"; } }
        public override string DisplaySection { get { return "IFI Life Support"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return true; } }

        [GameParameters.CustomParameterUI("Use KSP skin")]
        public bool useKSPskin = true;

        [GameParameters.CustomParameterUI("Active",
            toolTip = "Mod active in this game.")]
        public bool active = true;

        [GameParameters.CustomParameterUI("Classic Life Support",
            toolTip = "Basic life support, no recycling of any kind.")]
#if false
        public bool classic = true;
        bool oldClassic = false;

        public bool improved = false;
        bool oldImproved = false;

        public bool advanced = false;
        bool oldAdvanced = false;

        public bool extreme = false;
        bool oldExtreme = false;

        public bool showInResourcePanel = false;
#else
        public bool classic = true;
        bool oldClassic = false;


        [GameParameters.CustomParameterUI("Improved Life Support",
            toolTip = "Greenhouses are available with a 50% recycle rate")]
        public bool improved = false;
        bool oldImproved = false;


        [GameParameters.CustomParameterUI("Advanced Life Support",
            toolTip = "Microbiome and AlgaeHouse available to recycle sludge ")]
        public bool advanced = false;
        bool oldAdvanced = false;


        [GameParameters.CustomParameterUI("Extreme Life Support",
            toolTip = "BioReactor available to create new LifeSupport")]
        public bool extreme = false;
        bool oldExtreme = false;


        [GameParameters.CustomParameterUI("Display Organic Slurry & Sludge in resource panel",
            toolTip = "If enabled, then the Organic Slurry and Sludge (if applicable) will be shown in the resource panels of parts")]
        public bool showInResourcePanel = false;
#endif

        [GameParameters.CustomIntParameterUI("Update Frequency (secs)", minValue = 15, maxValue = 300)]
        public int refreshInterval = 15;

        public float RefreshInterval { get { return TimeWarp.CurrentRate * refreshInterval; } }

        [GameParameters.CustomIntParameterUI("Display Update Frequency (secs)", minValue = 15, maxValue = 300)]
        public int displayRefreshInterval = 15;

        [GameParameters.CustomParameterUI("Initted",
    toolTip = "Set to false to have the selection window show up at the Space Center")]
        public bool selectionMade = false;



        public enum LifeSupportLevel { none, classic, improved, advanced, extreme };
        public LifeSupportLevel Level
        {
            get
            {
                if (classic) return LifeSupportLevel.classic;
                if (improved) return LifeSupportLevel.improved;
                if (advanced) return LifeSupportLevel.advanced;
                if (extreme) return LifeSupportLevel.extreme;
                return LifeSupportLevel.none;
            }
        }

        void SetLevel(LifeSupportLevel lsl)
        {
            LifeSupportDisplayInfo.ReinitInfoWindowPos();
            switch (lsl)
            {
                case LifeSupportLevel.classic:
                    classic = true;
                    improved = false;
                    advanced = false;
                    extreme = false;
                    break;
                case LifeSupportLevel.improved:
                    classic = false;
                    improved = true;
                    advanced = false;
                    extreme = false;
                    break;
                case LifeSupportLevel.advanced:
                    classic = false;
                    improved = false;
                    advanced = true;
                    extreme = false;
                    break;
                case LifeSupportLevel.extreme:
                    classic = false;
                    improved = false;
                    advanced = false;
                    extreme = true;
                    break;
            }

#if false
            classic = true;
            improved = false;
            advanced = false;
            extreme = false;
#endif
            oldAdvanced = advanced;
            oldImproved = improved;
            oldClassic = classic;
        }

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            Log.Info("IFILS1.SetDifficultyPreset: " + preset);
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    SetLevel(LifeSupportLevel.classic);
                    break;

                case GameParameters.Preset.Normal:
                    SetLevel(LifeSupportLevel.improved);

                    break;

                case GameParameters.Preset.Moderate:
                    classic = false;

                    SetLevel(LifeSupportLevel.improved);
                    break;

                case GameParameters.Preset.Hard:

                    SetLevel(LifeSupportLevel.advanced);
                    break;
            }
        }

        bool oldUseKspSkin = true;
        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (useKSPskin != oldUseKspSkin)
            {
                LifeSupportDisplayInfo.infoWindowPos.height = 100;
                oldUseKspSkin = useKSPskin;
            }
            return true;
        }
        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (oldClassic != classic)
                SetLevel(LifeSupportLevel.classic);

            if (oldImproved != improved)
                SetLevel(LifeSupportLevel.improved);

            if (oldAdvanced != advanced)
                SetLevel(LifeSupportLevel.advanced);

            if (oldExtreme != extreme)
                SetLevel(LifeSupportLevel.extreme);

            return true;
        }

        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }

    public class IFILS2 : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "Options"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "IFI Life Support"; } }
        public override string DisplaySection { get { return "IFI Life Support"; } }
        public override int SectionOrder { get { return 2; } }
        public override bool HasPresets { get { return true; } }

#if false
        [GameParameters.CustomParameterUI("Mod Enabled?",
            toolTip = "Changing this requires restarting the game")]
        public bool enabled = true;
#endif
        //
        // Constants first, then variables.  Constants are the "normal" settings
        // defined here since this is the only place they are used
        //
        //double RATE_PER_KERBAL = 1 / 3600 / 6; // 0.000046;  1 unit per kerbal per day
        const double BREATHABLE_ATMO_PRESSURE = 57.625f; // kerbin, about 3250
        const double INTAKE_AIR_ATMO_PRESSURE = 12.33f; // kerbin. about 12123

        const double BREATHABLE_HOMEWORLD_ATMO_ADJUSTMENT = 0.20;
        const double BREATHABLE_ATMO_ADJUSTMENT = 0.60;
        const double LOW_EC_ADJUSTMENT = 1.2;

        [GameParameters.CustomParameterUI("Easy", toolTip = "Set all values to the standard Easy level")]
        public bool easy = false;
        bool oldEasy = false;

        [GameParameters.CustomParameterUI("Normal",
            toolTip = "Set all values to the standard Normal level")]
        public bool normal = false;
        bool oldNormal = false;

        [GameParameters.CustomParameterUI("Moderate",
            toolTip = "Set all values to the standard Moderate level")]
        public bool moderate = false;
        bool oldModerate = false;

        [GameParameters.CustomParameterUI("Hard",
            toolTip = "Set all values to the standard Hard level")]
        public bool hard = false;
        bool oldHard = false;

        //
        // RatePerKerbalPerMinute returns the amount of LS used in a single minute
        //
        public double RatePerKerbalPerMinute
        {
            get
            {
                return lsRatePerDay / (60 * (GameSettings.KERBIN_TIME ? 6 : 24));
            }
        }

        [GameParameters.CustomIntParameterUI("Auto Warp Cancellation lead time (days)", minValue = 0, maxValue = 3,
            toolTip = "When warping, if LS drops below the warning level (yellow or red), and number of days before it runs out is less than this value")]
        public int warpCancellationLeadTime = 3;


        [GameParameters.CustomFloatParameterUI("LS rate per Kerbal per day", minValue = 0.5f, maxValue = 2f, stepCount = 100, displayFormat = "N2",
            toolTip = "How much LS a kerbal uses a day when in space and with EC available")]
        public double lsRatePerDay = 1.0f;


        [GameParameters.CustomFloatParameterUI("Breathable Atmo Pressure", minValue = 46.1f, maxValue = 69.15f, stepCount = 100, displayFormat = "N2",
            toolTip = "The pressure at which a Kerbal can breath")]
        public double breathableAtmoPressure = BREATHABLE_ATMO_PRESSURE;

        [GameParameters.CustomFloatParameterUI("Min Intake Air Atmo Pressure", minValue = 9.864f, maxValue = 14.796f, stepCount = 100, displayFormat = "N3",
            toolTip = "Minimum air pressure needed for the Circular Intake to work")]
        public double intakeAirAtmoPressure = INTAKE_AIR_ATMO_PRESSURE;

        [GameParameters.CustomFloatParameterUI("Breathable Homeworld Atmo Adj", minValue = .16f, maxValue = .24f, stepCount = 100, displayFormat = "N2",
            toolTip = "Adjustment when on the homeworld (multiplier)")]
        public double breathableHomeworldAtmoAdjustment = BREATHABLE_HOMEWORLD_ATMO_ADJUSTMENT;

        [GameParameters.CustomFloatParameterUI("Breathable Atmo Adj", minValue = 0.48f, maxValue = 0.72f, stepCount = 100, displayFormat = "N2",
            toolTip = "Adjustment when on a planet other than the homeworld with Oxygen (multiplier)")]
        public double breathableAtmoAdjustment = BREATHABLE_ATMO_ADJUSTMENT;

        [GameParameters.CustomFloatParameterUI("Low EC Adj", minValue = 0.96f, maxValue = 1.44f, stepCount = 100, displayFormat = "N2",
            toolTip = "When no EC is available, LS is used at a higher rate, controlled by this value")]
        public double lowEcAdjustment = LOW_EC_ADJUSTMENT;

        [GameParameters.CustomParameterUI("Kerbals can die")]
        public bool kerbalsCanDie = true;

        [GameParameters.CustomParameterUI("EVA Kerbals can die")]
        public bool EVAkerbalsCanDie = true;

        [GameParameters.CustomIntParameterUI("Inactive time before dying (hours)", minValue = 0, maxValue = 60)]
        public int inactiveTimeBeforeDeath = 1;


        [GameParameters.CustomParameterUI("Kerbals can die during timewarp",
            toolTip = "If false, then kerbals will not die during timewarp, the time until death will begin when the timewarp ends")]
        public bool dieDuringTimewarp = true;


        public int InactiveTimeBeforeDeathSecs { get { return inactiveTimeBeforeDeath * 3600; } }
        [GameParameters.CustomParameterUI("Debug mode",
            toolTip = "Writes reports to the log file ")]
        public bool Debug = false;



        void SetValues(double mult)
        {
            Log.Info("IFILS2.SetValues, mult: " + mult);
            lsRatePerDay = 1 * mult;

            breathableAtmoPressure = BREATHABLE_ATMO_PRESSURE / mult; // For the atmo pressure, use a division so that easier becomes higher
            intakeAirAtmoPressure = INTAKE_AIR_ATMO_PRESSURE / mult;

            breathableHomeworldAtmoAdjustment = BREATHABLE_HOMEWORLD_ATMO_ADJUSTMENT * mult;
            breathableAtmoAdjustment = BREATHABLE_ATMO_ADJUSTMENT * mult;
            lowEcAdjustment = LOW_EC_ADJUSTMENT * mult;
        }

        static void SetValuesStatic(double mult)
        {
            Log.Info("IFILS2.SetValuesStatic, mult: " + mult);

            //Rate_Per_Kerbal = RATE_PER_KERBAL * mult;
            //lsRatePerDay / 3600 / 6 * mult;
            HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().lsRatePerDay = 1 * mult;

            HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().breathableAtmoPressure = BREATHABLE_ATMO_PRESSURE / mult; // For the atmo pressure, use a division so that easier becomes higher
            HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().intakeAirAtmoPressure = INTAKE_AIR_ATMO_PRESSURE / mult;

            HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().breathableHomeworldAtmoAdjustment = BREATHABLE_HOMEWORLD_ATMO_ADJUSTMENT * mult;
            HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().breathableAtmoAdjustment = BREATHABLE_ATMO_ADJUSTMENT * mult;
            HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().lowEcAdjustment = LOW_EC_ADJUSTMENT * mult;

        }

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            SetDiffPreset(preset);
        }

        public static void SetDiffPresetStatic(GameParameters.Preset preset)
        {
            Log.Info("IFILS2.SetDifficultyPreset: " + preset);
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    SetValuesStatic(0.8);
                    if (HighLogic.CurrentGame == null)
                        return;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().easy = true;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().normal = false;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().moderate = false;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().hard = false;
                    break;

                case GameParameters.Preset.Normal:
                    SetValuesStatic(1);
                    if (HighLogic.CurrentGame == null)
                        return;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().normal = true;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().easy = false;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().moderate = false;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().hard = false;
                    break;

                case GameParameters.Preset.Moderate:
                    SetValuesStatic(1.1);
                    if (HighLogic.CurrentGame == null)
                        return;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().moderate = true;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().easy = false;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().normal = false;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().hard = false;
                    break;

                case GameParameters.Preset.Hard:
                    SetValuesStatic(1.2);
                    if (HighLogic.CurrentGame == null)
                        return;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().hard = true;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().easy = false;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().normal = false;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().moderate = false;
                    break;
            }
            HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().oldEasy = HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().easy;
            HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().oldNormal = HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().normal;
            HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().oldModerate = HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().moderate;
            HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().oldHard = HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().hard;
        }

        public void SetDiffPreset(GameParameters.Preset preset)
        {
            Log.Info("IFILS2.SetDifficultyPreset: " + preset);
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    SetValues(0.8);
                    easy = true;
                    normal = false;
                    moderate = false;
                    hard = false;
                    break;

                case GameParameters.Preset.Normal:
                    SetValues(1);
                    normal = true;
                    easy = false;
                    moderate = false;
                    hard = false;
                    break;

                case GameParameters.Preset.Moderate:
                    SetValues(1.1);
                    moderate = true;
                    easy = false;
                    normal = false;
                    hard = false;
                    break;

                case GameParameters.Preset.Hard:
                    SetValues(1.2);
                    hard = true;
                    easy = false;
                    normal = false;
                    moderate = false;
                    break;
            }
            oldEasy = easy;
            oldNormal = normal;
            oldModerate = moderate;
            oldHard = hard;
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            return true;
        }
        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (easy != oldEasy)
                SetDifficultyPreset(GameParameters.Preset.Easy);

            if (normal != oldNormal)
                SetDifficultyPreset(GameParameters.Preset.Normal);

            if (moderate != oldModerate)
                SetDifficultyPreset(GameParameters.Preset.Moderate);

            if (hard != oldHard)
                SetDifficultyPreset(GameParameters.Preset.Hard);


            return true;
        }
        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }
}
