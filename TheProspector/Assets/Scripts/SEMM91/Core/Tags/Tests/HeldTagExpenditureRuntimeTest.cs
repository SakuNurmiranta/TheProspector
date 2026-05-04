using SEMM91.Core.Tags;
using UnityEngine;

public class HeldTagExpenditureRuntimeTest : MonoBehaviour
{
    private void Start()
    {
        var container = new TagContainer(TagContainerType.Transient);

        container.SetTag(
            new TagInstance(TagAxis.Symbolic, TagPole.Negative, TagDegree.Weak)
        );

        container.HeldTag.MarkUnstable(
            1,
            "Opposes conviction; must be expended into an Idea before next turn."
        );

        Debug.Log($"Before expenditure: HasHeldTag={container.HasHeldTag}");
        Debug.Log($"Before expenditure tag: {container.HeldTag}");

        bool result = container.TryExpendHeldTag(out HeldTag expendedTag);

        Debug.Log($"Expenditure result: {result}");
        Debug.Log($"Expended tag: {expendedTag}");
        Debug.Log($"After expenditure: HasHeldTag={container.HasHeldTag}");
    }
}