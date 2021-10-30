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
        const int IFICWLS = 25;


        public string name;
        public string trait;
        public double startTime;

        public StarvingKerbal(string n, string t)
        {
            name = n;
            trait = t;
            startTime = Planetarium.GetUniversalTime();
        }


        public static void CrewTestEVA(Vessel IV, double l)
        {
#if true
            StarvingKerbal sk;
            if (LifeSupportUpdate.starvingKerbals.TryGetValue(IV.rootPart.protoModuleCrew[0].name, out sk))
            {
                if (sk.startTime + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().inactiveTimeBeforeDeath > Planetarium.GetUniversalTime())
                {
                    return;
                }
            }
            else
            {
                if (IV.loaded)
                {
                    Part p = IV.rootPart;
                    ProtoCrewMember iCrew = p.protoModuleCrew[0];

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
            float rand;
            int CUR_CWLS = IFICWLS;
            CUR_CWLS += (Convert.ToInt32(l) * 10);
            rand = UnityEngine.Random.Range(0.0f, 100.0f);
            if (CUR_CWLS > rand)
            {
                if (IV.loaded)
                {
                    Part p = IV.rootPart;
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

        public static void CrewTest(int REASON, Part p, double l)
        {
            int CUR_CWLS = IFICWLS;
            CUR_CWLS += (Convert.ToInt32(l) * 10);
            float rand;
            ProtoCrewMember iCrew;
            for (int i = 0; i < p.protoModuleCrew.Count; i++)
            {
#if true
                iCrew = p.protoModuleCrew[i];
                StarvingKerbal sk;
                if (LifeSupportUpdate.starvingKerbals.TryGetValue(iCrew.name, out sk))
                {
                    if (sk.startTime + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().inactiveTimeBeforeDeath > Planetarium.GetUniversalTime())
                    {
                        return;
                    }
                }
                else
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
#endif
                rand = UnityEngine.Random.Range(0.0f, 100.0f);
                IFIDebug.IFIMess("!!!!!!!!");
                IFIDebug.IFIMess("Testing Crew Death Crewmember=" + p.protoModuleCrew[i].name);
                IFIDebug.IFIMess("Crew Death Chance = " + Convert.ToString(CUR_CWLS));
                IFIDebug.IFIMess("Crew Death Roll = " + Convert.ToString(rand));
                IFIDebug.IFIMess("!!!!!!!!");

                if (CUR_CWLS > rand)
                {
                    iCrew = p.protoModuleCrew[i];
                    if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().kerbalsCanDie)
                    {
                        p.RemoveCrewmember(iCrew);// Remove crew from part
                        iCrew.Die();  // Kill crew after removal or death will reset to active.
                        IFIDebug.IFIMess(p.vessel.vesselName + " POD Kerbal Killed due to no LS - " + iCrew.name);
                        string message = ""; message += p.vessel.vesselName + "\n\n"; message += iCrew.name + "\n Was killed due to ::";
                        message += "No Life Support Remaining";
                        message += "::";
                        MessageSystem.Message m = new MessageSystem.Message("Kerbal Death from LifeSupport System", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                        MessageSystem.Instance.AddMessage(m);
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

        public static void CrewTestProto(int REASON, ProtoPartSnapshot p, double l)
        {

            int CUR_CWLS = IFICWLS;
            CUR_CWLS += (Convert.ToInt32(l) * 10);
            float rand;

            ProtoCrewMember iCrew;
            for (int i = 0; i < p.protoModuleCrew.Count; i++)
            {

#if true
                iCrew = p.protoModuleCrew[i];
                StarvingKerbal sk;
                if (LifeSupportUpdate.starvingKerbals.TryGetValue(iCrew.name, out sk))
                {
                    if (sk.startTime + HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().inactiveTimeBeforeDeath > Planetarium.GetUniversalTime())
                    {
                        return;
                    }
                }
                else
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
#endif


                rand = UnityEngine.Random.Range(0.0f, 100.0f);
                IFIDebug.IFIMess("!!!!!!!!");
                IFIDebug.IFIMess("Testing Crew Death Crewmember=" + p.protoModuleCrew[i].name);
                IFIDebug.IFIMess("Crew Death Chance = " + Convert.ToString(CUR_CWLS));
                IFIDebug.IFIMess("Crew Death Roll = " + Convert.ToString(rand));
                IFIDebug.IFIMess("!!!!!!!!");

                if (CUR_CWLS > rand)
                {
                    iCrew = p.protoModuleCrew[i];
                    if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().kerbalsCanDie)
                    {
                        iCrew.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                        p.RemoveCrew(iCrew);

                        IFIDebug.IFIMess(p.pVesselRef.vesselName + " POD Kerbal Killed due to no LS - " + iCrew.name);
                        string message = ""; message += p.pVesselRef.vesselName + "\n\n"; message += iCrew.name + "\n Was killed due to ::";
                        message += "No Life Support Remaining";
                        message += "::";
                        MessageSystem.Message m = new MessageSystem.Message("Kerbal Death from LifeSupport Failure", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                        MessageSystem.Instance.AddMessage(m);
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
