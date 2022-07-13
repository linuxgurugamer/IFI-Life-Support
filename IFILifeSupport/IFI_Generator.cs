using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

using UnityEngine.SceneManagement;
using static IFILifeSupport.RegisterToolbar;

namespace IFILifeSupport
{

    internal class IFI_Generator
    {
        internal string partName;
        internal string partTitle;
        internal SortedList<string, IFI_ResourceRatio> resList;
        internal int numAvailable = 0;
        internal IFILS1.LifeSupportLevel level;

        internal double maxLS;

        internal bool consumesSlurry = false;
        internal bool consumesSludge = false;
        internal bool producesFilteredO2 = false;
        internal bool consumesFilteredO2 = false;
        internal bool consumesLiquidO2 = false;
      

        internal string Key()
        {
            string str = ((int)level).ToString() + ":" + maxLS.ToString("F1") + partName;
            return str;
        }

        internal IFI_Generator(string partName, string partTitle, Part part, PartModule partModule)
        {
            resList = new SortedList<string, IFI_ResourceRatio>();

            this.partName = partName;
            this.partTitle = partTitle;
            maxLS = 0f;

            level = IFILS1.LifeSupportLevel.classic;
            for (int m = 0; m < part.Modules.Count; m++)
            {
                string s = part.Modules[m].moduleName;
                if (s == "IFI_Improved")
                    level = IFILS1.LifeSupportLevel.improved;
                if (s == "IFI_Advanced")
                    level = IFILS1.LifeSupportLevel.advanced;
                if (s == "IFI_Extreme")
                    level = IFILS1.LifeSupportLevel.extreme;
            }
            partTitle += "(" + level.ToString() + ")";

            ModuleIFILifeSupport m1 = (ModuleIFILifeSupport)partModule;

            //
            // Found that sometimes the inputList was coming in null, this fixes that
            //
            //if (m1.inputList == null)
            {
                m1.OnLoad(part.partInfo.partConfig);
            }

            for (int i = 0; i < m1.inputList.Count; i++)
            {
                IFI_ResourceRatio inp = m1.inputList[i];
                resList.Add("1" + inp.ResourceName, new IFI_ResourceRatio(inp.ResourceName, inp.RatioPerDay, inp.RatioPerDay / IFI_LifeSupportTrackingDisplay.SECSPERDAY, true));

            }

            for (int i = 0; i < m1.outputList.Count; i++)
            {
                IFI_ResourceRatio output = m1.outputList[i];
                resList.Add("0" + output.ResourceName, new IFI_ResourceRatio(output.ResourceName, output.RatioPerDay, output.RatioPerDay / IFI_LifeSupportTrackingDisplay.SECSPERDAY, false));
                if (output.ResourceName == "LifeSupport")
                    maxLS = output.RatioPerSec; 
            }
        }
    }

}
