namespace SEMM91.Core.Tags
{
    /// <summary>
    /// Authored undirected adjacency relationships between
    /// the fourteen Tag identities.
    ///
    /// Tag identity consists of Axis + Pole. Degree does not
    /// affect adjacency.
    /// </summary>
    public static class TagAdjacency
    {
        private readonly struct AdjacencyEdge
        {
            private readonly TagAxis _firstAxis;
            private readonly TagPole _firstPole;

            private readonly TagAxis _secondAxis;
            private readonly TagPole _secondPole;

            public AdjacencyEdge(
                TagAxis firstAxis,
                TagPole firstPole,
                TagAxis secondAxis,
                TagPole secondPole)
            {
                _firstAxis = firstAxis;
                _firstPole = firstPole;
                _secondAxis = secondAxis;
                _secondPole = secondPole;
            }

            public bool Connects(
                TagAxis firstAxis,
                TagPole firstPole,
                TagAxis secondAxis,
                TagPole secondPole)
            {
                bool forward =
                    _firstAxis == firstAxis &&
                    _firstPole == firstPole &&
                    _secondAxis == secondAxis &&
                    _secondPole == secondPole;

                bool reverse =
                    _firstAxis == secondAxis &&
                    _firstPole == secondPole &&
                    _secondAxis == firstAxis &&
                    _secondPole == firstPole;

                return forward || reverse;
            }
        }

        /*
         * Each relationship is stored once because adjacency
         * is undirected.
         */
        private static readonly AdjacencyEdge[] Edges =
        {
            // Profane: Malevolent, Raw, Void
            new(
                TagAxis.Symbolic,
                TagPole.Negative,
                TagAxis.Physical,
                TagPole.Negative
            ),
            new(
                TagAxis.Symbolic,
                TagPole.Negative,
                TagAxis.Expressive,
                TagPole.Negative
            ),
            new(
                TagAxis.Symbolic,
                TagPole.Negative,
                TagAxis.Interpretive,
                TagPole.Negative
            ),

            // Cold: Void, Morbid, Slow
            new(
                TagAxis.Emotional,
                TagPole.Negative,
                TagAxis.Interpretive,
                TagPole.Negative
            ),
            new(
                TagAxis.Emotional,
                TagPole.Negative,
                TagAxis.Existential,
                TagPole.Negative
            ),
            new(
                TagAxis.Emotional,
                TagPole.Negative,
                TagAxis.Temporal,
                TagPole.Positive
            ),

            // Raw: Fast, Malevolent
            // Raw ↔ Profane is already declared above.
            new(
                TagAxis.Expressive,
                TagPole.Negative,
                TagAxis.Temporal,
                TagPole.Negative
            ),
            new(
                TagAxis.Expressive,
                TagPole.Negative,
                TagAxis.Physical,
                TagPole.Negative
            ),

            // Fast: Malevolent, Vital
            // Fast ↔ Raw is already declared above.
            new(
                TagAxis.Temporal,
                TagPole.Negative,
                TagAxis.Physical,
                TagPole.Negative
            ),
            new(
                TagAxis.Temporal,
                TagPole.Negative,
                TagAxis.Existential,
                TagPole.Positive
            ),

            // Morbid: Slow, Void
            // Morbid ↔ Cold is already declared above.
            new(
                TagAxis.Existential,
                TagPole.Negative,
                TagAxis.Temporal,
                TagPole.Positive
            ),
            new(
                TagAxis.Existential,
                TagPole.Negative,
                TagAxis.Interpretive,
                TagPole.Negative
            ),

            // Sacred: Meaning, Slow, Honed
            new(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagAxis.Interpretive,
                TagPole.Positive
            ),
            new(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagAxis.Temporal,
                TagPole.Positive
            ),
            new(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagAxis.Expressive,
                TagPole.Positive
            ),

            // Warm: Vital, Benevolent, Honed
            new(
                TagAxis.Emotional,
                TagPole.Positive,
                TagAxis.Existential,
                TagPole.Positive
            ),
            new(
                TagAxis.Emotional,
                TagPole.Positive,
                TagAxis.Physical,
                TagPole.Positive
            ),
            new(
                TagAxis.Emotional,
                TagPole.Positive,
                TagAxis.Expressive,
                TagPole.Positive
            ),

            // Honed: Meaning
            // Honed ↔ Sacred and Warm are already declared.
            new(
                TagAxis.Expressive,
                TagPole.Positive,
                TagAxis.Interpretive,
                TagPole.Positive
            ),

            // Benevolent: Vital, Meaning
            // Benevolent ↔ Warm is already declared.
            new(
                TagAxis.Physical,
                TagPole.Positive,
                TagAxis.Existential,
                TagPole.Positive
            ),
            new(
                TagAxis.Physical,
                TagPole.Positive,
                TagAxis.Interpretive,
                TagPole.Positive
            )
        };

        public static bool AreAdjacent(
            TagAxis firstAxis,
            TagPole firstPole,
            TagAxis secondAxis,
            TagPole secondPole)
        {
            // A Tag is an exact match with itself, not adjacent
            // to itself.
            if (firstAxis == secondAxis &&
                firstPole == secondPole)
            {
                return false;
            }

            foreach (AdjacencyEdge edge in Edges)
            {
                if (edge.Connects(
                        firstAxis,
                        firstPole,
                        secondAxis,
                        secondPole
                    ))
                {
                    return true;
                }
            }

            return false;
        }
    }
}