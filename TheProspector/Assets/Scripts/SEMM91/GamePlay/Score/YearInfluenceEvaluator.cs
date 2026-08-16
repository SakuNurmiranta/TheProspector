using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Score
{
    /// <summary>
    /// Projects the authoritative ScoreLedger into
    /// completed-year cultural influence.
    ///
    /// Peak-2 YearInfluence contains only:
    ///
    /// - ordinary Field Gravity;
    /// - CanonRetained frozen Gravity;
    /// - post-tenure frontier Institutional Gravity.
    ///
    /// It deliberately excludes First-Fetter
    /// Resonance and Hole Mandate institutional
    /// credit.
    /// </summary>
    public sealed class YearInfluenceEvaluator
    {
        public IReadOnlyList<YearInfluenceEvaluation>
            Evaluate(
                string sceneId,
                int lastTurnInclusive,
                int turnsPerYear,
                IReadOnlyList<string>
                    beneficiaryEntityIds,
                ScoreLedger ledger)
        {
            sceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
                );

            if (lastTurnInclusive < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lastTurnInclusive)
                );
            }

            if (turnsPerYear <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(turnsPerYear)
                );
            }

            if (beneficiaryEntityIds == null)
            {
                throw new ArgumentNullException(
                    nameof(beneficiaryEntityIds)
                );
            }

            if (ledger == null)
            {
                throw new ArgumentNullException(
                    nameof(ledger)
                );
            }

            int firstTurnInclusive =
                lastTurnInclusive -
                (turnsPerYear - 1);

            if (firstTurnInclusive < 0)
            {
                throw new ArgumentException(
                    "YearInfluence cannot evaluate an " +
                    "incomplete pre-session year.",
                    nameof(lastTurnInclusive)
                );
            }

            string[] beneficiaries =
                NormalizeBeneficiaries(
                    beneficiaryEntityIds
                );

            Dictionary<string, List<ScoreEvent>>
                eventsByBeneficiary =
                    new(
                        StringComparer.Ordinal
                    );

            foreach (
                string beneficiary
                in beneficiaries)
            {
                eventsByBeneficiary.Add(
                    beneficiary,
                    new List<ScoreEvent>()
                );
            }

            /*
             * The Score ledger is append-only.
             *
             * Preserve ledger chronology inside each
             * YearInfluence result rather than
             * reconstructing or sorting causal history.
             */
            foreach (
                ScoreEvent scoreEvent
                in ledger.History)
            {
                if (scoreEvent.SceneId !=
                    sceneId)
                {
                    continue;
                }

                if (scoreEvent.GlobalTurn <
                        firstTurnInclusive ||
                    scoreEvent.GlobalTurn >
                        lastTurnInclusive)
                {
                    continue;
                }

                if (!IsInfluenceBearing(
                        scoreEvent.Kind))
                {
                    continue;
                }

                if (!eventsByBeneficiary.TryGetValue(
                        scoreEvent
                            .BeneficiaryEntityId,
                        out List<ScoreEvent> events))
                {
                    /*
                     * Score may legitimately contain
                     * provenance for entities outside
                     * the current participant roster.
                     *
                     * Keeper candidacy is evaluated only
                     * for the explicitly supplied active
                     * participant population.
                     */
                    continue;
                }

                events.Add(
                    scoreEvent
                );
            }

            List<YearInfluenceEvaluation>
                result =
                    new();

            /*
             * beneficiaries is ordinal-sorted, giving
             * deterministic output independent of the
             * caller's player collection order.
             */
            foreach (
                string beneficiary
                in beneficiaries)
            {
                result.Add(
                    new YearInfluenceEvaluation(
                        beneficiary,
                        sceneId,
                        firstTurnInclusive,
                        lastTurnInclusive,
                        eventsByBeneficiary[
                            beneficiary
                        ]
                    )
                );
            }

            return result;
        }

        public static bool IsInfluenceBearing(
            ScoreEventKind kind)
        {
            switch (kind)
            {
                case ScoreEventKind.FieldGravity:
                case ScoreEventKind
                    .CanonRetainedGravity:
                case ScoreEventKind
                    .InstitutionalGravity:

                    return true;

                case ScoreEventKind
                    .FirstFetterResonance:

                case ScoreEventKind
                    .HoleMandateGravity:

                    return false;

                default:

                    throw new ArgumentOutOfRangeException(
                        nameof(kind),
                        kind,
                        "Undefined Score category cannot " +
                        "be classified for YearInfluence."
                    );
            }
        }

        private static string[]
            NormalizeBeneficiaries(
                IReadOnlyList<string>
                    beneficiaryEntityIds)
        {
            List<string> result =
                new();

            HashSet<string> seen =
                new(
                    StringComparer.Ordinal
                );

            foreach (
                string beneficiaryEntityId
                in beneficiaryEntityIds)
            {
                string normalized =
                    RequireText(
                        beneficiaryEntityId,
                        nameof(
                            beneficiaryEntityIds
                        )
                    );

                if (!seen.Add(
                        normalized))
                {
                    throw new ArgumentException(
                        "YearInfluence beneficiary " +
                        "population contains duplicate " +
                        "identity.",
                        nameof(
                            beneficiaryEntityIds
                        )
                    );
                }

                result.Add(
                    normalized
                );
            }

            if (result.Count == 0)
            {
                throw new ArgumentException(
                    "YearInfluence requires at least " +
                    "one beneficiary.",
                    nameof(
                        beneficiaryEntityIds
                    )
                );
            }

            result.Sort(
                StringComparer.Ordinal
            );

            return result.ToArray();
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "YearInfluence identity cannot " +
                    "be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}