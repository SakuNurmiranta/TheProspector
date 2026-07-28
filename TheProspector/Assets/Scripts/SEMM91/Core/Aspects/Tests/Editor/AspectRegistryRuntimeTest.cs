using SEMM91.Core.Aspects;
using UnityEngine;

public class AspectRegistryRuntimeTest : MonoBehaviour
{
    private void Start()
    {
        var globalRegistry = new GlobalAspectRegistry();

        var hasGuitar = new Aspect(
            "ASPECT_HAS_GUITAR",
            "Has Guitar",
            "The character has access to a guitar as a usable item.",
            AspectType.Item
        );

        var knowsGuitar = new Aspect(
            "ASPECT_KNOWS_GUITAR",
            "Knows Guitar",
            "The character can express ideas through guitar playing.",
            AspectType.Capability
        );

        var tremoloPicking = new Aspect(
            "ASPECT_TREMOLO_PICKING",
            "Tremolo Picking",
            "The character can perform rapid repeated picking as a guitar technique.",
            AspectType.Capability,
            new[] { "ASPECT_KNOWS_GUITAR", "ASPECT_HAS_GUITAR" },
            "ASPECT_KNOWS_GUITAR"
        );

        globalRegistry.RegisterAspect(hasGuitar);
        globalRegistry.RegisterAspect(knowsGuitar);
        globalRegistry.RegisterAspect(tremoloPicking);

        var treeProfile = new AspectTreeProfile(globalRegistry);
        treeProfile.DebugPrintTree();

        var kvltRegistry = new KVLTAspectRegistry(globalRegistry);

        kvltRegistry.AcceptAspect("ASPECT_HAS_GUITAR");
        kvltRegistry.AcceptAspect("ASPECT_KNOWS_GUITAR");

        Debug.Log($"Is Tremolo accepted by KVLT? {kvltRegistry.IsAccepted("ASPECT_TREMOLO_PICKING")}");
        Debug.Log($"Is Tremolo novel to KVLT? {kvltRegistry.IsNovel("ASPECT_TREMOLO_PICKING")}");

        kvltRegistry.AcceptAspect("ASPECT_TREMOLO_PICKING");

        Debug.Log($"After canonization, is Tremolo accepted by KVLT? {kvltRegistry.IsAccepted("ASPECT_TREMOLO_PICKING")}");
        Debug.Log($"After canonization, is Tremolo novel to KVLT? {kvltRegistry.IsNovel("ASPECT_TREMOLO_PICKING")}");

        kvltRegistry.DebugPrintAcceptedAspects();
    }
}