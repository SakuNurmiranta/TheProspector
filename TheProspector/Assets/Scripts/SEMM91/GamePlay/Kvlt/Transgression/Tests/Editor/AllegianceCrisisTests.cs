using System;
using NUnit.Framework;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Transgression.Tests
{
    public class AllegianceCrisisTests
    {
        [Test]
        public void TriggeringActorCannotBeEligibleVoter()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    new AllegianceCrisis(
                        Question(),
                        new[]
                        {
                            "TRANSGRESSOR",
                            "PLAYER_B"
                        },
                        "PLAYER_B"
                    )
            );
        }

        [Test]
        public void OnlyEligibleCharactersVoteAndEachVotesOnce()
        {
            AllegianceCrisis crisis =
                Crisis(
                    "KEEPER",
                    "PLAYER_B"
                );

            Assert.That(
                crisis.TryCastVote(
                    Vote(
                        "OUTSIDER",
                        AllegianceChoice.Kvlt
                    )
                ),
                Is.False
            );

            Assert.That(
                crisis.TryCastVote(
                    Vote(
                        "KEEPER",
                        AllegianceChoice.Kvlt
                    )
                ),
                Is.True
            );

            Assert.That(
                crisis.TryCastVote(
                    Vote(
                        "KEEPER",
                        AllegianceChoice.Society
                    )
                ),
                Is.False
            );

            Assert.That(
                crisis.Votes.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void CrisisCannotResolveUntilEveryEligibleVoteExists()
        {
            AllegianceCrisis crisis =
                Crisis(
                    "KEEPER",
                    "PLAYER_B"
                );

            Assert.That(
                crisis.TryCastVote(
                    Vote(
                        "KEEPER",
                        AllegianceChoice.Kvlt
                    )
                ),
                Is.True
            );

            Assert.That(
                crisis.TryResolve(
                    9,
                    out _
                ),
                Is.False
            );

            Assert.That(
                crisis.IsOpen,
                Is.True
            );
        }

        [Test]
        public void UnanimousKvltProducesKvltOutcomeAndZeroFracture()
        {
            AllegianceCrisis crisis =
                Crisis(
                    "KEEPER",
                    "PLAYER_B",
                    "PLAYER_C"
                );

            VoteAll(
                crisis,
                AllegianceChoice.Kvlt
            );

            Assert.That(
                crisis.TryResolve(
                    9,
                    out AllegianceCrisisResolution
                        result
                ),
                Is.True
            );

            Assert.That(
                result.Outcome,
                Is.EqualTo(
                    AllegianceCrisisOutcome.Kvlt
                )
            );

            Assert.That(
                result.CohesionFracture,
                Is.EqualTo(0f)
            );

            Assert.That(
                result.KvltVoteCount,
                Is.EqualTo(3)
            );

            Assert.That(
                result.WeightedKvltVoteCount,
                Is.EqualTo(4)
            );
        }

        [Test]
        public void UnanimousSocietyProducesSocietyOutcomeAndZeroFracture()
        {
            AllegianceCrisis crisis =
                Crisis(
                    "KEEPER",
                    "PLAYER_B",
                    "PLAYER_C"
                );

            VoteAll(
                crisis,
                AllegianceChoice.Society
            );

            Assert.That(
                crisis.TryResolve(
                    9,
                    out AllegianceCrisisResolution
                        result
                ),
                Is.True
            );

            Assert.That(
                result.Outcome,
                Is.EqualTo(
                    AllegianceCrisisOutcome.Society
                )
            );

            Assert.That(
                result.CohesionFracture,
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void CohesionFractureUsesHeadcountNotKeeperWeight()
        {
            AllegianceCrisis crisis =
                Crisis(
                    "KEEPER",
                    "PLAYER_B",
                    "PLAYER_C",
                    "PLAYER_D"
                );

            Assert.That(
                crisis.TryCastVote(
                    Vote(
                        "KEEPER",
                        AllegianceChoice.Kvlt
                    )
                ),
                Is.True
            );

            Assert.That(
                crisis.TryCastVote(
                    Vote(
                        "PLAYER_B",
                        AllegianceChoice.Kvlt
                    )
                ),
                Is.True
            );

            Assert.That(
                crisis.TryCastVote(
                    Vote(
                        "PLAYER_C",
                        AllegianceChoice.Society
                    )
                ),
                Is.True
            );

            Assert.That(
                crisis.TryCastVote(
                    Vote(
                        "PLAYER_D",
                        AllegianceChoice.Society
                    )
                ),
                Is.True
            );

            Assert.That(
                crisis.TryResolve(
                    9,
                    out AllegianceCrisisResolution
                        result
                ),
                Is.True
            );

            Assert.That(
                result.KvltVoteCount,
                Is.EqualTo(2)
            );

            Assert.That(
                result.SocietyVoteCount,
                Is.EqualTo(2)
            );

            Assert.That(
                result.WeightedKvltVoteCount,
                Is.EqualTo(3)
            );

            Assert.That(
                result.WeightedSocietyVoteCount,
                Is.EqualTo(2)
            );

            Assert.That(
                result.CohesionFracture,
                Is.EqualTo(1f)
            );
        }

        [Test]
        public void KeeperBreaksTieRemainingAfterInstitutionalWeight()
        {
            AllegianceCrisis crisis =
                Crisis(
                    "KEEPER",
                    "PLAYER_B",
                    "PLAYER_C"
                );

            Assert.That(
                crisis.TryCastVote(
                    Vote(
                        "KEEPER",
                        AllegianceChoice.Kvlt
                    )
                ),
                Is.True
            );

            Assert.That(
                crisis.TryCastVote(
                    Vote(
                        "PLAYER_B",
                        AllegianceChoice.Society
                    )
                ),
                Is.True
            );

            Assert.That(
                crisis.TryCastVote(
                    Vote(
                        "PLAYER_C",
                        AllegianceChoice.Society
                    )
                ),
                Is.True
            );

            // Raw:
            // KVLT 1 / Society 2
            //
            // Keeper institutional weight:
            // KVLT 2 / Society 2
            //
            // Remaining tie:
            // Keeper breaks toward KVLT.

            Assert.That(
                crisis.TryResolve(
                    9,
                    out AllegianceCrisisResolution
                        result
                ),
                Is.True
            );

            Assert.That(
                result.WeightedKvltVoteCount,
                Is.EqualTo(2)
            );

            Assert.That(
                result.WeightedSocietyVoteCount,
                Is.EqualTo(2)
            );

            Assert.That(
                result.Outcome,
                Is.EqualTo(
                    AllegianceCrisisOutcome.Kvlt
                )
            );

            Assert.That(
                result.CohesionFracture,
                Is.EqualTo(
                    2f / 3f
                ).Within(0.0001f)
            );
        }

        [Test]
        public void TieWithoutKeeperVoteRemainsUnresolvedRatherThanInventingRule()
        {
            AllegianceCrisis crisis =
                new AllegianceCrisis(
                    Question(),
                    new[]
                    {
                        "PLAYER_B",
                        "PLAYER_C"
                    },
                    "KEEPER"
                );

            Assert.That(
                crisis.TryCastVote(
                    Vote(
                        "PLAYER_B",
                        AllegianceChoice.Kvlt
                    )
                ),
                Is.True
            );

            Assert.That(
                crisis.TryCastVote(
                    Vote(
                        "PLAYER_C",
                        AllegianceChoice.Society
                    )
                ),
                Is.True
            );

            Assert.That(
                crisis.TryResolve(
                    9,
                    out _
                ),
                Is.False
            );

            Assert.That(
                crisis.IsOpen,
                Is.True
            );
        }

        [Test]
        public void RegistryRetainsOpenAndResolvedCrisisHistory()
        {
            AllegianceCrisisRegistry registry =
                new AllegianceCrisisRegistry();

            AllegianceCrisis crisis =
                Crisis(
                    "KEEPER",
                    "PLAYER_B"
                );

            registry.Record(crisis);

            Assert.That(
                registry.GetOpen().Count,
                Is.EqualTo(1)
            );

            VoteAll(
                crisis,
                AllegianceChoice.Kvlt
            );

            Assert.That(
                crisis.TryResolve(
                    9,
                    out _
                ),
                Is.True
            );

            Assert.That(
                registry.GetOpen(),
                Is.Empty
            );

            Assert.That(
                registry.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                registry.TryGet(
                    crisis.Question.QuestionId,
                    out AllegianceCrisis stored
                ),
                Is.True
            );

            Assert.That(
                stored,
                Is.SameAs(crisis)
            );

            Assert.That(
                stored.Votes.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                stored.Resolution,
                Is.Not.Null
            );
        }

        private static
            AllegianceCrisisQuestion
            Question()
        {
            return new AllegianceCrisisQuestion(
                "HAPPENING",
                "INTENT",
                "TRANSGRESSOR",
                "CHURCH_ARSON",
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Transgressive,
                8,
                "BEHAVIOR"
            );
        }

        private static AllegianceCrisis Crisis(
            params string[] voters)
        {
            return new AllegianceCrisis(
                Question(),
                voters,
                "KEEPER"
            );
        }

        private static AllegianceCrisisVote Vote(
            string voter,
            AllegianceChoice choice)
        {
            return new AllegianceCrisisVote(
                voter,
                choice,
                8
            );
        }

        private static void VoteAll(
            AllegianceCrisis crisis,
            AllegianceChoice choice)
        {
            foreach (
                string voter
                in crisis.EligibleVoterEntityIds)
            {
                Assert.That(
                    crisis.TryCastVote(
                        Vote(
                            voter,
                            choice
                        )
                    ),
                    Is.True
                );
            }
        }
    }
}