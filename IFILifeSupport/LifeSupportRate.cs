using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static IFILifeSupport.RegisterToolbar;

namespace IFILifeSupport
{
    public static class LifeSupportRate
    {
        //private static double Rate_Per_Kerbal = 0.000046;

        // private static double Rate_Per_Kerbal = HighLogic.CurrentGame.Parameters.CustomParams<IFILS>().Rate_Per_Kerbal;


        public static double GetRatePerMinute()
        {
            double ratePerMin = HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().RatePerKerbalPerMinute * GetTechRateAdjustment();
            Log.Info("GetRatePerMinute, ratePerMin: " + ratePerMin);

            return ratePerMin;
        }

        private static double GetTechRateAdjustment()
        {
            return 1;
#if false
            double Adjustment = 1.00;
            if (ResearchAndDevelopment.Instance != null)
            {
                Adjustment = 1.25;
                if (ResearchAndDevelopment.GetTechnologyState("advScienceTech") == RDTech.State.Available)
                { Adjustment = 0.80; }
                else if (ResearchAndDevelopment.GetTechnologyState("advExploration") == RDTech.State.Available)
                { Adjustment = 0.90; }
                else if (ResearchAndDevelopment.GetTechnologyState("scienceTech") == RDTech.State.Available)
                { Adjustment = 1.00; }
            }
            Log.Info("GetTechRateAdjustment: " + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().Rate_Per_Kerbal + ", Adjustment: " + Adjustment);
            return Adjustment;
#endif
        }



        /// <summary>
        /// Is there breathable atmosphere where the vessel is
        /// </summary>
        /// <param name="active"></param>
        /// <returns></returns>
        public static bool BreathableAtmosphere(Vessel active)
        {
            if (active.mainBody.ocean && active.altitude < 0.0)
                return false;

            Debug.Log("BreathableAtmosphere, active.mainBody.atmospherePressureCurve.Evaluate((float)active.altitude: " + active.mainBody.atmospherePressureCurve.Evaluate((float)active.altitude));

            if (active.mainBody.name == FlightGlobals.GetHomeBodyName() &&
                active.mainBody.atmospherePressureCurve.Evaluate((float)active.altitude) <= HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().breathableAtmoPressure)
                return true;

            return false;
        }

        /// <summary>
        /// Determines whether there is enough Oxygen avaliable in the air to supplement the LS
        /// </summary>
        /// <param name="active"></param>
        /// <returns></returns>
        public static bool IntakeAirAvailable(Vessel active, out double usageAdjustment)
        {
            usageAdjustment = 1f;
            if (active.mainBody.atmosphereContainsOxygen)
            {
                if (active.mainBody.ocean && active.altitude < 0.0)
                    return false;
                if (active.mainBody.atmospherePressureCurve.Evaluate((float)active.altitude) >= HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().intakeAirAtmoPressure)
                {
                    if (active.mainBody.name == FlightGlobals.GetHomeBodyName())

                        usageAdjustment = active.mainBody.name == FlightGlobals.GetHomeBodyName() ? 
                            HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().breathableHomeworldAtmoAdjustment :
                            HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().breathableAtmoAdjustment;
                    return true;

                }
                //            if (active.mainBody.name == FlightGlobals.GetHomeBodyName() && active.altitude <= 12123)
                //                return true;
            }
            return false;
        }
    }
}
