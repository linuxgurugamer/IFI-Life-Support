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
    internal class IFI_Parts
    {
        internal static SortedList<string, IFI_Generator> allResProcessors = new SortedList<string, IFI_Generator>();
        internal static SortedList<string, IFI_Generator> usefulResProcessors = new SortedList<string, IFI_Generator>();
        string[] resources = new string[] { Constants.SLUDGE, Constants.LIFESUPPORT, Constants.SLURRY };

        internal static void GetPartsList()
        {
            Log.Info("GetPartsList");
            List<AvailablePart> loadedParts = new List<AvailablePart>();
            loadedParts.AddRange(PartLoader.LoadedPartsList); // make a copy we can manipulate

            // these two parts are internal and just serve to mess up our lists and stuff
            AvailablePart kerbalEVA = null;
            AvailablePart flag = null;
            for (int i = 0; i < loadedParts.Count; i++)
            {
                if (loadedParts[i].name == "kerbalEVA")
                    kerbalEVA = loadedParts[i];
                else if (loadedParts[i].name == "flag")
                    flag = loadedParts[i];

                for (int m = 0; m < loadedParts[i].partPrefab.Modules.Count; m++)
                {
                    if (loadedParts[i].partPrefab.Modules[m].moduleName == "ModuleIFILifeSupport")
                    {
                        IFI_Generator ig = new IFI_Generator(loadedParts[i].name, loadedParts[i].title, loadedParts[i].partPrefab, loadedParts[i].partPrefab.Modules[m]);
                        string s = ig.Key();
                        allResProcessors.Add(ig.Key(), ig);

                        foreach (var r in ig.resList)
                        {

                            if (r.Value.resourceName == Constants.SLURRY && r.Value.input)
                                ig.consumesSlurry = true;
                            if (r.Value.resourceName == Constants.SLUDGE && r.Value.input)
                                ig.consumesSludge = true;
                            if (r.Value.resourceName == Constants.FILTERED_O2 && !r.Value.input)
                                ig.producesFilteredO2 = true;
                            if (r.Value.resourceName == Constants.FILTERED_O2 && r.Value.input)
                                ig.consumesFilteredO2 = true;
                            if (r.Value.resourceName == Constants.LIQUID_O2 && r.Value.input)
                                ig.consumesLiquidO2 = true;
                        }

                        foreach (var r in ig.resList)
                        {
                            //if (resources.Contains(r.Value.resname))
                            {
                                usefulResProcessors.Add(ig.Key(), ig);
                                break;
                            }
                        }
                    }
                }
            }

            // still need to prevent errors with null refs when looking up these parts though
            if (kerbalEVA != null)
            {
                loadedParts.Remove(kerbalEVA);

            }
            if (flag != null)
            {
                loadedParts.Remove(flag);
            }

#if false
            Log.Info("GetPartsList, List of IFI parts & resource usage");
            foreach (var a in allResProcessors)
            {
                Log.Info("part: " + a.Value.partName + ", consumesSlurry: " + a.Value.consumesSlurry + ", consumesSludge: " + a.Value.consumesSludge +
                    ", consumesFilteredO2: " + a.Value.consumesFilteredO2 + ", consumesLiquidO2: " + a.Value.consumesLiquidO2);
                foreach (var b in a.Value.resList)
                    Log.Info("Key: " + a.Key + ", part: " + a.Value.partName + ", resource; " + b.Value.resourceName + ", ratio: " + b.Value.RatioPerDay + ", input: " + b.Value.input);
            }
#endif
        }
    }
}
