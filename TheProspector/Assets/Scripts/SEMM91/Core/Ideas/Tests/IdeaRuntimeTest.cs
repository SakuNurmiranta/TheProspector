using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using UnityEngine;

public class IdeaRuntimeTest : MonoBehaviour
{
    private void Start()
    {
        var idea = new Idea(
            "IDEA_TEST_TREMOLO_PROFANE",
            "ASPECT_TREMOLO_PICKING",
            new TagInstance(TagAxis.Symbolic, TagPole.Negative, TagDegree.Weak),
            0.75f
        );

        Debug.Log($"Idea test: {idea}");
    }
}