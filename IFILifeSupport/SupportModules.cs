using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static IFILifeSupport.RegisterToolbar;



namespace IFILifeSupport
{

    public class IFI_Resources : PartModule
    {
        public const int BASE_SECS_PER_DAY = 21600;
        public const int BASE_MULTIPLIER = 1; // 10
        //public const int MULTIPLER = BASE_SECS_PER_DAY * BASE_MULTIPLIER;


        internal string info = "";
#if false
        public override string GetInfo()
        {
            string info2 = info;
            foreach (PartResource partResource in part.Resources)
            {
                if (partResource.resourceName == Constants.LIFESUPPORT)
                    info2 += "\nLife Support: " + partResource.amount  + " / " + partResource.maxAmount;
                if (partResource.resourceName == Constants.SLURRY)
                    info2 += "\nSlurry: " + partResource.amount  + " / " + partResource.maxAmount ;
                if (partResource.resourceName == Constants.SLURRY)
                    info2 += "\nSludge: " + partResource.amount  + " / " + partResource.maxAmount ;
            }
            return info2;
        }
#endif
        bool initted = false;
        void LateUpdate()
        {
            if (!initted)
                Start();
        }

        public void Start()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorShipModified.Add(ShipModified);
            }
            else
            {
                GameEvents.onPartResourceFlowModeChange.Add(onPartResourceFlowModeChange);
            }

            ShipModified(new ShipConstruct("initship", "descr", this.part));
            initted = true;
        }

        void OnDestroy()
        {
            if (Log != null)
            Log.Info("SupportModules.OnDestroy");
            if (initted && HighLogic.LoadedSceneIsEditor)
            {
                Log.Info("IFI_Resources.OnDestory");
                GameEvents.onEditorShipModified.Remove(ShipModified);
                initted = false;
            }
            else
            {
                GameEvents.onPartResourceFlowModeChange.Remove(onPartResourceFlowModeChange);
            }
        }


        void onPartResourceFlowModeChange(GameEvents.HostedFromToAction<PartResource, PartResource.FlowMode> data)
        {
            Log.Info("onPartResourceFlowModeChange");
            PartResource host = data.host;
            Part part = host.part;

            Log.Info("host.part: " + part.partInfo.description);
            Log.Info("data.from.displayDescription: " + data.from.displayDescription());
            Log.Info("data.to.displayDescription." + data.to.displayDescription());

        }

        internal static string[] DISPLAY_RESOURCES = new string[] { Constants.LIFESUPPORT, Constants.SLURRY, Constants.SLUDGE };
        internal static string[] RESOURCES = new string[] { Constants.LIFESUPPORT, Constants.SLURRY, Constants.SLURRY };

        static public void UpdateDisplayValues(Vessel v)
        {
            for (int i = v.Parts.Count - 1; i >= 0; i--)
            {
                UpdatePartInfo( v.Parts[i]);
            }
        }

        static internal void UpdatePartInfo(Part p)
        {
            for (int i = RESOURCES.Length - 1; i >= 0; i--)
            {
                if (p.Resources[RESOURCES[i]] != null && p.Resources[DISPLAY_RESOURCES[i]] != null)
                {
                    //Log.Info("UpdatePartInfo, p.Resources[RESOURCES[i]].isVisible: " + p.Resources[RESOURCES[i]].isVisible + ",  p.Resources[DISPLAY_RESOURCES[i]].isVisible: " + p.Resources[DISPLAY_RESOURCES[i]].isVisible);

                    //Log.Info("resLS.amount: " + p.Resources[RESOURCES[i]].amount + ",  resLSdisplay.amount: " + p.Resources[DISPLAY_RESOURCES[i]].amount);
                    if (HighLogic.LoadedSceneIsEditor)
                    {
                        //Log.Info("Updating resource: " + RESOURCES[i]);
                        p.Resources[RESOURCES[i]].amount = p.Resources[DISPLAY_RESOURCES[i]].amount ;
                        p.Resources[RESOURCES[i]].maxAmount = p.Resources[DISPLAY_RESOURCES[i]].maxAmount ;
                        //Log.Info("setting " + RESOURCES[i] + ": amount: " + p.Resources[RESOURCES[i]].amount + ", maxAmount: " + p.Resources[RESOURCES[i]].maxAmount);
                    }
                    else
                    {
                        //Log.Info("Updating resource: " + DISPLAY_RESOURCES[i]);
                        p.Resources[DISPLAY_RESOURCES[i]].amount = p.Resources[RESOURCES[i]].amount ;
                        p.Resources[DISPLAY_RESOURCES[i]].maxAmount = p.Resources[RESOURCES[i]].maxAmount ;
                        //Log.Info("Transfer  Part: " + p.partInfo.title + ", setting " + RESOURCES[i] + ".amount: " + p.Resources[RESOURCES[i]].amount + ", maxAmount: " + p.Resources[RESOURCES[i]].maxAmount);
                        //Log.Info("Transfer  Part: " + p.partInfo.title + ", setting " + DISPLAY_RESOURCES[i] + ".amount: " + p.Resources[DISPLAY_RESOURCES[i]].amount + ", maxAmount: " + p.Resources[DISPLAY_RESOURCES[i]].maxAmount);
                    }
                }
            }
        }

        void ShipModified(ShipConstruct sc)
        {
            Log.Info("IFI_Resources.ShipModified, sc: " + sc.shipName);
            UpdatePartInfo(this.part);            
        }
    }

    public class IFI_Basic : IFI_Resources
    {
        new void Start()
        {
            Log.Info("IFI_Basic");
            info = "IFI_Basic";
        }
    }
    public class IFI_Improved : IFI_Resources
    {
        new void Start()
        {
            Log.Info("IFI_Improved");
            info = "IFI_Improved";
        }
    }
    public class IFI_Advanced : IFI_Resources
    {
        new void Start()
        {
            info = "IFI_Advanced";
            Log.Info("IFI_Advanced");
        }
    }
    public class IFI_Extreme : IFI_Resources
    {
        new void Start()
        {
            info = "IFI_Extreme";
            Log.Info("IFI_Extreme");
        }
    }
}
