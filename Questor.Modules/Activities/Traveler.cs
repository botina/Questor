﻿// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
// 
//     Please look in the accompanying license.htm file for the license that 
//     applies to this source code. (a copy can also be found at: 
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

namespace Questor.Modules.Activities
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using DirectEve;
    using global::Questor.Modules.Actions;
    using global::Questor.Modules.Caching;
    using global::Questor.Modules.Combat;
    using global::Questor.Modules.Logging;
    using global::Questor.Modules.Lookup;
    using global::Questor.Modules.States;

    public class Traveler
    {
        private TravelerDestination _destination;
        private DateTime _nextTravelerAction;
        //private DateTime _lastDock;

        public DirectBookmark UndockBookmark { get; set; }

        public TravelerDestination Destination
        {
            get { return _destination; }
            set
            {
                _destination = value;
                _States.CurrentTravelerState = _destination == null ? TravelerState.AtDestination : TravelerState.Idle;
            }
        }

        /// <summary>
        ///   Navigate to a solar system
        /// </summary>
        /// <param name = "solarSystemId"></param>
        private void NagivateToBookmarkSystem(long solarSystemId)
        {
            if (_nextTravelerAction > DateTime.Now)
            {
                //Logging.Log("Traveler: will continue in [ " + Math.Round(_nextTravelerAction.Subtract(DateTime.Now).TotalSeconds, 0) + " ]sec");
                return;
            }

            DirectBookmark undockBookmark = UndockBookmark;
            UndockBookmark = undockBookmark;

            List<long> destination = Cache.Instance.DirectEve.Navigation.GetDestinationPath();
            if (destination.Count == 0 || !destination.Any(d => d == solarSystemId))
            {
                // We do not have the destination set
                DirectLocation location = Cache.Instance.DirectEve.Navigation.GetLocation(solarSystemId);
                if (location.IsValid)
                {
                    Logging.Log("Traveler: Setting destination to [" + location.Name + "]", Logging.green);
                    location.SetDestination();
                }
                else
                {
                    Logging.Log("Traveler: Error setting solar system destination [" + solarSystemId + "]", Logging.green);
                    _States.CurrentTravelerState = TravelerState.Error;
                }
                return;
            }
            else
            {
                if (!Cache.Instance.InSpace)
                {
                    if (Cache.Instance.InStation)
                    {
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdExitStation);
                        _nextTravelerAction = DateTime.Now.AddSeconds((int)Time.TravelerExitStationAmIInSpaceYet_seconds);
                    }

                    // We are not yet in space, wait for it
                    return;
                }

                // We are apparently not really in space yet...
                if (Cache.Instance.DirectEve.ActiveShip.Entity == null)
                return;

                // Find the first waypoint
                long waypoint = destination.First();

                // Get the name of the next system
                string locationName = Cache.Instance.DirectEve.Navigation.GetLocationName(waypoint);

                // Find the stargate associated with it
                IEnumerable<EntityCache> entities = Cache.Instance.EntitiesByName(locationName).Where(e => e.GroupId == (int)Group.Stargate);
                if (!entities.Any())
                {
                    // not found, that cant be true?!?!?!?!
                    Logging.Log("Traveler: Error [" + locationName + "] not found, most likely lag waiting 15 seconds.", Logging.red);
                    _nextTravelerAction = DateTime.Now.AddSeconds((int)Time.TravelerNoStargatesFoundRetryDelay_seconds);
                    return;
                }

                // Warp to, approach or jump the stargate
                EntityCache entity = entities.First();
                if (entity.Distance < (int)Distance.DecloakRange)
                {
                    Logging.Log("Traveler: Jumping to [" + locationName + "]", Logging.green);
                    entity.Jump();
                    Cache.Instance.NextInSpaceorInStation = DateTime.Now;
                    _nextTravelerAction = DateTime.Now.AddSeconds((int)Time.TravelerJumpedGateNextCommandDelay_seconds);
                    Cache.Instance.NextActivateSupportModules = DateTime.Now.AddSeconds((int)Time.TravelerJumpedGateNextCommandDelay_seconds);
                }
                else if (entity.Distance < (int)Distance.WarptoDistance)
                {
                    if (DateTime.Now > Cache.Instance.NextApproachAction && !Cache.Instance.IsApproaching)
                    {
                        entity.Approach(); //you could use a negative approach distance here but ultimately that is a bad idea.. Id like to go toward the entity without approaching it so we would end up inside the docking ring (eventually)
                        Cache.Instance.NextApproachAction = DateTime.Now.AddSeconds((int)Time.ApproachDelay_seconds);
                    }
                }
                else
                {
                    if (DateTime.Now > Cache.Instance.NextWarpTo)
                    {
                        Logging.Log("Traveler: Warping to [" + locationName + "][" + Math.Round((entity.Distance / 1000) / 149598000, 2) + " AU away]", Logging.green);
                        entity.WarpTo();
                        Combat.ReloadAll();
                        Cache.Instance.NextWarpTo = DateTime.Now.AddSeconds((int)Time.WarptoDelay_seconds);
                    }
                }
            }
        }

        public void ProcessState()
        {
            switch (_States.CurrentTravelerState)
            {
                case TravelerState.Idle:
                    _States.CurrentTravelerState = TravelerState.Traveling;
                    break;

                case TravelerState.Traveling:
                    if (Destination == null)
                    {
                        _States.CurrentTravelerState = TravelerState.Error;
                        break;
                    }

                    if (Destination.SolarSystemId != Cache.Instance.DirectEve.Session.SolarSystemId)
                    {
                        //Logging.Log("traveler: NagivateToBookmarkSystem(Destination.SolarSystemId);");
                        NagivateToBookmarkSystem(Destination.SolarSystemId);
                    }
                    else if (Destination.PerformFinalDestinationTask())
                    {
                        //Logging.Log("traveler: _States.CurrentTravelerState = TravelerState.AtDestination;");
                        _States.CurrentTravelerState = TravelerState.AtDestination;
                    }
                    break;

                case TravelerState.AtDestination:
                    //do nothing when at destination
                    //Traveler sits in AtDestination when it has nothing to do, NOT in idle. 
                    break;

                default:
                    break;
            }
        }
    }
}