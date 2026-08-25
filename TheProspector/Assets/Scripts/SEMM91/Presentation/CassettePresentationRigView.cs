using TMPro;
using UnityEngine;

namespace SEMM91.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CassettePresentationRigView : MonoBehaviour
    {
        [Header("Identity Presentation")]
        [SerializeField]
        private TMP_Text physicalTitleText;

        private CassettePresentationData _currentData;

        public CassettePresentationData CurrentData =>
            _currentData;

        public void Apply(
            CassettePresentationData data)
        {
            _currentData = data;

            if (physicalTitleText != null)
            {
                physicalTitleText.text =
                    data.DisplayName;
            }
        }
    }
}