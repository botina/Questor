﻿// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

namespace Questor.Modules.States
{
    //using LavishScriptAPI;
    //using Questor.Modules.Caching;
    //using Questor.Modules.Lookup;

    public static class _State
    {

        //public static void LavishEvent_QuestorPausedState()
        //{
        //    if (Settings.Instance.UseInnerspace)
        //    {
        //        uint QuestorPausedStateEvent = LavishScript.Events.RegisterEvent("QuestorPausedState");
        //        LavishScript.Events.ExecuteEvent(QuestorPausedStateEvent, Cache.Instance.Paused.ToString());
        //    }
        //}

        public static SkillTrainerState CurrentSkillTrainerState { get; set; }
    }

    public enum SkillTrainerState
    {
        Idle,
        Begin,
        Done,
        LoadPlan,
        ReadCharacterSheetSkills,
        AreThereSkillsReadyToInject,
        CheckTrainingQueue,
        Error,
        CloseQuestor,
        GenerateInnerspaceProfile,
    }
}