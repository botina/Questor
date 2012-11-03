﻿
namespace Questor.Modules.States
{
    using LavishScriptAPI;
    using Questor.Modules.Lookup;

    public class _States
    {
        public static void LavishEvent_QuestorIdle()
        {
            if (Settings.Instance.UseInnerspace)
            {
                uint QuestorIdleEvent = LavishScript.Events.RegisterEvent("QuestorIdle");
                LavishScript.Events.ExecuteEvent(QuestorIdleEvent, "");
            }
        }

        public static void LavishEvent_QuestorState()
        {
            if (Settings.Instance.UseInnerspace)
            {
                uint QuestorStateEvent = LavishScript.Events.RegisterEvent("QuestorState");
                LavishScript.Events.ExecuteEvent(QuestorStateEvent, _States.CurrentQuestorState.ToString());
            }
        }

        public static void LavishEvent_QuestorCombatMissionsBehaviorState()
        {
            if (Settings.Instance.UseInnerspace)
            {
                uint QuestorCombatMissionsBehaviorStateEvent = LavishScript.Events.RegisterEvent("QuestorCombatMissionsBehaviorState");
                LavishScript.Events.ExecuteEvent(QuestorCombatMissionsBehaviorStateEvent, _States.CurrentCombatMissionBehaviorState.ToString());
            }
        }

        public static QuestorState CurrentQuestorState { get; set; }

        public static DroneState CurrentDroneState { get; set; }

        public static CleanupState CurrentCleanupState { get; set; }

        public static LocalWatchState CurrentLocalWatchState { get; set; }

        public static SalvageState CurrentSalvageState { get; set; }

        public static ScoopState CurrentScoopState { get; set; }

        public static PanicState CurrentPanicState { get; set; }

        public static CombatState CurrentCombatState { get; set; }

        public static MiningState CurrentMiningState { get; set; }

        public static TravelerState CurrentTravelerState { get; set; }

        public static CombatMissionsBehaviorState CurrentCombatMissionBehaviorState { get; set; }

        public static DedicatedBookmarkSalvagerBehaviorState CurrentDedicatedBookmarkSalvagerBehaviorState { get; set; }

        public static CombatHelperBehaviorState CurrentCombatHelperBehaviorState { get; set; }

        public static DirectionalScannerBehaviorState CurrentDirectionalScannerBehaviorState { get; set; }

        public static DebugBehaviorState CurrentDebugBehaviorState { get; set; }

        public static DebugHangarsBehaviorState CurrentDebugHangarBehaviorState { get; set; }

        public static CourierMissionCtrlState CurrentCourierMissionCtrlState { get; set; }

        public static CombatMissionCtrlState CurrentCombatMissionCtrlState { get; set; }

        //public static ActionState CurrentActionState { get; set; }
        public static AgentInteractionState CurrentAgentInteractionState { get; set; }

        public static ArmState CurrentArmState { get; set; }

        public static BuyState CurrentBuyState { get; set; }

        public static BuyLPIState CurrentBuyLPIState { get; set; }

        public static DebugModuleState CurrentDebugModuleState { get; set; }

        public static DropState CurrentDropState { get; set; }

        public static GrabState CurrentGrabState { get; set; }

        public static ScanInteractionState CurrentScanInteractionState { get; set; }

        public static SellState CurrentSellState { get; set; }

        public static SwitchShipState CurrentSwitchShipState { get; set; }

        public static UnloadLootState CurrentUnloadLootState { get; set; }

        public static ValueDumpState CurrentValueDumpState { get; set; }

        public static StorylineState CurrentStorylineState { get; set; }
    }
}