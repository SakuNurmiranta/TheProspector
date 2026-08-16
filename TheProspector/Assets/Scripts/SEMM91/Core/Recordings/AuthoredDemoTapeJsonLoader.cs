using System;
using System.Collections.Generic;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;
using UnityEngine;

namespace SEMM91.Core.Recordings
{
    /// <summary>
    /// Thin authored-data adapter.
    ///
    /// JSON is converted immediately into the existing
    /// Track / Idea / Tag domain and then recorded through
    /// DemoTapeTrackSnapshotFactory.
    ///
    /// When requested, the loader also returns the exact
    /// RehearsalSet containing the mutable source Tracks
    /// from which the immutable DemoTape snapshots were
    /// produced.
    ///
    /// The private JSON types below are serialization
    /// shims only. They are not gameplay/domain types.
    /// </summary>
    public static class AuthoredDemoTapeJsonLoader
    {
        public static DemoTape Load(
            TextAsset jsonAsset,
            string sourceEntityId,
            int recordedTurn)
        {
            return Load(
                jsonAsset,
                sourceEntityId,
                recordedTurn,
                out _
            );
        }

        public static DemoTape Load(
            TextAsset jsonAsset,
            string sourceEntityId,
            int recordedTurn,
            out RehearsalSet sourceRehearsalSet)
        {
            if (jsonAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(jsonAsset)
                );
            }

            return Load(
                jsonAsset.text,
                sourceEntityId,
                recordedTurn,
                out sourceRehearsalSet
            );
        }

        public static DemoTape Load(
            string json,
            string sourceEntityId,
            int recordedTurn)
        {
            return Load(
                json,
                sourceEntityId,
                recordedTurn,
                out _
            );
        }

        public static DemoTape Load(
            string json,
            string sourceEntityId,
            int recordedTurn,
            out RehearsalSet sourceRehearsalSet)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException(
                    "Authored DemoTape JSON cannot be empty.",
                    nameof(json)
                );
            }

            if (string.IsNullOrWhiteSpace(
                    sourceEntityId))
            {
                throw new ArgumentException(
                    "Source entity identity cannot be empty.",
                    nameof(sourceEntityId)
                );
            }

            /*
             * Scenario bootstrap starts at turn 0.
             * Authored setup does not model a negative-turn
             * prehistory.
             */
            if (recordedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(recordedTurn)
                );
            }

            JsonDemoTape data =
                JsonUtility.FromJson<JsonDemoTape>(
                    json
                );

            if (data == null)
            {
                throw new FormatException(
                    "Authored DemoTape JSON produced no data."
                );
            }

            RequireText(
                data.demoTapeId,
                nameof(data.demoTapeId)
            );

            RequireText(
                data.displayName,
                nameof(data.displayName)
            );

            RequireText(
                data.sourceSetId,
                nameof(data.sourceSetId)
            );

            RequireText(
                data.sourceSetName,
                nameof(data.sourceSetName)
            );

            if (data.takeCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(data.takeCount)
                );
            }

            RequireFiniteNonNegative(
                data.recordingInterest,
                nameof(data.recordingInterest)
            );

            if (data.tracks == null ||
                data.tracks.Count == 0)
            {
                throw new ArgumentException(
                    "Authored DemoTape must contain " +
                    "at least one Track."
                );
            }

            /*
             * This is the actual mutable rehearsal source
             * from which the DemoTape recording snapshots
             * below are produced.
             *
             * A scenario-seeded recording therefore never
             * appears without its logical RehearsalSet.
             */
            sourceRehearsalSet =
                new RehearsalSet(
                    vhsSetId:
                        data.sourceSetId,
                    displayName:
                        data.sourceSetName,
                    createdTurn:
                        recordedTurn
                );

            HashSet<string> trackIds =
                new(StringComparer.Ordinal);

            HashSet<string> ideaIds =
                new(StringComparer.Ordinal);

            List<DemoTapeTrackSnapshot>
                snapshots =
                    new(data.tracks.Count);

            foreach (
                JsonTrack trackData
                in data.tracks)
            {
                Track track =
                    BuildTrack(
                        trackData,
                        sourceEntityId,
                        recordedTurn,
                        trackIds,
                        ideaIds
                    );

                /*
                 * Source first.
                 *
                 * The exact same Track instance is added
                 * to the rehearsal VHS before its immutable
                 * recording snapshot is created.
                 */
                sourceRehearsalSet.AddTrack(
                    track
                );

                snapshots.Add(
                    DemoTapeTrackSnapshotFactory.Create(
                        track,
                        trackData.recordedConveyance
                    )
                );
            }

            return new DemoTape(
                demoTapeId:
                    data.demoTapeId,

                displayName:
                    data.displayName,

                sourceSetId:
                    data.sourceSetId,

                sourceSetName:
                    data.sourceSetName,

                recordedTurn:
                    recordedTurn,

                takeCount:
                    data.takeCount,

                recordingInterest:
                    data.recordingInterest,

                snapshots:
                    snapshots
            );
        }

        private static Track BuildTrack(
            JsonTrack data,
            string sourceEntityId,
            int recordedTurn,
            HashSet<string> trackIds,
            HashSet<string> ideaIds)
        {
            if (data == null)
            {
                throw new ArgumentException(
                    "Authored DemoTape contains a null Track."
                );
            }

            RequireText(
                data.trackId,
                nameof(data.trackId)
            );

            if (!trackIds.Add(data.trackId))
            {
                throw new ArgumentException(
                    "Duplicate authored Track identity | " +
                    $"trackId={data.trackId}"
                );
            }

            RequireUnitInterval(
                data.sourceConveyance,
                nameof(data.sourceConveyance)
            );

            RequireUnitInterval(
                data.recordedConveyance,
                nameof(data.recordedConveyance)
            );

            if (data.ideas == null ||
                data.ideas.Count == 0)
            {
                throw new ArgumentException(
                    "Authored Track contains no Ideas | " +
                    $"trackId={data.trackId}"
                );
            }

            Track track =
                new Track(
                    vhsTrackId:
                        data.trackId,

                    /*
                     * Authored data does not specify Track
                     * titles. The production naming system
                     * owns them.
                     */
                    displayName:
                        "Untitled Track",

                    initialConveyance:
                        data.sourceConveyance,

                    createdTurn:
                        recordedTurn
                );

            foreach (
                JsonIdea ideaData
                in data.ideas)
            {
                track.AddIdea(
                    BuildIdea(
                        ideaData,
                        sourceEntityId,
                        ideaIds
                    )
                );
            }

            if (!TrackNamingGenerator.TryGenerate(
                    track,
                    out string generatedTitle))
            {
                throw new InvalidOperationException(
                    "Track naming failed for authored Track | " +
                    $"trackId={data.trackId}"
                );
            }

            if (!track.TrySetGeneratedDisplayName(
                    generatedTitle))
            {
                throw new InvalidOperationException(
                    "Generated Track name could not be applied | " +
                    $"trackId={data.trackId} | " +
                    $"generatedTitle={generatedTitle}"
                );
            }

            return track;
        }

        private static Idea BuildIdea(
            JsonIdea data,
            string sourceEntityId,
            HashSet<string> ideaIds)
        {
            if (data == null)
            {
                throw new ArgumentException(
                    "Authored Track contains a null Idea."
                );
            }

            RequireText(
                data.ideaId,
                nameof(data.ideaId)
            );

            RequireText(
                data.aspectId,
                nameof(data.aspectId)
            );

            RequireUnitInterval(
                data.conveyance,
                nameof(data.conveyance)
            );

            if (!ideaIds.Add(data.ideaId))
            {
                throw new ArgumentException(
                    "Duplicate authored Idea identity | " +
                    $"ideaId={data.ideaId}"
                );
            }

            /*
             * JsonUtility can materialize an omitted nested
             * serializable object as an empty JsonTag.
             *
             * Therefore null alone is not a reliable test
             * for whether tag data was actually authored.
             */
            bool hasSingleTag =
                HasAuthoredTagData(
                    data.tag
                );

            bool hasDominant =
                HasAuthoredTagData(
                    data.dominantTag
                );

            bool hasSubmissive =
                HasAuthoredTagData(
                    data.submissiveTag
                );

            if (hasSingleTag &&
                !hasDominant &&
                !hasSubmissive)
            {
                return new Idea(
                    ideaId:
                        data.ideaId,

                    aspectId:
                        data.aspectId,

                    tagInstance:
                        BuildTag(data.tag),

                    conveyance:
                        data.conveyance,

                    sourceEntityId:
                        sourceEntityId,

                    sourceContainerType:
                        TagContainerType.Transient
                );
            }

            if (!hasSingleTag &&
                hasDominant &&
                hasSubmissive)
            {
                TagInstance dominant =
                    BuildTag(
                        data.dominantTag
                    );

                TagInstance submissive =
                    BuildTag(
                        data.submissiveTag
                    );

                if (!TagPair.TryCreate(
                        dominant,
                        submissive,
                        out TagPair pair))
                {
                    throw new ArgumentException(
                        "Authored Idea contains an invalid " +
                        "TagPair | " +
                        $"ideaId={data.ideaId}"
                    );
                }

                return new Idea(
                    ideaId:
                        data.ideaId,

                    aspectId:
                        data.aspectId,

                    tagPair:
                        pair,

                    conveyance:
                        data.conveyance,

                    sourceEntityId:
                        sourceEntityId,

                    sourceContainerType:
                        TagContainerType.Transient
                );
            }

            throw new ArgumentException(
                "Authored Idea must contain either exactly " +
                "one solitary tag or exactly one dominant/" +
                "submissive TagPair | " +
                $"ideaId={data.ideaId}"
            );
        }

        private static TagInstance BuildTag(
            JsonTag data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(
                    nameof(data)
                );
            }

            return new TagInstance(
                ParseEnum<TagAxis>(
                    data.axis,
                    nameof(data.axis)
                ),

                ParseEnum<TagPole>(
                    data.pole,
                    nameof(data.pole)
                ),

                ParseEnum<TagDegree>(
                    data.degree,
                    nameof(data.degree)
                )
            );
        }

        private static bool HasAuthoredTagData(
            JsonTag data)
        {
            if (data == null)
            {
                return false;
            }

            /*
             * "Any field present" is intentional.
             *
             * A partially authored malformed Tag should
             * count as authored data and then fail normal
             * BuildTag validation instead of being mistaken
             * for an absent field.
             */
            return
                !string.IsNullOrWhiteSpace(
                    data.axis
                ) ||
                !string.IsNullOrWhiteSpace(
                    data.pole
                ) ||
                !string.IsNullOrWhiteSpace(
                    data.degree
                );
        }

        private static TEnum ParseEnum<TEnum>(
            string value,
            string fieldName)
            where TEnum : struct, Enum
        {
            RequireText(
                value,
                fieldName
            );

            /*
             * Authored files use semantic names such as
             * "Symbolic" and "Weak", never opaque enum
             * integers.
             */
            if (int.TryParse(
                    value,
                    out _))
            {
                throw new ArgumentException(
                    $"{fieldName} must use a named " +
                    "enum value."
                );
            }

            if (!Enum.TryParse(
                    value,
                    ignoreCase: true,
                    out TEnum parsed) ||
                !Enum.IsDefined(
                    typeof(TEnum),
                    parsed))
            {
                throw new ArgumentException(
                    $"Unknown {typeof(TEnum).Name} " +
                    $"value '{value}'."
                );
            }

            return parsed;
        }

        private static void RequireText(
            string value,
            string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    $"{fieldName} cannot be empty."
                );
            }
        }

        private static void RequireUnitInterval(
            float value,
            string fieldName)
        {
            RequireFinite(
                value,
                fieldName
            );

            if (value < 0f ||
                value > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    fieldName
                );
            }
        }

        private static void RequireFiniteNonNegative(
            float value,
            string fieldName)
        {
            RequireFinite(
                value,
                fieldName
            );

            if (value < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    fieldName
                );
            }
        }

        private static void RequireFinite(
            float value,
            string fieldName)
        {
            if (float.IsNaN(value) ||
                float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    fieldName
                );
            }
        }

        /*
         * PRIVATE serializer shims only.
         *
         * These deliberately do not escape from this class.
         * Once parsing finishes, only established domain
         * objects remain.
         */

        [Serializable]
        private sealed class JsonDemoTape
        {
            public string demoTapeId;
            public string displayName;
            public string sourceSetId;
            public string sourceSetName;
            public int takeCount;
            public float recordingInterest;
            public List<JsonTrack> tracks =
                new();
        }

        [Serializable]
        private sealed class JsonTrack
        {
            public string trackId;
            public float sourceConveyance;
            public float recordedConveyance;
            public List<JsonIdea> ideas =
                new();
        }

        [Serializable]
        private sealed class JsonIdea
        {
            public string ideaId;
            public string aspectId;
            public float conveyance;

            public JsonTag tag;
            public JsonTag dominantTag;
            public JsonTag submissiveTag;
        }

        [Serializable]
        private sealed class JsonTag
        {
            public string axis;
            public string pole;
            public string degree;
        }
    }
}