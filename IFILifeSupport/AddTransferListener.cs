using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using static IFILifeSupport.RegisterToolbar;

namespace IFILifeSupport
{

    [KSPAddon(KSPAddon.Startup﻿.Flight﻿, false)]
    public class AddTransferListener : MonoBehaviour﻿
    {
        public static EventData<UIPartActionResourceTransfer> onTransferSpawn = new EventData<UIPartActionResourceTransfer>("onTransferSpawn");
        UIPartActionResourceTransfer t;

        private void Start()
        {
            Log.Info("Transfer.Start");

            onTransferSpawn.Add(OnTransferSpawned);

            //This probably only needs to be attached once, but using AddOrGetComponent insures that you don't
            //end up with multiple copies of your listener
            UIPartActionController.Instance.resourceTransferItemPrefab.gameObject.AddOrGetComponent<TransferListener>();
        }

        void OnDestroy()
        {
            Log.Info("Transfer.OnDestroy");
        }
        void DisplayTransferInfo(UIPartActionResourceTransfer t, bool descend = false)
        {
#if false
            Log.Info("\r\n\r\n DisplayTransferInfo, descend: " + descend);
            Log.Info("Transfer.OnTransferSpawned, transfer.lastUT: " + t.lastUT);

            Part p = t.Part;
            PartResource r = t.Resource;
            List<UIPartActionResourceTransfer> targets = t.Targets;
            
            if (t.Part != null)
                Log.Info("transfer.Part: " + t.Part.partInfo.title);
            
            if (t.Resource != null)
                Log.Info("Transfer.Resource: " + t.Resource.resourceName);
            Log.Info("Transfer.state: " + t.state);
            if (t.Part != null)
            {
                double m = r.maxAmount;
                Log.Info("Transfer rate: " + m + " per second");
            }
            foreach (UIPartActionResourceTransfer t1 in targets)
            {
                Log.Info("t: " + t1);
                Log.Info("Transfer.Destination transfer.Part: " + t1.Part.partInfo.title);
                Log.Info("Transfer.Destination transfer.Resource: " + t1.Resource.resourceName);
                if (descend)
                    DisplayTransferInfo(t1);
            }
#endif
        }

        Part source;
        double maxRequested;
        string hiddenResource;
        bool transferInProgress = false;
        double lastUT;

        void GetTransferValues(UIPartActionResourceTransfer transfer)
        {
            maxRequested = 0;
            if (t.state == UIPartActionResourceTransfer.FlowState.In)
            {
                var r = t.Part.Resources[hiddenResource];
                maxRequested += r.maxAmount;
            }
            if (t.state == UIPartActionResourceTransfer.FlowState.Out)
                source = t.Part;

            List<UIPartActionResourceTransfer> targets = t.Targets;
            foreach (UIPartActionResourceTransfer t1 in targets)
            {
                if (t1.state == UIPartActionResourceTransfer.FlowState.In)
                {
                    var r = t1.Part.Resources[hiddenResource];
                    maxRequested += r.maxAmount;
                }
                if (t1.state == UIPartActionResourceTransfer.FlowState.Out)
                    source = t1.Part;
            }
            lastUT = transfer.lastUT;
            maxRequested = maxRequested / 20;
            Log.Info("GetTransferValues, maxRequested/sec: " + maxRequested);
            Log.Info("GetTransferValues, source: " + source.partInfo.title);
        }

        private void OnTransferSpawned(UIPartActionResourceTransfer transfer)
        {
            Log.Info("OnTransferSpawned");
            t = transfer;

            DisplayTransferInfo(t, true);

            //Add your own listeners to the in, out, and stop buttons
            transfer.flowInBtn.onClick.AddListener(OnInClick);
            transfer.flowOutBtn.onClick.AddListener(OnOutClick);
            transfer.flowStopBtn.onClick.AddListener(OnStopClick);          
        }

        bool LifeSupportResource(string resource)
        {
            var b = IFI_Resources.DISPLAY_RESOURCES.Contains(resource);
            Log.Info("Transfer.LifesupportResource, res: " + resource);
            if (b)
            {
                hiddenResource = IFI_Resources.RESOURCES[IFI_Resources.DISPLAY_RESOURCES.IndexOf(resource)];
                Log.Info("Transfer.LifeSupportResource, hiddenResource: " + hiddenResource);
                GetTransferValues(t);
            }
            return b;
        }

        private void OnInClick()
        {
            Log.Info("Transfer.OnInClick, transfer.lastUT: " + t.lastUT);
            DisplayTransferInfo(t, true);
            transferInProgress = LifeSupportResource(t.Resource.resourceName);

        }

        private void OnStopClick()
        {
            Log.Info("Transfer.OnStopClick, transfer.lastUT: " + t.lastUT);
            DisplayTransferInfo(t, true);
            transferInProgress = false;
            t.FlowStop();

        }

        private void OnOutClick()
        {
            Log.Info("Transfer.OnOutClick, transfer.lastUT: " + t.lastUT);
            DisplayTransferInfo(t, true);
            transferInProgress = LifeSupportResource(t.Resource.resourceName);
        }

        void FixedUpdate()
        {
            if (transferInProgress && t != null && t.Resource != null && lastUT != t.lastUT && t.state != UIPartActionResourceTransfer.FlowState.None)
            {
                double timeSlice = t.lastUT - lastUT;
                Log.Info("timeSlice: " + timeSlice);
                double requestAmt = maxRequested * timeSlice;
                double totalTransferred = 0;               

                // Get the available resources for transfer
                double availResForTransfer = source.RequestResource(hiddenResource, requestAmt, ResourceFlowMode.NO_FLOW);

                // now transfer the resources into the new parts.

                if (t.state == UIPartActionResourceTransfer.FlowState.In)
                { 
                    double percentageOfTotal = t.Part.Resources[hiddenResource].maxAmount / 20f * timeSlice / requestAmt;
                    var amtToTransfer = -1 * availResForTransfer * percentageOfTotal;
                    
                    var transferredAmt = t.Part.RequestResource(hiddenResource, amtToTransfer, ResourceFlowMode.NO_FLOW);
                    totalTransferred += transferredAmt;
                    //Log.Info("Transfer 1, after: Part: " + t.Part.partInfo.title + ",  amount: " + t.Part.Resources[hiddenResource].amount + ", transferred: " + totalTransferred);
                }


                List<UIPartActionResourceTransfer> targets = t.Targets;
                foreach (UIPartActionResourceTransfer t1 in targets)
                {
                    if (t1.state == UIPartActionResourceTransfer.FlowState.In)
                    {
                        double percentageOfTotal = t1.Part.Resources[hiddenResource].maxAmount / 20f * timeSlice / requestAmt;

                        var amtToTransfer = -1 * availResForTransfer * percentageOfTotal;
                        var beforeAmt = t1.Part.Resources[hiddenResource].amount;

                        Log.Info("Transfer 2, before, amount: " + t1.Part.Resources[hiddenResource].amount + ", transferred: " + totalTransferred);

                        var transferredAmt = t1.Part.RequestResource(hiddenResource, amtToTransfer, ResourceFlowMode.NO_FLOW);
                        totalTransferred += transferredAmt;
                        //Log.Info("Transfer 2, after:  amount: " + t1.Part.Resources[hiddenResource].amount + ", transferred: " + totalTransferred);
                        var afterAmt = t1.Part.Resources[hiddenResource].amount;
                        Log.Info("Transfer 2, after:  amount: " + afterAmt + ", Logged transfer amt: " + transferredAmt + ", actual diff (after - before): " + (afterAmt - beforeAmt));
                        IFI_Resources.UpdatePartInfo(t1.Part);
                    }
                }

                // Now return the unused resources
                Log.Info("Transfer Returning, availResources: " + availResForTransfer + ", total transferred: " + totalTransferred + ", returning: " + (availResForTransfer + totalTransferred).ToString());
                Log.Info("Transfer, before returning avail resources,  amount: " + source.Resources[hiddenResource].amount);
                source.RequestResource(hiddenResource, -1 * (availResForTransfer + totalTransferred), ResourceFlowMode.NO_FLOW);
                Log.Info("Transfer, after returning avail resources,  amount: " + source.Resources[hiddenResource].amount);
                lastUT = t.lastUT;
               
                if (totalTransferred == 0)
                    OnStopClick();
                IFI_Resources.UpdatePartInfo(t.Part);
                foreach (UIPartActionResourceTransfer t1 in targets)
                    IFI_Resources.UpdatePartInfo(t1.Part);
                
                    Log.Info("Transfer --------------------------------------------------------------------------------------");
            }
            else
            if (transferInProgress)
            {
                if (t != null)
                {
                    if (t.Resource == null)
                        Log.Info("Transfer t.Resource is null");
                    if (lastUT == t.lastUT)
                        Log.Info("Transfer lastUT unchanged, Planetarium.GetUniversalTime: " + Planetarium.GetUniversalTime());
                    if (t.state == UIPartActionResourceTransfer.FlowState.None)
                        Log.Info("Transfer t.state == UIPartActionResourceTransfer.FlowState.None");
                }
                else
                    Log.Info("t is null");
                Log.Info("Transfer --------------------------------------------------------------------------------------");
            }
            
        }
    }

    public class TransferListener : MonoBehaviour
    {
        private void Awake()
        {
            AddTransferListener.onTransferSpawn.Fire(gameObject.GetComponentInParent<UIPartActionResourceTransfer>());
        }
    }
}
