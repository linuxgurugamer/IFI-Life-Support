using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace IFILifeSupport
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames | ScenarioCreationOptions.AddToExistingCareerGames,
        GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class Scenario : ScenarioModule
    {
        const string IFILS = "IFILS";
        const string TIMER = "TIMER";

        public override void OnSave(ConfigNode parentNode)
        {
            ConfigNode node = new ConfigNode(IFILS);

            foreach (var t in LifeSupportUsageUpdate.starvingKerbals)
            {
                node.AddNode(t.Value.ToNode());
            }
            parentNode.AddNode(node);
            base.OnSave(parentNode);
        }

        public override void OnLoad(ConfigNode parentNode)
        {
            base.OnLoad(parentNode);

            ConfigNode node = parentNode.GetNode(IFILS);

            if (node != null)
            {
                ConfigNode[] nodes = node.GetNodes();

                foreach (var n in nodes)
                {
                    StarvingKerbal sk = new StarvingKerbal(node);
                    if (!LifeSupportUsageUpdate.starvingKerbals.ContainsKey(sk.name))
                        LifeSupportUsageUpdate.starvingKerbals.Add(sk.name, sk);
                }
            }
        }
    }
}
