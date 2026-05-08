using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using UnityEngine;

public class IdeaTRVERuntimeTest : MonoBehaviour
{
    private void Start()
    {
        var singleTagIdea = new Idea(
            "IDEA_SINGLE_TAG",
            "ASPECT_TREMOLO_PICKING",
            new TagInstance(TagAxis.Symbolic, TagPole.Negative, TagDegree.Weak),
            0.75f
        );

        TagPair.TryCreate(
            new TagInstance(TagAxis.Symbolic, TagPole.Negative, TagDegree.Weak),
            new TagInstance(TagAxis.Symbolic, TagPole.Positive, TagDegree.Weak),
            out TagPair validPair
        );

        var validPairIdea = new Idea(
            "IDEA_VALID_PAIR",
            "ASPECT_TREMOLO_PICKING",
            validPair,
            0.8f
        );

        TagPair.TryCreate(
            new TagInstance(TagAxis.Symbolic, TagPole.Negative, TagDegree.Neutral),
            new TagInstance(TagAxis.Symbolic, TagPole.Positive, TagDegree.Weak),
            out TagPair tooWeakPair
        );

        var tooWeakPairIdea = new Idea(
            "IDEA_TOO_WEAK_PAIR",
            "ASPECT_TREMOLO_PICKING",
            tooWeakPair,
            0.8f
        );

        bool invalidPairCreated = TagPair.TryCreate(
            new TagInstance(TagAxis.Symbolic, TagPole.Negative, TagDegree.Weak),
            new TagInstance(TagAxis.Expressive, TagPole.Positive, TagDegree.Weak),
            out TagPair invalidPair
        );

        Debug.Log($"Invalid pair created? {invalidPairCreated}");

        Debug.Log($"Single tag TRVE eligible? {singleTagIdea.IsTRVEEligible()}");
        Debug.Log($"Valid pair TRVE eligible? {validPairIdea.IsTRVEEligible()}");
        Debug.Log($"Too weak pair TRVE eligible? {tooWeakPairIdea.IsTRVEEligible()}");
    }
}