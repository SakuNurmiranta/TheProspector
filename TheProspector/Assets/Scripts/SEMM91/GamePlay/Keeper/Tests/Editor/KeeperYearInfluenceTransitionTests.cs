using NUnit.Framework;

namespace SEMM91.GamePlay.Keeper.Tests.Editor
{
    public sealed class
        KeeperYearInfluenceTransitionTests
    {
        private readonly
            KeeperTransitionResolver
            resolver =
                new();

        [Test]
        public void
            EqualInfluenceChallengerCannotDethroneIncumbentEvenWithHigherStanding()
        {
            KeeperTransitionResult result =
                resolver.ResolveYearEnd(
                    resolvedRound:
                        2,
                    currentKeeperClientId:
                        1,
                    new[]
                    {
                        Candidate(
                            clientId:
                                1,
                            owner:
                                "INCUMBENT",
                            influence:
                                4f,
                            standing:
                                0.20f
                        ),

                        Candidate(
                            clientId:
                                2,
                            owner:
                                "CHALLENGER",
                            influence:
                                4f,
                            standing:
                                0.95f
                        )
                    }
                );

            Assert.That(
                result.Reason,
                Is.EqualTo(
                    KeeperTransitionReason
                        .YearEndRetained
                )
            );

            Assert.That(
                result.NextKeeperClientId,
                Is.EqualTo(1ul)
            );

            Assert.That(
                result.WinningInfluence,
                Is.EqualTo(4f)
            );
        }

        [Test]
        public void
            TiedWinningChallengersUseSceneStanding()
        {
            KeeperTransitionResult result =
                resolver.ResolveYearEnd(
                    resolvedRound:
                        2,
                    currentKeeperClientId:
                        1,
                    new[]
                    {
                        Candidate(
                            1,
                            "INCUMBENT",
                            2f,
                            0.90f
                        ),

                        Candidate(
                            2,
                            "CHALLENGER_A",
                            5f,
                            0.25f
                        ),

                        Candidate(
                            3,
                            "CHALLENGER_B",
                            5f,
                            0.75f
                        )
                    }
                );

            Assert.That(
                result.Reason,
                Is.EqualTo(
                    KeeperTransitionReason
                        .YearEndReplaced
                )
            );

            Assert.That(
                result.NextKeeperClientId,
                Is.EqualTo(3ul)
            );

            Assert.That(
                result.WinningInfluence,
                Is.EqualTo(5f)
            );
        }

        [Test]
        public void
            FullyTiedChallengersUseLowestClientId()
        {
            KeeperTransitionResult result =
                resolver.ResolveYearEnd(
                    resolvedRound:
                        2,
                    currentKeeperClientId:
                        9,
                    new[]
                    {
                        Candidate(
                            9,
                            "INCUMBENT",
                            1f,
                            0.50f
                        ),

                        Candidate(
                            5,
                            "CHALLENGER_A",
                            3f,
                            0.75f
                        ),

                        Candidate(
                            2,
                            "CHALLENGER_B",
                            3f,
                            0.75f
                        )
                    }
                );

            Assert.That(
                result.Reason,
                Is.EqualTo(
                    KeeperTransitionReason
                        .YearEndReplaced
                )
            );

            Assert.That(
                result.NextKeeperClientId,
                Is.EqualTo(2ul)
            );
        }

        [Test]
        public void
            IneligibleHighestInfluenceChallengerIsSkipped()
        {
            KeeperTransitionResult result =
                resolver.ResolveYearEnd(
                    resolvedRound:
                        2,
                    currentKeeperClientId:
                        1,
                    new[]
                    {
                        Candidate(
                            1,
                            "INCUMBENT",
                            2f,
                            0.40f
                        ),

                        Candidate(
                            2,
                            "INELIGIBLE",
                            100f,
                            1.00f,
                            isEligible:
                                false
                        ),

                        Candidate(
                            3,
                            "ELIGIBLE",
                            3f,
                            0.20f
                        )
                    }
                );

            Assert.That(
                result.Reason,
                Is.EqualTo(
                    KeeperTransitionReason
                        .YearEndReplaced
                )
            );

            Assert.That(
                result.NextKeeperClientId,
                Is.EqualTo(3ul)
            );

            Assert.That(
                result.WinningInfluence,
                Is.EqualTo(3f)
            );
        }

        [Test]
        public void
            AllChallengersIneligibleLeavesIncumbentInOffice()
        {
            KeeperTransitionResult result =
                resolver.ResolveYearEnd(
                    resolvedRound:
                        2,
                    currentKeeperClientId:
                        1,
                    new[]
                    {
                        Candidate(
                            1,
                            "INCUMBENT",
                            1f,
                            0.10f
                        ),

                        Candidate(
                            2,
                            "POSER_A",
                            50f,
                            1.00f,
                            isEligible:
                                false
                        ),

                        Candidate(
                            3,
                            "POSER_B",
                            60f,
                            1.00f,
                            isEligible:
                                false
                        )
                    }
                );

            Assert.That(
                result.Reason,
                Is.EqualTo(
                    KeeperTransitionReason
                        .YearEndRetained
                )
            );

            Assert.That(
                result.KeeperChanged,
                Is.False
            );

            Assert.That(
                result.NextKeeperClientId,
                Is.EqualTo(1ul)
            );

            Assert.That(
                result.WinningInfluence,
                Is.EqualTo(1f)
            );
        }

        private static KeeperCandidate Candidate(
            ulong clientId,
            string owner,
            float influence,
            float? standing,
            bool isEligible = true)
        {
            return new KeeperCandidate(
                clientId,
                owner,
                yearInfluence:
                    influence,
                sceneStanding:
                    standing,
                isEligible:
                    isEligible
            );
        }
    }
}