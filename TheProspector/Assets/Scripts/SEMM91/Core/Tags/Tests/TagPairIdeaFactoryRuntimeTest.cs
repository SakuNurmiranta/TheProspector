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

        Debug.Log("=== DISSEMBODIED CONTAINER TEST: SHOULD FAIL ===");

        var disembodiedConvictionContainer = new TagContainer(TagContainerType.Conviction);
        var disembodiedTransientContainer = new TagContainer(TagContainerType.Transient);

        disembodiedConvictionContainer.SetTag(
            new TagInstance(TagAxis.Symbolic, TagPole.Positive, TagDegree.Weak)
        );

        disembodiedTransientContainer.SetTag(
            new TagInstance(TagAxis.Symbolic, TagPole.Negative, TagDegree.Weak)
        );

        bool disembodiedResult = ideaFactory.TryCreateTagPairIdeaFromHeldTags(
            entity,
            "ASPECT_TREMOLO_PICKING",
            disembodiedTransientContainer,
            disembodiedConvictionContainer,
            0.8f,
            out Idea disembodiedIdea
        );

        Debug.Log($"Disembodied tag-pair idea creation result: {disembodiedResult}");
        Debug.Log($"Disembodied idea: {disembodiedIdea}");

        if (disembodiedResult)
        {
            Debug.LogError(
                "TEST FAILED: IdeaFactory accepted disembodied TagContainers. " +
                "Source containers must belong to the GameEntity."
            );
            return;
        }

        Debug.Log("Disembodied container rejection passed.");

        Debug.Log("=== GAMEENTITY-OWNED CONTAINER TEST: SHOULD PASS ===");

        if (!entity.TryGetTagContainer(TagContainerType.Conviction, out TagContainer convictionContainer))
        {
            Debug.LogError("TEST FAILED: GameEntity does not have a Conviction container.");
            return;
        }

        if (!entity.TryGetTagContainer(TagContainerType.Transient, out TagContainer transientContainer))
        {
            Debug.LogError("TEST FAILED: GameEntity does not have a Transient container.");
            return;
        }

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

        if (idea != null)
        {
            Debug.Log($"Idea source entity: {idea.SourceEntityId}");
            Debug.Log($"Idea source container: {idea.SourceContainerType}");
        }

        if (!result)
        {
            Debug.LogError("TEST FAILED: GameEntity-owned containers did not create a tag-pair idea.");
            return;
        }

        if (idea == null || !idea.IsTRVEEligible())
        {
            Debug.LogError("TEST FAILED: Created idea is null or not TRVE eligible.");
            return;
        }

        if (transientContainer.HasHeldTag)
        {
            Debug.LogError("TEST FAILED: Transient container should have been consumed.");
            return;
        }

        if (!convictionContainer.HasHeldTag)
        {
            Debug.LogError("TEST FAILED: Conviction container should have remained held.");
            return;
        }

        if (entity.Ideas.Count == 0)
        {
            Debug.LogError("TEST FAILED: Created idea was not stored on the GameEntity.");
            return;
        }

        Debug.Log("TAG-PAIR IDEA FACTORY TEST PASSED.");
    }
}