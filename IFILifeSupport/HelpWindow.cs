using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ClickThroughFix;
using ToolbarControl_NS;

using static IFILifeSupport.RegisterToolbar;



namespace IFILifeSupport
{
    internal class HelpWindow : MonoBehaviour
    {
        static HelpWindow helpWindow = null;
        static int helpWinId;
        const string TITLE = "IFI Life Support Information";

        IFILS1.LifeSupportLevel currentTab = IFILS1.LifeSupportLevel.general;
        Vector2 calcScrollPos;

        bool resizingHelpWindow = false;


        internal static void InstantiateHelpWindow()
        {
            if (helpWindow == null)
            {
                GameObject gameObject = new GameObject();
                helpWindow = gameObject.AddComponent<HelpWindow>();
            }
            else
                Destroy(helpWindow);
        }

        void Start()
        {
            helpWinId = SpaceTuxUtility.WindowHelper.NextWindowId("helpWinId");
        }

        void OnGUI()
        {
            Settings.winPos[(int)Settings.ActiveWin.HelpWin] = ClickThruBlocker.GUILayoutWindow(helpWinId, Settings.winPos[(int)Settings.ActiveWin.HelpWin], LSHelpWindow, TITLE); //, LifeSupportDisplay.layoutOptions);
        }
        void OnDestroy()
        {
            helpWindow = null;
        }
        void AppendLine(ref StringBuilder data, string str)
        {
            data.Append(str + "\n");
        }

        private void LSHelpWindow(int windowId)
        {
            if (GUI.Button(new Rect(Settings.winPos[(int)Settings.ActiveWin.HelpWin].width - 24, 3f, 23, 15f), new GUIContent("X")))
                Destroy(this);

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("General", GUILayout.Width(75)))
                    currentTab = IFILS1.LifeSupportLevel.general;

                GUILayout.Space(30);
                GUILayout.Label("Part Levels:");
                GUILayout.Space(10);
                if (GUILayout.Button("Improved", GUILayout.Width(75)))
                    currentTab = IFILS1.LifeSupportLevel.improved;
                if (GUILayout.Button("Advanced", GUILayout.Width(75)))
                    currentTab = IFILS1.LifeSupportLevel.advanced;
                if (GUILayout.Button("Extreme", GUILayout.Width(75)))
                    currentTab = IFILS1.LifeSupportLevel.extreme;
                GUILayout.FlexibleSpace();
            }

            StringBuilder data = new StringBuilder();
            switch (currentTab)
            {
                case IFILS1.LifeSupportLevel.general:
                    var s = HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level.ToString();
                    AppendLine(ref data, "Level: " + char.ToUpper(s[0]) + s.Substring(1));
                    AppendLine(ref data, "Update Frequency (secs): " + HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().RefreshInterval.ToString());
                    AppendLine(ref data, "Auto Warp Cancellation lead time (days): " + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().warpCancellationLeadTime);
                    AppendLine(ref data, "\nLS rate per Kerbal per day: " + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().lsRatePerDay);
                    AppendLine(ref data, "Breathable Atmo Pressure: " + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().breathableAtmoPressure.ToString("N2"));
                    AppendLine(ref data, "Min Intake Air Atmo Pressure: " + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().intakeAirAtmoPressure.ToString("N3"));
                    AppendLine(ref data, "Breathable Homeworld Atmo Adj: " + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().breathableHomeworldAtmoAdjustment.ToString("N2"));
                    AppendLine(ref data, "\nKerbals can die: " + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().kerbalsCanDie.ToString());
                    AppendLine(ref data, "EVA Kerbals can die: " + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().EVAkerbalsCanDie.ToString());
                    AppendLine(ref data, "Inactive time before dying (secs): " + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().InactiveTimeBeforeDeathSecs.ToString());

                    AppendLine(ref data, "\nFollowing adjustments are multiplied to the LS Rate when applicable\n");
                    AppendLine(ref data, "Breathable Atmo Adj: " + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().breathableAtmoAdjustment.ToString("N2"));
                    AppendLine(ref data, "No EC Adj: " + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().lowEcAdjustment.ToString("N2"));
                    break;

                case IFILS1.LifeSupportLevel.improved:
                case IFILS1.LifeSupportLevel.advanced:
                case IFILS1.LifeSupportLevel.extreme:
                    calcScrollPos = GUILayout.BeginScrollView(calcScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
                    foreach (var p in IFI_Parts.usefulResProcessors)
                    {
                        var processor = p.Value;
                        if (p.Value.level != currentTab)
                            continue;

                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label(processor.partTitle, bold);
                            GUILayout.FlexibleSpace();
                        }

                        List<string> consumers = new List<string>();
                        List<string> producers = new List<string>();

                        foreach (var resource in processor.resList)
                        {
                            if (resource.Value.input)
                            {
                                double singleConsumption = resource.Value.RatioPerDay;

                                bool perSec = false;
                                if (resource.Value.resourceName == Constants.ELECTRIC_CHARGE)
                                {
                                    Log.Info("EC usage: RatioPerSec: " + resource.Value.RatioPerSec + ", RatioPerDay: " + resource.Value.RatioPerDay);
                                    singleConsumption = resource.Value.RatioPerSec;
                                    perSec = true;
                                }

                                string c = resource.Value.ResourceName + ": " + singleConsumption.ToString("F1");
                                if (perSec)
                                    c += "/sec";
                                else
                                    c += "/day";
                                consumers.Add(c);
                            }
                            else
                            {
                                double singleProduction = resource.Value.RatioPerDay;

                                string product;
                                {
                                    product = resource.Value.resourceName + ": " + singleProduction + "/day";

                                }
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
                    GUILayout.EndScrollView();

                    break;
            }
            using (new GUILayout.VerticalScope())
            {
                GUILayout.TextArea(data.ToString());

                GUILayout.Space(20);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Close"))
                        Destroy(this);
                    GUILayout.FlexibleSpace();
                }
            }
            if (GUI.RepeatButton(new Rect(Settings.winPos[(int)Settings.ActiveWin.HelpWin].width - 23f, Settings.winPos[(int)Settings.ActiveWin.HelpWin].height - 23f, 16, 16), "", Settings.Instance.resizeButton))
            {
                resizingHelpWindow = true;
            }
            ResizeHelpWindow(Settings.ActiveWin.HelpWin);

            GUI.DragWindow();
        }

        void ResizeHelpWindow(Settings.ActiveWin activeWin)
        {
            if (Input.GetMouseButtonUp(0))
            {
                resizingHelpWindow = false;
            }

            if (resizingHelpWindow)
            {
                Settings.winPos[(int)activeWin].width = Math.Min(Settings.HELPWIN_MINWIDTH, Input.mousePosition.x - Settings.winPos[(int)activeWin].x + 10);
                Settings.winPos[(int)activeWin].height = (Screen.height - Input.mousePosition.y) - Settings.winPos[(int)activeWin].y + 10;
            }
        }
    }
}
