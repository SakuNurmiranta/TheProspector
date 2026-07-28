using SEMM91.Core.Aspects;
using UnityEngine;

public class AspectJsonRegistryRuntimeTest : MonoBehaviour
{
    private void Start()
    {
        TextAsset json = Resources.Load<TextAsset>("AspectData/GlobalAspects");

        var globalRegistry = new GlobalAspectRegistry();
        globalRegistry.LoadFromJson(json);

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
    }
}