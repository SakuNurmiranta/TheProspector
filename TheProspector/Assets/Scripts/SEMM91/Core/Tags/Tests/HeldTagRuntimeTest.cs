using UnityEngine;

namespace SEMM91.Core.Tags.Tests
{
    public class HeldTagRuntimeTest : MonoBehaviour
    {
        private void Start()
        {
            var container = new TagContainer(TagContainerType.Transient);

            var tag = new TagInstance(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Weak
            );

            container.SetTag(tag);

            container.HeldTag.MarkUnstable(
                1,
                "Opposes conviction; must be expended into an Idea before next turn."
            );

            Debug.Log($"Held tag test: {container.HeldTag}");
        }
    }
}