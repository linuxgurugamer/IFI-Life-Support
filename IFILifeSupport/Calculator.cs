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
    partial class Editor
    {
        internal int numKerbals = 1;
        internal float amtKB = 0;
        internal int daysLS = 0;
        double producedLS = 0;
        double createdLS = 0, inputSlurry = 0, inputSludge = 0, outputSludge = 0, actualOutputSludge = 0, outputFilteredO2 = 0, outputLiquidO2 = 0;
        double dailySlurry;


        const float CALCBUTTONWIDTH = 50;
        const float LABEL_WIDTH = 180;
        const float NUM_ENTRY_WIDTH = 75;

        Vector2 calcScrollPos;

        float maxKB = 1000;
        float maxDays = 100;

        bool resizingWindow = false;

        internal void Calculator(int winId)
        {
            string str;
            if (GUI.Button(new Rect(Settings.winPos[(int)Settings.ActiveWin.CalculatorWin].width - 24, 3f, 23, 15f), new GUIContent("X")))
            {
                LifeSupportDisplayInfo.displayCalculator = !LifeSupportDisplayInfo.displayCalculator;
            }

            using (new GUILayout.HorizontalScope())
            {

                using (new GUILayout.VerticalScope(GUILayout.Width(600)))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Number of Kerbals:", GUILayout.Width(LABEL_WIDTH));
                        str = numKerbals.ToString();
                        str = GUILayout.TextField(str, GUILayout.Width(NUM_ENTRY_WIDTH));
                        numKerbals = int.Parse(str);
                        if (GUILayout.Button("-", GUILayout.Width(26)))
                            numKerbals--;
                        if (GUILayout.Button("+", GUILayout.Width(26)))
                            numKerbals++;
                        numKerbals = Math.Max(1, numKerbals);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Calc", GUILayout.Width(CALCBUTTONWIDTH)))
                        {
                            if (daysLS <= 0)
                                numKerbals = 0;
                            else
                                numKerbals = (int)(amtKB / daysLS);
                        }
                        GUILayout.Space(15);
                    }
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Amount of Kibbles & Bits:", GUILayout.Width(LABEL_WIDTH));

                        str = amtKB.ToString();
                        str = GUILayout.TextField(str, GUILayout.Width(NUM_ENTRY_WIDTH));
                        amtKB = int.Parse(str);
                        amtKB = Math.Max(1, amtKB);

                        amtKB = (int)GUILayout.HorizontalSlider(amtKB, 1, maxKB, GUILayout.Width(125));
                        GUILayout.Label(maxKB.ToString("F0"), GUILayout.Width(60));
                        if (GUILayout.Button("-", GUILayout.Width(26)))
                            maxKB = Math.Max(10, maxKB / 10);
                        if (GUILayout.Button("+", GUILayout.Width(26)))
                            maxKB *= 10;

                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Calc", GUILayout.Width(CALCBUTTONWIDTH)))
                            amtKB = numKerbals * daysLS;
                        GUILayout.Space(15);
                    }
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Days needed:", GUILayout.Width(LABEL_WIDTH));
                        str = daysLS.ToString();
                        str = GUILayout.TextField(str, GUILayout.Width(NUM_ENTRY_WIDTH));
                        daysLS = int.Parse(str);
                        daysLS = Math.Max(1, daysLS);
                        daysLS = (int)GUILayout.HorizontalSlider(daysLS, 1, maxDays, GUILayout.Width(125));
                        GUILayout.Label(maxDays.ToString("F0"), GUILayout.Width(60));
                        if (GUILayout.Button("-", GUILayout.Width(26)))
                            maxDays = Math.Max(10, maxDays / 10);
                        if (GUILayout.Button("+", GUILayout.Width(26)))
                            maxDays *= 10;

                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Calc", GUILayout.Width(CALCBUTTONWIDTH)))
                        {
                            if (numKerbals <= 0)
                                daysLS = 0;
                            else
                                daysLS = (int)(amtKB / numKerbals);
                        }
                        GUILayout.Space(15);
                    }
                    // First, get amount of organic slurry output (should be equial to number of kerbals)
                    // Second, figure out the level 1 output using the organic slurry.  also figure the other outputs which will be used by the next level

                    // 

                    dailySlurry = 0f;

                    createdLS = inputSlurry = inputSludge = outputSludge = 0;
                    dailySlurry = numKerbals;

                    // Get totals for all specified processors

                    Log.Info("usefulResProcessors.Count: " + IFI_Parts.usefulResProcessors.Count);
                    foreach (var p in IFI_Parts.usefulResProcessors)
                    {
                        var processor = p.Value;
                        if (processor.consumesSlurry)
                        {
                            foreach (var r in processor.resList)
                            {
                                if (r.Value.resourceName == Constants.LIFESUPPORT && !r.Value.input)
                                    createdLS += r.Value.RatioPerDay * processor.numAvailable;
                                if (r.Value.resourceName == Constants.SLUDGE && !r.Value.input)
                                    outputSludge += r.Value.RatioPerDay * processor.numAvailable;
                                if (r.Value.resourceName == Constants.SLURRY && r.Value.input)
                                    inputSlurry += r.Value.RatioPerDay * processor.numAvailable;
                            }
                        }
                    }

                    // Calculate percentage of available input slurry
                    Log.Info("dailySlurry: " + dailySlurry + ", inputSlurry: " + inputSlurry);
                    double slurryRatio;
                    if (inputSlurry > 0)
                    {
                        slurryRatio = Math.Min(1f, dailySlurry / inputSlurry);
                        //slurryRatio *= createdLS / (createdLS + outputSludge);
                    }
                    else
                        slurryRatio = 0;

                    actualOutputSludge = outputSludge * Math.Min(1f, dailySlurry / inputSlurry);


                    createdLS *= slurryRatio;

                    double createdLS2 = 0f;

                    // Now get a total of all the modules that have an input of Sludge
                    foreach (var p in IFI_Parts.usefulResProcessors)
                    {
                        var processor = p.Value;

                        if (processor.consumesSludge && !processor.consumesLiquidO2)
                        {
                            foreach (var r in processor.resList)
                            {
                                if (r.Value.resourceName == Constants.LIFESUPPORT && !r.Value.input)
                                    createdLS2 += r.Value.RatioPerDay * processor.numAvailable;

                                if (r.Value.resourceName == Constants.SLUDGE && r.Value.input)
                                    inputSludge += r.Value.RatioPerDay * processor.numAvailable;
                            }
                        }
                    }

                    Log.Info("outputSludge: " + outputSludge + ", inputSludge: " + inputSludge + ", createdLS2: " + createdLS2);

                    double sludgeRatio = 0;
                    if (inputSludge > 0)
                        sludgeRatio = outputSludge / inputSludge;
                    outputSludge = Math.Max(0, outputSludge - inputSludge); // used further down
                    inputSludge *= slurryRatio * sludgeRatio;

                    createdLS2 *= slurryRatio * sludgeRatio;

                    Log.Info("slurryRatio: " + slurryRatio + ", sludgeRatio: " + sludgeRatio + ", inputSludge: " + inputSludge);
                    Log.Info("createdLS: " + createdLS + ", createdLS2: " + createdLS2);
                    createdLS += createdLS2;


                    double inputLqdO2 = 0, outputLqdO2 = 0, inputFilteredO2 = 0, createdLS3 = 0, inputSludge2 = 0;
                    outputFilteredO2 = 0;

                    foreach (var p in IFI_Parts.usefulResProcessors)
                    {
                        var processor = p.Value;

                        if (processor.producesFilteredO2)
                        {
                            foreach (var r in processor.resList)
                            {
                                if (r.Value.resourceName == Constants.FILTERED_O2 && !r.Value.input)
                                    outputFilteredO2 += r.Value.RatioPerDay * processor.numAvailable;
                            }
                        }

                        if (processor.consumesFilteredO2)
                        {
                            foreach (var r in processor.resList)
                            {
                                if (r.Value.resourceName == Constants.FILTERED_O2 && r.Value.input)
                                    inputFilteredO2 += r.Value.RatioPerDay * processor.numAvailable;

                                if (r.Value.resourceName == Constants.LIQUID_O2 && !r.Value.input)
                                    outputLqdO2 += r.Value.RatioPerDay * processor.numAvailable;
                            }
                        }
                        if (processor.consumesLiquidO2)
                        {
                            foreach (var r in processor.resList)
                            {
                                if (r.Value.resourceName == Constants.LIQUID_O2 && r.Value.input)
                                    inputLqdO2 += r.Value.RatioPerDay * processor.numAvailable;

                                if (r.Value.resourceName == Constants.SLUDGE && r.Value.input)
                                    inputSludge2 += r.Value.RatioPerDay * processor.numAvailable;

                                if (r.Value.resourceName == Constants.LIFESUPPORT && !r.Value.input)
                                    createdLS3 += r.Value.RatioPerDay * processor.numAvailable;

                            }
                        }
                    }

                    double filterdO2Ratio = 0;
                    if (inputFilteredO2 > 0)
                        filterdO2Ratio = Math.Min(1, outputFilteredO2 / inputFilteredO2);

                    double sludgeRatio2 = 0;
                    if (inputSludge2 > 0)
                        sludgeRatio2 = outputSludge / inputSludge2;

                    createdLS3 *= Math.Min(sludgeRatio2, filterdO2Ratio);
                    outputLiquidO2 = outputLqdO2 *  filterdO2Ratio;

                    Log.Info("filteredO2Ratio: " + filterdO2Ratio);
                    Log.Info("sludgeRatio2: " + sludgeRatio2);

                    createdLS += createdLS3;

                    producedLS = 0f;
                    calcScrollPos = GUILayout.BeginScrollView(calcScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

                    IFILS1.LifeSupportLevel curLevel = IFILS1.LifeSupportLevel.improved;
                    using (new GUILayout.HorizontalScope())
                        GUILayout.Label("vvvvv " + IFILS1.LevelString[(int)curLevel] + " vvvvv");

                    // only show those parts which have slurry, sludge or livesupport in the resource list
                    foreach (var p in IFI_Parts.usefulResProcessors)
                    {
                        var processor = p.Value;
                        if (p.Value.level > curLevel)
                        {
                            if (curLevel >= HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level)
                                break;

                                GUILayout.Label("^^^^^ " + IFILS1.LevelString[(int)curLevel] + " ^^^^^");
                            curLevel = p.Value.level;
                                GUILayout.Label("vvvvv " + IFILS1.LevelString[(int)curLevel] + " vvvvv");
                            using (new GUILayout.HorizontalScope())
                                GUILayout.Space(10);
                        }
                        //if (processor.resList.resources.Contains(r.Value.resname))


                        using (new GUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("-", GUILayout.Width(26)))
                                processor.numAvailable = Math.Max(0, processor.numAvailable - 1);

                            GUILayout.Label("  " + processor.numAvailable + " ");
                            if (GUILayout.Button("+", GUILayout.Width(26)))
                                processor.numAvailable++;
                            GUILayout.Label(processor.partTitle, bold);
                            GUILayout.FlexibleSpace();
                        }

                        List<string> consumers = new List<string>();
                        List<string> producers = new List<string>();

                        foreach (var resource in processor.resList)
                        {
                            if (resource.Value.input)
                            {
                                if (resource.Value.resourceName == Constants.SLUDGE)
                                    outputSludge -= resource.Value.RatioPerDay;

                                double singleConsumption = resource.Value.RatioPerDay;
                                double maxConsumption = singleConsumption * processor.numAvailable;

                                bool perSec = false;
                                if (resource.Value.resourceName == Constants.ELECTRIC_CHARGE)
                                {
                                    Log.Info("EC usage: RatioPerSec: " + resource.Value.RatioPerSec + ", RatioPerDay: " + resource.Value.RatioPerDay);
                                    singleConsumption = resource.Value.RatioPerSec;
                                    maxConsumption = singleConsumption * processor.numAvailable;
                                    perSec = true;
                                }

                                string c = resource.Value.ResourceName + " (" + singleConsumption.ToString("F1") + "): " + maxConsumption.ToString("F1") ;
                                if (perSec)
                                    c += "/sec";
                                consumers.Add(c);
                            }
                            else
                            {
                                double singleProduction = resource.Value.RatioPerDay;
                                double maxProduction = singleProduction * processor.numAvailable;

                                double production = maxProduction;
                                string product;
                                if (resource.Value.resourceName == Constants.LIFESUPPORT)
                                {
                                    production = Math.Min(maxProduction, numKerbals);
                                    producedLS += production;
                                    product = resource.Value.resourceName + " (" + singleProduction+"): " + production.ToString("F1") ;

                                }
                                else
                                    product = resource.Value.resourceName + ":    " + maxProduction.ToString("F1");
                                producers.Add(product);
                            }
                        }
                        for (int i = 0; i < Math.Max(consumers.Count, producers.Count); i++)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Space(45);
                                if (i < consumers.Count)
                                    GUILayout.Label(consumers[i], GUILayout.Width(250));
                                else
                                    GUILayout.Space(250);

                                if (i < producers.Count)
                                    GUILayout.Label(producers[i], GUILayout.Width(250));
                                else
                                    GUILayout.Space(250);
                            }
                        }

                    }

                    using (new GUILayout.HorizontalScope())
                        GUILayout.Label("^^^^^ " + IFILS1.LevelString[(int)curLevel] + " ^^^^^");

                    GUILayout.EndScrollView();
                }

                using (new GUILayout.VerticalScope(GUILayout.Width(200)))
                {
                    double dailyUsage = numKerbals;
                    double finalUsage = dailyUsage - createdLS;
                    double finalLSDays = amtKB / finalUsage;

                    using (new GUILayout.HorizontalScope())
                        GUILayout.Label("Slurry: " + dailyUsage);
                    using (new GUILayout.HorizontalScope())
                        GUILayout.Label("Sludge: " + actualOutputSludge.ToString("F1"));
                    if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level == IFILS1.LifeSupportLevel.extreme)
                    {
                        using (new GUILayout.HorizontalScope())
                            GUILayout.Label("Filtered O2: " + outputFilteredO2.ToString("F1"));
                        using (new GUILayout.HorizontalScope())
                            GUILayout.Label("Liquid O2: " + outputLiquidO2.ToString("F1"));
                    }


                    GUILayout.Space(20);
                    using (new GUILayout.HorizontalScope())
                        GUILayout.Label(Util.Colorized(Util.lblGreenColor, "Estimated Daily Usage: " + finalUsage.ToString("F2")));
                    using (new GUILayout.HorizontalScope())
                        GUILayout.Label(Util.Colorized(Util.lblDrkGreenColor, "Max possible K&B: " + producedLS.ToString("F1")));
                    using (new GUILayout.HorizontalScope())
                        GUILayout.Label(Util.Colorized(Util.lblDrkGreenColor, "Actual K&B Production: " + createdLS.ToString("F1")));

                    using (new GUILayout.HorizontalScope())
                        GUILayout.Label(Util.Colorized(Util.lblGreenColor, "Estimated days: " + finalLSDays.ToString("F1")), bold);

                }
            }
            if (GUI.RepeatButton(new Rect(Settings.winPos[(int)Settings.ActiveWin.CalculatorWin].width - 23f, Settings.winPos[(int)Settings.ActiveWin.CalculatorWin].height - 23f, 16, 16), "", Settings.Instance.resizeButton))
            {
                resizingWindow = true;
            }
            ResizeWindow(ref Settings.winPos[(int)Settings.ActiveWin.CalculatorWin]);

            GUI.DragWindow();
        }

        private void ResizeWindow(ref Rect winPos)
        {
            if (Input.GetMouseButtonUp(0))
            {
                resizingWindow = false;
            }

            if (resizingWindow)
            {
                //Log.Info("ResizeWindow, calculatedMinWidth: " + calculatedMinWidth + ", winPos.width: " + winPos.width);
                float minHeight = HighLogic.LoadedSceneIsEditor ? Settings.EDITOR_WIN_HEIGHT : Settings.WINDOW_HEIGHT;
                float calculatedMinWidth = 800f;

                winPos.width = Input.mousePosition.x - winPos.x + 10;
                winPos.width = Mathf.Clamp(winPos.width, calculatedMinWidth, Screen.width);
                winPos.height = (Screen.height - Input.mousePosition.y) - winPos.y + 10;
                winPos.height = Mathf.Clamp(winPos.height, minHeight, Screen.height);
            }
        }

    }
}
