using System;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Pressure;
using SEMM91.GamePlay.Kvlt.Standing;

namespace SEMM91.GamePlay.Kvlt.Scenario
{
    public static class
        Peak2KvltScenarioProfileFactory
    {
        public const string ProfileId =
            "PEAK2_DEFAULT";

        public const string FoundingSplatId =
            "MAYHEM";

        public const string FoundingDemoTapeId =
            "HISTORICAL_DEMO_MAYHEM_FREEZING_MOON";

        public static KvltScenarioProfile
            CreateDefault()
        {
            return new KvltScenarioProfile(
                profileId:
                    ProfileId,

                version:
                    1,

                requiredKvltPlayerCount:
                    5,

                turnsPerYear:
                    4,

                sharedHappeningWindowSeconds:
                    60f,

                freshReleasePosition:
                    0.01f,

                innerFieldEntryCeiling:
                    0.75f,

                outerBoundary:
                    0f,

                nexusBoundary:
                    1f,

                fieldDriftScale:
                    1f,

                breakthroughDriftMultiplier:
                    0.60f,

                initialSceneDamage:
                    0f,

                foundingKeeperSplatId:
                    FoundingSplatId,

                /*
                 * Provisional Peak-2 calibration.
                 * The economy remains scenario-owned.
                 */
                startingKeeperPull:
                    4f,

                standingProjectionPolicy:
                    new SceneStandingProjectionPolicy(
                        canonLegacyBaseWeight:
                            1f,
                        canonLegacyBreakthroughDegreeWeight:
                            1f,
                        rejectionScarBaseWeight:
                            1f,
                        rejectionPeakPenetrationWeight:
                            1f,
                        rejectionOutwardOvershootWeight:
                            1f
                    ),

                pressureRebuildPolicy:
                    new ScenePressureRebuildPolicy(
                        neutralDirectionScale:
                            0.25f,
                        counterCanonicalScale:
                            0.50f,
                        maxAbsoluteEffectivePressure:
                            0.75f
                    ),

                normativePressureBlendPolicy:
                    new NormativePressureBlendPolicy(
                        provisionalAffinityCeiling:
                            0.50f
                    ),

                pullEconomy:
                    new KvltPullEconomyProfile(
                        gravityDripRate:
                            0.25f,
                        winningHailGrant:
                            1f,
                        successfulHappeningGrant:
                            1f,
                        pullRegenRate:
                            0.25f,
                        releaseStabilizationCost:
                            1f,
                        sceneDamageHealCost:
                            2f,
                        poserClearCost:
                            2f,
                        poserAccusationCost:
                            3f,
                        failedAccusationCompensationCost:
                            2f
                    ),

                /*
                 * These describe the state that is true
                 * when turn 0 begins.
                 *
                 * They are not simulated negative-turn
                 * events.
                 */
                sourceStartingCanon:
                    new[]
                    {
                        StartingCanon(
                            TagAxis.Symbolic
                        ),

                        StartingCanon(
                            TagAxis.Existential
                        ),

                        StartingCanon(
                            TagAxis.Physical
                        )
                    },

                sourceStartingSocietyNorms:
                    new[]
                    {
                        new KvltSocietyNormSeed(
                            TagAxis.Symbolic,
                            TagPole.Positive,
                            TagDegree.Transgressive
                        ),

                        new KvltSocietyNormSeed(
                            TagAxis.Existential,
                            TagPole.Positive,
                            TagDegree.Dominant
                        ),

                        new KvltSocietyNormSeed(
                            TagAxis.Physical,
                            TagPole.Positive,
                            TagDegree.Dominant
                        )
                    },

                /*
                 * We will seed actual runtime praxis
                 * history when the Happening runtime
                 * consumes this profile.
                 */
                sourceStartingAcceptedTransgressions:
                    Array.Empty<
                        KvltAcceptedTransgressionSeed>(),

                sourceStartingHailPraxisHistory:
                    Array.Empty<
                        KvltHistoricalHailPraxisSeed>()
            );
        }

        private static CanonPrecedentRecord
            StartingCanon(
                TagAxis axis)
        {
            return new CanonPrecedentRecord(
                axis,
                TagPole.Negative,
                TagDegree.Weak,
                CanonProvenanceKind.ScenarioSeed,
                FoundingDemoTapeId,

                /*
                 * Exact Track/Idea provenance is derived
                 * from the actual authored DemoTape in
                 * Entry 6 rather than duplicated here.
                 */
                sourceTrackId:
                    null,

                sourceIdeaId:
                    null,

                establishedTurn:
                    0
            );
        }
    }
}