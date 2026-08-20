using UnityEngine;

namespace SEMM91.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CassetteAnimationEventRelay : MonoBehaviour
    {
        [SerializeField]
        private CassettePresentationController controller;

        public void PlaySegment2()
        {
            controller.PlaySegment2();
        }

        public void PlaySegment3()
        {
            controller.PlaySegment3();
        }

        public void PlaySegment5()
        {
            controller.PlaySegment5();
        }
        
        public void NotifyReadyReached()
        {
            controller.NotifyReadyReached();
        }

        public void NotifyPlayingReached()
        {
            controller.NotifyPlayingReached();
        }
        
        public void PlayReturnSegment2()
        {
            controller.PlayReturnSegment2();
        }

        public void PlayReturnSegment1()
        {
            controller.PlayReturnSegment1();
        }

        public void NotifyTowerRestReached()
        {
            controller.NotifyTowerRestReached();
        }
    }
}