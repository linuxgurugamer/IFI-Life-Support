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
    partial class Editor : MonoBehaviour
    {
        static public Editor Instance;

        //private static UrlDir.UrlConfig[] configs = null;

#if false
        internal static SortedList<string, IFI_Generator> allResProcessors = new SortedList<string, IFI_Generator>();
        internal static SortedList<string, IFI_Generator> usefulResProcessors = new SortedList<string, IFI_Generator>();
        string[] resources = new string[] { Constants.SLUDGE, Constants.LIFESUPPORT, Constants.SLURRY };

        static bool processorsInitted = false;

        List<AvailablePart> GetPartsList()
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

                if (!processorsInitted)
                {
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
            }
            processorsInitted = true;

            // still need to prevent errors with null refs when looking up these parts though
            if (kerbalEVA != null)
            {
                loadedParts.Remove(kerbalEVA);

            }
            if (flag != null)
            {
                loadedParts.Remove(flag);
            }

#if DEBUG
            Log.Info("GetPartsList, List of IFI parts & resource usage");
            foreach (var a in allResProcessors)
            {
                Log.Info("part: " + a.Value.partName + ", consumesSlurry: " + a.Value.consumesSlurry + ", consumesSludge: " + a.Value.consumesSludge +
                    ", consumesFilteredO2: " + a.Value.consumesFilteredO2 + ", consumesLiquidO2: " + a.Value.consumesLiquidO2);
                foreach (var b in a.Value.resList)
                    Log.Info("Key: " + a.Key + ", part: " + a.Value.partName + ", resource; " + b.Value.resourceName + ", ratio: " + b.Value.RatioPerDay + ", input: " + b.Value.input);
            }
#endif
            return loadedParts;
        }
#endif
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

#if fasle
        EditorPartListFilter<AvailablePart> filter;
#endif

        internal void DefineFilters()
        {
            Log.Info("DefineFilters");
#if false
            return;
            //if (configs == null)
            //    configs = GameDatabase.Instance.GetConfigs("PART");

            //ConfigNode n;

            if (filter != null)
                EditorPartList.Instance.ExcludeFilters.RemoveFilter(filter);
            filter = new EditorPartListFilter<AvailablePart>("Unpurchased Filter", (part => PartShouldBeHidden(part)));
            EditorPartList.Instance.ExcludeFilters.AddFilter(filter);
            EditorPartList.Instance.Refresh();
#endif
        }

        void InitData()
        {
            //if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().Level == IFILS1.LifeSupportLevel.extreme)
            //    return;
            //if (configs == null)
            //    configs = GameDatabase.Instance.GetConfigs("PART");
            //List<AvailablePart> loadedParts =
#if false
            GetPartsList();
#endif
        }

        public void Start()
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<IFILS1>().active)
                return;
            Instance = this;
            Log.Info("Editor.Start");
            InitData();
            DefineFilters();
            GameEvents.onEditorPartPlaced.Add(onEditorPartPlaced);
            GameEvents.onEditorPartDeleted.Add(onEditorPartPlaced);
            GameEvents.onEditorLoad.Add(this.OnShipLoad);
            GameEvents.onEditorStarted.Add(this.OnEditorStarted);
            GameEvents.onEditorPodPicked.Add(this.onEditorPodPicked);
            GameEvents.onEditorShipModified.Add(onEditorShipModified);
            GameEvents.StageManager.OnGUIStageSequenceModified.Add(OnStagingChanged);
            GameEvents.StageManager.OnGUIStageAdded.Add(OnStagingAddedRemoved);
            GameEvents.StageManager.OnGUIStageRemoved.Add(OnStagingAddedRemoved);
        }

        void OnDestroy()
        {
            Log.Info("Editor.Destroy");
            GameEvents.onEditorPartPlaced.Remove(onEditorPartPlaced);
            GameEvents.onEditorPartDeleted.Remove(onEditorPartPlaced);
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
            UpdateLSDisplay();

        }
        void onEditorPartPlaced(Part p)
        {
            Log.Info("onEditorPartPlaced");
            UpdateLSDisplay();
        }

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
