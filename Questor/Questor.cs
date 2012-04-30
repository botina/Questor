﻿// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using Questor.Behaviors;


namespace Questor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using DirectEve;
    using global::Questor.Modules.Caching;
    using global::Questor.Modules.Logging;
    using global::Questor.Modules.Lookup;
    using global::Questor.Modules.States;
    using global::Questor.Modules.BackgroundTasks;
    using LavishScriptAPI;

    public class Questor
    {
        private readonly QuestorfrmMain m_Parent;
        private readonly LocalWatch _localwatch;
        private readonly Defense _defense;
        private readonly DirectEve _directEve;
        
        private DateTime _lastPulse;
        private DateTime _lastSalvageTrip = DateTime.MinValue;
        private readonly CombatMissionsBehavior _combatMissionsBehavior;
        private readonly Cleanup _cleanup;
        
        public DateTime LastFrame;
        public DateTime LastAction;
        //private readonly Random _random;
        //private int _randomDelay;
        //public static long AgentID;

        //private double _lastX;
        //private double _lastY;
        //private double _lastZ;
        //private bool _firstStart = true;
        public bool Panicstatereset = false;

        //DateTime _nextAction = DateTime.Now;
        private readonly Stopwatch _watch;

        public Questor(QuestorfrmMain form1)
        {
            m_Parent = form1;
            _lastPulse = DateTime.MinValue;

            _defense = new Defense();
            _localwatch = new LocalWatch();
            _combatMissionsBehavior = new CombatMissionsBehavior();
            _cleanup = new Cleanup();
            _watch = new Stopwatch();
            
            // State fixed on ExecuteMission
            _States.CurrentQuestorState = QuestorState.Idle;

            _directEve = new DirectEve();
            Cache.Instance.DirectEve = _directEve;

            Cache.Instance.StopTimeSpecified = Program.StopTimeSpecified;
            Cache.Instance.MaxRuntime = Program.MaxRuntime;
            Cache.Instance.StopTime = Program.StopTime;
            Cache.Instance.StartTime = Program.startTime;
            Cache.Instance.QuestorStarted_DateTime = DateTime.Now;
            
            //foreach (string state in Enum.GetNames(typeof(CombatMissionsBehaviorState)))
            //    QuestorfrmMain.MainBehaviorComboBox.Items.Add(state);
            //QuestorfrmMain.lblQuestorState.Text = "CombatMissionsBehaviorState";
            //CombatMissionsBehavior.State = CombatMissionsBehaviorState.Idle;

            
            // get the current process
            Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            // get the physical mem usage
            Cache.Instance.TotalMegaBytesOfMemoryUsed = ((currentProcess.WorkingSet64 / 1024) / 1024);
            Logging.Log("Questor: EVE instance: totalMegaBytesOfMemoryUsed - " +
                        Cache.Instance.TotalMegaBytesOfMemoryUsed + " MB");
            Cache.Instance.SessionIskGenerated = 0;
            Cache.Instance.SessionLootGenerated = 0;
            Cache.Instance.SessionLPGenerated = 0;

            _directEve.OnFrame += OnFrame;
        }

        private bool _closeQuestorCMDUplink = true;
        public bool CloseQuestorflag = true;
        private DateTime CloseQuestorDelay { get; set; }
        private bool _closeQuestor10SecWarningDone = false;
        public string CharacterName { get; set; }
        public static long AgentID;

        public void DebugCombatMissionsBehaviorStates()
        {
            if (Settings.Instance.DebugStates)
                Logging.Log("CombatMissionsBehavior.State = " + _States.CurrentQuestorState);
        }

        //public void DebugPanicstates()
        //{
        //    if (Settings.Instance.DebugStates)
        //        Logging.Log("Panic.State = " + _panic.State);
        //    }

        public void DebugPerformanceClearandStartTimer()
        {
            _watch.Reset();
            _watch.Start();
        }

        public void DebugPerformanceStopandDisplayTimer(string whatWeAreTiming)
        {
            _watch.Stop();
            if (Settings.Instance.DebugPerformance)
                Logging.Log(whatWeAreTiming + " took " + _watch.ElapsedMilliseconds + "ms");
        }

        public static void BeginClosingQuestor()
        {
            Cache.Instance.EnteredCloseQuestor_DateTime = DateTime.Now;
            _States.CurrentQuestorState = QuestorState.CloseQuestor;
        }

        /*
        public void RecallDrones()
        {
            if (Cache.Instance.InSpace && Cache.Instance.ActiveDrones.Any() &&
                DateTime.Now > Cache.Instance.NextDroneRecall)
            {
                Logging.Log("QuestorState." + State + ": We are not scrambled and will be warping soon: pulling drones");
                // Tell the drones module to retract drones
                Cache.Instance.IsMissionPocketDone = true;
                Cache.Instance.NextDroneRecall = DateTime.Now.AddSeconds(10);
                _drones.ProcessState();
            }
        }

        private void TravelToAgentsStation()
        {
            var baseDestination = Traveler.Destination as StationDestination;
            if (baseDestination == null || baseDestination.StationId != Cache.Instance.Agent.StationId)
              _traveler.Destination = new StationDestination(Cache.Instance.Agent.SolarSystemId, Cache.Instance.Agent.StationId, Cache.Instance.DirectEve.GetLocationName(Cache.Instance.Agent.StationId));
                                                               Cache.Instance.Agent.StationId,
                                                               Cache.Instance.DirectEve.GetLocationName(
                                                                   Cache.Instance.Agent.StationId));
            //
            // is there a reason we do not just let combat.cs pick targets? 
            // I am not seeing why we are limiting ourselves to priority targets
            //
            if (Cache.Instance.PriorityTargets.Any(pt => pt != null && pt.IsValid))
            {
                Logging.Log("QuestorState." + State + ": TravelToAgentsStation: Priority targets found, engaging!");
                _combat.ProcessState();
                _drones.ProcessState(); //do we really want to use drones here? 
            }
            else
            {
                if (Cache.Instance.InSpace && Cache.Instance.ActiveDrones.Any() &&
                    DateTime.Now > Cache.Instance.NextDroneRecall)
                {
                    Logging.Log("QuestorState." + State +
                                ": We are not scrambled and will be warping soon: pulling drones");
                    // Tell the drones module to retract drones
                    Cache.Instance.IsMissionPocketDone = true;
                    Cache.Instance.NextDroneRecall = DateTime.Now.AddSeconds(10);
                }
            }
            _traveler.ProcessState();
            if (Settings.Instance.DebugStates)
            {
                Logging.Log("Traveler.State = " + _traveler.State);
            }
        }

        private void AvoidBumpingThings()
        {
            // anti bump
            EntityCache bigObjects =
                Cache.Instance.Entities.Where(
                    i => i.GroupId == (int) Group.LargeCollidableStructure || i.GroupId == (int) Group.SpawnContainer).
                    OrderBy(t => t.Distance).FirstOrDefault();
            //
            // always shoot at NPCs while getting un-hung
            //
            _combat.ProcessState();

            //
            // only use drones if warp scrambled as we do not want to leave them behind accidentally
            //
            if (Cache.Instance.InSpace && Cache.Instance.TargetedBy.Any(t => t.IsWarpScramblingMe))
            {
                _drones.ProcessState();
            }
            //
            // if we are "too close" to the bigObject move away... (is orbit the best thing to do here?)
            //
            if (Cache.Instance.InSpace && bigObjects != null && bigObjects.Distance < (int) Distance.TooCloseToStructure)
            {
                if (DateTime.Now > Cache.Instance.NextOrbit)
                {
                    bigObjects.Orbit((int) Distance.SafeDistancefromStructure);
                    Logging.Log("QuestorState: " + State + ": initiating Orbit of [" + bigObjects.Name +
                                "] orbiting at [" + Cache.Instance.OrbitDistance + "]");
                    Cache.Instance.NextOrbit = DateTime.Now.AddSeconds((int) Time.OrbitDelay_seconds);
                }
                return; //we are still too close, do not continue through the rest until we are not "too close" anymore
            }
            else
            {
                //we are no longer "too close" and can proceed. 
            }
        }

        */

        public static void CheckEVEStatus()
        {
            // get the current process
            Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();

            // get the physical mem usage (this only runs between missions)
            Cache.Instance.TotalMegaBytesOfMemoryUsed = ((currentProcess.WorkingSet64 / 1024) / 1024);
            Logging.Log("CombatMissionsBehavior: EVE instance: totalMegaBytesOfMemoryUsed - " +
                        Cache.Instance.TotalMegaBytesOfMemoryUsed + " MB");

            // If Questor window not visible, schedule a restart of questor in the uplink so that the GUI will start normally

            /*
             * 
             if (!m_Parent.Visible)
            //GUI isn't visible and CloseQuestorflag is true, so that his code block only runs once
            {
                //m_Parent.Visible = true; //this does not work for some reason - innerspace issue?
                Cache.Instance.ReasonToStopQuestor =
                    "The Questor GUI is not visible: did EVE get restarted due to a crash or lag?";
                Logging.Log(Cache.Instance.ReasonToStopQuestor);
                Cache.Instance.CloseQuestorCMDLogoff = false;
                Cache.Instance.CloseQuestorCMDExitGame = true;
                Cache.Instance.SessionState = "Exiting";
                BeginClosingQuestor();
            }
            else 
          
             */

            if (Cache.Instance.TotalMegaBytesOfMemoryUsed > (Settings.Instance.EVEProcessMemoryCeiling - 50) &&
                        Settings.Instance.EVEProcessMemoryCeilingLogofforExit != "")
            {
                Logging.Log(
                    "CombatMissionsBehavior: Memory usage is above the EVEProcessMemoryCeiling threshold. EVE instance: totalMegaBytesOfMemoryUsed - " +
                    Cache.Instance.TotalMegaBytesOfMemoryUsed + " MB");
                Cache.Instance.ReasonToStopQuestor =
                    "Memory usage is above the EVEProcessMemoryCeiling threshold. EVE instance: totalMegaBytesOfMemoryUsed - " +
                    Cache.Instance.TotalMegaBytesOfMemoryUsed + " MB";
                if (Settings.Instance.EVEProcessMemoryCeilingLogofforExit == "logoff")
                {
                    Cache.Instance.CloseQuestorCMDLogoff = true;
                    Cache.Instance.CloseQuestorCMDExitGame = false;
                    Cache.Instance.SessionState = "LoggingOff";
                    BeginClosingQuestor();
                    return;
                }
                if (Settings.Instance.EVEProcessMemoryCeilingLogofforExit == "exit")
                {
                    Cache.Instance.CloseQuestorCMDLogoff = false;
                    Cache.Instance.CloseQuestorCMDExitGame = true;
                    Cache.Instance.SessionState = "Exiting";
                    BeginClosingQuestor();
                    return;
                }
                Logging.Log(
                    "CombatMissionsBehavior: EVEProcessMemoryCeilingLogofforExit was not set to exit or logoff - doing nothing ");
            }
            else
            {
                Cache.Instance.SessionState = "Running";
            }
        }


        private void OnFrame(object sender, EventArgs e)
        {
            var watch = new Stopwatch();
            Cache.Instance.LastFrame = DateTime.Now;

            // Only pulse state changes every 1.5s
            if (DateTime.Now.Subtract(_lastPulse).TotalMilliseconds < (int)Time.QuestorPulse_milliseconds) //default: 1500ms
                return;
            _lastPulse = DateTime.Now;

            // Session is not ready yet, do not continue
            if (!Cache.Instance.DirectEve.Session.IsReady)
                return;

            if (Cache.Instance.DirectEve.Session.IsReady)
                Cache.Instance.LastSessionIsReady = DateTime.Now;
            
            // We are not in space or station, don't do shit yet!
            if (!Cache.Instance.InSpace && !Cache.Instance.InStation)
            {
                Cache.Instance.NextInSpaceorInStation = DateTime.Now.AddSeconds(7);
                return;
            }

            if (DateTime.Now < Cache.Instance.NextInSpaceorInStation)
                return;

            // New frame, invalidate old cache
            Cache.Instance.InvalidateCache();

            // Update settings (settings only load if character name changed)
            Settings.Instance.LoadSettings();
            CharacterName = Cache.Instance.DirectEve.Me.Name;

            // Check 3D rendering
            if (Cache.Instance.DirectEve.Session.IsInSpace &&
                Cache.Instance.DirectEve.Rendering3D != !Settings.Instance.Disable3D)
                Cache.Instance.DirectEve.Rendering3D = !Settings.Instance.Disable3D;


            if (DateTime.Now.Subtract(Cache.Instance.LastupdateofSessionRunningTime).TotalSeconds <
                (int) Time.SessionRunningTimeUpdate_seconds)
            {
                Cache.Instance.SessionRunningTime =
                    (int) DateTime.Now.Subtract(Cache.Instance.QuestorStarted_DateTime).TotalMinutes;
                Cache.Instance.LastupdateofSessionRunningTime = DateTime.Now;
            }

            if (!Cache.Instance.Paused)
            {
                if (DateTime.Now.Subtract(Cache.Instance.LastWalletCheck).TotalMinutes > (int) Time.WalletCheck_minutes)
                {
                    Cache.Instance.LastWalletCheck = DateTime.Now;
                    //Logging.Log("[Questor] Wallet Balance Debug Info: lastknowngoodconnectedtime = " + Settings.Instance.lastKnownGoodConnectedTime);
                    //Logging.Log("[Questor] Wallet Balance Debug Info: DateTime.Now - lastknowngoodconnectedtime = " + DateTime.Now.Subtract(Settings.Instance.lastKnownGoodConnectedTime).TotalSeconds);
                    if (Math.Round(DateTime.Now.Subtract(Cache.Instance.LastKnownGoodConnectedTime).TotalMinutes) > 1)
                    {
                        Logging.Log(String.Format("Questor: Wallet Balance Has Not Changed in [ {0} ] minutes.",
                                                  Math.Round(
                                                      DateTime.Now.Subtract(Cache.Instance.LastKnownGoodConnectedTime).
                                                          TotalMinutes, 0)));
                    }

                    //Settings.Instance.walletbalancechangelogoffdelay = 2;  //used for debugging purposes
                    //Logging.Log("Cache.Instance.lastKnownGoodConnectedTime is currently: " + Cache.Instance.lastKnownGoodConnectedTime);
                    if (Math.Round(DateTime.Now.Subtract(Cache.Instance.LastKnownGoodConnectedTime).TotalMinutes) <
                        Settings.Instance.Walletbalancechangelogoffdelay)
                    {
                        //if (State == QuestorState.Salvage)
                        //{
                        //    Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                        //    Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                        //}
                        //else
                        //{
                            if (Cache.Instance.MyWalletBalance != Cache.Instance.DirectEve.Me.Wealth)
                            {
                                Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                                Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                            }
                        //}
                    }
                    else
                    {
                        Logging.Log(
                            String.Format(
                                "Questor: Wallet Balance Has Not Changed in [ {0} ] minutes. Switching to QuestorState.CloseQuestor",
                                Math.Round(
                                    DateTime.Now.Subtract(Cache.Instance.LastKnownGoodConnectedTime).TotalMinutes, 0)));
                        Cache.Instance.ReasonToStopQuestor = "Wallet Balance did not change for over " +
                                                             Settings.Instance.Walletbalancechangelogoffdelay + "min";

                        if (Settings.Instance.WalletbalancechangelogoffdelayLogofforExit == "logoff")
                        {
                            Logging.Log("Questor: walletbalancechangelogoffdelayLogofforExit is set to: " +
                                        Settings.Instance.WalletbalancechangelogoffdelayLogofforExit);
                            Cache.Instance.CloseQuestorCMDLogoff = true;
                            Cache.Instance.CloseQuestorCMDExitGame = false;
                            Cache.Instance.SessionState = "LoggingOff";
                        }
                        if (Settings.Instance.WalletbalancechangelogoffdelayLogofforExit == "exit")
                        {
                            Logging.Log("Questor: walletbalancechangelogoffdelayLogofforExit is set to: " +
                                        Settings.Instance.WalletbalancechangelogoffdelayLogofforExit);
                            Cache.Instance.CloseQuestorCMDLogoff = false;
                            Cache.Instance.CloseQuestorCMDExitGame = true;
                            Cache.Instance.SessionState = "Exiting";
                        }
                        BeginClosingQuestor();
                        return;
                    }
                }
            }

            // We always check our defense state if we're in space, regardless of questor state
            // We also always check panic
            if (Cache.Instance.InSpace)
            {
                DebugPerformanceClearandStartTimer();
                if (!Cache.Instance.DoNotBreakInvul)
                {
                    _defense.ProcessState();
                }
                DebugPerformanceStopandDisplayTimer("Defense.ProcessState");
            }

            if (Cache.Instance.Paused)
            {
                Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                Cache.Instance.GotoBaseNow = false;
                Cache.Instance.SessionState = string.Empty;
                return;
            }

            if (Cache.Instance.SessionState == "Quitting")
            {
                if (_States.CurrentQuestorState != QuestorState.CloseQuestor)
                {
                    BeginClosingQuestor();
                }
            }

            // Start _cleanup.ProcessState
            // Description: Closes Windows, and eventually other things considered 'cleanup' useful to more than just Questor(Missions) but also Anomalies, Mining, etc
            //
            DebugPerformanceClearandStartTimer();
            _cleanup.ProcessState();
            DebugPerformanceStopandDisplayTimer("Cleanup.ProcessState");

            if (Settings.Instance.DebugStates)
                Logging.Log("Cleanup.State = " + _States.CurrentCleanupState);

            // Done
            // Cleanup State: ProcessState

            // When in warp there's nothing we can do, so ignore everything
            if (Cache.Instance.InWarp)
                return;

            //DirectAgentMission mission;
            switch (_States.CurrentQuestorState)
            {
                case QuestorState.Idle:
                    // Every 5 min of idle check and make sure we aren't supposed to stop...
                    if (Math.Round(DateTime.Now.Subtract(Cache.Instance.LastTimeCheckAction).TotalMinutes) > 5)
                    {
                        Cache.Instance.LastTimeCheckAction = DateTime.Now;
                        if (DateTime.Now.Subtract(Cache.Instance.QuestorStarted_DateTime).TotalMinutes >
                            Cache.Instance.MaxRuntime)
                        {
                            // quit questor
                            Logging.Log("Questor: Maximum runtime exceeded.  Quiting...");
                            Cache.Instance.ReasonToStopQuestor = "Maximum runtime specified and reached.";
                            Settings.Instance.AutoStart = false;
                            Cache.Instance.CloseQuestorCMDLogoff = false;
                            Cache.Instance.CloseQuestorCMDExitGame = true;
                            Cache.Instance.SessionState = "Exiting";
                            if (_States.CurrentQuestorState == QuestorState.Idle)
                            {
                                BeginClosingQuestor();
                            }
                            return;
                        }
                        if (Cache.Instance.StopTimeSpecified)
                        {
                            if (DateTime.Now >= Cache.Instance.StopTime)
                            {
                                Logging.Log("Questor: Time to stop.  Quitting game.");
                                Cache.Instance.ReasonToStopQuestor = "StopTimeSpecified and reached.";
                                Settings.Instance.AutoStart = false;
                                Cache.Instance.CloseQuestorCMDLogoff = false;
                                Cache.Instance.CloseQuestorCMDExitGame = true;
                                Cache.Instance.SessionState = "Exiting";
                                if (_States.CurrentQuestorState == QuestorState.Idle)
                                {
                                    BeginClosingQuestor();
                                }
                                return;
                            }
                        }
                    }
                    if (Cache.Instance.StopBot)
                        return;

                    if (Settings.Instance.AutoStart)
                    {
                        if (_States.CurrentQuestorState == QuestorState.Idle)
                        {
                            _States.CurrentQuestorState = QuestorState.Start;
                        }
                        return;
                    }
                    break;

                case QuestorState.CombatMissionsBehavior: 
                    //
                    // QuestorState will stay here until changed externally by the behavior we just kicked into starting
                    //
                    if (_States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Idle)
                    {
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
                    }
                           _combatMissionsBehavior.ProcessState();

                    break;

                case QuestorState.Start:
                    if (Settings.Instance.CharacterMode.ToLower() == "combat missions" || Settings.Instance.CharacterMode.ToLower() == "dps")
                    {
                        if (_States.CurrentQuestorState == QuestorState.Start)
                        {
                            Logging.Log("Questor: Start Mission Behavior");
                            _States.CurrentQuestorState = QuestorState.CombatMissionsBehavior;
                        }
                        break;
                    }
                    if (Settings.Instance.CharacterMode.ToLower() == "salvage")
                    {
                        if (_States.CurrentQuestorState == QuestorState.Start)
                        {
                            Logging.Log("Questor: Start Salvaging Behavior");
                            //State = QuestorState.SalvageBehavior;
                        }
                        break;
                    }
                    break;
                    
                case QuestorState.GotoBase:
                    bool debugGotoBase = false;

                    if (debugGotoBase) Logging.Log("QuestorState: GotoBase: AvoidBumpingThings()");

                    //AvoidBumpingThings();

                    if (debugGotoBase) Logging.Log("QuestorState: GotoBase: TravelToAgentsStation()");

                    //TravelToAgentsStation();

                    //if (_traveler.State == TravelerState.AtDestination)
                    //{
                    //    if (debugGotoBase) Logging.Log("QuestorState: GotoBase: We are at destination");
                    //    Cache.Instance.GotoBaseNow = false; //we are there - turn off the 'forced' gotobase
                    //    Cache.Instance.Mission = Cache.Instance.GetAgentMission(AgentID);
                    //
                    //    if (State == QuestorState.GotoBase)
                    //    {
                    //        State = QuestorState.Error;
                    //    }
                    //   Traveler.Destination = null;
                    //}
                    break;

                
                case QuestorState.CloseQuestor:
                    Cache.Instance.SessionState = "Quitting!!"; //so that IF we changed the state we would not be caught in a loop of re-entering closequestor
                    if (!Cache.Instance.CloseQuestorCMDLogoff && !Cache.Instance.CloseQuestorCMDExitGame)
                    {
                        Cache.Instance.CloseQuestorCMDExitGame = true;
                    }
                    //if (_traveler.State == TravelerState.Idle)
                    //{
                    //    Logging.Log(
                    //        "QuestorState.CloseQuestor: Entered Traveler - making sure we will be docked at Home Station");
                    //}
                    //AvoidBumpingThings();
                    //TravelToAgentsStation();

                    //if (_traveler.State == TravelerState.AtDestination ||
                    //    DateTime.Now.Subtract(Cache.Instance.EnteredCloseQuestor_DateTime).TotalSeconds >
                     //   Settings.Instance.SecondstoWaitAfterExteringCloseQuestorBeforeExitingEVE)
                    //{
                        //Logging.Log("QuestorState.CloseQuestor: At Station: Docked");
                        // Write to Session log
                        if (!Statistics.WriteSessionLogClosing()) break;

                        if (Settings.Instance.AutoStart)
                            //if autostart is disabled do not schedule a restart of questor - let it stop gracefully.
                        {
                            if (Cache.Instance.CloseQuestorCMDLogoff)
                            {
                                if (CloseQuestorflag)
                                {
                                    Logging.Log(
                                        "Questor: We are in station: Logging off EVE: In theory eve and questor will restart on their own when the client comes back up");
                                    LavishScript.ExecuteCommand(
                                        "uplink echo Logging off EVE:  \\\"${Game}\\\" \\\"${Profile}\\\"");
                                    Logging.Log(
                                        "Questor: you can change this option by setting the wallet and eveprocessmemoryceiling options to use exit instead of logoff: see the settings.xml file");
                                    Logging.Log("Questor: Logging Off eve in 15 seconds.");
                                    CloseQuestorflag = false;
                                    CloseQuestorDelay =
                                        DateTime.Now.AddSeconds((int) Time.CloseQuestorDelayBeforeExit_seconds);
                                }
                                if (CloseQuestorDelay.AddSeconds(-10) < DateTime.Now)
                                {
                                    Logging.Log("Questor: Exiting eve in 10 seconds");
                                }
                                if (CloseQuestorDelay < DateTime.Now)
                                {
                                    Logging.Log("Questor: Exiting eve now.");
                                    Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdLogOff);
                                }
                                Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdLogOff);
                                break;
                            }
                            if (Cache.Instance.CloseQuestorCMDExitGame)
                            {
                                //Logging.Log("Questor: We are in station: Exit option has been configured.");
                                if ((Settings.Instance.CloseQuestorCMDUplinkIsboxerCharacterSet) &&
                                    (Settings.Instance.CloseQuestorCMDUplinkInnerspaceProfile))
                                {
                                    Logging.Log(
                                        "Questor: We are in station: Don't be silly you cant use both the CloseQuestorCMDUplinkIsboxerProfile and the CloseQuestorCMDUplinkIsboxerProfile setting, choose one");
                                }
                                else
                                {
                                    if (Settings.Instance.CloseQuestorCMDUplinkInnerspaceProfile)
                                        //if configured as true we will use the innerspace profile to restart this session
                                    {
                                        //Logging.Log("Questor: We are in station: CloseQuestorCMDUplinkInnerspaceProfile is ["+ CloseQuestorCMDUplinkInnerspaceProfile.tostring() +"]");
                                        if (_closeQuestorCMDUplink)
                                        {
                                            Logging.Log(
                                                "Questor: We are in station: Starting a timer in the innerspace uplink to restart this innerspace profile session");
                                            LavishScript.ExecuteCommand(
                                                "uplink exec timedcommand 350 open \\\"${Game}\\\" \\\"${Profile}\\\"");
                                            Logging.Log(
                                                "Questor: Done: quitting this session so the new innerspace session can take over");
                                            Logging.Log("Questor: Exiting eve in 15 seconds.");
                                            _closeQuestorCMDUplink = false;
                                            CloseQuestorDelay =
                                                DateTime.Now.AddSeconds((int) Time.CloseQuestorDelayBeforeExit_seconds);
                                        }
                                        if ((CloseQuestorDelay.AddSeconds(-10) == DateTime.Now) &&
                                            (!_closeQuestor10SecWarningDone))
                                        {
                                            _closeQuestor10SecWarningDone = true;
                                            Logging.Log("Questor: Exiting eve in 10 seconds");
                                            Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdQuitGame);
                                        }
                                        if (CloseQuestorDelay < DateTime.Now)
                                        {
                                            Logging.Log("Questor: Exiting eve now.");
                                            Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdQuitGame);
                                        }
                                        return;
                                    }
                                    else if (Settings.Instance.CloseQuestorCMDUplinkIsboxerCharacterSet)
                                        //if configured as true we will use isboxer to restart this session
                                    {
                                        //Logging.Log("Questor: We are in station: CloseQuestorCMDUplinkIsboxerProfile is ["+ CloseQuestorCMDUplinkIsboxerProfile.tostring() +"]");
                                        if (_closeQuestorCMDUplink)
                                        {
                                            Logging.Log(
                                                "Questor: We are in station: Starting a timer in the innerspace uplink to restart this isboxer character set");
                                            LavishScript.ExecuteCommand(
                                                "uplink timedcommand 350 runscript isboxer -launch \\\"${ISBoxerCharacterSet}\\\"");
                                            Logging.Log(
                                                "Questor: Done: quitting this session so the new isboxer session can take over");
                                            Logging.Log("Questor: We are in station: Exiting eve.");
                                            _closeQuestorCMDUplink = false;
                                            CloseQuestorDelay =
                                                DateTime.Now.AddSeconds(
                                                    (int) Time.CloseQuestorDelayBeforeExit_seconds);
                                        }
                                        if ((CloseQuestorDelay.AddSeconds(-10) == DateTime.Now) &&
                                            (!_closeQuestor10SecWarningDone))
                                        {
                                            _closeQuestor10SecWarningDone = true;
                                            Logging.Log("Questor: Exiting eve in 10 seconds");
                                            Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdQuitGame);
                                        }
                                        if (CloseQuestorDelay < DateTime.Now)
                                        {
                                            Logging.Log("Questor: Exiting eve now.");
                                            Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdQuitGame);
                                        }
                                        return;
                                    }
                                    else if (!Settings.Instance.CloseQuestorCMDUplinkInnerspaceProfile &&
                                             !Settings.Instance.CloseQuestorCMDUplinkIsboxerCharacterSet)
                                    {
                                        Logging.Log(
                                            "Questor: CloseQuestorCMDUplinkInnerspaceProfile and CloseQuestorCMDUplinkIsboxerProfile both false");
                                        if (_closeQuestorCMDUplink)
                                        {
                                            _closeQuestorCMDUplink = false;
                                            CloseQuestorDelay =
                                                DateTime.Now.AddSeconds(
                                                    (int) Time.CloseQuestorDelayBeforeExit_seconds);
                                        }
                                        if ((CloseQuestorDelay.AddSeconds(-10) == DateTime.Now) &&
                                            (!_closeQuestor10SecWarningDone))
                                        {
                                            _closeQuestor10SecWarningDone = true;
                                            Logging.Log("Questor: Exiting eve in 10 seconds");
                                            Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdQuitGame);
                                        }
                                        if (CloseQuestorDelay < DateTime.Now)
                                        {
                                            Logging.Log("Questor: Exiting eve now.");
                                            Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdQuitGame);
                                        }
                                        return;
                                    }
                                }
                            }
                        //}
                        Logging.Log("Autostart is false: Stopping EVE with quit command");
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdQuitGame);
                        break;
                    }
                    //if (Settings.Instance.DebugStates)
                    //    Logging.Log("Traveler.State = " + _traveler.State);
                    break;

                case QuestorState.DebugCloseQuestor:
                    //Logging.Log("ISBoxerCharacterSet: " + Settings.Instance.Lavish_ISBoxerCharacterSet);
                    //Logging.Log("Profile: " + Settings.Instance.Lavish_InnerspaceProfile);
                    //Logging.Log("Game: " + Settings.Instance.Lavish_Game);
                    Logging.Log("CloseQuestorCMDUplinkInnerspaceProfile: " +
                                Settings.Instance.CloseQuestorCMDUplinkInnerspaceProfile);
                    Logging.Log("CloseQuestorCMDUplinkISboxerCharacterSet: " +
                                Settings.Instance.CloseQuestorCMDUplinkIsboxerCharacterSet);
                    Logging.Log("walletbalancechangelogoffdelay: " + Settings.Instance.Walletbalancechangelogoffdelay);
                    Logging.Log("walletbalancechangelogoffdelayLogofforExit: " +
                                Settings.Instance.WalletbalancechangelogoffdelayLogofforExit);
                    Logging.Log("walletbalancechangelogoffdelayLogofforExit: " +
                                Settings.Instance.WalletbalancechangelogoffdelayLogofforExit);
                    Logging.Log("EVEProcessMemoryCeiling: " + Settings.Instance.EVEProcessMemoryCeiling);
                    Logging.Log("EVEProcessMemoryCielingLogofforExit: " +
                                Settings.Instance.EVEProcessMemoryCeilingLogofforExit);
                    if (_States.CurrentQuestorState == QuestorState.DebugCloseQuestor)
                    {
                        _States.CurrentQuestorState = QuestorState.Error;
                    }
                    return;

                case QuestorState.DebugWindows:
                    List<DirectWindow> windows = Cache.Instance.Windows;

                    foreach (DirectWindow window in windows)
                    {
                        Logging.Log("Debug_Questor_WindowNames: [" + window.Name + "]");
                    }
                    foreach (DirectWindow window in windows)
                    {
                        Logging.Log("Debug_Windowcaptions: [" + window.Name + window.Caption + "]");
                    }
                    foreach (DirectWindow window in windows)
                    {
                        Logging.Log("Debug_WindowTypes: [" + window.Name + window.Type + "]");
                    }
                    foreach (DirectWindow window in windows)
                    {
                        Logging.Log("Debug_Questor_WindowNames: [" + window.Name + "]");
                        Logging.Log("Debug_WindowTypes: [" + window.Html + "]");
                    }
                    if (_States.CurrentQuestorState == QuestorState.DebugWindows)
                    {
                        _States.CurrentQuestorState = QuestorState.Error;
                    }
                    return;
                
            }
        }
    }
}