using SEMM91.Core.Aspects;
using UnityEngine;

public class AspectAcceptanceRegistryRuntimeTest : MonoBehaviour
{
    private void Start()
    {
        TextAsset globalJson = Resources.Load<TextAsset>("AspectData/GlobalAspects");
        TextAsset kvltJson = Resources.Load<TextAsset>("AspectData/KVLTAcceptedAspects");
        TextAsset societyJson = Resources.Load<TextAsset>("AspectData/SocietyAcceptedAspects");

        var globalRegistry = new GlobalAspectRegistry();
        globalRegistry.LoadFromJson(globalJson);

        var kvltRegistry = new AspectAcceptanceRegistry("KVLT", globalRegistry);
        var societyRegistry = new AspectAcceptanceRegistry("Society", globalRegistry);

        kvltRegistry.LoadAcceptedIdsFromJson(kvltJson);
        societyRegistry.LoadAcceptedIdsFromJson(societyJson);

        string tremolo = "ASPECT_TREMOLO_PICKING";

        Debug.Log($"KVLT accepts Tremolo? {kvltRegistry.IsAccepted(tremolo)}");
        Debug.Log($"Society accepts Tremolo? {societyRegistry.IsAccepted(tremolo)}");

        Debug.Log($"Tremolo novel to KVLT? {kvltRegistry.IsNovel(tremolo)}");
        Debug.Log($"Tremolo novel to Society? {societyRegistry.IsNovel(tremolo)}");

        kvltRegistry.AcceptAspect(tremolo);

        Debug.Log("After KVLT canonization:");
        Debug.Log($"KVLT accepts Tremolo? {kvltRegistry.IsAccepted(tremolo)}");
        Debug.Log($"Society accepts Tremolo? {societyRegistry.IsAccepted(tremolo)}");

        societyRegistry.AcceptAspect(tremolo);

        Debug.Log("After Society exposure:");
        Debug.Log($"KVLT accepts Tremolo? {kvltRegistry.IsAccepted(tremolo)}");
        Debug.Log($"Society accepts Tremolo? {societyRegistry.IsAccepted(tremolo)}");
    }
}