using System;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// Evaluates prospective supersession of one
    /// canonical Tag-polarity frontier.
    ///
    /// Canon is monotonic. Therefore neither the
    /// established frontier nor the turn-start Canon
    /// may disappear or decrease in the supplied
    /// post-settlement state.
    /// </summary>
    public sealed class
        CanonFrontierSupersessionEvaluator
    {
        public CanonFrontierSupersessionEvaluation
            Evaluate(
                TagAxis axis,
                TagPole pole,
                TagDegree canonicalDegreeEstablished,
                CanonState turnStartCanon,
                CanonState postSettlementCanon,
                int settledTurn)
        {
            if (turnStartCanon == null)
            {
                throw new ArgumentNullException(
                    nameof(turnStartCanon)
                );
            }

            if (postSettlementCanon == null)
            {
                throw new ArgumentNullException(
                    nameof(postSettlementCanon)
                );
            }

            if (canonicalDegreeEstablished ==
                TagDegree.Neutral)
            {
                throw new ArgumentException(
                    "Canonical frontier degree must " +
                    "be greater than Neutral.",
                    nameof(canonicalDegreeEstablished)
                );
            }

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            if (!turnStartCanon
                    .TryGetCanonicalDegree(
                        axis,
                        pole,
                        out TagDegree turnStartDegree))
            {
                throw new ArgumentException(
                    "Turn-start Canon does not contain " +
                    "the historical frontier polarity.",
                    nameof(turnStartCanon)
                );
            }

            if ((int)turnStartDegree <
                (int)canonicalDegreeEstablished)
            {
                throw new ArgumentException(
                    "Turn-start Canon is lower than a " +
                    "frontier already established in " +
                    "canonical history.",
                    nameof(turnStartCanon)
                );
            }

            if (!postSettlementCanon
                    .TryGetCanonicalDegree(
                        axis,
                        pole,
                        out TagDegree postDegree))
            {
                throw new ArgumentException(
                    "Post-settlement Canon lost an " +
                    "existing canonical polarity.",
                    nameof(postSettlementCanon)
                );
            }

            if ((int)postDegree <
                (int)turnStartDegree)
            {
                throw new ArgumentException(
                    "Canon cannot decrease during " +
                    "settlement.",
                    nameof(postSettlementCanon)
                );
            }

            CanonFrontierSupersessionDisposition
                disposition;

            if ((int)turnStartDegree >
                (int)canonicalDegreeEstablished)
            {
                disposition =
                    CanonFrontierSupersessionDisposition
                        .AlreadySuperseded;
            }
            else if ((int)postDegree >
                    (int)canonicalDegreeEstablished)
            {
                disposition =
                    CanonFrontierSupersessionDisposition
                        .SupersededAfterSettlement;
            }
            else
            {
                disposition =
                    CanonFrontierSupersessionDisposition
                        .RemainsCurrent;
            }

            return new
                CanonFrontierSupersessionEvaluation(
                    axis,
                    pole,
                    canonicalDegreeEstablished,
                    turnStartDegree,
                    postDegree,
                    settledTurn,
                    disposition
                );
        }
    }
}