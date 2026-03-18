using UnityEngine;

namespace SEMM91.UI
{
    public class UIFocusAnchor : MonoBehaviour
    {
        [SerializeField] private GameUIState state;
        [SerializeField] private float targetZoom = 5f;

        public GameUIState State => state;
        public float TargetZoom => targetZoom;
        public Vector3 Position => transform.position;
    }
}