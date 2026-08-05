using System;
using Unity.Collections;
using Unity.Netcode;

namespace SEMM91.Networking.DebugSnapshots
{
    public static class SceneReleaseStructuralPairQuery
    {
        public static bool TryFindFirst(
            FixedString64Bytes releaseId,
            NetworkList<
                DomainSnapshotReplicator
                    .KeeperReleaseDebugRow>
                releaseRows,
            NetworkList<
                DemoTapeIdeaSemanticDebugRow>
                ideaRows,
            NetworkList<
                DemoTapeTagSemanticDebugRow>
                tagRows,
            out DemoTapeStructuralPairCandidate
                candidate)
        {
            if (releaseRows == null)
            {
                throw new ArgumentNullException(
                    nameof(releaseRows)
                );
            }

            if (ideaRows == null)
            {
                throw new ArgumentNullException(
                    nameof(ideaRows)
                );
            }

            if (tagRows == null)
            {
                throw new ArgumentNullException(
                    nameof(tagRows)
                );
            }

            for (int releaseIndex = 0;
                 releaseIndex < releaseRows.Count;
                 releaseIndex++)
            {
                DomainSnapshotReplicator
                    .KeeperReleaseDebugRow release =
                        releaseRows[releaseIndex];

                if (!release.ReleaseId.Equals(
                        releaseId
                    ))
                {
                    continue;
                }

                return
                    DemoTapeStructuralPairQuery
                        .TryFindFirst(
                            release.SourceDemoTapeId,
                            ideaRows,
                            tagRows,
                            out candidate
                        );
            }

            candidate = default;
            return false;
        }
    }
}