using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Events;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Circulation
{
    public sealed class
        ActivationLegitimacyCrisisGroupingService
    {
        public IReadOnlyList<
            ActivationLegitimacyCrisisGroup>
            Group(
                IReadOnlyList<
                    ActivationLegitimacyAssessment>
                    assessments,
                int openedTurn)
        {
            return Group(
                assessments,
                openedTurn,
                Array.Empty<Happening>());
        }

        public IReadOnlyList<
            ActivationLegitimacyCrisisGroup>
            Group(
                IReadOnlyList<
                    ActivationLegitimacyAssessment>
                    assessments,
                int openedTurn,
                IReadOnlyList<Happening> happenings)
        {
            if (assessments == null)
            {
                throw new ArgumentNullException(
                    nameof(assessments)
                );
            }

            if (openedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(openedTurn)
                );
            }

            if (happenings == null)
            {
                throw new ArgumentNullException(
                    nameof(happenings)
                );
            }

            Dictionary<string, BehaviorOccurrence>
                factualBehaviors =
                    BuildBehaviorLookup(happenings);

            Dictionary<
                GroupKey,
                List<ActivationLegitimacyAssessment>>
                grouped =
                    new();

            List<GroupKey> order =
                new();

            foreach (
                ActivationLegitimacyAssessment assessment
                in assessments)
            {
                if (assessment == null)
                {
                    throw new ArgumentException(
                        "Assessment collection cannot " +
                        "contain null.",
                        nameof(assessments)
                    );
                }

                if (assessment.Disposition !=
                    ActivationLegitimacyDisposition
                        .RequiresAllegianceCrisis)
                {
                    continue;
                }

                ActivationLegitimacyCandidate candidate =
                    assessment.Candidate;

                BehaviorOccurrence factualBehavior = null;

                if (!string.IsNullOrWhiteSpace(
                        candidate.BehaviorOccurrenceId))
                {
                    factualBehaviors.TryGetValue(
                        BuildBehaviorKey(
                            candidate.HappeningId,
                            candidate.BehaviorOccurrenceId),
                        out factualBehavior);
                }

                GroupKey key =
                    new(
                        candidate.HappeningId,
                        factualBehavior?.SourceIntentId ??
                            candidate.SourceIntentId,
                        factualBehavior?.ActorEntityIds[0] ??
                            candidate.ActorEntityId,
                        candidate.BehaviorTypeId,
                        candidate.Axis,
                        candidate.Pole,
                        candidate.PraxisDegree,
                        candidate.BehaviorOccurrenceId
                    );

                if (!grouped.TryGetValue(
                        key,
                        out List<
                            ActivationLegitimacyAssessment>
                            group))
                {
                    group =
                        new List<
                            ActivationLegitimacyAssessment>();

                    grouped.Add(
                        key,
                        group
                    );

                    order.Add(key);
                }

                group.Add(assessment);
            }

            List<ActivationLegitimacyCrisisGroup>
                results =
                    new();

            foreach (GroupKey key in order)
            {
                AllegianceCrisisQuestion question =
                    new(
                        key.HappeningId,
                        key.SourceIntentId,
                        key.ActorEntityId,
                        key.BehaviorTypeId,
                        key.Axis,
                        key.Pole,
                        key.PraxisDegree,
                        openedTurn,
                        key.BehaviorOccurrenceId
                    );

                results.Add(
                    new ActivationLegitimacyCrisisGroup(
                        question,
                        grouped[key]
                    )
                );
            }

            return results.ToArray();
        }

        private static Dictionary<string, BehaviorOccurrence>
            BuildBehaviorLookup(
                IReadOnlyList<Happening> happenings)
        {
            Dictionary<string, BehaviorOccurrence> result =
                new(StringComparer.Ordinal);

            foreach (Happening happening in happenings)
            {
                if (happening == null)
                {
                    throw new ArgumentException(
                        "Happening population cannot contain null.",
                        nameof(happenings));
                }

                foreach (BehaviorOccurrence behavior
                         in happening.BehaviorOccurrences)
                {
                    result.Add(
                        BuildBehaviorKey(
                            happening.HappeningId,
                            behavior.BehaviorOccurrenceId),
                        behavior);
                }
            }

            return result;
        }

        private static string BuildBehaviorKey(
            string happeningId,
            string behaviorOccurrenceId)
        {
            return happeningId + "\u001f" +
                   behaviorOccurrenceId;
        }

        private readonly struct GroupKey :
            IEquatable<GroupKey>
        {
            public string HappeningId { get; }
            public string SourceIntentId { get; }
            public string ActorEntityId { get; }
            public string BehaviorTypeId { get; }
            public TagAxis Axis { get; }
            public TagPole Pole { get; }
            public TagDegree PraxisDegree { get; }
            public string BehaviorOccurrenceId { get; }

            public GroupKey(
                string happeningId,
                string sourceIntentId,
                string actorEntityId,
                string behaviorTypeId,
                TagAxis axis,
                TagPole pole,
                TagDegree praxisDegree,
                string behaviorOccurrenceId)
            {
                HappeningId = happeningId;
                SourceIntentId = sourceIntentId;
                ActorEntityId = actorEntityId;
                BehaviorTypeId = behaviorTypeId;
                Axis = axis;
                Pole = pole;
                PraxisDegree = praxisDegree;
                BehaviorOccurrenceId =
                    behaviorOccurrenceId;
            }

            public bool Equals(GroupKey other)
            {
                return
                    HappeningId == other.HappeningId &&
                    SourceIntentId ==
                        other.SourceIntentId &&
                    ActorEntityId ==
                        other.ActorEntityId &&
                    BehaviorTypeId ==
                        other.BehaviorTypeId &&
                    Axis == other.Axis &&
                    Pole == other.Pole &&
                    PraxisDegree ==
                        other.PraxisDegree &&
                    BehaviorOccurrenceId ==
                        other.BehaviorOccurrenceId;
            }

            public override bool Equals(object obj)
            {
                return
                    obj is GroupKey other &&
                    Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;

                    hash =
                        hash * 31 +
                        StringComparer.Ordinal
                            .GetHashCode(HappeningId);

                    hash =
                        hash * 31 +
                        StringComparer.Ordinal
                            .GetHashCode(SourceIntentId);

                    hash =
                        hash * 31 +
                        StringComparer.Ordinal
                            .GetHashCode(ActorEntityId);

                    hash =
                        hash * 31 +
                        StringComparer.Ordinal
                            .GetHashCode(BehaviorTypeId);

                    hash =
                        hash * 31 +
                        Axis.GetHashCode();

                    hash =
                        hash * 31 +
                        Pole.GetHashCode();

                    hash =
                        hash * 31 +
                        PraxisDegree.GetHashCode();

                    hash =
                        hash * 31 +
                        (BehaviorOccurrenceId == null
                            ? 0
                            : StringComparer.Ordinal
                                .GetHashCode(
                                    BehaviorOccurrenceId
                                ));

                    return hash;
                }
            }
        }
    }
}
