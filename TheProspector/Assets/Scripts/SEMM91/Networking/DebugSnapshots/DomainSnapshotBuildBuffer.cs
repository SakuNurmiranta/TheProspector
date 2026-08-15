using System.Collections.Generic;

namespace SEMM91.Networking.DebugSnapshots
{
    /// <summary>
    /// Server-local projection buffer. Building a snapshot
    /// here is network-silent; publication later diffs this
    /// ordered state against the replicated NetworkLists.
    /// </summary>
    public sealed class DomainSnapshotBuildBuffer
    {
        public List<
                DomainSnapshotReplicator
                    .PlayerInventoryDebugRow>
            PlayerInventoryRows { get; } =
            new();

        public List<KvltTurnResolutionDebugRow>
            KvltTurnResolutionRows { get; } =
            new();

        public List<KvltCanonPrecedentDebugRow>
            KvltCanonPrecedentRows { get; } =
            new();

        public List<KvltSemanticEnvironmentDebugRow>
            KvltSemanticEnvironmentRows { get; } =
            new();

        public List<KvltHappeningDebugRow>
            KvltHappeningRows { get; } =
            new();

        public List<KvltHailDebugRow>
            KvltHailRows { get; } =
            new();

        public List<KvltParadigmGripDebugRow>
            KvltParadigmGripRows { get; } =
            new();

        public List<KvltParadigmBeefDebugRow>
            KvltParadigmBeefRows { get; } =
            new();

        public List<KvltPoserDebugRow>
            KvltPoserRows { get; } =
            new();

        public List<
                DomainSnapshotReplicator
                    .RehearsalSetDebugRow>
            RehearsalSetRows { get; } =
            new();

        public List<
                DomainSnapshotReplicator
                    .RehearsalTrackDebugRow>
            RehearsalTrackRows { get; } =
            new();

        public List<
                DomainSnapshotReplicator
                    .KeeperReleaseDebugRow>
            KeeperReleaseRows { get; } =
            new();

        public List<
                DomainSnapshotReplicator
                    .DemoTapeDebugRow>
            DemoTapeRows { get; } =
            new();

        public List<DemoTapeTrackSemanticDebugRow>
            DemoTapeTrackSemanticRows { get; } =
            new();

        public List<DemoTapeIdeaSemanticDebugRow>
            DemoTapeIdeaSemanticRows { get; } =
            new();

        public List<DemoTapeTagSemanticDebugRow>
            DemoTapeTagSemanticRows { get; } =
            new();

        public DomainSnapshotReplicator
                .KeeperInterventionDebugSnapshot
            KeeperInterventionState { get; set; }

        public KvltSceneDebugSnapshot
            KvltSceneState { get; set; }

        public void Reset()
        {
            PlayerInventoryRows.Clear();
            KvltTurnResolutionRows.Clear();
            KvltCanonPrecedentRows.Clear();
            KvltSemanticEnvironmentRows.Clear();
            KvltHappeningRows.Clear();
            KvltHailRows.Clear();
            KvltParadigmGripRows.Clear();
            KvltParadigmBeefRows.Clear();
            KvltPoserRows.Clear();
            RehearsalSetRows.Clear();
            RehearsalTrackRows.Clear();
            KeeperReleaseRows.Clear();
            DemoTapeRows.Clear();
            DemoTapeTrackSemanticRows.Clear();
            DemoTapeIdeaSemanticRows.Clear();
            DemoTapeTagSemanticRows.Clear();

            KeeperInterventionState =
                default;

            KvltSceneState =
                default;
        }
    }
}
