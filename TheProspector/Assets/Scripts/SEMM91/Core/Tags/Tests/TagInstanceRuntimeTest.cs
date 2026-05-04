using UnityEngine;

namespace SEMM91.Core.Tags.Tests
{
    public class TagInstanceRuntimeTest : MonoBehaviour
    {
        private void Start()
        {
            var profane = new TagInstance(
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Weak
            );

            var sacred = new TagInstance(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Weak
            );

            Debug.Log($"Opposition test result: {profane.IsOpposedTo(sacred)}");
        }
    }
}