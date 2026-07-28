using SEMM91.Core.Aspects;
using UnityEngine;

public class AspectCulturalStatusRuntimeTest : MonoBehaviour
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

        var evaluator = new AspectCulturalStatusEvaluator(
            kvltRegistry,
            societyRegistry
        );

        string tremolo = "ASPECT_TREMOLO_PICKING";
        string guitarItem = "ASPECT_GUITAR_ITEM";

        Debug.Log($"Initial tremolo status: {evaluator.GetStatus(tremolo)}");
        Debug.Log(evaluator.GetStatusDescription(tremolo));

        Debug.Log($"Initial guitar item status: {evaluator.GetStatus(guitarItem)}");
        Debug.Log(evaluator.GetStatusDescription(guitarItem));

        kvltRegistry.AcceptAspect(tremolo);

        Debug.Log($"After KVLT canonization: {evaluator.GetStatus(tremolo)}");
        Debug.Log(evaluator.GetStatusDescription(tremolo));

        societyRegistry.AcceptAspect(tremolo);

        Debug.Log($"After Society exposure: {evaluator.GetStatus(tremolo)}");
        Debug.Log(evaluator.GetStatusDescription(tremolo));
    }
}