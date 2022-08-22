using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using KSP.IO;

using ToolbarControl_NS;
using ClickThroughFix;

using static IFILifeSupport.RegisterToolbar;

namespace IFILifeSupport
{

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    class ModeSelectWindow : MonoBehaviour
    {
        //GUI related members
        private int winId;
        private Rect winRect;
        private CelestialBody HomeBody;

        bool oldClassicMode, oldImprovedMode, oldAdvancedMode, oldExtremeMode;
        bool active, classicMode, improvedMode, advancedMode, extremeMode;

        bool easyLevel = false;
        bool normalLevel = true;
        bool moderateLevel = false;
        bool hardLevel = false;


        //string abbr;

        bool visible = false;

        const string DISPLAY_TEXT = "Classic mode\tBasic life support, no recycling of any kind.\n\n" +
                "Improved Mode\tGreenhouses are available with a 50% recycle rate\n\n" +
                "Advanced Mode\tMicrobiome and AlgaeHouse available to recycle sludge\n\n" +
                "Extreme Mode\tBioReactor available to create new LifeSupport\n\n\n" +
                "Active\t\tIndicates mod is active in this game.  Automatically\n\t\tenabled if any mode is selected\n";
        void Start()
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().active)
                return;
            visible = !(HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().selectionMade);

            //HomeBody = FlightGlobals.GetHomeBody();
            //abbr = HomeBody.name.Substring(0, 1) + "EI";

            winId = GUIUtility.GetControlID(FocusType.Passive);
            winRect.width = 600;
            winRect.height = 300;
            winRect.x = (Screen.width - winRect.width) / 2;
            winRect.y = (Screen.height - winRect.height) / 2;



            active = HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().active;
            classicMode = HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().classic;
            improvedMode = HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().improved;
            advancedMode = HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().advanced;
            extremeMode = HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().extreme;

            if (!classicMode && !improvedMode && !advancedMode && !extremeMode)
                HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().classic = classicMode = true;

            easyLevel = HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().easy;
            normalLevel = HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().normal;
            moderateLevel = HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().moderate;
            hardLevel = HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().hard;
            if (!easyLevel && !normalLevel && !moderateLevel && !hardLevel)
                HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().normal = normalLevel = true;
        }

        public void OnGUI()
        {
            if (!visible) return;

            winRect = ClickThruBlocker.GUILayoutWindow(
                winId,
                winRect,
                RenderMainWindow,
                "IFI Life Support Mode Selection",
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );
        }

        private void RenderMainWindow(int windowId)
        {
            GUILayout.BeginHorizontal();
            GUILayout.EndHorizontal();
            GUILayout.BeginVertical();
            GUILayout.TextArea(DISPLAY_TEXT);
            GUILayout.Space(20);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(30);
                classicMode = GUILayout.Toggle(classicMode, " Classic Mode");
                if (classicMode)
                    active = true;
            }
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(30);
                improvedMode = GUILayout.Toggle(improvedMode, " Improved Mode");
                if (improvedMode)
                    active = true;
            }
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(30);
                advancedMode = GUILayout.Toggle(advancedMode, " Advanced Mode");
                if (advancedMode)
                    active = true;
            }
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(30);
                extremeMode = GUILayout.Toggle(extremeMode, " Extreme Mode");
                if (extremeMode)
                    active = true;
            }
            GUILayout.Space(10);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(30);
                active = GUILayout.Toggle(active, " Active in this game");
                if (!active)
                    classicMode = improvedMode = advancedMode = false;
            }

            GUILayout.Space(20);



            if (active)
            {
                GUILayout.Label("Difficulty Settings");
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(30);
                    easyLevel = GUILayout.Toggle(easyLevel, "Set all values to the standard Easy level");
                    if (easyLevel)
                        normalLevel = moderateLevel = hardLevel = false;
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(30);
                    normalLevel = GUILayout.Toggle(normalLevel, "Set all values to the standard Normal level");
                    if (normalLevel)
                        easyLevel = moderateLevel = hardLevel = false;
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(30);
                    moderateLevel = GUILayout.Toggle(moderateLevel, "Set all values to the standard Moderate level");
                    if (moderateLevel)
                        normalLevel = easyLevel = hardLevel = false;
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(30);
                    hardLevel = GUILayout.Toggle(hardLevel, "Set all values to the standard Hard level");
                    if (hardLevel)
                        normalLevel = moderateLevel = easyLevel = false;
                }
            }



            if (oldClassicMode != classicMode)
            {
                improvedMode = advancedMode = extremeMode = false;
            }
            else
            {
                if (oldImprovedMode != improvedMode)
                {
                    classicMode = advancedMode = extremeMode = false;
                }
                else
                {
                    if (oldAdvancedMode != advancedMode)
                    {
                        classicMode = improvedMode = extremeMode = false;
                    }
                    else
                    {
                        if (oldExtremeMode != extremeMode)
                        {
                            classicMode = improvedMode = advancedMode = false;
                        }
                    }
                }
            }
            oldClassicMode = classicMode;
            oldImprovedMode = improvedMode;
            oldAdvancedMode = advancedMode;
            oldExtremeMode = extremeMode;
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (classicMode | improvedMode | advancedMode || extremeMode)
                    active = true;
                GUI.enabled = (active & (classicMode | improvedMode | advancedMode | extremeMode)) || !active;
                if (GUILayout.Button(active ? "Confirm" : "Confirm IFI Life Support is disabled", GUILayout.Width(180)))
                {
                    visible = false;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().active = active;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().classic = classicMode;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().improved = improvedMode;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().advanced = advancedMode;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().extreme = extremeMode;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().selectionMade = true;


                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().easy = easyLevel;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().normal = normalLevel;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().moderate = moderateLevel;
                    HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().hard = hardLevel;

                    if (easyLevel)
                        IFILS2.SetDiffPresetStatic(GameParameters.Preset.Easy);

                    if (normalLevel)
                        IFILS2.SetDiffPresetStatic(GameParameters.Preset.Normal);

                    if (moderateLevel)
                        IFILS2.SetDiffPresetStatic(GameParameters.Preset.Moderate);

                    if (hardLevel)
                        IFILS2.SetDiffPresetStatic(GameParameters.Preset.Hard);



                }
                GUI.enabled = false;
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}

