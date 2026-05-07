using SEMM91.Core.Aspects;
using SEMM91.GamePlay.Entities;
using UnityEngine;

public class AspectUsabilityRuntimeTest : MonoBehaviour
{
    private void Start()
    {
        TextAsset globalJson = Resources.Load<TextAsset>("AspectData/GlobalAspects");

        var globalRegistry = new GlobalAspectRegistry();
        globalRegistry.LoadFromJson(globalJson);

        var evaluator = new AspectUsabilityEvaluator(globalRegistry);

        GameEntity entity = GetComponent<GameEntity>();

        string tremolo = "ASPECT_TREMOLO_PICKING";

        Debug.Log($"Can use Tremolo with no requirements? {evaluator.CanUseAspect(entity, tremolo)}");

        entity.AddAspectId("ASPECT_KNOWS_GUITAR");
        Debug.Log($"Can use Tremolo with only Knows Guitar? {evaluator.CanUseAspect(entity, tremolo)}");

        entity.AddAspectId("ASPECT_HAS_GUITAR");
        Debug.Log($"Can use Tremolo with Knows Guitar + Has Guitar? {evaluator.CanUseAspect(entity, tremolo)}");
    }
}