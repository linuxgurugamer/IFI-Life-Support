using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP;
using KSP.UI.Screens;
using KSP.UI;

using UnityEngine.SceneManagement;


namespace IFILifeSupport
{

    public class IFI_Improved : PartModule
    {
        void Start()
        {
            Log.Info("IFI_Improved");
        }
    }
    public class IFI_Advanced : PartModule
    {
        void Start()
        {
            Log.Info("IFI_Advanced");
        }
    }
    public class IFI_Extreme : PartModule
    {
        void Start()
        {
            Log.Info("IFI_Advanced");
        }
    }

    /// <summary>
    /// Need to keep it around always since going into settings and out doesn't seem to be a new scene
    /// </summary>
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class PartUpdater : MonoBehaviour
    {
        void Awake()
        {
            OnGameSettingsApplied();
            GameEvents.OnGameSettingsApplied.Add(OnGameSettingsApplied);
            DontDestroyOnLoad(this);
        }

        void Destroy()
        {
            GameEvents.OnGameSettingsApplied.Remove(OnGameSettingsApplied);
        }

        /// <summary>
        /// Examine the loaded parts, changing the TechRequired as necessary to hide or show the parts in the tech tree
        /// Similar code is used in the editor, to hide or show the parts there
        /// </summary>
        void OnGameSettingsApplied()
        {
            if (PartLoader.LoadedPartsList == null)
                return;
            Log.Info("PartUpdater.OnGameSettingsApplied, LoadedPartsList.Count: " + PartLoader.LoadedPartsList.Count);
            for (int i = 0; i < PartLoader.LoadedPartsList.Count; i++)
            {
                AvailablePart p = PartLoader.LoadedPartsList[i];

                ConfigNode node;
                if (!Editor.PartShouldBeHidden(p, out node))
                {
                    Log.Info("Hiding name: " + p.name + ",   " + p.TechRequired);
                    p.TechRequired = "none";
                }
                else
                {
                    if (node != null)
                    {
                        string techRequired = node.GetValue("TechRequired");

                        p.TechRequired = techRequired;
                        Log.Info("Restoring name: " + p.name + ",   " + p.TechRequired);
                    }
                }
            }
        }
    }

}
