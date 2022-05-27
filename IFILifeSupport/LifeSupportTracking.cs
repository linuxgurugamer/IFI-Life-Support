using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;
using ClickThroughFix;
using ToolbarControl_NS;

using static IFILifeSupport.RegisterToolbar;

namespace IFILifeSupport
{

    [KSPAddon(KSPAddon.Startup.FlightEditorAndKSC, false)]
    public class IFI_LifeSupportTrackingDisplay : MonoBehaviour
    {
        public static IFI_LifeSupportTrackingDisplay Instance;

        internal const string MODID = "IFILifeSupport_NS";
        internal const string MODNAME = "IFI Life Support";
        private static ToolbarControl toolbarControl;
        internal static ToolbarControl Toolbar { get { return toolbarControl; } }

        private const bool KerbalEVARescueDetect = false;

        internal string[,] LS_Status_Hold;

        class LS_Status_Row
        {
            public string[] data = new string[MAX_STATUSES];
        }

        private List<LS_Status_Row> LS_Status_Cache;
        private int[] LS_Status_Width;
        private int[] LS_Status_Spacing;
        internal int LS_Status_Hold_Count;
        // Make sure LS remaining Display conforms to Kerbin time setting.
        public static int HoursPerDay { get { return GameSettings.KERBIN_TIME ? 6 : 24; } }

        const int VESSEL = 0;
        const int LOCATION = 1;
        const int CREW = 2;
        const int LSAVAIL = 3;
        const int DAYS_REM = 4;
        const int SLURRYAVAIL = 5;
        const int SLURRY_DAYS_TO_PROCESS = 6;
        //const int SLURRYRATE = ???;
        const int SLUDGEAVAIL = 7;
        const int SLUDGE_DAYS_TO_PROCESS = 8;
        const int SLUDGE_PROCESS_RATE = 8;
        const int SLURRYCONVRATE = 9;
        const int SLUDGECONVRATE = 10;
        const int SLUDGE_OUTPUT_RATE = 11;
        const int LIFESUPPORT_OUTPUT_RATE = 12;

        internal const int MAX_STATUSES = 13;

        string lblGreenColor = "00ff00";
        string lblDrkGreenColor = "ff9d00";
        string lblBlueColor = "3DB1FF";
        string lblYellowColor = "FFD966";
        string lblRedColor = "f90000";



        public void Awake()
        {
            Log.Info("IFI_LIFESUPPORT_TRACKING.Awake");
            Instance = this;
        }

        public void Start()
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().active)
                return;
            Log.Info("IFI_LIFESUPPORT_TRACKING.Start");

            if (toolbarControl == null)
            {
                toolbarControl = gameObject.AddComponent<ToolbarControl>();
                toolbarControl.AddToAllToolbars(GUIToggle, GUIToggle,
                     ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.TRACKSTATION | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.MAPVIEW,
                     MODID,
                     "IFILS_Button",
                     "IFILS/Textures/IFI_LS_GRN_38",
                     "IFILS/Textures/IFI_LS_GRN_24",
                    MODNAME);
            }
            LS_Status_Width = new int[MAX_STATUSES];
            LS_Status_Spacing = new int[MAX_STATUSES];

            GameEvents.onVesselRecoveryProcessingComplete.Add(onVesselRecoveryProcessingComplete);
            GameEvents.OnVesselRecoveryRequested.Add(OnVesselRecoveryRequested);
            GameEvents.onCrewBoardVessel.Add(onCrewBoardVessel);

            GameEvents.onHideUI.Add(OnHideUI);
            GameEvents.onShowUI.Add(OnShowUI);
        }

        private void GUIToggle()
        {
            LifeSupportDisplayInfo.LSDisplayActive = !LifeSupportDisplayInfo.LSDisplayActive;
            if (HighLogic.LoadedSceneIsEditor)
            {
                InitStatusCache();
                ClearStageSummaryList();
                GetStageSummary();
            }
        }

        internal void SetUpDisplayLines(Vessel vessel, string IFI_Location, int IFI_Crew, double days_rem, double LSAval, double SlurryAvail, double SludgeAvail,
            double slurryRate, double sludgeRate, ref int LS_Status_Hold_Count)
        {
            LS_Status_Hold[LS_Status_Hold_Count, VESSEL] = Colorized(lblGreenColor, vessel.vesselName);
            LS_Status_Hold[LS_Status_Hold_Count, LOCATION] = Colorized(lblBlueColor, IFI_Location); // IFI_Location;
            string H_Crew = Convert.ToString(IFI_Crew);

            if (vessel.vesselType == VesselType.EVA)
            {
                H_Crew = "EVA";
            }

            LS_Status_Hold[LS_Status_Hold_Count, CREW] = H_Crew;
            if (vessel.vesselType == VesselType.EVA && LifeSupportUsageUpdate.KerbalEVARescueDetect(vessel, IFI_Crew))
            {
                LS_Status_Hold[LS_Status_Hold_Count, LSAVAIL] = "RESCUE";
                LS_Status_Hold[LS_Status_Hold_Count, DAYS_REM] = "RESCUE";
            }
            else
            {
                {
                    string color = lblGreenColor;
                    if (IFI_Crew > 0 && days_rem < 3)
                        color = lblYellowColor;
                    if (IFI_Crew > 0 && days_rem < 1)
                        color = lblRedColor;

                    LS_Status_Hold[LS_Status_Hold_Count, LSAVAIL] = Colorized(color, Convert.ToString(Math.Round(LSAval, 1)));
                    if (IFI_Crew > 0)
                        LS_Status_Hold[LS_Status_Hold_Count, DAYS_REM] = Colorized(color, FormatToDateTime(days_rem));
                }
            }
            if (vessel.vesselType != VesselType.EVA)
            {
                double secsPerDay = FlightGlobals.GetHomeBody().solarDayLength;

                // Need to scan for all greenhouses here
                LS_Status_Hold[LS_Status_Hold_Count, SLURRYAVAIL] = Math.Round(SlurryAvail, 1).ToString("F1");

                // Need to scan for all sludge convertors here
                LS_Status_Hold[LS_Status_Hold_Count, SLUDGEAVAIL] = Math.Round(SludgeAvail, 1).ToString("F1");


                Log.Info("Slurry: " + SlurryAvail + ",    rate: " + (secsPerDay * slurryRate));
                if (slurryRate > 0)
                    LS_Status_Hold[LS_Status_Hold_Count, SLURRY_DAYS_TO_PROCESS] = (SlurryAvail / (secsPerDay * slurryRate)).ToString("N1");
                else
                    LS_Status_Hold[LS_Status_Hold_Count, SLURRY_DAYS_TO_PROCESS] = "n/a";
                if (sludgeRate > 0)
                    LS_Status_Hold[LS_Status_Hold_Count, SLUDGE_DAYS_TO_PROCESS] = (SludgeAvail / (secsPerDay * sludgeRate)).ToString("N1");
                else
                    LS_Status_Hold[LS_Status_Hold_Count, SLUDGE_DAYS_TO_PROCESS] = "n/a";

                LS_Status_Hold[LS_Status_Hold_Count, SLURRYCONVRATE] = (secsPerDay * slurryRate).ToString("N1");
                LS_Status_Hold[LS_Status_Hold_Count, SLUDGECONVRATE] = (secsPerDay * sludgeRate).ToString("N1");
            }

            LS_Status_Hold_Count += 1;
        }

        string FormatToDateTime(double d)
        {
            d = d * FlightGlobals.GetHomeBody().solarDayLength;
            double days = Math.Floor(d / FlightGlobals.GetHomeBody().solarDayLength);
            double hours = Math.Floor((d - days * FlightGlobals.GetHomeBody().solarDayLength) / 3600f);
            double minutes = Math.Floor((d - days * FlightGlobals.GetHomeBody().solarDayLength - hours * 3600f) / 60f);
            double secs = Math.Floor(d - days * FlightGlobals.GetHomeBody().solarDayLength - hours * 3600f - minutes * 60f);
            return days.ToString("N0") + "d " +
                string.Format("{0:00.}", hours) + "h " +
                string.Format("{0:00.}", minutes) + "m " +
                string.Format("{0:00.}", secs) + "s";
        }

        internal void CheckAlertLevels(double days_rem, ref int LS_ALERT_LEVEL)
        {
            if (LS_ALERT_LEVEL < 2 && days_rem < 3)
            {
                toolbarControl.SetTexture("IFILS/Textures/IFI_LS_CAU_38", "IFILS/Textures/IFI_LS_CAU_24");
                LS_ALERT_LEVEL = 2;
                if (LifeSupportDisplayInfo.WarpCancel && days_rem < HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().warpCancellationLeadTime && TimeWarp.CurrentRate > 1)
                {
                    TimeWarp.SetRate(0, false);
                    ScreenMessages.PostScreenMessage("TimeWarp canceled due to less than 3 days of Kibbles & Bits remaining", 10f);

                }
            }
            if (LS_ALERT_LEVEL < 3 && days_rem <= 1)
            {
                toolbarControl.SetTexture("IFILS/Textures/IFI_LS_DAN_38", "IFILS/Textures/IFI_LS_DAN_24");
                LS_ALERT_LEVEL = 3;
                if (LifeSupportDisplayInfo.WarpCancel && days_rem < HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().warpCancellationLeadTime && TimeWarp.CurrentRate > 1)
                {
                    TimeWarp.SetRate(0, false);
                    ScreenMessages.PostScreenMessage("TimeWarp canceled due to less than 1 day of Kibbles & Bits remaining", 10f);
                }
            }
        }



        bool hide = false;
        void OnHideUI() { hide = true; }
        void OnShowUI() { hide = false; }
        bool Hide { get { return hide; } }

        void onCrewBoardVessel(GameEvents.FromToAction<Part, Part> fta)
        {
            RestoreStarvingCrew(fta.from.vessel, fta.to.vessel.protoVessel);
        }
        void OnVesselRecoveryRequested(Vessel v)
        {
            Log.Info("OnVesselRecoveryRequested");
            ScreenMessages.PostScreenMessage("OnVesselRecoveryRequested", 5);
            RestoreStarvingCrew(v);
        }

        void OnDestroy()
        {
            Log.Info("LifeSupportTracking.OnDestroy");
            GameEvents.onVesselRecoveryProcessingComplete.Remove(onVesselRecoveryProcessingComplete);
            GameEvents.OnVesselRecoveryRequested.Remove(OnVesselRecoveryRequested);
            GameEvents.onCrewBoardVessel.Remove(onCrewBoardVessel);

            Instance = null;
        }

        void onVesselRecoveryProcessingComplete(ProtoVessel pv, MissionRecoveryDialog mrd, float f)
        {
            Log.Info("onVesselRecoveryProcessingComplete");
            ScreenMessages.PostScreenMessage("onVesselRecoveryProcessingComplete", 5);

            RestoreStarvingCrew(null, pv);
        }


        public static void RestoreStarvingCrew(Vessel v = null, ProtoVessel pv = null)
        {
            StarvingKerbal sk;
            List<string> toDelete = new List<string>();
            Log.Info("RestoreStarvingCrew");
            if (v != null)
            {
                foreach (ProtoCrewMember c in v.GetVesselCrew())
                {
                    Log.Info("RestoreStarvingcrew, v.crew.name: " + c.name);
                    if (LifeSupportUsageUpdate.starvingKerbals.TryGetValue(c.name, out sk))
                    {
                        // Found a starving kerbal in the Crew building
                        Log.Info("Found starving kerbal: " + c.name);
                        toDelete.Add(c.name);
                        c.type = ProtoCrewMember.KerbalType.Crew;
                        KerbalRoster.SetExperienceTrait(c, sk.trait);
                        ScreenMessages.PostScreenMessage(c.name + " restored to active status", 5);

                    }
                }
            }
            if (pv != null)
            {
                foreach (ProtoCrewMember c in pv.GetVesselCrew())
                {
                    Log.Info("RestoreStarvingcrew, pv.crew.name: " + c.name);
                    if (LifeSupportUsageUpdate.starvingKerbals.TryGetValue(c.name, out sk))
                    {
                        // Found a starving kerbal in the Crew building
                        Log.Info("Found starving kerbal: " + c.name);
                        toDelete.Add(c.name);
                        c.type = ProtoCrewMember.KerbalType.Crew;
                        KerbalRoster.SetExperienceTrait(c, sk.trait);
                        ScreenMessages.PostScreenMessage(c.name + " restored to active status", 5);

                    }
                }
            }

            Log.Info("RestoreStarvingCrew");
            if (HighLogic.CurrentGame == null || HighLogic.CurrentGame.CrewRoster == null || HighLogic.CurrentGame.CrewRoster.Crew == null)
                return;

            var s = HighLogic.CurrentGame.CrewRoster.Crew;

            foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Tourist)
            {
                Log.Info("CrewRoster.Crew kerbal.name: " + kerbal.name + ",  rosterStatus: " + kerbal.rosterStatus + ", type: " + kerbal.type);
                if (kerbal.rosterStatus == ProtoCrewMember.RosterStatus.Available)
                {
                    if (LifeSupportUsageUpdate.starvingKerbals.TryGetValue(kerbal.name, out sk))
                    {
                        // Found a starving kerbal in the Crew building
                        Log.Info("Found starving kerbal: " + kerbal.name);
                        toDelete.Add(kerbal.name);
                        kerbal.type = ProtoCrewMember.KerbalType.Crew;
                        KerbalRoster.SetExperienceTrait(kerbal, sk.trait);
                        ScreenMessages.PostScreenMessage(kerbal.name + " restored to active status", 5);

                    }
                }
            }
            if (toDelete.Count > 0)
            {
                foreach (var s1 in toDelete)
                    LifeSupportUsageUpdate.starvingKerbals.Remove(s1);
                toDelete.Clear();
#if false
                for (int i = 0; i < HighLogic.CurrentGame.CrewRoster.Count; i++)
                {
                    Log.Info("KerbalRoster[" + i + "]: " + HighLogic.CurrentGame.CrewRoster[i].name + ", trait: " + HighLogic.CurrentGame.CrewRoster[i].trait);
                }
#endif
            }
        }



        Vessel lastVesselChecked = null;
        IFILS1.LifeSupportLevel lastLSlevel = IFILS1.LifeSupportLevel.none;
        bool lastShowInResourcePanel = false;

        void FixedUpdate()
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT &&
                (lastVesselChecked != FlightGlobals.ActiveVessel ||
                lastLSlevel != HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level ||
                lastShowInResourcePanel != HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().showInResourcePanel))
            {
                lastVesselChecked = FlightGlobals.ActiveVessel;
                lastLSlevel = HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level;
                lastShowInResourcePanel = HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().showInResourcePanel;

                for (int idx = 0; idx < FlightGlobals.ActiveVessel.Parts.Count; idx++)
                {
                    var p = FlightGlobals.ActiveVessel.Parts[idx];

                    for (int idx2 = 0; idx2 < p.Resources.Count; idx2++)
                    {
                        var r = p.Resources[idx2];

                        if (r.resourceName == Constants.SLUDGE)
                        {
                            if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().showInResourcePanel &&
                                HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.advanced)
                                r.isVisible = true;
                            else
                                r.isVisible = false;

                        }
                        if (r.resourceName == Constants.SLURRY)
                        {
                            if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().showInResourcePanel &&
                                HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.improved)
                                r.isVisible = true;
                            else
                                r.isVisible = false;
                        }
                    }
                }
            }
        }

        string tooltip = "";

        private void OnGUI()
        {
            if (HighLogic.LoadedSceneIsGame && !Hide && LifeSupportDisplayInfo.LSDisplayActive && HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().active)
            {
                if (!RegisterToolbar.initted)
                {
                    RegisterToolbar.initted = true;

                    RegisterToolbar.tooltipStyle = new GUIStyle(GUI.skin.GetStyle("label"));
                    Texture2D texBack = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                    texBack.SetPixel(0, 0, new Color(0.0f, 0.0f, 0.0f, 1f));
                    texBack.Apply();
                    RegisterToolbar.tooltipStyle.normal.background = texBack;

                    RegisterToolbar.kspToolTipStyle = new GUIStyle(HighLogic.Skin.GetStyle("label"));
                    texBack = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                    texBack.SetPixel(0, 0, new Color(0.0f, 0.0f, 0.0f, 1f));
                    texBack.Apply();
                    RegisterToolbar.kspToolTipStyle.normal.background = texBack;
                }

                if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().useKSPskin)
                    GUI.skin = HighLogic.Skin;
                if (!HighLogic.LoadedSceneIsEditor && HighLogic.LoadedSceneIsGame)
                {
                    string TITLE = "IFI Life Support Vessel Status Display ";

                    LifeSupportDisplayInfo.statusWindowPos = ClickThruBlocker.GUILayoutWindow(99988, LifeSupportDisplayInfo.statusWindowPos, LSFlightStatusWindow, TITLE); //, LifeSupportDisplay.layoutOptions);
                }
                else
                    if (HighLogic.LoadedSceneIsEditor)
                {
                    string TITLE = "IFI Life Support Vessel Info ";

                    LifeSupportDisplayInfo.editorInfoWindowPos = ClickThruBlocker.GUILayoutWindow(99988, LifeSupportDisplayInfo.editorInfoWindowPos, LSEditorInfoWindow, TITLE); //, LifeSupportDisplay.layoutOptions);

                }
                if (LifeSupportDisplayInfo.LSInfoDisplay)
                {
                    string TITLE = "IFI Life Support Information ";

                    LifeSupportDisplayInfo.infoWindowPos = ClickThruBlocker.GUILayoutWindow(99989, LifeSupportDisplayInfo.infoWindowPos, LSInfoWindow, TITLE); //, LifeSupportDisplay.layoutOptions);

                }
                DrawToolTip();

            }
        }

        public void InitStatusCache()
        {
            if (LS_Status_Cache != null)
                LS_Status_Cache.Clear();
            else
                LS_Status_Cache = new List<LS_Status_Row>();
        }
        public void ClearStageSummaryList()
        {
            maxStage = -1;
            if (stageSummaryList != null)
                stageSummaryList.Clear();
            else
                stageSummaryList = new Dictionary<int, StageSummary>();
        }
        internal void ClearStatusWidths()
        {
            for (int i = 0; i < MAX_STATUSES; i++)
            {
                LS_Status_Spacing[i] = 0;
                LS_Status_Width[i] = 0;
            }
        }

        LS_Status_Row lsStatusRow;

        void GetWidth(string s, int i, bool init = false, bool InitStatusRow = false)
        {
            float minWidth, maxWidth;

            if (InitStatusRow)
            {
                lsStatusRow = new LS_Status_Row();
            }

            GUI.skin.label.CalcMinMaxWidth(new GUIContent(s), out minWidth, out maxWidth);
            maxWidth += 10;
            if (init)
                LS_Status_Width[i] = (int)maxWidth;
            else
                LS_Status_Width[i] = Math.Max(LS_Status_Width[i], (int)maxWidth);
            lsStatusRow.data[i] = s;
        }

        void AddSpacing(int col, int spacing)
        {
            LS_Status_Spacing[col] += spacing;
        }

        void FinishStatusRow()
        {
            LS_Status_Cache.Add(lsStatusRow);
            lsStatusRow = null;
        }

        static bool fullDisplay = false;

        class StageSummary
        {
            public int stage;
            public int crew;
            public double LifeSupport;
            public double MaxLifeSupport;
            public double OrganicSlurry;
            public double Sludge;
            public double SlurryProcessRate;
            public double SludgeProcessRate;

            public double LifeSupportOutputRate;
            public double SludgeOutputRate;

            public StageSummary(int s)
            {
                stage = s;
                crew = 0;
                LifeSupport = 0;
                MaxLifeSupport = 0;
                OrganicSlurry = 0;
                Sludge = 0;
                SlurryProcessRate = 0;
                SludgeProcessRate = 0;
                LifeSupportOutputRate = 0;
                SludgeOutputRate = 0;
            }
        }

        Dictionary<int, StageSummary> stageSummaryList = new Dictionary<int, StageSummary>();
        int maxStage = -1;
        public void GetStageSummary()
        {
            Log.Info("GetStageSummary");
            if (!HighLogic.LoadedSceneIsEditor)
                return;
            List<Part> parts = EditorLogic.fetch.ship != null ? EditorLogic.fetch.ship.Parts : new List<Part>();
            maxStage = -1;
            if (parts != null && parts.Count > 0)
            {
                Log.Info("Parts.count: " + parts.Count);
                foreach (Part part in parts)
                {
                    Log.Info("part: " + part.partInfo.name);
                    int stage;
                    if (LifeSupportDisplayInfo.Summarize)
                        stage = 0;
                    else
                        stage = part.inverseStage + 1;
                    maxStage = Math.Max(maxStage, stage);
                    StageSummary stageSummary;
                    bool newStage = false;
                    if (!stageSummaryList.TryGetValue(stage, out stageSummary))
                    {
                        stageSummary = new StageSummary(stage);
                        newStage = true;
                    }
                    stageSummary.crew += part.CrewCapacity;
                    foreach (PartResource partResource in part.Resources)
                    {
                        if (partResource.resourceName == Constants.LIFESUPPORT)
                            stageSummary.LifeSupport += partResource.amount;
                        if (partResource.resourceName == Constants.LIFESUPPORT)
                            stageSummary.maxLifeSupport += partResource.maxAmount;
                        if (partResource.resourceName == Constants.SLURRY)
                            stageSummary.OrganicSlurry += partResource.maxAmount;
                        if (partResource.resourceName == Constants.SLUDGE)
                            stageSummary.Sludge += partResource.maxAmount;
                    }
                    for (int m = 0; m < part.Modules.Count; m++)
                    {
                        if (part.Modules[m].moduleName == "ModuleIFILifeSupport")
                        {

                            ModuleIFILifeSupport m1 = (ModuleIFILifeSupport)part.Modules[m];


                            for (int i = 0; i < m1.inputList.Count; i++)
                            {
                                ResourceRatio inp = m1.inputList[i].rr;
                                if (inp.ResourceName == Constants.SLURRY)
                                    stageSummary.SlurryProcessRate += inp.Ratio;
                                if (inp.ResourceName == Constants.SLUDGE)
                                    stageSummary.SludgeProcessRate += inp.Ratio;
                            }
                            for (int i = 0; i < m1.outputList.Count; i++)
                            {
                                ResourceRatio output = m1.outputList[i];
                                if (output.ResourceName == Constants.LIFESUPPORT)
                                    stageSummary.LifeSupportOutputRate += output.Ratio;
                                if (output.ResourceName == Constants.SLUDGE)
                                    stageSummary.SludgeOutputRate += output.Ratio;
                            }
                        }
                    }
                    if (newStage)
                    {
                        if (stageSummary.crew > 0 || stageSummary.MaxStageSummary > 0 || stageSummary.OrganicSlurry > 0 || stageSummary.Sludge > 0 ||
                            stageSummary.SlurryProcessRate > 0 || stageSummary.SludgeProcessRate > 0 ||
                            stageSummary.LifeSupportOutputRate > 0 || stageSummary.SludgeOutputRate > 0)
                            stageSummaryList.Add(stage, stageSummary);
                        stageSummary = null;
                    }

                }
            }
        }
        string Colorized(string color, string txt)
        {
            return String.Format("<color=#{0}>{1}</color>", color, txt);
        }
        void GetColorized(int i, ref string txt)
        {
            switch (i)
            {
                case SLURRYAVAIL:
                case SLURRYCONVRATE:
                case SLURRY_DAYS_TO_PROCESS:
                    txt = Colorized(lblGreenColor, txt);
                    break;
                case SLUDGEAVAIL:
                case SLUDGECONVRATE:
                case SLUDGE_DAYS_TO_PROCESS:
                case SLUDGE_OUTPUT_RATE:
                    txt = Colorized(lblDrkGreenColor, txt);
                    break;
                case LIFESUPPORT_OUTPUT_RATE:
                    txt = Colorized(lblBlueColor, txt);
                    break;
            }
        }
        const int SPACING = 30;
        private void LSEditorInfoWindow(int windowId)
        {

            var bold = new GUIStyle(GUI.skin.label);
            bold.fontStyle = FontStyle.Bold;

            InitStatusCache();
            ClearStatusWidths();
            double secsPerDay = 3600 * (GameSettings.KERBIN_TIME ? 6 : 24);

            GetWidth("Stage", LOCATION, true, true);
            GetWidth("Crew", CREW, true);
            GetWidth("   Life\nSupport", LSAVAIL, true);
            GetWidth("Days", DAYS_REM, true);
            if (LifeSupportDisplayInfo.ShowRecyclers && HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.improved)
            {
                AddSpacing(DAYS_REM, SPACING);
                GetWidth("   Slurry\nCapacity", SLURRYAVAIL, true);
                GetWidth("Daily\nProcess\nRate", SLURRY_DAYS_TO_PROCESS, true);
                AddSpacing(SLURRY_DAYS_TO_PROCESS, SPACING);
                if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.advanced)
                {
                    GetWidth("Sludge\nOutput\nRate", SLUDGE_OUTPUT_RATE, true);
                    GetWidth("Sludge\nCapacity", SLUDGEAVAIL, true);
                    GetWidth("Daily\nProcess\nRate", SLUDGE_PROCESS_RATE, true);
                    AddSpacing(SLUDGE_PROCESS_RATE, SPACING);
                }

                GetWidth("LifeSupport\nDaily Output\nRate", LIFESUPPORT_OUTPUT_RATE, true, false);

            }
            FinishStatusRow();

            for (int i = 0; i <= maxStage; i++)
            {
                StageSummary ss;
                if (stageSummaryList.TryGetValue(i, out ss))
                {

                    GetWidth(ss.stage.ToString(), LOCATION, false, true);
                    GetWidth(ss.crew.ToString(), CREW);
                    GetWidth(ss.LifeSupport.ToString("N2"), LSAVAIL);
                    if (ss.crew > 0)
                        GetWidth((ss.LifeSupport / ss.crew).ToString("N2"), DAYS_REM);
                    else
                        GetWidth("n/a", DAYS_REM);
                    if (LifeSupportDisplayInfo.ShowRecyclers && HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.improved)
                    {
                        GetWidth(ss.OrganicSlurry.ToString("N2"), SLURRYAVAIL);
                        GetWidth((secsPerDay * ss.SlurryProcessRate).ToString("N2"), SLURRY_DAYS_TO_PROCESS);
                        if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.advanced)
                        {
                            GetWidth((secsPerDay * ss.SludgeOutputRate).ToString("N2"), SLUDGE_OUTPUT_RATE);
                            GetWidth(ss.Sludge.ToString(), SLUDGEAVAIL);
                            if (ss.SludgeProcessRate > 0)
                                GetWidth((secsPerDay * ss.SludgeProcessRate).ToString("N2"), SLUDGE_PROCESS_RATE);
                            else
                                GetWidth("n/a", SLUDGE_PROCESS_RATE);
                        }

                        GetWidth((secsPerDay * ss.LifeSupportOutputRate).ToString("n2"), LIFESUPPORT_OUTPUT_RATE);
                    }
                    FinishStatusRow();
                }
            }


            GUILayout.BeginVertical();
            //Log.Info("Displaying headers");

            GUILayout.BeginHorizontal();
            for (int i = 0; i < MAX_STATUSES; i++)
            {
                LS_Status_Width[i] += LS_Status_Spacing[i];
                if (LS_Status_Cache[0].data[i] != null)
                {
                    //Log.Info("i: " + LS_Status_Cache[0].data[i]);
                    string txt = LS_Status_Cache[0].data[i];

                    GetColorized(i, ref txt);

                    GUILayout.Label(txt, bold, GUILayout.Width(LS_Status_Width[i]));
                }
            }
            GUILayout.Label(" ", GUILayout.Width(15));

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            LifeSupportDisplayInfo.infoScrollPos = GUILayout.BeginScrollView(LifeSupportDisplayInfo.infoScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.MaxHeight(345));
            for (int i1 = 1; i1 < LS_Status_Cache.Count; i1++)
            {
                GUILayout.BeginHorizontal();
                for (int i = 1; i < MAX_STATUSES; i++)
                {
                    if (LS_Status_Cache[i1].data[i] != null)
                    {
                        string txt = LS_Status_Cache[i1].data[i];
                        GetColorized(i, ref txt);
                        GUILayout.Label(txt, bold, GUILayout.Width(LS_Status_Width[i]));
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();

            bool b = GUILayout.Toggle(LifeSupportDisplayInfo.Summarize, "Summarize");
            if (b != LifeSupportDisplayInfo.Summarize)
            {
                LifeSupportDisplayInfo.Summarize = b;
                InitStatusCache();
                ClearStageSummaryList();
                GetStageSummary();
            }
            GUILayout.FlexibleSpace();
            if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level > IFILS1.LifeSupportLevel.classic)
            {
                b = GUILayout.Toggle(LifeSupportDisplayInfo.ShowRecyclers, "Show Recyclers");
                if (b != LifeSupportDisplayInfo.ShowRecyclers)
                {
                    LifeSupportDisplayInfo.ShowRecyclers = b;
                    Editor.Instance.DefineFilters();
                    LifeSupportDisplayInfo.ReinitEditorInfoWindowPos();
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUI.DragWindow();
        }


        void AppendLine(ref StringBuilder data, string str)
        {
            data.Append(str + "\n");
        }
        private void LSInfoWindow(int windowId)
        {
            StringBuilder data = new StringBuilder();

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

            GUILayout.BeginVertical();
            GUILayout.TextArea(data.ToString());

            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close"))
                LifeSupportDisplayInfo.LSInfoDisplay = false;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void LSFlightStatusWindow(int windowId)
        {
            var bold = new GUIStyle(GUI.skin.label);
            bold.fontStyle = FontStyle.Bold;
            InitStatusCache();

            GetWidth("Vessel", VESSEL, true, true);
            GetWidth("Location", LOCATION, true);
            GetWidth("Crew", CREW, true);
            GetWidth("   Life\nSupport", LSAVAIL, true);
            GetWidth("     Days\nRemaining", DAYS_REM, true);
            if (fullDisplay && HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.improved)
            {
                GetWidth("Slurry (rate)", SLURRYAVAIL, true);
                GetWidth("Process\n Time", SLURRY_DAYS_TO_PROCESS, true);
                if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.advanced)
                {
                    GetWidth("Sludge (rate)", SLUDGEAVAIL, true);
                    GetWidth("Process\n Time", SLUDGE_DAYS_TO_PROCESS, true, false);
                }
            }
            FinishStatusRow();
            for (int LLC = 0; LLC < LS_Status_Hold_Count; LLC++)
            {
                GetWidth(LS_Status_Hold[LLC, VESSEL], VESSEL, false, true);
                GetWidth(LS_Status_Hold[LLC, LOCATION], LOCATION);
                GetWidth("  " + LS_Status_Hold[LLC, CREW], CREW);
                GetWidth("  " + LS_Status_Hold[LLC, LSAVAIL], LSAVAIL);
                GetWidth("  " + LS_Status_Hold[LLC, DAYS_REM], DAYS_REM);

                if (fullDisplay && HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.improved)
                {
                    if (LS_Status_Hold[LLC, SLURRYAVAIL] != null && LS_Status_Hold[LLC, SLURRYAVAIL] != "")
                    {
                        if (LS_Status_Hold[LLC, SLURRYCONVRATE] != null && LS_Status_Hold[LLC, SLURRYCONVRATE] != "")
                            GetWidth(LS_Status_Hold[LLC, SLURRYAVAIL] + "(" + LS_Status_Hold[LLC, SLURRYCONVRATE] + ")", SLURRYAVAIL);
                        else
                            GetWidth(" ", SLURRYAVAIL);
                    }
                    else
                        GetWidth(" ", SLURRYAVAIL);

                    if (LS_Status_Hold[LLC, SLURRY_DAYS_TO_PROCESS] != null && LS_Status_Hold[LLC, SLURRY_DAYS_TO_PROCESS] != "n/a" && LS_Status_Hold[LLC, SLURRY_DAYS_TO_PROCESS] != "")
                        GetWidth(LS_Status_Hold[LLC, SLURRY_DAYS_TO_PROCESS] + "d", SLURRY_DAYS_TO_PROCESS);
                    else
                        GetWidth(" ", SLURRY_DAYS_TO_PROCESS);

                    if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level >= IFILS1.LifeSupportLevel.advanced)
                    {
                        if (LS_Status_Hold[LLC, SLUDGEAVAIL] != null && LS_Status_Hold[LLC, SLUDGEAVAIL] != "")
                        {
                            if (LS_Status_Hold[LLC, SLUDGECONVRATE] != null && LS_Status_Hold[LLC, SLUDGECONVRATE] != "")
                                GetWidth(LS_Status_Hold[LLC, SLUDGEAVAIL] + "(" + LS_Status_Hold[LLC, SLUDGECONVRATE] + ")", SLUDGEAVAIL);
                            else
                                GetWidth(LS_Status_Hold[LLC, SLUDGEAVAIL], SLUDGEAVAIL);
                        }
                        else
                            GetWidth(" ", SLUDGEAVAIL);

                        if (LS_Status_Hold[LLC, SLUDGE_DAYS_TO_PROCESS] != null && LS_Status_Hold[LLC, SLUDGE_DAYS_TO_PROCESS] != "n/a" && LS_Status_Hold[LLC, SLUDGE_DAYS_TO_PROCESS] != "")
                            GetWidth(LS_Status_Hold[LLC, SLUDGE_DAYS_TO_PROCESS] + "d", SLUDGE_DAYS_TO_PROCESS);
                        else
                            GetWidth(" ", SLUDGE_DAYS_TO_PROCESS);
                    }
                }
                FinishStatusRow();
            }

            GUILayout.BeginVertical();
            //Log.Info("Displaying headers");

            GUILayout.BeginHorizontal();
            for (int i = 0; i < MAX_STATUSES; i++)
            {
                LS_Status_Width[i] += LS_Status_Spacing[i];
                if (LS_Status_Cache[0].data[i] != null)
                {
                    string txt = LS_Status_Cache[0].data[i];

                    GetColorized(i, ref txt);

                    GUILayout.Label(txt, bold, GUILayout.Width(LS_Status_Width[i]));
                }
            }
            GUILayout.Label(" ", GUILayout.Width(15));

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            LifeSupportDisplayInfo.infoScrollPos = GUILayout.BeginScrollView(LifeSupportDisplayInfo.infoScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.MaxHeight(345));
            for (int i1 = 1; i1 < LS_Status_Cache.Count; i1++)
            {
                GUILayout.BeginHorizontal();
                for (int i = 0; i < MAX_STATUSES; i++)
                {
                    if (LS_Status_Cache[i1].data[i] != null)
                    {
                        string txt = LS_Status_Cache[i1].data[i];
                        GetColorized(i, ref txt);
                        GUILayout.Label(txt, bold, GUILayout.Width(LS_Status_Width[i]));
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
            //GUILayout.FlexibleSpace();
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();

            var s = new GUIContent("Auto-Cancel Warp on low K & B", "Only for crewed vessels");
            LifeSupportDisplayInfo.WarpCancel = GUILayout.Toggle(LifeSupportDisplayInfo.WarpCancel, s);
            GUILayout.FlexibleSpace();
            if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level > IFILS1.LifeSupportLevel.classic)
            {

                if (fullDisplay)
                {
                    if (GUILayout.Button("<<", GUILayout.Width(26), GUILayout.Height(22)))
                    {
                        fullDisplay = false;
                        LifeSupportDisplayInfo.ReinitInfoWindowPos();
                    }
                }
                else
                {
                    if (GUILayout.Button(">>", GUILayout.Width(26), GUILayout.Height(22)))
                        fullDisplay = true;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            if (GUI.Button(new Rect(3, 3f, 20, 15f), new GUIContent("I")))
            {
                LifeSupportDisplayInfo.LSInfoDisplay = !LifeSupportDisplayInfo.LSInfoDisplay;
            }
            if (GUI.Button(new Rect(LifeSupportDisplayInfo.statusWindowPos.width - 24, 3f, 23, 15f), new GUIContent("X")))
            {
                toolbarControl.SetFalse(true);
            }
            GUI.DragWindow();
            if (Event.current.type == EventType.Repaint)
                tooltip = GUI.tooltip;
        }

        void DrawToolTip()
        {
            if (tooltip == "")
                return;
            GUIStyle activeStyle;
            GUISkin activeSkin = GUI.skin;
            float multiplier = 1f;
            if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().useKSPskin)
            {
                activeStyle = RegisterToolbar.kspToolTipStyle;
                multiplier = 1.5f;
            }
            else
            {
                activeStyle = RegisterToolbar.tooltipStyle;
            }

            Rect pos = new Rect();
            pos.x = Event.current.mousePosition.x + 10;
            pos.y = Event.current.mousePosition.y + 20;
            Vector2 size = activeSkin.box.CalcSize(new GUIContent(tooltip));
            pos.width = size.x*multiplier;
            pos.height = size.y*multiplier;

            GUI.Window(9999345, pos, DrawToolTipWindow, "", activeStyle);
        }

        void DrawToolTipWindow(int id)
        {
            GUILayout.Label(tooltip);
            GUI.BringWindowToFront(id);
        }



    }
}