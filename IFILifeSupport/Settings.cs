using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;



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



        [GameParameters.CustomParameterUI("Basic Life Support")]
        public bool basic = false;
        bool oldBasic = false;


        [GameParameters.CustomParameterUI("Improved Life Support")]
        public bool improved = false;
        bool oldImproved = false;


        [GameParameters.CustomParameterUI("Advanced Life Support")]
        public bool advanced = false;
        bool oldAdvanced = false;


        [GameParameters.CustomParameterUI("Extreme Life Support")]
        public bool extreme = false;
        bool oldExtreme = false;

        public enum LifeSupportLevel { none, basic, improved, advanced, extreme};
        public LifeSupportLevel Level {  get
            {
                if (basic) return LifeSupportLevel.basic;
                if (improved) return LifeSupportLevel.improved;
                if (advanced) return LifeSupportLevel.advanced;
                if (extreme) return LifeSupportLevel.extreme;
                return LifeSupportLevel.none;
            }
        }

        void SetLevel(LifeSupportLevel lsl)
        {
            switch (lsl)
            {
                case LifeSupportLevel.basic:
                    basic = true;
                    improved = false;
                    advanced = false;
                    extreme = false;
                    break;
                case LifeSupportLevel.improved:
                    basic = false;
                    improved = true;
                    advanced = false;
                    extreme = false;
                    break;
                case LifeSupportLevel.advanced:
                    basic = false;
                    improved = false;
                    advanced = true;
                    extreme = false;
                    break;
                case LifeSupportLevel.extreme:
                    basic = false;
                    improved = false;
                    advanced = false;
                    extreme = true;
                    break;
            }
            oldAdvanced = advanced;
            oldImproved = improved;
            oldBasic = basic;
        }

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    SetLevel(LifeSupportLevel.basic);
                    break;

                case GameParameters.Preset.Normal:
                    SetLevel(LifeSupportLevel.improved);

                    break;

                case GameParameters.Preset.Moderate:
                    basic = false;

                    SetLevel(LifeSupportLevel.improved);
                    break;

                case GameParameters.Preset.Hard:

                    SetLevel(LifeSupportLevel.advanced);
                    break;
            }
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            return true;
        }
        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (oldBasic != basic)
                SetLevel(LifeSupportLevel.basic);

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

        [GameParameters.CustomParameterUI("Easy")]
        public bool easy = false;
        bool oldEasy = false;

        [GameParameters.CustomParameterUI("Normal")]
        public bool normal = false;
        bool oldNormal = false;

        [GameParameters.CustomParameterUI("Moderate")]
        public bool moderate = false;
        bool oldModerate = false;

        [GameParameters.CustomParameterUI("Hard")]
        public bool hard = false;
        bool oldHard = false;

        public double Rate_Per_Kerbal
        {
            get
            {
                return lsRatePerDay / 3600 / (GameSettings.KERBIN_TIME ? 6 : 24);
            }
        }

        [GameParameters.CustomFloatParameterUI("LS rate per Kerbal per day", minValue = 0.5f, maxValue = 2f, stepCount = 100, displayFormat = "N2")]
        public double lsRatePerDay = 1.0f;


        [GameParameters.CustomFloatParameterUI("Breathable Atmo Pressure", minValue = 46.1f, maxValue = 69.15f, stepCount = 100, displayFormat = "N2")]
        public double breathableAtmoPressure = BREATHABLE_ATMO_PRESSURE;

        [GameParameters.CustomFloatParameterUI("Min Intake Air Atmo Pressure", minValue = 9.864f, maxValue = 14.796f, stepCount = 100, displayFormat = "N3")]
        public double intakeAirAtmoPressure = INTAKE_AIR_ATMO_PRESSURE;

        [GameParameters.CustomFloatParameterUI("Breathable Homeworld Atmo Adj", minValue = .16f, maxValue = .24f, stepCount = 100, displayFormat = "N2")]
        public double breathableHomeworldAtmoAdjustment = BREATHABLE_HOMEWORLD_ATMO_ADJUSTMENT;

        [GameParameters.CustomFloatParameterUI("Breathable Atmo Adj", minValue = 0.48f, maxValue = 0.72f, stepCount = 100, displayFormat = "N2")]
        public double breathableAtmoAdjustment = BREATHABLE_ATMO_ADJUSTMENT;

        [GameParameters.CustomFloatParameterUI("Low EC Adj", minValue = 0.96f, maxValue = 1.44f, stepCount = 100, displayFormat = "N2")]
        public double lowEcAdjustment = LOW_EC_ADJUSTMENT;

        [GameParameters.CustomParameterUI("Kerbals can die")]
        public bool kerbalsCanDie = true;

        [GameParameters.CustomParameterUI("EVA Kerbals can die")]
        public bool EVAkerbalsCanDie = true;

        [GameParameters.CustomIntParameterUI("Kerbals inactive time before dying (not yet implemented)", minValue = 0, maxValue = 3600)]
        public double inactiveTimeBeforeDeath = 0;



        void SetValues(double mult)
        {
            //Rate_Per_Kerbal = RATE_PER_KERBAL * mult;
            //lsRatePerDay / 3600 / 6 * mult;
            lsRatePerDay = 1 * mult;

            breathableAtmoPressure = BREATHABLE_ATMO_PRESSURE / mult; // For the atmo pressure, use a division so that easier becomes higher
            intakeAirAtmoPressure = INTAKE_AIR_ATMO_PRESSURE / mult;

            breathableHomeworldAtmoAdjustment = BREATHABLE_HOMEWORLD_ATMO_ADJUSTMENT * mult;
            breathableAtmoAdjustment = BREATHABLE_ATMO_ADJUSTMENT * mult;
            lowEcAdjustment = LOW_EC_ADJUSTMENT * mult;
        }

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
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
