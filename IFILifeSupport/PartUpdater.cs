using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

using static IFILifeSupport.RegisterToolbar;


namespace IFILifeSupport
{
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

        void Start()
        {
            IFI_Parts.GetPartsList();
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
                if (Editor.PartShouldBeHidden(p, out node))
                {
                    Log.Info("Hiding name: " + p.name + ",   " + p.TechRequired);
                    p.TechRequired = "none";
                    p.TechHidden = true;
                }
                else
                {
                    if (node != null)
                    {
                        string techRequired = node.GetValue("TechRequired");

                        p.TechRequired = techRequired;
                        p.TechHidden = false;
                        Log.Info("Restoring name: " + p.name + ",   " + p.TechRequired);
                    }
                }
            }
        }
    }
}
