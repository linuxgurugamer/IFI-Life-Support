using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using SpaceTuxUtility;
using static IFILifeSupport.RegisterToolbar;

namespace IFILifeSupport
{
    public class IFI_ResourceRatio
    {
        // Following are loaded and saved 
        public string ResourceName;
        public double RatioPerSec;
        public double RatioPerDay;
        public bool DumpExcess;

        // Following for internal use only
        internal bool input;
        public int resourceId;
        public float uvMultipler;

        internal string resourceName {  get { return ResourceName; } }

        public IFI_ResourceRatio(ConfigNode node)
        {
            Load(node);
        }
        public IFI_ResourceRatio()
        { }

        public IFI_ResourceRatio(string name, double ratioPerDay, double ratioPerSec, bool input)
        {
            ResourceName = name;
            RatioPerDay = ratioPerDay;
            RatioPerSec = ratioPerSec;
            this.input = input;
            resourceId = PartResourceLibrary.Instance.GetDefinition(ResourceName).id;
        }

        public void Load(ConfigNode node)
        {
            ResourceName = node.SafeLoad("ResourceName", "");
            RatioPerSec = node.SafeLoad("RatioPerSec", (double)0f);
            RatioPerDay = node.SafeLoad("RatioPerDay", (double)0f);
            DumpExcess = node.SafeLoad("DumpExcess", true);

            if (RatioPerSec == 0)
                RatioPerSec = RatioPerDay / IFI_LifeSupportTrackingDisplay.SECSPERDAY;
            if (RatioPerDay == 0)
                RatioPerDay = RatioPerSec * IFI_LifeSupportTrackingDisplay.SECSPERDAY;

            resourceId = PartResourceLibrary.Instance.GetDefinition(ResourceName).id;

        }
        public void Save(ConfigNode node)
        {
            node.AddValue("ResourceName", ResourceName);
            node.AddValue("RatioPerSec", RatioPerSec);
            node.AddValue("RatioPerDay", RatioPerDay);
            node.AddValue("DumpExcess", DumpExcess);
        }
    }
}
