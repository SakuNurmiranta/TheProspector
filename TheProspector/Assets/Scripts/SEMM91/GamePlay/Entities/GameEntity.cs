using SEMM91.GamePlay.Entities;
using UnityEngine;

public class GameEntity : MonoBehaviour
{

    [Header("Identity")] 
    [SerializeField] private string entityId;
    [SerializeField] private string displayName;
    [SerializeField] private GameEntityType entityType;
    
    public string EntityId => entityId;
    public string DisplayName => displayName;
    public GameEntityType EntityType => entityType;

    [Header("Scope")] 
    [SerializeField] private string nodeId;
    
    public string NodeId => nodeId;

    [Header("State")]
    [SerializeField] private GameEntityState state;
    
    public GameEntityState State => state;

    [Header("Information")] [SerializeField]
    private InformationScope informationScope;
    public InformationScope InformationScope => informationScope;
    
    [Header("Debug")]
    [SerializeField] private string debugNodeInput;

    [ContextMenu("Set Node (Debug)")]
    private void DebugSetNode()
    {
        SetNode(debugNodeInput);
    }
    
    [ContextMenu("Debug/Add Damaged")]
    private void DebugAddDamaged()
    {
        AddState(GameEntityState.Damaged);
    }

    [ContextMenu("Debug/Remove Damaged")]
    private void DebugRemoveDamaged()
    {
        RemoveState(GameEntityState.Damaged);
    }
    
    [ContextMenu("Debug/Set Information Scope: Experienced")]
    private void DebugSetExperienced()
    {
        SetInformationScope(InformationScope.Experienced);
    }
    
    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            entityId = System.Guid.NewGuid().ToString();
            Debug.Log($"Generated entityId={entityId} for {gameObject.name}");
        }
    }

    public void SetNode(string newNodeId)
    {
        nodeId = newNodeId;
        Debug.Log($"Set entity {entityId} to node {nodeId}");
    }

    public bool HasState(GameEntityState targetState)
    {
        return (state & targetState) != 0;
    }

    public void AddState(GameEntityState newState)
    {
        state |= newState;
        Debug.Log($"Added state {newState} to entity {entityId}");
    }

    public void RemoveState(GameEntityState removedState)
    {
        state &= ~removedState;
        Debug.Log($"Removed state {removedState} from entity {entityId}");
    }

    public void SetInformationScope(InformationScope newScope)
    {
        informationScope = newScope;
        Debug.Log($"Set information scope for entity {entityId} to {informationScope}");
    }
}
