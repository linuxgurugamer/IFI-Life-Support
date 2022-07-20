using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static IFILifeSupport.RegisterToolbar;


namespace IFILifeSupport
{
    // The methods in the following class are copied from the following post:
    // https://forum.kerbalspaceprogram.com/index.php?/topic/124247-how-to-get-quantity-of-a-resource/&do=findComment&comment=2255442
    //
    public static class Util
    {
        //gets the id of the named resource
        public static int GetResourceID﻿(string resourceName)
        {
            PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(resourceName);
            return resource.id;
        }

        //gets the amount of resource in one part.
        public static double GetResourceAmount(this Part part, string resourceName)
        {
            PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(resourceName);
            return part.Resources.Get(resource.id).amount;
        }

        //gets how much empty resource space the part has
        public static double GetResourceSpace(this Part part, string resourceName)
        {
            PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(resourceName);
            double amt = part.Resources.Get(resource.id).amount;
            double max = part.Resources.Get(resource.id).maxAmount;
            return max - amt;
        }

        //returns the first part found with a named resource.  I use this to locate a part that I'm going to use All_Vessel flow mode on.
        public static Part GetResourcePart(Vessel v, string resourceName)
        {
            foreach (Part mypart in v.parts)
            {
                if (mypart.Resources.Contains(resourceName))
                {
                    return mypart;
                }
            }
            return null;
        }

        //gets the total amount of a resource found on the entire vessel
        public static double GetResourceTotal(Vessel v, string resourceName)
        {
            double amount, maxAmount;
            v.rootPart.GetConnectedResourceTotals(GetResourceID(resourceName), ResourceFlowMode.ALL_VESSEL,  out amount, out maxAmount);
            return amount;
        }

        public static WaitForSeconds WaitForSecondsLogged(string caller, float seconds)
        {
            Log.Info("WaitForSecondsLogged: From: " + caller + " for: " + seconds);
            return new WaitForSeconds(seconds);
        }

        public static WaitForSecondsRealtime WaitForSecondsRealtimeLogged(string caller, float seconds)
        {
            if (seconds > 1)
                Log.Info("WaitForSecondsRealtimeLogged: From: " + caller + " for: " + seconds);
            return new WaitForSecondsRealtime(seconds);
        }


        internal static string lblGreenColor = "00ff00";
        internal static string lblDrkGreenColor = "ff9d00";
        internal static string lblBlueColor = "3DB1FF";
        internal static string lblYellowColor = "FFD966";
        internal static string lblRedColor = "f90000";

        public static string Colorized(string color, string txt)
        {
            return String.Format("<color=#{0}>{1}</color>", color, txt);
        }
        public static void GetColorized(int i, ref string txt)
        {
            switch (i)
            {
                case IFI_LifeSupportTrackingDisplay.SLURRYAVAIL:
                case IFI_LifeSupportTrackingDisplay.SLURRYCONVRATE:
                case IFI_LifeSupportTrackingDisplay.SLURRY_DAYS_TO_PROCESS:
                    txt = Colorized(lblGreenColor, txt);
                    break;
                case IFI_LifeSupportTrackingDisplay.SLUDGEAVAIL:
                case IFI_LifeSupportTrackingDisplay.SLUDGECONVRATE:
                case IFI_LifeSupportTrackingDisplay.SLUDGE_DAYS_TO_PROCESS:
                case IFI_LifeSupportTrackingDisplay.SLUDGE_OUTPUT_RATE:
                    txt = Colorized(lblDrkGreenColor, txt);
                    break;
                case IFI_LifeSupportTrackingDisplay.LIFESUPPORT_OUTPUT_RATE:
                    txt = Colorized(lblBlueColor, txt);
                    break;
            }
        }

    }
}
