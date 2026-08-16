using System;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Pure facade for generating a release-level title from
    /// the Tracks contained in one RehearsalSet.
    ///
    /// First attempts naming from TRVE-eligible formal Pair
    /// material only. If that existing naming grammar cannot
    /// produce a valid title, falls back to the ordinary
    /// complete semantic naming projection.
    ///
    /// This class never mutates the RehearsalSet or its Tracks.
    /// </summary>
    public static class ReleaseNamingGenerator
    {
        private static readonly TrackNamingLexiconCatalog
            DefaultCatalog =
                TrackNamingLexiconCatalogData.CreateDefault();

        public static bool TryGenerate(
            RehearsalSet rehearsalSet,
            out string title)
        {
            if (rehearsalSet == null)
            {
                throw new ArgumentNullException(
                    nameof(rehearsalSet)
                );
            }

            return TryGenerate(
                rehearsalSet,
                DefaultCatalog,
                TrackNamingSeed.FromTrackId(
                    rehearsalSet.VhsSetId
                ),
                out title
            );
        }

        public static bool TryGenerate(
            RehearsalSet rehearsalSet,
            TrackNamingLexiconCatalog catalog,
            int namingSeed,
            out string title)
        {
            if (rehearsalSet == null)
            {
                throw new ArgumentNullException(
                    nameof(rehearsalSet)
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

            TrackNamingPositionSelection
                trvePreferredSelection =
                    ReleaseNamingProjectionPipeline
                        .ResolveTrvePreferred(
                            rehearsalSet
                        );

            if (TryRender(
                    trvePreferredSelection,
                    catalog,
                    namingSeed,
                    out title))
            {
                return true;
            }

            TrackNamingPositionSelection
                regularFallbackSelection =
                    ReleaseNamingProjectionPipeline
                        .ResolveRegularFallback(
                            rehearsalSet
                        );

            return TryRender(
                regularFallbackSelection,
                catalog,
                namingSeed,
                out title
            );
        }

        private static bool TryRender(
            TrackNamingPositionSelection selection,
            TrackNamingLexiconCatalog catalog,
            int namingSeed,
            out string title)
        {
            TrackNamingGrammarAnalysis analysis =
                TrackNamingGrammarAnalyzer.Analyze(
                    selection
                );

            return TrackNamingGrammarRenderer.TryRender(
                analysis,
                catalog,
                namingSeed,
                out title
            );
        }
    }
}