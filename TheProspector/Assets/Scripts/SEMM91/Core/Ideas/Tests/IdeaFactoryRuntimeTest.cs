using SEMM91.Core.Aspects;
using SEMM91.Core.Entities;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using UnityEngine;

public class IdeaFactoryRuntimeTest : MonoBehaviour
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

        var transientContainer = new TagContainer(TagContainerType.Transient);

        transientContainer.SetTag(
            new TagInstance(TagAxis.Symbolic, TagPole.Negative, TagDegree.Weak)
        );

        bool result = ideaFactory.TryCreateIdeaFromHeldTag(
            entity,
            "ASPECT_TREMOLO_PICKING",
            transientContainer,
            0.75f,
            out Idea idea
        );

        if (result)
        {
            entity.AddIdea(idea);   
        }

        Debug.Log($"Idea creation result: {result}");
        Debug.Log($"Created idea: {idea}");
        Debug.Log($"Entity idea count: {entity.Ideas.Count}");
        Debug.Log($"Transient container still has held tag: {transientContainer.HasHeldTag}");
    }
}