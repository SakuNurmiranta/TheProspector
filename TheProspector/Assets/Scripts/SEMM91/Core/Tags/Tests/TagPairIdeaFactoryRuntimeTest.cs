using SEMM91.Core.Aspects;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Entities;
using UnityEngine;

public class TagPairIdeaFactoryRuntimeTest : MonoBehaviour
{
    private void Start()
    {
        TextAsset globalJson = Resources.Load<TextAsset>("AspectData/GlobalAspects");

        var globalRegistry = new GlobalAspectRegistry();
        globalRegistry.LoadFromJson(globalJson);

        var usabilityEvaluator = new AspectUsabilityEvaluator(globalRegistry);
        var ideaFactory = new IdeaFactory(usabilityEvaluator);

        GameEntity entity = GetComponent<GameEntity>();

        entity.AddAspectId("ASPECT_KNOWS_GUITAR");
        entity.AddAspectId("ASPECT_HAS_GUITAR");

        var convictionContainer = new TagContainer(TagContainerType.Conviction);
        var transientContainer = new TagContainer(TagContainerType.Transient);

        convictionContainer.SetTag(
            new TagInstance(TagAxis.Symbolic, TagPole.Positive, TagDegree.Weak)
        );

        transientContainer.SetTag(
            new TagInstance(TagAxis.Symbolic, TagPole.Negative, TagDegree.Weak)
        );

        bool result = ideaFactory.TryCreateTagPairIdeaFromHeldTags(
            entity,
            "ASPECT_TREMOLO_PICKING",
            transientContainer,
            convictionContainer,
            0.8f,
            out Idea idea
        );

        if (result)
        {
            entity.AddIdea(idea);
        }

        Debug.Log($"Tag-pair idea creation result: {result}");
        Debug.Log($"Created tag-pair idea: {idea}");
        Debug.Log($"Idea TRVE eligible? {idea != null && idea.IsTRVEEligible()}");
        Debug.Log($"Transient still has held tag: {transientContainer.HasHeldTag}");
        Debug.Log($"Conviction still has held tag: {convictionContainer.HasHeldTag}");
        Debug.Log($"Entity idea count: {entity.Ideas.Count}");
    }
}