using System;
using System.Collections.Generic;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;

namespace SEMM91.GamePlay.Kvlt.Pressure
{
    /// <summary>
    /// Rebuilds Dynamic Scene Pressure from scratch
    /// using the surviving non-canon Field corpus.
    ///
    /// Pressure is based on immutable recorded
    /// semantics. Activation is deliberately not an
    /// input.
    /// </summary>
    public sealed class ScenePressureRebuilder
    {
        public ScenePressureRebuildEvaluation Rebuild(
            string sceneId,
            int settledTurn,
            IReadOnlyList<SceneRelease> releases,
            IReadOnlyList<DemoTape> demoTapes,
            CanonState canon,
            ScenePressureRebuildPolicy policy)
        {
            sceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
                );

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            if (releases == null)
            {
                throw new ArgumentNullException(
                    nameof(releases)
                );
            }

            if (demoTapes == null)
            {
                throw new ArgumentNullException(
                    nameof(demoTapes)
                );
            }

            if (canon == null)
            {
                throw new ArgumentNullException(
                    nameof(canon)
                );
            }

            if (policy == null)
            {
                throw new ArgumentNullException(
                    nameof(policy)
                );
            }

            Dictionary<string, DemoTape>
                tapesById =
                    BuildTapeLookup(
                        demoTapes
                    );

            HashSet<string> seenReleaseIds =
                new(
                    StringComparer.Ordinal
                );

            List<ScenePressureSourceContribution>
                contributions =
                    new();

            foreach (
                SceneRelease release
                in releases)
            {
                if (release == null)
                {
                    throw new ArgumentException(
                        "Pressure release population " +
                        "cannot contain null.",
                        nameof(releases)
                    );
                }

                if (!seenReleaseIds.Add(
                        release.ReleaseId))
                {
                    throw new ArgumentException(
                        "Pressure release population " +
                        "contains duplicate identity.",
                        nameof(releases)
                    );
                }

                if (!string.Equals(
                        release.HostedSceneNodeId,
                        sceneId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                /*
                 * Exact lifecycle gate.
                 *
                 * Fringe, FailedToFetter, Rejected,
                 * CanonRetained and HistoricalCanon
                 * contribute no Dynamic Scene Pressure.
                 */
                if (release.LifecycleState !=
                    SceneReleaseLifecycleState.Field)
                {
                    continue;
                }

                if (!tapesById.TryGetValue(
                        release.SourceDemoTapeId,
                        out DemoTape demoTape))
                {
                    throw new InvalidOperationException(
                        "Field SceneRelease has no source " +
                        "DemoTape for Pressure rebuild | " +
                        $"release={release.ReleaseId} | " +
                        $"demo={release.SourceDemoTapeId}"
                    );
                }

                ProjectRelease(
                    sceneId,
                    settledTurn,
                    release,
                    demoTape,
                    contributions
                );
            }

            contributions.Sort(
                CompareContributions
            );

            Dictionary<
                    (TagAxis Axis, TagPole Pole),
                    float>
                rawByTag =
                    new();

            foreach (
                ScenePressureSourceContribution
                    contribution
                in contributions)
            {
                var key =
                    (
                        contribution.Axis,
                        contribution.Pole
                    );

                rawByTag.TryGetValue(
                    key,
                    out float current
                );

                rawByTag[key] =
                    current +
                    contribution
                        .SignedRawContribution;
            }

            List<ScenePressureEntry> entries =
                new();

            foreach (
                TagAxis axis
                in Enum.GetValues(
                    typeof(TagAxis)))
            {
                foreach (
                    TagPole pole
                    in Enum.GetValues(
                        typeof(TagPole)))
                {
                    rawByTag.TryGetValue(
                        (axis, pole),
                        out float raw
                    );

                    if (raw == 0f)
                    {
                        continue;
                    }

                    float effective =
                        policy.ResolveEffectivePressure(
                            canon,
                            axis,
                            pole,
                            raw
                        );

                    entries.Add(
                        new ScenePressureEntry(
                            axis,
                            pole,
                            raw,
                            effective
                        )
                    );
                }
            }

            return new ScenePressureRebuildEvaluation(
                sceneId,
                settledTurn,
                new SettledScenePressure(
                    entries
                ),
                contributions
            );
        }

        private static void ProjectRelease(
            string sceneId,
            int settledTurn,
            SceneRelease release,
            DemoTape demoTape,
            List<
                ScenePressureSourceContribution>
                output)
        {
            foreach (
                DemoTapeTrackSnapshot track
                in demoTape.TrackSnapshots)
            {
                foreach (
                    DemoTapeIdeaSnapshot idea
                    in track.IdeaSnapshots)
                {
                    foreach (
                        DemoTapeTagOccurrenceSnapshot
                            occurrence
                        in idea.TagOccurrences)
                    {
                        if (occurrence.Degree ==
                            TagDegree.Neutral)
                        {
                            continue;
                        }

                        float sign =
                            occurrence.Role ==
                            DemoTapeTagOccurrenceRole
                                .PairSubmissive
                                ? -1f
                                : 1f;

                        output.Add(
                            new
                                ScenePressureSourceContribution(
                                    sceneId,
                                    settledTurn,
                                    release.ReleaseId,
                                    release.SourceDemoTapeId,
                                    release
                                        .SourceOwnerEntityId,
                                    track.SourceTrackId,
                                    idea.SourceIdeaId,
                                    idea.IdeaIndex,
                                    occurrence.Axis,
                                    occurrence.Pole,
                                    occurrence.Degree,
                                    occurrence.Role,
                                    sign *
                                    (int)occurrence.Degree
                                )
                        );
                    }
                }
            }
        }

        private static Dictionary<string, DemoTape>
            BuildTapeLookup(
                IReadOnlyList<DemoTape> demoTapes)
        {
            Dictionary<string, DemoTape> result =
                new(
                    StringComparer.Ordinal
                );

            foreach (
                DemoTape tape
                in demoTapes)
            {
                if (tape == null)
                {
                    throw new ArgumentException(
                        "Pressure DemoTape population " +
                        "cannot contain null.",
                        nameof(demoTapes)
                    );
                }

                if (!result.TryAdd(
                        tape.DemoTapeId,
                        tape))
                {
                    throw new ArgumentException(
                        "Pressure DemoTape population " +
                        "contains duplicate identity.",
                        nameof(demoTapes)
                    );
                }
            }

            return result;
        }

        private static int CompareContributions(
            ScenePressureSourceContribution left,
            ScenePressureSourceContribution right)
        {
            int release =
                string.CompareOrdinal(
                    left.SceneReleaseId,
                    right.SceneReleaseId
                );

            if (release != 0)
            {
                return release;
            }

            int track =
                string.CompareOrdinal(
                    left.SourceTrackId,
                    right.SourceTrackId
                );

            if (track != 0)
            {
                return track;
            }

            int ideaIndex =
                left.IdeaIndex.CompareTo(
                    right.IdeaIndex
                );

            if (ideaIndex != 0)
            {
                return ideaIndex;
            }

            int idea =
                string.CompareOrdinal(
                    left.SourceIdeaId,
                    right.SourceIdeaId
                );

            if (idea != 0)
            {
                return idea;
            }

            return left.Role.CompareTo(
                right.Role
            );
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Scene identity cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}