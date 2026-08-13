using System;
using System.Collections.Generic;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Kvlt.Scenario
{
    /// <summary>
    /// Applies the authoritative semantic starting state
    /// of one configured KVLT scenario.
    ///
    /// This is initial-state construction, not a replay
    /// of events that happened before turn 0.
    ///
    /// Canon provenance is resolved from the actual
    /// authored DemoTape rather than duplicated in
    /// Scenario configuration.
    /// </summary>
    public sealed class
        KvltStartingWorldStateBootstrapper
    {
        public void Apply(
            KvltScenarioProfile profile,
            SeededWorldState worldState,
            DemoTape foundingDemoTape,
            int startingTurn)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(
                    nameof(profile)
                );
            }

            if (worldState == null)
            {
                throw new ArgumentNullException(
                    nameof(worldState)
                );
            }

            if (foundingDemoTape == null)
            {
                throw new ArgumentNullException(
                    nameof(foundingDemoTape)
                );
            }

            if (startingTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingTurn)
                );
            }

            /*
             * Initial-state bootstrap is one-shot.
             *
             * We do not merge scenario seeds into an
             * already interpreted Scene.
             */
            if (worldState.KvltCanon.Records.Count != 0)
            {
                throw new InvalidOperationException(
                    "Starting KVLT state cannot be applied " +
                    "over existing Canon."
                );
            }

            if (worldState.SocietyNorms.Count != 0)
            {
                throw new InvalidOperationException(
                    "Starting KVLT state cannot be applied " +
                    "over existing Society norms."
                );
            }

            /*
             * Resolve every configured Canon declaration
             * against an actual formal-pair dominant on
             * the founding DemoTape BEFORE mutating the
             * world.
             *
             * This prevents partial bootstrap if authored
             * data and Scenario configuration disagree.
             */
            List<
                (
                    CanonPrecedentRecord Seed,
                    string TrackId,
                    string IdeaId
                )>
                resolvedCanon =
                    new();

            foreach (
                CanonPrecedentRecord seed
                in profile.StartingCanon)
            {
                if (seed == null)
                {
                    throw new InvalidOperationException(
                        "Starting Canon contains a null seed."
                    );
                }

                if (seed.ProvenanceKind !=
                    CanonProvenanceKind.ScenarioSeed)
                {
                    throw new InvalidOperationException(
                        "Starting Canon must use " +
                        "ScenarioSeed provenance."
                    );
                }

                if (seed.SourceArtifactId !=
                    foundingDemoTape.DemoTapeId)
                {
                    throw new InvalidOperationException(
                        "Starting Canon references the wrong " +
                        "founding artifact | " +
                        $"expected={foundingDemoTape.DemoTapeId} | " +
                        $"actual={seed.SourceArtifactId}"
                    );
                }

                ResolveUniqueFormalPairDominant(
                    foundingDemoTape,
                    seed,
                    out string trackId,
                    out string ideaId
                );

                /*
                 * If configuration already carries exact
                 * provenance, it must agree with the
                 * authored artifact.
                 *
                 * Null remains valid because the runtime
                 * bootstrap is allowed to derive it.
                 */
                if (seed.SourceTrackId != null &&
                    seed.SourceTrackId != trackId)
                {
                    throw new InvalidOperationException(
                        "Configured starting Canon Track " +
                        "provenance disagrees with the " +
                        "founding DemoTape."
                    );
                }

                if (seed.SourceIdeaId != null &&
                    seed.SourceIdeaId != ideaId)
                {
                    throw new InvalidOperationException(
                        "Configured starting Canon Idea " +
                        "provenance disagrees with the " +
                        "founding DemoTape."
                    );
                }

                resolvedCanon.Add(
                    (
                        seed,
                        trackId,
                        ideaId
                    )
                );
            }

            /*
             * All structural validation is complete.
             * Commit Society first.
             */
            foreach (
                KvltSocietyNormSeed societySeed
                in profile.StartingSocietyNorms)
            {
                if (!worldState
                        .SocietyNorms
                        .SetNormativeDegree(
                            societySeed.Axis,
                            societySeed.Pole,
                            societySeed.Degree
                        ))
                {
                    throw new InvalidOperationException(
                        "Starting Society norm could not " +
                        "be installed."
                    );
                }
            }

            /*
             * Install exact Canon provenance derived from
             * the actual recorded artifact.
             *
             * startingTurn is authoritative. We do not
             * use a narrative negative-turn prehistory.
             */
            foreach (
                var resolved
                in resolvedCanon)
            {
                CanonPrecedentRecord seed =
                    resolved.Seed;

                bool added =
                    worldState.KvltCanon
                        .TryRecordPrecedent(
                            seed.Axis,
                            seed.Pole,
                            seed.Degree,
                            CanonProvenanceKind
                                .ScenarioSeed,
                            foundingDemoTape.DemoTapeId,
                            resolved.TrackId,
                            resolved.IdeaId,
                            establishedTurn:
                                startingTurn
                        );

                if (!added)
                {
                    throw new InvalidOperationException(
                        "Resolved starting Canon could not " +
                        "be installed | " +
                        $"axis={seed.Axis} | " +
                        $"pole={seed.Pole} | " +
                        $"degree={seed.Degree}"
                    );
                }
            }

            /*
             * Initial Pressure is empty, so the first
             * authoritative Normative Centre is the
             * Canon-only interpretation.
             */
            NormativeCentre centre =
                new CanonicalNormativeCentreDeriver()
                    .Derive(
                        worldState.KvltCanon
                    );

            worldState.ApplySettledNormativeCentre(
                centre
            );
        }

        private static void
            ResolveUniqueFormalPairDominant(
                DemoTape demoTape,
                CanonPrecedentRecord seed,
                out string sourceTrackId,
                out string sourceIdeaId)
        {
            sourceTrackId =
                null;

            sourceIdeaId =
                null;

            int matches =
                0;

            foreach (
                DemoTapeTrackSnapshot track
                in demoTape.TrackSnapshots)
            {
                foreach (
                    DemoTapeIdeaSnapshot idea
                    in track.IdeaSnapshots)
                {
                    /*
                     * Starting Canon must come from the
                     * dominant side of an intact formal
                     * pair.
                     *
                     * A matching solitary Tag is not a
                     * canonical source claim.
                     */
                    if (idea.PayloadType !=
                        IdeaPayloadType.TagPair)
                    {
                        continue;
                    }

                    if (idea.TagOccurrences.Count != 2)
                    {
                        continue;
                    }

                    DemoTapeTagOccurrenceSnapshot
                        dominant =
                            idea.TagOccurrences[0];

                    if (dominant.Role !=
                        DemoTapeTagOccurrenceRole
                            .PairDominant)
                    {
                        continue;
                    }

                    if (dominant.Axis !=
                            seed.Axis ||
                        dominant.Pole !=
                            seed.Pole ||
                        dominant.Degree !=
                            seed.Degree)
                    {
                        continue;
                    }

                    matches++;

                    sourceTrackId =
                        track.SourceTrackId;

                    sourceIdeaId =
                        idea.SourceIdeaId;
                }
            }

            if (matches != 1)
            {
                throw new InvalidOperationException(
                    "Starting Canon must resolve to " +
                    "exactly one formal-pair dominant on " +
                    "the founding DemoTape | " +
                    $"axis={seed.Axis} | " +
                    $"pole={seed.Pole} | " +
                    $"degree={seed.Degree} | " +
                    $"matches={matches}"
                );
            }
        }
    }
}