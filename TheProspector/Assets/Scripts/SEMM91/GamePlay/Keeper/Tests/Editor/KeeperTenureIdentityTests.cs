using NUnit.Framework;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Keeper.Tests.Editor
{
    public class KeeperTenureIdentityTests
    {
        private KeeperLegacyResolver
            legacyResolver;

        private SeededWorldState
            world;

        [SetUp]
        public void SetUp()
        {
            legacyResolver =
                new KeeperLegacyResolver();

            /*
             * No SceneRelease fixtures are necessary.
             * These tests exercise continuous office
             * identity only, not legacy canonization.
             */
            world =
                new SeededWorldState(
                    null
                );
        }

        [Test]
        public void
            NewKeeperTenure_HasPersistentIdentity()
        {
            KeeperTenureState tenure =
                KeeperTenureState.Create(
                    keeperClientId: 1,
                    startedRound: 1
                );

            Assert.That(
                tenure.KeeperTenureId,
                Is.Not.Null
            );

            Assert.That(
                tenure.KeeperTenureId,
                Is.Not.Empty
            );
        }

        [Test]
        public void
            OrdinaryTenureStateEvolution_PreservesIdentity()
        {
            KeeperTenureState original =
                new KeeperTenureState(
                    keeperClientId: 1,
                    startedRound: 1,
                    canonizationSubjectReleaseId:
                        string.Empty,
                    subjectTenureYears: 0,
                    pull: 2f,
                    keeperTenureId:
                        "TENURE_A"
                );

            KeeperTenureState withSubject =
                original.WithCanonizationSubject(
                    "RELEASE"
                );

            KeeperTenureState advanced =
                withSubject
                    .AdvanceCanonizationSubjectYear();

            KeeperTenureState withPull =
                advanced.AddPull(
                    1f
                );

            Assert.That(
                withPull.TrySpendPull(
                    1f,
                    out
                        KeeperTenureState
                        afterSpend
                ),
                Is.True
            );

            KeeperTenureState
                afterIntervention =
                    afterSpend.RecordIntervention(
                        KeeperInterventionType
                            .BoostVisibility,
                        turn: 5
                    );

            Assert.That(
                withSubject.KeeperTenureId,
                Is.EqualTo("TENURE_A")
            );

            Assert.That(
                advanced.KeeperTenureId,
                Is.EqualTo("TENURE_A")
            );

            Assert.That(
                withPull.KeeperTenureId,
                Is.EqualTo("TENURE_A")
            );

            Assert.That(
                afterSpend.KeeperTenureId,
                Is.EqualTo("TENURE_A")
            );

            Assert.That(
                afterIntervention.KeeperTenureId,
                Is.EqualTo("TENURE_A")
            );
        }

        [Test]
        public void
            IncumbentKeeperReselection_PreservesTenureIdentity()
        {
            KeeperLegacyResolution initial =
                legacyResolver.Resolve(
                    Transition(
                        KeeperTransitionReason
                            .InitialAssignment,
                        previousKeeper:
                            ulong.MaxValue,
                        nextKeeper:
                            1,
                        round:
                            1
                    ),
                    currentTenure:
                        null,
                    world,
                    nextKeeperOwnerEntityId:
                        "OWNER_1"
                );

            string originalTenureId =
                initial.NextTenure
                    .KeeperTenureId;

            KeeperLegacyResolution retained =
                legacyResolver.Resolve(
                    Transition(
                        KeeperTransitionReason
                            .YearEndRetained,
                        previousKeeper:
                            1,
                        nextKeeper:
                            1,
                        round:
                            2
                    ),
                    initial.NextTenure,
                    world,
                    nextKeeperOwnerEntityId:
                        "OWNER_1"
                );

            Assert.That(
                retained.NextTenure
                    .KeeperClientId,
                Is.EqualTo(1)
            );

            Assert.That(
                retained.NextTenure
                    .KeeperTenureId,
                Is.EqualTo(
                    originalTenureId
                )
            );
        }

        [Test]
        public void
            DifferentKeeperTakingOffice_StartsNewTenureIdentity()
        {
            KeeperLegacyResolution initial =
                legacyResolver.Resolve(
                    Transition(
                        KeeperTransitionReason
                            .InitialAssignment,
                        ulong.MaxValue,
                        1,
                        1
                    ),
                    null,
                    world,
                    "OWNER_1"
                );

            KeeperLegacyResolution replacement =
                legacyResolver.Resolve(
                    Transition(
                        KeeperTransitionReason
                            .YearEndReplaced,
                        1,
                        2,
                        2
                    ),
                    initial.NextTenure,
                    world,
                    "OWNER_2"
                );

            Assert.That(
                replacement.NextTenure
                    .KeeperClientId,
                Is.EqualTo(2)
            );

            Assert.That(
                replacement.NextTenure
                    .KeeperTenureId,
                Is.Not.EqualTo(
                    initial.NextTenure
                        .KeeperTenureId
                )
            );
        }

        [Test]
        public void
            FormerKeeperReturningLater_GetsNewTenureIdentity()
        {
            KeeperLegacyResolution firstTenure =
                legacyResolver.Resolve(
                    Transition(
                        KeeperTransitionReason
                            .InitialAssignment,
                        ulong.MaxValue,
                        1,
                        1
                    ),
                    null,
                    world,
                    "OWNER_1"
                );

            KeeperLegacyResolution secondTenure =
                legacyResolver.Resolve(
                    Transition(
                        KeeperTransitionReason
                            .YearEndReplaced,
                        1,
                        2,
                        2
                    ),
                    firstTenure.NextTenure,
                    world,
                    "OWNER_2"
                );

            KeeperLegacyResolution returningKeeper =
                legacyResolver.Resolve(
                    Transition(
                        KeeperTransitionReason
                            .YearEndReplaced,
                        2,
                        1,
                        3
                    ),
                    secondTenure.NextTenure,
                    world,
                    "OWNER_1"
                );

            Assert.That(
                returningKeeper.NextTenure
                    .KeeperClientId,
                Is.EqualTo(1)
            );

            Assert.That(
                returningKeeper.NextTenure
                    .KeeperTenureId,
                Is.Not.EqualTo(
                    firstTenure.NextTenure
                        .KeeperTenureId
                )
            );

            Assert.That(
                returningKeeper.NextTenure
                    .KeeperTenureId,
                Is.Not.EqualTo(
                    secondTenure.NextTenure
                        .KeeperTenureId
                )
            );
        }

        private static KeeperTransitionResult
            Transition(
                KeeperTransitionReason reason,
                ulong previousKeeper,
                ulong nextKeeper,
                int round)
        {
            return new KeeperTransitionResult(
                resolvedRound:
                    round,
                reason,
                previousKeeperClientId:
                    previousKeeper,
                nextKeeperClientId:
                    nextKeeper,
                previousSubjectReleaseId:
                    string.Empty,
                canonizedReleaseId:
                    string.Empty,
                incomingSubjectReleaseId:
                    string.Empty,
                winningSceneOutput:
                    1f,
                pullGrant:
                    0f
            );
        }
    }
}