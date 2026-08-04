#if UNITY_EDITOR

using SEMM91.Core.Entities;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Rehearsal.Development
{
    /// <summary>
    /// Creates the deterministic semantic fixtures used by
    /// the Peak 1 direct-Rehearsal development harness.
    ///
    /// This class does not place Ideas into a Track.
    /// It only places known Ideas into the authoritative
    /// leader's available Idea inventory.
    /// </summary>
    public static class Peak1DevelopmentIdeaSeeder
    {
        private const string LyricsAspectId =
            "ASPECT_PEAK1_LYRICS";

        private const string VocalsAspectId =
            "ASPECT_PEAK1_VOCALS";

        private const string GuitarAspectId =
            "ASPECT_PEAK1_GUITAR";

        private const string DrumsAspectId =
            "ASPECT_PEAK1_DRUMS";

        public static bool Seed(
            GameEntity playerEntity,
            out string message)
        {
            if (playerEntity == null)
            {
                message =
                    "Cannot seed Ideas without a player entity.";

                return false;
            }

            AddDevelopmentAspects(playerEntity);

            string idPrefix =
                $"PEAK1_{playerEntity.EntityId}";

            int addedCount = 0;

            TagPair profaneSacredPair = new(
                dominantTag:
                new TagInstance(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant
                ),
                submissiveTag:
                new TagInstance(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                )
            );

            addedCount += AddIfMissing(
                playerEntity,
                new Idea(
                    ideaId:
                    $"{idPrefix}_PAIR_PROFANE2_SACRED1",
                    aspectId:
                    LyricsAspectId,
                    tagPair:
                    profaneSacredPair,
                    conveyance:
                    1.0f,
                    sourceEntityId:
                    playerEntity.EntityId,
                    sourceContainerType:
                    TagContainerType.Transient
                )
            );

            addedCount += AddIfMissing(
                playerEntity,
                new Idea(
                    ideaId:
                    $"{idPrefix}_SOLITARY_SACRED2",
                    aspectId:
                    VocalsAspectId,
                    tagInstance:
                    new TagInstance(
                        TagAxis.Symbolic,
                        TagPole.Positive,
                        TagDegree.Dominant
                    ),
                    conveyance:
                    1.0f,
                    sourceEntityId:
                    playerEntity.EntityId,
                    sourceContainerType:
                    TagContainerType.Transient
                )
            );

            addedCount += AddIfMissing(
                playerEntity,
                new Idea(
                    ideaId:
                    $"{idPrefix}_SOLITARY_RAW2",
                    aspectId:
                    GuitarAspectId,
                    tagInstance:
                    new TagInstance(
                        TagAxis.Expressive,
                        TagPole.Negative,
                        TagDegree.Dominant
                    ),
                    conveyance:
                    1.0f,
                    sourceEntityId:
                    playerEntity.EntityId,
                    sourceContainerType:
                    TagContainerType.Transient
                )
            );

            addedCount += AddIfMissing(
                playerEntity,
                new Idea(
                    ideaId:
                    $"{idPrefix}_SOLITARY_PROFANE1",
                    aspectId:
                    DrumsAspectId,
                    tagInstance:
                    new TagInstance(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Weak
                    ),
                    conveyance:
                    1.0f,
                    sourceEntityId:
                    playerEntity.EntityId,
                    sourceContainerType:
                    TagContainerType.Transient
                )
            );

            message =
                $"Peak 1 Idea seed ready | " +
                $"added={addedCount} | " +
                $"totalIdeas={playerEntity.Ideas.Count}";

            return true;
        }

        private static void AddDevelopmentAspects(
            GameEntity playerEntity)
        {
            playerEntity.AddAspectId(LyricsAspectId);
            playerEntity.AddAspectId(VocalsAspectId);
            playerEntity.AddAspectId(GuitarAspectId);
            playerEntity.AddAspectId(DrumsAspectId);
        }

        private static int AddIfMissing(
            GameEntity playerEntity,
            Idea candidate)
        {
            foreach (Idea existing in playerEntity.Ideas)
            {
                if (existing != null &&
                    existing.IdeaId == candidate.IdeaId)
                {
                    return 0;
                }
            }

            playerEntity.AddIdea(candidate);
            return 1;
        }
    }
}

#endif