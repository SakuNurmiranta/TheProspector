using System;
using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Pressure;
using SEMM91.GamePlay.Kvlt.Standing;

namespace SEMM91.GamePlay.Kvlt.Scenario.Tests.Editor
{
    public class KvltScenarioProfileTests
    {
        [Test]
        public void
            ValidProfilePreservesAuthoritativeConfiguration()
        {
            KvltScenarioProfile profile =
                Profile();

            Assert.That(
                profile.ProfileId,
                Is.EqualTo(
                    "PEAK2_INTEGRATION_TEST"
                )
            );

            Assert.That(
                profile.Version,
                Is.EqualTo(1)
            );

            Assert.That(
                profile.RequiredKvltPlayerCount,
                Is.EqualTo(5)
            );

            Assert.That(
                profile.TurnsPerYear,
                Is.EqualTo(4)
            );

            Assert.That(
                profile.SharedHappeningWindowSeconds,
                Is.EqualTo(60f)
            );

            Assert.That(
                profile.FreshReleasePosition,
                Is.EqualTo(0.01f)
            );

            Assert.That(
                profile.NexusBoundary,
                Is.EqualTo(1f)
            );

            Assert.That(
                profile.FoundingKeeperSplatId,
                Is.EqualTo("MAYHEM")
            );
        }

        [Test]
        public void
            FieldGeometryRequiresOrderedFringeToNexusTopology()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    CreateProfile(
                        freshReleasePosition:
                            0.01f,
                        innerFieldEntryCeiling:
                            1f,
                        outerBoundary:
                            0f,
                        nexusBoundary:
                            1f
                    )
            );

            Assert.Throws<ArgumentException>(
                () =>
                    CreateProfile(
                        freshReleasePosition:
                            -0.10f,
                        innerFieldEntryCeiling:
                            0.75f,
                        outerBoundary:
                            0f,
                        nexusBoundary:
                            1f
                    )
            );
        }

        [Test]
        public void
            SettlementPolicyUsesScenarioOwnedCalibration()
        {
            KvltScenarioProfile profile =
                Profile();

            var policy =
                profile.CreateSceneSettlementPolicy();

            Assert.That(
                policy.FieldDriftScale,
                Is.EqualTo(
                    profile.FieldDriftScale
                )
            );

            Assert.That(
                policy.BreakthroughDriftMultiplier,
                Is.EqualTo(
                    profile
                        .BreakthroughDriftMultiplier
                )
            );

            Assert.That(
                policy.OuterBoundary,
                Is.EqualTo(
                    profile.OuterBoundary
                )
            );

            Assert.That(
                policy.NexusBoundary,
                Is.EqualTo(
                    profile.NexusBoundary
                )
            );

            Assert.That(
                policy.StandingProjectionPolicy,
                Is.SameAs(
                    profile.StandingProjectionPolicy
                )
            );

            Assert.That(
                policy.PressureRebuildPolicy,
                Is.SameAs(
                    profile.PressureRebuildPolicy
                )
            );

            Assert.That(
                policy.NormativePressureBlendPolicy,
                Is.SameAs(
                    profile
                        .NormativePressureBlendPolicy
                )
            );
        }

        [Test]
        public void
            HistoricalAcceptedTransgressionSeedAllowsPreSessionTurn()
        {
            KvltAcceptedTransgressionSeed seed =
                new(
                    "AT_LOW_BLOOD",
                    "BLOOD_SPECTACLE",
                    TagAxis.Physical,
                    TagPole.Negative,
                    TagDegree.Weak,
                    establishedTurn: -1,
                    sourceId:
                        "PRESESSION_HISTORY"
                );

            Assert.That(
                seed.EstablishedTurn,
                Is.EqualTo(-1)
            );
        }

        [Test]
        public void
            HistoricalHailPraxisSeedAllowsPreSessionTurn()
        {
            KvltHistoricalHailPraxisSeed seed =
                new(
                    "HAIL_PRACTICE",
                    "ASPECT_DEATH",
                    "CORPSE_IMAGERY",
                    establishedTurn: -1
                );

            Assert.That(
                seed.EstablishedTurn,
                Is.EqualTo(-1)
            );
        }

        [Test]
        public void
            DuplicateSocietyPolarityIsRejected()
        {
            KvltSocietyNormSeed duplicate =
                new(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Dominant
                );

            Assert.Throws<ArgumentException>(
                () =>
                    CreateProfile(
                        societyNorms:
                            new[]
                            {
                                duplicate,
                                duplicate
                            }
                    )
            );
        }

        [Test]
        public void
            SeedCollectionsAreDefensivelyCopied()
        {
            CanonPrecedentRecord[] canon =
            {
                new CanonPrecedentRecord(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    CanonProvenanceKind
                        .ScenarioSeed,
                    "TEST_CANON",
                    establishedTurn: -1
                )
            };

            KvltScenarioProfile profile =
                CreateProfile(
                    canon:
                        canon
                );

            canon[0] =
                new CanonPrecedentRecord(
                    TagAxis.Physical,
                    TagPole.Positive,
                    TagDegree.Transgressive,
                    CanonProvenanceKind
                        .ScenarioSeed,
                    "MUTATED_EXTERNAL_ARRAY",
                    establishedTurn: -1
                );

            Assert.That(
                profile.StartingCanon[0].Axis,
                Is.EqualTo(
                    TagAxis.Symbolic
                )
            );

            Assert.That(
                profile.StartingCanon[0]
                    .SourceArtifactId,
                Is.EqualTo(
                    "TEST_CANON"
                )
            );
        }

        [Test]
        public void
            PullRegenUsesRemainingPullRatherThanFlatGrant()
        {
            KvltPullEconomyProfile pull =
                Pull();

            Assert.That(
                pull.CalculatePassiveRegen(
                    8f
                ),
                Is.EqualTo(2f)
                    .Within(0.0001f)
            );

            Assert.That(
                pull.CalculatePassiveRegen(
                    2f
                ),
                Is.EqualTo(0.5f)
                    .Within(0.0001f)
            );
        }

        private static KvltScenarioProfile Profile()
        {
            return CreateProfile();
        }

        private static KvltScenarioProfile
            CreateProfile(
                float freshReleasePosition =
                    0.01f,
                float innerFieldEntryCeiling =
                    0.75f,
                float outerBoundary =
                    0f,
                float nexusBoundary =
                    1f,
                CanonPrecedentRecord[] canon =
                    null,
                KvltSocietyNormSeed[]
                    societyNorms =
                    null)
        {
            canon ??=
                new[]
                {
                    new CanonPrecedentRecord(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Weak,
                        CanonProvenanceKind
                            .ScenarioSeed,
                        "TEST_CANON",
                        establishedTurn: -1
                    )
                };

            societyNorms ??=
                new[]
                {
                    new KvltSocietyNormSeed(
                        TagAxis.Symbolic,
                        TagPole.Positive,
                        TagDegree.Transgressive
                    )
                };

            return new KvltScenarioProfile(
                profileId:
                    "PEAK2_INTEGRATION_TEST",

                version:
                    1,

                requiredKvltPlayerCount:
                    5,

                turnsPerYear:
                    4,

                sharedHappeningWindowSeconds:
                    60f,

                freshReleasePosition:
                    freshReleasePosition,

                innerFieldEntryCeiling:
                    innerFieldEntryCeiling,

                outerBoundary:
                    outerBoundary,

                nexusBoundary:
                    nexusBoundary,

                fieldDriftScale:
                    1f,

                breakthroughDriftMultiplier:
                    0.60f,

                initialSceneDamage:
                    0f,

                foundingKeeperSplatId:
                    "MAYHEM",

                startingKeeperPull:
                    2f,

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
                    Pull(),

                sourceStartingCanon:
                    canon,

                sourceStartingSocietyNorms:
                    societyNorms,

                sourceStartingAcceptedTransgressions:
                    new[]
                    {
                        new
                            KvltAcceptedTransgressionSeed(
                                "AT_TEST",
                                "LOW_SPECTACLE",
                                TagAxis.Physical,
                                TagPole.Negative,
                                TagDegree.Weak,
                                -1,
                                "TEST_HISTORY"
                            )
                    },

                sourceStartingHailPraxisHistory:
                    new[]
                    {
                        new
                            KvltHistoricalHailPraxisSeed(
                                "HAIL_TEST",
                                "ASPECT_DEATH",
                                "LOW_SPECTACLE",
                                -1
                            )
                    }
            );
        }

        private static KvltPullEconomyProfile Pull()
        {
            /*
             * Test calibration only.
             * Production provisional values are
             * introduced with the real thesis profile.
             */
            return new KvltPullEconomyProfile(
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
                    1f,
                poserClearCost:
                    1f,
                poserAccusationCost:
                    2f,
                failedAccusationCompensationCost:
                    1f
            );
        }
    }
}