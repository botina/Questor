//------------------------------------------------------------------------------
//  <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//    Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//    Please look in the accompanying license.htm file for the license that
//    applies to this source code. (a copy can also be found at:
//    http://www.thehackerwithin.com/license.htm)
//  </copyright>
//-------------------------------------------------------------------------------

using Questor.Modules.BackgroundTasks;

namespace ValueDump
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;
    using DirectEve;
    using Questor.Modules.Actions;
    using Questor.Modules.Caching;
    using Questor.Modules.Lookup;
    using Questor.Modules.Logging;
    using Questor.Modules.States;

    public partial class ValueDumpUI : Form
    {
        private DateTime _lastPulse;
        private DirectEve _directEve { get; set; }
        public string CharacterName { get; set; }
        private readonly Market _market;

        public ValueDumpUI()
        {
            Logging.Log("ValueDump","Starting ValueDump",Logging.Orange);
            InitializeComponent();
            _market = new Market();

            #region Load DirectEVE
            //
            // Load DirectEVE
            //

            try
            {
                if (Cache.Instance.DirectEve == null)
                {
                    //
                    // DE now has cloaking enabled using easyhook, see forums or IRC for more info, you should use it!
                    // 
                    //
                    //Logging.Log("Startup", "temporarily disabling the loading of DE for debugging purposes, halting", Logging.Debug);
                    //while (Cache.Instance.DirectEve == null)
                    //{
                    //    System.Threading.Thread.Sleep(50); //this pauses forever...
                    //}   
                    Cache.Instance.DirectEve = new DirectEve();
                }
            }
            catch (Exception ex)
            {
                Logging.Log("Valuedump", "Error on Loading DirectEve, maybe server is down", Logging.Orange);
                Logging.Log("Valuedump", string.Format("DirectEVE: Exception {0}...", ex), Logging.White);
                Cache.Instance.CloseQuestorCMDLogoff = false;
                Cache.Instance.CloseQuestorCMDExitGame = true;
                Cache.Instance.CloseQuestorEndProcess = true;
                Settings.Instance.AutoStart = true;
                Cache.Instance.ReasonToStopQuestor = "Error on Loading DirectEve, maybe server is down";
                Cache.Instance.SessionState = "Quitting";
                Cleanup.CloseQuestor(Cache.Instance.ReasonToStopQuestor);
                return;
            }
            #endregion Load DirectEVE

            #region Verify DirectEVE Support Instances
            //
            // Verify DirectEVE Support Instances
            //

            try
            {
                if (Cache.Instance.DirectEve != null && Cache.Instance.DirectEve.HasSupportInstances())
                {
                    Logging.Log("Valuedump", "You have a valid directeve.lic file and have instances available", Logging.Orange);
                }
                else
                {
                    Logging.Log("Valuedump", "You have 0 Support Instances available [ Cache.Instance.DirectEve.HasSupportInstances() is false ]", Logging.Orange);
                }

            }
            catch (Exception exception)
            {
                Logging.Log("Valuedump", "Exception while checking: _directEve.HasSupportInstances() - exception was: [" + exception + "]", Logging.Orange);
            }

            #endregion Verify DirectEVE Support Instances

            try
            {
                Cache.Instance.DirectEve.OnFrame += ValuedumpOnFrame;
            }
            catch (Exception ex)
            {
                Logging.Log("Valuedump", string.Format("DirectEVE.OnFrame: Exception {0}...", ex), Logging.White);
            }
        }

        private void ValuedumpOnFrame(object sender, EventArgs e)
        {
            Cache.Instance.LastFrame = DateTime.UtcNow;

            // Only pulse state changes every .5s
            if (DateTime.UtcNow.Subtract(_lastPulse).TotalMilliseconds < Time.Instance.ValueDumpPulse_milliseconds) //default: 500ms
            {
                return;
            }
            _lastPulse = DateTime.UtcNow;

            // Session is not ready yet, do not continue
            if (!Cache.Instance.DirectEve.Session.IsReady)
            {
                return;
            }

            if (Cache.Instance.DirectEve.Session.IsReady)
            {
                Cache.Instance.LastSessionIsReady = DateTime.UtcNow;
            }

            // We are not in space or station, don't do shit yet!
            if (!Cache.Instance.InSpace && !Cache.Instance.InStation)
            {
                Cache.Instance.NextInSpaceorInStation = DateTime.UtcNow.AddSeconds(12);
                Cache.Instance.LastSessionChange = DateTime.UtcNow;
                return;
            }

            if (DateTime.UtcNow < Cache.Instance.NextInSpaceorInStation)
            {
                return;
            }

            // New frame, invalidate old cache
            Cache.Instance.InvalidateCache();

            // Update settings (settings only load if character name changed)
            if (!Settings.Instance.DefaultSettingsLoaded)
            {
                Settings.Instance.LoadSettings();
            }

            if (DateTime.UtcNow.Subtract(Cache.Instance.LastUpdateOfSessionRunningTime).TotalSeconds < Time.Instance.SessionRunningTimeUpdate_seconds)
            {
                Cache.Instance.SessionRunningTime = (int)DateTime.UtcNow.Subtract(Cache.Instance.QuestorStarted_DateTime).TotalMinutes;
                Cache.Instance.LastUpdateOfSessionRunningTime = DateTime.UtcNow;
            }

            if (_States.CurrentValueDumpState == ValueDumpState.Idle)
            {
                return;
            }

            ProcessState();
        }

        public void ProcessState()
        {
            switch (_States.CurrentValueDumpState)
            {
                case ValueDumpState.Done:
                    _States.CurrentValueDumpState = ValueDumpState.Idle;
                    break;

                case ValueDumpState.Idle:
                    break;

                case ValueDumpState.CheckMineralPrices:
                    if (Settings.Instance.DebugValuedump) Logging.Log("ValueDump", "case ValueDumpState.CheckMineralPrices:", Logging.Debug);
                    if (!Market.CheckMineralPrices("ValueDump", RefineCheckBox.Checked)) return;
                    _States.CurrentValueDumpState = ValueDumpState.SaveMineralPrices;
                    break;

                case ValueDumpState.SaveMineralPrices:
                    if (Settings.Instance.DebugValuedump) Logging.Log("ValueDump", "case ValueDumpState.SaveMineralPrices:", Logging.Debug);
                    if (!Market.SaveMineralprices("ValueDump")) return;
                    _States.CurrentValueDumpState = ValueDumpState.Idle;    
                    break;

                case ValueDumpState.GetItems:
                    if (Settings.Instance.DebugValuedump) Logging.Log("ValueDump", "case ValueDumpState.GetItems:", Logging.Debug);
                    if (!Cache.Instance.OpenItemsHangar("ValueDump")) break;
                    Logging.Log("ValueDump", "Loading hangar items", Logging.White);

                    // Clear out the old
                    Market.Items.Clear();
                    List<DirectItem> hangarItems = Cache.Instance.ItemHangar.Items;
                    if (Cache.Instance.ItemHangar.Items != null && Cache.Instance.ItemHangar.Items.Any())
                    {
                        Market.Items.AddRange(hangarItems.Where(i => i.ItemId > 0 && i.Quantity > 0).Select(i => new ItemCacheMarket(i, RefineCheckBox.Checked)));
                    }

                    _States.CurrentValueDumpState = ValueDumpState.UpdatePrices;
                    break;

                case ValueDumpState.UpdatePrices:
                    if (Settings.Instance.DebugValuedump) Logging.Log("ValueDump", "case ValueDumpState.UpdatePrices:", Logging.Debug);
                    if (!Market.UpdatePrices("ValueDump", cbxSell.Checked, RefineCheckBox.Checked, cbxUndersell.Checked)) return;
                    //
                    // we are out of items
                    //
                    _States.CurrentValueDumpState = ValueDumpState.Idle;
                    break;

                case ValueDumpState.NextItem:
                    if (Settings.Instance.DebugValuedump) Logging.Log("ValueDump", "case ValueDumpState.NextItem:", Logging.Debug);
                    if (!Market.NextItem("ValueDump")) return;
                    _States.CurrentValueDumpState = ValueDumpState.StartQuickSell;
                    break;

                case ValueDumpState.StartQuickSell:
                    if (Settings.Instance.DebugValuedump) Logging.Log("ValueDump", "case ValueDumpState.StartQuickSell:", Logging.Debug);
                    if (!Market.StartQuickSell("ValueDump", cbxSell.Checked)) return;
                    _States.CurrentValueDumpState = ValueDumpState.InspectOrder;
                    break;

                case ValueDumpState.InspectOrder:
                    if (Settings.Instance.DebugValuedump) Logging.Log("ValueDump", "case ValueDumpState.InspectOrder:", Logging.Debug);
                    if (!Market.Inspectorder("ValueDump", cbxSell.Checked, RefineCheckBox.Checked, cbxUndersell.Checked, (double)RefineEfficiencyInput.Value)) return;
                    _States.CurrentValueDumpState = ValueDumpState.WaitingToFinishQuickSell;
                    break;

                case ValueDumpState.InspectRefinery:
                    if (Settings.Instance.DebugValuedump) Logging.Log("ValueDump", "case ValueDumpState.InspectRefinery:", Logging.Debug);
                    if (!Market.InspectRefinery("ValueDump", (double)RefineEfficiencyInput.Value)) return;
                    _States.CurrentValueDumpState = ValueDumpState.NextItem;
                    break;

                case ValueDumpState.WaitingToFinishQuickSell:
                    if (Settings.Instance.DebugValuedump) Logging.Log("ValueDump", "case ValueDumpState.WaitingToFinishQuickSell:", Logging.Debug);
                    if (!Market.WaitingToFinishQuickSell("ValueDump")) return;
                    _States.CurrentValueDumpState = ValueDumpState.NextItem;
                    break;

                case ValueDumpState.RefineItems:
                    if (Settings.Instance.DebugValuedump) Logging.Log("ValueDump", "case ValueDumpState.RefineItems:", Logging.Debug);
                    if (Market.RefineItems("ValueDump", RefineCheckBox.Checked)) return;
                    _States.CurrentValueDumpState = ValueDumpState.Idle;
                    break;
            }
        }

        private void BtnHangarClick(object sender, EventArgs e)
        {
            _States.CurrentValueDumpState = ValueDumpState.GetItems;
            ProcessItems(cbxSell.Checked);
        }

        private void ProcessItems(bool sell)
        {
            // Wait for the items to load
            Logging.Log("ValueDump", "Waiting for items", Logging.White);
            while (_States.CurrentValueDumpState != ValueDumpState.Idle)
            {
                System.Threading.Thread.Sleep(50);
                Application.DoEvents();
            }

            lvItems.Items.Clear();

            if (Market.Items.Any())
            {
                foreach (ItemCacheMarket item in Market.Items.Where(i => i.InvType != null).OrderByDescending(i => i.InvType.MedianBuy * i.Quantity))
                {
                    ListViewItem listItem = new ListViewItem(item.Name);
                    listItem.SubItems.Add(string.Format("{0:#,##0}", item.Quantity));
                    listItem.SubItems.Add(string.Format("{0:#,##0}", item.QuantitySold));
                    listItem.SubItems.Add(string.Format("{0:#,##0}", item.InvType.MedianBuy));
                    listItem.SubItems.Add(string.Format("{0:#,##0}", item.StationBuy));

                    if (sell)
                    {
                        listItem.SubItems.Add(string.Format("{0:#,##0}", item.StationBuy * item.QuantitySold));
                    }
                    else
                    {
                        listItem.SubItems.Add(string.Format("{0:#,##0}", item.InvType.MedianBuy * item.Quantity));
                    }

                    lvItems.Items.Add(listItem);
                }

                if (sell)
                {
                    tbTotalMedian.Text = string.Format("{0:#,##0}", Market.Items.Where(i => i.InvType != null).Sum(i => i.InvType.MedianBuy * i.QuantitySold));
                    tbTotalSold.Text = string.Format("{0:#,##0}", Market.Items.Sum(i => i.StationBuy * i.QuantitySold));
                }
                else
                {
                    tbTotalMedian.Text = string.Format("{0:#,##0}", Market.Items.Where(i => i.InvType != null).Sum(i => i.InvType.MedianBuy * i.Quantity));
                    tbTotalSold.Text = "";
                }
            } 
        }

        private void ValueDumpUIFormClosed(object sender, FormClosedEventArgs e)
        {
            Cache.Instance.DirectEve.Dispose();
            Cache.Instance.DirectEve = null;
        }

        private void BtnStopClick(object sender, EventArgs e)
        {
            _States.CurrentValueDumpState = ValueDumpState.Idle;
        }

        private void UpdateMineralPricesButtonClick(object sender, EventArgs e)
        {
            _States.CurrentValueDumpState = ValueDumpState.CheckMineralPrices;
        }

        private void LvItemsColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListViewColumnSort oCompare = new ListViewColumnSort();

            if (lvItems.Sorting == SortOrder.Ascending)
                oCompare.Sorting = SortOrder.Descending;
            else
                oCompare.Sorting = SortOrder.Ascending;
            lvItems.Sorting = oCompare.Sorting;
            oCompare.ColumnIndex = e.Column;

            switch (e.Column)
            {
                case 1:
                    oCompare.CompararPor = ListViewColumnSort.TipoCompare.Cadena;
                    break;

                case 2:
                    oCompare.CompararPor = ListViewColumnSort.TipoCompare.Numero;
                    break;

                case 3:
                    oCompare.CompararPor = ListViewColumnSort.TipoCompare.Numero;
                    break;

                case 4:
                    oCompare.CompararPor = ListViewColumnSort.TipoCompare.Numero;
                    break;

                case 5:
                    oCompare.CompararPor = ListViewColumnSort.TipoCompare.Numero;
                    break;

                case 6:
                    oCompare.CompararPor = ListViewColumnSort.TipoCompare.Numero;
                    break;
            }

            lvItems.ListViewItemSorter = oCompare;
        }

        private void ValueDumpUILoad(object sender, EventArgs e)
        {
        }

        private void LvItemsSelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void RefineCheckBoxCheckedChanged(object sender, EventArgs e)
        {
        }
    }
}