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
    public class StarvingKerbal
    {
        const float IFICWLS = .25f;


        public string name;
        public string trait;
        public double startTime;

        public StarvingKerbal(string n, string t)
        {
            name = n;
            trait = t;
            startTime = Planetarium.GetUniversalTime();
        }


        public static void CrewTestEVA(Vessel kerbalVessel, double CUR_CWLS)
        {
            Log.Info("CrewTestEVA");
            if (!HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().dieDuringTimewarp && TimeWarp.CurrentRate > 1)
                return;

            StarvingKerbal sk;
            if (LifeSupportUsageUpdate.starvingKerbals.TryGetValue(kerbalVessel.rootPart.protoModuleCrew[0].name, out sk))
            {
                if (sk.startTime + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().InactiveTimeBeforeDeathSecs > Planetarium.GetUniversalTime())
                {
                    return;
                }
            }
            else
            {
                if (kerbalVessel.loaded)
                {
                    Part p = kerbalVessel.rootPart;
                    ProtoCrewMember iCrew = p.protoModuleCrew[0];

                    sk = new StarvingKerbal(iCrew.name, iCrew.trait);
                    IFIDebug.IFIMess(" EVA Kerbal turned into tourist due to no LS - " + iCrew.name);
                    string message = "\n\n\n";
                    message += iCrew.name + ":\n Was turned into a  tourist for Life Support Failure.";
                    MessageSystem.Message m = new MessageSystem.Message("Kerbal transformed to Tourist on EVA", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                    MessageSystem.Instance.AddMessage(m);
                    ScreenMessages.PostScreenMessage(iCrew.name + " (eva) was turned into a  tourist for Life Support Failure.", 10f);

                    Log.Info("Old experienceTrait: " + iCrew.trait);
                    LifeSupportUsageUpdate.starvingKerbals.Add(sk.name, sk);
                    iCrew.type = ProtoCrewMember.KerbalType.Tourist;
                    KerbalRoster.SetExperienceTrait(iCrew, "Tourist");
                }
            }

            float rand;
            //int CUR_CWLS = IFICWLS;
           
            rand = UnityEngine.Random.Range(0.0f, 1f);
            if (CUR_CWLS > rand)
            {
                if (kerbalVessel.loaded)
                {
                    Part p = kerbalVessel.rootPart;
                    ProtoCrewMember iCrew = p.protoModuleCrew[0];

                    if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().EVAkerbalsCanDie)
                    {
                        iCrew.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                        p.Die();
                        IFIDebug.IFIMess(" EVA Kerbal Killed due to no LS - " + iCrew.name);
                        string message = "\n\n\n";
                        message += iCrew.name + ":\n Was killed for Life Support Failure.";
                        MessageSystem.Message m = new MessageSystem.Message("Kerbal Death on EVA", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                        MessageSystem.Instance.AddMessage(m);
                        ScreenMessages.PostScreenMessage(iCrew.name + " (eva) was killed for Life Support Failure.", 10f);
                    }
#if false
                    else
                    {
                        StarvingKerbal sk;
                        if (!LifeSupportUpdate.starvingKerbals.TryGetValue(iCrew.name, out sk))
                        {
                            sk = new StarvingKerbal(iCrew.name, iCrew.trait);

                            IFIDebug.IFIMess(" EVA Kerbal turned into tourist due to no LS - " + iCrew.name);
                            string message = "\n\n\n";
                            message += iCrew.name + ":\n Was turned into a  tourist for Life Support Failure.";
                            MessageSystem.Message m = new MessageSystem.Message("Kerbal transformed to Tourist on EVA", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                            MessageSystem.Instance.AddMessage(m);
                            Log.Info("Old experienceTrait: " + iCrew.trait);
                            LifeSupportUpdate.starvingKerbals.Add(sk.name, sk);
                            iCrew.type = ProtoCrewMember.KerbalType.Tourist;
                            KerbalRoster.SetExperienceTrait(iCrew, "Tourist");
                        }
                    }
#endif
                }
                else
                {
                    // Removed Killing Kerbals on EVA when not loaded to fix ghosting bug. 
                }
            }

        }

        public static void CrewTest(int REASON, Part p, double deathChance           )
        {
            float rand;
            ProtoCrewMember iCrew;

            if (!HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().dieDuringTimewarp && TimeWarp.CurrentRate > 1)
                return;

            for (int i = 0; i < p.protoModuleCrew.Count; i++)
            {
#if true
                iCrew = p.protoModuleCrew[i];
                StarvingKerbal sk;
                if (LifeSupportUsageUpdate.starvingKerbals.TryGetValue(iCrew.name, out sk))
                {
                    if (sk.startTime + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().InactiveTimeBeforeDeathSecs > Planetarium.GetUniversalTime())
                    {
                        return;
                    }
                }
                else
                {
                    sk = new StarvingKerbal(iCrew.name, iCrew.trait);
                    Log.Info("Old experienceTrait: " + iCrew.trait);
                    LifeSupportUsageUpdate.starvingKerbals.Add(sk.name, sk);
                    iCrew.type = ProtoCrewMember.KerbalType.Tourist;
                    KerbalRoster.SetExperienceTrait(iCrew, "Tourist");
                    IFIDebug.IFIMess(p.vessel.vesselName + " POD Kerbal turned into tourist due to no LS - " + iCrew.name);
                    string message = ""; message += p.vessel.vesselName + "\n\n"; message += iCrew.name + "\n Was turned into a tourist due to ::";
                    message += "No Kibbles & Bits Remaining";
                    message += "::";
                    MessageSystem.Message m = new MessageSystem.Message("Kerbal transformed into Tourist from LifeSupport System", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                    MessageSystem.Instance.AddMessage(m);
                    ScreenMessages.PostScreenMessage(iCrew.name + " (in a pod) turned into tourist due to no Life Support", 10f);
                }
#endif
                rand = UnityEngine.Random.Range(0.0f, 1f);
                IFIDebug.IFIMess("!!!!!!!!");
                IFIDebug.IFIMess("Testing Crew Death Crewmember=" + p.protoModuleCrew[i].name);
                IFIDebug.IFIMess("Crew Death Chance = " + Convert.ToString(deathChance));
                IFIDebug.IFIMess("Crew Death Roll = " + Convert.ToString(rand));
                IFIDebug.IFIMess("!!!!!!!!");

                if (deathChance > rand)
                {
                    iCrew = p.protoModuleCrew[i];
                    if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().kerbalsCanDie)
                    {
                        p.RemoveCrewmember(iCrew);// Remove crew from part
                        iCrew.Die();  // Kill crew after removal or death will reset to active.
                        IFIDebug.IFIMess(p.vessel.vesselName + " POD Kerbal Killed due to no LS - " + iCrew.name);
                        string message = ""; message += p.vessel.vesselName + "\n\n"; message += iCrew.name + "\n Was killed due to ::";
                        message += "No Kibbles & Bits Remaining";
                        message += "::";
                        MessageSystem.Message m = new MessageSystem.Message("Kerbal Death from LifeSupport System", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                        MessageSystem.Instance.AddMessage(m);
                        ScreenMessages.PostScreenMessage(iCrew.name + " (in a pod) was killed for Life Support Failure.", 10f);
                    }
#if false
                    else
                    {
                        StarvingKerbal sk;
                        if (!LifeSupportUpdate.starvingKerbals.TryGetValue(iCrew.name, out sk))
                        {
                            sk = new StarvingKerbal(iCrew.name, iCrew.trait);
                            Log.Info("Old experienceTrait: " + iCrew.trait);
                            LifeSupportUpdate.starvingKerbals.Add(sk.name, sk);
                            iCrew.type = ProtoCrewMember.KerbalType.Tourist;
                            KerbalRoster.SetExperienceTrait(iCrew, "Tourist");

                            IFIDebug.IFIMess(p.vessel.vesselName + " POD Kerbal turned into tourist due to no LS - " + iCrew.name);
                            string message = ""; message += p.vessel.vesselName + "\n\n"; message += iCrew.name + "\n Was turned into a tourist due to ::";
                            message += "No Life Support Remaining";
                            message += "::";
                            MessageSystem.Message m = new MessageSystem.Message("Kerbal transformed into Tourist from LifeSupport System", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                            MessageSystem.Instance.AddMessage(m);
                        }
                    }
#endif
                }
            }
        }

        public static void CrewTestProto(int REASON, ProtoPartSnapshot p, double deathChance)
        {
            float rand;

            Log.Info("CrewTestProto");
            ProtoCrewMember iCrew;
            for (int i = 0; i < p.protoModuleCrew.Count; i++)
            {

#if true
                iCrew = p.protoModuleCrew[i];
                Log.Info("CrewTestProto, crew: " + iCrew.name);
                StarvingKerbal sk;
                if (LifeSupportUsageUpdate.starvingKerbals.TryGetValue(iCrew.name, out sk))
                {
                    if (sk.startTime + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().InactiveTimeBeforeDeathSecs > Planetarium.GetUniversalTime())
                    {
                        return;
                    }
                }
                else
                {
                    sk = new StarvingKerbal(iCrew.name, iCrew.trait);
                    Log.Info("Old experienceTrait: " + iCrew.trait);
                    LifeSupportUsageUpdate.starvingKerbals.Add(sk.name, sk);
                    iCrew.type = ProtoCrewMember.KerbalType.Tourist;
                    KerbalRoster.SetExperienceTrait(iCrew, "Tourist");
                    IFIDebug.IFIMess(p.pVesselRef.vesselName + " POD Kerbal turned into tourist due to no LS - " + iCrew.name);
                    string message = ""; message += p.pVesselRef.vesselName + "\n\n"; message += iCrew.name + "\n Was turned into a tourist due to ::";
                    message += "No Kibbles & Bits Remaining";
                    message += "::";
                    MessageSystem.Message m = new MessageSystem.Message("Kerbal transformed into Tourist from LifeSupport System", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                    MessageSystem.Instance.AddMessage(m);
                    ScreenMessages.PostScreenMessage(iCrew.name + "(in a pod) turned into tourist due to no Life Support", 10f);
                }
#endif


                rand = UnityEngine.Random.Range(0.0f, 1f);
                IFIDebug.IFIMess("!!!!!!!!");
                IFIDebug.IFIMess("Testing Crew Death Crewmember=" + p.protoModuleCrew[i].name);
                IFIDebug.IFIMess("Crew Death Chance = " + Convert.ToString(deathChance));
                IFIDebug.IFIMess("Crew Death Roll = " + Convert.ToString(rand));
                IFIDebug.IFIMess("!!!!!!!!");

                if (deathChance > rand)
                {
                    iCrew = p.protoModuleCrew[i];
                    if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().kerbalsCanDie)
                    {
                        iCrew.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                        p.RemoveCrew(iCrew);

                        IFIDebug.IFIMess(p.pVesselRef.vesselName + " POD Kerbal Killed due to no LS - " + iCrew.name);
                        string message = ""; message += p.pVesselRef.vesselName + "\n\n"; message += iCrew.name + "\n Was killed due to ::";
                        message += "No Kibbles & Bits Remaining";
                        message += "::";
                        MessageSystem.Message m = new MessageSystem.Message("Kerbal Death from LifeSupport Failure", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                        MessageSystem.Instance.AddMessage(m);
                        ScreenMessages.PostScreenMessage(iCrew.name + " (in a pod) killed due to no Kibbles & Bits", 10f);
                    }
#if false
                    else
                    {
                        StarvingKerbal sk;
                        if (!LifeSupportUpdate.starvingKerbals.TryGetValue(iCrew.name, out sk))
                        {
                            sk = new StarvingKerbal(iCrew.name, iCrew.trait);
                            Log.Info("Old experienceTrait: " + iCrew.trait);
                            LifeSupportUpdate.starvingKerbals.Add(sk.name, sk);
                            iCrew.type = ProtoCrewMember.KerbalType.Tourist;
                            KerbalRoster.SetExperienceTrait(iCrew, "Tourist");

                            IFIDebug.IFIMess(p.pVesselRef.vesselName + " POD Kerbal turned into tourist due to no LS - " + iCrew.name);
                            string message = ""; message += p.pVesselRef.vesselName + "\n\n"; message += iCrew.name + "\n Was turned into a tourist due to ::";
                            message += "No Life Support Remaining";
                            message += "::";
                            MessageSystem.Message m = new MessageSystem.Message("Kerbal transformed into Tourist from LifeSupport System", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                            MessageSystem.Instance.AddMessage(m);
                        }
                    }
#endif
                }
            }
        }

    }


}
