using System;
using System.Linq;
using SEMM91.Core.Aspects;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Entities;
using UnityEngine;

namespace SEMM91.GamePlay.Gestation
{
    public class ActionResolver
    {
        private IdeaFactory _ideaFactory;
        private readonly Action<string> _log;
        private readonly Action<string> _errorLog;

        public ActionResolver(
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

        public void ResolveCreateIdea(ulong clientId, GameEntity controller)
        {
            if (controller == null)
                return;

            if (controller.AspectIds.Count == 0)
            {
                _log?.Invoke($"[GESTATE BLOCKED] Client {clientId} has no aspects.");
                return;
            }

            if (!controller.TryGetTagContainer(TagContainerType.Conviction, out var tagContainer))
            {
                _log?.Invoke($"[GESTATE BLOCKED] Client {clientId} has no conviction tag container.");
                return;
            }

            if (!tagContainer.HasHeldTag)
            {
                _log?.Invoke($"[GESTATE BLOCKED] Client {clientId} has no conviction tag.");
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

            _log?.Invoke(
                $"[GESTATE CREATED] Client {clientId} controller={controller.DisplayName} " +
                $"idea={idea} totalIdeas={controller.Ideas.Count}"
            );
        }
    }
}