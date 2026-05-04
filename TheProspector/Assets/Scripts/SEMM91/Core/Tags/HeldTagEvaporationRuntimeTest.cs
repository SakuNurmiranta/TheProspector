using SEMM91.Core.Tags;
using UnityEngine;

public class HeldTagEvaporationRuntimeTest : MonoBehaviour
{
    private void Start()
    {
        var container = new TagContainer(TagContainerType.Transient);

        var tag = new TagInstance(
            TagAxis.Symbolic,
            TagPole.Negative,
            TagDegree.Weak
        );

        container.SetTag(tag);

        container.HeldTag.MarkUnstable(
            1,
            "Opposes conviction; must be expended into an Idea before next turn."
        );

        Debug.Log($"Before boundary: hasHeldTag={container.HasHeldTag}, tag={container.HeldTag}");

        container.ResolveTurnBoundaryLifecycle();

        Debug.Log($"After boundary: hasHeldTag={container.HasHeldTag}");
    }
}