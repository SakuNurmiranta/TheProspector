using SEMM91.Core.Tags;
using SEMM91.GamePlay.Entities;
using UnityEngine;

public class ConvictionTransientRuntimeTest : MonoBehaviour
{
    private void Start()
    {
        GameEntity entity = GetComponent<GameEntity>();

        entity.TrySetTag(
            TagContainerType.Conviction,
            new TagInstance(TagAxis.Symbolic, TagPole.Positive, TagDegree.Weak)
        );

        entity.TrySetTag(
            TagContainerType.Transient,
            new TagInstance(TagAxis.Symbolic, TagPole.Negative, TagDegree.Weak)
        );
    }
}