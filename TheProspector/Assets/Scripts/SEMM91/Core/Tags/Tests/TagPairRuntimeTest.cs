using SEMM91.Core.Tags;
using UnityEngine;

public class TagPairRuntimeTest : MonoBehaviour
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

        var validPair = new TagPair(profane, sacred);

        Debug.Log($"Valid pair: {validPair}");
        Debug.Log($"Is valid opposition: {validPair.IsValidOpposition()}");
        Debug.Log($"Has minimum TRVE degrees: {validPair.HasMinimumTRVEDegrees()}");

        var raw = new TagInstance(
            TagAxis.Expressive,
            TagPole.Negative,
            TagDegree.Weak
        );

        var invalidPair = new TagPair(profane, raw);

        Debug.Log($"Invalid pair: {invalidPair}");
        Debug.Log($"Is valid opposition: {invalidPair.IsValidOpposition()}");
    }
}