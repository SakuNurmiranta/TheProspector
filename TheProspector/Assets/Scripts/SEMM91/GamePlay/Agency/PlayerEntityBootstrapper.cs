using System;
using SEMM91.Core.Entities;
using SEMM91.Core.Tags;
using UnityEngine;

namespace SEMM91.GamePlay.Agency
{
    /// <summary>
    /// Constructs the server-domain character shell for
    /// one playable participant.
    ///
    /// Scenario-owned starting media, historical
    /// recordings and Scene state are deliberately not
    /// created here.
    /// </summary>
    public sealed class PlayerEntityBootstrapper
    {
        private readonly Action<string> _log;

        private const string LyricsAspectId =
            "ASPECT_LYRICS";

        private const string VocalsAspectId =
            "ASPECT_VOCALS";

        private const string GuitarAspectId =
            "ASPECT_GUITAR";

        private const string DrumsAspectId =
            "ASPECT_DRUMS";

        public PlayerEntityBootstrapper(
            Action<string> log = null)
        {
            _log =
                log ??
                Debug.Log;
        }

        public GameEntity CreatePlayerEntity(
            ulong clientId,
            string displayName)
        {
            if (string.IsNullOrWhiteSpace(
                    displayName))
            {
                throw new ArgumentException(
                    "Player display name cannot be empty.",
                    nameof(displayName)
                );
            }

            GameObject playerEntityObject =
                new GameObject(
                    $"Leader_{clientId}"
                );

            GameEntity playerEntity =
                playerEntityObject
                    .AddComponent<GameEntity>();

            playerEntity.InitializeIdentity(
                displayName.Trim(),
                GameEntityType.Character
            );

            /*
             * Persistent playable capabilities.
             *
             * These are character facts, not startup
             * media fixtures.
             */
            playerEntity.AddAspectId(
                "ASPECT_KNOWS_GUITAR"
            );

            playerEntity.AddAspectId(
                "ASPECT_HAS_GUITAR"
            );

            playerEntity.AddAspectId(
                LyricsAspectId
            );

            playerEntity.AddAspectId(
                VocalsAspectId
            );

            playerEntity.AddAspectId(
                GuitarAspectId
            );

            playerEntity.AddAspectId(
                DrumsAspectId
            );

            playerEntity.AddTagContainer(
                TagContainerType.Resonance
            );

            playerEntity.AddTagContainer(
                TagContainerType.Conviction
            );

            playerEntity.AddTagContainer(
                TagContainerType.Mood
            );

            playerEntity.AddTagContainer(
                TagContainerType.Transient
            );

            playerEntity.TrySetTag(
                TagContainerType.Resonance,
                new TagInstance(
                    TagAxis.Physical,
                    TagPole.Negative,
                    TagDegree.Weak
                )
            );

            playerEntity.TrySetTag(
                TagContainerType.Conviction,
                new TagInstance(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                )
            );

            playerEntity.TrySetTag(
                TagContainerType.Mood,
                new TagInstance(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                )
            );

            _log?.Invoke(
                "[PLAYER ENTITY CREATED] " +
                $"client={clientId} | " +
                $"entity={playerEntity.EntityId} | " +
                $"name={playerEntity.DisplayName} | " +
                $"aspects={playerEntity.AspectIds.Count} | " +
                $"tagContainers=" +
                $"{playerEntity.TagContainers.Count} | " +
                $"vhsSets={playerEntity.VhsSets.Count} | " +
                $"demoTapes={playerEntity.DemoTapes.Count}"
            );

            return playerEntity;
        }
    }
}
