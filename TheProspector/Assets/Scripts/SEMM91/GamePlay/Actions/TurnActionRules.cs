namespace SEMM91.GamePlay.Actions
{
    public static class TurnActionRules
    {
        public const int StandardProductiveActionCapacity = 2;
        public const int MaximumProductiveActions = 3;

        public static bool IsProductive(
            DraftedActionType actionType)
        {
            return actionType != DraftedActionType.None &&
                   actionType != DraftedActionType.Rest &&
                   actionType != DraftedActionType.Wait;
        }

        public static bool IsOverreach(
            int productiveActionCount)
        {
            return productiveActionCount > 
                   StandardProductiveActionCapacity;
        }

        public static bool IsValidProductiveActionCount(
            int productiveActionCount)
        {
            return productiveActionCount >= 0 &&
                   productiveActionCount <= 
                   MaximumProductiveActions;
        }
    }
}