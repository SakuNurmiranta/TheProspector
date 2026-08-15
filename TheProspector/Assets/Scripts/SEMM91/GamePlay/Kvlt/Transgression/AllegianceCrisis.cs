using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Transgression
{
    public sealed class AllegianceCrisis
    {
        private readonly
            string[]
                eligibleVoterEntityIds;

        private readonly
            HashSet<string>
                eligibleVoterSet =
                    new(
                        StringComparer.Ordinal
                    );

        private readonly
            List<AllegianceCrisisVote>
                votes =
                    new();

        private readonly
            Dictionary<string, AllegianceCrisisVote>
                votesByVoter =
                    new(
                        StringComparer.Ordinal
                    );

        public AllegianceCrisisQuestion
            Question { get; }

        public string KeeperEntityId { get; }

        public IReadOnlyList<string>
            EligibleVoterEntityIds =>
                eligibleVoterEntityIds;

        public IReadOnlyList<AllegianceCrisisVote>
            Votes =>
                votes;

        public AllegianceCrisisResolution
            Resolution { get; private set; }

        public bool IsResolved =>
            Resolution != null;

        public bool IsOpen =>
            Resolution == null;

        public bool AllEligibleVotesCast =>
            votes.Count ==
            eligibleVoterEntityIds.Length;

        public AllegianceCrisis(
            AllegianceCrisisQuestion question,
            IEnumerable<string>
                eligibleVoterEntityIds,
            string keeperEntityId)
        {
            Question =
                question ??
                throw new ArgumentNullException(
                    nameof(question)
                );

            if (eligibleVoterEntityIds == null)
            {
                throw new ArgumentNullException(
                    nameof(eligibleVoterEntityIds)
                );
            }

            if (string.IsNullOrWhiteSpace(
                    keeperEntityId))
            {
                throw new ArgumentException(
                    "Allegiance Crisis requires " +
                    "current Keeper identity.",
                    nameof(keeperEntityId)
                );
            }

            KeeperEntityId =
                keeperEntityId.Trim();

            List<string> voters =
                new();

            foreach (
                string voterEntityId
                in eligibleVoterEntityIds)
            {
                if (string.IsNullOrWhiteSpace(
                        voterEntityId))
                {
                    throw new ArgumentException(
                        "Eligible voter identity " +
                        "cannot be empty.",
                        nameof(eligibleVoterEntityIds)
                    );
                }

                string normalized =
                    voterEntityId.Trim();

                if (normalized ==
                    Question.TriggeringActorEntityId)
                {
                    throw new ArgumentException(
                        "Triggering actor cannot vote " +
                        "in their own Allegiance Crisis.",
                        nameof(eligibleVoterEntityIds)
                    );
                }

                if (!eligibleVoterSet.Add(
                        normalized))
                {
                    throw new ArgumentException(
                        "Eligible voter list contains " +
                        "a duplicate entity.",
                        nameof(eligibleVoterEntityIds)
                    );
                }

                voters.Add(normalized);
            }

            if (voters.Count == 0)
            {
                throw new ArgumentException(
                    "Allegiance Crisis requires at " +
                    "least one eligible voter.",
                    nameof(eligibleVoterEntityIds)
                );
            }

            this.eligibleVoterEntityIds =
                voters.ToArray();
        }

        public bool TryCastVote(
            AllegianceCrisisVote vote)
        {
            if (vote == null ||
                IsResolved)
            {
                return false;
            }

            if (!eligibleVoterSet.Contains(
                    vote.VoterEntityId))
            {
                return false;
            }

            if (vote.CastTurn <
                Question.OpenedTurn)
            {
                return false;
            }

            if (votesByVoter.ContainsKey(
                    vote.VoterEntityId))
            {
                return false;
            }

            votes.Add(vote);

            votesByVoter.Add(
                vote.VoterEntityId,
                vote
            );

            return true;
        }

        public bool TryResolve(
            int resolvedTurn,
            out AllegianceCrisisResolution resolution)
        {
            resolution = null;

            if (IsResolved ||
                !AllEligibleVotesCast)
            {
                return false;
            }

            if (resolvedTurn <
                Question.OpenedTurn)
            {
                return false;
            }

            int kvltVotes = 0;
            int societyVotes = 0;

            AllegianceCrisisVote keeperVote =
                null;

            int latestVoteTurn =
                Question.OpenedTurn;

            foreach (
                AllegianceCrisisVote vote
                in votes)
            {
                if (vote.CastTurn >
                    latestVoteTurn)
                {
                    latestVoteTurn =
                        vote.CastTurn;
                }

                if (vote.Choice ==
                    AllegianceChoice.Kvlt)
                {
                    kvltVotes++;
                }
                else
                {
                    societyVotes++;
                }

                if (vote.VoterEntityId ==
                    KeeperEntityId)
                {
                    keeperVote =
                        vote;
                }
            }

            if (resolvedTurn <
                latestVoteTurn)
            {
                return false;
            }

            int weightedKvlt =
                kvltVotes;

            int weightedSociety =
                societyVotes;

            if (keeperVote != null)
            {
                // Ordinary vote already counted.
                // Add one extra institutional weight.

                if (keeperVote.Choice ==
                    AllegianceChoice.Kvlt)
                {
                    weightedKvlt++;
                }
                else
                {
                    weightedSociety++;
                }
            }

            AllegianceCrisisOutcome outcome;

            if (weightedKvlt >
                weightedSociety)
            {
                outcome =
                    AllegianceCrisisOutcome.Kvlt;
            }
            else if (weightedSociety >
                     weightedKvlt)
            {
                outcome =
                    AllegianceCrisisOutcome.Society;
            }
            else
            {
                // Existing design makes Keeper the
                // institutional tie-breaker.

                if (keeperVote == null)
                {
                    /*
                     * The Keeper may be the triggering
                     * actor and therefore ineligible to
                     * vote. Without an affirmative KVLT
                     * majority, an exact tie cannot
                     * establish the new transgression.
                     */
                    outcome =
                        AllegianceCrisisOutcome.Society;
                }
                else
                {
                    outcome =
                        keeperVote.Choice ==
                        AllegianceChoice.Kvlt
                            ? AllegianceCrisisOutcome.Kvlt
                            : AllegianceCrisisOutcome.Society;
                }
            }

            int totalVoters =
                kvltVotes +
                societyVotes;

            float cohesionFracture =
                totalVoters == 0
                    ? 0f
                    : 2f *
                      Math.Min(
                          kvltVotes,
                          societyVotes
                      ) /
                      totalVoters;

            Resolution =
                new AllegianceCrisisResolution(
                    outcome,
                    kvltVotes,
                    societyVotes,
                    weightedKvlt,
                    weightedSociety,
                    cohesionFracture,
                    keeperVote != null,
                    keeperVote?.Choice,
                    resolvedTurn
                );

            resolution =
                Resolution;

            return true;
        }
    }
}
