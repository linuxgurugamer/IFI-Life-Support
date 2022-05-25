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
#if false
            PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(resourceName);
            double amount = 0;
            foreach (Part mypart in v.parts)
            {
                if (mypart.Resources.Contains(resourceName))
                {
                    amount += GetResourceAmount(mypart, resourceName);
                }
            }
            return amount;
#endif
        }

        public static WaitForSeconds WaitForSecondsLogged(string caller, float seconds)
        {
            Log.Info("WaitForSecondsLogged: From: " + caller + " for: " + seconds);
            return new WaitForSeconds(seconds);
        }

        public static WaitForSecondsRealtime WaitForSecondsRealtimeLogged(string caller, float seconds)
        {
            Log.Info("WaitForSecondsRealtimeLogged: From: " + caller + " for: " + seconds);
            return new WaitForSecondsRealtime(seconds);
        }
    }
}
