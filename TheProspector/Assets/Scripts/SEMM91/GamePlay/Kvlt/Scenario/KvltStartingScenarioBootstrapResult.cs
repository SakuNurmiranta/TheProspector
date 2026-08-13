namespace SEMM91.GamePlay.Kvlt.Scenario
{
    public sealed class
        KvltStartingScenarioBootstrapResult
    {
        public ulong FoundingClientId { get; }

        public string FoundingEntityId { get; }

        public string FoundingDemoTapeId { get; }

        public int ParticipantCount { get; }

        public KvltStartingScenarioBootstrapResult(
            ulong foundingClientId,
            string foundingEntityId,
            string foundingDemoTapeId,
            int participantCount)
        {
            FoundingClientId =
                foundingClientId;

            FoundingEntityId =
                foundingEntityId;

            FoundingDemoTapeId =
                foundingDemoTapeId;

            ParticipantCount =
                participantCount;
        }
    }
}