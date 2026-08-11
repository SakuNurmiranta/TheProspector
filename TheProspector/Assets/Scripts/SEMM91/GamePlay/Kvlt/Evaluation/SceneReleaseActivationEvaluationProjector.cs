using System;
using System.Collections.Generic;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    /// <summary>
    /// Projects authoritative current SceneRelease
    /// pair activation into the immutable evaluation-time
    /// activation contract established in Camp 1.
    ///
    /// Pending activation is deliberately invisible here.
    /// Only current legitimate A is projected.
    /// </summary>
    public sealed class
        SceneReleaseActivationEvaluationProjector
    {
        public IReadOnlyList<
            TrackActivationEvaluationSnapshot>
            Project(
                SceneRelease release,
                DemoTape demoTape)
        {
            if (release == null)
            {
                throw new ArgumentNullException(
                    nameof(release)
                );
            }

            if (demoTape == null)
            {
                throw new ArgumentNullException(
                    nameof(demoTape)
                );
            }

            if (release.SourceDemoTapeId !=
                demoTape.DemoTapeId)
            {
                throw new ArgumentException(
                    "SceneRelease and DemoTape " +
                    "identity do not match.",
                    nameof(demoTape)
                );
            }

            List<
                TrackActivationEvaluationSnapshot>
                trackSnapshots =
                    new();

            foreach (
                DemoTapeTrackSnapshot track
                in demoTape.TrackSnapshots)
            {
                trackSnapshots.Add(
                    ProjectTrack(
                        release,
                        demoTape,
                        track
                    )
                );
            }

            return trackSnapshots.ToArray();
        }

        private static
            TrackActivationEvaluationSnapshot
            ProjectTrack(
                SceneRelease release,
                DemoTape demoTape,
                DemoTapeTrackSnapshot track)
        {
            List<IdeaActivationEvaluationSnapshot>
                ideaActivations =
                    new();

            foreach (
                DemoTapeIdeaSnapshot idea
                in track.IdeaSnapshots)
            {
                if (idea.PayloadType !=
                    IdeaPayloadType.TagPair)
                {
                    continue;
                }

                if (!release.TryGetPairActivationState(
                        track.SourceTrackId,
                        idea.SourceIdeaId,
                        idea.IdeaIndex,
                        out SceneReleasePairActivationState
                            state))
                {
                    // Missing current state means A = 0.
                    // TrackActivationEvaluationSnapshot
                    // already owns that semantic default.
                    continue;
                }

                DemoTapeTagOccurrenceSnapshot
                    recordedDominant =
                        FindRecordedDominant(
                            idea
                        );

                if (state.RecordedDominantDegree !=
                    recordedDominant.Degree)
                {
                    throw new InvalidOperationException(
                        "SceneRelease activation state " +
                        "does not match immutable DemoTape " +
                        "dominant degree | " +
                        $"release={release.ReleaseId} | " +
                        $"track={track.SourceTrackId} | " +
                        $"idea={idea.SourceIdeaId} | " +
                        $"stateD=" +
                        $"{state.RecordedDominantDegree} | " +
                        $"recordedD=" +
                        $"{recordedDominant.Degree}"
                    );
                }

                if (state.CurrentActivationDegree ==
                    TagDegree.Neutral)
                {
                    // A = 0 need not be explicitly emitted.
                    continue;
                }

                ideaActivations.Add(
                    new IdeaActivationEvaluationSnapshot(
                        idea.SourceIdeaId,
                        (int)state.CurrentActivationDegree
                    )
                );
            }

            return new TrackActivationEvaluationSnapshot(
                release.ReleaseId,
                demoTape.DemoTapeId,
                track.SourceTrackId,
                ideaActivations
            );
        }

        private static
            DemoTapeTagOccurrenceSnapshot
            FindRecordedDominant(
                DemoTapeIdeaSnapshot idea)
        {
            foreach (
                DemoTapeTagOccurrenceSnapshot occurrence
                in idea.TagOccurrences)
            {
                if (occurrence.Role ==
                    DemoTapeTagOccurrenceRole
                        .PairDominant)
                {
                    return occurrence;
                }
            }

            throw new InvalidOperationException(
                "Formal Tag-pair snapshot has no " +
                "dominant occurrence | " +
                $"idea={idea.SourceIdeaId}"
            );
        }
    }
}