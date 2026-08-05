using System;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Pure facade for generating a title from an authoritative
    /// Track composition.
    ///
    /// This class never mutates the Track.
    /// </summary>
    public static class TrackNamingGenerator
    {
        private static readonly TrackNamingLexiconCatalog
            DefaultCatalog =
                TrackNamingLexiconCatalogData.CreateDefault();

        public static bool TryGenerate(
            Track track,
            int namingSeed,
            out string title)
        {
            return TryGenerate(
                track,
                DefaultCatalog,
                namingSeed,
                out title
            );
        }

        public static bool TryGenerate(
            Track track,
            TrackNamingLexiconCatalog catalog,
            int namingSeed,
            out string title)
        {
            if (track == null)
            {
                throw new ArgumentNullException(
                    nameof(track)
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

            TrackNamingPositionSelection selection =
                TrackNamingProjectionPipeline.Resolve(
                    track
                );

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
        
        public static bool TryGenerate(
            Track track,
            out string title)
        {
            if (track == null)
            {
                throw new ArgumentNullException(
                    nameof(track)
                );
            }

            return TryGenerate(
                track,
                DefaultCatalog,
                TrackNamingSeed.FromTrack(track),
                out title
            );
        }

        public static bool TryGenerate(
            Track track,
            TrackNamingLexiconCatalog catalog,
            out string title)
        {
            if (track == null)
            {
                throw new ArgumentNullException(
                    nameof(track)
                );
            }

            return TryGenerate(
                track,
                catalog,
                TrackNamingSeed.FromTrack(track),
                out title
            );
        }
    }
}