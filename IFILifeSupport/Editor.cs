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
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class Editor : MonoBehaviour
    {
        static public Editor Instance;

        private Dictionary<AvailablePart, PartInfo> partInfos = new Dictionary<AvailablePart, PartInfo>();
        private static UrlDir.UrlConfig[] configs = null;

        List<AvailablePart> GetPartsList()
        {
            List<AvailablePart> loadedParts = new List<AvailablePart>();
            loadedParts.AddRange(PartLoader.LoadedPartsList); // make a copy we can manipulate

            // these two parts are internal and just serve to mess up our lists and stuff
            AvailablePart kerbalEVA = null;
            AvailablePart flag = null;
            for (int i = 0; i < loadedParts.Count; i++)
            //foreach (var part in loadedParts)
            {
                if (loadedParts[i].name == "kerbalEVA")
                    kerbalEVA = loadedParts[i];
                else if (loadedParts[i].name == "flag")
                    flag = loadedParts[i];
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
            return loadedParts;
        }

        static internal bool PartShouldBeHidden(AvailablePart part)
        {
            return PartShouldBeHidden(part, out ConfigNode n);
        }
        /// <summary>
        /// Used both in the Editor and in the PartUpdater.
        /// The confignode is only used in the PartUpdater code
        /// </summary>
        /// <param name="part"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        static internal bool PartShouldBeHidden(AvailablePart part, out ConfigNode n)
        {
            n = null;

            if (part.partConfig == null)
                return true;
            ConfigNode partNode = part.partConfig;
            var nodes = partNode.GetNodes();
            for (int i = 0; i < nodes.Count(); i++)
            {
                var node = nodes[i];
                string s = node.GetValue("name");
                if (s != null)
                {
                    Log.Info("Part: " + part.name + ",   node name: " + s);
                    n = node;

                    bool b = false;
                    if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level <= IFILS1.LifeSupportLevel.classic && s == "IFI_Improved") // improved
                        b = true;
                    if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level <= IFILS1.LifeSupportLevel.improved && s == "IFI_Advanced") // advanced
                        b = true;
                    if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level <= IFILS1.LifeSupportLevel.advanced && s == "IFI_Extreme") // Extreme
                        b = true;

                    if (HighLogic.LoadedSceneIsEditor && !LifeSupportDisplayInfo.ShowRecyclers &&
                        (s == "IFI_Improved" || s == "IFI_Advanced" || s == "IFI_Extreme"))
                        b = true;
                    if (b)
                    {
                        Log.Info("Part: " + part.name + " should be hidden, current level: " + HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level +
                            ", node name: " + s);

                        return true;
                    }
                }
            }
            n = null;
            return false;
        }

        EditorPartListFilter<AvailablePart> filter;
        internal void DefineFilters()
        {
            Log.Info("DefineFilters");
            return;
            if (configs == null)
                configs = GameDatabase.Instance.GetConfigs("PART");

            //ConfigNode n;

            if (filter != null)
                EditorPartList.Instance.ExcludeFilters.RemoveFilter(filter);
            filter = new EditorPartListFilter<AvailablePart>("Unpurchased Filter", (part => PartShouldBeHidden(part)));
            EditorPartList.Instance.ExcludeFilters.AddFilter(filter);
            EditorPartList.Instance.Refresh();

        }
        void InitData()
        {
            if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level == IFILS1.LifeSupportLevel.extreme)
                return;
            if (configs == null)
                configs = GameDatabase.Instance.GetConfigs("PART");
            List<AvailablePart> loadedParts = GetPartsList();
        }
        public void Start()
        {
            Instance = this;
            Log.Info("Editor.Start");
            InitData();
            DefineFilters();
            GameEvents.onEditorPartPlaced.Add(onEditorPartPlaced);
            GameEvents.onEditorPartDeleted.Add(onEditorPartPlaced);
            // GameEvents.onEditorPartEvent.Add(this.onEditorPartEvent);
            GameEvents.onEditorLoad.Add(this.OnShipLoad);
            GameEvents.onEditorStarted.Add(this.OnEditorStarted);
            GameEvents.onEditorPodPicked.Add(this.onEditorPodPicked);
            GameEvents.onEditorShipModified.Add(onEditorShipModified);
            GameEvents.StageManager.OnGUIStageSequenceModified.Add(OnStagingChanged);
            GameEvents.StageManager.OnGUIStageAdded.Add(OnStagingAddedRemoved);
            GameEvents.StageManager.OnGUIStageRemoved.Add(OnStagingAddedRemoved);

        }
        void Destroy()
        {
            Log.Info("Editor.Destroy");
            GameEvents.onEditorPartPlaced.Remove(onEditorPartPlaced);
            GameEvents.onEditorPartDeleted.Remove(onEditorPartPlaced);
            //GameEvents.onEditorPartEvent.Remove(this.onEditorPartEvent);
            GameEvents.onEditorLoad.Remove(this.OnShipLoad);
            GameEvents.onEditorStarted.Remove(this.OnEditorStarted);
            GameEvents.onEditorPodPicked.Remove(this.onEditorPodPicked);
            GameEvents.onEditorShipModified.Remove(onEditorShipModified);
            GameEvents.StageManager.OnGUIStageSequenceModified.Remove(OnStagingChanged);
            GameEvents.StageManager.OnGUIStageAdded.Remove(OnStagingAddedRemoved);
            GameEvents.StageManager.OnGUIStageRemoved.Remove(OnStagingAddedRemoved);

        }

        void UpdateLSDisplay()
        {
            if (LifeSupportDisplayInfo.LSDisplayActive)
            {
                IFI_LifeSupportTrackingDisplay.Instance.ClearStageSummaryList();
                IFI_LifeSupportTrackingDisplay.Instance.GetStageSummary();
            }
        }

        void OnStagingAddedRemoved(int i)
        {
            Log.Info("OnStagingAddedRemoved");
            UpdateLSDisplay();
        }
        private void OnStagingChanged()
        {
            Log.Info("OnStagingChanged");
            UpdateLSDisplay();
        }

        void onEditorShipModified(ShipConstruct sc)
        {
            Log.Info("onEditorShipModified");
            if (LifeSupportDisplayInfo.LSDisplayActive)
                UpdateLSDisplay();

        }
        void onEditorPartPlaced(Part p)
        {
            Log.Info("onEditorPartPlaced");
            UpdateLSDisplay();

        }
#if false
        private void onEditorPartEvent(ConstructionEventType a, Part e)
        {
            return;
            Log.Info("onEditorPartEvent: " + a.ToString());
            if (LifeSupportDisplay.LSDisplayActive)
            {

                if (a == ConstructionEventType.PartAttached || a == ConstructionEventType.PartDetached ||
                    a == ConstructionEventType.PartDeleted)
                {

                    IFILS_Main.lsTracking.ClearStageSummaryList();
                    IFILS_Main.lsTracking.GetStageSummary();
                }
            }
        }
#endif
        private void OnShipLoad(ShipConstruct ship, CraftBrowserDialog.LoadType loadType)
        {
            Log.Info("OnShipLoad");
            UpdateLSDisplay();

        }
        void OnEditorStarted()
        {
            Log.Info("OnEditorStarted");
            UpdateLSDisplay();

        }
        private void onEditorPodPicked(Part part)
        {
            Log.Info("onEditorPodPicked");
            UpdateLSDisplay();
        }
    }
}
