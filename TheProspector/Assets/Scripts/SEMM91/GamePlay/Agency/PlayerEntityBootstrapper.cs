using System;
using SEMM91.Core.Entities;
using SEMM91.Core.Tags;
using UnityEngine;

namespace SEMM91.GamePlay.Agency
{
    public class PlayerEntityBootstrapper
    {
        private readonly Action<string> _log;

        public PlayerEntityBootstrapper(Action<string> log = null)
        {
            _log = log ?? Debug.Log;
        }

        public GameEntity CreateStartingPlayerEntity(ulong clientId)
        {
            GameObject playerEntityObj = new GameObject($"Leader_{clientId}");
            GameEntity playerEntity = playerEntityObj.AddComponent<GameEntity>();

            playerEntity.InitializeIdentity(
                $"Player {clientId}",
                GameEntityType.Character
            );

            playerEntity.AddAspectId("ASPECT_KNOWS_GUITAR");
            playerEntity.AddAspectId("ASPECT_HAS_GUITAR");

            playerEntity.AddTagContainer(TagContainerType.Resonance);
            playerEntity.AddTagContainer(TagContainerType.Conviction);
            playerEntity.AddTagContainer(TagContainerType.Mood);
            playerEntity.AddTagContainer(TagContainerType.Transient);
            
            playerEntity.TrySetTag(
                TagContainerType.Resonance,
                new TagInstance(TagAxis.Physical, TagPole.Negative, TagDegree.Weak)
            );

            playerEntity.TrySetTag(
                TagContainerType.Conviction,
                new TagInstance(TagAxis.Symbolic, TagPole.Negative, TagDegree.Weak)
            );
            playerEntity.TrySetTag(
                TagContainerType.Mood,
                new TagInstance(TagAxis.Symbolic, TagPole.Negative, TagDegree.Weak)
                );
            
            /*playerEntity.TrySetTag(
                TagContainerType.Transient,
                new TagInstance(TagAxis.Expressive, TagPole.Negative, TagDegree.Dominant)
            );*/

            _log?.Invoke(
                $"[ENTITY SEED] {playerEntity.DisplayName} " +
                $"aspects={playerEntity.AspectIds.Count} " +
                $"tags={playerEntity.TagContainers.Count}"
            );

            return playerEntity;
        }
    }
}