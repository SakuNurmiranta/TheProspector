using System;
using System.Linq;
using SEMM91.Core.Aspects;
using SEMM91.Core.Entities;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using UnityEngine;

namespace SEMM91.GamePlay.Gestation
{
    public class GestationActionResolver
    {
        private IdeaFactory _ideaFactory;
        private readonly Action<string> _log;
        private readonly Action<string> _errorLog;

        public GestationActionResolver(
            Action<string> log = null,
            Action<string> errorLog = null)
        {
            _log = log ?? Debug.Log;
            _errorLog = errorLog ?? Debug.LogError;
        }

        public void Initialize()
        {
            TextAsset globalJson = Resources.Load<TextAsset>("AspectData/GlobalAspects");

            if (globalJson == null)
            {
                _errorLog?.Invoke("[GestationActionResolver] Could not load global aspect data.");
                return;
            }

            var globalRegistry = new GlobalAspectRegistry();
            globalRegistry.LoadFromJson(globalJson);

            var usabilityEvaluator = new AspectUsabilityEvaluator(globalRegistry);
            _ideaFactory = new IdeaFactory(usabilityEvaluator);

            _log?.Invoke("[GestationActionResolver] Idea factory initialized.");
        }

        public void ResolveCreateIdea(
            ulong clientId, 
            GameEntity controller,
            TagContainerType sourceContainerType
            )
        {
            if (controller == null)
                return;

            if (controller.AspectIds.Count == 0)
            {
                _log?.Invoke($"[GESTATE BLOCKED] Client {clientId} has no aspects.");
                return;
            }

            if (!controller.TryGetTagContainer(
                    sourceContainerType,
                    out var tagContainer))
            {
                _log?.Invoke(
                    $"[GESTATE BLOCKED] Client {clientId} " +
                    $"has no {sourceContainerType} tag container."
                );

                return;
            }

            if (!tagContainer.HasHeldTag)
            {
                _log?.Invoke(
                    $"[GESTATE BLOCKED] Client {clientId} " +
                    $"has no held tag in {sourceContainerType}."
                );

                return;
            }

            if (_ideaFactory == null)
            {
                _log?.Invoke($"[GESTATE BLOCKED] Client {clientId} has no idea factory initialized.");
                return;
            }

            string aspectId = controller.AspectIds.First();

            bool created = _ideaFactory.TryCreateIdeaFromHeldTag(
                controller,
                aspectId,
                tagContainer,
                0.5f,
                out var idea
            );

            if (!created)
            {
                _log?.Invoke($"[GESTATE BLOCKED] Client {clientId} could not create idea from held tag.");
                return;
            }

            controller.AddIdea(idea);
            
            /*if (tagContainer.ShouldConsumeOnIdeaUse())
            {
                if (!tagContainer.TryExpendHeldTag(out _))
                {
                    _errorLog?.Invoke(
                        $"[GESTATE CONSUMPTION ERROR] Client {clientId} " +
                        $"created an Idea from {sourceContainerType}, " +
                        "but the held tag could not be expended."
                    );
                }
            }*/

            _log?.Invoke(
                $"[GESTATE CREATED] Client {clientId} " +
                $"controller={controller.DisplayName} " +
                $"sourceContainer={sourceContainerType} " +
                $"idea={idea} " +
                $"totalIdeas={controller.Ideas.Count}"
            );
        }

        public bool ResolveCreateTagPairIdea(
            ulong clientId,
            GameEntity controller,
            TagContainerType dominantSourceContainerType)
        {
            if (controller == null)
                return false;

            if (controller.AspectIds.Count == 0)
            {
                _log?.Invoke(
                    $"[GESTATE PAIR BLOCKED] Client {clientId} has no aspects."
                );

                return false;
            }

            if (!controller.TryGetTagContainer(
                    dominantSourceContainerType,
                    out TagContainer dominantSourceContainer) ||
                !dominantSourceContainer.HasHeldTag)
            {
                _log?.Invoke(
                    $"[GESTATE PAIR BLOCKED] Client {clientId} " +
                    $"has no held tag in {dominantSourceContainerType}."
                );

                return false;
            }

            TagContainer submissiveSourceContainer =
                FindFirstOpposedHeldTagContainer(
                    controller,
                    dominantSourceContainer
                );

            if (submissiveSourceContainer == null)
            {
                _log?.Invoke(
                    $"[GESTATE PAIR BLOCKED] Client {clientId} " +
                    $"has no held tag opposed to " +
                    $"{dominantSourceContainerType}."
                );

                return false;
            }

            if (_ideaFactory == null)
            {
                _log?.Invoke(
                    $"[GESTATE PAIR BLOCKED] Client {clientId} " +
                    "has no idea factory initialized."
                );

                return false;
            }

            string aspectId = controller.AspectIds.First();

            bool created =
                _ideaFactory.TryCreateTagPairIdeaFromHeldTags(
                    controller,
                    aspectId,
                    dominantSourceContainer,
                    submissiveSourceContainer,
                    0.5f,
                    out Idea idea
                );

            if (!created)
            {
                _log?.Invoke(
                    $"[GESTATE PAIR BLOCKED] Client {clientId} " +
                    "could not create an Idea from the opposed held tags."
                );

                return false;
            }

            controller.AddIdea(idea);

            _log?.Invoke(
                $"[GESTATE PAIR CREATED] Client {clientId} " +
                $"controller={controller.DisplayName} " +
                $"dominantSource={dominantSourceContainerType} " +
                $"submissiveSource=" +
                $"{submissiveSourceContainer.ContainerType} " +
                $"idea={idea} " +
                $"totalIdeas={controller.Ideas.Count}"
            );

            return true;
        }

        private static TagContainer
            FindFirstOpposedHeldTagContainer(
                GameEntity controller,
                TagContainer dominantSourceContainer)
        {
            TagInstance dominantTag =
                dominantSourceContainer.HeldTag.TagInstance;

            foreach (TagContainer candidate
                     in controller.TagContainers)
            {
                if (candidate == null ||
                    ReferenceEquals(
                        candidate,
                        dominantSourceContainer) ||
                    !candidate.HasHeldTag)
                {
                    continue;
                }

                if (dominantTag.IsOpposedTo(
                        candidate.HeldTag.TagInstance))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
