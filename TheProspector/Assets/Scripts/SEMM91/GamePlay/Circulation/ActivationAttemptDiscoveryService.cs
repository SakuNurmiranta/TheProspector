using System;
using System.Collections.Generic;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Events;

namespace SEMM91.GamePlay.Circulation
{
    public sealed class
        ActivationAttemptDiscoveryService
    {
        public IReadOnlyList<
            SceneReleaseActivationAttempt>
            Discover(
                Happening happening,
                string intentId,
                IReadOnlyList<
                    SceneReleaseActivationSource>
                    publicSources)
        {
            if (happening == null)
            {
                throw new ArgumentNullException(
                    nameof(happening)
                );
            }

            if (publicSources == null)
            {
                throw new ArgumentNullException(
                    nameof(publicSources)
                );
            }

            if (string.IsNullOrWhiteSpace(
                    intentId))
            {
                return Array.Empty<
                    SceneReleaseActivationAttempt>();
            }

            if (!happening
                    .TryGetParticipantIntent(
                        intentId,
                        out HappeningParticipantIntent
                            intent
                    ))
            {
                return Array.Empty<
                    SceneReleaseActivationAttempt>();
            }

            switch (intent)
            {
                case HappeningPerformIntent perform:
                    return DiscoverPerformance(
                        happening,
                        perform,
                        publicSources
                    );

                case HappeningEnactBehaviorIntent behavior
                    when behavior.HasIntendedHail:
                    return DiscoverHail(
                        happening,
                        behavior,
                        publicSources
                    );

                default:
                    return Array.Empty<
                        SceneReleaseActivationAttempt>();
            }
        }

        private static IReadOnlyList<
            SceneReleaseActivationAttempt>
            DiscoverPerformance(
                Happening happening,
                HappeningPerformIntent intent,
                IReadOnlyList<
                    SceneReleaseActivationSource>
                    sources)
        {
            foreach (
                SceneReleaseActivationSource source
                in sources)
            {
                ValidateSource(source);

                SceneRelease release =
                    source.Release;

                if (release.ReleaseId !=
                    intent.TargetSceneReleaseId)
                {
                    continue;
                }

                if (!IsAvailableAtIntentTurn(
                        release,
                        intent.DeclaredTurn
                    ))
                {
                    return Array.Empty<
                        SceneReleaseActivationAttempt>();
                }

                return new[]
                {
                    SceneReleaseActivationAttempt
                        .ForPerformance(
                            happening.HappeningId,
                            intent.IntentId,
                            intent.ActorEntityId,
                            intent.DeclaredTurn,
                            release.ReleaseId,
                            source.DemoTape.DemoTapeId
                        )
                };
            }

            return Array.Empty<
                SceneReleaseActivationAttempt>();
        }

        private static IReadOnlyList<
            SceneReleaseActivationAttempt>
            DiscoverHail(
                Happening happening,
                HappeningEnactBehaviorIntent intent,
                IReadOnlyList<
                    SceneReleaseActivationSource>
                    sources)
        {
            List<SceneReleaseActivationAttempt>
                attempts =
                    new();

            foreach (
                SceneReleaseActivationSource source
                in sources)
            {
                ValidateSource(source);

                SceneRelease release =
                    source.Release;

                if (!IsAvailableAtIntentTurn(
                        release,
                        intent.DeclaredTurn
                    ))
                {
                    continue;
                }

                List<HailAnsweringPairCandidate>
                    pairCandidates =
                        FindAnsweringPairs(
                            source.DemoTape,
                            intent.Axis,
                            intent.Pole
                        );

                if (pairCandidates.Count == 0)
                {
                    continue;
                }

                attempts.Add(
                    SceneReleaseActivationAttempt
                        .ForHail(
                            happening.HappeningId,
                            intent.IntentId,
                            intent.ActorEntityId,
                            intent.DeclaredTurn,
                            release.ReleaseId,
                            source.DemoTape.DemoTapeId,
                            intent.IntendedHailedAspectId,
                            pairCandidates
                        )
                );
            }

            return attempts.ToArray();
        }

        private static
            List<HailAnsweringPairCandidate>
            FindAnsweringPairs(
                DemoTape demoTape,
                TagAxis intendedAxis,
                TagPole intendedPole)
        {
            List<HailAnsweringPairCandidate>
                results =
                    new();

            foreach (
                DemoTapeTrackSnapshot track
                in demoTape.TrackSnapshots)
            {
                foreach (
                    DemoTapeIdeaSnapshot idea
                    in track.IdeaSnapshots)
                {
                    if (idea.PayloadType !=
                        IdeaPayloadType.TagPair)
                    {
                        continue;
                    }

                    DemoTapeTagOccurrenceSnapshot
                        dominant =
                            FindDominant(
                                idea
                            );

                    // Camp 1 currently treats persisted
                    // formal pairs as Intact. When mutable
                    // Pair Integrity enters authoritative
                    // release state, its gate belongs here.

                    if (dominant.Degree ==
                        TagDegree.Neutral)
                    {
                        continue;
                    }

                    if (dominant.Axis !=
                            intendedAxis ||
                        dominant.Pole !=
                            intendedPole)
                    {
                        continue;
                    }

                    results.Add(
                        new HailAnsweringPairCandidate(
                            track.SourceTrackId,
                            idea.SourceIdeaId,
                            idea.IdeaIndex,
                            idea.AspectId,
                            dominant.Axis,
                            dominant.Pole,
                            dominant.Degree
                        )
                    );
                }
            }

            return results;
        }

        private static
            DemoTapeTagOccurrenceSnapshot
            FindDominant(
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

        private static bool
            IsAvailableAtIntentTurn(
                SceneRelease release,
                int intentTurn)
        {
            if (release.ReleasedTurn < 0 ||
                release.ReleasedTurn > intentTurn)
            {
                return false;
            }

            if (release.LifecycleState ==
                SceneReleaseLifecycleState
                    .FailedToFetter)
            {
                return false;
            }

            return true;
        }

        private static void ValidateSource(
            SceneReleaseActivationSource source)
        {
            if (source == null)
            {
                throw new ArgumentException(
                    "Activation discovery cannot " +
                    "contain a null public source."
                );
            }
        }
    }
}