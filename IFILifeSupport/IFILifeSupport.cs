using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static IFILifeSupport.RegisterToolbar;


namespace IFILifeSupport
{
    public class IFILifeSupport : PartModule  // Life Support Tracking on Pods
    {

        public bool initialized = false;
        double lastUpdateTime;

        
        //public static int HoursPerDay { get { return GameSettings.KERBIN_TIME ? 6 : 24; } } // Make sure LS remaining Display conforms to Kerbin time setting.
       // public static int HoursPerDay { get { return (int)IFI_LifeSupportTrackingDisplay.HOURSPERDAY; } } // Make sure LS remaining Display conforms to Kerbin time setting.

        // Right Click Info display for Part
        [KSPField(guiActive = true, guiName = "Life Support Status", isPersistant = false)]
        public string lifeSupportStatus;

        [KSPField(guiActive = true, guiName = "Kibbles & Bits", guiUnits = " Days ", guiFormat = "F2", isPersistant = false)]
        public float displayRate;

        [KSPField(guiActive = false, isPersistant = true)]
        public bool RescueFlag;

        //private int IFITIM = 0;

#if !DEBUG
        // Debug Button for right click info - TO BE removed after testing.
        [KSPField(guiActive = true, guiName = "Debug Logging", isPersistant = false)]
        public string DebugStatus = "Disabled";

        [KSPEvent(name = "ToggleDebug", active = true, guiActive = true, guiActiveUnfocused = true, guiName = "LS Debug Info")]

        public void ToggleDebug()
        {
            if (IFIDebug.IsON)
            {
                IFIDebug.Toggle();
                DebugStatus = "Disabled";
            }
            else
            {
                IFIDebug.Toggle();
                DebugStatus = "Enabled";
            }
        }
#endif
#if false
        const int SECS_PER_DAY = 21600;
        public override string GetInfo()
        {
            string info = "Interstellar Flight Inc. Life Support Systems MK XV Installed";
            foreach (PartResource partResource in part.Resources)
            {
                if (partResource.resourceName == Constants.LIFESUPPORT)
                   info += "\nLife Support: " + partResource.amount / SECS_PER_DAY + " / " + partResource.maxAmount / SECS_PER_DAY;
                if (partResource.resourceName == Constants.SLURRY)
                    info += "\nSlurry: " + partResource.amount / SECS_PER_DAY + " / " + partResource.maxAmount / SECS_PER_DAY;
                if (partResource.resourceName == Constants.SLURRY)
                    info += "\nSludge: " + partResource.amount / SECS_PER_DAY + " / " + partResource.maxAmount / SECS_PER_DAY;
            }
            return info;
        }
#endif

        public override string GetInfo()
        {
            return "Interstellar Flight Inc. Life Support Systems MK XV Installed";
        }

        bool refresh = false;
        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!RegisterToolbar.GamePaused /*("IFILifeSupport.OnUpdate")*/ && !HighLogic.LoadedSceneIsEditor &&
                (!refresh || Planetarium.GetUniversalTime() - lastUpdateTime >= HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().RefreshInterval  || TimeWarp.CurrentRate > 800))
            {
                Life_Support_Update();
                lastUpdateTime = Planetarium.GetUniversalTime();
                refresh = true;
            }
        }

        void Life_Support_Update()
        { 
#if !DEBUG
            if (IFIDebug.IsON) { DebugStatus = "Active"; } else { DebugStatus = "Inactive"; }
#endif

            int crewCount = 0;
            crewCount = this.part.protoModuleCrew.Count;
            if (crewCount > 0)
            {
                this.Fields[1].guiActive = true;
                Vessel active = this.part.vessel;
                double LS_RR = LifeSupportRate.GetRatePerMinute();

                IFIDebug.IFIMess("IFILifeSupport.OnUpdate, active.mainBody.name: " + active.mainBody.name +
                    ",  FlightGlobals.GetHomeBodyName(): " + FlightGlobals.GetHomeBodyName() + ",   active.altitude: " + active.altitude);

                if (LifeSupportRate.IntakeAirAvailable(active, out double usageAdjustment))
                {
                    lifeSupportStatus = "Air Intake";
                    LS_RR *= usageAdjustment;
                }
                else
                {
                    lifeSupportStatus = "Active";
                }

                double ResourceAval = IFIGetAllResources(Constants.LIFESUPPORT);

                if (IFIGetAllResources("ElectricCharge") < 0.1)
                {
                    LS_RR *= HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().lowEcAdjustment; // 1.2;
                }
                displayRate = (float)(ResourceAval / (LS_RR * IFIGetAllKerbals()) / IFI_LifeSupportTrackingDisplay.HOURSPERDAY / 60); 

                if (displayRate > 1 && displayRate <= 3)
                {
                    lifeSupportStatus = "CAUTION ";
                }
                if (displayRate > 0 && displayRate < 1)
                {
                    lifeSupportStatus = "Warning!";
                }
                else if (displayRate <= 0)
                {
                    lifeSupportStatus = "Danger!";
                }


            }  //end of if crew > 0
            else if (crewCount == 0)
            {
                lifeSupportStatus = "Pod Standby";
                this.Fields[1].guiActive = false;
            }
        }

        private double IFIGetAllResources(string IFIResource)
        {
            double IFIResourceAmt = 0.0;
            int id = PartResourceLibrary.Instance.GetDefinition(IFIResource).id;
            this.part.GetConnectedResourceTotals(id, out IFIResourceAmt, out double maxAvail, true);

            return IFIResourceAmt;
        }

        private int IFIGetAllKerbals() // Find all Kerbals Hiding on Vessel. Show Life Support Remaining Tag is accurate in each pod on vessel
        {
            int KerbalCount = 0;
            Vessel active = this.part.vessel;
            try
            {
                for (int idx = 0; idx < active.parts.Count; idx++)
                //foreach (Part p in active.parts)
                {
                    Part p = active.parts[idx];
                    int IFIcrew = p.protoModuleCrew.Count;
                    if (IFIcrew > 0) KerbalCount += IFIcrew;
                }
            }
            catch (Exception ex) { IFIDebug.IFIMess("Vessel IFI Exception ++Finding Kerbals++MainLSMod " + ex.Message); }
            return KerbalCount;
        }

    }
}
