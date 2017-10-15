using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;



namespace IFILifeSupport
{
    // http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
    // search for "Mod integration into Stock Settings

    //   HighLogic.CurrentGame.Parameters.CustomParams<IFILS>()

    public class IFILS : GameParameters.CustomParameterNode
    {
        public override string Title { get { return ""; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "IFI Life Support"; } }
        public override string DisplaySection { get { return "IFI Life Support"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return true; } }

#if false
        [GameParameters.CustomParameterUI("Mod Enabled?",
            toolTip = "Changing this requires restarting the game")]
        public bool enabled = true;
#endif
        //
        // Constants first, then variables.  Constants are the "normal" settings
        //
        const double RATE_PER_KERBAL = 0.000046;
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

        [GameParameters.CustomFloatParameterUI("LS rate per tic", minValue = 0.0000368f, maxValue = 0.0000552f, stepCount = 100, displayFormat = "N7",
            toolTip = "A tic is 1/20 of a second")]
        public double Rate_Per_Kerbal = RATE_PER_KERBAL;

        [GameParameters.CustomFloatParameterUI("Breathable Atmo Pressure", minValue = 46.1f, maxValue = 69.15f, stepCount = 100, displayFormat = "N2")]
        public double breathableAtmoPressure = BREATHABLE_ATMO_PRESSURE;

        [GameParameters.CustomFloatParameterUI("Min Intake Air Atmo Pressure", minValue = 9.864f, maxValue = 14.796f, stepCount = 100, displayFormat = "N3")]
        public double intakeAirAtmoPressure = INTAKE_AIR_ATMO_PRESSURE;

        [GameParameters.CustomFloatParameterUI("Breathable Homeworld Atmo Adj", minValue = .16f, maxValue = .24f, stepCount = 100, displayFormat = "N2")]
        public double breathableHomeworldAtmoAdjustment = BREATHABLE_HOMEWORLD_ATMO_ADJUSTMENT;

        [GameParameters.CustomFloatParameterUI("Breathable Homeworld Atmo Adj", minValue = 0.48f, maxValue = 0.72f, stepCount = 100, displayFormat = "N2")]
        public double breathableAtmoAdjustment = BREATHABLE_ATMO_ADJUSTMENT;

        [GameParameters.CustomFloatParameterUI("Low EC Adj", minValue = 0.96f, maxValue = 1.44f, stepCount = 100, displayFormat = "N2")]
        public double lowEcAdjustment = LOW_EC_ADJUSTMENT;


        void SetValues(double mult)
        {
            Rate_Per_Kerbal = RATE_PER_KERBAL * mult;

            breathableAtmoPressure = BREATHABLE_ATMO_PRESSURE / mult; // For the atmo pressure, use a division so that easier becomes higher
            intakeAirAtmoPressure = INTAKE_AIR_ATMO_PRESSURE / mult;

            breathableHomeworldAtmoAdjustment = BREATHABLE_HOMEWORLD_ATMO_ADJUSTMENT * mult;
            breathableAtmoAdjustment = BREATHABLE_ATMO_ADJUSTMENT * mult;
            lowEcAdjustment = LOW_EC_ADJUSTMENT * mult;
        }

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            UnityEngine.Debug.Log("Setting difficulty preset: " + preset);
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
