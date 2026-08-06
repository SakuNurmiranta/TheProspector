using System;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Renders the initial deterministic grammar templates.
    ///
    /// This stage performs no collision checking, semantic
    /// validation, template fallback, or Track mutation.
    /// </summary>
    public static class TrackNamingGrammarRenderer
    {
        public static bool TryRender(
            TrackNamingGrammarAnalysis analysis,
            TrackNamingLexiconCatalog catalog,
            int namingSeed,
            out string title)
        {
            if (analysis == null)
            {
                throw new ArgumentNullException(
                    nameof(analysis)
                );
            }

            if (catalog == null)
            {
                throw new ArgumentNullException(
                    nameof(catalog)
                );
            }

            if (namingSeed < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(namingSeed),
                    namingSeed,
                    "Naming seed cannot be negative."
                );
            }

            string rawTitle;

            switch (analysis.Shape)
            {
                case TrackNamingGrammarShape.Empty:
                    title = string.Empty;
                    return false;

                case TrackNamingGrammarShape
                    .OneAccumulative:

                    rawTitle =
                        RenderOneAccumulative(
                            analysis,
                            catalog,
                            namingSeed
                        );

                    break;

                case TrackNamingGrammarShape
                    .TwoAccumulative:

                    rawTitle =
                        RenderTwoAccumulative(
                            analysis,
                            catalog,
                            namingSeed
                        );

                    break;

                case TrackNamingGrammarShape.Pair:
                    rawTitle =
                        RenderPair(
                            analysis.PairDominant,
                            analysis.PairSubmissive,
                            catalog,
                            namingSeed
                        );

                    break;

                case TrackNamingGrammarShape
                    .PairWithSurface:

                    rawTitle =
                        RenderPairWithSurface(
                            analysis,
                            catalog,
                            namingSeed
                        );

                    break;

                case TrackNamingGrammarShape
                    .ThreeAccumulative:

                    // The initial lexicon contract does not yet
                    // expose an explicitly eligible context,
                    // abstraction, object, or consequence slot.
                    title = string.Empty;
                    return false;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(analysis.Shape),
                        analysis.Shape,
                        "Unsupported track-naming grammar shape."
                    );
            }

            title =
                TrackNamingTitleCase.Apply(
                    rawTitle
                );

            bool valid =
                TrackNamingTitleValidator.TryValidate(
                    title,
                    out _
                );

            if (!valid)
            {
                title = string.Empty;
                return false;
            }

            return true;
        }

        private static string RenderOneAccumulative(
            TrackNamingGrammarAnalysis analysis,
            TrackNamingLexiconCatalog catalog,
            int namingSeed)
        {
            TrackNamingSemanticPosition position =
                analysis.AccumulativePositions[0];

            string modifier =
                Select(
                    catalog,
                    position,
                    TrackNamingLexicalCategory
                        .SurfaceModifier,
                    namingSeed
                );

            string noun =
                Select(
                    catalog,
                    position,
                    TrackNamingLexicalCategory
                        .HeadNoun,
                    namingSeed
                );

            return modifier + " " + noun;
        }

        private static string RenderTwoAccumulative(
            TrackNamingGrammarAnalysis analysis,
            TrackNamingLexiconCatalog catalog,
            int namingSeed)
        {
            TrackNamingSemanticPosition governor =
                analysis.AccumulativePositions[0];

            TrackNamingSemanticPosition support =
                analysis.AccumulativePositions[1];

            string modifier =
                Select(
                    catalog,
                    support,
                    TrackNamingLexicalCategory
                        .SurfaceModifier,
                    namingSeed
                );

            string noun =
                Select(
                    catalog,
                    governor,
                    TrackNamingLexicalCategory
                        .HeadNoun,
                    namingSeed
                );

            return modifier + " " + noun;
        }

        private static string RenderPairWithSurface(
            TrackNamingGrammarAnalysis analysis,
            TrackNamingLexiconCatalog catalog,
            int namingSeed)
        {
            string surfaceModifier =
                Select(
                    catalog,
                    analysis.SurfacePosition,
                    TrackNamingLexicalCategory
                        .SurfaceModifier,
                    namingSeed
                );

            string pairCore =
                RenderPair(
                    analysis.PairDominant,
                    analysis.PairSubmissive,
                    catalog,
                    namingSeed
                );

            return surfaceModifier + " " + pairCore;
        }

        private static string RenderPair(
            TrackNamingSemanticPosition dominant,
            TrackNamingSemanticPosition submissive,
            TrackNamingLexiconCatalog catalog,
            int namingSeed)
        {
            string action =
                Select(
                    catalog,
                    dominant,
                    TrackNamingLexicalCategory
                        .DominantAction,
                    namingSeed
                );

            string target =
                Select(
                    catalog,
                    submissive,
                    TrackNamingLexicalCategory
                        .SubmissivePhrase,
                    namingSeed
                );

            return action + " " + target;
        }

        private static string Select(
            TrackNamingLexiconCatalog catalog,
            TrackNamingSemanticPosition position,
            TrackNamingLexicalCategory category,
            int namingSeed)
        {
            return TrackNamingLexicalSelector.Select(
                catalog,
                position,
                category,
                namingSeed
            );
        }
    }
}