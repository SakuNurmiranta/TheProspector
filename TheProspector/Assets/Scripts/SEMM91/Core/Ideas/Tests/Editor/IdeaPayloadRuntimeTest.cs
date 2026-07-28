using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using UnityEngine;

public class IdeaPayloadRuntimeTest : MonoBehaviour
{
    private void Start()
    {
        var singleTagIdea = new Idea(
            "IDEA_SINGLE_TAG_TEST",
            "ASPECT_TREMOLO_PICKING",
            new TagInstance(TagAxis.Symbolic, TagPole.Negative, TagDegree.Weak),
            0.75f
        );

        var tagPair = new TagPair(
            new TagInstance(TagAxis.Symbolic, TagPole.Negative, TagDegree.Weak),
            new TagInstance(TagAxis.Symbolic, TagPole.Positive, TagDegree.Weak)
        );

        var tagPairIdea = new Idea(
            "IDEA_TAG_PAIR_TEST",
            "ASPECT_TREMOLO_PICKING",
            tagPair,
            0.8f
        );

        Debug.Log($"Single tag idea: {singleTagIdea}");
        Debug.Log($"Single tag idea payload type: {singleTagIdea.PayloadType}");

        Debug.Log($"Tag-pair idea: {tagPairIdea}");
        Debug.Log($"Tag-pair idea payload type: {tagPairIdea.PayloadType}");
        Debug.Log($"Tag-pair valid opposition: {tagPairIdea.TagPair.IsValidOpposition()}");
    }
}