using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static IFILifeSupport.RegisterToolbar;


namespace IFILifeSupport
{
    public class IFILifeSupportEVA : PartModule  // Life Support Tracking for RT Click Menu EVA
    {

        public bool initialized = false;
        //private static double Rate_Per_Kerbal_Adj = LifeSupportRate.GetRate();

        // Right Click Info display for Part
        [KSPField(guiActive = true, guiName = "Life Support Pack Status", isPersistant = false)]
        public string lifeSupportStatus;

        [KSPField(guiActive = true, guiName = "Kibbles & Bits", guiUnits = " HOURS ", guiFormat = "F2", isPersistant = false)]
        public float displayRate;


        public override void OnUpdate()
        {
            base.OnUpdate();
            if (RegisterToolbar.GamePaused /*("EVAPartModule") */ )
                return;
            Vessel active = this.part.vessel;
            if (active.isEVA == true)
            {
                IFIDebug.IFIMess("IFILifeSupportEVA.OnUpdate, active.mainBody.name: " + active.mainBody.name +
                    ",  FlightGlobals.GetHomeBodyName(): " + FlightGlobals.GetHomeBodyName() + ",   active.altitude: " + active.altitude);
                if (LifeSupportRate.BreathableAtmosphere(active))
                {
                    lifeSupportStatus = "Visor";
                }
                else
                {
                    lifeSupportStatus = "Active";
                }
                double ResourceAval = IFIGetAllResources(Constants.LIFESUPPORT);
                displayRate = (float)((ResourceAval / LifeSupportRate.GetRatePerMinute()));
                if (displayRate >= 0.5 && displayRate <= 1)
                {
                    lifeSupportStatus = "CAUTION ";
                }
                else if (displayRate > 0 && displayRate < 0.5)
                {
                    lifeSupportStatus = "Warning!";
                }
                else if (displayRate <= 0)
                {
                    lifeSupportStatus = "Danger!";
                }

            }

        }



        private double IFIGetAllResources(string IFIResource)
        {
            if (IFIResource == null)
            {
                Log.Info("IFIGetAllResources, IFIResource is null");
                return 0;
            }
            if (this.part == null)
            {
                Log.Info("IFIGetAllResources, part is null");
                return 0;
            }
            if (this.part.Resources == null)
            {
                Log.Info("this.part.Resources is null");
                return 0;
            }
            Log.Info("IFIGetAllResources, resource: " + IFIResource);
            double IFIResourceAmt = 0.0;
            for (int i = 0; i < part.Resources.Count; i++)
            //foreach (PartResource pr in this.part.Resources)
            {
                PartResource pr = this.part.Resources[i];
                if (pr.resourceName.Equals(IFIResource))
                {
                    if (pr.flowState)
                    {
                        IFIResourceAmt += pr.amount;
                    }
                }
            }
            return IFIResourceAmt;
        }
    }
}