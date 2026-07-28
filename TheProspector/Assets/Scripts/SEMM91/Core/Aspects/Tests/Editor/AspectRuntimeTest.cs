using SEMM91.Core.Aspects;
using UnityEngine;

public class AspectRuntimeTest : MonoBehaviour
{
    private void Start()
    {
        // Earlier basic aspect test: no requirements, no parent.
        var guitarAspect = new Aspect(
            "ASPECT_GUITAR_CAPABILITY",
            "Knows Guitar",
            "The character can express ideas through guitar playing.",
            AspectType.Capability
        );

        // Earlier requirement test: capability requiring item access.
        var guitarCapability = new Aspect(
            "ASPECT_KNOWS_GUITAR",
            "Knows Guitar",
            "The character can express ideas through guitar playing.",
            AspectType.Capability,
            new[] { "ASPECT_HAS_GUITAR" }
        );

        // New tree test: a specific guitar technique under Knows Guitar.
        var tremoloPicking = new Aspect(
            "ASPECT_TREMOLO_PICKING",
            "Tremolo Picking",
            "The character can perform rapid repeated picking as a guitar technique.",
            AspectType.Capability,
            new[] { "ASPECT_KNOWS_GUITAR", "ASPECT_HAS_GUITAR" },
            "ASPECT_KNOWS_GUITAR"
        );

        Debug.Log($"Basic aspect test: {guitarAspect}");
        Debug.Log($"Requirement aspect test: {guitarCapability}");
        Debug.Log($"Tree child aspect test: {tremoloPicking}");

        foreach (string requirement in guitarCapability.RequiredAspectIds)
        {
            Debug.Log($"Guitar capability requirement: {requirement}");
        }

        foreach (string requirement in tremoloPicking.RequiredAspectIds)
        {
            Debug.Log($"Tremolo requirement: {requirement}");
        }

        Debug.Log($"Tremolo has parent: {tremoloPicking.HasParent}");
        Debug.Log($"Tremolo parent: {tremoloPicking.ParentAspectId}");
    }
}