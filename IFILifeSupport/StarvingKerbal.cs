using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;
using ClickThroughFix;
using ToolbarControl_NS;
using SpaceTuxUtility;
using static IFILifeSupport.RegisterToolbar;

namespace IFILifeSupport
{
    public class StarvingKerbal
    {
        const float IFICWLS = .25f;


        public string name;
        public string trait;
        public ProtoCrewMember.KerbalType type;
        public double startTime;

        public ConfigNode ToNode()
        {
            ConfigNode node = new ConfigNode(name);

            node.AddValue("name", name);
            node.AddValue("trait", trait);
            node.AddValue("type", (int)type);
            node.AddValue("startTime", startTime);

            return node;

        }
        public StarvingKerbal(ConfigNode node)
        {
            name = node.SafeLoad("name", "");
            trait = node.SafeLoad("trait", "");
            int t = node.SafeLoad("type", 0);
            type = (ProtoCrewMember.KerbalType)t;
            startTime = node.SafeLoad("startTime", 0f);
        }

        public StarvingKerbal(string name, string trait, ProtoCrewMember.KerbalType type)
        {
            this.name = name;
            this.trait = trait;
            this.type = type;
            startTime = Planetarium.GetUniversalTime();
        }

        public static string ConvertUT(double UT)
        {
            double time = UT;
            int[] ret = { 0, 0, 0, 0, 0 };
            ret[0] = (int)Math.Floor(time / (KSPUtil.dateTimeFormatter.Year)) + 1; //year
            time %= (KSPUtil.dateTimeFormatter.Year);
            ret[1] = (int)Math.Floor(time / (KSPUtil.dateTimeFormatter.Day)) + 1; //days
            time %= (KSPUtil.dateTimeFormatter.Day);
            ret[2] = (int)Math.Floor(time / (KSPUtil.dateTimeFormatter.Hour)); //hours
            time %= (KSPUtil.dateTimeFormatter.Hour);
            ret[3] = (int)Math.Floor(time / (KSPUtil.dateTimeFormatter.Minute)); //minutes
            time %= (KSPUtil.dateTimeFormatter.Minute);
            ret[4] = (int)Math.Floor(time); //seconds

            string s = ret[0].ToString() + "y " +ret[1].ToString()+"d " + ret[2].ToString()+"h " + ret[3].ToString()+"m"  + ret[4].ToString()+"s";
            return s;
        }

        public static void RestoreKerbalToActive(StarvingKerbal sk, ProtoCrewMember iCrew, string flag)
        {
            string message = "";
            string mStr = "Kerbal restored to active status";
            var dt = Planetarium.GetUniversalTime();

            message = iCrew.name + " restored to active status";

            mStr += ": " + flag + " "+ConvertUT(dt); 
            message += ": " + flag + " " + ConvertUT(dt);
            MessageSystem.Message m = new MessageSystem.Message(mStr, message, MessageSystemButton.MessageButtonColor.GREEN, MessageSystemButton.ButtonIcons.MESSAGE);
            MessageSystem.Instance.AddMessage(m);

            iCrew.type = sk.type;
            KerbalRoster.SetExperienceTrait(iCrew, sk.trait);
            ScreenMessages.PostScreenMessage(iCrew.name + " restored to active status", 5);
        }

        public static void SetKerbalToTourist(string vesselName, ProtoCrewMember iCrew, bool eva, string reason)
        {
            string message = "";
            string mStr = "Kerbal transformed into Tourist from LifeSupport System";
            var dt = Planetarium.GetUniversalTime();

            if (vesselName != null)
            {
                message = vesselName + "\n\n";
            }
            if (eva)
            {
                message += "EVA kerbal: ";
                mStr = "EVA Kerbal transformed to Tourist";
            }
            message += iCrew.name + " was turned into a tourist (" + reason + ") due to no Kibbles & Bits remaining";
            message += " " + ConvertUT(dt);
            Log.Warning(message);
            MessageSystem.Message m = new MessageSystem.Message(mStr, message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
            MessageSystem.Instance.AddMessage(m);

            ScreenMessages.PostScreenMessage(iCrew.name + " (" + reason + ") turned into tourist due to no Life Support", 10f);

            StarvingKerbal sk = new StarvingKerbal(iCrew.name, iCrew.trait, iCrew.type);
            LifeSupportUsageUpdate.starvingKerbals.Add(sk.name, sk);
            iCrew.type = ProtoCrewMember.KerbalType.Tourist;
            KerbalRoster.SetExperienceTrait(iCrew, "Tourist");
        }

        public static void ResetCrewEVA(Vessel kerbalVessel)
        {
            Log.Info("ResetCrewEVA");
            Part p = kerbalVessel.rootPart;
            ProtoCrewMember iCrew = p.protoModuleCrew[0];

            StarvingKerbal sk = new StarvingKerbal(iCrew.name, iCrew.trait, iCrew.type);
            Log.Info("Old experienceTrait: " + iCrew.trait);
            if (LifeSupportUsageUpdate.starvingKerbals.ContainsKey(sk.name))
            {
                LifeSupportUsageUpdate.starvingKerbals.Remove(sk.name);

                RestoreKerbalToActive(sk, iCrew, "four");
#if false
                iCrew.type = sk.type;
                KerbalRoster.SetExperienceTrait(iCrew, sk.trait);
                ScreenMessages.PostScreenMessage(iCrew.name + " restored to active status", 5);
#endif
            }
            if (iCrew.type == ProtoCrewMember.KerbalType.Tourist)
            {
                sk.type = ProtoCrewMember.KerbalType.Crew;
                RestoreKerbalToActive(sk, iCrew, "five");
            }
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
                    SetKerbalToTourist(null, iCrew, true, "eva");

#if false
                    Log.Warning(" EVA Kerbal turned into tourist due to no LS - " + iCrew.name);
                    string message = "\n\n\n";
                    message += iCrew.name + ":\n Was turned into a  tourist for Life Support Failure.";
                    MessageSystem.Message m = new MessageSystem.Message("Kerbal transformed to Tourist on EVA", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                    MessageSystem.Instance.AddMessage(m);
                    ScreenMessages.PostScreenMessage(iCrew.name + " (eva) was turned into a  tourist for Life Support Failure.", 10f);

                    Log.Info("Old experienceTrait: " + iCrew.trait);

                    sk = new StarvingKerbal(iCrew.name, iCrew.trait, iCrew.type);
                    LifeSupportUsageUpdate.starvingKerbals.Add(sk.name, sk);
                    iCrew.type = ProtoCrewMember.KerbalType.Tourist;
                    KerbalRoster.SetExperienceTrait(iCrew, "Tourist");
#endif
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
                        Log.Warning(" EVA Kerbal Killed due to no LS - " + iCrew.name);
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

                            Log.Warning(" EVA Kerbal turned into tourist due to no LS - " + iCrew.name);
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

        public static void ResetCrew(ProtoVessel v, ProtoPartSnapshot p)
        {
            Log.Info("ResetCrew (unloaded vessel)");
            foreach (var sk in LifeSupportUsageUpdate.starvingKerbals)
                Log.Info("LifeSupportUsageUpdate.starvingKerbals.name: " + sk.Key);
            foreach (ProtoCrewMember crewMember in HighLogic.CurrentGame.CrewRoster.Tourist)
                Log.Info("HighLogic.CurrentGame.CrewRoster.Tourist.crewMember: " + crewMember.name);


            for (int i = p.protoModuleCrew.Count - 1; i >= 0; i--)
            {
                ProtoCrewMember iCrew = p.protoModuleCrew[i];

                if (LifeSupportUsageUpdate.starvingKerbals.TryGetValue(iCrew.name, out StarvingKerbal sk))
                {
                    LifeSupportUsageUpdate.starvingKerbals.Remove(iCrew.name);

                    StarvingKerbal.RestoreKerbalToActive(sk, iCrew, "six");
                }
            }
        }

        public static void ResetCrew(Vessel v, Part p)
        {
            Log.Info("ResetCrew (loaded vessel)");
            foreach (var sk in LifeSupportUsageUpdate.starvingKerbals)
                Log.Info("LifeSupportUsageUpdate.starvingKerbals.name: " + sk.Key);
            foreach (ProtoCrewMember crewMember in HighLogic.CurrentGame.CrewRoster.Tourist)
                Log.Info("HighLogic.CurrentGame.CrewRoster.Tourist.crewMember: " + crewMember.name);


            for (int i = p.protoModuleCrew.Count - 1; i >= 0; i--)
            {
                ProtoCrewMember iCrew = p.protoModuleCrew[i];
                Log.Info("iCrew.name: " + iCrew.name + ", trait: " + iCrew.trait + ", type: " + iCrew.type);

                if (LifeSupportUsageUpdate.starvingKerbals.TryGetValue(iCrew.name, out StarvingKerbal sk))
                {
                    LifeSupportUsageUpdate.starvingKerbals.Remove(iCrew.name);

                    StarvingKerbal.RestoreKerbalToActive(sk, iCrew, "six");
                }
#if false
                // temporary to fix existing game
                if (iCrew.type == ProtoCrewMember.KerbalType.Tourist)
                {
                     sk = new StarvingKerbal(iCrew.name, iCrew.trait, iCrew.type);
                    sk.type = ProtoCrewMember.KerbalType.Crew;
                    sk.trait = "Pilot";
                    RestoreKerbalToActive(sk, iCrew, "seven");
                }
#endif
            }
        }


        public static void CrewTest(Vessel v, int REASON, Part p, double deathChance)
        {
            float rand;
            ProtoCrewMember iCrew;

            Log.Info("CrewTest, vessel: " + v.vesselName);
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
                    if (UnLockLockedLSTanks(v))
                        return;

                    SetKerbalToTourist(p.vessel.vesselName, iCrew, false, "in a pod 1");
#if false
                    Log.Warning(p.vessel.vesselName + " POD Kerbal turned into tourist due to no LS - " + iCrew.name);
                    string message = ""; message += p.vessel.vesselName + "\n\n"; message += iCrew.name + "\n Was turned into a tourist due to ::";
                    message += "No Kibbles & Bits Remaining";
                    message += "::";
                    MessageSystem.Message m = new MessageSystem.Message("Kerbal transformed into Tourist from LifeSupport System", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                    MessageSystem.Instance.AddMessage(m);
                    ScreenMessages.PostScreenMessage(iCrew.name + " (in a pod 1) turned into tourist due to no Life Support", 10f);

                    sk = new StarvingKerbal(iCrew.name, iCrew.trait, iCrew.type);
                    Log.Info("Old experienceTrait: " + iCrew.trait);
                    LifeSupportUsageUpdate.starvingKerbals.Add(sk.name, sk);

                    iCrew.type = ProtoCrewMember.KerbalType.Tourist;
                    KerbalRoster.SetExperienceTrait(iCrew, "Tourist");
#endif
                }
#endif
                rand = UnityEngine.Random.Range(0.0f, 1f);
                Log.Warning("!!!!!!!!");
                Log.Warning("Testing Crew Death Crewmember=" + p.protoModuleCrew[i].name);
                Log.Warning("Crew Death Chance = " + Convert.ToString(deathChance));
                Log.Warning("Crew Death Roll = " + Convert.ToString(rand));
                Log.Warning("!!!!!!!!");

                if (deathChance > rand)
                {
                    iCrew = p.protoModuleCrew[i];
                    if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().kerbalsCanDie)
                    {
                        p.RemoveCrewmember(iCrew);// Remove crew from part
                        iCrew.Die();  // Kill crew after removal or death will reset to active.
                        Log.Warning(p.vessel.vesselName + " POD Kerbal Killed due to no LS - " + iCrew.name);
                        string message = ""; message += p.vessel.vesselName + "\n\n"; message += iCrew.name + "\n Was killed due to ::";
                        message += "No Kibbles & Bits Remaining";
                        message += "::";
                        MessageSystem.Message m = new MessageSystem.Message("Kerbal Death from LifeSupport System", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                        MessageSystem.Instance.AddMessage(m);
                        ScreenMessages.PostScreenMessage(iCrew.name + " (in a pod 2) was killed for Life Support Failure.", 10f);
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

                            Log.Warning(p.vessel.vesselName + " POD Kerbal turned into tourist due to no LS - " + iCrew.name);
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

        static bool UnLockLockedLSTanks(Vessel vessel)
        {
            for (int i = 0; i < vessel.Parts.Count; i++)
            {
                var p = vessel.Parts[i];
                foreach (var r in p.Resources)
                {
                    if (r.resourceName == Constants.LIFESUPPORT)
                    {
                        if (r.flowState == false)
                        {
                            if (r.amount > 0)
                            {
                                r.flowState = true;

                                MessageSystem.Message m = new MessageSystem.Message("Kibbles & Bits Tank", "Unlocked a Kibbles & Bits tank on " + vessel.vesselName, MessageSystemButton.MessageButtonColor.ORANGE, MessageSystemButton.ButtonIcons.ALERT);
                                MessageSystem.Instance.AddMessage(m);
                                ScreenMessages.PostScreenMessage("Unlocked a Kibbles & Bits tank due to no available Kibbles & Bits on " + vessel.vesselName, 10f);

                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static void CrewTestProto(Vessel v, int REASON, ProtoPartSnapshot p, double deathChance)
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
                    SetKerbalToTourist(p.pVesselRef.vesselName, iCrew, true, "in a pod 3");
#if false
                    Log.Warning(p.pVesselRef.vesselName + " POD Kerbal turned into tourist due to no LS - " + iCrew.name);
                    string message = ""; message += p.pVesselRef.vesselName + "\n\n"; message += iCrew.name + "\n Was turned into a tourist due to ::";
                    message += "No Kibbles & Bits Remaining";
                    message += "::";
                    MessageSystem.Message m = new MessageSystem.Message("Kerbal transformed into Tourist from LifeSupport System", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                    MessageSystem.Instance.AddMessage(m);
                    ScreenMessages.PostScreenMessage(iCrew.name + "(in a pod 3) turned into tourist due to no Life Support", 10f);

                    sk = new StarvingKerbal(iCrew.name, iCrew.trait, iCrew.type);
                    Log.Info("Old experienceTrait: " + iCrew.trait);
                    LifeSupportUsageUpdate.starvingKerbals.Add(sk.name, sk);
                    iCrew.type = ProtoCrewMember.KerbalType.Tourist;
                    KerbalRoster.SetExperienceTrait(iCrew, "Tourist");
#endif
                }
#endif


                rand = UnityEngine.Random.Range(0.0f, 1f);
                Log.Warning("!!!!!!!!");
                Log.Warning("Testing Crew Death Crewmember=" + p.protoModuleCrew[i].name);
                Log.Warning("Crew Death Chance = " + Convert.ToString(deathChance));
                Log.Warning("Crew Death Roll = " + Convert.ToString(rand));
                Log.Warning("!!!!!!!!");

                if (deathChance > rand)
                {
                    iCrew = p.protoModuleCrew[i];
                    if (HighLogic.CurrentGame.Parameters.CustomParams<IFILS2>().kerbalsCanDie)
                    {
                        iCrew.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                        p.RemoveCrew(iCrew);

                        Log.Warning(p.pVesselRef.vesselName + " POD Kerbal Killed due to no LS - " + iCrew.name);
                        string message = ""; message += p.pVesselRef.vesselName + "\n\n"; message += iCrew.name + "\n Was killed due to ::";
                        message += "No Kibbles & Bits Remaining";
                        message += "::";
                        MessageSystem.Message m = new MessageSystem.Message("Kerbal Death from LifeSupport Failure", message, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.ALERT);
                        MessageSystem.Instance.AddMessage(m);
                        ScreenMessages.PostScreenMessage(iCrew.name + " (in a pod 4) killed due to no Kibbles & Bits", 10f);
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

                            Log.Warning(p.pVesselRef.vesselName + " POD Kerbal turned into tourist due to no LS - " + iCrew.name);
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
